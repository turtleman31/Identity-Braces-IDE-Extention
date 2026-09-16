package identitybraces.editor.scenes

import identitybraces.core.TraitIds
import identitybraces.editor.scenes.Keyframes.Key
import java.awt.AlphaComposite
import java.awt.Font
import java.awt.Graphics2D
import java.awt.Rectangle
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.min

/**
 * Two braces on the same line visit each other's column, and come back.
 *
 * The braces themselves stay put; each sends a likeness — a copy of its own glyph, in its
 * own colour — which arcs across to the other's column and back while the originals hold
 * their ground. Lifting the copies clear of the line as they cross is what stops them
 * reading as a rendering fault: two glyphs sliding *through* the text between them would
 * look like a redraw bug; two glyphs hopping over it looks deliberate.
 */
class SwapPlacesScene : Scene {
    override val traitId: String = TraitIds.SWAP_PLACES
    override val subjectTraitId: String? = null
    override val durationMs: Long = 2400

    /**
     * Finds two candidates sharing a line at a sensible distance. The director resolves
     * candidates as a consecutive run through the brace map, which is what makes this likely
     * to succeed: consecutive braces are usually neighbours on a line.
     */
    override fun tryCast(actors: List<SceneActor>, subjects: List<SceneActor>): List<SceneActor>? {
        for (i in actors.indices) {
            for (j in i + 1 until actors.size) {
                if (actors[i].lineStart != actors[j].lineStart) {
                    continue
                }

                val separation = abs(actors[i].column - actors[j].column)
                if (separation < MIN_SEPARATION || separation > MAX_SEPARATION) {
                    continue
                }

                return listOf(actors[i], actors[j])
            }
        }

        return null
    }

    override fun bounds(cast: List<SceneActor>): Rectangle {
        val left = min(cast[0].cellLeft, cast[1].cellLeft) - cast[0].cellWidth
        val right = max(cast[0].cellLeft, cast[1].cellLeft) + cast[0].cellWidth * 2
        val top = cast[0].textTop - cast[0].textHeight * 1.2
        return Rectangle(left.toInt(), top.toInt(), (right - left).toInt(), (cast[0].textHeight * 3.4).toInt())
    }

    override fun paint(g: Graphics2D, cast: List<SceneActor>, progress: Double, glyphFont: Font) {
        // Arcing in opposite directions, so the two never occupy the same air. Sent the same
        // way they would collide in the middle, which reads as one glyph rather than as two
        // passing.
        send(g, cast[0], cast[1], -1.0, progress, glyphFont)
        send(g, cast[1], cast[0], 1.0, progress, glyphFont)
    }

    /** Draws [from]'s likeness on its way to [to] and back. */
    private fun send(g: Graphics2D, from: SceneActor, to: SceneActor, lift: Double, t: Double, font: Font) {
        val travel = to.cellLeft - from.cellLeft
        val height = from.textHeight * 0.95 * lift

        val dx = Keyframes.at(
            t,
            Key(0.0, 0.0),
            Key(0.40, travel, Keyframes::sineInOut),
            Key(0.60, travel),
            Key(0.95, 0.0, Keyframes::sineInOut),
        )

        val dy = Keyframes.at(
            t,
            Key(0.0, 0.0),
            Key(0.20, height, Keyframes::sineOut),
            Key(0.40, 0.0, Keyframes::sineIn),
            Key(0.77, height, Keyframes::sineOut),
            Key(0.95, 0.0, Keyframes::sineIn),
        )

        // Faded at both ends, so a likeness never sits on top of the original it copied —
        // which would show as one brace suddenly rendering twice as boldly.
        val opacity = Keyframes.at(t, Key(0.0, 0.0), Key(0.12, 1.0), Key(0.85, 1.0), Key(0.96, 0.0))
        if (opacity <= 0.01) {
            return
        }

        val g2 = g.create() as Graphics2D
        try {
            g2.composite = AlphaComposite.SrcOver.derive(opacity.toFloat())
            g2.font = font
            g2.color = from.color
            g2.drawString(from.character.toString(), (from.cellLeft + dx).toFloat(), (from.baselineY - dy).toFloat())
        } finally {
            g2.dispose()
        }
    }

    companion object {
        /**
         * Furthest apart, in columns, that two braces will bother to trade. A swap across
         * forty columns is two things moving independently at opposite ends of the screen,
         * not a pair trading.
         */
        private const val MAX_SEPARATION = 24

        /** Nearest, in columns. Adjacent braces just look like a wobble. */
        private const val MIN_SEPARATION = 2
    }
}
