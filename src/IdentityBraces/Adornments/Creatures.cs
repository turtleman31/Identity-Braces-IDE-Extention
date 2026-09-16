using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Silhouettes built around the glyph.
    /// </summary>
    /// <remarks>
    /// Every shape is authored in ink units — 0 is the ink's top-centre, 1 unit is its height
    /// — so a creature drawn once holds its proportions at any font size or zoom.
    /// <para>
    /// Nothing here is finer than about 0.15 units. At Consolas 10pt one unit is 12&#160;px, so
    /// that is the 2&#160;px floor below which detail averages into its neighbours and turns to
    /// mush; it is the constraint that killed the first attempt at the cat.
    /// </para>
    /// </remarks>
    internal static class Creatures
    {
        private static readonly Color InnerEar = Color.FromRgb(0xF7, 0xAE, 0xD0);
        private static readonly Color Pale = Color.FromRgb(0xEF, 0xE9, 0xF6);
        private static readonly Color Dark = Color.FromRgb(0x23, 0x22, 0x2C);

        public static void Register(Dictionary<string, Action<BraceDrawContext>> map)
        {
            map[TraitIds.Catgirl] = Cat;
            map[TraitIds.Bunny] = Bunny;
            map[TraitIds.Devil] = Devil;
            map[TraitIds.Angel] = Angel;
            map[TraitIds.Fox] = Fox;
            map[TraitIds.Wolf] = Wolf;
            map[TraitIds.Frog] = Frog;
            map[TraitIds.Owl] = Owl;
            map[TraitIds.Mushroom] = Mushroom;
            map[TraitIds.Robot] = Robot;
            map[TraitIds.Bee] = Bee;
            map[TraitIds.Unicorn] = Unicorn;
            map[TraitIds.Vampire] = Vampire;
            map[TraitIds.Crab] = Crab;
            map[TraitIds.Bat] = Bat;
            map[TraitIds.Penguin] = Penguin;
            map[TraitIds.Cactus] = Cactus;
            map[TraitIds.Slime] = Slime;
            map[TraitIds.Snake] = Snake;
            map[TraitIds.Ghost] = Ghost;
            map[TraitIds.Wizard] = Wizard;
            map[TraitIds.Dragon] = Dragon;
            map[TraitIds.Spider] = Spider;
            map[TraitIds.Cthulhu] = Cthulhu;
            map[TraitIds.Pirate] = Pirate;
        }

        /// <summary>Two ears with a slanted base, so they read as ears rather than horns.</summary>
        private static void Cat(BraceDrawContext c)
        {
            c.Triangle(c.Color, -0.44, 0.01, -0.31, -0.83, -0.06, -0.16);
            c.Triangle(InnerEar, -0.33, -0.11, -0.26, -0.56, -0.13, -0.21);
            c.Triangle(c.Color, 0.44, 0.01, 0.31, -0.83, 0.06, -0.16);
            c.Triangle(InnerEar, 0.33, -0.11, 0.26, -0.56, 0.13, -0.21);
        }

        /// <summary>Tall and narrow, with the right ear folded at the tip.</summary>
        private static void Bunny(BraceDrawContext c)
        {
            c.Triangle(c.Color, -0.30, 0.02, -0.24, -1.15, -0.05, 0.00);
            c.Triangle(InnerEar, -0.24, -0.10, -0.21, -0.85, -0.12, -0.08);
            c.Triangle(c.Color, 0.30, 0.02, 0.20, -0.95, 0.05, 0.00);
            c.Stroke(c.Color, 0.13, c.P(0.20, -0.95), c.P(0.36, -1.08));
        }

        private static void Devil(BraceDrawContext c)
        {
            c.Curve(c.Color, 0.14, c.P(-0.34, 0.00), c.P(-0.40, -0.45), c.P(-0.30, -0.62), c.P(-0.16, -0.66));
            c.Curve(c.Color, 0.14, c.P(0.34, 0.00), c.P(0.40, -0.45), c.P(0.30, -0.62), c.P(0.16, -0.66));
            c.Curve(c.Color, 0.13, c.P(0.30, 0.98), c.P(0.62, 1.02), c.P(0.66, 0.72), c.P(0.50, 0.60));
            c.Triangle(c.Color, 0.40, 0.62, 0.62, 0.58, 0.48, 0.44);
        }

        private static void Angel(BraceDrawContext c)
        {
            c.Ring(Color.FromRgb(0xF5, 0xD9, 0x6B), 0.00, -0.62, 0.30, 0.11);
        }

        private static void Fox(BraceDrawContext c)
        {
            c.Triangle(c.Color, -0.42, 0.00, -0.36, -0.92, -0.04, -0.14);
            c.Triangle(Pale, -0.32, -0.10, -0.29, -0.62, -0.14, -0.18);
            c.Triangle(c.Color, 0.42, 0.00, 0.36, -0.92, 0.04, -0.14);
            c.Triangle(Pale, 0.32, -0.10, 0.29, -0.62, 0.14, -0.18);
            c.Curve(c.Color, 0.20, c.P(0.26, 1.00), c.P(0.70, 1.02), c.P(0.72, 0.62), c.P(0.46, 0.50));
        }

        private static void Wolf(BraceDrawContext c)
        {
            c.Triangle(c.Color, -0.46, 0.02, -0.44, -0.72, -0.10, -0.12);
            c.Triangle(c.Color, 0.46, 0.02, 0.44, -0.72, 0.10, -0.12);
            c.Dot(c.Color, 0.00, -0.20, 0.10);
        }

        private static void Frog(BraceDrawContext c)
        {
            c.Dot(c.Color, -0.24, -0.20, 0.20);
            c.Dot(c.Color, 0.24, -0.20, 0.20);
            c.Dot(Dark, -0.24, -0.20, 0.08);
            c.Dot(Dark, 0.24, -0.20, 0.08);
        }

        private static void Owl(BraceDrawContext c)
        {
            c.Dot(Pale, -0.22, 0.28, 0.24);
            c.Dot(Pale, 0.22, 0.28, 0.24);
            c.Dot(Dark, -0.22, 0.28, 0.11);
            c.Dot(Dark, 0.22, 0.28, 0.11);
            c.Triangle(Color.FromRgb(0xE8, 0xA5, 0x3C), -0.08, 0.44, 0.08, 0.44, 0.00, 0.62);
        }

        private static void Mushroom(BraceDrawContext c)
        {
            c.Box(Color.FromRgb(0xD1, 0x3B, 0x3B), -0.42, -0.44, 0.84, 0.34, 0.17);
            c.Dot(Pale, -0.18, -0.30, 0.09);
            c.Dot(Pale, 0.16, -0.24, 0.07);
        }

        private static void Robot(BraceDrawContext c)
        {
            c.Stroke(c.Color, 0.11, c.P(0.00, -0.10), c.P(0.00, -0.60));
            var bulb = c.Dot(Color.FromRgb(0xE0, 0x3B, 0x3B), 0.00, -0.70, 0.14);

            var blink = new DoubleAnimationUsingKeyFrames { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever };
            blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromPercent(0.0)));
            blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.2, KeyTime.FromPercent(0.5)));
            blink.Duration = new System.Windows.Duration(TimeSpan.FromSeconds(1.4));
            c.Animate(bulb, System.Windows.UIElement.OpacityProperty, blink);

            c.Box(c.Color, -0.50, 0.30, 0.14, 0.14);
            c.Box(c.Color, 0.36, 0.30, 0.14, 0.14);
        }

        private static void Bee(BraceDrawContext c)
        {
            c.Box(Color.FromRgb(0xE8, 0xC0, 0x30), -0.36, 0.34, 0.72, 0.14);
            c.Box(Color.FromRgb(0xE8, 0xC0, 0x30), -0.36, 0.62, 0.72, 0.14);
            c.Dot(Pale, -0.46, 0.16, 0.16);
            c.Dot(Pale, 0.46, 0.16, 0.16);
        }

        private static void Unicorn(BraceDrawContext c)
        {
            c.Triangle(Color.FromRgb(0xF5, 0xD9, 0x6B), -0.13, -0.02, 0.00, -0.92, 0.13, -0.02);
            c.Stroke(InnerEar, 0.09, c.P(-0.07, -0.28), c.P(0.07, -0.40));
            c.Stroke(InnerEar, 0.09, c.P(-0.04, -0.52), c.P(0.06, -0.62));
        }

        private static void Vampire(BraceDrawContext c)
        {
            c.Triangle(Pale, -0.24, 0.86, -0.10, 0.86, -0.17, 1.14);
            c.Triangle(Pale, 0.10, 0.86, 0.24, 0.86, 0.17, 1.14);
        }

        private static void Crab(BraceDrawContext c)
        {
            c.Stroke(c.Color, 0.14, c.P(-0.36, 0.50), c.P(-0.62, 0.36));
            c.Triangle(c.Color, -0.62, 0.44, -0.86, 0.26, -0.60, 0.22);
            c.Stroke(c.Color, 0.14, c.P(0.36, 0.50), c.P(0.62, 0.36));
            c.Triangle(c.Color, 0.62, 0.44, 0.86, 0.26, 0.60, 0.22);
        }

        private static void Bat(BraceDrawContext c)
        {
            c.Triangle(c.Color, -0.30, 0.20, -0.86, 0.06, -0.62, 0.52);
            c.Triangle(c.Color, 0.30, 0.20, 0.86, 0.06, 0.62, 0.52);
        }

        private static void Penguin(BraceDrawContext c)
        {
            c.Triangle(Color.FromRgb(0xE8, 0xA5, 0x3C), -0.10, 0.26, 0.10, 0.26, 0.00, 0.46);
            c.Triangle(c.Color, -0.34, 0.52, -0.60, 0.74, -0.32, 0.80);
            c.Triangle(c.Color, 0.34, 0.52, 0.60, 0.74, 0.32, 0.80);
        }

        private static void Cactus(BraceDrawContext c)
        {
            for (int i = 0; i < 3; i++)
            {
                double y = 0.16 + (i * 0.28);
                c.Stroke(c.Color, 0.09, c.P(-0.34, y), c.P(-0.52, y - 0.08));
                c.Stroke(c.Color, 0.09, c.P(0.34, y), c.P(0.52, y - 0.08));
            }

            c.Dot(Color.FromRgb(0xE8, 0x5C, 0x9A), 0.00, -0.16, 0.13);
        }

        private static void Slime(BraceDrawContext c)
        {
            var drip = c.Dot(c.Color, 0.10, 1.02, 0.13);
            var fall = new DoubleAnimation(0, c.Unit * 0.9, new System.Windows.Duration(TimeSpan.FromSeconds(2.2)))
            {
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
            };

            var move = new TranslateTransform();
            drip.RenderTransform = move;
            c.Animate(move, TranslateTransform.YProperty, fall);

            var fade = new DoubleAnimation(1, 0, new System.Windows.Duration(TimeSpan.FromSeconds(2.2)))
            {
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
            };
            c.Animate(drip, System.Windows.UIElement.OpacityProperty, fade);
        }

        private static void Snake(BraceDrawContext c)
        {
            var tongue = c.Stroke(Color.FromRgb(0xE0, 0x3B, 0x6B), 0.09, c.P(0.30, 0.44), c.P(0.62, 0.44));
            c.Triangle(Color.FromRgb(0xE0, 0x3B, 0x6B), 0.62, 0.38, 0.78, 0.32, 0.62, 0.50);

            if (tongue != null)
            {
                var flick = new DoubleAnimation(1, 0.1, new System.Windows.Duration(TimeSpan.FromSeconds(0.9)))
                {
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                };
                c.Animate(tongue, System.Windows.UIElement.OpacityProperty, flick);
            }
        }

        private static void Ghost(BraceDrawContext c)
        {
            if (c.GlyphElement != null)
            {
                c.GlyphElement.Opacity = 0.55;
            }

            c.Dot(c.Color, -0.20, 1.02, 0.10);
            c.Dot(c.Color, 0.06, 1.06, 0.10);
            c.Dot(c.Color, 0.30, 1.02, 0.10);
        }

        private static void Wizard(BraceDrawContext c)
        {
            c.Triangle(Color.FromRgb(0x5A, 0x3F, 0xA8), -0.44, -0.14, 0.00, -1.24, 0.44, -0.14);
            c.Box(Color.FromRgb(0x5A, 0x3F, 0xA8), -0.56, -0.20, 1.12, 0.13, 0.06);
            c.Dot(Color.FromRgb(0xF5, 0xD9, 0x6B), 0.06, -0.72, 0.10);
        }

        private static void Dragon(BraceDrawContext c)
        {
            c.Curve(c.Color, 0.13, c.P(-0.34, 0.00), c.P(-0.52, -0.34), c.P(-0.36, -0.62), c.P(-0.10, -0.58));
            c.Curve(c.Color, 0.13, c.P(0.34, 0.00), c.P(0.52, -0.34), c.P(0.36, -0.62), c.P(0.10, -0.58));
            c.Triangle(c.Color, 0.30, 0.30, 0.80, 0.12, 0.66, 0.62);
        }

        private static void Spider(BraceDrawContext c)
        {
            for (int i = 0; i < 3; i++)
            {
                double y = 0.24 + (i * 0.26);
                c.Stroke(c.Color, 0.08, c.P(-0.30, y), c.P(-0.72, y - 0.16));
                c.Stroke(c.Color, 0.08, c.P(0.30, y), c.P(0.72, y - 0.16));
            }
        }

        private static void Cthulhu(BraceDrawContext c)
        {
            for (int i = 0; i < 4; i++)
            {
                double x = -0.30 + (i * 0.20);
                var arm = c.Curve(c.Color, 0.10, c.P(x, 0.90), c.P(x - 0.06, 1.10), c.P(x + 0.08, 1.20), c.P(x, 1.34));

                if (arm == null)
                {
                    continue;
                }

                var sway = new DoubleAnimation(-6, 6, new System.Windows.Duration(TimeSpan.FromSeconds(1.8 + (i * 0.2))))
                {
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                };

                var rotate = new RotateTransform();
                arm.RenderTransformOrigin = new System.Windows.Point(0.5, 0);
                arm.RenderTransform = rotate;
                c.Animate(rotate, RotateTransform.AngleProperty, sway);
            }
        }

        private static void Pirate(BraceDrawContext c)
        {
            c.Box(Dark, -0.46, 0.18, 0.92, 0.15, 0.04);
            c.Stroke(Dark, 0.07, c.P(-0.46, 0.18), c.P(-0.62, 0.06));
            c.Dot(Color.FromRgb(0xD1, 0x3B, 0x3B), 0.00, -0.10, 0.12);
        }
    }
}
