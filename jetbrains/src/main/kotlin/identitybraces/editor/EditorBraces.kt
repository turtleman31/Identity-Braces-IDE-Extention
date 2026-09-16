package identitybraces.editor

import com.intellij.openapi.Disposable
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.EditorKind
import com.intellij.openapi.editor.VisualPosition
import com.intellij.openapi.editor.colors.EditorColorsListener
import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.editor.event.CaretEvent
import com.intellij.openapi.editor.event.CaretListener
import com.intellij.openapi.editor.event.DocumentEvent
import com.intellij.openapi.editor.event.DocumentListener
import com.intellij.openapi.editor.event.VisibleAreaEvent
import com.intellij.openapi.editor.event.VisibleAreaListener
import com.intellij.openapi.editor.ex.EditorEx
import com.intellij.openapi.editor.ex.FoldingListener
import com.intellij.openapi.editor.ex.RangeHighlighterEx
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.editor.markup.CustomHighlighterOrder
import com.intellij.openapi.editor.markup.CustomHighlighterRenderer
import com.intellij.openapi.editor.markup.HighlighterLayer
import com.intellij.openapi.editor.markup.HighlighterTargetArea
import com.intellij.openapi.editor.markup.RangeHighlighter
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.util.Key
import com.intellij.openapi.application.ModalityState
import com.intellij.util.Alarm
import com.intellij.util.SingleAlarm
import org.jetbrains.annotations.TestOnly
import identitybraces.core.BraceInfo
import identitybraces.core.BraceMap
import identitybraces.core.BracePair
import identitybraces.editor.scenes.SceneDirector
import identitybraces.render.BraceGeometry
import identitybraces.render.BraceRenderer
import identitybraces.render.BraceStyle
import identitybraces.render.GlyphMetrics
import identitybraces.render.RenderContext
import identitybraces.render.StyleSettings
import identitybraces.settings.BraceColorKeys
import identitybraces.settings.BraceColorMode
import identitybraces.settings.IdentityBracesSettings
import identitybraces.settings.IdentityBracesSettingsListener
import identitybraces.settings.RenderMode
import java.awt.Color
import java.awt.Font
import java.awt.Graphics
import java.awt.Graphics2D
import java.awt.Rectangle
import javax.swing.Timer
import kotlin.math.max
import kotlin.math.min

/**
 * Everything Identity Braces does to one editor.
 *
 * Three kinds of highlighter, all on the editor's own markup model:
 *
 * - one per visible plain brace, carrying nothing but a foreground colour — the equivalent
 *   of the Visual Studio classifier's tags. Cheap, and only ever as many as fit on screen
 *   plus a margin;
 * - one over the whole document with a renderer painted *after the text*: the adornment
 *   layer, where every personality brace is drawn. The real character underneath is painted
 *   transparent by its plain highlighter, so nothing is ever drawn twice;
 * - one over the whole document painted *after the background*, for the indent guides,
 *   which belong behind the code rather than across it.
 *
 * Everything is re-derived from live geometry on every paint. Nothing remembers where a
 * brace was: the Visual Studio extension learned twice that a position trusted from
 * creation time is a position that is wrong by the next layout.
 */
class EditorBraces private constructor(val editor: EditorEx) : Disposable {
    val document = editor.document
    private val cache = BraceMapCache.of(document)
    private val fonts = EditorFonts(editor)

    private class PlainEntry(val highlighter: RangeHighlighter, val look: String)

    private val plain = HashMap<Int, PlainEntry>()
    private var layer: RangeHighlighter? = null
    private var guides: RangeHighlighter? = null

    // Any modality, deliberately: with the default the refresh waits for every dialog to
    // close, so the editors behind Settings would not follow an Apply, and a file opened
    // behind a prompt would sit uncoloured until the prompt was answered.
    private val refreshAlarm = SingleAlarm(::refresh, 30, this, Alarm.ThreadToUse.SWING_THREAD, ModalityState.any())
    private val clock = Timer(66) { tick() }

    /** Rects of the braces that moved this frame, for the clock to repaint. */
    private val animatedRects = ArrayList<Rectangle>()

    /** When each brace was first drawn here, for `typewriter`. Keyed by identity and offset. */
    private val firstSeen = HashMap<Long, Long>()

    private var caretLine = -1
    private var caretColumn = -1
    private var caretOffset = -1
    private var spotlight: BracePair? = null

    val guidePainter = IndentGuidePainter(this)

