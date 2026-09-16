package identitybraces.editor.scenes

import identitybraces.core.TraitIds
import identitybraces.editor.scenes.Keyframes.Key
import java.awt.AlphaComposite
import java.awt.BasicStroke
import java.awt.Color
import java.awt.Font
import java.awt.Graphics2D
import java.awt.Rectangle
import java.awt.geom.Ellipse2D
import java.awt.geom.Path2D
import java.awt.geom.Rectangle2D
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.min

/**
 * A brace crews a fire truck, drives to a burning brace, hoses it down, and goes home.
 *
 * The scene the director was built for: the only thing in the catalogue that needs two
 * braces that do not know about each other and are not adjacent. The crew carries
 * `firebrigade`, the casualty carries `fire`, and neither can see the other from inside a
 * one-character canvas.
 *
 * **The fire does not go out.** It is drawn by the `fire` effect on the casualty itself,
 * which a scene may not touch — so the brigade turns out, sprays, raises some steam, and
 * drives home with the brace still merrily alight. Given what this plugin is for, that is
 * the better ending anyway.
 */
class FireBrigadeScene : Scene {
    override val traitId: String = TraitIds.FIRE_BRIGADE
    override val subjectTraitId: String = TraitIds.FIRE
    override val durationMs: Long = 3400

    /**
     * Casts the nearest crew to a fire, on the same line. Driving between rows would mean
     * pathing a prop through the text vertically, which at this size reads as something
     * falling rather than as a journey. `cast[0]` is the fire; `cast[1]` is the crew.
     */
    override fun tryCast(actors: List<SceneActor>, subjects: List<SceneActor>): List<SceneActor>? {
        for (fire in subjects) {
            var best: SceneActor? = null
            var bestDistance = Int.MAX_VALUE

            for (crew in actors) {
                if (crew.lineStart != fire.lineStart || crew.position == fire.position) {
                    continue
                }

                val distance = abs(crew.column - fire.column)
                if (distance in 1..MAX_CALL_OUT_DISTANCE && distance < bestDistance) {
                    bestDistance = distance
                    best = crew
                }
            }

            if (best != null) {
                return listOf(fire, best)
            }
        }

        return null
    }

    override fun bounds(cast: List<SceneActor>): Rectangle {
        val fire = cast[0]
        val crew = cast[1]
        val left = min(fire.cellLeft, crew.cellLeft) - crew.cellWidth * 2
        val right = max(fire.cellLeft, crew.cellLeft) + crew.cellWidth * 3
        val top = fire.textTop - fire.textHeight
        return Rectangle(left.toInt(), top.toInt(), (right - left).toInt(), (fire.textHeight * 2.5).toInt())
    }

    override fun paint(g: Graphics2D, cast: List<SceneActor>, progress: Double, glyphFont: Font) {
        val fire = cast[0]
        val crew = cast[1]

        val width = crew.cellWidth * 1.7
        val height = crew.textHeight * 0.40
        val rightwards = fire.cellCenterX > crew.cellCenterX

        // Stopping a little short, so the truck parks beside the fire rather than on top of
        // it. Parked over the casualty, the whole scene would be one indistinct blob.
        val approach = if (rightwards) -width * 0.8 else width * 0.8
        val startX = crew.cellCenterX - width / 2.0
        val stopX = fire.cellCenterX - width / 2.0 + approach
        val roadY = crew.baselineY - height

        // Out, hold while working, back. One track for the whole call-out so the phases cannot
        // drift apart from each other.
        val x = Keyframes.at(
            progress,
            Key(0.0, startX),
            Key(0.28, stopX, Keyframes::easeOut),
            Key(0.70, stopX),
            Key(0.97, startX, Keyframes::easeIn),
        )

        val arriving = Keyframes.at(progress, Key(0.0, 0.0), Key(0.08, 1.0), Key(0.92, 1.0), Key(1.0, 0.0))
        if (arriving > 0.01) {
            val g2 = g.create() as Graphics2D
            try {
                g2.composite = AlphaComposite.SrcOver.derive(arriving.toFloat())
                drawTruck(g2, x, roadY, width, height, rightwards)
            } finally {
                g2.dispose()
            }
        }

        spray(g, fire, height, rightwards, progress)
        puff(g, fire, progress)
    }

