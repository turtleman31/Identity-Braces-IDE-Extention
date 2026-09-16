namespace IdentityBraces.Core
{
    /// <summary>
    /// Gives a brace a name, derived from its identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stable for the life of the brace, and stable across machines, because it is a pure
    /// function of the same identity hash that decides the brace's colour and traits. Two
    /// people looking at the same file agree about which one is Reginald.
    /// </para>
    /// <para>
    /// Built from three independent draws rather than one list of names, so the catalogue is a
    /// few dozen words instead of thousands and still rarely repeats: with 24 titles, 32 given
    /// names and 24 epithets there are around eighteen thousand combinations, and a file with a
    /// hundred named braces has a coin-flip's chance of any collision at all.
    /// </para>
    /// </remarks>
    internal static class BraceNames
    {
        private const ulong TitleSalt = 0x717DE5A17UL;
        private const ulong GivenSalt = 0x91BE0DA17UL;
        private const ulong EpithetSalt = 0xED17BE7FUL;
        private const ulong ShapeSalt = 0x5AAFE5A17UL;

        private static readonly string[] Titles =
        {
            "Sir", "Dame", "Captain", "Doctor", "Professor", "Admiral", "Baron", "Duchess",
            "Chief", "Judge", "Marshal", "Bishop", "Colonel", "Commodore", "Vicar", "Warden",
            "Lord", "Lady", "Sergeant", "Inspector", "Abbot", "Regent", "Consul", "Steward",
        };

        private static readonly string[] Given =
        {
            "Reginald", "Beatrice", "Mortimer", "Winifred", "Cuthbert", "Prudence", "Barnaby",
            "Millicent", "Horace", "Agatha", "Percival", "Edwina", "Clarence", "Hyacinth",
            "Ambrose", "Theodora", "Gideon", "Rosalind", "Alistair", "Cordelia", "Bartholomew",
            "Philippa", "Montgomery", "Griselda", "Rupert", "Josephine", "Egbert", "Marigold",
            "Fitzwilliam", "Henrietta", "Archibald", "Wilhelmina",
        };

        private static readonly string[] Epithets =
        {
            "the Unclosed", "the Patient", "the Unmatched", "of the Third Nesting",
            "the Indented", "the Verbose", "the Deprecated", "the Refactored", "the Unreachable",
            "the Well-Formed", "the Dangling", "the Terse", "the Overloaded", "the Immutable",
            "the Recursive", "the Deeply Nested", "the Uncommented", "the Legacy",
            "the Escaped", "the Trailing", "the Orphaned", "the Idempotent", "the Volatile",
            "the Considered Harmful",
        };

        /// <summary>
        /// This brace's name.
        /// </summary>
        /// <remarks>
        /// Three shapes, so a screenful is not a wall of identically-structured names: some
        /// braces get a title, some an epithet, some both, some just a name. The shape is drawn
        /// from the identity too, so it is as stable as the rest of it.
        /// </remarks>
        public static string Of(ulong identity)
        {
            string title = Pick(Titles, identity, TitleSalt);
            string given = Pick(Given, identity, GivenSalt);
            string epithet = Pick(Epithets, identity, EpithetSalt);

            switch ((int)(Hash.ToUnitInterval(Hash.Mix(identity, ShapeSalt)) * 4) % 4)
            {
                case 0: return given;
                case 1: return title + " " + given;
                case 2: return given + " " + epithet;
                default: return title + " " + given + " " + epithet;
            }
        }

        private static string Pick(string[] from, ulong identity, ulong salt)
        {
            return from[Hash.ToIndex(Hash.Mix(identity, salt), from.Length)];
        }
    }
}
