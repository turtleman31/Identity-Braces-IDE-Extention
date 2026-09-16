import * as vscode from 'vscode';
import { BraceMap } from './core/braceMap';
import { scanBraces } from './core/braceScanner';
import { withAlpha } from './core/palette';
import { TraitIds } from './core/traitIds';
import { colorFor, computeStyle, RenderContext } from './render/braceStyle';
import { DecorationCache } from './render/decorationCache';
import { appliesTo, readSettings, Settings } from './settings';

/**
 * Keeps every visible editor's braces painted.
 *
 * Two caches sit behind this. The scan is cached per document version, because it is a
 * whole-file lexer and the result is a pure function of the text and the settings. The
 * decoration types are cached per appearance, because they are stylesheet rules. Between
 * them, an animation frame with nothing new on screen costs one pass over the visible
 * braces and a handful of `setDecorations` calls with ranges that were already built.
 */

/**
 * How long to wait after a keystroke before rescanning.
 *
 * The scan is whole-file and linear: about 150&nbsp;ms for a 1.17&nbsp;MB file with 200,000
 * braces, against the 18&nbsp;ms the C# original manages on the same corpus. That gap is the
 * price of doing 64-bit arithmetic on pairs of 32-bit halves, and it is why a big file waits
 * longer before being rescanned — a pause after you stop typing is invisible, a pause in the
 * middle of typing is not.
 */
const RESCAN_DEBOUNCE_MS = 25;

/** Above this many characters, the longer debounce applies. */
const LARGE_DOCUMENT_CHARS = 200000;

/** Roughly the same document, in lines, for when we have not scanned it yet. */
const LARGE_DOCUMENT_LINES = 4000;

const LARGE_RESCAN_DEBOUNCE_MS = 200;

/** How many lines either side of the viewport to decorate, so a small scroll is instant. */
const VIEWPORT_MARGIN_LINES = 20;

interface DocumentState {
    version: number;
    settingsRevision: number;
    map: BraceMap;

    /** What the text measured last time we read it, so the debounce need not read it again. */
    length: number;

    /** One range per brace, built once per document version rather than once per frame. */
    ranges: (vscode.Range | undefined)[];
}

export class Decorator implements vscode.Disposable {
    private settings: Settings;
    private settingsRevision = 0;
    private readonly cache: DecorationCache;
    private readonly documents = new Map<string, DocumentState>();
    private readonly appliedTypes = new Map<string, Set<vscode.TextEditorDecorationType>>();
    private readonly sessionStart = Date.now();

    private rescanTimer: NodeJS.Timeout | undefined;
    private motionTimer: NodeJS.Timeout | undefined;
    private motionWanted = false;

    private readonly disposables: vscode.Disposable[] = [];

    constructor() {
        this.settings = readSettings(undefined);
        this.cache = new DecorationCache(this.settings);

        this.disposables.push(
            vscode.workspace.onDidChangeTextDocument((e) => this.onDocumentChanged(e.document)),
            vscode.workspace.onDidCloseTextDocument((d) => this.documents.delete(d.uri.toString())),
            vscode.window.onDidChangeVisibleTextEditors(() => this.refreshAll()),
            vscode.window.onDidChangeTextEditorVisibleRanges((e) => this.refresh(e.textEditor)),
            vscode.window.onDidChangeTextEditorSelection((e) => this.onSelectionChanged(e.textEditor)),
            vscode.workspace.onDidChangeConfiguration((e) => {
                if (e.affectsConfiguration('identityBraces') || e.affectsConfiguration('editor')) {
                    this.reload();
                }
            }),
        );

        this.refreshAll();
    }

    dispose(): void {
        this.stopMotion();
        if (this.rescanTimer) {
            clearTimeout(this.rescanTimer);
        }

        for (const d of this.disposables) {
            d.dispose();
        }

        this.clearAll();
        this.cache.disposeAll();
    }

    /** Settings changed: every cached scan and every cached rule is now wrong. */
    reload(): void {
        this.settings = readSettings(vscode.window.activeTextEditor?.document.uri);
        this.settingsRevision++;
        this.clearAll();
        this.cache.reset(this.settings);
        this.documents.clear();
        this.refreshAll();
    }

