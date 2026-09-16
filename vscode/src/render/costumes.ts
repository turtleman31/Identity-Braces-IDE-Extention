import { TraitIds } from '../core/traitIds';
import { Painter } from './creatures';
import { SvgContext } from './svgContext';

/**
 * Things worn over the glyph rather than replacing it.
 *
 * The thigh highs are the exception, and they are not here: they are a recolouring of the
 * brace's own stroke rather than a shape drawn near it, so they live in
 * {@link module:render/glyphPaint} where the glyph itself is styled. Drawing a rectangle
 * instead once meant an 8&nbsp;px slab behind a 6&nbsp;px stroke, which read as a bar with a
 * brace lost inside it — the same mistake is available here and worth not making twice.
 */

const DARK = '#23222C';
const PALE = '#EFE9F6';
const GOLD = '#F5D96B';

export function registerCostumes(map: Map<string, Painter>): void {
    map.set(TraitIds.TopHat, topHat);
    map.set(TraitIds.Crown, crown);
    map.set(TraitIds.Sunglasses, sunglasses);
    map.set(TraitIds.Scarf, scarf);
    map.set(TraitIds.Bowtie, bowtie);
    map.set(TraitIds.PartyHat, partyHat);
    map.set(TraitIds.Moustache, moustache);
    map.set(TraitIds.Beanie, beanie);
    map.set(TraitIds.FlowerCrown, flowerCrown);
    map.set(TraitIds.Headphones, headphones);
    map.set(TraitIds.Bandage, bandage);
    map.set(TraitIds.Necktie, necktie);
    map.set(TraitIds.Wings, wings);
    map.set(TraitIds.Armour, armour);
    map.set(TraitIds.Cape, cape);
    map.set(TraitIds.Backpack, backpack);
    map.set(TraitIds.Monocle, monocle);
}

function topHat(c: SvgContext): void {
    c.box(DARK, -0.56, -0.22, 1.12, 0.12, 0.05);
    c.box(DARK, -0.34, -0.78, 0.68, 0.58, 0.04);
    c.box('#D13B3B', -0.34, -0.36, 0.68, 0.12);
}

function crown(c: SvgContext): void {
    c.triangle(GOLD, -0.4, -0.06, -0.3, -0.52, -0.14, -0.06);
    c.triangle(GOLD, -0.14, -0.06, 0.0, -0.62, 0.14, -0.06);
    c.triangle(GOLD, 0.14, -0.06, 0.3, -0.52, 0.4, -0.06);
    c.box(GOLD, -0.42, -0.1, 0.84, 0.12);
}

function sunglasses(c: SvgContext): void {
    c.box(DARK, -0.5, 0.2, 0.4, 0.2, 0.05);
    c.box(DARK, 0.1, 0.2, 0.4, 0.2, 0.05);
    c.box(DARK, -0.12, 0.26, 0.24, 0.06);
}

function scarf(c: SvgContext): void {
    const wool = '#D13B5A';
    c.box(wool, -0.44, 0.46, 0.88, 0.16, 0.04);
    c.curve(wool, 0.14, c.p(0.36, 0.54), c.p(0.66, 0.66), c.p(0.62, 0.9), c.p(0.44, 1.0));
}

function bowtie(c: SvgContext): void {
    const silk = '#D13B3B';
    c.triangle(silk, -0.44, 0.34, -0.44, 0.7, -0.06, 0.52);
    c.triangle(silk, 0.44, 0.34, 0.44, 0.7, 0.06, 0.52);
    c.dot(silk, 0.0, 0.52, 0.09);
}

function partyHat(c: SvgContext): void {
    c.triangle('#E03B9A', -0.34, -0.1, 0.0, -0.94, 0.34, -0.1);
    c.box(GOLD, -0.24, -0.44, 0.48, 0.09);
    c.dot(PALE, 0.0, -0.98, 0.11);
}

function moustache(c: SvgContext): void {
    c.curve(DARK, 0.16, c.p(-0.44, 0.48), c.p(-0.22, 0.34), c.p(-0.06, 0.5), c.p(0.0, 0.52));
    c.curve(DARK, 0.16, c.p(0.44, 0.48), c.p(0.22, 0.34), c.p(0.06, 0.5), c.p(0.0, 0.52));
}

function beanie(c: SvgContext): void {
    const wool = '#3B7AD1';
    c.box(wool, -0.44, -0.58, 0.88, 0.44, 0.2);
    c.box(PALE, -0.48, -0.2, 0.96, 0.13, 0.05);
    c.dot(PALE, 0.0, -0.66, 0.13);
}

function flowerCrown(c: SvgContext): void {
    c.dot('#E85C9A', -0.28, -0.14, 0.13);
    c.dot(GOLD, 0.0, -0.22, 0.13);
    c.dot('#7BC4E8', 0.28, -0.14, 0.13);
}

function headphones(c: SvgContext): void {
    c.curve(DARK, 0.12, c.p(-0.48, 0.18), c.p(-0.44, -0.42), c.p(0.44, -0.42), c.p(0.48, 0.18));
    c.box(DARK, -0.58, 0.12, 0.2, 0.3, 0.07);
    c.box(DARK, 0.38, 0.12, 0.2, 0.3, 0.07);
}

function bandage(c: SvgContext): void {
    const tape = '#E8C9A0';
    c.stroke(tape, 0.2, c.p(-0.38, 0.24), c.p(0.38, 0.66));
    c.stroke(tape, 0.2, c.p(-0.38, 0.66), c.p(0.38, 0.24));
}

function necktie(c: SvgContext): void {
    const silk = '#2E5CA8';
    c.triangle(silk, -0.14, 0.36, 0.14, 0.36, 0.0, 0.52);
    c.triangle(silk, -0.13, 0.54, 0.13, 0.54, 0.0, 1.02);
}

function wings(c: SvgContext): void {
    c.triangle(PALE, -0.3, 0.24, -0.82, 0.02, -0.6, 0.56);
    c.triangle(PALE, 0.3, 0.24, 0.82, 0.02, 0.6, 0.56);
}

function armour(c: SvgContext): void {
    const steel = '#8E96A6';
    c.box(steel, -0.42, 0.4, 0.84, 0.26, 0.06);
    c.dot(PALE, 0.0, 0.53, 0.07);
}

function cape(c: SvgContext): void {
    c.triangle('#8A1F3C', -0.3, 0.16, 0.3, 0.16, 0.0, 1.2);
}

function backpack(c: SvgContext): void {
    const canvas = '#5A7A4A';
    c.box(canvas, 0.3, 0.3, 0.4, 0.5, 0.1);
    c.stroke(canvas, 0.08, c.p(0.3, 0.38), c.p(0.06, 0.44));
}

function monocle(c: SvgContext): void {
    c.ring(GOLD, 0.26, 0.3, 0.24, 0.09);
    c.stroke(GOLD, 0.07, c.p(0.3, 0.52), c.p(0.36, 0.94));
}
