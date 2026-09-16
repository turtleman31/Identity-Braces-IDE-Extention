using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>Everything <see cref="BraceScanner"/> needs, with no reference to Visual Studio.</summary>
    internal struct ScanSettings
    {
        public bool Curly;
        public bool Round;
        public bool Square;

        /// <summary>
        /// Every trait and how often it comes up. Supplied by the settings layer so the
        /// scanner stays free of both the catalogue and Visual Studio.
        /// </summary>
        public IList<TraitWeight> TraitWeights;

        /// <summary>
        /// When true, a brace with no partner always renders as '?'. A brace that has lost
        /// its other half genuinely does not know who it is, and it doubles as a syntax hint.
        /// </summary>
        public bool QuestionUnmatched;

        /// <summary>
        /// When true, a closing brace gets its own identity instead of inheriting its
        /// opener's — so <c>{</c> and its <c>}</c> are different colours, and may be
        /// different creatures entirely.
        /// </summary>
        public bool IndependentBraces;

        /// <summary>
        /// Colour by nesting depth instead of by identity — the one setting here that makes
        /// code <em>easier</em> to read.
        /// </summary>
        /// <remarks>
        /// Resolved in the scanner rather than at either consumer, because the classifier and
        /// the adornment layer must never disagree about a brace's colour: a personality brace
        /// a different colour from the plain brace beside it looks like a bug in the palette.
        /// One value, one place, both paths.
        /// </remarks>
        public bool ColorByDepth;

        /// <summary>
        /// Braces nested at least this deep are visibly distressed. Zero switches it off.
        /// </summary>
        /// <remarks>
        /// A trait whose roll is replaced by a predicate. It is forced on regardless of the
        /// weight table, because it is a warning rather than a costume — a brace that only
        /// sweats four times in a hundred is not telling you anything.
        /// </remarks>
        public int ComplexityWarningDepth;

        public static ScanSettings Default
        {
            get
            {
                return new ScanSettings
                {
                    Curly = true,
                    Round = true,
                    Square = true,
                    TraitWeights = DefaultWeights(),
                    QuestionUnmatched = true,
                    IndependentBraces = true,
                    ColorByDepth = false,
                    ComplexityWarningDepth = 0,
                };
            }
        }

        /// <summary>The catalogue's own defaults, for tests and for a first run.</summary>
        public static IList<TraitWeight> DefaultWeights()
        {
            var weights = new List<TraitWeight>(TraitCatalog.All.Count);
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];
                weights.Add(new TraitWeight
                {
                    Id = info.Id,
                    Layer = info.Layer,
                    Percent = info.DefaultPercent,
                });
            }

            return weights;
        }

        public bool Includes(BraceKind kind)
        {
            switch (kind)
            {
                case BraceKind.Curly: return Curly;
                case BraceKind.Round: return Round;
                case BraceKind.Square: return Square;
                default: return false;
            }
        }
    }
}
