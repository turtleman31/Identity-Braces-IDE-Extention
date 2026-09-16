using System;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Builds a <see cref="GlyphContext"/> from the editor's own font and line metrics.
    /// </summary>
    /// <remarks>
    /// Shared by <see cref="AdornmentManager"/> and <see cref="BraceLineTransformSource"/>.
    /// They have to agree exactly: one reserves the space above a line and the other draws
    /// into it, and any disagreement shows up as clipped ears or a ragged gap.
    /// </remarks>
    internal static class GlyphContextFactory
    {
        private static readonly Typeface Fallback = new Typeface("Consolas");

        public static GlyphContext Build(
            IClassificationFormatMap formatMap,
            IWpfTextView view,
            ITextViewLine line)
        {
            var context = new GlyphContext
            {
                TextHeight = line.TextHeight,

                // ITextViewLine.Baseline is the distance from the top of the LINE to the
                // baseline — a small relative value, around 12px. TextTop and Top are
                // absolute view coordinates, in the hundreds or thousands.
                //
                // This was `line.Baseline - line.TextTop`, which mixes the two spaces and
                // yields roughly -488, varying per line by exactly the negative of that
                // line's own position. The glyph inside the container was therefore offset
                // by the negation of wherever the container had been correctly placed, so
                // the two cancelled and every glyph collapsed onto a single screen row with
                // its horizontal position untouched.
                //
                // That is the bug behind every "all the special braces are on one line"
                // report, and it survived ten rounds of positioning fixes because those all
                // corrected the container while this quietly undid them — and because the
                // diagnostics measured the container rather than the glyph.
                //
                // Baseline is already measured from the text top, so it is used as-is.
                //
                // Confirmed empirically on CodeLens lines. CodeLens reserves space above a
                // line through a line transform, exactly as the cat-ear headroom does, which
                // makes TextTop - Top equal to the CodeLens height there and zero everywhere
                // else. Subtracting that term put the glyphs of a decorated line up inside
                // the CodeLens row while leaving every undecorated line correct — so the term
                // was wrong, and Baseline needs no re-basing at all.
                BaselineOffset = line.Baseline,
                Typeface = Fallback,
                FontSize = 12.0,
                PixelsPerDip = 1.0,
                CellWidth = 0,
                IsDarkTheme = Classification.BraceColors.IsDarkTheme(formatMap),
                BraceScale = Options.IdentityBracesSettings.Current.BraceScalePercent / 100.0,
            };

            try
            {
                TextFormattingRunProperties properties = formatMap.DefaultTextProperties;

                if (!properties.TypefaceEmpty)
                {
                    context.Typeface = properties.Typeface;
                }

                if (!properties.FontRenderingEmSizeEmpty)
                {
                    context.FontSize = properties.FontRenderingEmSize;
                }
            }
            catch (InvalidOperationException)
            {
                // Fall back to the defaults above rather than losing the adornment.
            }

            try
            {
                if (view != null && view.VisualElement != null)
                {
                    context.PixelsPerDip = VisualTreeHelper.GetDpi(view.VisualElement).PixelsPerDip;
                }
            }
            catch (Exception)
            {
                // Ink measurement tolerates a wrong DPI far better than a missing adornment.
            }

            return context;
        }
    }
}
