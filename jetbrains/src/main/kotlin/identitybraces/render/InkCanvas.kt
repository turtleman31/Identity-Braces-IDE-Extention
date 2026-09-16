package identitybraces.render

import identitybraces.core.Hash
import java.awt.AlphaComposite
import java.awt.BasicStroke
import java.awt.Color
import java.awt.Graphics2D
import java.awt.geom.CubicCurve2D
import java.awt.geom.Ellipse2D
import java.awt.geom.Path2D
import java.awt.geom.Rectangle2D
import java.awt.geom.RoundRectangle2D
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.max

/** A point in ink units. */
class Pt(val x: Double, val y: Double)

/**
 * Everything a trait's draw function needs, plus primitives to draw with.
 *
 * Coordinates are supplied in *ink units*: 0 is the horizontal centre of the glyph's ink and
 * the top of it vertically, and 1 unit is the ink's height. So a shape authored once holds
 * its proportions at any font, size or zoom — which is the only way eighty-six traits stay
 * maintainable, and it is why every creature here is the Visual Studio extension's geometry
 * unchanged.
 *
 * This is the Java2D counterpart of the WPF `BraceDrawContext` and the SVG `SvgContext`. It
 * is immediate-mode: a painter runs on every repaint and asks [phase] for the time, so an
 * animated trait is simply one that draws differently from one frame to the next.
 */
