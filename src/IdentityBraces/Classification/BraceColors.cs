using System.Windows.Media;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace IdentityBraces.Classification
{
    /// <summary>
    /// Resolves the colour a brace should actually be drawn in, given the settings and the
    /// editor's theme.
    /// </summary>
    /// <remarks>
    /// Single source of truth for both paths: the classifier colours plain braces through it,
    /// and the adornment factory colours drawn ones through it. They must never disagree, or
    /// a personality brace would be a different colour from the plain brace beside it.
    /// </remarks>
    internal static class BraceColors
    {
        /// <remarks>
        /// <see cref="BraceColorMode.Depth"/> needs no branch here: the scanner has already
        /// resolved the index from nesting depth rather than from the identity hash, so by the
        /// time a colour is being resolved there is nothing left to decide. Both modes are
        /// "look up this index in the palette", and keeping it that way means depth colour
        /// reaches the drawn braces and the plain ones through exactly one path.
        /// </remarks>
        public static Color Resolve(int colorIndex, IdentityBracesSettings settings, bool isDarkTheme)
        {
            if (settings.ColorMode == BraceColorMode.Monochrome)
            {
                return isDarkTheme ? Colors.White : Colors.Black;
            }

            return BracePalette.GetColor(colorIndex);
        }

        /// <summary>
        /// True when the editor is painting on a dark background.
        /// </summary>
        /// <remarks>
        /// Read from the editor's own background brush rather than from the Visual Studio
        /// theme, so a custom Fonts and Colors background is honoured too — someone running a
        /// dark editor inside a light shell gets white braces, which is what they meant.
        /// <para>
        /// The threshold is relative luminance, not a naive RGB average: a saturated blue and
        /// a saturated yellow of the same average are nowhere near the same brightness.
        /// </para>
        /// </remarks>
        public static bool IsDarkTheme(IClassificationFormatMap formatMap)
        {
            if (formatMap == null)
            {
                return true;
            }

            try
            {
                TextFormattingRunProperties properties = formatMap.DefaultTextProperties;
                if (properties.BackgroundBrushEmpty)
                {
                    return true;
                }

                var brush = properties.BackgroundBrush as SolidColorBrush;
                if (brush == null)
                {
                    return true;
                }

                return RelativeLuminance(brush.Color) < 0.5;
            }
            catch (System.Exception)
            {
                return true;
            }
        }

        private static double RelativeLuminance(Color color)
        {
            return (0.2126 * Linearize(color.R))
                 + (0.7152 * Linearize(color.G))
                 + (0.0722 * Linearize(color.B));
        }

        private static double Linearize(byte channel)
        {
            double v = channel / 255.0;
            return v <= 0.03928 ? v / 12.92 : System.Math.Pow((v + 0.055) / 1.055, 2.4);
        }
    }
}
