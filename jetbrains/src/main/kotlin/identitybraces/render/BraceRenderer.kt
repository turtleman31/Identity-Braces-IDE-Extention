package identitybraces.render

import identitybraces.core.BraceInfo
import identitybraces.core.TraitIds
import identitybraces.settings.RenderMode
import java.awt.AlphaComposite
import java.awt.Color
import java.awt.Font
import java.awt.Graphics2D
import java.awt.Rectangle
import java.awt.RenderingHints
import java.awt.geom.AffineTransform
import java.awt.geom.Rectangle2D
import kotlin.math.ceil
import kotlin.math.floor

/** Where one brace's character cell sits, in the coordinates of the graphics being painted. */
class BraceGeometry(
    val cellLeft: Double,
    val cellWidth: Double,
    val lineTop: Double,
    val lineHeight: Double,

    /** Y of the text baseline. */
    val baselineY: Double,

    /** The real character's ink, measured in the editor's font. */
    val ink: GlyphInk,
) {
    /** Canvas X of the ink's horizontal centre — not the cell's, which side bearing shifts. */
    val centerX: Double
        get() = cellLeft + ink.centerX

    val inkTop: Double
        get() = baselineY - ink.ascentAboveBaseline

    /** Ink height in device pixels. The unit for every authored coordinate. */
    val unit: Double
        get() = ink.height
}

/** The fonts a frame draws with, resolved by the editor layer. */
interface FontSource {
    /** The editor's own font in a style, at the editor's current size. */
    fun editorFont(bold: Boolean, italic: Boolean): Font

    /** A font able to display [text] — an emoji needs a fallback the editor font lacks. */
    fun fontFor(text: String, base: Font): Font

    /** The one deliberately wrong font, for `foreignfont`. */
    fun foreignFont(base: Font): Font
}

/**
 * Paints one drawn brace: shadow, glyph, stockings, creature, costume, motion, effects.
 *
 * Draw order is back to front, the same as the WPF original: backdrop effects, then the
 * glyph, then costume, then creature. That ordering is why a cape sits behind a brace and a
 * hat sits on top of it without either trait knowing the other exists. The whole assembly
 * shares one transform, so a spinning brace spins its ears with it.
 */
object BraceRenderer {
    /** The drawing region around a brace, in ink units — the VS Code port's SVG viewBox. */
    private const val REGION_LEFT = -1.4
    private const val REGION_TOP = -1.6
    private const val REGION_WIDTH = 2.9
    private const val REGION_HEIGHT = 3.4

    /**
     * The pixels a brace can touch, generously: the drawing region plus room for every
     * motion to move it. Used both to decide whether a brace intersects a repaint clip and
     * to ask for the next frame's repaint.
     */
    fun bounds(geometry: BraceGeometry, decorScale: Double): Rectangle {
        val scale = geometry.unit * decorScale
        val slack = geometry.unit * 0.6 + 4
        val x = geometry.centerX + REGION_LEFT * scale - slack
        val y = geometry.inkTop + REGION_TOP * scale - slack
        val w = REGION_WIDTH * scale + slack * 2
        val h = REGION_HEIGHT * scale + slack * 2
        return Rectangle(floor(x).toInt(), floor(y).toInt(), ceil(w).toInt() + 1, ceil(h).toInt() + 1)
    }

