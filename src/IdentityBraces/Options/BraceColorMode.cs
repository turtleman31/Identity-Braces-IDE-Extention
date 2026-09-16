namespace IdentityBraces.Options
{
    /// <summary>How a brace picks its colour.</summary>
    public enum BraceColorMode
    {
        /// <summary>One of the 32 identity colours. The default, and the point of the extension.</summary>
        Palette = 0,

        /// <summary>
        /// A single colour that follows the editor theme: white on a dark background, black
        /// on a light one.
        /// </summary>
        /// <remarks>
        /// Keeps the personalities — the question marks, the ears, the stockings — while
        /// dropping the colour identity, for when the palette is too much but the creatures
        /// are not.
        /// </remarks>
        Monochrome = 1,

        /// <summary>
        /// Colour by nesting depth: every brace at the same level shares a colour, and a pair
        /// agrees with itself.
        /// </summary>
        /// <remarks>
        /// The one setting in the extension that makes code <em>easier</em> to read — this is
        /// what a rainbow-brace extension does, reachable from here for anyone who wants the
        /// creatures without the deliberate hostility of identity colour. It uses the same 32
        /// entries, indexed by depth rather than by hash, and wraps past 32 levels.
        /// </remarks>
        Depth = 2,
    }
}
