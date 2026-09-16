import { BraceInfo } from '../core/braceInfo';
import { ringColor, RING_COUNT } from '../core/colorRing';
import { roll, u64 } from '../core/hash';
import { darken, withAlpha } from '../core/palette';
import { TraitIds } from '../core/traitIds';
import { Settings } from '../settings';
import { catTail, CREATURE_GLYPH_OPACITY } from './creatures';
import { currentBuildReaction } from './effects';
import { INK, PHASE_STEPS, SvgContext } from './svgContext';
import { paint, resolveBody } from './traitDrawing';

/**
 * Turns a scanned brace into the CSS and SVG that draw it.
 *
 * The Visual Studio extension has a WPF canvas per brace and can animate a brush in place.
 * A VS Code decoration is a stylesheet rule and a list of ranges, so the shape of the
 * problem is different: every distinct *appearance* is a rule, and animation is moving
 * ranges between rules. That is why everything here is quantised — the phase, the colour
 * ring, the transforms — and why {@link BraceStyle.key} exists. A brace that looks the same
 * as another brace shares its rule, and a brace that looks the same as it did two frames ago
 * costs nothing at all.
 */

/** The complete appearance of one brace. */
export interface BraceStyle {
    /** Two braces with the same key are rendered by the same decoration type. */
    key: string;

    /** True when this appearance depends on the clock and has to be recomputed per frame. */
    animated: boolean;

    /** Colour for the character in the buffer, or null when it is hidden and redrawn. */
    color: string | null;

    /** CSS for the range's own span: transforms and opacity, which must move everything. */
    spanCss: string;

    /** A redrawn glyph, when the real one is hidden. */
    glyph: GlyphLayer | null;

    /** The drawn overlay, as an SVG document. */
    overlay: string | null;

    /** The brace's own identity colour, before dimming — for the hover and the guides. */
    baseColor: string;
}

export interface GlyphLayer {
    text: string;
    color: string;
    css: string;
}

/** What the world looks like at the moment of drawing. */
export interface RenderContext {
    settings: Settings;

    /** Quantised frame time in milliseconds. */
    timeMs: number;

    /** Minutes since the window opened, for the traits that wear down over a session. */
    sessionMinutes: number;

    /** Where the caret is, for the two traits that react to it. -1 when there is none. */
    caretLine: number;
    caretColumn: number;

    /** This brace's own line and column, so the caret traits can compare. */
    line: number;
    column: number;

    /** Dimmed by the scope spotlight. */
    dim: boolean;
}

const STOCKING_BODY = '#7B7490';
const STOCKING_WELT = '#C6BFD4';
const SULK_GREY = '#8A8A92';

const TILT_SALT = u64('0x7117ED');
const DRUNK_SALT = u64('0xD204070');
const DRIFT_SALT = u64('0x0D21F7');
const WAVE_SALT = u64('0x7AFE12');
const CYCLE_SALT = u64('0xC17C1E');
const EMPHASIS_SALT = u64('0xB01D17');

