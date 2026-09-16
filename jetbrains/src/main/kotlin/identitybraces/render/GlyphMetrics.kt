package identitybraces.render

import java.awt.Font
import java.awt.font.FontRenderContext
import java.util.concurrent.ConcurrentHashMap

/** Where a character's actual ink sits, relative to its baseline and its layout origin. */
class GlyphInk(
    val ascentAboveBaseline: Double,
    val descentBelowBaseline: Double,
    val left: Double,
    val right: Double,
) {
    val height: Double
        get() = ascentAboveBaseline + descentBelowBaseline

    val centerX: Double
        get() = (left + right) / 2.0
}

/**
 * Measures the ink box of a glyph and caches it per font.
 *
 * This exists because the em box is not the glyph. For Consolas at 13.33 px the cell is
 * 7.33 × 15 but a brace's ink is only 6 × 12, sitting 3.3 px below the top of the em box
 * and 0.7 px left of the cell's centre. Decorations anchored to the em box float above the
 * glyph and lean right, which is exactly what the first version of the cat ears did.
 *
 * The Visual Studio extension measures this through `FormattedText`; the VS Code port has no
 * font metrics at all and assumes Consolas' proportions. Java2D gives them back: a glyph
 * vector's visual bounds are the real outline.
 */
object GlyphMetrics {
    private data class Key(val font: Font, val character: Char)

    private val cache = ConcurrentHashMap<Key, GlyphInk>()

    fun measure(font: Font, frc: FontRenderContext, character: Char): GlyphInk {
        return cache.computeIfAbsent(Key(font, character)) { measureCore(font, frc, character) }
    }

    private fun measureCore(font: Font, frc: FontRenderContext, character: Char): GlyphInk {
        try {
            val vector = font.createGlyphVector(frc, charArrayOf(character))
            val bounds = vector.visualBounds
            if (bounds.isEmpty || bounds.height <= 0) {
                return fallback(font.size2D.toDouble())
            }

            // Visual bounds are relative to the glyph origin, which sits on the baseline, so
            // a negative y is ink above it.
            return GlyphInk(
                ascentAboveBaseline = -bounds.y,
                descentBelowBaseline = bounds.y + bounds.height,
                left = bounds.x,
                right = bounds.x + bounds.width,
            )
        } catch (e: Exception) {
            // A broken or unusual font must not cost us the drawing.
            return fallback(font.size2D.toDouble())
        }
    }

    /** Proportions close to Consolas, used when a font refuses to be measured. */
    private fun fallback(fontSize: Double): GlyphInk {
        return GlyphInk(fontSize * 0.675, fontSize * 0.225, 0.0, fontSize * 0.45)
    }
}