class InkCanvas(
    private val g: Graphics2D,

    /** The brace's own colour, after any motion or effect has recoloured it. */
    val color: Color,

    val character: Char,
    val identity: ULong,

    /** Ink height in device pixels. The unit for every authored coordinate. */
    private val unit: Double,

    /** Canvas X of the ink's horizontal centre. */
    private val centerX: Double,

    /** Canvas Y of the ink's top edge. */
    private val inkTop: Double,

    /** Multiplier for creature and costume geometry. */
    private val decorScale: Double,

    /** Milliseconds on the animation clock. */
    private val timeMs: Long,

    private val motionEnabled: Boolean,

    /** Draws another copy of the glyph, offset in ink units. See [glyph]. */
    private val glyphPainter: ((Color, Double, Double, Double) -> Unit)?,
) {
    private var animatedFlag = false
    private val composites = ArrayList<java.awt.Composite>()

    // The absolute alpha everything is drawn at: the brace's own opacity, which the renderer
    // has already put on the graphics, times every open group.
    private var opacity = (g.composite as? AlphaComposite)?.alpha?.toDouble() ?: 1.0

    /**
     * Where this brace is in a cycle of [seconds], as a value in [0, 1). Offset per brace
     * when a salt is given, so a screenful of the same trait does not pulse in unison —
     * which reads as the page flashing rather than as forty separate creatures.
     */
    fun phase(seconds: Double, salt: ULong? = null): Double {
        animatedFlag = true
        if (!motionEnabled) {
            return 0.0
        }

        val offset = if (salt != null) Hash.toUnitInterval(Hash.mix(identity, salt)) else 0.0
        val raw = (timeMs / (seconds * 1000.0) + offset) % 1.0
        return if (raw < 0) raw + 1.0 else raw
    }

    /** A phase mapped onto a smooth there-and-back, for anything that eases rather than loops. */
    fun swing(seconds: Double, salt: ULong? = null): Double {
        return (1 - cos(phase(seconds, salt) * PI * 2)) / 2
    }

    /** True when anything drawn here asked for the time, and so has to be redrawn. */
    val animated: Boolean
        get() = animatedFlag

    /**
     * A deterministic value in [0,1) from this brace's identity, for traits that want stable
     * variety — a lean angle, a colour pick, a phase offset.
     */
    fun roll(salt: ULong): Double = Hash.toUnitInterval(Hash.mix(identity, salt))

    fun p(x: Double, y: Double): Pt = Pt(x, y)

    // ---- coordinate conversion ----

    private fun x(units: Double): Double = centerX + units * unit * decorScale

    private fun y(units: Double): Double = inkTop + units * unit * decorScale

    private fun len(units: Double): Double = units * unit * decorScale

    /** Stroke widths obey the same one-device-pixel floor the WPF original used. */
    private fun thick(thickness: Double): Float = max(1.0, len(thickness)).toFloat()

    // ---- primitives ----

    fun triangle(color: Color, x1: Double, y1: Double, x2: Double, y2: Double, x3: Double, y3: Double) {
        val path = Path2D.Double()
        path.moveTo(x(x1), y(y1))
        path.lineTo(x(x2), y(y2))
        path.lineTo(x(x3), y(y3))
        path.closePath()
        g.color = color
        g.fill(path)
    }

    fun dot(color: Color, cx: Double, cy: Double, radius: Double) {
        val r = len(radius)
        g.color = color
        g.fill(Ellipse2D.Double(x(cx) - r, y(cy) - r, r * 2, r * 2))
    }

    /**
     * A flattened ring. The vertical squash is what makes a halo read as a halo seen at a
     * slight angle rather than as an O.
     */
    fun ring(color: Color, cx: Double, cy: Double, radius: Double, thickness: Double) {
        val r = len(radius)
        g.color = color
        g.stroke = BasicStroke(thick(thickness))
        g.draw(Ellipse2D.Double(x(cx) - r, y(cy) - r * 0.55, r * 2, r * 1.1))
    }

    fun box(color: Color, x: Double, y: Double, w: Double, h: Double, radius: Double = 0.0) {
        val width = max(1.0, len(w))
        val height = max(1.0, len(h))
        g.color = color
        if (radius > 0) {
            val arc = len(radius) * 2
            g.fill(RoundRectangle2D.Double(x(x), y(y), width, height, arc, arc))
        } else {
            g.fill(Rectangle2D.Double(x(x), y(y), width, height))
        }
    }

    fun stroke(color: Color, thickness: Double, vararg points: Pt) {
        if (points.size < 2) {
            return
        }

        val path = Path2D.Double()
        path.moveTo(x(points[0].x), y(points[0].y))
        for (i in 1 until points.size) {
            path.lineTo(x(points[i].x), y(points[i].y))
        }

        g.color = color
        g.stroke = BasicStroke(thick(thickness), BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND)
        g.draw(path)
    }

    fun curve(color: Color, thickness: Double, start: Pt, c1: Pt, c2: Pt, end: Pt) {
        g.color = color
        g.stroke = BasicStroke(thick(thickness), BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND)
        g.draw(CubicCurve2D.Double(x(start.x), y(start.y), x(c1.x), y(c1.y), x(c2.x), y(c2.y), x(end.x), y(end.y)))
    }

    /**
     * Another copy of the brace's own glyph, offset from where the real one sits, in ink
     * units. For traits that need a whole second brace rather than a decoration — `mitosis`
     * buds one off and lets it drift away. It goes through the renderer rather than being
     * drawn here so it is the same glyph: same body substitution, same font, same emphasis.
     */
    fun glyph(color: Color, dx: Double, dy: Double, opacity: Double) {
        if (opacity <= 0.01) {
            return
        }

        glyphPainter?.invoke(color, len(dx), len(dy), opacity * this.opacity)
    }

    /** Opens a group every following shape is drawn at [alpha] within, until [endGroup]. */
    fun group(alpha: Double) {
        composites.add(g.composite)
        opacity *= alpha.coerceIn(0.0, 1.0)
        g.composite = AlphaComposite.SrcOver.derive(opacity.toFloat())
    }

    fun endGroup() {
        if (composites.isEmpty()) {
            return
        }

        val previous = composites.removeAt(composites.size - 1)
        g.composite = previous
        opacity = (previous as? AlphaComposite)?.alpha?.toDouble() ?: 1.0
    }
}
