/**
 * The 32 identities a brace can wear.
 *
 * Hues are spaced by the golden angle (137.508 degrees) rather than evenly, so consecutive
 * indices — which frequently end up next to each other on screen — land far apart on the
 * colour wheel instead of shading into one another.
 *
 * Every entry was then lightness-solved to a relative luminance of 0.2072, the point at
 * which contrast against the dark editor background (#1E1E1E) and against white are equal.
 * The result is ~4.05:1 on both, so one palette serves every theme instead of looking
 * correct on dark and vanishing on light. Override them with `identityBraces.palette`.
 */
export const PALETTE_COUNT = 32;

export const PALETTE: readonly string[] = [
    '#EE3333', // 00
    '#19903C', // 01
    '#B047FA', // 02
    '#8C7E21', // 03
    '#0D89A2', // 04
    '#DD3C93', // 05
    '#279204', // 06
    '#7570DE', // 07
    '#D75311', // 08
    '#198E63', // 09
    '#D806EB', // 10
    '#718620', // 11
    '#137DE9', // 12
    '#DE4565', // 13
    '#049310', // 14
    '#9266DB', // 15
    '#A7740E', // 16
    '#198C88', // 17
    '#EA06B0', // 18
    '#528C21', // 19
    '#5A73F2', // 20
    '#DC4D38', // 21
    '#04923F', // 22
    '#B155D7', // 23
    '#82820B', // 24
    '#1F87B2', // 25
    '#F60669', // 26
    '#2F9022', // 27
    '#7F68F3', // 28
    '#BC6821', // 29
    '#048E6C', // 30
    '#D139CA', // 31
];

const HEX = /^#?([0-9a-f]{6})$/i;

/**
 * Merges a user palette over the built-in one.
 *
 * Short and malformed entries fall back rather than throwing: a typo in one of thirty-two
 * colours should cost you that colour, not the extension.
 */
export function resolvePalette(overrides: readonly string[] | undefined): readonly string[] {
    if (!overrides || overrides.length === 0) {
        return PALETTE;
    }

    return PALETTE.map((fallback, i) => {
        const match = HEX.exec((overrides[i] ?? '').trim());
        return match ? `#${match[1].toUpperCase()}` : fallback;
    });
}

/** Splits `#RRGGBB` into channels. Returns mid-grey for anything unparseable. */
export function toRgb(hex: string): { r: number; g: number; b: number } {
    const match = HEX.exec(hex.trim());
    if (!match) {
        return { r: 128, g: 128, b: 128 };
    }

    const value = parseInt(match[1], 16);
    return { r: (value >> 16) & 0xff, g: (value >> 8) & 0xff, b: value & 0xff };
}

export function toHex(r: number, g: number, b: number): string {
    const clamp = (v: number) => Math.max(0, Math.min(255, Math.round(v)));
    return `#${((1 << 24) | (clamp(r) << 16) | (clamp(g) << 8) | clamp(b)).toString(16).slice(1).toUpperCase()}`;
}

/** `#RRGGBB` plus an alpha in [0,1], as the `#RRGGBBAA` the editor accepts. */
export function withAlpha(hex: string, alpha: number): string {
    const a = Math.max(0, Math.min(255, Math.round(alpha * 255)));
    return `${hex}${a.toString(16).padStart(2, '0').toUpperCase()}`;
}

/** Scales every channel toward black. Used for the gradient effect's lower band. */
export function darken(hex: string, factor: number): string {
    const { r, g, b } = toRgb(hex);
    return toHex(r * factor, g * factor, b * factor);
}