    /** Paints [brace] at [geometry]. Returns true if the appearance was animated. */
    fun paint(
        g: Graphics2D,
        brace: BraceInfo,
        geometry: BraceGeometry,
        appearance: GlyphAppearance,
        context: RenderContext,
        fonts: FontSource,
    ): Boolean {
        val settings = context.settings
        val g2 = g.create() as Graphics2D
        try {
            g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON)
            g2.setRenderingHint(RenderingHints.KEY_STROKE_CONTROL, RenderingHints.VALUE_STROKE_PURE)
            g2.setRenderingHint(RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY)

            if (appearance.opacity < 0.999) {
                g2.composite = AlphaComposite.SrcOver.derive(appearance.opacity.toFloat())
            }

            if (appearance.ops.isNotEmpty()) {
                g2.transform(motionTransform(appearance, geometry))
            }

            val baseFont = fonts.editorFont(appearance.bold, appearance.italic)
            val glyphFont = when {
                appearance.foreignFont -> fonts.foreignFont(baseFont)
                else -> fonts.fontFor(appearance.bodyText, baseFont)
            }

            val glyphPainter = { color: Color, dx: Double, dy: Double, alpha: Double ->
                val g3 = g2.create() as Graphics2D
                try {
                    g3.composite = AlphaComposite.SrcOver.derive(alpha.coerceIn(0.0, 1.0).toFloat())
                    g3.translate(dx, dy)
                    drawGlyph(g3, appearance.bodyText, glyphFont, color, geometry, appearance.bodyTransform, emptyList())
                } finally {
                    g3.dispose()
                }
            }

            // ---- shadow, the one backdrop effect that is a glyph rather than a shape ----

            if (appearance.shadow) {
                val g3 = g2.create() as Graphics2D
                try {
                    val offset = geometry.unit * 0.08
                    g3.translate(offset, offset)
                    drawGlyph(g3, appearance.bodyText, glyphFont, Color(0, 0, 0, 115), geometry, appearance.bodyTransform, emptyList())
                } finally {
                    g3.dispose()
                }
            }

            // ---- the glyph itself, with anything clipped to its stroke ----

            drawGlyph(g2, appearance.bodyText, glyphFont, appearance.color, geometry, appearance.bodyTransform, appearance.bands)

            // ---- the overlay ----

            var animated = appearance.animated
            if (settings.renderMode == RenderMode.Full) {
                val canvas = InkCanvas(
                    g2,
                    appearance.color,
                    brace.character,
                    brace.identity,
                    geometry.unit,
                    geometry.centerX,
                    geometry.inkTop,
                    settings.decorScale,
                    context.timeMs,
                    settings.enableMotion,
                    glyphPainter,
                )

                val traits = brace.traits
                for (effect in traits.effects) {
                    TraitDrawing.paint(effect, canvas)
                }

                TraitDrawing.paint(traits.costume, canvas)
                TraitDrawing.paint(traits.creature, canvas)
                if (traits.creature == TraitIds.CATGIRL && settings.tail) {
                    Creatures.catTail(canvas)
                }

                TraitDrawing.paint(traits.motion, canvas)
                animated = animated || canvas.animated
            }

            return animated
        } finally {
            g2.dispose()
        }
    }

    /**
     * The motion of the whole assembly, pivoting on a point of the ink rather than of the
     * cell — a cell-centred rotation would swing the glyph around empty space.
     */
    private fun motionTransform(appearance: GlyphAppearance, geometry: BraceGeometry): AffineTransform {
        val ox = geometry.centerX
        val oy = geometry.inkTop + geometry.unit * appearance.originY
        val at = AffineTransform()
        at.translate(ox, oy)

        for (op in appearance.ops) {
            when (op) {
                is Op.Rotate -> at.rotate(Math.toRadians(op.degrees))
                is Op.Scale -> at.scale(op.factor, op.factor)
                is Op.TranslateUnits -> at.translate(op.dx * geometry.unit, op.dy * geometry.unit)
                is Op.TranslatePx -> at.translate(op.dx, op.dy)
                is Op.TranslateCells -> at.translate(op.cells * geometry.cellWidth, 0.0)
            }
        }

        at.translate(-ox, -oy)
        return at
    }

    /**
     * Draws the glyph centred in its cell on the editor's baseline, so a substituted
     * character lines up with the surrounding code rather than floating.
     *
     * Bands are painted as clipped copies of the glyph's own outline — never as shapes
     * behind it — so a stocking is exactly as wide as the stroke it clothes at every font
     * size, automatically. Drawing a rectangle instead once meant an 8 px slab behind a 6 px
     * stroke, which read as a bar with a brace lost inside it.
     */
    private fun drawGlyph(
        g: Graphics2D,
        text: String,
        font: Font,
        color: Color,
        geometry: BraceGeometry,
        bodyTransform: BodyTransform?,
        bands: List<Band>,
    ) {
        val g2 = g.create() as Graphics2D
        try {
            g2.font = font
            val advance = g2.fontMetrics.stringWidth(text).toDouble()
            val x = geometry.cellLeft + (geometry.cellWidth - advance) / 2.0
            val y = geometry.baselineY

            if (bodyTransform != null) {
                val cx = geometry.cellLeft + geometry.cellWidth / 2.0
                val cy = geometry.inkTop + geometry.unit / 2.0
                val at = AffineTransform()
                when (bodyTransform) {
                    BodyTransform.Mirrored -> {
                        at.translate(cx, cy)
                        at.scale(-1.0, 1.0)
                        at.translate(-cx, -cy)
                    }

                    BodyTransform.UpsideDown -> {
                        at.translate(cx, cy)
                        at.rotate(Math.PI)
                        at.translate(-cx, -cy)
                    }

                    BodyTransform.Subscript -> {
                        val bottom = geometry.inkTop + geometry.unit
                        at.translate(cx, bottom)
                        at.scale(0.6, 0.6)
                        at.translate(-cx, -bottom)
                    }
                }

                g2.transform(at)
            }

            g2.color = color
            g2.drawString(text, x.toFloat(), y.toFloat())

            if (bands.isNotEmpty()) {
                val outline = font.createGlyphVector(g2.fontRenderContext, text).getOutline(x.toFloat(), y.toFloat())
                g2.clip(outline)

                val left = geometry.cellLeft - geometry.cellWidth
                val width = geometry.cellWidth * 3
                for (band in bands) {
                    val top = geometry.inkTop + band.from * geometry.unit
                    val bottom = geometry.inkTop + band.to * geometry.unit
                    if (bottom <= top) {
                        continue
                    }

                    g2.color = band.color
                    g2.fill(Rectangle2D.Double(left, top, width, bottom - top))
                }
            }
        } finally {
            g2.dispose()
        }
    }
}
