import { u64 } from '../core/hash';
import { TraitIds } from '../core/traitIds';
import { Painter } from './creatures';
import { SvgContext } from './svgContext';

/**
 * Effects that add something to the glyph.
 *
 * The other half of the effect layer — the ones that lean, dim, blur or recolour the glyph
 * rather than drawing beside it — are not here. Those are CSS on the brace itself and live
 * in {@link module:render/braceStyle}, because a transform applied to the overlay would move
 * the ears and leave the brace behind.
 */

const SWEAT_BEAD = '#8FD4F2';
const FLAME_TIP = '#F2C14E';
const FLAME_MID = '#F08B33';
const FLAME_CORE = '#E8542B';
const CELEBRATION_GREEN = '#4CC25E';

const MITOSIS_SALT = u64('0x5D1177E');

/**
 * What a build did recently, for {@link TraitIds.BuildReactive}.
 *
 * Set from the task-process events in the extension host. Null outside the reaction window,
 * which is what keeps this from being a permanent change of costume — a brace scrolled into
 * view after the window has passed simply draws nothing.
 */
export type BuildReaction = 'succeeded' | 'failed' | null;

let buildReaction: BuildReaction = null;

export function setBuildReaction(reaction: BuildReaction): void {
    buildReaction = reaction;
}

export function currentBuildReaction(): BuildReaction {
    return buildReaction;
}

export function registerEffects(map: Map<string, Painter>): void {
    map.set(TraitIds.Fire, fire);
    map.set(TraitIds.Underline, underline);
    map.set(TraitIds.Distressed, distressed);
    map.set(TraitIds.Seasonal, seasonal);
    map.set(TraitIds.Mitosis, mitosis);
    map.set(TraitIds.BuildReactive, buildReactive);
}

/**
 * Flames licking up from the glyph, three tongues at different rates.
 *
 * Layered darkest-to-brightest so the core reads even when the whole thing is only a few
 * pixels across, and each tongue gets its own period so they never pulse in lockstep —
 * synchronised flames read as a flashing light rather than as fire.
 */
function fire(c: SvgContext): void {
    const xs = [-0.26, 0.02, 0.28];
    const colors = [FLAME_TIP, FLAME_MID, FLAME_CORE];
    const heights = [0.62, 0.86, 0.54];

    for (let i = 0; i < xs.length; i++) {
        const scale = 0.62 + c.swing(0.84 + i * 0.34) * 0.53;
        c.triangle(colors[i], xs[i] - 0.17, 0.1, xs[i], 0.1 - heights[i] * scale, xs[i] + 0.17, 0.1);
    }
}

function underline(c: SvgContext): void {
    c.stroke(
        '#D13B3B',
        0.08,
        c.p(-0.4, 1.12),
        c.p(-0.2, 1.04),
        c.p(0.0, 1.12),
        c.p(0.2, 1.04),
        c.p(0.4, 1.12),
    );
}

/**
 * Sweating and unsteady: two beads running off the glyph. The shake that goes with them is
 * applied to the brace, not here.
 *
 * The only trait that can arrive by predicate as well as by roll — the complexity warning
 * forces it onto anything nested past its threshold — so it has to read as *distress*
 * rather than as one more costume. Nothing here changes the silhouette or reaches above the
 * glyph, which is what keeps it distinguishable from a creature at six pixels across.
 *
 * The beads run downward and fade rather than pulsing in place. At this size a shape that
 * grows and shrinks reads as a blinking indicator light, not as sweat.
 */
function distressed(c: SvgContext): void {
    bead(c, 0.52, -0.04, 0.14, 0.4, 0.62);
    bead(c, -0.5, 0.12, 0.11, 0.3, 0.83);
}

function bead(c: SvgContext, x: number, y: number, radius: number, distance: number, seconds: number): void {
    // With motion off the phase is zero, which leaves both beads sitting where they were
    // drawn at full opacity. That is the intended still frame: the warning still reads
    // without anything moving.
    const t = c.phase(seconds);
    c.group(`opacity="${(0.95 * (1 - t)).toFixed(2)}"`);
    c.dot(SWEAT_BEAD, x, y + distance * t, radius);
    c.endGroup();
}

/** A seasonal accent: a pumpkin dot, a snowflake, a heart. */
function seasonal(c: SvgContext): void {
    const month = new Date().getMonth() + 1;
    const accent =
        month === 10 ? '#F27A1A' : month === 12 || month === 1 ? '#CFE8F7' : month === 2 ? '#E83B6B' : '#6BC45A';

    c.dot(accent, 0.42, -0.16, 0.13);
}

/**
 * Occasionally buds off a second brace, which drifts away and dissolves.
 *
 * Rare on purpose. The whole cycle is half a minute and the twin is invisible for nine
 * tenths of it: mitosis that happened every second would be a brace with two heads, not a
 * brace that occasionally divides. The phase is drawn from the identity so a screenful of
 * them do not all divide in unison, which would read as a page transition rather than as
 * cell division.
 */
function mitosis(c: SvgContext): void {
    const t = c.phase(29, MITOSIS_SALT);
    if (t < 0.86) {
        return;
    }

    const progress = (t - 0.86) / 0.14;
    const eased = 1 - (1 - progress) * (1 - progress);
    const angle = c.roll(MITOSIS_SALT) * Math.PI * 2;
    const reach = 0.85 * eased;
    const opacity = progress < 0.3 ? progress / 0.3 : (1 - progress) / 0.7;

    c.glyph(c.color, Math.cos(angle) * reach, Math.sin(angle) * reach, Math.max(0, opacity) * 0.9);
}

/**
 * Celebrates a green build and sulks at a red one, for a short while afterwards.
 *
 * Deliberately small. A build failing is already loud — the problems panel, the squiggles,
 * the terminal — and a brace that reacted at the same volume would be one more thing to
 * read at the worst moment. The sulk itself is a lean and a grey, applied to the brace in
 * {@link module:render/braceStyle}; only the sparks are drawn here.
 */
function buildReactive(c: SvgContext): void {
    if (buildReaction !== 'succeeded') {
        return;
    }

    const xs = [-0.34, 0.06, 0.4];
    for (let i = 0; i < xs.length; i++) {
        const t = c.phase(1.1 + i * 0.13);
        const eased = 1 - (1 - t) * (1 - t);
        c.group(`opacity="${(1 - t).toFixed(2)}"`);
        c.dot(CELEBRATION_GREEN, xs[i], -0.1 - 0.75 * eased, 0.11);
        c.endGroup();
    }
}
