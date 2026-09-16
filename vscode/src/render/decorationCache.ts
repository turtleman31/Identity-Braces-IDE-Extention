import * as vscode from 'vscode';
import { Settings } from '../settings';
import { BraceStyle } from './braceStyle';
import { GLYPH_CSS, overlayCss } from './css';

/**
 * One decoration type per distinct appearance, kept alive between frames.
 *
 * A `TextEditorDecorationType` is a stylesheet rule. Creating one per brace per frame would
 * be a rule per brace per frame, so instead braces that look alike share a rule and rules
 * are reused across frames — which is what makes the quantisation in
 * {@link module:render/braceStyle} worth doing. The cap is a backstop: an editor left open
 * for a day with several motion traits running should not accumulate rules forever.
 */
const MAX_TYPES = 400;

export class DecorationCache {
    private readonly types = new Map<string, vscode.TextEditorDecorationType>();
    private readonly lastUsed = new Map<string, number>();
    private clock = 0;

    constructor(private settings: Settings) {}

    /** Settings changed, so every rule built from the old ones is wrong. */
    reset(settings: Settings): void {
        this.settings = settings;
        this.disposeAll();
    }

    typeFor(style: BraceStyle): vscode.TextEditorDecorationType {
        this.clock++;
        this.lastUsed.set(style.key, this.clock);

        const existing = this.types.get(style.key);
        if (existing) {
            return existing;
        }

        if (this.types.size >= MAX_TYPES) {
            this.evictOldest();
        }

        const created = vscode.window.createTextEditorDecorationType(this.optionsFor(style));
        this.types.set(style.key, created);
        return created;
    }

    /** A decoration type built straight from render options, for the indent guides. */
    typeForOptions(key: string, options: vscode.DecorationRenderOptions): vscode.TextEditorDecorationType {
        this.clock++;
        this.lastUsed.set(key, this.clock);

        const existing = this.types.get(key);
        if (existing) {
            return existing;
        }

        if (this.types.size >= MAX_TYPES) {
            this.evictOldest();
        }

        const created = vscode.window.createTextEditorDecorationType(options);
        this.types.set(key, created);
        return created;
    }

    disposeAll(): void {
        for (const type of this.types.values()) {
            type.dispose();
        }

        this.types.clear();
        this.lastUsed.clear();
    }

    private evictOldest(): void {
        let oldestKey: string | null = null;
        let oldest = Number.MAX_SAFE_INTEGER;

        for (const [key, used] of this.lastUsed) {
            if (used < oldest) {
                oldest = used;
                oldestKey = key;
            }
        }

        if (oldestKey !== null) {
            this.types.get(oldestKey)?.dispose();
            this.types.delete(oldestKey);
            this.lastUsed.delete(oldestKey);
        }
    }

    private optionsFor(style: BraceStyle): vscode.DecorationRenderOptions {
        const options: vscode.DecorationRenderOptions = {
            rangeBehavior: vscode.DecorationRangeBehavior.ClosedClosed,
        };

        // A brace is never invisible by accident. When nothing is drawn over it the real
        // character keeps its colour; it only goes transparent when something is about to
        // take its place in the pseudo-element below.
        options.color = style.color ?? 'transparent';

        if (style.spanCss) {
            options.textDecoration = `none; ${style.spanCss}`;
        }

        if (style.glyph) {
            options.before = {
                contentText: style.glyph.text,
                color: style.glyph.color,
                textDecoration: `none; ${GLYPH_CSS}` + (style.glyph.css ? `; ${style.glyph.css}` : ''),
            };
        }

        if (style.overlay) {
            options.after = {
                contentText: '',
                textDecoration: `none; ${overlayCss(style.overlay, this.settings)}`,
            };
        }

        return options;
    }
}
