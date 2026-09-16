package identitybraces.core

import kotlin.math.pow

/**
 * A smooth hue wheel for animated braces, held at the same relative luminance as the static
 * palette (0.2072 — the point of equal contrast against dark and light editor backgrounds).
 *
 * The static palette cannot be reused for animation: its entries are spaced by the golden
 * angle precisely so neighbours look unrelated, which is the opposite of what a smooth cycle
 * needs. This ring is evenly spaced instead, so a brace sweeps the wheel without ever dimming
 * out against the background as it passes through yellow or blue.
 */
object ColorRing {
    const val COUNT = 36
    private const val TARGET_LUMINANCE = 0.2072

    private val ring = IntArray(COUNT) { i -> solveForLuminance(i * (360.0 / COUNT), 0.85) }

    /** The ARGB of ring entry [index], wrapping in both directions. */
    fun argb(index: Int): Int = ring[((index % COUNT) + COUNT) % COUNT]

    /** The ring entry for a phase in [0, 1). */
    fun at(phase: Double): Int = argb((phase * COUNT).toInt())

    /**
     * Binary-searches HSL lightness until the result hits [TARGET_LUMINANCE]. Hues differ
     * wildly in how much lightness they need (yellow reaches the target far darker than blue
     * does), so a fixed lightness would not do. Cheap enough to run once at class load.
     */
    private fun solveForLuminance(hue: Double, saturation: Double): Int {
        var lo = 0.0
        var hi = 1.0
        var color = 0xFF808080.toInt()

        repeat(24) {
            val mid = (lo + hi) / 2.0
            color = fromHsl(hue, saturation, mid)
            if (relativeLuminance(color) < TARGET_LUMINANCE) {
                lo = mid
            } else {
                hi = mid
            }
        }

        return color
    }

    private fun fromHsl(hue: Double, saturation: Double, lightness: Double): Int {
        val h = ((hue % 360.0) + 360.0) % 360.0 / 360.0
        val a = saturation * (if (lightness < 0.5) lightness else 1.0 - lightness)

        val r = channel(0.0, h, lightness, a)
        val g = channel(8.0, h, lightness, a)
        val b = channel(4.0, h, lightness, a)
        return (0xFF shl 24) or (r shl 16) or (g shl 8) or b
    }

    private fun channel(n: Double, hue: Double, lightness: Double, a: Double): Int {
        val k = (n + hue * 12.0) % 12.0
        var min = k - 3.0
        if (9.0 - k < min) {
            min = 9.0 - k
        }

        if (1.0 < min) {
            min = 1.0
        }

        if (min < -1.0) {
            min = -1.0
        }

        val value = (lightness - a * min) * 255.0
        return (if (value < 0) 0.0 else if (value > 255) 255.0 else value).toInt()
    }

    /** WCAG relative luminance of an ARGB value. */
    fun relativeLuminance(argb: Int): Double {
        return 0.2126 * linearize((argb shr 16) and 0xFF) +
            0.7152 * linearize((argb shr 8) and 0xFF) +
            0.0722 * linearize(argb and 0xFF)
    }

    private fun linearize(channel: Int): Double {
        val v = channel / 255.0
        return if (v <= 0.03928) v / 12.92 else ((v + 0.055) / 1.055).pow(2.4)
    }
}
