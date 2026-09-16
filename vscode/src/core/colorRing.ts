import { toHex } from './palette';

/**
 * A smooth hue wheel for animated braces, held at the same relative luminance as the static
 * palette (0.2072 — the point of equal contrast against dark and light editor backgrounds).
 *
 * The static palette cannot be reused for animation: its entries are spaced by the golden
 * angle precisely so neighbours look unrelated, which is the opposite of what a smooth
 * cycle needs. This ring is evenly spaced instead, so a brace sweeps the wheel without ever
 * dimming out against the background as it passes through yellow or blue.
 */
export const RING_COUNT = 36;

const TARGET_LUMINANCE = 0.2072;

const RING: readonly string[] = build();

export function ringColor(index: number): string {
    return RING[((index % RING_COUNT) + RING_COUNT) % RING_COUNT];
}

function build(): string[] {
    const ring: string[] = [];
    for (let i = 0; i < RING_COUNT; i++) {
        ring.push(solveForLuminance((i * 360.0) / RING_COUNT, 0.85));
    }

    return ring;
}

/**
 * Binary-searches HSL lightness until the result hits {@link TARGET_LUMINANCE}. Cheap
 * enough to run once at module load; hues differ wildly in how much lightness they need
 * (yellow reaches the target far darker than blue does), so a fixed lightness would not do.
 */
function solveForLuminance(hue: number, saturation: number): string {
    let lo = 0.0;
    let hi = 1.0;
    let rgb = { r: 128, g: 128, b: 128 };

    for (let i = 0; i < 24; i++) {
        const mid = (lo + hi) / 2.0;
        rgb = fromHsl(hue, saturation, mid);
        if (relativeLuminance(rgb) < TARGET_LUMINANCE) {
            lo = mid;
        } else {
            hi = mid;
        }
    }

    return toHex(rgb.r, rgb.g, rgb.b);
}

function fromHsl(hue: number, saturation: number, lightness: number): { r: number; g: number; b: number } {
    const h = ((((hue % 360.0) + 360.0) % 360.0) / 360.0);
    const a = saturation * (lightness < 0.5 ? lightness : 1.0 - lightness);

    return {
        r: channel(0, h, lightness, a),
        g: channel(8, h, lightness, a),
        b: channel(4, h, lightness, a),
    };
}

function channel(n: number, hue: number, lightness: number, a: number): number {
    const k = (n + hue * 12.0) % 12.0;
    let min = k - 3.0;
    if (9.0 - k < min) {
        min = 9.0 - k;
    }

    if (1.0 < min) {
        min = 1.0;
    }

    if (min < -1.0) {
        min = -1.0;
    }

    const value = (lightness - a * min) * 255.0;
    return value < 0 ? 0 : value > 255 ? 255 : Math.trunc(value);
}

function relativeLuminance(rgb: { r: number; g: number; b: number }): number {
    return 0.2126 * linearize(rgb.r) + 0.7152 * linearize(rgb.g) + 0.0722 * linearize(rgb.b);
}

function linearize(channelValue: number): number {
    const v = channelValue / 255.0;
    return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
}
