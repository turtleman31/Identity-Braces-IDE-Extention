using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>
    /// Generates representative braces for a preview, without a buffer to scan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Split out of the options page so it can be tested. The page's job is drawing; deciding
    /// <em>what</em> to draw is arithmetic on the same weight table the scanner uses, and a
    /// preview that disagrees with the scanner would be worse than no preview — it would be
    /// confidently wrong about a catalogue nobody can otherwise inspect.
    /// </para>
    /// <para>
    /// So this rolls through <see cref="TraitRoll"/> exactly as <see cref="BraceScanner"/>
    /// does. The only thing it invents is the identities, which real braces get from their
    /// declaring text.
    /// </para>
    /// </remarks>
    internal static class TraitSampler
    {
        /// <summary>
        /// Fixed, so a sample set does not reshuffle every time a weight changes.
        /// </summary>
        /// <remarks>
        /// This matters more than it sounds. With a fresh seed per render, nudging one slider
        /// would redraw a completely different set of braces and you could not tell what your
        /// change did. Holding the identities still means only the traits that actually depend
        /// on the moved weight change.
        /// </remarks>
        public const ulong Seed = 0x9E3779B97F4A7C15UL;

        /// <summary>The deepest level <see cref="DepthOf"/> descends to before coming back out.</summary>
        public const int DeepestSample = 5;

        public struct Sample
        {
            public ulong Identity;

            /// <summary>A plausible nesting level, for previewing the depth colour mode.</summary>
            public int Depth;

            public BraceTraits Traits;
        }

        /// <summary>Rolls <paramref name="count"/> braces against <paramref name="weights"/>.</summary>
        public static Sample[] Take(IList<TraitWeight> weights, int count)
        {
            if (count < 0)
            {
                count = 0;
            }

            var table = new TraitTable(weights);
            var samples = new Sample[count];

            for (int i = 0; i < count; i++)
            {
                ulong identity = Hash.Mix(Seed, (ulong)i);

                samples[i] = new Sample
                {
                    Identity = identity,
                    Depth = DepthOf(i),
                    Traits = TraitRoll.Roll(identity, table, false),
                };
            }

            return samples;
        }

        /// <summary>
        /// A nesting profile that descends and comes back out, so a depth-coloured preview
        /// shows a ramp rather than a flat run of one colour.
        /// </summary>
        public static int DepthOf(int index)
        {
            if (index < 0)
            {
                index = -index;
            }

            int period = DeepestSample * 2;
            int phase = index % period;
            return phase <= DeepestSample ? phase : period - phase;
        }

        /// <summary>
        /// A brace wearing exactly one trait, whatever that trait's weight is.
        /// </summary>
        /// <remarks>
        /// Built rather than rolled, because the question it answers is "what <em>is</em> a
        /// cthulhu brace" — asked before deciding whether to enable it. Rolling would only show
        /// it once the weight was already turned up, which is the loop the preview exists to
        /// break.
        /// </remarks>
        public static BraceTraits Single(TraitLayer layer, string id)
        {
            var traits = new BraceTraits { Effects = EmptyEffects };

            switch (layer)
            {
                case TraitLayer.Body:
                    traits.Body = id;
                    break;

                case TraitLayer.Creature:
                    traits.Creature = id;
                    break;

                case TraitLayer.Costume:
                    traits.Costume = id;
                    break;

                case TraitLayer.Motion:
                    traits.Motion = id;
                    break;

                default:
                    traits.Effects = new[] { id };
                    break;
            }

            // Forced, not derived. An unrecognised layer would otherwise produce a brace the
            // factory declines to draw, and the preview would silently show an empty cell for
            // a trait that is perfectly real.
            traits.IsDrawn = true;
            return traits;
        }

        private static readonly string[] EmptyEffects = new string[0];
    }
}