    /** The map behind an editor, for the hover and the identify command. */
    mapFor(document: vscode.TextDocument): BraceMap | null {
        const state = this.stateFor(document);
        return state ? state.map : null;
    }

    get currentSettings(): Settings {
        return this.settings;
    }

    refreshAll(): void {
        for (const editor of vscode.window.visibleTextEditors) {
            this.refresh(editor);
        }
    }

    private onDocumentChanged(document: vscode.TextDocument): void {
        if (this.rescanTimer) {
            clearTimeout(this.rescanTimer);
        }

        // Between the edit and the rescan, VS Code moves the existing decorations with the
        // text itself, so the delay costs a slightly stale personality rather than a screen
        // of braces in the wrong place.
        this.rescanTimer = setTimeout(
            () => {
                this.rescanTimer = undefined;
                this.documents.delete(document.uri.toString());
                for (const editor of vscode.window.visibleTextEditors) {
                    if (editor.document === document) {
                        this.refresh(editor);
                    }
                }
            },
            this.isLarge(document) ? LARGE_RESCAN_DEBOUNCE_MS : RESCAN_DEBOUNCE_MS,
        );
    }

    /**
     * Whether this document is big enough to be worth waiting longer for.
     *
     * From the last scan's own measurement rather than from `getText()`: materialising a
     * megabyte of string on every keystroke to decide how long to wait before reading it is
     * the cost this is supposed to be managing.
     */
    private isLarge(document: vscode.TextDocument): boolean {
        const state = this.documents.get(document.uri.toString());
        return state ? state.length > LARGE_DOCUMENT_CHARS : document.lineCount > LARGE_DOCUMENT_LINES;
    }

    private onSelectionChanged(editor: vscode.TextEditor): void {
        // Only the settings that actually read the caret need a repaint when it moves.
        if (this.settings.scopeSpotlight || this.usesCaretTraits()) {
            this.refresh(editor);
        }
    }

    private usesCaretTraits(): boolean {
        return this.settings.scan.traitWeights.some(
            (w) => w.percent > 0 && (w.id === TraitIds.FleeCursor || w.id === TraitIds.StageFright),
        );
    }

    refresh(editor: vscode.TextEditor): void {
        const state = this.stateFor(editor.document);
        if (!state) {
            this.clear(editor);
            return;
        }

        const grouped = new Map<vscode.TextEditorDecorationType, vscode.Range[]>();
        let animated = false;

        const timeMs = Date.now() - this.sessionStart;
        const sessionMinutes = timeMs / 60000;
        const caret = editor.selection.active;
        const spotlight = this.spotlightRange(state, editor);

        for (const window of this.visibleWindows(editor, state)) {
            for (let i = window.start; i < window.end; i++) {
                const brace = state.map.at(i);
                const range = this.rangeFor(editor.document, state, i);
                const position = range.start;

                const context: RenderContext = {
                    settings: this.settings,
                    timeMs,
                    sessionMinutes,
                    caretLine: caret.line,
                    caretColumn: caret.character,
                    line: position.line,
                    column: position.character,
                    dim: spotlight !== null && (brace.position < spotlight.from || brace.position > spotlight.to),
                };

                const style = computeStyle(brace, context);
                animated = animated || style.animated;
                push(grouped, this.cache.typeFor(style), range);
            }
        }

        if (this.settings.indentGuides) {
            this.addIndentGuides(editor, state, grouped);
        }

        this.apply(editor, grouped);

        if (animated) {
            this.motionWanted = true;
            this.startMotion();
        }
    }

    // ---- scanning ----

