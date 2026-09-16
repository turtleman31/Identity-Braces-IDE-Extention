import { roll as rollHash, U64 } from '../core/hash';

/** A point in ink units. */
export type Pt = readonly [number, number];

/**
 * How finely a cycle is chopped up. Twelve is enough that a sway looks continuous at
 * fifteen frames a second, and few enough that the twelve rules it generates are cached
 * within one cycle and never rebuilt.
 */
export const PHASE_STEPS = 12;

/**
 * How a brace's ink relates to the font, and how much of the surrounding box we draw into.
 *
 * The Visual Studio extension measures this from the real typeface through `FormattedText`.
 * There is no equivalent here — an extension host has no font metrics — so these are the
 * measured Consolas proportions, expressed relative to the em box and therefore correct for
 * any monospace face within a few percent. `identityBraces.overlayNudge` exists for the
 * faces where a few percent is not close enough.
 */
export const INK = {
    /** Height of a brace's ink, as a fraction of the font size. */
    heightEm: 0.9,

    /** How far the ink's top sits below the top of the inline box, as a fraction of font size. */
    topEm: 0.24,

    /** Left edge of the drawing region, in ink units from the ink's horizontal centre. */
    left: -1.3,

    /** Top edge of the drawing region, in ink units below the ink's top. */
    top: -1.5,

    width: 2.6,
    height: 3.1,
};

/**
 * Everything a trait's draw function needs, plus primitives to draw with.
 *
 * Coordinates are supplied in *ink units*: 0 is the horizontal centre of the glyph's ink and
 * the top of it vertically, and 1 unit is the ink's height. So a shape authored once holds
 * its proportions at any font, size or zoom — which is the only way eighty-six traits stay
 * maintainable, and it is why the geometry below could be lifted from the WPF original
 * almost verbatim.
 */
export class SvgContext {
    private readonly parts: string[] = [];

    private animatedFlag = false;

    /**
     * @param unitPx The ink height in device pixels. Only used for the minimum-thickness
     * floors, which exist because a stripe under two pixels averages into its neighbours and
     * reads as a smear rather than as detail.
     */
    constructor(
        readonly color: string,
        readonly character: string,
        readonly idHi: number,
        readonly idLo: number,
        readonly unitPx: number,
        readonly timeMs: number,
        readonly motionEnabled: boolean,
        readonly fontFamily: string,
    ) {}

    /**
     * Where this brace is in a cycle of `seconds`, as a value in [0, 1).
     *
     * Quantised, and that is the whole design. A decoration in VS Code is a stylesheet rule,
     * not a scene graph — there is nothing to animate in place, so a moving brace is a new
     * rule every frame. Rounding the phase to {@link PHASE_STEPS} means the rules repeat
     * instead of accumulating, and the cache converges on a small fixed set however long the
     * editor stays open.
     *
     * Offset per brace, so a screenful of the same trait does not pulse in unison — which
     * reads as the page flashing rather than as forty separate creatures.
     */
    phase(seconds: number, salt?: U64): number {
        this.animatedFlag = true;
        if (!this.motionEnabled) {
            return 0;
        }

        const offset = salt ? rollHash(this.idHi, this.idLo, salt) : 0;
        const raw = (this.timeMs / (seconds * 1000) + offset) % 1;
        return Math.floor(raw * PHASE_STEPS) / PHASE_STEPS;
    }

    /** A phase mapped onto a smooth there-and-back, for anything that eases rather than loops. */
    swing(seconds: number, salt?: U64): number {
        return (1 - Math.cos(this.phase(seconds, salt) * Math.PI * 2)) / 2;
    }

    /** True when anything drawn here asked for the time, and so has to be redrawn. */
    get animated(): boolean {
        return this.animatedFlag;
    }

    /** One device pixel, in ink units. */
    private get pixel(): number {
        return this.unitPx > 0 ? 1 / this.unitPx : 0.08;
    }

    /**
     * A point, for the primitives that take them.
     *
     * Coordinates go into the SVG in ink units unchanged: the viewBox is authored in the
     * same units, and `identityBraces.decorScale` scales the box the overlay is painted
     * into rather than the shapes inside it — so turning it up cannot push a wizard hat
     * outside the viewBox, and the anchor stays on the glyph's ink at any size.
     */
    p(x: number, y: number): Pt {
        return [x, y];
    }

    triangle(color: string, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number): void {
        const pts = `${n(x1)},${n(y1)} ${n(x2)},${n(y2)} ${n(x3)},${n(y3)}`;
        this.parts.push(`<polygon points="${pts}" fill="${esc(color)}"/>`);
    }

