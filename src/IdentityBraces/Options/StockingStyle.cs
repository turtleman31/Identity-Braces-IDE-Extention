namespace IdentityBraces.Options
{
    /// <summary>
    /// How much stripe detail a catgirl brace's thigh highs carry.
    /// </summary>
    /// <remarks>
    /// The variants differ only in a table of horizontal bands, so all of them ship and the
    /// choice is a setting rather than a rebuild.
    /// <para>
    /// The stripes are clipped copies of the brace's own stroke, never a shape drawn behind
    /// it — that is what keeps a stocking exactly as wide as the glyph it clothes.
    /// </para>
    /// </remarks>
    public enum StockingStyle
    {
        /// <summary>No thigh highs. Ears only.</summary>
        Off = 0,

        /// <summary>The lower stroke recoloured, no welt. Nothing under 5&#160;px; clearest at small sizes.</summary>
        TwoTone = 1,

        /// <summary>One welt stripe at the top of the stocking, held at the 2&#160;px floor. The default.</summary>
        Garter = 2,

        /// <summary>
        /// The proper two-stripe welt. Correct at larger font sizes; at 10&#160;pt the stripes
        /// fall to 1.3&#160;px and begin to average back into one band.
        /// </summary>
        Banded = 3,
    }
}