    /** The multi-brace performances. Struck by anything that moves the text. */
    val director = SceneDirector(this)

    init {
        Disposer.register(this, director)

        document.addDocumentListener(object : DocumentListener {
            override fun documentChanged(event: DocumentEvent) {
                director.strike()
                scheduleRefresh()
            }
        }, this)

        editor.scrollingModel.addVisibleAreaListener(object : VisibleAreaListener {
            override fun visibleAreaChanged(e: VisibleAreaEvent) {
                val old = e.oldRectangle
                val new = e.newRectangle
                if (old == null || old.y != new.y || old.height != new.height) {
                    director.strike()
                    scheduleRefresh()
                }
            }
        }, this)

        editor.caretModel.addCaretListener(object : CaretListener {
            override fun caretPositionChanged(event: CaretEvent) = onCaretMoved()
        }, this)

        editor.foldingModel.addListener(object : FoldingListener {
            override fun onFoldProcessingEnd() {
                director.strike()
                scheduleRefresh()
            }
        }, this)

        val bus = ApplicationManager.getApplication().messageBus.connect(this)
        bus.subscribe(IdentityBracesSettingsListener.TOPIC, IdentityBracesSettingsListener { onSettingsChanged() })
        bus.subscribe(EditorColorsManager.TOPIC, EditorColorsListener { scheduleRefresh() })

        BraceHover.install(this)
        updateCaret()
        scheduleRefresh()
    }

    // ---- lifecycle ----

    override fun dispose() {
        clock.stop()
        clearAll()
    }

    fun scheduleRefresh() {
        if (!editor.isDisposed) {
            refreshAlarm.cancelAndRequest()
        }
    }

    /** The current map, scanning if the document or the settings moved since the last one. */
    fun map(): BraceMap = cache.get(document)

    /** A synchronous refresh, for tests that cannot wait on the alarm. */
    @TestOnly
    fun refreshNow() {
        refreshAlarm.cancel()
        refresh()
    }

    /** How many plain highlighters are live, for tests. */
    @TestOnly
    fun plainHighlighterCount(): Int = plain.size

    private fun onSettingsChanged() {
        firstSeen.clear()
        clock.delay = frameInterval()
        director.reschedule()
        scheduleRefresh()
    }

    /** The editor's plain font at its current size, for scenes that draw a likeness of a glyph. */
    fun glyphFont(): Font = fonts.editorFont(bold = false, italic = false)

    /** Makes sure the frame clock is running — a scene has just started. */
    fun wakeClock() {
        if (!clock.isRunning && IdentityBracesSettings.instance.enableMotion) {
            clock.delay = frameInterval()
            clock.initialDelay = clock.delay
            clock.start()
        }
    }

    // ---- the refresh: highlighters for what is on screen ----

    private fun refresh() {
        if (editor.isDisposed) {
            return
        }

        val settings = IdentityBracesSettings.instance
        if (!settings.enabled) {
            clearAll()
            editor.contentComponent.repaint()
            return
        }

        val map = map()
        updateCaret()
        spotlight = if (settings.scopeSpotlight && caretOffset >= 0) map.enclosingPair(caretOffset) else null

        ensureLayers(settings)
        syncPlain(map, settings)
        repaintVisible()
    }

    private fun clearAll() {
        for (entry in plain.values) {
            entry.highlighter.dispose()
        }

        plain.clear()
        layer?.dispose()
        layer = null
        guides?.dispose()
        guides = null
        animatedRects.clear()
        director.strike()
        clock.stop()
    }

    /** The two whole-document highlighters, recreated only if they stopped covering the text. */
    private fun ensureLayers(settings: IdentityBracesSettings) {
        val wantLayer = settings.renderMode != RenderMode.Plain
        val wantGuides = settings.indentGuides

        layer = ensureLayer(layer, wantLayer, CustomHighlighterOrder.AFTER_TEXT) { g -> paintLayer(g) }
        guides = ensureLayer(guides, wantGuides, CustomHighlighterOrder.AFTER_BACKGROUND) { g -> guidePainter.paint(g) }
    }

