using System.Text;

namespace IdentityBraces.Core
{
    /// <summary>
    /// Deterministic hashing.
    /// <para>
    /// Deliberately not <c>string.GetHashCode</c>: that is randomised per process on
    /// several runtimes, which would hand every brace a brand new personality on each
    /// Visual Studio restart. A brace's identity has to outlive the session.
    /// </para>
    /// </summary>
    internal static class Hash
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        /// <summary>FNV-1a over the UTF-16 code units of <paramref name="value"/>.</summary>
        public static ulong Fnv1a(string value)
        {
            ulong hash = FnvOffsetBasis;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= FnvPrime;
            }

            return hash;
        }

        /// <summary>FNV-1a over a <see cref="StringBuilder"/>, avoiding an interim string.</summary>
        public static ulong Fnv1a(StringBuilder value)
        {
            ulong hash = FnvOffsetBasis;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= FnvPrime;
            }

            return hash;
        }

        /// <summary>
        /// SplitMix64 finaliser. Folds <paramref name="salt"/> in and avalanches the
        /// result, so two hashes that differ in one bit land far apart.
        /// </summary>
        public static ulong Mix(ulong hash, ulong salt)
        {
            ulong z = hash + salt * 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Maps a hash onto [0, 1) using the 53 bits a double can hold exactly.</summary>
        public static double ToUnitInterval(ulong hash)
        {
            return (hash >> 11) * (1.0 / 9007199254740992.0);
        }

        /// <summary>Maps a hash onto [0, <paramref name="count"/>).</summary>
        public static int ToIndex(ulong hash, int count)
        {
            return (int)(hash % (ulong)count);
        }
    }
}
