import { Settings } from '../settings';
import { INK } from './svgContext';

/**
 * Where the drawn overlay sits relative to the character it belongs to.
 *
 * Absolutely positioned, so it takes no space in the line and cannot change the layout —
 * this is the one thing that in the Visual Studio extension needed a line transform, and a
 * line transform that reads the geometry of the line it is sizing is a feedback loop that
 * broke that editor's layout twice before it was got right. Here there is nothing to
 * reserve: the ears simply overhang the line above, which is what the original does by
 * default anyway.
 *
 * Everything is in `em`, so it tracks the font size and the editor's zoom without being
 * told. The vertical anchor is half the leading plus the distance from the top of the font's
 * box to the top of a brace's ink; `identityBraces.overlayNudge` corrects the second of
 * those for a face where the assumed proportions are wrong.
 *
 * Shared with the trait gallery, which renders its previews through this same function
 * rather than illustrating them — so a preview cannot drift from what the editor draws.
 */
export function overlayCss(svg: string, settings: Settings): string {
    const widthEm = INK.width * INK.heightEm * settings.decorScale;
    const heightEm = INK.height * INK.heightEm * settings.decorScale;

    const inkTopEm = (settings.lineHeightEm - 1) / 2 + INK.topEm + settings.overlayNudgeEm;
    const inkTopWithinBox = ((0 - INK.top) / INK.height) * heightEm;
    const marginTop = inkTopEm - inkTopWithinBox;

    return (
        'position: absolute; pointer-events: none' +
        `; width: ${widthEm.toFixed(3)}em; height: ${heightEm.toFixed(3)}em` +
        // The overlay rides in the `::after` slot, whose static position is at the far side
        // of the character — so centring it on the cell means coming back half a cell to the
        // left of where it starts, not half a cell to the right of where the character does.
        `; margin-top: ${marginTop.toFixed(3)}em; margin-left: calc(-0.5ch - ${(widthEm / 2).toFixed(3)}em)` +
        `; background-image: url("${dataUri(svg)}")` +
        '; background-repeat: no-repeat; background-size: contain; background-position: center'
    );
}

/** The CSS the redrawn glyph carries, over and above its own trait styling. */
export const GLYPH_CSS = 'position: absolute; width: 1ch; text-align: center; pointer-events: none';

/**
 * An SVG as a URL.
 *
 * Percent-encoded rather than minimally escaped, because this ends up inside a `url()`
 * inside a declaration inside a rule: a stray quote or semicolon would not produce a broken
 * brace, it would produce a broken stylesheet.
 */
export function dataUri(svg: string): string {
    return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}
