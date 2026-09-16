using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace IdentityBraces.Adornments
{
    /// <summary>Where a character's actual ink sits, relative to its baseline and layout origin.</summary>
    internal struct GlyphInk
    {
        public double AscentAboveBaseline;
        public double DescentBelowBaseline;
        public double Left;
        public double Right;

        /// <summary>
        /// Baseline measured from the top of the font's own layout box.
        /// </summary>
        /// <remarks>
        /// The font's number, not the line's. Anything consumed by
        /// <see cref="BraceLineTransformSource"/> must come from here rather than from
        /// <c>line.Baseline - line.TextTop</c>: a line transform that reads the geometry of
        /// the line it is sizing feeds back into itself and never converges.
        /// </remarks>
        public double LayoutBaseline;

        public double Height
        {
            get { return AscentAboveBaseline + DescentBelowBaseline; }
        }

        public double CenterX
        {
            get { return (Left + Right) / 2.0; }
        }

        public bool IsEmpty
        {
            get { return Height <= 0 || Right <= Left; }
        }
    }

    /// <summary>
    /// Measures the ink box of a glyph and caches it per font.
    /// </summary>
    /// <remarks>
    /// This exists because the em box is not the glyph. For Consolas at 13.33&#160;px the cell is
    /// 7.33&#160;&#215;&#160;15 but a brace's ink is only 6&#160;&#215;&#160;12, sitting 3.3&#160;px below the top of the
    /// em box and 0.7&#160;px left of the cell's centre (it carries right side bearing).
    /// <para>
    /// Decorations anchored to the em box therefore float above the glyph and lean right,
    /// which is exactly what the first version of the cat ears did. Everything drawn on or
    /// around a brace is positioned from these numbers instead.
    /// </para>
    /// </remarks>
    internal static class GlyphMetrics
    {
        private struct Key : IEquatable<Key>
        {
            public Typeface Typeface;
            public double FontSize;
            public char Character;

            public bool Equals(Key other)
            {
                return Character == other.Character
                    && FontSize.Equals(other.FontSize)
                    && Equals(Typeface, other.Typeface);
            }

            public override bool Equals(object obj)
            {
                return obj is Key && Equals((Key)obj);
            }

            public override int GetHashCode()
            {
                int hash = Character.GetHashCode();
                hash = (hash * 397) ^ FontSize.GetHashCode();
                hash = (hash * 397) ^ (Typeface == null ? 0 : Typeface.GetHashCode());
                return hash;
            }
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<Key, GlyphInk> Cache = new Dictionary<Key, GlyphInk>();

        public static GlyphInk Measure(Typeface typeface, double fontSize, char character, double pixelsPerDip)
        {
            var key = new Key { Typeface = typeface, FontSize = fontSize, Character = character };

            lock (Gate)
            {
                GlyphInk cached;
                if (Cache.TryGetValue(key, out cached))
                {
                    return cached;
                }
            }

            GlyphInk ink = MeasureCore(typeface, fontSize, character, pixelsPerDip);

            lock (Gate)
            {
                Cache[key] = ink;
            }

            return ink;
        }

        private static GlyphInk MeasureCore(Typeface typeface, double fontSize, char character, double pixelsPerDip)
        {
            try
            {
                var text = new FormattedText(
                    character.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.Black,
                    pixelsPerDip <= 0 ? 1.0 : pixelsPerDip);

                Geometry geometry = text.BuildGeometry(new Point(0, 0));
                Rect bounds = geometry == null ? Rect.Empty : geometry.Bounds;

                if (bounds.IsEmpty || bounds.Height <= 0)
                {
                    return Fallback(fontSize);
                }

                return new GlyphInk
                {
                    AscentAboveBaseline = text.Baseline - bounds.Top,
                    DescentBelowBaseline = bounds.Bottom - text.Baseline,
                    Left = bounds.Left,
                    Right = bounds.Right,
                    LayoutBaseline = text.Baseline,
                };
            }
            catch (Exception)
            {
                // A broken or unusual font must not cost us the adornment.
                return Fallback(fontSize);
            }
        }

        /// <summary>Proportions close to Consolas, used when a font refuses to be measured.</summary>
        private static GlyphInk Fallback(double fontSize)
        {
            return new GlyphInk
            {
                AscentAboveBaseline = fontSize * 0.675,
                DescentBelowBaseline = fontSize * 0.225,
                Left = 0,
                Right = fontSize * 0.45,
                LayoutBaseline = fontSize * 0.9,
            };
        }
    }
}
