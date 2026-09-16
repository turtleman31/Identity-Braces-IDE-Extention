using System;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments.Scenes
{
    /// <summary>
    /// Which traits are performed by the director rather than painted per brace.
    /// </summary>
    /// <remarks>
    /// The second half of the registry, for the handful of traits that do not fit the
    /// per-brace model. <see cref="TraitDrawing"/> answers "is there a painter for this?" and
    /// this answers "is there a scene for this?" — and something is implemented if either says
    /// yes.
    /// <para>
    /// It exists mostly so the options page can tell the truth. Without it, a trait with a
    /// scene but no painter would be labelled "catalogued, not yet drawn" while it was busy
    /// throwing tables around the editor.
    /// </para>
    /// </remarks>
    internal static class SceneCatalog
    {
        private static readonly string[] Performed =
        {
            TraitIds.TableFlip,
            TraitIds.SwapPlaces,
            TraitIds.FireBrigade,
        };

        /// <summary>A fresh set of scenes, for one director.</summary>
        /// <remarks>
        /// Instances rather than a shared array: a scene may reasonably hold state about the
        /// performance it is currently giving, and two views must not share it.
        /// </remarks>
        public static IScene[] Create()
        {
            return new IScene[]
            {
                new TableFlipScene(),
                new SwapPlacesScene(),
                new FireBrigadeScene(),
            };
        }

        public static bool Has(string traitId)
        {
            if (traitId == null)
            {
                return false;
            }

            for (int i = 0; i < Performed.Length; i++)
            {
                if (string.Equals(Performed[i], traitId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
