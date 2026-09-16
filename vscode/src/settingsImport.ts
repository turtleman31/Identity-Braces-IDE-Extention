import * as os from 'node:os';
import * as path from 'node:path';
import { TRAIT_CATALOG } from './core/traitCatalog';
import { isImplemented } from './render/traitDrawing';

/**
 * Reads the Visual Studio extension's settings file and turns it into VS Code settings.
 *
 * The two extensions keep their settings in completely different places — a hand-rolled
 * key/value file under `%APPDATA%` on one side, the workspace configuration on the other —
 * but they describe the same braces, so anyone running both should only have to decide once.
 *
 * The import is deliberately *authoritative*: every mapped key is written, including the
 * ones that happen to match the VS Code default. Writing only the differences would leave
 * whatever was in `settings.json` beforehand in charge of the rest, and "port my settings"
 * would quietly mean "port some of my settings".
 */

/** Where the Visual Studio extension keeps its settings. */
export function visualStudioSettingsPath(): string {
    const appData = process.env.APPDATA ?? path.join(os.homedir(), 'AppData', 'Roaming');
    return path.join(appData, 'IdentityBraces', 'settings.ini');
}

export interface ImportResult {
    /** The VS Code settings to write, keyed without the `identityBraces.` prefix. */
    settings: Record<string, unknown>;

    /** Things the caller should say out loud, because they did not survive the crossing. */
    notes: string[];
}

const COLOR_MODES = ['palette', 'monochrome', 'depth'] as const;
const STOCKING_STYLES = ['off', 'twotone', 'garter', 'banded'] as const;

/**
 * Settings that exist in Visual Studio and have no meaning here.
 *
 * Named rather than silently dropped: someone who set `ReserveEarSpace` did it for a reason
 * and deserves to know it has not come with them.
 */
const UNTRANSLATABLE: Record<string, string> = {
    SceneIntervalSeconds: 'scenes are not implemented in this port',
    ReserveEarSpace: 'VS Code cannot make one line taller than another, so the ears overhang instead',
    BraceScalePercent: 'the glyph is drawn by the editor here, at the editor font size',
    DiagnosticLog: 'no equivalent',
};

export function importVisualStudioSettings(ini: string): ImportResult {
    const values = parseIni(ini);
    const notes: string[] = [];

    const bool = (key: string, fallback: boolean) => {
        const raw = values.get(key);
        return raw === undefined ? fallback : raw.trim().toLowerCase() === 'true';
    };

    const int = (key: string, fallback: number) => {
        const parsed = Number.parseInt(values.get(key) ?? '', 10);
        return Number.isFinite(parsed) ? parsed : fallback;
    };

    const settings: Record<string, unknown> = {
        enabled: bool('Enabled', true),
        curly: bool('ColorCurlyBraces', true),
        round: bool('ColorParentheses', true),
        square: bool('ColorSquareBrackets', true),
        questionUnmatched: bool('QuestionUnmatched', true),
        independentBraces: bool('IndependentBraces', true),

        colorMode: COLOR_MODES[clamp(int('ColorMode', 0), 0, 2)],
        scopeSpotlight: bool('ScopeSpotlight', false),
        spotlightDim: clamp(int('SpotlightDimPercent', 30), 0, 100),
        complexityWarningDepth: clamp(int('ComplexityWarningDepth', 0), 0, 64),
        indentGuides: bool('IndentGuides', false),
        indentGuideStrength: clamp(int('IndentGuideOpacityPercent', 45), 5, 100),

        enableMotion: bool('EnableMotion', true),
        animationFrameRate: clamp(int('AnimationFrameRate', 15), 1, 60),
        colorCycleSeconds: clamp(int('CycleSeconds', 6), 1, 120),

        maxFileLength: Math.max(0, int('MaxFileLength', 1000000)),

        // Ear scale is the closest thing Visual Studio has to a decoration scale; the brace
        // scale beside it has no counterpart, because here the glyph belongs to the editor.
        decorScale: clamp(int('EarScalePercent', 100), 40, 200) / 100,
        thighHighStyle: STOCKING_STYLES[clamp(int('Stocking', 2), 0, 3)],
        tail: bool('CatgirlTail', true),

        traits: readTraits(values),
    };

    for (const [key, why] of Object.entries(UNTRANSLATABLE)) {
        if (values.has(key)) {
            notes.push(`${key} was not imported — ${why}.`);
        }
    }

    const inert = TRAIT_CATALOG.filter(
        (t) => !isImplemented(t.id) && ((settings.traits as Record<string, number>)[t.id] ?? 0) > 0,
    );

    if (inert.length > 0) {
        notes.push(
            `${inert.map((t) => `${t.name} (${t.id})`).join(', ')} came across at ` +
                'their Visual Studio weights, but do not draw anything in this port. They still ' +
                'roll, so they still cost you the braces they land on.',
        );
    }

    return { settings, notes };
}

/**
 * Every trait, explicitly.
 *
 * Visual Studio only writes the traits it has turned on, and treats anything absent as zero.
 * VS Code falls back to the *catalogue* default for anything absent. So a trait the user
 * deliberately switched off would come back on if the import only copied what it found —
 * which is why the zeroes are written too.
 */
function readTraits(values: Map<string, string>): Record<string, number> {
    const traits: Record<string, number> = {};

    for (const trait of TRAIT_CATALOG) {
        const parsed = Number.parseInt(values.get(`Trait.${trait.id}`) ?? '', 10);
        traits[trait.id] = Number.isFinite(parsed) ? clamp(parsed, 0, 100) : 0;
    }

    return traits;
}

function parseIni(text: string): Map<string, string> {
    const values = new Map<string, string>();

    // The file is written with a BOM, and its comment character is '#'.
    for (const line of text.replace(/^﻿/, '').split(/\r?\n/)) {
        const trimmed = line.trim();
        if (trimmed === '' || trimmed.startsWith('#')) {
            continue;
        }

        const split = trimmed.indexOf('=');
        if (split > 0) {
            values.set(trimmed.slice(0, split).trim(), trimmed.slice(split + 1).trim());
        }
    }

    return values;
}

function clamp(value: number, lo: number, hi: number): number {
    return Math.max(lo, Math.min(hi, value));
}
