using System;
using IdentityBraces.Adornments.Scenes;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Whether anything at all realises a trait, wherever that happens to live.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Most traits are a painter in <see cref="TraitDrawing"/>. A few are performances in
    /// <see cref="SceneCatalog"/>. The rest cannot be either, because what they do is not
    /// drawing: a brace that edges away from the caret changes its <em>placement</em>, which
    /// only the adornment manager may write, and a brace with a name puts it in the editor's
    /// quick-info rather than on the glyph.
    /// </para>
    /// <para>
    /// This exists so the options page can tell the truth about all three. It is a list rather
    /// than something derived, which means it has to be updated by hand when a trait is
    /// implemented outside the two registries — but that is three entries against eighty-six,
    /// and the alternative is a registration ceremony for traits that register nothing.
    /// </para>
    /// </remarks>
    internal static class TraitImplementation
    {
        /// <summary>
        /// Traits realised by something that is not a painter and not a scene.
        /// </summary>
        private static readonly string[] Elsewhere =
        {
            // Placement, applied by AdornmentManager: the render transform is contested by
            // every motion trait, so fleeing is expressed as an offset to the character cell.
            TraitIds.FleeCursor,

            // Strength, applied by AdornmentManager alongside the scope spotlight, because the
            // two share a channel and have to be resolved into one number.
            TraitIds.StageFright,

            // The editor's quick-info, because making a brace adornment hit-testable would have
            // it swallow the clicks that place the caret.
            TraitIds.Named,
        };

        public static bool IsImplemented(string traitId)
        {
            if (traitId == null)
            {
                return false;
            }

            if (TraitDrawing.Has(traitId) || SceneCatalog.Has(traitId))
            {
                return true;
            }

            for (int i = 0; i < Elsewhere.Length; i++)
            {
                if (string.Equals(Elsewhere[i], traitId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
