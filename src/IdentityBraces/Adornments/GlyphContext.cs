using System.Windows.Media;

namespace IdentityBraces.Adornments
{
    /// <summary>Font and cell geometry for one drawn brace.</summary>
    internal struct GlyphContext
    {
        public Typeface Typeface;
        public double FontSize;
        public double PixelsPerDip;

        /// <summary>Width of the character cell in the view.</summary>
        public double CellWidth;

        /// <summary>Height of the line's text.</summary>
        public double TextHeight;

        /// <summary>Baseline measured down from the line's text top.</summary>
        public double BaselineOffset;

        /// <summary>Whether the editor is painting on a dark background.</summary>
        public bool IsDarkTheme;

        /// <summary>Brace size multiplier, 1.0 at 100%.</summary>
        public double BraceScale;
    }
}
