package identitybraces.settings

import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.editor.colors.EditorColorsScheme
import com.intellij.openapi.editor.ex.util.EditorUIUtil
import identitybraces.core.BraceInfo
import identitybraces.core.BraceKind
import identitybraces.core.BraceTraits
import identitybraces.core.Hash
import identitybraces.core.Palette
import identitybraces.editor.EditorFonts
import identitybraces.editor.Session
import identitybraces.render.BraceGeometry
import identitybraces.render.BraceRenderer
import identitybraces.render.BraceStyle
import identitybraces.render.GlyphMetrics
import identitybraces.render.RenderContext
import identitybraces.render.StyleSettings
import java.awt.Dimension
import java.awt.Graphics
import java.awt.Graphics2D
import java.awt.RenderingHints
import javax.swing.JComponent
import javax.swing.Timer
import kotlin.math.max

/** One brace to preview: a character wearing some traits in some colour. */
class PreviewBrace(val character: Char, val traits: BraceTraits, val colorIndex: Int, val identity: ULong)

/**
 * Draws sample braces with the real renderer — the same code the editor draws with — so
 * the preview cannot drift from what you will actually get.
 *
 * Two are used: one magnified and animated, showing what a single trait *is*; one at editor
 * size and deliberately held still, showing the density a weight table produces. A hundred
 * and twenty simultaneous animations in a settings dialog is not a preview, it is a stress
 * test.
 */
class BracePreview(
    private val magnification: Float,
    private val animated: Boolean,

    /** Character cells per brace, so the strip does not crowd and the magnified pair has room. */
    private val cellsPerBrace: Int,
) : JComponent() {
    var braces: List<PreviewBrace> = emptyList()
        set(value) {
            field = value
            revalidate()
            repaint()
        }

    var styleSettings: StyleSettings? = null

    private val scheme: EditorColorsScheme
        get() = EditorColorsManager.getInstance().globalScheme

    private val fonts = EditorFonts({ scheme }, { this }, magnification)

    private val clock = Timer(66) { if (isShowing) repaint() }

    init {
        isOpaque = true
    }

    override fun addNotify() {
        super.addNotify()
        if (animated) {
            clock.start()
        }
    }

    override fun removeNotify() {
        clock.stop()
        super.removeNotify()
    }

    private fun cellWidth(): Int = getFontMetrics(fonts.editorFont(bold = false, italic = false)).charWidth('{')

    private fun lineHeight(): Int {
        val metrics = getFontMetrics(fonts.editorFont(bold = false, italic = false))
        // Room above for ears and below for tails, scaled with the glyph.
        return (metrics.height * 2.2).toInt()
    }

    override fun getPreferredSize(): Dimension {
        val cell = cellWidth() * cellsPerBrace
        val line = lineHeight()
        val perRow = max(1, if (width > 0) width / cell else 40)
        val rows = max(1, (braces.size + perRow - 1) / perRow)
        return Dimension(cell * minOf(braces.size, perRow).coerceAtLeast(2), line * rows)
    }

    override fun paintComponent(g: Graphics) {
        val g2 = g as Graphics2D
        g2.color = scheme.defaultBackground
        g2.fillRect(0, 0, width, height)

        EditorUIUtil.setupAntialiasing(g2)
        g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON)

        val settings = styleSettings ?: return
        val font = fonts.editorFont(bold = false, italic = false)
        val metrics = getFontMetrics(font)
        val cell = cellWidth() * cellsPerBrace
        val line = lineHeight()
        val perRow = max(1, width / cell)
        val now = if (animated) Session.timeMs else 0L

        for ((index, brace) in braces.withIndex()) {
            val row = index / perRow
            val column = index % perRow
            val cellLeft = column * cell + (cell - cellWidth()) / 2.0
            val lineTop = row * line + (line - metrics.height) / 2.0
            val baseline = lineTop + metrics.ascent
            val ink = GlyphMetrics.measure(font, fonts.renderContext, brace.character)

            val geometry = BraceGeometry(
                cellLeft = cellLeft,
                cellWidth = cellWidth().toDouble(),
                lineTop = lineTop,
                lineHeight = metrics.height.toDouble(),
                baselineY = baseline,
                ink = ink,
            )

            val color = BraceColorKeys.resolve(scheme, brace.colorIndex)

            if (!brace.traits.isDrawn) {
                g2.font = font
                g2.color = color
                g2.drawString(brace.character.toString(), cellLeft.toFloat(), baseline.toFloat())
                continue
            }

            val info = BraceInfo(0, brace.character, kindOf(brace.character), isOpener(brace.character))
            info.identity = brace.identity
            info.colorIndex = brace.colorIndex
            info.traits = brace.traits

            val context = RenderContext(
                settings = settings,
                timeMs = now,
                sessionMinutes = 0.0,
                caretLine = -1,
                caretColumn = -1,
                line = 0,
                column = 0,
                dim = false,
                ageMs = 10_000,
                unitPx = geometry.unit,
            )

            val appearance = BraceStyle.compute(info, color, context)
            try {
                BraceRenderer.paint(g2, info, geometry, appearance, context, fonts)
            } catch (e: Exception) {
                // A preview that fails to draw one brace draws the rest.
            }
        }
    }

    companion object {
        private fun kindOf(c: Char): BraceKind = BraceKind.classify(c)?.first ?: BraceKind.Curly

        private fun isOpener(c: Char): Boolean = BraceKind.classify(c)?.second ?: true

        /** A stable colour for a preview identity, the way the scanner would pick one. */
        fun colorIndexOf(identity: ULong): Int = Hash.toIndex(identity, Palette.COUNT)
    }
}
