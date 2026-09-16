import { BraceTraits } from './traitRoll';

/** The three bracket families we colour. */
export const enum BraceKind {
    Curly = 0,
    Round = 1,
    Square = 2,
}

/**
 * One brace, with the identity derived for it.
 *
 * Whether a matched pair shares its identity, colour and traits depends on
 * `ScanSettings.independentBraces`. Sharing them was the last thing here that still helped
 * you read code, so it is off by default.
 *
 * The identity is carried as two 32-bit halves rather than a `bigint` for the reason given
 * in {@link module:core/hash} — a scan of a large file touches this a few million times.
 */
export interface BraceInfo {
    /** Offset into the document text. */
    position: number;

    /** The literal character in the buffer. */
    character: string;

    kind: BraceKind;

    isOpen: boolean;

    /** False when this brace has no partner — a real syntax error. */
    isMatched: boolean;

    /** High half of the stable hash of the pair's declaring text. */
    idHi: number;

    /** Low half of the same. */
    idLo: number;

    /** Index into the palette. */
    colorIndex: number;

    /**
     * How many pairs enclose this one. Zero at the outermost level.
     *
     * A closing brace reports its *opener's* depth, not the depth of the position it sits
     * at, so a pair agrees with itself under depth colouring.
     */
    depth: number;

    /**
     * Index of the matching brace in the same map, or -1 when there is none. Set on both
     * halves, so given either brace you can reach the other in one step.
     */
    partnerIndex: number;

    /**
     * Index of the opening brace that encloses this one, or -1 at the outermost level.
     *
     * The scan already has this on its stack, so recording it is free — and it turns "which
     * pairs enclose this position?" from a walk back through the file into a walk up a chain
     * as long as the nesting is deep.
     *
     * It can point at an *unmatched* opener: one that never found its partner stays on the
     * stack and legitimately encloses everything after it.
     */
    parentIndex: number;

    /** What this brace turned out to be, across every layer. */
    traits: BraceTraits;
}

/** True when this brace is drawn rather than merely coloured. */
export function isAdorned(brace: BraceInfo): boolean {
    return brace.traits.isDrawn;
}
