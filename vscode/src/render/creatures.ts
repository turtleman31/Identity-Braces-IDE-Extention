import { u64 } from '../core/hash';
import { TraitIds } from '../core/traitIds';
import { SvgContext } from './svgContext';

/**
 * Silhouettes built around the glyph.
 *
 * Every shape is authored in ink units — 0 is the ink's top-centre, 1 unit is its height —
 * so a creature drawn once holds its proportions at any font size or zoom. The coordinates
 * are the Visual Studio extension's, unchanged: they were tuned against real glyph ink and
 * there is nothing editor-specific about them.
 *
 * Nothing here is finer than about 0.15 units. At 10pt one unit is 12&nbsp;px, so that is
 * the 2&nbsp;px floor below which detail averages into its neighbours and turns to mush; it
 * is the constraint that killed the first attempt at the cat.
 */

const INNER_EAR = '#F7AED0';
const PALE = '#EFE9F6';
const DARK = '#23222C';

const SLIME_SALT = u64('0x5117E');
const CTHULHU_SALT = u64('0xC7147E');

export type Painter = (c: SvgContext) => void;

export function registerCreatures(map: Map<string, Painter>): void {
    map.set(TraitIds.Catgirl, cat);
    map.set(TraitIds.Bunny, bunny);
    map.set(TraitIds.Devil, devil);
    map.set(TraitIds.Angel, angel);
    map.set(TraitIds.Fox, fox);
    map.set(TraitIds.Wolf, wolf);
    map.set(TraitIds.Frog, frog);
    map.set(TraitIds.Owl, owl);
    map.set(TraitIds.Mushroom, mushroom);
    map.set(TraitIds.Robot, robot);
    map.set(TraitIds.Bee, bee);
    map.set(TraitIds.Unicorn, unicorn);
    map.set(TraitIds.Vampire, vampire);
    map.set(TraitIds.Crab, crab);
    map.set(TraitIds.Bat, bat);
    map.set(TraitIds.Penguin, penguin);
    map.set(TraitIds.Cactus, cactus);
    map.set(TraitIds.Slime, slime);
    map.set(TraitIds.Snake, snake);
    map.set(TraitIds.Ghost, ghost);
    map.set(TraitIds.Wizard, wizard);
    map.set(TraitIds.Dragon, dragon);
    map.set(TraitIds.Spider, spider);
    map.set(TraitIds.Cthulhu, cthulhu);
    map.set(TraitIds.Pirate, pirate);
}

/** Two ears with a slanted base, so they read as ears rather than horns. */
function cat(c: SvgContext): void {
    c.triangle(c.color, -0.44, 0.01, -0.31, -0.83, -0.06, -0.16);
    c.triangle(INNER_EAR, -0.33, -0.11, -0.26, -0.56, -0.13, -0.21);
    c.triangle(c.color, 0.44, 0.01, 0.31, -0.83, 0.06, -0.16);
    c.triangle(INNER_EAR, 0.33, -0.11, 0.26, -0.56, 0.13, -0.21);
}

/**
 * A tail curling off the bottom right.
 *
 * Kept out of {@link cat} because it is a setting of its own: it overhangs the next cell
 * slightly, and it is also the single thing that makes the glyph read as a creature rather
 * than as a brace wearing something.
 *
 * It starts inside the glyph's ink rather than beside it. Anchored any further out it stops
 * reading as a tail and starts reading as a stray mark next to the brace, which is the one
 * thing a decoration on punctuation cannot afford to look like.
 */
export function catTail(c: SvgContext): void {
    c.curve(c.color, 0.12, c.p(0.16, 0.94), c.p(0.52, 1.02), c.p(0.58, 0.68), c.p(0.42, 0.58));
}

/** Tall and narrow, with the right ear folded at the tip. */
function bunny(c: SvgContext): void {
    c.triangle(c.color, -0.3, 0.02, -0.24, -1.15, -0.05, 0.0);
    c.triangle(INNER_EAR, -0.24, -0.1, -0.21, -0.85, -0.12, -0.08);
    c.triangle(c.color, 0.3, 0.02, 0.2, -0.95, 0.05, 0.0);
    c.stroke(c.color, 0.13, c.p(0.2, -0.95), c.p(0.36, -1.08));
}

