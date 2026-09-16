using System;
using System.Collections.Generic;
using System.Windows.Media;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Things worn over the glyph rather than replacing it.
    /// </summary>
    /// <remarks>
    /// The thigh highs are the exception worth reading: they are clipped copies of the brace's
    /// own stroke, not a shape drawn behind it. Nothing is painted under the text, so a
    /// stocking is exactly as wide as the stroke it clothes at every font size. Drawing a
    /// rectangle instead once meant an 8&#160;px slab behind a 6&#160;px stroke, which read as
    /// a bar with a brace lost inside it.
    /// </remarks>
    internal static class Costumes
    {
        private static readonly Color StockingBody = Color.FromRgb(0x7B, 0x74, 0x90);
        private static readonly Color StockingWelt = Color.FromRgb(0xC6, 0xBF, 0xD4);
        private static readonly Color Dark = Color.FromRgb(0x23, 0x22, 0x2C);
        private static readonly Color Pale = Color.FromRgb(0xEF, 0xE9, 0xF6);
        private static readonly Color Gold = Color.FromRgb(0xF5, 0xD9, 0x6B);

        public static void Register(Dictionary<string, Action<BraceDrawContext>> map)
        {
            map[TraitIds.ThighHighs] = ThighHighs;
            map[TraitIds.TopHat] = TopHat;
            map[TraitIds.Crown] = Crown;
            map[TraitIds.Sunglasses] = Sunglasses;
            map[TraitIds.Scarf] = Scarf;
            map[TraitIds.Bowtie] = Bowtie;
            map[TraitIds.PartyHat] = PartyHat;
            map[TraitIds.Moustache] = Moustache;
            map[TraitIds.Beanie] = Beanie;
            map[TraitIds.FlowerCrown] = FlowerCrown;
            map[TraitIds.Headphones] = Headphones;
            map[TraitIds.Bandage] = Bandage;
            map[TraitIds.Necktie] = Necktie;
            map[TraitIds.Bowtie] = Bowtie;
            map[TraitIds.Wings] = Wings;
            map[TraitIds.Armour] = Armour;
            map[TraitIds.Cape] = Cape;
            map[TraitIds.Backpack] = Backpack;
            map[TraitIds.Monocle] = Monocle;
        }

        /// <summary>
        /// The lower stroke recoloured, with a welt stripe at the top of the stocking.
        /// </summary>
        /// <remarks>
        /// Clipped copies of the glyph, so the stocking can never be wider than the stroke.
        /// The band at the top is what makes a two-tone leg read as clothing rather than as a
        /// gradient or a rendering fault.
        /// </remarks>
        private static void ThighHighs(BraceDrawContext c)
        {
            double legTop = 0.55;
            double welt = Math.Max(2.0 / Math.Max(c.Unit, 1.0), 0.17);

            c.ClipGlyphBand(StockingBody, legTop, 1.05);
            c.ClipGlyphBand(StockingWelt, legTop, legTop + welt);
        }

        private static void TopHat(BraceDrawContext c)
        {
            c.Box(Dark, -0.56, -0.22, 1.12, 0.12, 0.05);
            c.Box(Dark, -0.34, -0.78, 0.68, 0.58, 0.04);
            c.Box(Color.FromRgb(0xD1, 0x3B, 0x3B), -0.34, -0.36, 0.68, 0.12);
        }

        private static void Crown(BraceDrawContext c)
        {
            c.Triangle(Gold, -0.40, -0.06, -0.30, -0.52, -0.14, -0.06);
            c.Triangle(Gold, -0.14, -0.06, 0.00, -0.62, 0.14, -0.06);
            c.Triangle(Gold, 0.14, -0.06, 0.30, -0.52, 0.40, -0.06);
            c.Box(Gold, -0.42, -0.10, 0.84, 0.12);
        }

        private static void Sunglasses(BraceDrawContext c)
        {
            c.Box(Dark, -0.50, 0.20, 0.40, 0.20, 0.05);
            c.Box(Dark, 0.10, 0.20, 0.40, 0.20, 0.05);
            c.Box(Dark, -0.12, 0.26, 0.24, 0.06);
        }

        private static void Scarf(BraceDrawContext c)
        {
            Color wool = Color.FromRgb(0xD1, 0x3B, 0x5A);
            c.Box(wool, -0.44, 0.46, 0.88, 0.16, 0.04);
            c.Curve(wool, 0.14, c.P(0.36, 0.54), c.P(0.66, 0.66), c.P(0.62, 0.90), c.P(0.44, 1.00));
        }

        private static void Bowtie(BraceDrawContext c)
        {
            Color silk = Color.FromRgb(0xD1, 0x3B, 0x3B);
            c.Triangle(silk, -0.44, 0.34, -0.44, 0.70, -0.06, 0.52);
            c.Triangle(silk, 0.44, 0.34, 0.44, 0.70, 0.06, 0.52);
            c.Dot(silk, 0.00, 0.52, 0.09);
        }

        private static void PartyHat(BraceDrawContext c)
        {
            c.Triangle(Color.FromRgb(0xE0, 0x3B, 0x9A), -0.34, -0.10, 0.00, -0.94, 0.34, -0.10);
            c.Box(Gold, -0.24, -0.44, 0.48, 0.09);
            c.Dot(Pale, 0.00, -0.98, 0.11);
        }

        private static void Moustache(BraceDrawContext c)
        {
            c.Curve(Dark, 0.16, c.P(-0.44, 0.48), c.P(-0.22, 0.34), c.P(-0.06, 0.50), c.P(0.00, 0.52));
            c.Curve(Dark, 0.16, c.P(0.44, 0.48), c.P(0.22, 0.34), c.P(0.06, 0.50), c.P(0.00, 0.52));
        }

        private static void Beanie(BraceDrawContext c)
        {
            Color wool = Color.FromRgb(0x3B, 0x7A, 0xD1);
            c.Box(wool, -0.44, -0.58, 0.88, 0.44, 0.20);
            c.Box(Pale, -0.48, -0.20, 0.96, 0.13, 0.05);
            c.Dot(Pale, 0.00, -0.66, 0.13);
        }

        private static void FlowerCrown(BraceDrawContext c)
        {
            c.Dot(Color.FromRgb(0xE8, 0x5C, 0x9A), -0.28, -0.14, 0.13);
            c.Dot(Color.FromRgb(0xF5, 0xD9, 0x6B), 0.00, -0.22, 0.13);
            c.Dot(Color.FromRgb(0x7B, 0xC4, 0xE8), 0.28, -0.14, 0.13);
        }

        private static void Headphones(BraceDrawContext c)
        {
            c.Curve(Dark, 0.12, c.P(-0.48, 0.18), c.P(-0.44, -0.42), c.P(0.44, -0.42), c.P(0.48, 0.18));
            c.Box(Dark, -0.58, 0.12, 0.20, 0.30, 0.07);
            c.Box(Dark, 0.38, 0.12, 0.20, 0.30, 0.07);
        }

        private static void Bandage(BraceDrawContext c)
        {
            Color tape = Color.FromRgb(0xE8, 0xC9, 0xA0);
            c.Stroke(tape, 0.20, c.P(-0.38, 0.24), c.P(0.38, 0.66));
            c.Stroke(tape, 0.20, c.P(-0.38, 0.66), c.P(0.38, 0.24));
        }

        private static void Necktie(BraceDrawContext c)
        {
            Color silk = Color.FromRgb(0x2E, 0x5C, 0xA8);
            c.Triangle(silk, -0.14, 0.36, 0.14, 0.36, 0.00, 0.52);
            c.Triangle(silk, -0.13, 0.54, 0.13, 0.54, 0.00, 1.02);
        }

        private static void Wings(BraceDrawContext c)
        {
            c.Triangle(Pale, -0.30, 0.24, -0.82, 0.02, -0.60, 0.56);
            c.Triangle(Pale, 0.30, 0.24, 0.82, 0.02, 0.60, 0.56);
        }

        private static void Armour(BraceDrawContext c)
        {
            Color steel = Color.FromRgb(0x8E, 0x96, 0xA6);
            c.Box(steel, -0.42, 0.40, 0.84, 0.26, 0.06);
            c.Dot(Pale, 0.00, 0.53, 0.07);
        }

        private static void Cape(BraceDrawContext c)
        {
            c.Triangle(Color.FromRgb(0x8A, 0x1F, 0x3C), -0.30, 0.16, 0.30, 0.16, 0.00, 1.20);
        }

        private static void Backpack(BraceDrawContext c)
        {
            Color canvas = Color.FromRgb(0x5A, 0x7A, 0x4A);
            c.Box(canvas, 0.30, 0.30, 0.40, 0.50, 0.10);
            c.Stroke(canvas, 0.08, c.P(0.30, 0.38), c.P(0.06, 0.44));
        }

        private static void Monocle(BraceDrawContext c)
        {
            c.Ring(Gold, 0.26, 0.30, 0.24, 0.09);
            c.Stroke(Gold, 0.07, c.P(0.30, 0.52), c.P(0.36, 0.94));
        }
    }
}