    private stateFor(document: vscode.TextDocument): DocumentState | null {
        const key = document.uri.toString();
        const existing = this.documents.get(key);
        if (existing && existing.version === document.version && existing.settingsRevision === this.settingsRevision) {
            return existing;
        }

        const text = document.getText();
        if (!appliesTo(this.settings, document, text.length)) {
            this.documents.delete(key);
            return null;
        }

        const braces = scanBraces(text, this.settings.scan);
        const state: DocumentState = {
            version: document.version,
            settingsRevision: this.settingsRevision,
            map: new BraceMap(braces),
            length: text.length,
            ranges: new Array(braces.length),
        };

        this.documents.set(key, state);
        return state;
    }

    /**
     * Positions are resolved once per document version, not once per frame.
     *
     * `positionAt` is a binary search over the line starts, which is cheap on its own and
     * not cheap fifteen times a second for every brace on screen.
     */
    private rangeFor(document: vscode.TextDocument, state: DocumentState, index: number): vscode.Range {
        const cached = state.ranges[index];
        if (cached) {
            return cached;
        }

        const offset = state.map.at(index).position;
        const start = document.positionAt(offset);
        const range = new vscode.Range(start, start.translate(0, 1));
        state.ranges[index] = range;
        return range;
    }

    private visibleWindows(editor: vscode.TextEditor, state: DocumentState): { start: number; end: number }[] {
        const document = editor.document;
        const windows: { start: number; end: number }[] = [];

        for (const visible of editor.visibleRanges) {
            const firstLine = Math.max(0, visible.start.line - VIEWPORT_MARGIN_LINES);
            const lastLine = Math.min(document.lineCount - 1, visible.end.line + VIEWPORT_MARGIN_LINES);

            const from = document.offsetAt(new vscode.Position(firstLine, 0));
            const to = document.offsetAt(document.lineAt(lastLine).range.end);

            windows.push({
                start: state.map.firstIndexAtOrAfter(from),
                end: state.map.firstIndexAtOrAfter(to + 1),
            });
        }

        return windows;
    }

    // ---- the scope spotlight ----

    /**
     * The span of the innermost pair containing the caret. Everything outside it fades.
     *
     * With thirty-two colours competing this is the one thing that still tells you which
     * block you are in.
     */
    private spotlightRange(state: DocumentState, editor: vscode.TextEditor): { from: number; to: number } | null {
        if (!this.settings.scopeSpotlight) {
            return null;
        }

        const offset = editor.document.offsetAt(editor.selection.active);
        const pair = state.map.enclosingPair(offset);
        if (!pair) {
            return null;
        }

        return {
            from: state.map.at(pair.openIndex).position,
            to: state.map.at(pair.closeIndex).position,
        };
    }

    // ---- coloured indent guides ----

    /**
     * A vertical guide down the inside of every pair that spans more than one line, in that
     * pair's colour, at the indent of the line its opener sits on.
     *
     * Assembled from visible lines only — each visible line contributes its own segment,
     * because a pair can easily span more screens than the monitor has and an off-screen
     * line has no geometry to ask about. Which pairs are open at a given line comes from a
     * walk *up* the nesting rather than back through the file, so scrolling to the end of a
     * large document costs the same as scrolling to the start.
     */
    private addIndentGuides(
        editor: vscode.TextEditor,
        state: DocumentState,
        grouped: Map<vscode.TextEditorDecorationType, vscode.Range[]>,
    ): void {
        const document = editor.document;

        for (const visible of editor.visibleRanges) {
            for (let line = visible.start.line; line <= visible.end.line; line++) {
                const offset = document.offsetAt(new vscode.Position(line, 0));
                let pair = state.map.enclosingPair(offset);

                for (let depth = 0; pair !== null && depth < 64; depth++) {
                    const opener = state.map.at(pair.openIndex);
                    const closer = state.map.at(pair.closeIndex);
                    const openLine = document.positionAt(opener.position).line;
                    const closeLine = document.positionAt(closer.position).line;

                    if (openLine < line && line <= closeLine) {
                        const indent = document.lineAt(openLine).firstNonWhitespaceCharacterIndex;
                        const color = withAlpha(colorFor(opener, this.settings), this.settings.indentGuideStrength);
                        this.addGuide(document, grouped, line, indent, color);
                    }

                    pair = state.map.parentPair(pair.openIndex);
                }
            }
        }
    }

