using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Modifiers that roll independently, so a brace can carry several at once.
    /// </summary>
    /// <remarks>
    /// Unlike the other layers, effects do not share a single 0-100 band — each is its own
    /// coin flip. A brace can be on fire, tilted and carrying a shadow simultaneously, which
    /// is the point: the layers exist so a wizard can also be burning.
    /// </remarks>
    internal static class Effects
    {
        private static readonly Color FlameCore = Color.FromRgb(0xFF, 0xD4, 0x4A);
        private static readonly Color FlameMid = Color.FromRgb(0xF2, 0x7A, 0x1A);
        private static readonly Color FlameTip = Color.FromRgb(0xD1, 0x2B, 0x1B);
        private static readonly Color SweatBead = Color.FromRgb(0x6C, 0xC6, 0xF0);

        public static void Register(Dictionary<string, Action<BraceDrawContext>> map)
        {
            map[TraitIds.Fire] = Fire;
            map[TraitIds.Tilted] = Tilted;
            map[TraitIds.Shadow] = Shadow;
            map[TraitIds.Underline] = Underline;
            map[TraitIds.GradientFill] = GradientFill;
            map[TraitIds.Drunk] = Drunk;
            map[TraitIds.Gravity] = Gravity;
            map[TraitIds.Nocturnal] = Nocturnal;
            map[TraitIds.Seasonal] = Seasonal;
            map[TraitIds.Distressed] = Distressed;
            map[TraitIds.Mitosis] = Mitosis;
            map[TraitIds.BuildReactive] = BuildReactive;
        }

        private static readonly Color CelebrationGreen = Color.FromRgb(0x4C, 0xC2, 0x5E);
        private static readonly Color SulkGrey = Color.FromRgb(0x8A, 0x8A, 0x92);

        /// <summary>
        /// Celebrates a green build and sulks at a red one, for a short while afterwards.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Draws nothing at all outside <see cref="Options.BuildStatus.ReactionWindow"/>, which
        /// is what keeps this from being a permanent change of costume. It also means nothing
        /// has to invalidate the adornments a second time when the window closes: a brace
        /// scrolled into view after it has passed simply draws nothing.
        /// </para>
        /// <para>
        /// Deliberately small. A build failing is already loud — the error list, the squiggles,
        /// the output window — and a brace that reacted at the same volume would be one more
        /// thing to read at the worst moment.
        /// </para>
        /// </remarks>
        private static void BuildReactive(BraceDrawContext c)
        {
            Options.BuildResult result = Options.BuildStatus.Recent;

            if (result == Options.BuildResult.Succeeded)
            {
                Celebrate(c);
            }
            else if (result == Options.BuildResult.Failed)
            {
                Sulk(c);
            }
        }

        /// <summary>Three sparks thrown upward, once, then gone.</summary>
        private static void Celebrate(BraceDrawContext c)
        {
            double[] xs = { -0.34, 0.06, 0.40 };

            for (int i = 0; i < xs.Length; i++)
            {
                Ellipse spark = c.Dot(CelebrationGreen, xs[i], -0.10, 0.11);
                if (spark == null)
                {
                    continue;
                }

                var lift = new TranslateTransform();
                spark.RenderTransform = lift;

                var duration = new Duration(TimeSpan.FromSeconds(1.1 + (i * 0.13)));

                // No RepeatBehavior: this happens once per build, not forever. A brace that
                // kept celebrating would be celebrating during the next failure.
                c.Animate(lift, TranslateTransform.YProperty,
                    new DoubleAnimation(0, -c.Unit * 0.75, duration)
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    });

                c.Animate(spark, UIElement.OpacityProperty, new DoubleAnimation(1.0, 0.0, duration));
            }
        }

        /// <summary>Droops, greys, and stays that way until the reaction window closes.</summary>
        private static void Sulk(BraceDrawContext c)
        {
            c.ClipGlyphBand(SulkGrey, -0.15, 1.15);
            Apply(c, new RotateTransform(9));
            Apply(c, new TranslateTransform(0, c.Unit * 0.10));
        }

        /// <summary>
        /// Occasionally buds off a second brace, which drifts away and dissolves.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The copy comes from the factory rather than being drawn here, so it is genuinely the
        /// same glyph — a brace that also rolled a body trait divides into two question marks
        /// rather than into a question mark and a brace.
        /// </para>
        /// <para>
        /// Rare on purpose. The whole cycle is half a minute and the twin is invisible for
        /// nine tenths of it: mitosis that happened every second would be a brace with two
        /// heads, not a brace that occasionally divides. The phase is drawn from the identity
        /// so a screenful of them do not all divide in unison, which would read as a page
        /// transition rather than as cell division.
        /// </para>
        /// </remarks>
        private static void Mitosis(BraceDrawContext c)
        {
            FrameworkElement twin = c.CopyGlyph(c.Color);
            if (twin == null)
            {
                return;
            }

            twin.Opacity = 0;

            var drift = new TranslateTransform();
            twin.RenderTransform = drift;

            double angle = c.Roll(0x5D1177EUL) * Math.PI * 2;
            double reach = c.Unit * 0.85;
            var period = new Duration(TimeSpan.FromSeconds(29));

            // A long flat stretch, then a short division. Key times rather than a delay so the
            // whole thing is one repeating clock the view can pause with everything else.
            var fade = new DoubleAnimationUsingKeyFrames { Duration = period, RepeatBehavior = RepeatBehavior.Forever };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.00)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.86)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromPercent(0.90)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(1.00)));

            c.Animate(twin, UIElement.OpacityProperty, fade);
            c.Animate(drift, TranslateTransform.XProperty, Budding(period, Math.Cos(angle) * reach));
            c.Animate(drift, TranslateTransform.YProperty, Budding(period, Math.Sin(angle) * reach));
        }

        /// <summary>The twin's path: still, then pushed away, then back for the next division.</summary>
        private static DoubleAnimationUsingKeyFrames Budding(Duration period, double distance)
        {
            var move = new DoubleAnimationUsingKeyFrames { Duration = period, RepeatBehavior = RepeatBehavior.Forever };
            move.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.00)));
            move.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.86)));
            move.KeyFrames.Add(new EasingDoubleKeyFrame(distance, KeyTime.FromPercent(1.00))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });

            return move;
        }

        /// <summary>
        /// Sweating and unsteady: two beads running off the glyph, and a fast shake.
        /// </summary>
        /// <remarks>
        /// The only trait that can arrive by predicate as well as by roll — the complexity
        /// warning forces it onto anything nested past its threshold — so it has to read as
        /// <em>distress</em> rather than as one more costume. Nothing here changes the
        /// silhouette or reaches above the glyph, which is what keeps it distinguishable from
        /// a creature at six pixels across and stops it needing headroom.
        /// <para>
        /// The beads run downward and fade rather than pulsing in place. At this size a shape
        /// that grows and shrinks reads as a blinking indicator light, not as sweat.
        /// </para>
        /// </remarks>
        private static void Distressed(BraceDrawContext c)
        {
            Bead(c, c.Dot(SweatBead, 0.52, -0.04, 0.14), 0.40, 0.62);
            Bead(c, c.Dot(SweatBead, -0.50, 0.12, 0.11), 0.30, 0.83);

            // Small and fast. A wide, slow wobble reads as a personality; a nervous vibration
            // reads as a brace that is not coping, which is the point.
            var shake = new RotateTransform(0);
            Apply(c, shake);

            c.Animate(shake, RotateTransform.AngleProperty,
                new DoubleAnimation(-4.5, 4.5, new Duration(TimeSpan.FromSeconds(0.11)))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
        }

        /// <summary>Runs one bead down and fades it out, on repeat.</summary>
        /// <remarks>
        /// With motion disabled <see cref="BraceDrawContext.Animate"/> applies nothing, which
        /// leaves both beads sitting where they were drawn at full opacity. That is the
        /// intended still frame: the warning still reads without anything moving.
        /// </remarks>
        private static void Bead(BraceDrawContext c, Ellipse bead, double distanceUnits, double seconds)
        {
            if (bead == null)
            {
                return;
            }

            var fall = new TranslateTransform();
            bead.RenderTransform = fall;

            var duration = new Duration(TimeSpan.FromSeconds(seconds));

            c.Animate(fall, TranslateTransform.YProperty,
                new DoubleAnimation(0, distanceUnits * c.Unit * c.DecorScale, duration)
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                });

            c.Animate(bead, UIElement.OpacityProperty,
                new DoubleAnimation(0.95, 0.0, duration)
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                });
        }

        /// <summary>
        /// Flames licking up from the glyph, three tongues at different rates.
        /// </summary>
        /// <remarks>
        /// Layered darkest-to-brightest so the core reads even when the whole thing is only a
        /// few pixels across, and each tongue gets its own period so they never pulse in
        /// lockstep — synchronised flames read as a flashing light rather than as fire.
        /// </remarks>
        private static void Fire(BraceDrawContext c)
        {
            double[] xs = { -0.26, 0.02, 0.28 };
            Color[] colors = { FlameTip, FlameMid, FlameCore };
            double[] heights = { 0.62, 0.86, 0.54 };

            for (int i = 0; i < xs.Length; i++)
            {
                var tongue = c.Triangle(
                    colors[i],
                    xs[i] - 0.17, 0.10,
                    xs[i], -heights[i],
                    xs[i] + 0.17, 0.10);

                var scale = new ScaleTransform(1, 1);
                tongue.RenderTransformOrigin = new Point(0.5, 1.0);
                tongue.RenderTransform = scale;

                c.Animate(scale, ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(0.62, 1.15, new Duration(TimeSpan.FromSeconds(0.42 + (i * 0.17))))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                    });
            }
        }

        /// <summary>A fixed lean, stable per brace, so nothing is quite straight.</summary>
        private static void Tilted(BraceDrawContext c)
        {
            double angle = -16 + (c.Roll(0x7117EDUL) * 32);
            Apply(c, new RotateTransform(angle));
        }

        private static void Shadow(BraceDrawContext c)
        {
            c.ClipGlyphBand(Color.FromArgb(0x70, 0x00, 0x00, 0x00), -0.2, 1.3);
        }

        private static void Underline(BraceDrawContext c)
        {
            Color squiggle = Color.FromRgb(0xD1, 0x3B, 0x3B);
            c.Stroke(squiggle, 0.08,
                c.P(-0.40, 1.12), c.P(-0.20, 1.04), c.P(0.00, 1.12), c.P(0.20, 1.04), c.P(0.40, 1.12));
        }

        /// <summary>
        /// A vertical ramp through the stroke, from the identity colour to a darker shade.
        /// </summary>
        private static void GradientFill(BraceDrawContext c)
        {
            Color top = c.Color;
            var bottom = Color.FromRgb(
                (byte)(c.Color.R * 0.45),
                (byte)(c.Color.G * 0.45),
                (byte)(c.Color.B * 0.45));

            c.ClipGlyphBand(bottom, 0.58, 1.10);
            c.ClipGlyphBand(top, -0.10, 0.42);
        }

        /// <summary>
        /// A lean that grows through the session and resets when Visual Studio restarts.
        /// </summary>
        /// <remarks>
        /// Deliberately measured in minutes rather than hours. An effect keyed to wall-clock
        /// time is invisible during the sitting in which you enable it, which makes it
        /// impossible to tell whether it works.
        /// </remarks>
        private static void Drunk(BraceDrawContext c)
        {
            double minutes = SessionMinutes();
            double lean = Math.Min(24.0, minutes * 1.2);
            double direction = c.Roll(0xD204070UL) < 0.5 ? -1 : 1;
            Apply(c, new RotateTransform(lean * direction));
        }

        /// <summary>Sags toward the bottom of the cell as the session wears on.</summary>
        private static void Gravity(BraceDrawContext c)
        {
            double sag = Math.Min(c.Unit * 0.30, SessionMinutes() * 0.05 * c.Unit);
            Apply(c, new TranslateTransform(0, sag));
        }

        /// <summary>Droops and dims late in a session.</summary>
        private static void Nocturnal(BraceDrawContext c)
        {
            double tired = Math.Min(1.0, SessionMinutes() / 20.0);
            c.Canvas.Opacity = 1.0 - (tired * 0.45);
            Apply(c, new RotateTransform(tired * 10));
        }

        /// <summary>A seasonal accent: a pumpkin dot, a snowflake, a heart.</summary>
        private static void Seasonal(BraceDrawContext c)
        {
            int month = DateTime.Now.Month;
            Color accent;

            if (month == 10)
            {
                accent = Color.FromRgb(0xF2, 0x7A, 0x1A);
            }
            else if (month == 12 || month == 1)
            {
                accent = Color.FromRgb(0xCF, 0xE8, 0xF7);
            }
            else if (month == 2)
            {
                accent = Color.FromRgb(0xE8, 0x3B, 0x6B);
            }
            else
            {
                accent = Color.FromRgb(0x6B, 0xC4, 0x5A);
            }

            c.Dot(accent, 0.42, -0.16, 0.13);
        }

        private static readonly DateTime SessionStart = DateTime.UtcNow;

        private static double SessionMinutes()
        {
            return (DateTime.UtcNow - SessionStart).TotalMinutes;
        }

        /// <summary>
        /// Adds a transform without discarding one already present.
        /// </summary>
        /// <remarks>
        /// Effects roll independently, so two of them can land on the same brace. Assigning
        /// <c>RenderTransform</c> directly would mean the second silently erased the first.
        /// </remarks>
        private static void Apply(BraceDrawContext c, Transform transform)
        {
            c.Canvas.RenderTransformOrigin = new Point(0.5, 0.75);

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
    }
}