    dot(color: string, cx: number, cy: number, radius: number): void {
        this.parts.push(
            `<circle cx="${n(cx)}" cy="${n(cy)}" r="${n(radius)}" fill="${esc(color)}"/>`,
        );
    }

    /**
     * A flattened ring. The vertical squash is what makes a halo read as a halo seen at a
     * slight angle rather than as an O.
     */
    ring(color: string, cx: number, cy: number, radius: number, thickness: number): void {
        this.parts.push(
            `<ellipse cx="${n(cx)}" cy="${n(cy)}" rx="${n(radius)}" ry="${n(radius * 0.55)}"` +
                ` fill="none" stroke="${esc(color)}" stroke-width="${n(this.thick(thickness))}"/>`,
        );
    }

    box(color: string, x: number, y: number, w: number, h: number, radius = 0): void {
        const width = Math.max(this.pixel, w);
        const height = Math.max(this.pixel, h);
        this.parts.push(
            `<rect x="${n(x)}" y="${n(y)}" width="${n(width)}" height="${n(height)}"` +
                (radius > 0 ? ` rx="${n(radius)}" ry="${n(radius)}"` : '') +
                ` fill="${esc(color)}"/>`,
        );
    }

    stroke(color: string, thickness: number, ...points: Pt[]): void {
        if (points.length < 2) {
            return;
        }

        const d = points.map(([px, py]) => `${n(px)},${n(py)}`).join(' ');
        this.parts.push(
            `<polyline points="${d}" fill="none" stroke="${esc(color)}"` +
                ` stroke-width="${n(this.thick(thickness))}" stroke-linecap="round" stroke-linejoin="round"/>`,
        );
    }

    curve(color: string, thickness: number, start: Pt, c1: Pt, c2: Pt, end: Pt): void {
        const d =
            `M ${n(start[0])} ${n(start[1])} C ${n(c1[0])} ${n(c1[1])}, ` +
            `${n(c2[0])} ${n(c2[1])}, ${n(end[0])} ${n(end[1])}`;
        this.parts.push(
            `<path d="${d}" fill="none" stroke="${esc(color)}"` +
                ` stroke-width="${n(this.thick(thickness))}" stroke-linecap="round"/>`,
        );
    }

    /**
     * Another copy of the brace's own character, offset from where the real one sits.
     *
     * For traits that need a whole second brace rather than a decoration — `mitosis` buds
     * one off and lets it drift away. It is drawn in the editor's own font family so it
     * matches the original; what it cannot match is a body substitution, so a question mark
     * divides into a question mark and a brace. The alternative was not having mitosis.
     */
    glyph(color: string, dx: number, dy: number, opacity: number): void {
        if (opacity <= 0.01) {
            return;
        }

        // A brace's baseline sits about three quarters of the way down its ink; the font
        // size that produces one unit of ink is a little over one unit.
        const size = 1.111;
        const baseline = 0.833;
        this.parts.push(
            `<text x="${n(dx)}" y="${n(baseline + dy)}" font-size="${n(size)}"` +
                ` font-family="${esc(this.fontFamily)}" text-anchor="middle" fill="${esc(color)}"` +
                ` opacity="${opacity.toFixed(2)}">${esc(this.character)}</text>`,
        );
    }

    /** Opens a group every following shape is added to, until {@link endGroup}. */
    group(attributes: string): void {
        this.parts.push(`<g ${attributes}>`);
    }

    endGroup(): void {
        this.parts.push('</g>');
    }

    /**
     * A deterministic value in [0,1) from this brace's identity, for traits that want stable
     * variety — a lean angle, a colour pick, a phase offset.
     */
    roll(salt: U64): number {
        return rollHash(this.idHi, this.idLo, salt);
    }

    get isEmpty(): boolean {
        return this.parts.length === 0;
    }

    /** Stroke widths obey the same one-device-pixel floor the WPF original used. */
    private thick(thickness: number): number {
        return Math.max(this.pixel, thickness);
    }

    /** The finished overlay, as an SVG document sized in ink units. */
    toSvg(): string {
        return (
            `<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%"` +
            ` viewBox="${n(INK.left)} ${n(INK.top)} ${n(INK.width)} ${n(INK.height)}"` +
            ` preserveAspectRatio="xMidYMid meet" shape-rendering="geometricPrecision">` +
            this.parts.join('') +
            `</svg>`
        );
    }
}

/** Three decimal places is under a tenth of a pixel at any sane font size, and keeps the
 * data URI — which ends up inside a CSS rule — from tripling in length for nothing. */
function n(value: number): string {
    return Number.isInteger(value) ? String(value) : value.toFixed(3).replace(/0+$/, '').replace(/\.$/, '');
}

function esc(value: string): string {
    return value.replace(/[<>&"']/g, (c) => `&#${c.charCodeAt(0)};`);
}
