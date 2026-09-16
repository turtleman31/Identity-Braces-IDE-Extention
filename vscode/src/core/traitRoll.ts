import { mix, R, toUnitInterval, u64 } from './hash';
import { TraitIds } from './traitIds';
import { TraitLayer } from './traitLayer';
import { TraitTable } from './traitTable';

/** What a single brace turned out to be. */
export interface BraceTraits {
    body: string | null;
    creature: string | null;
    costume: string | null;
    motion: string | null;
    effects: readonly string[];

    /**
     * True when anything at all was rolled, so the brace has to be drawn rather than merely
     * coloured.
     */
    isDrawn: boolean;
}

// One salt per layer. Without these the layers would correlate — every brace with a wizard
// hat would also have the same motion, because both would read the same bits.
const BODY_SALT = u64('0xB0D1E5A17C0FFEE');
const CREATURE_SALT = u64('0xC8EA7C0DE1DEA5');
const COSTUME_SALT = u64('0xC05715E5A1701');
const MOTION_SALT = u64('0x0713C0DE5EED17');
const EFFECT_SALT = u64('0xEFEC7B0DE5A1ED');
const GOLDEN = u64('0x9E3779B97F4A7C15');

const NO_EFFECTS: readonly string[] = [];

/** The traits every brace has when nothing at all is switched on. */
export const PLAIN_TRAITS: BraceTraits = {
    body: null,
    creature: null,
    costume: null,
    motion: null,
    effects: NO_EFFECTS,
    isDrawn: false,
};

function roll100(hi: number, lo: number, saltHi: number, saltLo: number): number {
    mix(hi, lo, saltHi, saltLo);
    return toUnitInterval(R.hi, R.lo) * 100.0;
}

/**
 * Turns a brace's identity hash into its traits.
 *
 * Deliberately free of any editor API: the renderer needs to know both whether a brace is
 * drawn (so it can hide the real one) and what to draw, and those two must never disagree.
 * Keeping the roll pure means both call the same function and get the same answer.
 *
 * @param isDistressed Set by the complexity warning, which replaces this one trait's roll
 * with a predicate on nesting depth. It is forced on regardless of its weight: a warning
 * that only fires on four braces in a hundred is not a warning.
 */
export function rollTraits(
    hi: number,
    lo: number,
    table: TraitTable,
    isUnmatched: boolean,
    isDistressed: boolean,
): BraceTraits {
    let body = table.pickOne(TraitLayer.Body, roll100(hi, lo, BODY_SALT.hi, BODY_SALT.lo));
    const creature = table.pickOne(TraitLayer.Creature, roll100(hi, lo, CREATURE_SALT.hi, CREATURE_SALT.lo));
    const costume = table.pickOne(TraitLayer.Costume, roll100(hi, lo, COSTUME_SALT.hi, COSTUME_SALT.lo));
    const motion = table.pickOne(TraitLayer.Motion, roll100(hi, lo, MOTION_SALT.hi, MOTION_SALT.lo));
    let effects = pickEffects(hi, lo, table);

    // A brace with no partner overrides whatever body it rolled: it genuinely does not know
    // what it is, and it doubles as a syntax hint.
    if (isUnmatched) {
        body = TraitIds.Question;
    }

    if (isDistressed) {
        effects = withDistress(effects);
    }

    return {
        body,
        creature,
        costume,
        motion,
        effects,
        isDrawn:
            body !== null ||
            creature !== null ||
            costume !== null ||
            motion !== null ||
            effects.length > 0,
    };
}

/**
 * Rolls every effect independently, so a brace can be on fire and tilted and carry a
 * shadow all at once.
 */
function pickEffects(hi: number, lo: number, table: TraitTable): readonly string[] {
    const count = table.effectCount;
    if (count === 0) {
        return NO_EFFECTS;
    }

    let chosen: string[] | null = null;
    let streamHi = EFFECT_SALT.hi;
    let streamLo = EFFECT_SALT.lo;

    for (let i = 0; i < count; i++) {
        // Advance the stream per effect so each gets its own independent roll.
        mix(streamHi, streamLo, GOLDEN.hi, GOLDEN.lo);
        streamHi = R.hi;
        streamLo = R.lo;

        if (roll100(hi, lo, streamHi, streamLo) < table.effectChance(i)) {
            (chosen ??= []).push(table.effectAt(i));
        }
    }

    return chosen ?? NO_EFFECTS;
}

/** Appends the distress effect, leaving an already-rolled one alone. */
function withDistress(effects: readonly string[]): readonly string[] {
    return effects.includes(TraitIds.Distressed) ? effects : [...effects, TraitIds.Distressed];
}
