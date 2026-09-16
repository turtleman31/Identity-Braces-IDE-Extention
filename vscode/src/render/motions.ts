import { TraitIds } from '../core/traitIds';
import { Painter } from './creatures';
import { SvgContext } from './svgContext';

/**
 * The two motion traits that draw something.
 *
 * Every other motion is a transform or an opacity on the brace as a whole, and those are in
 * {@link module:render/braceStyle}. The split is not arbitrary: a transform belongs on the
 * element that carries the glyph, and this overlay is a separate element sitting over it.
 * Put a spin here and the ears would rotate around a stationary brace.
 */

export function registerMotions(map: Map<string, Painter>): void {
    map.set(TraitIds.Sparkle, sparkle);
    map.set(TraitIds.Shimmer, shimmer);
}

/** Four-point stars appearing and fading, on two different periods. */
function sparkle(c: SvgContext): void {
    for (let i = 0; i < 2; i++) {
        const x = i === 0 ? -0.44 : 0.44;
        const y = i === 0 ? -0.1 : 0.62;
        const twinkle = c.swing(2.2 + i * 1.0);

        c.group(`opacity="${twinkle.toFixed(2)}"`);
        c.dot('#FFF3C4', x, y, 0.1);
        c.endGroup();
    }
}

/** A bright band sweeping down the stroke. */
function shimmer(c: SvgContext): void {
    const t = c.phase(2.4);
    c.group('opacity="0.56"');
    c.box('#FFFFFF', -0.5, -0.1 + t * 1.2, 1.0, 0.22);
    c.endGroup();
}
