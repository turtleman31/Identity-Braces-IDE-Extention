package identitybraces.editor.scenes

import identitybraces.core.TraitIds
import identitybraces.editor.scenes.Keyframes.Key
import java.awt.AlphaComposite
import java.awt.Font
import java.awt.Graphics2D
import java.awt.Rectangle
import java.awt.geom.Rectangle2D
import kotlin.math.max
import kotlin.math.min

/**
 * (╯°□°)╯︵ ┻━┻ — a brace throws a table into the empty space beside it.
 *
 * The simplest scene that genuinely needs a director: it is one brace, but a per-brace
 * painter is handed a canvas exactly one character wide and knows nothing about what is
 * next to it. A table thrown without checking would land in the middle of somebody's
 * identifier.
 *
 * The brace itself does not move. The table launching out of its cell carries the joke
 * without needing the thrower to lean.
 */
class TableFlipScene : Scene {
    override val traitId: String = TraitIds.TABLE_FLIP

    /** Nothing is thrown *at* anybody. The empty space is the target. */
    override val subjectTraitId: String? = null

    override val durationMs: Long = 2100

    /** The whole flight is the first part of the duration; the rest is the table lying there. */
    private val flightFraction = 950.0 / 2100.0

    /**
     * Casts one brace with somewhere to throw. Right is preferred over left because a brace
     * usually has the rest of its line free to the right and code to the left.
     */
    override fun tryCast(actors: List<SceneActor>, subjects: List<SceneActor>): List<SceneActor>? {
        val actor = actors.firstOrNull { it.roomRight >= REQUIRED_ROOM || it.roomLeft >= REQUIRED_ROOM } ?: return null
        return listOf(actor)
    }

    override fun bounds(cast: List<SceneActor>): Rectangle {
        val actor = cast[0]
        val reach = (PREFERRED_THROW + 2) * actor.cellWidth
        return Rectangle(
            (actor.cellLeft - reach).toInt(),
            (actor.textTop - actor.textHeight * 1.5).toInt(),
            (reach * 2 + actor.cellWidth * 2).toInt(),
            (actor.textHeight * 3).toInt(),
        )
    }

    override fun paint(g: Graphics2D, cast: List<SceneActor>, progress: Double, glyphFont: Font) {
        val actor = cast[0]

        val throwRight = actor.roomRight >= REQUIRED_ROOM
        val room = if (throwRight) actor.roomRight else actor.roomLeft
        val columns = min(PREFERRED_THROW, room)

        val width = actor.cellWidth * 1.5
        val height = actor.textHeight * 0.42

        val startX = actor.cellCenterX - width / 2.0
        val landX = startX + (if (throwRight) 1 else -1) * columns * actor.cellWidth

        // Sitting on the baseline rather than centred in the cell, so the table stands on the
        // same line the text does.
        val restY = actor.baselineY - height
        val apexY = restY - actor.textHeight * 1.15

        // Thrown, not slid: X carries on at a constant rate while Y rises and falls, which is
        // what makes the arc read as a throw rather than a hop.
        val flight = (progress / flightFraction).coerceIn(0.0, 1.0)
        val x = startX + (landX - startX) * flight
        val y = Keyframes.at(
            flight,
            Key(0.0, restY),
            Key(0.42, apexY, Keyframes::easeOut),
            Key(1.0, restY, Keyframes::easeIn),
        )

        // Lands upside down and stays that way. A table that rights itself on landing is a
        // table nobody flipped.
        val angle = Math.toRadians((if (throwRight) 200.0 else -200.0) * Keyframes.cubicOut(flight))

        // A beat lying there before it fades, so the punchline lands.
        val opacity = Keyframes.at(progress, Key(0.0, 1.0), Key(0.78, 1.0), Key(1.0, 0.0))
        if (opacity <= 0.01) {
            return
        }

        val g2 = g.create() as Graphics2D
        try {
            g2.composite = AlphaComposite.SrcOver.derive(opacity.toFloat())
            g2.rotate(angle, x + width / 2.0, y + height / 2.0)
            g2.color = actor.color

            // A table top on two legs. The legs are a fifth of the width each, which at a 7 px
            // cell is a shade over 2 px — the floor below which a detail averages into its
            // neighbours.
            val topThickness = max(1.5, height * 0.3)
            val legWidth = max(1.5, width * 0.19)
            g2.fill(Rectangle2D.Double(x, y, width, topThickness))
            g2.fill(Rectangle2D.Double(x + width * 0.14, y + topThickness, legWidth, height - topThickness))
            g2.fill(Rectangle2D.Double(x + width - width * 0.14 - legWidth, y + topThickness, legWidth, height - topThickness))
        } finally {
            g2.dispose()
        }
    }

    companion object {
        /**
         * Columns of clear space the table needs to land in. Three is the point at which the
         * table is clearly somewhere else rather than overlapping the brace that threw it.
         */
        private const val REQUIRED_ROOM = 3

        /** How far it travels, in columns, when there is room to spare. */
        private const val PREFERRED_THROW = 4
    }
}