export function computeStyle(brace: BraceInfo, context: RenderContext): BraceStyle {
    const settings = context.settings;
    const traits = brace.traits;
    const baseColor = colorFor(brace, settings);

    // The overwhelmingly common case, and the one that has to stay cheap: a plain brace is
    // one colour and nothing else. No pseudo-elements, no SVG, no key beyond the colour.
    if (!traits.isDrawn || settings.renderMode === 'plain') {
        const color = context.dim ? withAlpha(baseColor, settings.spotlightDim) : baseColor;
        return {
            key: `p:${color}`,
            animated: false,
            color,
            spanCss: '',
            glyph: null,
            overlay: null,
            baseColor,
        };
    }

    const parts: string[] = [];
    const transforms: string[] = [];
    const glyphCss: string[] = [];
    let animated = false;
    let color = baseColor;
    let opacity = 1;
    let transformOriginY = 75;

    // ---- motion that recolours ----

    if (traits.motion === TraitIds.ColourCycle && settings.enableMotion) {
        const offset = roll(brace.idHi, brace.idLo, CYCLE_SALT);
        const turns = context.timeMs / (settings.colorCycleSeconds * 1000) + offset;
        color = ringColor(Math.floor((turns % 1) * RING_COUNT));
        animated = true;
    }

    // ---- effects that recolour or wear the glyph down ----

    if (has(traits.effects, TraitIds.Nocturnal)) {
        const tired = Math.min(1, context.sessionMinutes / 20);
        opacity *= 1 - tired * 0.45;
        transforms.push(`rotate(${(tired * 10).toFixed(1)}deg)`);
    }

    if (has(traits.effects, TraitIds.BuildReactive) && currentBuildReaction() === 'failed') {
        color = SULK_GREY;
        transforms.push('rotate(9deg)', `translateY(${(0.1 * INK.heightEm).toFixed(3)}em)`);
    }

    if (traits.creature !== null && CREATURE_GLYPH_OPACITY[traits.creature] !== undefined) {
        opacity *= CREATURE_GLYPH_OPACITY[traits.creature];
    }

    // ---- motion that moves ----

    if (settings.enableMotion) {
        animated = applyMotion(traits.motion, brace, context, transforms) || animated;
        if (traits.motion === TraitIds.Spin || traits.motion === TraitIds.Flip) {
            transformOriginY = 50;
        }

        const fade = motionOpacity(traits.motion, context.timeMs);
        if (fade < 1) {
            opacity *= fade;
            animated = true;
        } else if (traits.motion === TraitIds.Blink || traits.motion === TraitIds.Flicker) {
            // Full brightness is a frame of the animation like any other, and the appearance
            // still has to be recomputed on the next one.
            animated = true;
        }
    }

    // ---- effects that lean or shake ----

    if (has(traits.effects, TraitIds.Tilted)) {
        transforms.push(`rotate(${(-16 + roll(brace.idHi, brace.idLo, TILT_SALT) * 32).toFixed(1)}deg)`);
    }

    if (has(traits.effects, TraitIds.Drunk)) {
        const lean = Math.min(24, context.sessionMinutes * 1.2);
        const direction = roll(brace.idHi, brace.idLo, DRUNK_SALT) < 0.5 ? -1 : 1;
        transforms.push(`rotate(${(lean * direction).toFixed(1)}deg)`);
    }

    if (has(traits.effects, TraitIds.Gravity)) {
        const sag = Math.min(0.3, context.sessionMinutes * 0.05);
        transforms.push(`translateY(${(sag * INK.heightEm).toFixed(3)}em)`);
    }

    if (has(traits.effects, TraitIds.Distressed) && settings.enableMotion) {
        // Small and fast. A wide, slow wobble reads as a personality; a nervous vibration
        // reads as a brace that is not coping, which is the point.
        const step = Math.floor(context.timeMs / 110) % 2 === 0 ? -4.5 : 4.5;
        transforms.push(`rotate(${step}deg)`);
        animated = true;
    }

    // ---- the two traits that watch the caret ----

    if (has(traits.effects, TraitIds.StageFright) && context.caretLine === context.line) {
        // Almost out, not out: a brace that is genuinely invisible is the one failure this
        // extension treats as unacceptable.
        opacity *= 0.18;
    }

    if (has(traits.effects, TraitIds.FleeCursor) && context.caretLine === context.line) {
        const distance = context.column - context.caretColumn;
        if (Math.abs(distance) <= 6) {
            const push = (distance >= 0 ? 1 : -1) * (1 - Math.abs(distance) / 6) * 0.55;
            transforms.push(`translateX(${push.toFixed(2)}ch)`);
        }
    }

    // ---- the glyph itself ----

    const bodyText = resolveBody(traits.body, brace.character);
    const bodyTransform = bodyTransformFor(traits.body);
    if (bodyTransform) {
        glyphCss.push(bodyTransform);
    }

    if (traits.body === TraitIds.ForeignFont) {
        glyphCss.push("font-family: 'Comic Sans MS', 'Comic Neue', cursive");
    }

    if (has(traits.effects, TraitIds.BoldItalic)) {
        const emphasis = roll(brace.idHi, brace.idLo, EMPHASIS_SALT);
        glyphCss.push(emphasis < 0.5 ? 'font-weight: 700' : 'font-style: italic');
    }

    if (has(traits.effects, TraitIds.Shadow)) {
        glyphCss.push('text-shadow: 0.07em 0.07em 0 rgba(0,0,0,0.45)');
    }

    // A recolouring of the brace's own stroke, never a shape drawn behind it. The band is
    // clipped to the glyph, so a stocking is exactly as wide as the stroke it clothes at
    // every font size — which is the whole reason it reads as clothing.
    const band = glyphBand(traits, color, settings);
    if (band) {
        glyphCss.push(band);
    }

    // ---- the overlay ----

    let overlay: string | null = null;
    if (settings.renderMode === 'full') {
        const svg = new SvgContext(
            color,
            brace.character,
            brace.idHi,
            brace.idLo,
            INK.heightEm * settings.fontSizePx * settings.decorScale,
            context.timeMs,
            settings.enableMotion,
            settings.fontFamily,
        );

        paint(traits.creature, svg);
        if (traits.creature === TraitIds.Catgirl && settings.tail) {
            catTail(svg);
        }

        paint(traits.costume, svg);
        paint(traits.motion, svg);
        for (const effect of traits.effects) {
            paint(effect, svg);
        }

        if (!svg.isEmpty) {
            overlay = svg.toSvg();
        }

        animated = animated || svg.animated;
    }

    // ---- assembly ----

    if (context.dim) {
        opacity *= settings.spotlightDim;
    }

    if (transforms.length > 0) {
        parts.push(
            'display: inline-block',
            `transform-origin: 50% ${transformOriginY}%`,
            `transform: ${transforms.join(' ')}`,
        );
    }

    if (opacity < 0.999) {
        parts.push(`opacity: ${opacity.toFixed(3)}`);
    }

    const spanCss = parts.join('; ');
    const glyph: GlyphLayer = { text: bodyText, color, css: glyphCss.join('; ') };

    return {
        key: `d:${color}|${spanCss}|${glyph.text}|${glyph.css}|${overlay ?? ''}`,
        animated,
        // The real character is always hidden once anything is drawn, and redrawn in the
        // pseudo-element. One place for glyph styling beats two that have to agree.
        color: null,
        spanCss,
        glyph,
        overlay,
        baseColor,
    };
}

