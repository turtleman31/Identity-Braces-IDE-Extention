using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IdentityBraces.Classification;
using IdentityBraces.Core;
using IdentityBraces.Options;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Assembles a brace from its trait layers.
    /// </summary>
    /// <remarks>
    /// Draw order is back to front: backdrop effects, then the glyph, then costume, then
    /// creature, then motion over the whole thing. That ordering is why a cape sits behind a
    /// brace and a hat sits on top of it without either trait knowing the other exists.
    /// <para>
    /// The document is never modified. A brace drawn as a question mark in a wizard hat is
    /// still a brace in the buffer: it compiles, the caret lands on it, and Git sees nothing.
    /// </para>
    /// </remarks>
    internal static class BraceVisualFactory
    {
        private const int CycleKeyFrames = 12;

        public static BraceVisual Create(
            char character,
            BraceTraits traits,
            int colorIndex,
            ulong identity,
            GlyphContext context,
            IdentityBracesSettings settings)
        {
            if (!traits.IsDrawn)
            {
                return null;
            }

            GlyphInk ink = GlyphMetrics.Measure(context.Typeface, context.FontSize, character, context.PixelsPerDip);
            double earScale = settings.EarScalePercent / 100.0;

            // Anything worn above the glyph needs room. Reserve it once for whichever layer is
            // present rather than per trait, so line heights stay stable as traits change.
            bool needsHeadroom = traits.Creature != null || traits.Costume != null;
            double headroom = needsHeadroom
                ? CatgirlLayout.RequiredHeadroom(context.Typeface, context.FontSize, context.PixelsPerDip, character, earScale)
                : 0;

            var canvas = new Canvas
            {
                Width = context.CellWidth,
                Height = headroom + context.TextHeight,
                IsHitTestVisible = false,
            };

            var visual = new BraceVisual
            {
                Element = canvas,
                TopOffset = headroom,
                FleesCursor = HasEffect(traits, TraitIds.FleeCursor),
                HasStageFright = HasEffect(traits, TraitIds.StageFright),
            };

            Color color = BraceColors.Resolve(colorIndex, settings, context.IsDarkTheme);
            string text = TraitDrawing.ResolveBody(traits.Body, character);
            bool cycling = traits.Motion == TraitIds.ColourCycle && settings.EnableMotion;

            var brush = new SolidColorBrush(color);
            TextBlock glyph = CreateGlyph(text, brush, context, traits);
            double glyphTop = Place(canvas, glyph, context, headroom);

            double baselineY = headroom + context.BaselineOffset;
            double inkTop = baselineY - ink.AscentAboveBaseline;
            double unit = ink.Height;

            var draw = new BraceDrawContext
            {
                Canvas = canvas,
                Glyph = context,
                Ink = ink,
                Visual = visual,
                Settings = settings,
                Character = character,
                Identity = identity,
                Color = color,
                InkTop = inkTop,
                InkBottom = baselineY + ink.DescentBelowBaseline,
                Unit = unit,
                CenterX = InkCenterX(ink, context),
                DecorScale = earScale,
                GlyphElement = glyph,
            };

            draw.BandPainter = delegate(Color bandColor, double topUnits, double bottomUnits)
            {
                PaintBand(
                    canvas, text, bandColor, context, traits, headroom, glyphTop,
                    inkTop + (topUnits * unit), inkTop + (bottomUnits * unit));
            };

            draw.GlyphCopier = delegate(Color copyColor)
            {
                var copyBrush = new SolidColorBrush(copyColor);
                copyBrush.Freeze();

                TextBlock copy = CreateGlyph(text, copyBrush, context, traits);
                Place(canvas, copy, context, headroom);
                return copy;
            };

            TraitDrawing.ApplyBodyTransform(traits.Body, draw);

            // Effects run first: several are backdrops — shadow, gradient — that belong under
            // the costume and creature layers.
            for (int i = 0; i < traits.Effects.Length; i++)
            {
                TraitDrawing.Draw(traits.Effects[i], draw);
            }

            TraitDrawing.Draw(traits.Costume, draw);
            TraitDrawing.Draw(traits.Creature, draw);

            if (cycling)
            {
                AnimateColourCycle(brush, identity, visual, settings);
            }
            else
            {
                TraitDrawing.Draw(traits.Motion, draw);
            }

            return visual;
        }

        /// <summary>
        /// Draws another copy of the glyph, cropped to a horizontal band.
        /// </summary>
        /// <remarks>
        /// The band is the glyph clipped, never a rectangle behind it. That is what keeps a
        /// stocking exactly as wide as the stroke it clothes at any font size, and every
        /// effect that recolours part of a brace reuses it.
        /// </remarks>
        private static void PaintBand(
            Canvas canvas,
            string text,
            Color color,
            GlyphContext context,
            BraceTraits traits,
            double headroom,
            double glyphTop,
            double bandTop,
            double bandBottom)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            TextBlock copy = CreateGlyph(text, brush, context, traits);
            copy.Clip = new RectangleGeometry(new Rect(
                -context.CellWidth,
                bandTop - glyphTop,
                context.CellWidth * 3,
                Math.Max(0, bandBottom - bandTop)));

            Place(canvas, copy, context, headroom);
        }

        private static void AnimateColourCycle(
            SolidColorBrush brush,
            ulong identity,
            BraceVisual visual,
            IdentityBracesSettings settings)
        {
            // Animate the brush's Color rather than an element property: a colour change on a
            // SolidColorBrush is a render-thread operation, so it never triggers a measure or
            // arrange pass in the editor.
            var animation = new ColorAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(settings.CycleSeconds)),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            int start = (int)(identity % (ulong)ColorRing.Count);
            for (int k = 0; k <= CycleKeyFrames; k++)
            {
                animation.KeyFrames.Add(new LinearColorKeyFrame(
                    ColorRing.Get(start + (k * ColorRing.Count / CycleKeyFrames)),
                    KeyTime.FromPercent((double)k / CycleKeyFrames)));
            }

            Timeline.SetDesiredFrameRate(animation, settings.AnimationFrameRate);
            AnimationClock clock = animation.CreateClock();
            brush.ApplyAnimationClock(SolidColorBrush.ColorProperty, clock);
            visual.AddClock(clock.Controller);
        }

        /// <summary>
        /// Horizontal centre of the glyph's ink. Not the centre of the cell: side bearing puts
        /// a brace's ink about a tenth of a cell left of it, which is enough to make symmetric
        /// ears visibly lean.
        /// </summary>
        private static double InkCenterX(GlyphInk ink, GlyphContext context)
        {
            double runWidth = ink.Right - ink.Left;
            double runLeft = (context.CellWidth - runWidth) / 2.0;
            return runLeft + (runWidth / 2.0);
        }

        private static TextBlock CreateGlyph(string text, Brush brush, GlyphContext context, BraceTraits traits)
        {
            bool emphasised = HasEffect(traits, TraitIds.BoldItalic);
            bool foreign = traits.Body == TraitIds.ForeignFont;

            var block = new TextBlock
            {
                Text = text,
                FontFamily = foreign ? new FontFamily("Comic Sans MS") : context.Typeface.FontFamily,
                FontStyle = emphasised ? FontStyles.Italic : context.Typeface.Style,
                FontWeight = emphasised ? FontWeights.Bold : context.Typeface.Weight,
                FontStretch = context.Typeface.Stretch,
                FontSize = context.FontSize * context.BraceScale,
                Foreground = brush,
                IsHitTestVisible = false,
            };

            TextOptions.SetTextFormattingMode(block, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(block, TextRenderingMode.ClearType);

            // Measure now: BaselineOffset and DesiredSize are what align us to the editor.
            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return block;
        }

        private static bool HasEffect(BraceTraits traits, string id)
        {
            if (traits.Effects == null)
            {
                return false;
            }

            for (int i = 0; i < traits.Effects.Length; i++)
            {
                if (traits.Effects[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Centres the glyph in its cell and sits it on the editor's own baseline, so a
        /// substituted character lines up with the surrounding code rather than floating.
        /// </summary>
        private static double Place(Canvas canvas, TextBlock glyph, GlyphContext context, double topOffset)
        {
            double left = (context.CellWidth - glyph.DesiredSize.Width) / 2.0;
            double top = topOffset + context.BaselineOffset - glyph.BaselineOffset;

            Canvas.SetLeft(glyph, left);
            Canvas.SetTop(glyph, top);
            canvas.Children.Add(glyph);
            return top;
        }
    }
}