function devil(c: SvgContext): void {
    c.curve(c.color, 0.14, c.p(-0.34, 0.0), c.p(-0.4, -0.45), c.p(-0.3, -0.62), c.p(-0.16, -0.66));
    c.curve(c.color, 0.14, c.p(0.34, 0.0), c.p(0.4, -0.45), c.p(0.3, -0.62), c.p(0.16, -0.66));
    c.curve(c.color, 0.13, c.p(0.3, 0.98), c.p(0.62, 1.02), c.p(0.66, 0.72), c.p(0.5, 0.6));
    c.triangle(c.color, 0.4, 0.62, 0.62, 0.58, 0.48, 0.44);
}

function angel(c: SvgContext): void {
    c.ring('#F5D96B', 0.0, -0.62, 0.3, 0.11);
}

function fox(c: SvgContext): void {
    c.triangle(c.color, -0.42, 0.0, -0.36, -0.92, -0.04, -0.14);
    c.triangle(PALE, -0.32, -0.1, -0.29, -0.62, -0.14, -0.18);
    c.triangle(c.color, 0.42, 0.0, 0.36, -0.92, 0.04, -0.14);
    c.triangle(PALE, 0.32, -0.1, 0.29, -0.62, 0.14, -0.18);
    c.curve(c.color, 0.2, c.p(0.26, 1.0), c.p(0.7, 1.02), c.p(0.72, 0.62), c.p(0.46, 0.5));
}

function wolf(c: SvgContext): void {
    c.triangle(c.color, -0.46, 0.02, -0.44, -0.72, -0.1, -0.12);
    c.triangle(c.color, 0.46, 0.02, 0.44, -0.72, 0.1, -0.12);
    c.dot(c.color, 0.0, -0.2, 0.1);
}

function frog(c: SvgContext): void {
    c.dot(c.color, -0.24, -0.2, 0.2);
    c.dot(c.color, 0.24, -0.2, 0.2);
    c.dot(DARK, -0.24, -0.2, 0.08);
    c.dot(DARK, 0.24, -0.2, 0.08);
}

function owl(c: SvgContext): void {
    c.dot(PALE, -0.22, 0.28, 0.24);
    c.dot(PALE, 0.22, 0.28, 0.24);
    c.dot(DARK, -0.22, 0.28, 0.11);
    c.dot(DARK, 0.22, 0.28, 0.11);
    c.triangle('#E8A53C', -0.08, 0.44, 0.08, 0.44, 0.0, 0.62);
}

function mushroom(c: SvgContext): void {
    c.box('#D13B3B', -0.42, -0.44, 0.84, 0.34, 0.17);
    c.dot(PALE, -0.18, -0.3, 0.09);
    c.dot(PALE, 0.16, -0.24, 0.07);
}

function robot(c: SvgContext): void {
    c.stroke(c.color, 0.11, c.p(0.0, -0.1), c.p(0.0, -0.6));

    // A hard on/off rather than a fade: a bulb that dims smoothly reads as a glow, and at
    // this size a glow is just a smudge.
    const lit = c.phase(1.4) < 0.5;
    c.group(`opacity="${lit ? 1 : 0.2}"`);
    c.dot('#E03B3B', 0.0, -0.7, 0.14);
    c.endGroup();

    c.box(c.color, -0.5, 0.3, 0.14, 0.14);
    c.box(c.color, 0.36, 0.3, 0.14, 0.14);
}

function bee(c: SvgContext): void {
    c.box('#E8C030', -0.36, 0.34, 0.72, 0.14);
    c.box('#E8C030', -0.36, 0.62, 0.72, 0.14);
    c.dot(PALE, -0.46, 0.16, 0.16);
    c.dot(PALE, 0.46, 0.16, 0.16);
}

function unicorn(c: SvgContext): void {
    c.triangle('#F5D96B', -0.13, -0.02, 0.0, -0.92, 0.13, -0.02);
    c.stroke(INNER_EAR, 0.09, c.p(-0.07, -0.28), c.p(0.07, -0.4));
    c.stroke(INNER_EAR, 0.09, c.p(-0.04, -0.52), c.p(0.06, -0.62));
}

function vampire(c: SvgContext): void {
    c.triangle(PALE, -0.24, 0.86, -0.1, 0.86, -0.17, 1.14);
    c.triangle(PALE, 0.1, 0.86, 0.24, 0.86, 0.17, 1.14);
}

function crab(c: SvgContext): void {
    c.stroke(c.color, 0.14, c.p(-0.36, 0.5), c.p(-0.62, 0.36));
    c.triangle(c.color, -0.62, 0.44, -0.86, 0.26, -0.6, 0.22);
    c.stroke(c.color, 0.14, c.p(0.36, 0.5), c.p(0.62, 0.36));
    c.triangle(c.color, 0.62, 0.44, 0.86, 0.26, 0.6, 0.22);
}