    private fun ensureLayer(
        existing: RangeHighlighter?,
        wanted: Boolean,
        order: CustomHighlighterOrder,
        painter: (Graphics2D) -> Unit,
    ): RangeHighlighter? {
        if (!wanted) {
            existing?.dispose()
            return null
        }

        val length = document.textLength
        if (existing != null && existing.isValid && existing.startOffset == 0 && existing.endOffset == length) {
            return existing
        }

        existing?.dispose()
        val created = editor.markupModel.addRangeHighlighter(0, length, HighlighterLayer.ADDITIONAL_SYNTAX, null, HighlighterTargetArea.EXACT_RANGE)
        (created as? RangeHighlighterEx)?.let {
            // Greedy at both ends, so text typed at the very start or end of the file stays
            // inside the range that paints it.
            it.isGreedyToLeft = true
            it.isGreedyToRight = true
        }

        created.customRenderer = object : CustomHighlighterRenderer {
            override fun getOrder(): CustomHighlighterOrder = order

            override fun paint(editor: Editor, highlighter: RangeHighlighter, g: Graphics) {
                if (!editor.isDisposed) {
                    painter(g as Graphics2D)
                }
            }
        }

        return created
    }

    /**
     * Brings the plain highlighters into line with the braces currently on screen.
     *
     * Highlighters are range markers, so after an edit they have already moved with their
     * text; re-keying by where they are now means a brace that merely shifted keeps its
     * highlighter, and only the ones whose colour changed or that scrolled out are touched.
     */
    private fun syncPlain(map: BraceMap, settings: IdentityBracesSettings) {
        val range = visibleRange(marginLines = 24)

        val current = HashMap<Int, PlainEntry>(plain.size * 2)
        for (entry in plain.values) {
            val h = entry.highlighter
            if (h.isValid && h.endOffset - h.startOffset == 1 && !current.containsKey(h.startOffset)) {
                current[h.startOffset] = entry
            } else {
                h.dispose()
            }
        }

        val wanted = HashMap<Int, String>()
        val pair = spotlight
        map.forEachIn(range.first, range.last) { i, brace ->
            wanted[brace.position] = lookOf(i, brace, pair, settings)
        }

        plain.clear()
        for ((offset, entry) in current) {
            val look = wanted[offset]
            if (look != null && look == entry.look) {
                plain[offset] = entry
            } else {
                entry.highlighter.dispose()
            }
        }

        for ((offset, look) in wanted) {
            if (!plain.containsKey(offset) && offset < document.textLength) {
                plain[offset] = PlainEntry(addPlain(offset, look), look)
            }
        }
    }

    /**
     * What a plain highlighter should look like, as a key: a palette entry, a hidden brace,
     * or a dimmed one. Two braces with the same key are interchangeable.
     */
    private fun lookOf(index: Int, brace: BraceInfo, pair: BracePair?, settings: IdentityBracesSettings): String {
        val hidden = brace.traits.isDrawn && settings.renderMode != RenderMode.Plain
        if (hidden) {
            return "hidden"
        }

        val dim = pair != null && index != pair.openIndex && index != pair.closeIndex
        val mono = settings.colorMode == BraceColorMode.Monochrome
        return when {
            mono && dim -> "mono-dim"
            mono -> "mono"
            dim -> "dim:${brace.colorIndex}"
            else -> "key:${brace.colorIndex}"
        }
    }

    private fun addPlain(offset: Int, look: String): RangeHighlighter {
        return createPlain(offset, look).also { it.putUserData(OWNED, true) }
    }

    private fun createPlain(offset: Int, look: String): RangeHighlighter {
        val markup = editor.markupModel
        val layerIndex = PLAIN_LAYER

        if (look.startsWith("key:")) {
            // Key-based, so the colour follows the scheme — retune it under Color Scheme and
            // every open editor picks it up without a repaint of ours.
            val key = BraceColorKeys.keys[look.substring(4).toInt()]
            return markup.addRangeHighlighter(key, offset, offset + 1, layerIndex, HighlighterTargetArea.EXACT_RANGE)
        }

        val color = when {
            look == "hidden" -> Color(0, 0, 0, 0)
            look == "mono" -> editor.colorsScheme.defaultForeground
            look == "mono-dim" -> withAlpha(editor.colorsScheme.defaultForeground, dimAlpha())
            else -> withAlpha(BraceColorKeys.resolve(editor.colorsScheme, look.substring(4).toInt()), dimAlpha())
        }

        val attributes = TextAttributes(color, null, null, null, Font.PLAIN)
        return markup.addRangeHighlighter(offset, offset + 1, layerIndex, attributes, HighlighterTargetArea.EXACT_RANGE)
    }

    private fun dimAlpha(): Double = IdentityBracesSettings.instance.spotlightDimPercent / 100.0

    private fun withAlpha(color: Color, alpha: Double): Color {
        return Color(color.red, color.green, color.blue, (alpha.coerceIn(0.0, 1.0) * 255).toInt())
    }