    /** Three jets of water, arcing from the truck onto the casualty. */
    private fun spray(g: Graphics2D, fire: SceneActor, truckHeight: Double, rightwards: Boolean, t: Double) {
        val size = max(1.5, fire.cellWidth * 0.22)
        val from = fire.cellCenterX + (if (rightwards) -1 else 1) * fire.cellWidth * 1.3
        val reach = (fire.cellCenterX - from) * 0.95
        val baseY = fire.baselineY - truckHeight

        for (i in 0 until 3) {
            // Staggered, so the jets read as a stream rather than as three things thrown at
            // once.
            val open = 0.34 + i * 0.09

            val opacity = if (t < open) 0.0 else Keyframes.at(t, Key(open, 0.95), Key(open + 0.17, 0.0))
            if (opacity <= 0.01) {
                continue
            }

            val dx = Keyframes.at(t, Key(open, 0.0), Key(open + 0.16, reach))
            val dy = Keyframes.at(
                t,
                Key(open, 0.0),
                Key(open + 0.08, -fire.textHeight * 0.30, Keyframes::easeOut),
                Key(open + 0.16, 0.0, Keyframes::easeIn),
            )

            val g2 = g.create() as Graphics2D
            try {
                g2.composite = AlphaComposite.SrcOver.derive(opacity.toFloat())
                g2.color = WATER
                g2.fill(Ellipse2D.Double(from + dx, baseY + dy, size, size))
            } finally {
                g2.dispose()
            }
        }
    }

    /** Steam off the casualty, which goes on burning regardless. */
    private fun puff(g: Graphics2D, fire: SceneActor, t: Double) {
        val opacity = if (t < 0.42) 0.0 else Keyframes.at(t, Key(0.42, 0.55), Key(0.76, 0.0))
        if (opacity <= 0.01) {
            return
        }

        val size = fire.cellWidth * 0.9
        val rise = Keyframes.at(t, Key(0.0, 0.0), Key(0.42, 0.0), Key(0.75, -fire.textHeight * 0.8))

        val g2 = g.create() as Graphics2D
        try {
            g2.composite = AlphaComposite.SrcOver.derive(opacity.toFloat())
            g2.color = STEAM
            g2.fill(Ellipse2D.Double(fire.cellCenterX - fire.cellWidth * 0.45, fire.textTop + rise, size, size))
        } finally {
            g2.dispose()
        }
    }

    /**
     * A truck: body, cab, and two wheels. The cab goes at the front, which is how you can
     * tell it is driving somewhere rather than reversing. At a 7 px cell the whole thing is
     * about twelve pixels across, so the wheels are the only detail that survives.
     */
    private fun drawTruck(g: Graphics2D, x: Double, y: Double, width: Double, height: Double, facingRight: Boolean) {
        val bodyTop = height * 0.30
        val wheelSize = max(2.0, height * 0.42)
        val wheelTop = height - wheelSize
        val cabWidth = width * 0.34
        val cabLeft = if (facingRight) width - cabWidth else 0.0

        val shape = Path2D.Double()
        shape.append(Rectangle2D.Double(x, y + bodyTop, width, height * 0.45), false)
        shape.append(Rectangle2D.Double(x + cabLeft, y, cabWidth, bodyTop + 1), false)
        shape.append(Ellipse2D.Double(x + width * 0.08, y + wheelTop, wheelSize, wheelSize), false)
        shape.append(Ellipse2D.Double(x + width - width * 0.08 - wheelSize, y + wheelTop, wheelSize, wheelSize), false)

        g.color = TRUCK_RED
        g.fill(shape)
        g.color = TRUCK_TRIM
        g.stroke = BasicStroke(0.6f)
        g.draw(shape)
    }

    companion object {
        /**
         * Furthest the truck will drive, in columns. Both ends have to be on screen at once
         * or the scene is a prop wandering off one edge.
         */
        private const val MAX_CALL_OUT_DISTANCE = 40

        private val TRUCK_RED = Color(0xC8, 0x2F, 0x2F)
        private val TRUCK_TRIM = Color(0xF0, 0xE6, 0xD2)
        private val WATER = Color(0x5A, 0xB8, 0xE8)
        private val STEAM = Color(0xD8, 0xDE, 0xE4)
    }
}
