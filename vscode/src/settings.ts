import * as vscode from 'vscode';
import { PALETTE_COUNT, resolvePalette } from './core/palette';
import { ScanSettings, weightsFrom } from './core/scanSettings';

export type ColorMode = 'palette' | 'monochrome' | 'depth';
export type RenderMode = 'full' | 'text' | 'plain';
export type ThighHighStyle = 'off' | 'twotone' | 'garter' | 'banded';

/** Everything the extension reads out of the workspace configuration, resolved once. */
export interface Settings {
    enabled: boolean;
    scan: ScanSettings;

    palette: readonly string[];
    colorMode: ColorMode;
    monochromeColor: string;

    scopeSpotlight: boolean;
    spotlightDim: number;

    indentGuides: boolean;
    indentGuideStrength: number;

    enableMotion: boolean;
    animationFrameRate: number;
    colorCycleSeconds: number;

    maxFileLength: number;
    decorScale: number;
    thighHighStyle: ThighHighStyle;
    tail: boolean;
    renderMode: RenderMode;
    languages: readonly string[];

    /** Editor font size in CSS pixels, for the one-pixel floors in the drawing primitives. */
    fontSizePx: number;

    /** Line height as a multiple of the font size, which is what places the overlay. */
    lineHeightEm: number;

    fontFamily: string;

    /** A manual vertical correction for fonts whose braces do not sit where we assume. */
    overlayNudgeEm: number;
}

const SECTION = 'identityBraces';

export function readSettings(resource: vscode.Uri | undefined): Settings {
    const config = vscode.workspace.getConfiguration(SECTION, resource);
    const editor = vscode.workspace.getConfiguration('editor', resource);

    const colorMode = config.get<ColorMode>('colorMode', 'palette');
    const fontSizePx = clamp(editor.get<number>('fontSize', 14) || 14, 6, 100);

    return {
        enabled: config.get('enabled', true),
        scan: {
            curly: config.get('curly', true),
            round: config.get('round', true),
            square: config.get('square', true),
            traitWeights: weightsFrom(config.get<Record<string, number>>('traits', {})),
            questionUnmatched: config.get('questionUnmatched', true),
            independentBraces: config.get('independentBraces', true),
            colorByDepth: colorMode === 'depth',
            complexityWarningDepth: Math.max(0, config.get('complexityWarningDepth', 0)),
            paletteCount: PALETTE_COUNT,
        },

        palette: resolvePalette(config.get<string[]>('palette', [])),
        colorMode,
        monochromeColor: config.get('monochromeColor', '').trim(),

        scopeSpotlight: config.get('scopeSpotlight', false),
        spotlightDim: clamp(config.get('spotlightDim', 30), 0, 100) / 100,

        indentGuides: config.get('indentGuides', false),
        indentGuideStrength: clamp(config.get('indentGuideStrength', 45), 5, 100) / 100,

        enableMotion: config.get('enableMotion', true),
        animationFrameRate: clamp(config.get('animationFrameRate', 15), 1, 60),
        colorCycleSeconds: clamp(config.get('colorCycleSeconds', 6), 1, 120),

        maxFileLength: Math.max(0, config.get('maxFileLength', 1000000)),
        decorScale: clamp(config.get('decorScale', 1), 0.4, 2),
        thighHighStyle: config.get<ThighHighStyle>('thighHighStyle', 'garter'),
        tail: config.get('tail', true),
        renderMode: config.get<RenderMode>('renderMode', 'full'),
        languages: config.get<string[]>('languages', ['*']),

        fontSizePx,
        lineHeightEm: lineHeightEm(editor.get<number>('lineHeight', 0), fontSizePx),
        fontFamily: (editor.get<string>('fontFamily', '') || 'monospace').replace(/"/g, "'"),
        overlayNudgeEm: clamp(config.get('overlayNudge', 0), -1, 1),
    };
}

/**
 * The editor's line height, as a multiple of the font size.
 *
 * This is the one editor metric the overlay genuinely needs: half of the leading sits above
 * the text, so it is what decides how far below the top of the line a brace's ink starts.
 * VS Code's own rule is that zero means "derive it", a small number is a multiplier, and
 * anything else is pixels.
 */
function lineHeightEm(configured: number, fontSizePx: number): number {
    const GOLDEN_RATIO = process.platform === 'darwin' ? 1.5 : 1.35;

    if (!configured || configured <= 0) {
        return GOLDEN_RATIO;
    }

    if (configured < 8) {
        return configured;
    }

    return clamp(configured / fontSizePx, 1, 4);
}

/**
 * Whether this document is ours at all.
 *
 * The length is passed in rather than read from the document: the caller already holds the
 * text, and `getText()` on a large file is exactly the cost the length guard exists to
 * avoid paying.
 */
export function appliesTo(settings: Settings, document: vscode.TextDocument, length: number): boolean {
    if (!settings.enabled) {
        return false;
    }

    if (document.uri.scheme === 'output' || document.uri.scheme === 'vscode-terminal') {
        return false;
    }

    if (settings.maxFileLength > 0 && length > settings.maxFileLength) {
        return false;
    }

    return settings.languages.includes('*') || settings.languages.includes(document.languageId);
}

function clamp(value: number, lo: number, hi: number): number {
    if (!isFinite(value)) {
        return lo;
    }

    return Math.max(lo, Math.min(hi, value));
}