    // ---- geometry ----

    /** The offsets of the visual lines on screen, plus a margin either side. */
    fun visibleRange(marginLines: Int = 0): IntRange {
        val area = editor.scrollingModel.visibleArea
        val margin = marginLines * editor.lineHeight
        val top = max(0, area.y - margin)
        val bottom = area.y + area.height + margin

        val startLine = editor.yToVisualLine(top)
        val endLine = editor.yToVisualLine(bottom)

        val start = editor.logicalPositionToOffset(editor.visualToLogicalPosition(VisualPosition(startLine, 0)))
        val end = max(EditorUtil.getVisualLineEndOffset(editor, endLine), start)
        return start..min(end + 1, document.textLength)
    }

    /** Where a brace's cell is right now, or null if it is folded away. */
    fun geometryOf(brace: BraceInfo): BraceGeometry? {
        if (brace.position >= document.textLength || editor.foldingModel.isOffsetCollapsed(brace.position)) {
            return null
        }

        val point = editor.offsetToXY(brace.position, true, false)
        val cellWidth = EditorUtil.charWidth(brace.character, Font.PLAIN, editor)
        val ink = GlyphMetrics.measure(fonts.editorFont(bold = false, italic = false), fonts.renderContext, brace.character)

        return BraceGeometry(
            cellLeft = point.x.toDouble(),
            cellWidth = cellWidth.toDouble(),
            lineTop = point.y.toDouble(),
            lineHeight = editor.lineHeight.toDouble(),
            baselineY = (point.y + editor.ascent).toDouble(),
            ink = ink,
        )
    }

    /** The colour a brace wears before any personality gets hold of it. */
    fun baseColor(brace: BraceInfo): Color {
        val settings = IdentityBracesSettings.instance
        return if (settings.colorMode == BraceColorMode.Monochrome) {
            editor.colorsScheme.defaultForeground
        } else {
            BraceColorKeys.resolve(editor.colorsScheme, brace.colorIndex)
        }
    }

    // ---- the adornment layer ----

    private fun styleSettings(settings: IdentityBracesSettings) = StyleSettings(
        enableMotion = settings.enableMotion,
        cycleSeconds = settings.cycleSeconds,
        spotlightDim = settings.spotlightDimPercent / 100.0,
        stocking = settings.stocking,
        tail = settings.catgirlTail,
        renderMode = settings.renderMode,
        decorScale = settings.decorScalePercent / 100.0,
    )

    private fun paintLayer(g: Graphics2D) {
        val settings = IdentityBracesSettings.instance
        if (!settings.enabled || settings.renderMode == RenderMode.Plain) {
            return
        }

        val map = map()
        val style = styleSettings(settings)
        val range = visibleRange()
        val now = Session.timeMs
        val minutes = Session.minutes
        val pair = spotlight
        animatedRects.clear()

        // Every drawn brace on screen is painted, not only the ones inside the clip: Java2D
        // discards the rest cheaply, and it means the list of what is moving is always
        // complete — a partial repaint of one brace must not forget the others exist.
        map.forEachIn(range.first, range.last) { i, brace ->
            if (!brace.traits.isDrawn) {
                return@forEachIn
            }

            val geometry = geometryOf(brace) ?: return@forEachIn
            val position = editor.offsetToLogicalPosition(brace.position)
            val key = brace.identity.toLong() xor (brace.position.toLong() shl 40)
            val born = firstSeen.getOrPut(key) { now }

            val context = RenderContext(
                settings = style,
                timeMs = now,
                sessionMinutes = minutes,
                caretLine = caretLine,
                caretColumn = caretColumn,
                line = position.line,
                column = position.column,
                dim = pair != null && i != pair.openIndex && i != pair.closeIndex,
                ageMs = now - born,
                unitPx = geometry.unit,
            )

            val appearance = BraceStyle.compute(brace, baseColor(brace), context)
            val animated = try {
                BraceRenderer.paint(g, brace, geometry, appearance, context, fonts)
            } catch (e: Exception) {
                // One brace that will not draw must not take the paint pass down with it.
                false
            }

            if (animated) {
                animatedRects.add(BraceRenderer.bounds(geometry, style.decorScale))
            }
        }

        if (firstSeen.size > 8192) {
            firstSeen.clear()
        }

        // Scenes go on top of the braces: a thrown table passing behind the glyph that threw
        // it would look like a rendering fault rather than a throw.
        director.paint(g)
        director.activeBounds?.let { animatedRects.add(it) }

        updateClock(settings)
    }

