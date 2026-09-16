import { BraceKind } from './braceInfo';
import { TRAIT_CATALOG } from './traitCatalog';
import { TraitWeight } from './traitTable';

/** Everything {@link scanBraces} needs, with no reference to the editor. */
export interface ScanSettings {
    curly: boolean;
    round: boolean;
    square: boolean;

    /**
     * Every trait and how often it comes up, in catalogue order — the order decides which
     * trait a roll lands on, so it is part of the identity contract, not a detail.
     */
    traitWeights: readonly TraitWeight[];

    /**
     * When true, a brace with no partner always renders as '?'. A brace that has lost its
     * other half genuinely does not know who it is, and it doubles as a syntax hint.
     */
    questionUnmatched: boolean;

    /**
     * When true, a closing brace gets its own identity instead of inheriting its opener's —
     * so `{` and its `}` are different colours, and may be different creatures entirely.
     */
    independentBraces: boolean;

    /**
     * Colour by nesting depth instead of by identity — the one setting here that makes code
     * *easier* to read.
     *
     * Resolved in the scanner rather than at either consumer, because a personality brace a
     * different colour from the plain brace beside it looks like a bug in the palette. One
     * value, one place, both paths.
     */
    colorByDepth: boolean;

    /** Braces nested at least this deep are visibly distressed. Zero switches it off. */
    complexityWarningDepth: number;

    /** How many palette entries the colour index is taken modulo. */
    paletteCount: number;
}

/** The catalogue's own defaults, for tests and for a first run. */
export function defaultWeights(): TraitWeight[] {
    return TRAIT_CATALOG.map((info) => ({
        id: info.id,
        layer: info.layer,
        percent: info.defaultPercent,
    }));
}

/**
 * Catalogue defaults with `overrides` applied by id.
 *
 * Kept in catalogue order regardless of the order the overrides arrive in, because the
 * flattened trait table walks the list in order and a reshuffle would hand every brace a
 * different personality.
 */
export function weightsFrom(overrides: Readonly<Record<string, number>> | undefined): TraitWeight[] {
    return TRAIT_CATALOG.map((info) => {
        const override = overrides?.[info.id];
        const percent = typeof override === 'number' && isFinite(override) ? override : info.defaultPercent;
        return { id: info.id, layer: info.layer, percent: Math.max(0, Math.min(100, percent)) };
    });
}

export function defaultScanSettings(): ScanSettings {
    return {
        curly: true,
        round: true,
        square: true,
        traitWeights: defaultWeights(),
        questionUnmatched: true,
        independentBraces: true,
        colorByDepth: false,
        complexityWarningDepth: 0,
        paletteCount: 32,
    };
}

export function includesKind(settings: ScanSettings, kind: BraceKind): boolean {
    switch (kind) {
        case BraceKind.Curly:
            return settings.curly;
        case BraceKind.Round:
            return settings.round;
        case BraceKind.Square:
            return settings.square;
        default:
            return false;
    }
}
