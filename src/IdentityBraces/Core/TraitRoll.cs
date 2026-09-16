using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>One trait's identity and how often it should come up.</summary>
    internal struct TraitWeight
    {
        public string Id;
        public TraitLayer Layer;
        public int Percent;
    }

    /// <summary>What a single brace turned out to be.</summary>
    internal struct BraceTraits
    {
        public string Body;
        public string Creature;
        public string Costume;
        public string Motion;
        public string[] Effects;

        /// <summary>
        /// True when anything at all was rolled, so the classifier must paint the real glyph
        /// transparent and let the adornment layer draw it instead.
        /// </summary>
        public bool IsDrawn;
    }

    /// <summary>
    /// Turns a brace's identity hash into its traits.
    /// </summary>
    /// <remarks>
    /// Deliberately free of Visual Studio and WPF: the tagger needs to know whether a brace
    /// is drawn (so it can hide the real one) and the adornment layer needs to know what to
    /// draw, and those two must never disagree. Keeping the roll pure means both call the
    /// same function and get the same answer, and it stays testable on a bare runtime.
    /// </remarks>
    internal static class TraitRoll
    {
        // One salt per layer. Without these the layers would correlate — every brace with a
        // wizard hat would also have the same motion, because both would read the same bits.
        private const ulong BodySalt = 0xB0D1E5A17C0FFEEUL;
        private const ulong CreatureSalt = 0xC8EA7C0DE1DEA5UL;
        private const ulong CostumeSalt = 0xC05715E5A1701UL;
        private const ulong MotionSalt = 0x0713C0DE5EED17UL;
        private const ulong EffectSalt = 0xEFEC7B0DE5A1EDUL;

        public static BraceTraits Roll(ulong identity, TraitTable table, bool isUnmatched)
        {
            return Roll(identity, table, isUnmatched, false);
        }

        /// <param name="isDistressed">
        /// Set by the complexity warning, which replaces this one trait's roll with a
        /// predicate on nesting depth. It is forced on regardless of its weight: a warning
        /// that only fires on four braces in a hundred is not a warning.
        /// </param>
        public static BraceTraits Roll(ulong identity, TraitTable table, bool isUnmatched, bool isDistressed)
        {
            var traits = new BraceTraits
            {
                Body = table.PickOne(TraitLayer.Body, Roll100(identity, BodySalt)),
                Creature = table.PickOne(TraitLayer.Creature, Roll100(identity, CreatureSalt)),
                Costume = table.PickOne(TraitLayer.Costume, Roll100(identity, CostumeSalt)),
                Motion = table.PickOne(TraitLayer.Motion, Roll100(identity, MotionSalt)),
                Effects = PickEffects(identity, table),
            };

            // A brace with no partner overrides whatever body it rolled: it genuinely does not
            // know what it is, and it doubles as a syntax hint.
            if (isUnmatched)
            {
                traits.Body = TraitIds.Question;
            }

            if (isDistressed)
            {
                traits.Effects = WithDistress(traits.Effects);
            }

            traits.IsDrawn = traits.Body != null
                || traits.Creature != null
                || traits.Costume != null
                || traits.Motion != null
                || traits.Effects.Length > 0;

            return traits;
        }

        private static double Roll100(ulong identity, ulong salt)
        {
            return Hash.ToUnitInterval(Hash.Mix(identity, salt)) * 100.0;
        }

        /// <summary>
        /// Rolls every effect independently, so a brace can be on fire and tilted and carry a
        /// shadow all at once.
        /// </summary>
        private static string[] PickEffects(ulong identity, TraitTable table)
        {
            int count = table.EffectCount;
            if (count == 0)
            {
                return EmptyEffects;
            }

            List<string> chosen = null;
            ulong stream = EffectSalt;

            for (int i = 0; i < count; i++)
            {
                // Advance the stream per effect so each gets its own independent roll.
                stream = Hash.Mix(stream, 0x9E3779B97F4A7C15UL);

                if (Roll100(identity, stream) < table.EffectChance(i))
                {
                    if (chosen == null)
                    {
                        chosen = new List<string>(2);
                    }

                    chosen.Add(table.EffectAt(i));
                }
            }

            return chosen == null ? EmptyEffects : chosen.ToArray();
        }

        /// <summary>
        /// Appends the distress effect, leaving an already-rolled one alone.
        /// </summary>
        /// <remarks>
        /// Copies rather than appends in place: the empty case is a shared singleton, and a
        /// brace that also rolled distress honestly must not end up carrying it twice.
        /// </remarks>
        private static string[] WithDistress(string[] effects)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                if (string.Equals(effects[i], TraitIds.Distressed, System.StringComparison.Ordinal))
                {
                    return effects;
                }
            }

            var grown = new string[effects.Length + 1];
            System.Array.Copy(effects, grown, effects.Length);
            grown[effects.Length] = TraitIds.Distressed;
            return grown;
        }

        private static readonly string[] EmptyEffects = new string[0];
    }
}