    private addGuide(
        document: vscode.TextDocument,
        grouped: Map<vscode.TextEditorDecorationType, vscode.Range[]>,
        line: number,
        column: number,
        color: string,
    ): void {
        // A guide's column may be past the end of a short line, so the decoration is anchored
        // wherever the line does reach and pushed the rest of the way with a margin. Without
        // that, every blank line inside a block would lose its guides.
        const length = document.lineAt(line).text.length;
        const anchor = Math.min(column, length);
        const range = new vscode.Range(line, anchor, line, anchor);

        const key = `guide:${color}:${column - anchor}`;
        const type = this.cache.typeForOptions(key, {
            rangeBehavior: vscode.DecorationRangeBehavior.ClosedClosed,
            before: {
                contentText: '',
                textDecoration:
                    'none; position: absolute; width: 0; pointer-events: none' +
                    `; height: ${this.settings.lineHeightEm.toFixed(3)}em` +
                    `; border-left: 1px dotted ${color}` +
                    (column > anchor ? `; margin-left: ${column - anchor}ch` : ''),
            },
        });

        push(grouped, type, range);
    }

    // ---- applying ----

    private apply(
        editor: vscode.TextEditor,
        grouped: Map<vscode.TextEditorDecorationType, vscode.Range[]>,
    ): void {
        const key = editorKey(editor);
        const previous = this.appliedTypes.get(key);

        for (const [type, ranges] of grouped) {
            setSafely(editor, type, ranges);
        }

        // Anything painted last pass and not this one has to be told it is empty, or its
        // ranges stay on screen for the life of the type.
        if (previous) {
            for (const type of previous) {
                if (!grouped.has(type)) {
                    setSafely(editor, type, []);
                }
            }
        }

        this.appliedTypes.set(key, new Set(grouped.keys()));
    }

    private clear(editor: vscode.TextEditor): void {
        const key = editorKey(editor);
        for (const type of this.appliedTypes.get(key) ?? []) {
            setSafely(editor, type, []);
        }

        this.appliedTypes.delete(key);
    }

    private clearAll(): void {
        for (const editor of vscode.window.visibleTextEditors) {
            this.clear(editor);
        }

        this.appliedTypes.clear();
    }

    // ---- motion ----

    /**
     * The clock only runs while something on screen is actually moving.
     *
     * A timer that ticks regardless is a background cost paid by every user who never turned
     * a motion trait on. A pass over the visible braces can only ever switch it on; deciding
     * to switch it off is the tick's own job, because no single editor knows whether another
     * one still has something moving in it.
     */
    private startMotion(): void {
        if (this.motionTimer || !this.settings.enableMotion) {
            return;
        }

        const interval = Math.max(16, Math.round(1000 / this.settings.animationFrameRate));
        this.motionTimer = setInterval(() => this.tick(), interval);
    }

    private tick(): void {
        this.motionWanted = false;
        for (const editor of vscode.window.visibleTextEditors) {
            this.refresh(editor);
        }

        if (!this.motionWanted) {
            this.stopMotion();
        }
    }

    private stopMotion(): void {
        if (this.motionTimer) {
            clearInterval(this.motionTimer);
            this.motionTimer = undefined;
        }
    }
}

function push<K>(map: Map<K, vscode.Range[]>, key: K, range: vscode.Range): void {
    const existing = map.get(key);
    if (existing) {
        existing.push(range);
    } else {
        map.set(key, [range]);
    }
}

function editorKey(editor: vscode.TextEditor): string {
    return `${editor.document.uri.toString()}#${editor.viewColumn ?? 'x'}`;
}

/**
 * A decoration type the cache has since evicted takes its decorations with it, so applying
 * to one is both harmless and pointless — but it throws, and one stale type must not cost
 * the pass every brace after it.
 */
function setSafely(
    editor: vscode.TextEditor,
    type: vscode.TextEditorDecorationType,
    ranges: vscode.Range[],
): void {
    try {
        editor.setDecorations(type, ranges);
    } catch {
        // Disposed out from under us.
    }
}
