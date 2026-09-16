using System;
using System.Windows;
using System.Windows.Media;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// The geometry of a catgirl brace, in units of the glyph's ink height.
    /// </summary>
    /// <remarks>
    /// Split out from <see cref="BraceVisualFactory"/> so that
    /// <see cref="RequiredHeadroom"/> can be reached without touching a
    /// <see cref="GlyphContext"/>.
    /// <para>
    /// That separation is load-bearing, not tidiness. The headroom feeds a line transform,
    /// and a line transform that reads the geometry of the line it is sizing is a feedback
    /// loop — layout never converges, and the editor produces over-long documents, gaps and
    /// lines pushed out of view. An earlier version did exactly that. The signature here
    /// takes a font and nothing else, so there is no longer a way to express the mistake.
    /// </para>
    /// </remarks>
    internal static class CatgirlLayout
    {
        /// <summary>How far the ears rise above the ink top, as a fraction of ink height.</summary>
        public const double EarRise = 0.85;

        /// <summary>Where the stocking begins, as a fraction of ink height below the ink top.</summary>
        public const double LegStart = 0.55;

        /// <summary>
        /// The resolution floor. A stripe thinner than two device pixels averages into its
        /// neighbours and reads as a smear.
        /// </summary>
        public const double MinStripe = 2.0;

        public static readonly Point[] EarOuter =
        {
            new Point(-0.443, -0.008), new Point(-0.310, -0.833), new Point(-0.060, -0.158),
        };

        public static readonly Point[] EarInner =
        {
            new Point(-0.335, -0.108), new Point(-0.264, -0.558), new Point(-0.135, -0.208),
        };

        /// <summary>
        /// Space a catgirl brace needs above the line's text top.
        /// </summary>
        /// <remarks>
        /// A pure function of the font. Cached per font by <see cref="GlyphMetrics"/> and
        /// rounded up, so repeated calls for the same font return a bit-identical constant —
        /// which is what a line transform requires in order to converge.
        /// </remarks>
        public static double RequiredHeadroom(
            Typeface typeface,
            double fontSize,
            double pixelsPerDip,
            char character,
            double earScale)
        {
            GlyphInk ink = GlyphMetrics.Measure(typeface, fontSize, character, pixelsPerDip);
            double inkTopFromLayoutTop = ink.LayoutBaseline - ink.AscentAboveBaseline;
            double headroom = (EarRise * earScale * ink.Height) - inkTopFromLayoutTop;
            return headroom > 0 ? Math.Ceiling(headroom) : 0;
        }
    }
}
