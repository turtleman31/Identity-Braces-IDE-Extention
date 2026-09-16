package identitybraces.editor

import com.intellij.openapi.editor.VisualPosition
import com.intellij.openapi.editor.ex.util.EditorUtil
import identitybraces.core.BraceMap
import identitybraces.settings.IdentityBracesSettings
import java.awt.AlphaComposite
import java.awt.BasicStroke
import java.awt.Graphics2D
import java.awt.RenderingHints
import java.awt.geom.Line2D
import kotlin.math.round

/**
 * Draws a vertical guide down the inside of every multi-line pair, in that pair's colour.
 *
 * Everything here is derived from *visible* lines only. A pair can easily span more screens
 * than the monitor has, so instead of one tall line from opener to closer, each visible line
 * contributes its own segment, and a guide that runs off the top of the screen is just a
 * segment on every line down to the bottom of it.
 *
 * Which pairs are open at the top of the screen comes from the parent chain — a walk up the
 * nesting rather than back through the file, so scrolling to the end of a large document
 * costs the same as scrolling to the start of it.
 */
class IndentGuidePainter(private val owner: EditorBraces) {
    private class Guide(val openLine: Int, val closeLine: Int, val indentX: Int, val braceIndex: Int)

    private val guides = ArrayList<Guide>()

    fun paint(g: Graphics2D) {
        val settings = IdentityBracesSettings.instance
        if (!settings.enabled || !settings.indentGuides) {
            return
        }

        val editor = owner.editor
        val document = owner.document
        val map = owner.map()
        if (map.size == 0) {
            return
        }

        val range = owner.visibleRange()
        collect(map, range.first, range.last)
        if (guides.isEmpty()) {
            return
        }

        val g2 = g.create() as Graphics2D
        try {
            g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_OFF)
            g2.composite = AlphaComposite.SrcOver.derive((settings.indentGuideOpacityPercent / 100.0).toFloat())

            // Dotted rather than solid. A solid line at full palette saturation competes with
            // the code for attention; the guide is meant to be found when looked for and
            // ignored otherwise.
            g2.stroke = BasicStroke(1f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER, 1f, floatArrayOf(1f, 2f), 0f)

            val clip = g2.clipBounds
            val firstVisual = editor.yToVisualLine(clip.y)
            val lastVisual = editor.yToVisualLine(clip.y + clip.height)

            for (visual in firstVisual..lastVisual) {
                val logicalLine = editor.visualToLogicalPosition(VisualPosition(visual, 0)).line
                if (logicalLine >= document.lineCount) {
                    break
                }

                val yRange = editor.visualLineToYRange(visual)
                val top = yRange[0].toDouble()
                val bottom = yRange[1].toDouble()

                for (guide in guides) {
                    // The guide covers the inside of the pair only: it starts below the line
                    // the opener is on and stops above the line the closer is on, so it never
                    // runs alongside either brace it belongs to.
                    if (logicalLine <= guide.openLine || logicalLine >= guide.closeLine) {
                        continue
                    }

                    // Snapped to a device pixel and offset by a half so a one-pixel stroke
                    // lands on one pixel instead of straddling two and rendering grey.
                    val x = round(guide.indentX.toDouble()) + 0.5
                    g2.color = owner.baseColor(map[guide.braceIndex])
                    g2.draw(Line2D.Double(x, top, x, bottom))
                }
            }
        } finally {
            g2.dispose()
        }
    }

    /**
     * Works out which pairs cross the visible region: the ones already open at the top of
     * the screen, plus the ones that start on it.
     */
    private fun collect(map: BraceMap, viewStart: Int, viewEnd: Int) {
        guides.clear()

        var innermost = -1
        var pair = map.enclosingPair(viewStart)
        if (pair != null) {
            innermost = pair.openIndex
            while (pair != null) {
                addGuide(map, pair.openIndex, pair.closeIndex)
                pair = map.parentPair(pair.openIndex)
            }
        }

        map.forEachIn(viewStart, viewEnd) { i, brace ->
            // Skipping the innermost enclosing pair: when the top of the screen falls exactly
            // on an opening brace, that pair both encloses the position and opens within the
            // range, and would otherwise be drawn twice.
            if (brace.isOpen && brace.isMatched && brace.partnerIndex > i && i != innermost) {
                addGuide(map, i, brace.partnerIndex)
            }
        }
    }

    private fun addGuide(map: BraceMap, openIndex: Int, closeIndex: Int) {
        val document = owner.document
        val editor = owner.editor
        val opener = map[openIndex]
        val closer = map[closeIndex]

        if (opener.position >= document.textLength || closer.position >= document.textLength) {
            return
        }

        val openLine = document.getLineNumber(opener.position)
        val closeLine = document.getLineNumber(closer.position)

        // A pair that opens and closes on one line has no inside to draw down.
        if (closeLine <= openLine) {
            return
        }

        // The x of the guide is the indent of the line its opener is on, half a column in so
        // it sits inside its indent step rather than on the boundary shared with the level
        // above. Reading it from the opener's own x would put a guide for `if (x) {` far to
        // the right of the block it encloses.
        val lineStart = document.getLineStartOffset(openLine)
        val lineEnd = document.getLineEndOffset(openLine)
        var indentEnd = lineStart
        val text = document.immutableCharSequence
        while (indentEnd < lineEnd && (text[indentEnd] == ' ' || text[indentEnd] == '\t')) {
            indentEnd++
        }

        val indentX = editor.offsetToXY(indentEnd, true, false).x + EditorUtil.getPlainSpaceWidth(editor) / 2
        guides.add(Guide(openLine, closeLine, indentX, openIndex))
    }
}
