package identitybraces.core

import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNotEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test

/**
 * The 64-bit arithmetic underneath everything else.
 *
 * The JVM has real 64-bit integers, so unlike the TypeScript port there is no hand-rolled
 * carry to get wrong. What is left to get wrong is the *signedness* — a `shr` where the C#
 * has an unsigned shift, or a `%` that goes negative — and those fail quietly, as a mildly
 * uneven colour distribution rather than a broken hash.
 */
class HashTest {
    @Test
    fun `FNV-1a matches the published vectors`() {
        // The standard 64-bit FNV-1a test vectors, which is what the C# implements.
        assertEquals("cbf29ce484222325", Hash.hex(Hash.fnv1a("")))
        assertEquals("af63dc4c8601ec8c", Hash.hex(Hash.fnv1a("a")))
        assertEquals("85944171f73967e8", Hash.hex(Hash.fnv1a("foobar")))
    }

    @Test
    fun `mix avalanches`() {
        val salt = 0x9E3779B97F4A7C15uL
        val a = Hash.mix(1uL, salt)
        val b = Hash.mix(2uL, salt)

        assertNotEquals(a, b)

        // One bit of input difference should move roughly half the output bits. Anything
        // under a quarter means the finaliser is not finalising.
        val differing = (a xor b).countOneBits()
        assertTrue(differing in 17..47, "$differing of 64 bits differ")
    }

    @Test
    fun `mix is deterministic`() {
        val salt = 0x5EA1EDFA7ECAFEuL
        assertEquals(Hash.mix(0x123456789abcdef0uL, salt), Hash.mix(0x123456789abcdef0uL, salt))
    }

    @Test
    fun `toUnitInterval covers the interval without losing the top bits`() {
        assertEquals(0.0, Hash.toUnitInterval(0uL))
        assertTrue(Hash.toUnitInterval(ULong.MAX_VALUE) < 1.0)
        assertTrue(Hash.toUnitInterval(ULong.MAX_VALUE) > 0.9999999)

        // The top bit is where a signed shift would go wrong: a hash with it set must land in
        // the upper half of the interval, not below zero.
        assertTrue(kotlin.math.abs(Hash.toUnitInterval(0x8000000000000000uL) - 0.5) < 1e-12)
    }

    @Test
    fun `toIndex is an unsigned modulo`() {
        // 2^32 mod 7 is 4; 2^64 - 1 mod 32 is 31. A signed remainder gets the second one
        // wrong (as -1), which is the case this exists to catch.
        assertEquals(4, Hash.toIndex(0x100000000uL, 7))
        assertEquals(1, Hash.toIndex(33uL, 32))
        assertEquals(31, Hash.toIndex(ULong.MAX_VALUE, 32))

        for (i in 0 until 64) {
            val index = Hash.toIndex(Hash.mix(i.toULong(), 7uL), 32)
            assertTrue(index in 0 until 32)
        }
    }

    @Test
    fun `hex writes sixteen zero-padded digits`() {
        assertEquals("0000000000000001", Hash.hex(1uL))
        assertEquals("ffffffffffffffff", Hash.hex(ULong.MAX_VALUE))
        assertEquals("0b0d1e5a17c0ffee", Hash.hex(0xB0D1E5A17C0FFEEuL))
    }
}
