using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>A named set of trait weights.</summary>
    internal sealed class TraitPreset
    {
        public string Id;
        public string Name;
        public string Description;

        /// <summary>Explicit weights. Anything not listed is zero.</summary>
        public Dictionary<string, int> Weights;
    }

    /// <summary>
    /// Bundles that set every weight at once.
    /// </summary>
    /// <remarks>
    /// Eighty-five individual percentages is not a settings page, it is a spreadsheet. A
    /// preset is how someone gets a coherent look without reading the whole catalogue, and
    /// picking one is still just a write to the same weight table — so a preset can be
    /// applied and then adjusted, rather than being a mode you are locked into.
    /// </remarks>
    internal static class TraitPresets
    {
        public static readonly IList<TraitPreset> All = new List<TraitPreset>
        {
            new TraitPreset
            {
                Id = "off",
                Name = "Off",
                Description = "Colours only. Every brace keeps its own glyph.",
                Weights = new Dictionary<string, int>(),
            },

            new TraitPreset
            {
                Id = "default",
                Name = "Default",
                Description = "What the extension shipped with: a few questions, some colour cycling, the occasional cat.",
                Weights = new Dictionary<string, int>
                {
                    { TraitIds.Question, 6 },
                    { TraitIds.ColourCycle, 8 },
                    { TraitIds.Catgirl, 4 },
                    { TraitIds.ThighHighs, 4 },
                },
            },

            new TraitPreset
            {
                Id = "menagerie",
                Name = "Menagerie",
                Description = "Creatures and costumes across the board, but nothing that moves much.",
                Weights = BuildMenagerie(),
            },

            new TraitPreset
            {
                Id = "restless",
                Name = "Restless",
                Description = "Everything on the motion layer, spread evenly. Nothing on screen holds still.",
                Weights = BuildLayerSpread(TraitLayer.Motion, 90),
            },

            new TraitPreset
            {
                Id = "unusable",
                Name = "Unusable",
                Description = "Every layer saturated. This is the setting the extension was designed for.",
                Weights = BuildUnusable(),
            },
        };

        public static TraitPreset Find(string id)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }

        private static Dictionary<string, int> BuildMenagerie()
        {
            var weights = Merge(
                BuildLayerSpread(TraitLayer.Creature, 70),
                BuildLayerSpread(TraitLayer.Costume, 55));

            weights[TraitIds.Question] = 5;
            weights[TraitIds.ColourCycle] = 10;
            return weights;
        }

        private static Dictionary<string, int> BuildUnusable()
        {
            var weights = Merge(
                BuildLayerSpread(TraitLayer.Creature, 85),
                BuildLayerSpread(TraitLayer.Costume, 85),
                BuildLayerSpread(TraitLayer.Motion, 85),
                BuildLayerSpread(TraitLayer.Body, 45));

            // Effects roll independently rather than sharing a band, so they are set per
            // trait rather than spread across a budget.
            weights[TraitIds.Fire] = 8;
            weights[TraitIds.Tilted] = 25;
            weights[TraitIds.Shadow] = 20;
            weights[TraitIds.GradientFill] = 20;
            weights[TraitIds.BoldItalic] = 20;
            weights[TraitIds.Underline] = 10;
            weights[TraitIds.Distressed] = 12;
            return weights;
        }

        /// <summary>
        /// Divides a budget evenly across every trait on one layer.
        /// </summary>
        /// <remarks>
        /// Layers other than Effect share a single 0-100 roll, so their weights have to sum to
        /// the budget rather than each being an independent chance. Spreading rather than
        /// setting each to the budget is what stops the first trait in the list swallowing
        /// every brace.
        /// </remarks>
        private static Dictionary<string, int> BuildLayerSpread(TraitLayer layer, int budget)
        {
            var members = new List<TraitInfo>();
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                if (TraitCatalog.All[i].Layer == layer)
                {
                    members.Add(TraitCatalog.All[i]);
                }
            }

            var weights = new Dictionary<string, int>();
            if (members.Count == 0)
            {
                return weights;
            }

            int each = budget / members.Count;
            if (each < 1)
            {
                each = 1;
            }

            for (int i = 0; i < members.Count; i++)
            {
                weights[members[i].Id] = each;
            }

            return weights;
        }

        private static Dictionary<string, int> Merge(params Dictionary<string, int>[] parts)
        {
            var merged = new Dictionary<string, int>();
            for (int i = 0; i < parts.Length; i++)
            {
                foreach (KeyValuePair<string, int> pair in parts[i])
                {
                    merged[pair.Key] = pair.Value;
                }
            }

            return merged;
        }
    }
}