/** The brace's colour before any personality gets hold of it. */
export function colorFor(brace: BraceInfo, settings: Settings): string {
    if (settings.colorMode === 'monochrome') {
        return settings.monochromeColor || '#C8A2E8';
    }

    return settings.palette[brace.colorIndex % settings.palette.length];
}

function applyMotion(
    motion: string | null,
    brace: BraceInfo,
    context: RenderContext,
    transforms: string[],
): boolean {
    const t = (seconds: number, saltRoll = 0) =>
        quantise((context.timeMs / (seconds * 1000) + saltRoll) % 1);

    const unit = INK.heightEm;

    switch (motion) {
        case TraitIds.Wobble:
            transforms.push(`rotate(${(Math.sin(t(1.6) * Math.PI * 2) * 9).toFixed(1)}deg)`);
            return true;

        case TraitIds.Bounce: {
            // Up sharply, down slowly, which is what makes it read as a hop rather than as
            // a float.
            const p = t(1.4);
            const lift = p < 0.5 ? easeOut(p * 2) : 1 - easeOut((p - 0.5) * 2);
            transforms.push(`translateY(${(-0.28 * unit * lift).toFixed(3)}em)`);
            return true;
        }

        case TraitIds.Breathe: {
            const scale = 1 + Math.sin(t(2.6) * Math.PI * 2) * 0.06;
            transforms.push(`scale(${scale.toFixed(3)})`);
            return true;
        }

        case TraitIds.Heartbeat: {
            const p = t(1.4);
            const scale = p < 0.1 ? 1 + p * 1.8 : p < 0.2 ? 1.18 - (p - 0.1) * 1.8 : p < 0.3 ? 1 + (p - 0.2) * 1.4 : p < 0.42 ? 1.14 - (p - 0.3) * 1.167 : 1;
            transforms.push(`scale(${scale.toFixed(3)})`);
            return true;
        }

        case TraitIds.Shiver: {
            // Discrete steps, not a smooth path: sub-pixel jitter is the point.
            const step = Math.floor(context.timeMs / 80) % 3;
            const offset = step === 0 ? -0.6 : step === 1 ? 0.7 : -0.3;
            transforms.push(`translate(${offset}px, ${offset}px)`);
            return true;
        }

        case TraitIds.Spin:
            transforms.push(`rotate(${(t(4.5) * 360).toFixed(0)}deg)`);
            return true;

        case TraitIds.Flip:
            transforms.push(`rotate(${t(3.4) < 0.5 ? 0 : 180}deg)`);
            return true;

        case TraitIds.Glitch: {
            const p = t(2.0);
            const jump = p < 0.82 ? 0 : p < 0.86 ? 2 : p < 0.9 ? -1.5 : 0;
            transforms.push(`translateX(${jump}px)`);
            return true;
        }

        case TraitIds.Drift: {
            const period = 3 + roll(brace.idHi, brace.idLo, DRIFT_SALT) * 2;
            const x = Math.sin(t(period) * Math.PI * 2) * 1.6;
            const y = Math.cos(t(period * 1.37) * Math.PI * 2) * 1.2;
            transforms.push(`translate(${x.toFixed(1)}px, ${y.toFixed(1)}px)`);
            return true;
        }

        case TraitIds.Wave: {
            // The phase comes from the brace's own identity, so neighbours move in sequence
            // and the motion appears to travel along the line.
            const p = t(0.9, roll(brace.idHi, brace.idLo, WAVE_SALT));
            transforms.push(`translateY(${(-0.24 * unit * Math.abs(Math.sin(p * Math.PI))).toFixed(3)}em)`);
            return true;
        }

        default:
            return false;
    }
}

