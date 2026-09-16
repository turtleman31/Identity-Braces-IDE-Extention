package identitybraces.core

/**
 * Deterministic 64-bit hashing, bit-identical to the Visual Studio extension's
 * `IdentityBraces.Core.Hash` and the VS Code port's `hash.ts`.
 *
 * A brace's identity has to outlive the session, the machine and the editor: the same file
 * opened in Rider, in Visual Studio and in VS Code must hand the same brace the same colour
 * and the same personality. That means the same FNV-1a and the same SplitMix64 finaliser,
 * down to the wrapping arithmetic — and never `String.hashCode`, whose contract says nothing
 * about matching anyone else.
 *
 * Unlike the TypeScript port this is a straight transliteration of the C#: the JVM has real
 * 64-bit integers, and [ULong] makes the shifts and the modulo unsigned without any of the
 * two-halves arithmetic the JavaScript had to do.
 */
internal object Hash {
    private const val FNV_OFFSET_BASIS: ULong = 14695981039346656037uL
    private const val FNV_PRIME: ULong = 1099511628211uL

    /** FNV-1a over the UTF-16 code units of [value]. */
    fun fnv1a(value: CharSequence): ULong {
        var hash = FNV_OFFSET_BASIS
        for (i in 0 until value.length) {
            hash = hash xor value[i].code.toULong()
            hash *= FNV_PRIME
        }

        return hash
    }

    /**
     * SplitMix64 finaliser. Folds [salt] in and avalanches the result, so two hashes that
     * differ in one bit land far apart.
     */
    fun mix(hash: ULong, salt: ULong): ULong {
        var z = hash + salt * 0x9E3779B97F4A7C15uL
        z = (z xor (z shr 30)) * 0xBF58476D1CE4E5B9uL
        z = (z xor (z shr 27)) * 0x94D049BB133111EBuL
        return z xor (z shr 31)
    }

    /** Maps a hash onto [0, 1) using the 53 bits a double can hold exactly. */
    fun toUnitInterval(hash: ULong): Double {
        return (hash shr 11).toDouble() * (1.0 / 9007199254740992.0)
    }

    /** Maps a hash onto [0, [count]). */
    fun toIndex(hash: ULong, count: Int): Int {
        return (hash % count.toULong()).toInt()
    }

    /** The sixteen lower-case hex digits the parity fixture writes an identity as. */
    fun hex(hash: ULong): String {
        return hash.toString(16).padStart(16, '0')
    }
}
