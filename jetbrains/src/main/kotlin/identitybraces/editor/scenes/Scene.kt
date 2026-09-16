package identitybraces.editor.scenes

import java.awt.Color
import java.awt.Font
import java.awt.Graphics2D
import java.awt.Rectangle
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * One brace that a scene may act on, with everything it needs to know about where that
 * brace is and what is around it.
 *
 * Resolved by the director from live line geometry immediately before a scene is played,
 * and never held across a layout. See [SceneDirector] for why that is the whole safety story.
 */
class SceneActor(
    /** Document offset of the brace. */
    val position: Int,

    /** Start of the line this brace is on, so two actors can tell they share one. */
    val lineStart: Int,

    /** Column of the brace within its line. */
    val column: Int,

    val character: Char,

    /** Left edge of its character cell, in editor coordinates. */
    val cellLeft: Double,
    val cellWidth: Double,

    /** Top of the line, in editor coordinates. */
    val textTop: Double,
    val textHeight: Double,

    /** Y of the text baseline. */
    val baselineY: Double,

    /** Blank columns to the right before the next character. */
    val roomRight: Int,

    /** Blank columns to the left before the previous character. */
    val roomLeft: Int,

    /** The brace's own colour, so a prop can match its owner. */
    val color: Color,

    /** Stable per-brace value, for scenes wanting repeatable variety. */
    val identity: ULong,
) {
    val cellCenterX: Double
        get() = cellLeft + cellWidth / 2.0
}

/**
 * A multi-brace performance: something above the per-brace painters that needs more than
 * one glyph, or more than one cell, to make sense.
 *
 * The per-brace draw functions are handed a canvas the size of one character and know
 * nothing about their neighbours or the text around them. That is the right shape for
 * eighty-odd traits and the wrong shape for a table flip, which needs to know there is empty
 * space to throw a table into, and for a fire brigade, which needs to find a burning brace
 * and travel to it.
 *
 * A scene is stateless: it is asked to paint at a progress in [0, 1] and draws whatever the
 * performance looks like at that moment. The director owns the clock.
 */
interface Scene {
    /** The effect a brace must carry to perform in this scene. */
    val traitId: String

    /**
     * A second effect identifying something the scene acts *upon*, or null. The fire
     * brigade is why this exists: its actors carry `firebrigade` but the thing they turn out
     * for carries `fire`.
     */
    val subjectTraitId: String?

    /** How long the performance runs. */
    val durationMs: Long

    /**
     * Picks a cast, or returns null to sit this round out — the normal outcome for a scene
     * with nothing to do. `cast[0]` is whatever the scene is centred on.
     */
    fun tryCast(actors: List<SceneActor>, subjects: List<SceneActor>): List<SceneActor>?

    /** The pixels the performance may touch, so the director knows what to repaint. */
    fun bounds(cast: List<SceneActor>): Rectangle

    /** Paints the performance at [progress] in [0, 1]. */
    fun paint(g: Graphics2D, cast: List<SceneActor>, progress: Double, glyphFont: Font)
}

/** The keyframe arithmetic every scene is made of. */
object Keyframes {
    fun linear(t: Double): Double = t
    fun easeIn(t: Double): Double = t * t
    fun easeOut(t: Double): Double = 1 - (1 - t) * (1 - t)
    fun cubicOut(t: Double): Double = 1 - (1 - t) * (1 - t) * (1 - t)
    fun sineInOut(t: Double): Double = (1 - cos(t * PI)) / 2
    fun sineOut(t: Double): Double = sin(t * PI / 2)
    fun sineIn(t: Double): Double = 1 - cos(t * PI / 2)

    /** One keyframe: the value reached at [at], approached with [ease] from the previous one. */
    class Key(val at: Double, val value: Double, val ease: (Double) -> Double = ::linear)

    /** The value at [t] along a keyframe track. Before the first key it holds the first value; after the last, the last. */
    fun at(t: Double, vararg keys: Key): Double {
        if (keys.isEmpty()) {
            return 0.0
        }

        if (t <= keys[0].at) {
            return keys[0].value
        }

        for (i in 1 until keys.size) {
            val previous = keys[i - 1]
            val next = keys[i]
            if (t <= next.at) {
                val span = next.at - previous.at
                val local = if (span <= 0) 1.0 else ((t - previous.at) / span).coerceIn(0.0, 1.0)
                return previous.value + (next.value - previous.value) * next.ease(local)
            }
        }

        return keys[keys.size - 1].value
    }

    /** A value that snaps rather than interpolates: [value] from [at] onward. */
    fun step(t: Double, at: Double, before: Double, value: Double): Double = if (t < at) before else value
}
