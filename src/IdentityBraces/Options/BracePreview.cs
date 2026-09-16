using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IdentityBraces.Adornments;
using IdentityBraces.Classification;
using IdentityBraces.Core;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace IdentityBraces.Options
{
    /// <summary>
    /// Renders sample braces outside the editor, for the options page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole point is that this runs <see cref="BraceVisualFactory"/> itself rather than
    /// reimplementing it. A preview drawn by a second renderer would drift from the real one
    /// the first time a trait was tweaked, and a preview you cannot trust is worse than none —
    /// it would be actively misleading about a catalogue of eighty-six traits nobody can
    /// otherwise see without turning them on and hunting through a file.
    /// </para>
    /// <para>
    /// The only thing the factory needs that an options page does not have is a
    /// <see cref="GlyphContext"/>, which normally comes from a text view's line geometry. It is
    /// synthesised here from a <see cref="FormattedText"/> measurement of the same font, which
    /// is where the editor's own numbers come from anyway.
    /// </para>
    /// </remarks>
    internal static class BracePreview
    {
        private static readonly Typeface Fallback = new Typeface("Consolas");

        /// <summary>The characters a sample strip cycles through.</summary>
        private static readonly char[] Sequence = { '{', '}', '(', ')', '[', ']' };

        /// <summary>
        /// The editor's own font and theme, at <paramref name="targetFontSize"/> px — or at the
        /// editor's own size when that is zero or less.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read through the component model rather than a view, because an options page has no
        /// view. <c>GetClassificationFormatMap("text")</c> is the viewless overload and gives
        /// the same default text properties the editor would hand a real one — so the preview
        /// is in the font the braces will actually be.
        /// </para>
        /// <para>
        /// The magnified preview asks for an <em>absolute</em> size rather than a multiple of
        /// the editor's. A multiple would make the preview box four times whatever the user
        /// reads code at, so the panel would be one height for a 9pt editor and spill out of
        /// the dialog for a 16pt one.
        /// </para>
        /// </remarks>
        public static GlyphContext BuildContext(IdentityBracesSettings settings, double pixelsPerDip, double targetFontSize)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Typeface typeface = Fallback;
            double fontSize = 12.0;
            bool isDark = true;

            try
            {
                IClassificationFormatMap map = TryGetFormatMap();
                if (map != null)
                {
                    isDark = BraceColors.IsDarkTheme(map);

                    TextFormattingRunProperties properties = map.DefaultTextProperties;
                    if (!properties.TypefaceEmpty)
                    {
                        typeface = properties.Typeface;
                    }

                    if (!properties.FontRenderingEmSizeEmpty)
                    {
                        fontSize = properties.FontRenderingEmSize;
                    }
                }
            }
            catch (Exception)
            {
                // The preview is worth having in the wrong font. It is not worth an exception
                // out of an options page.
            }

            return PreviewMetrics.Build(
                typeface,
                targetFontSize > 0 ? targetFontSize : fontSize,
                pixelsPerDip,
                isDark,
                settings.BraceScalePercent / 100.0);
        }

        /// <summary>
        /// The editor's real background, for the strip to sit on.
        /// </summary>
        /// <remarks>
        /// Not cosmetic. The palette is luminance-solved to sit at equal contrast against
        /// <c>#1E1E1E</c> and against white, so judging whether a colour reads is only
        /// meaningful against the background it will actually be read on. A preview on the
        /// dialog's own grey would answer a question nobody asked.
        /// </remarks>
        public static Brush EditorBackground()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Color color = Color.FromRgb(0x1E, 0x1E, 0x1E);

            try
            {
                IClassificationFormatMap map = TryGetFormatMap();
                if (map != null)
                {
                    TextFormattingRunProperties properties = map.DefaultTextProperties;
                    var brush = properties.BackgroundBrushEmpty
                        ? null
                        : properties.BackgroundBrush as SolidColorBrush;

                    if (brush != null)
                    {
                        color = brush.Color;
                    }
                }
            }
            catch (Exception)
            {
            }

            var result = new SolidColorBrush(color);
            result.Freeze();
            return result;
        }

        private static IClassificationFormatMap TryGetFormatMap()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var model = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            if (model == null)
            {
                return null;
            }

            var service = model.GetService<IClassificationFormatMapService>();

            // The viewless overload. An options page has no view, and "text" is the category
            // the editor itself falls back to for default text properties.
            return service == null ? null : service.GetClassificationFormatMap("text");
        }

        /// <summary>
        /// One sample: the identity that produced it and everything it rolled.
        /// </summary>
        public struct Sample
        {
            public char Character;
            public ulong Identity;
            public int ColorIndex;
            public BraceTraits Traits;
        }

        /// <summary>
        /// Rolls <paramref name="count"/> braces against the current weights, and gives each a
        /// character and a colour.
        /// </summary>
        /// <remarks>
        /// The roll itself lives in <see cref="TraitSampler"/>, which is free of Visual Studio
        /// and WPF and therefore covered by the test suite. What is added here is only what
        /// needs the palette: which character to draw, and which of the 32 colours it wears.
        /// </remarks>
        public static List<Sample> Roll(IdentityBracesSettings settings, int count)
        {
            TraitSampler.Sample[] rolled = TraitSampler.Take(settings.ToTraitWeights(), count);
            bool byDepth = settings.ColorMode == BraceColorMode.Depth;

            var samples = new List<Sample>(rolled.Length);

            for (int i = 0; i < rolled.Length; i++)
            {
                samples.Add(new Sample
                {
                    Character = Sequence[i % Sequence.Length],
                    Identity = rolled[i].Identity,
                    ColorIndex = byDepth
                        ? rolled[i].Depth
                        : Hash.ToIndex(rolled[i].Identity, BracePalette.Count),
                    Traits = rolled[i].Traits,
                });
            }

            return samples;
        }

        /// <summary>
        /// Draws one sample, returning the element and the visual that owns its animations.
        /// </summary>
        /// <remarks>
        /// A brace that rolled nothing is not skipped — it is drawn plain, in its palette
        /// colour. Four braces in five are plain at the shipped defaults, and a strip showing
        /// only the decorated ones would misrepresent the density by a factor of five.
        /// </remarks>
        public static FrameworkElement Draw(
            Sample sample,
            GlyphContext context,
            IdentityBracesSettings settings,
            out BraceVisual visual)
        {
            visual = BraceVisualFactory.Create(
                sample.Character,
                sample.Traits,
                sample.ColorIndex,
                sample.Identity,
                context,
                settings);

            if (visual != null && visual.Element is FrameworkElement)
            {
                return (FrameworkElement)visual.Element;
            }

            visual = null;
            return DrawPlain(sample, context, settings);
        }

        private static FrameworkElement DrawPlain(
            Sample sample,
            GlyphContext context,
            IdentityBracesSettings settings)
        {
            var brush = new SolidColorBrush(
                BraceColors.Resolve(sample.ColorIndex, settings, context.IsDarkTheme));
            brush.Freeze();

            var glyph = new TextBlock
            {
                Text = sample.Character.ToString(),
                FontFamily = context.Typeface.FontFamily,
                FontStyle = context.Typeface.Style,
                FontWeight = context.Typeface.Weight,
                FontStretch = context.Typeface.Stretch,
                FontSize = context.FontSize,
                Foreground = brush,
                IsHitTestVisible = false,
            };

            // Wrapped in a cell of the same size a drawn brace gets, so plain and decorated
            // samples sit on one grid instead of the plain ones closing up the gaps.
            var cell = new Canvas
            {
                Width = context.CellWidth,
                Height = context.TextHeight,
                IsHitTestVisible = false,
            };

            cell.Children.Add(glyph);
            return cell;
        }
    }
}
