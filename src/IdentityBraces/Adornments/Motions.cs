using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Movement applied to the whole assembled brace.
    /// </summary>
    /// <remarks>
    /// One per brace, because they all compete for the same render transform. Every one of
    /// them runs through <see cref="BraceDrawContext.Animate"/>, which respects the global
    /// motion switch, caps the frame rate, and hands the clock to the view so it can be paused
    /// when the tab is not visible.
    /// <para>
    /// Transforms and opacity only — never anything that triggers a measure or arrange pass,
    /// because these run on braces by the dozen inside a live editor.
    /// </para>
    /// </remarks>
    internal static class Motions
    {
        public static void Register(Dictionary<string, Action<BraceDrawContext>> map)
        {
            map[TraitIds.ColourCycle] = ColourCycle;
            map[TraitIds.Wobble] = Wobble;
            map[TraitIds.Bounce] = Bounce;
            map[TraitIds.Breathe] = Breathe;
            map[TraitIds.Heartbeat] = Heartbeat;
            map[TraitIds.Shiver] = Shiver;
            map[TraitIds.Blink] = Blink;
            map[TraitIds.Spin] = Spin;
            map[TraitIds.Flip] = Flip;
            map[TraitIds.Glitch] = Glitch;
            map[TraitIds.Sparkle] = Sparkle;
            map[TraitIds.Drift] = Drift;
            map[TraitIds.Shimmer] = Shimmer;
            map[TraitIds.Flicker] = Flicker;
            map[TraitIds.Wave] = Wave;
            map[TraitIds.Typewriter] = Typewriter;
        }

        private static Duration Sec(double seconds)
        {
            return new Duration(TimeSpan.FromSeconds(seconds));
        }

        private static RotateTransform Rotator(BraceDrawContext c, double originY = 0.75)
        {
            var rotate = new RotateTransform();
            Combine(c, rotate, originY);
            return rotate;
        }

        private static TranslateTransform Mover(BraceDrawContext c)
        {
            var move = new TranslateTransform();
            Combine(c, move, 0.5);
            return move;
        }

        private static ScaleTransform Scaler(BraceDrawContext c)
        {
            var scale = new ScaleTransform(1, 1);
            Combine(c, scale, 0.5);
            return scale;
        }

        /// <summary>
        /// Attaches a transform to the whole canvas, preserving anything already there.
        /// </summary>
        /// <remarks>
        /// A body trait may already have set a mirror or a rotation on the glyph, and an
        /// effect may have set a tilt. Assigning over the top would silently drop those, so
        /// transforms compose into a group instead.
        /// </remarks>
        private static void Combine(BraceDrawContext c, Transform transform, double originY)
        {
            c.Canvas.RenderTransformOrigin = new Point(0.5, originY);

            var existing = c.Canvas.RenderTransform as TransformGroup;
            if (existing != null)
            {
                existing.Children.Add(transform);
                return;
            }

            var group = new TransformGroup();
            if (c.Canvas.RenderTransform != null && !(c.Canvas.RenderTransform is MatrixTransform))
            {
                group.Children.Add(c.Canvas.RenderTransform);
            }

            group.Children.Add(transform);
            c.Canvas.RenderTransform = group;
        }

        private static void ColourCycle(BraceDrawContext c)
        {
            // Handled by the factory, which owns the glyph's brush. Registered so the trait
            // resolves and the roll accounts for it.
        }

        private static void Wobble(BraceDrawContext c)
        {
            var rotate = Rotator(c);
            c.Animate(rotate, RotateTransform.AngleProperty, new DoubleAnimation(-9, 9, Sec(1.6))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });
        }

        private static void Bounce(BraceDrawContext c)
        {
            var move = Mover(c);
            c.Animate(move, TranslateTransform.YProperty, new DoubleAnimation(0, -c.Unit * 0.28, Sec(0.7))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
        }

        private static void Breathe(BraceDrawContext c)
        {
            var scale = Scaler(c);
            var pulse = new DoubleAnimation(0.94, 1.06, Sec(2.6))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };

            c.Animate(scale, ScaleTransform.ScaleXProperty, pulse);
            c.Animate(scale, ScaleTransform.ScaleYProperty, pulse.Clone());
        }

        private static void Heartbeat(BraceDrawContext c)
        {
            var scale = Scaler(c);
            var beat = new DoubleAnimationUsingKeyFrames
            {
                Duration = Sec(1.4),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            beat.KeyFrames.Add(new LinearDoubleKeyFrame(1.00, KeyTime.FromPercent(0.00)));
            beat.KeyFrames.Add(new LinearDoubleKeyFrame(1.18, KeyTime.FromPercent(0.10)));
            beat.KeyFrames.Add(new LinearDoubleKeyFrame(1.00, KeyTime.FromPercent(0.20)));
            beat.KeyFrames.Add(new LinearDoubleKeyFrame(1.14, KeyTime.FromPercent(0.30)));
            beat.KeyFrames.Add(new LinearDoubleKeyFrame(1.00, KeyTime.FromPercent(0.42)));

            c.Animate(scale, ScaleTransform.ScaleXProperty, beat);
            c.Animate(scale, ScaleTransform.ScaleYProperty, beat.Clone());
        }

        private static void Shiver(BraceDrawContext c)
        {
            var move = Mover(c);
            var jitter = new DoubleAnimationUsingKeyFrames
            {
                Duration = Sec(0.24),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            jitter.KeyFrames.Add(new DiscreteDoubleKeyFrame(-0.6, KeyTime.FromPercent(0.00)));
            jitter.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.7, KeyTime.FromPercent(0.33)));
            jitter.KeyFrames.Add(new DiscreteDoubleKeyFrame(-0.3, KeyTime.FromPercent(0.66)));

            c.Animate(move, TranslateTransform.XProperty, jitter);
            c.Animate(move, TranslateTransform.YProperty, jitter.Clone());
        }

        private static void Blink(BraceDrawContext c)
        {
            var blink = new DoubleAnimationUsingKeyFrames
            {
                Duration = Sec(3.1),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromPercent(0.00)));
            blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.15, KeyTime.FromPercent(0.88)));
            blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromPercent(0.94)));

            c.Animate(c.Canvas, UIElement.OpacityProperty, blink);
        }

        private static void Spin(BraceDrawContext c)
        {
            var rotate = Rotator(c, 0.5);
            c.Animate(rotate, RotateTransform.AngleProperty, new DoubleAnimation(0, 360, Sec(4.5))
            {
                RepeatBehavior = RepeatBehavior.Forever,
            });
        }

        private static void Flip(BraceDrawContext c)
        {
            var rotate = Rotator(c, 0.5);
            var flip = new DoubleAnimationUsingKeyFrames
            {
                Duration = Sec(3.4),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            flip.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0.00)));
            flip.KeyFrames.Add(new DiscreteDoubleKeyFrame(180, KeyTime.FromPercent(0.50)));

            c.Animate(rotate, RotateTransform.AngleProperty, flip);
        }

        private static void Glitch(BraceDrawContext c)
        {
            var move = Mover(c);
            var jump = new DoubleAnimationUsingKeyFrames
            {
                Duration = Sec(2.0),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            jump.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0.00)));
            jump.KeyFrames.Add(new DiscreteDoubleKeyFrame(2.0, KeyTime.FromPercent(0.82)));
            jump.KeyFrames.Add(new DiscreteDoubleKeyFrame(-1.5, KeyTime.FromPercent(0.86)));
            jump.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0.90)));

            c.Animate(move, TranslateTransform.XProperty, jump);
        }

        private static void Sparkle(BraceDrawContext c)
        {
            for (int i = 0; i < 2; i++)
            {
                double x = i == 0 ? -0.44 : 0.44;
                double y = i == 0 ? -0.10 : 0.62;
                var star = c.Dot(Color.FromRgb(0xFF, 0xF3, 0xC4), x, y, 0.10);

                var twinkle = new DoubleAnimation(0, 1, Sec(1.1 + (i * 0.5)))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                };

                c.Animate(star, UIElement.OpacityProperty, twinkle);
            }
        }

        private static void Drift(BraceDrawContext c)
        {
            var move = Mover(c);
            double phase = 3.0 + (c.Roll(0x0D21F7UL) * 2.0);

            c.Animate(move, TranslateTransform.XProperty, new DoubleAnimation(-1.6, 1.6, Sec(phase))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });

            c.Animate(move, TranslateTransform.YProperty, new DoubleAnimation(1.2, -1.2, Sec(phase * 1.37))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });
        }

        private static void Shimmer(BraceDrawContext c)
        {
            var band = c.Box(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF), -0.5, -0.1, 1.0, 0.22);
            var move = new TranslateTransform();
            band.RenderTransform = move;

            c.Animate(move, TranslateTransform.YProperty, new DoubleAnimation(0, c.Unit * 1.2, Sec(2.4))
            {
                RepeatBehavior = RepeatBehavior.Forever,
            });
        }

        private static void Flicker(BraceDrawContext c)
        {
            var flicker = new DoubleAnimationUsingKeyFrames
            {
                Duration = Sec(0.9),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(1.00, KeyTime.FromPercent(0.00)));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(0.72, KeyTime.FromPercent(0.22)));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromPercent(0.44)));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(0.65, KeyTime.FromPercent(0.68)));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(1.00, KeyTime.FromPercent(1.00)));

            c.Animate(c.Canvas, UIElement.OpacityProperty, flicker);
        }

        /// <summary>
        /// A bounce whose phase comes from the brace's column, so neighbours move in sequence
        /// and the motion appears to travel along the line.
        /// </summary>
        private static void Wave(BraceDrawContext c)
        {
            var move = Mover(c);
            var hop = new DoubleAnimation(0, -c.Unit * 0.24, Sec(0.9))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(c.Roll(0x7AFE12UL) * 0.9),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };

            c.Animate(move, TranslateTransform.YProperty, hop);
        }

        private static void Typewriter(BraceDrawContext c)
        {
            c.Animate(c.Canvas, UIElement.OpacityProperty, new DoubleAnimation(0, 1, Sec(0.45)));
        }
    }
}