/**
 * Opacity-only motions, kept apart from the transforms because they multiply rather than
 * compose.
 */
export function motionOpacity(motion: string | null, timeMs: number): number {
    const t = (seconds: number) => quantise((timeMs / (seconds * 1000)) % 1);

    switch (motion) {
        case TraitIds.Blink:
            return t(3.1) < 0.88 ? 1 : t(3.1) < 0.94 ? 0.15 : 1;
        case TraitIds.Flicker: {
            const p = t(0.9);
            return p < 0.22 ? 1 - p * 1.27 : p < 0.44 ? 0.72 + (p - 0.22) * 1.05 : p < 0.68 ? 0.95 - (p - 0.44) * 1.25 : 0.65 + (p - 0.68) * 1.09;
        }
        default:
            return 1;
    }
}

/**
 * A hard-stopped gradient clipped to the glyph's own strokes.
 *
 * The stops are in ink units and converted here, once, using the same font model the
 * overlay uses — so a stocking and a pair of ears drawn on the same brace agree about where
 * the leg starts.
 */
function glyphBand(
    traits: { costume: string | null; effects: readonly string[] },
    color: string,
    settings: Settings,
): string | null {
    const stops = bandStops(traits, color, settings);
    if (!stops) {
        return null;
    }

    const inkTopEm = (settings.lineHeightEm - 1) / 2 + INK.topEm;
    const at = (units: number) =>
        `${(((inkTopEm + units * INK.heightEm) / settings.lineHeightEm) * 100).toFixed(1)}%`;

    const css = stops
        .map(([c, from, to]) => `${c} ${at(from)} ${at(to)}`)
        .join(', ');

    return (
        `background-image: linear-gradient(to bottom, ${color} 0 ${at(stops[0][1])}, ${css})` +
        '; -webkit-background-clip: text; background-clip: text; -webkit-text-fill-color: transparent'
    );
}

function bandStops(
    traits: { costume: string | null; effects: readonly string[] },
    color: string,
    settings: Settings,
): [string, number, number][] | null {
    if (traits.costume === TraitIds.ThighHighs && settings.thighHighStyle !== 'off') {
        // A stripe under two device pixels averages into its neighbours and reads as a
        // smear, so the welt has a floor expressed in ink units.
        const unitPx = INK.heightEm * settings.fontSizePx;
        const legTop = 0.55;
        const welt = Math.max(2 / Math.max(unitPx, 1), 0.17);

        switch (settings.thighHighStyle) {
            case 'twotone':
                return [[STOCKING_BODY, legTop, 1.6]];
            case 'banded': {
                const thin = Math.max(1.3 / Math.max(unitPx, 1), 0.11);
                return [
                    [STOCKING_WELT, legTop, legTop + thin],
                    [STOCKING_BODY, legTop + thin, legTop + thin * 2],
                    [STOCKING_WELT, legTop + thin * 2, legTop + thin * 3],
                    [STOCKING_BODY, legTop + thin * 3, 1.6],
                ];
            }
            default:
                return [
                    [STOCKING_WELT, legTop, legTop + welt],
                    [STOCKING_BODY, legTop + welt, 1.6],
                ];
        }
    }

    if (has(traits.effects, TraitIds.GradientFill)) {
        return [[darken(color, 0.45), 0.5, 1.6]];
    }

    return null;
}

function bodyTransformFor(body: string | null): string | null {
    switch (body) {
        case TraitIds.Mirrored:
            return 'display: inline-block; transform: scaleX(-1)';
        case TraitIds.UpsideDown:
            return 'display: inline-block; transform: rotate(180deg)';
        case TraitIds.Subscript:
            return 'display: inline-block; transform-origin: 50% 100%; transform: scale(0.6)';
        default:
            return null;
    }
}

function has(effects: readonly string[], id: string): boolean {
    return effects.includes(id);
}

function easeOut(t: number): number {
    return 1 - (1 - t) * (1 - t);
}

/** Rounds a phase to the shared step count, so the rules it generates repeat. */
function quantise(phase: number): number {
    return Math.floor(((phase % 1) + 1) % 1 * PHASE_STEPS) / PHASE_STEPS;
}