    // ---- the clock ----

    private fun frameInterval(): Int = max(16, 1000 / IdentityBracesSettings.instance.animationFrameRate.coerceIn(1, 60))

    private fun updateClock(settings: IdentityBracesSettings) {
        val wanted = settings.enableMotion && animatedRects.isNotEmpty()
        if (wanted && !clock.isRunning) {
            clock.delay = frameInterval()
            clock.initialDelay = clock.delay
            clock.start()
        } else if (!wanted && clock.isRunning) {
            clock.stop()
        }
    }

    private fun tick() {
        if (editor.isDisposed) {
            clock.stop()
            return
        }

        if (animatedRects.isEmpty()) {
            clock.stop()
            return
        }

        // A hidden tab keeps its clock but paints nothing; the tick costs one check.
        val component = editor.contentComponent
        if (!component.isShowing) {
            return
        }

        for (rect in animatedRects) {
            component.repaint(rect)
        }
    }

    fun repaintVisible() {
        val area = editor.scrollingModel.visibleArea
        editor.contentComponent.repaint(area)
    }

    // ---- the caret ----

    private fun updateCaret() {
        val caret = editor.caretModel.primaryCaret
        val position = caret.logicalPosition
        caretLine = position.line
        caretColumn = position.column
        caretOffset = caret.offset
    }

    private fun onCaretMoved() {
        if (editor.isDisposed) {
            return
        }

        val previousLine = caretLine
        updateCaret()

        val settings = IdentityBracesSettings.instance
        if (settings.scopeSpotlight) {
            // Peek rather than scan: mid-edit the map is stale, and the refresh the edit
            // already scheduled will place the spotlight from a fresh one.
            val map = cache.peek(document)
            if (map != null) {
                val next = map.enclosingPair(caretOffset)
                val current = spotlight
                if (next?.openIndex != current?.openIndex || next?.closeIndex != current?.closeIndex) {
                    scheduleRefresh()
                }
            }
        }

        // The two traits that watch the caret are drawn from its line and column, so the
        // line it left and the line it arrived on both need a fresh frame.
        repaintLine(previousLine)
        if (caretLine != previousLine) {
            repaintLine(caretLine)
        }
    }

    private fun repaintLine(line: Int) {
        if (line < 0 || line >= document.lineCount) {
            return
        }

        val y = editor.logicalPositionToXY(com.intellij.openapi.editor.LogicalPosition(line, 0)).y
        val height = editor.lineHeight
        val area = editor.scrollingModel.visibleArea
        // Two lines of slack either side: ears reach above, tails below.
        editor.contentComponent.repaint(Rectangle(area.x, y - height * 2, area.width, height * 5))
    }

    companion object {
        private val KEY = Key.create<EditorBraces>("identitybraces.controller")

        /**
         * Above warnings and errors, so a brace keeps its colour under a squiggle, and below
         * the selection, so a selected brace inverts with the text around it.
         */
        const val PLAIN_LAYER: Int = HighlighterLayer.SELECTION - 1

        /** Marks the highlighters this plugin owns; the layer alone is shared with the brace matcher. */
        val OWNED: Key<Boolean> = Key.create("identitybraces.owned")

        fun of(editor: Editor): EditorBraces? = editor.getUserData(KEY)

        fun attach(editor: Editor) {
            if (editor.editorKind == EditorKind.MAIN_EDITOR || editor.editorKind == EditorKind.DIFF) {
                attachRegardless(editor)
            }
        }

        /** Attaches to an editor of any kind — the test fixture's editors are untyped. */
        @TestOnly
        fun attachForTest(editor: Editor): EditorBraces? {
            attachRegardless(editor)
            return of(editor)
        }

        private fun attachRegardless(editor: Editor) {
            if (editor !is EditorEx || of(editor) != null) {
                return
            }

            val controller = EditorBraces(editor)
            editor.putUserData(KEY, controller)
            EditorUtil.disposeWithEditor(editor, controller)
        }

        fun detach(editor: Editor) {
            val controller = of(editor) ?: return
            editor.putUserData(KEY, null)
            Disposer.dispose(controller)
        }

        /** Every attached editor, for the actions that change something global. */
        fun refreshAll() {
            for (editor in com.intellij.openapi.editor.EditorFactory.getInstance().allEditors) {
                of(editor)?.scheduleRefresh()
            }
        }
    }
}
