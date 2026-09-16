using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Builds a <see cref="GlyphContext"/> from a font alone, with no text view to measure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The options page has to draw braces and has no editor to take line geometry from. These
    /// are the same numbers a view would have supplied, read straight out of the font — which
    /// is where the editor gets them too.
    /// </para>
    /// <para>
    /// Deliberately free of Visual Studio types so it can be tested against real fonts on a
    /// bare runtime. A context with a zero cell width lays every sample on top of the last one
    /// and the preview renders as a single smear, which looks exactly like the feature not
    /// working at all — so it is worth being able to assert that it does not happen.
    /// </para>
    /// </remarks>
    internal static class PreviewMetrics
    {
        /// <summary>Proportions close to Consolas, for a font that refuses to be measured.</summary>
        private const double FallbackCellRatio = 0.55;
        private const double FallbackHeightRatio = 1.25;
        private const double FallbackBaselineRatio = 0.9;

        public static GlyphContext Build(
            Typeface typeface,
            double fontSize,
            double pixelsPerDip,
            bool isDarkTheme,
            double braceScale)
        {
            if (!(fontSize > 0))
            {
                fontSize = 12.0;
            }

            if (!(pixelsPerDip > 0))
            {
                pixelsPerDip = 1.0;
            }

            double cellWidth = fontSize * FallbackCellRatio;
            double textHeight = fontSize * FallbackHeightRatio;
            double baseline = fontSize * FallbackBaselineRatio;

            try
            {
                var text = new FormattedText(
                    "{",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface ?? new Typeface("Consolas"),
                    fontSize,
                    Brushes.Black,
                    pixelsPerDip);

                // The glyph's advance, not its ink. The editor reports the character cell
                // through GetCharacterBounds, and packing samples to their ink instead would
                // sit them tighter than real code and make every decoration look like it
                // overhangs.
                if (text.WidthIncludingTrailingWhitespace > 0)
                {
                    cellWidth = text.WidthIncludingTrailingWhitespace;
                }

                if (text.Height > 0)
                {
                    textHeight = text.Height;
                    baseline = text.Baseline;
                }
            }
            catch (Exception)
            {
                // The ratios above are close enough to lay out sensibly.
            }

            return new GlyphContext
            {
                Typeface = typeface,
                FontSize = fontSize,
                PixelsPerDip = pixelsPerDip,
                CellWidth = cellWidth,
                TextHeight = textHeight,
                BaselineOffset = baseline,
                IsDarkTheme = isDarkTheme,
                BraceScale = braceScale,
            };
        }
    }
}