function bat(c: SvgContext): void {
    c.triangle(c.color, -0.3, 0.2, -0.86, 0.06, -0.62, 0.52);
    c.triangle(c.color, 0.3, 0.2, 0.86, 0.06, 0.62, 0.52);
}

function penguin(c: SvgContext): void {
    c.triangle('#E8A53C', -0.1, 0.26, 0.1, 0.26, 0.0, 0.46);
    c.triangle(c.color, -0.34, 0.52, -0.6, 0.74, -0.32, 0.8);
    c.triangle(c.color, 0.34, 0.52, 0.6, 0.74, 0.32, 0.8);
}

function cactus(c: SvgContext): void {
    for (let i = 0; i < 3; i++) {
        const y = 0.16 + i * 0.28;
        c.stroke(c.color, 0.09, c.p(-0.34, y), c.p(-0.52, y - 0.08));
        c.stroke(c.color, 0.09, c.p(0.34, y), c.p(0.52, y - 0.08));
    }

    c.dot('#E85C9A', 0.0, -0.16, 0.13);
}

/** A drip that forms, falls and resets. */
function slime(c: SvgContext): void {
    const t = c.phase(2.2, SLIME_SALT);
    c.group(`opacity="${(1 - t).toFixed(2)}"`);
    c.dot(c.color, 0.1, 1.02 + t * 0.9, 0.13);
    c.endGroup();
}

function snake(c: SvgContext): void {
    const flick = 1 - c.swing(1.8) * 0.9;
    c.group(`opacity="${flick.toFixed(2)}"`);
    c.stroke('#E03B6B', 0.09, c.p(0.3, 0.44), c.p(0.62, 0.44));
    c.endGroup();
    c.triangle('#E03B6B', 0.62, 0.38, 0.78, 0.32, 0.62, 0.5);
}

function ghost(c: SvgContext): void {
    c.dot(c.color, -0.2, 1.02, 0.1);
    c.dot(c.color, 0.06, 1.06, 0.1);
    c.dot(c.color, 0.3, 1.02, 0.1);
}

function wizard(c: SvgContext): void {
    c.triangle('#5A3FA8', -0.44, -0.14, 0.0, -1.24, 0.44, -0.14);
    c.box('#5A3FA8', -0.56, -0.2, 1.12, 0.13, 0.06);
    c.dot('#F5D96B', 0.06, -0.72, 0.1);
}

function dragon(c: SvgContext): void {
    c.curve(c.color, 0.13, c.p(-0.34, 0.0), c.p(-0.52, -0.34), c.p(-0.36, -0.62), c.p(-0.1, -0.58));
    c.curve(c.color, 0.13, c.p(0.34, 0.0), c.p(0.52, -0.34), c.p(0.36, -0.62), c.p(0.1, -0.58));
    c.triangle(c.color, 0.3, 0.3, 0.8, 0.12, 0.66, 0.62);
}

function spider(c: SvgContext): void {
    for (let i = 0; i < 3; i++) {
        const y = 0.24 + i * 0.26;
        c.stroke(c.color, 0.08, c.p(-0.3, y), c.p(-0.72, y - 0.16));
        c.stroke(c.color, 0.08, c.p(0.3, y), c.p(0.72, y - 0.16));
    }
}

/** Four tentacles, each on its own period so they never wave in lockstep. */
function cthulhu(c: SvgContext): void {
    for (let i = 0; i < 4; i++) {
        const x = -0.3 + i * 0.2;
        const sway = (c.swing(1.8 + i * 0.2, CTHULHU_SALT) - 0.5) * 0.12;
        c.curve(
            c.color,
            0.1,
            c.p(x, 0.9),
            c.p(x - 0.06 + sway, 1.1),
            c.p(x + 0.08 + sway * 2, 1.2),
            c.p(x + sway * 3, 1.34),
        );
    }
}

function pirate(c: SvgContext): void {
    c.box(DARK, -0.46, 0.18, 0.92, 0.15, 0.04);
    c.stroke(DARK, 0.07, c.p(-0.46, 0.18), c.p(-0.62, 0.06));
    c.dot('#D13B3B', 0.0, -0.1, 0.12);
}

/** Creatures that make the glyph itself translucent rather than adding to it. */
export const CREATURE_GLYPH_OPACITY: Readonly<Record<string, number>> = {
    [TraitIds.Ghost]: 0.55,
};
