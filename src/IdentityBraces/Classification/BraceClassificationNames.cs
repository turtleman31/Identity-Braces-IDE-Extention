using System.Globalization;

namespace IdentityBraces.Classification
{
    /// <summary>
    /// The classification type names, which must exist as compile-time constants because
    /// MEF attributes take no runtime values. That constraint is exactly why the palette is
    /// a fixed 32 rather than unbounded: format definitions are composed once at start-up.
    /// </summary>
    internal static class BraceClassificationNames
    {
        /// <summary>Applied to braces the adornment layer draws itself, so the real glyph does not double up.</summary>
        public const string Hidden = "IdentityBraceHidden";

        /// <summary>
        /// Layered <em>over</em> a palette classification to fade a brace outside the caret's
        /// scope.
        /// </summary>
        /// <remarks>
        /// One type, not another thirty-two. It carries opacity and nothing else, so when the
        /// tagger yields it alongside a palette tag on the same character the format map
        /// merges the two — colour from one, opacity from the other. A parallel dim palette
        /// would have meant 64 format definitions to keep in step, and every future palette
        /// change made twice.
        /// </remarks>
        public const string Dim = "IdentityBraceDim";

        private static readonly string[] All =
        {
            "IdentityBrace00",
            "IdentityBrace01",
            "IdentityBrace02",
            "IdentityBrace03",
            "IdentityBrace04",
            "IdentityBrace05",
            "IdentityBrace06",
            "IdentityBrace07",
            "IdentityBrace08",
            "IdentityBrace09",
            "IdentityBrace10",
            "IdentityBrace11",
            "IdentityBrace12",
            "IdentityBrace13",
            "IdentityBrace14",
            "IdentityBrace15",
            "IdentityBrace16",
            "IdentityBrace17",
            "IdentityBrace18",
            "IdentityBrace19",
            "IdentityBrace20",
            "IdentityBrace21",
            "IdentityBrace22",
            "IdentityBrace23",
            "IdentityBrace24",
            "IdentityBrace25",
            "IdentityBrace26",
            "IdentityBrace27",
            "IdentityBrace28",
            "IdentityBrace29",
            "IdentityBrace30",
            "IdentityBrace31",
        };

        public static string Get(int index)
        {
            return All[((index % 32) + 32) % 32];
        }
    }
}
