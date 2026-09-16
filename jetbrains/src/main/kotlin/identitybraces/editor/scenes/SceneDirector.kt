package identitybraces.editor.scenes

import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.diagnostic.debug
import identitybraces.core.BraceMap
import identitybraces.core.SceneCasting
import identitybraces.editor.EditorBraces
import identitybraces.editor.Session
import identitybraces.settings.IdentityBracesSettings
import java.awt.Graphics2D
import java.awt.Rectangle
import javax.swing.Timer
import org.jetbrains.annotations.TestOnly

/**
 * Owns the performances that need more than one cell.
 *
 * **A scene never survives a layout.** Its props are placed from line geometry that is only
 * valid until the next change, so typing, scrolling, folding or re-theming strikes the set
 * immediately and the performance is simply lost. That is a deliberate trade, inherited from
 * the Visual Studio extension: the alternative is repositioning props on every change, and
 * unlike a brace, a prop that vanishes mid-flight costs nothing, because a few seconds later
 * there will be another one.
 *
 * One scene at a time, per editor, on a slow timer. These are meant to be caught out of the
 * corner of an eye, not watched.
 */
class SceneDirector(private val owner: EditorBraces) : Disposable {
    private class Performance(val scene: Scene, val cast: List<SceneActor>, val startedAt: Long, val bounds: Rectangle)

    private val scenes: List<Scene> = listOf(TableFlipScene(), SwapPlacesScene(), FireBrigadeScene())
    private val timer = Timer(8000) { tick() }
    private var performance: Performance? = null
    private var tick: ULong = 0uL

    private val candidateIndices = ArrayList<Int>()
    private val subjectIndices = ArrayList<Int>()

    init {
        timer.isRepeats = true
        reschedule()
    }

    override fun dispose() {
        timer.stop()
        strike()
    }

    /**
     * Starts or stops the clock according to whether any scene could play at all. Every
     * scene is motion by definition, so motion off means the timer should not exist rather
     * than tick and find nothing to do.
     */
    fun reschedule() {
        val settings = IdentityBracesSettings.instance
        val wanted = settings.enabled && settings.enableMotion && scenes.any { settings.traitWeight(it.traitId) > 0 }

        if (!wanted) {
            strike()
            timer.stop()
            return
        }

        val interval = settings.sceneIntervalSeconds.coerceIn(1, 600) * 1000
        if (timer.delay != interval) {
            timer.delay = interval
            timer.initialDelay = interval
        }

        if (!timer.isRunning) {
            timer.start()
        }
    }

    /** Ends whatever is playing. */
    fun strike() {
        val current = performance ?: return
        performance = null
        LOG.debug { "scene: ${current.scene.traitId} struck after ${Session.timeMs - current.startedAt} ms" }
        owner.editor.contentComponent.repaint(current.bounds)
    }

    /** The pixels the current performance owns, for the frame clock. Null when idle. */
    val activeBounds: Rectangle?
        get() = performance?.bounds

    /** Plays [traitId]'s scene right now if it can cast one, for tests. */
    @TestOnly
    fun playNow(traitId: String): Boolean {
        performance = null
        val index = scenes.indexOfFirst { it.traitId == traitId }
        if (index < 0) {
            return false
        }

        // The tick picks the scene round-robin; line it up so this tick lands on the one asked for.
        while ((tick % scenes.size.toULong()).toInt() != index) {
            tick++
        }

        tryPlay()
        return performance != null
    }

    private fun tick() {
        tick++

        val editor = owner.editor
        if (editor.isDisposed) {
            timer.stop()
            return
        }

        // One at a time. Two scenes running at once on the same screen stops reading as an
        // event and starts reading as a fault.
        if (performance != null) {
            return
        }

        if (!editor.contentComponent.isShowing) {
            return
        }

        try {
            tryPlay()
        } catch (e: Exception) {
            // Geometry moved between the map and the measurement. There will be another tick.
            strike()
        }
    }

    private fun tryPlay() {
        val settings = IdentityBracesSettings.instance
        val scene = scenes[(tick % scenes.size.toULong()).toInt()]
        if (settings.traitWeight(scene.traitId) <= 0) {
            return
        }

        val map = owner.map()
        if (map.size == 0) {
            return
        }

        val range = owner.visibleRange()
        SceneCasting.findCandidates(map, scene.traitId, range.first, range.last, candidateIndices)
        if (candidateIndices.isEmpty()) {
            LOG.debug { "scene: no ${scene.traitId} candidates among ${map.size} braces in $range" }
            return
        }

        val actors = resolve(map, candidateIndices)

        val subjectTrait = scene.subjectTraitId
        val subjects = if (subjectTrait == null) {
            emptyList()
        } else {
            SceneCasting.findCandidates(map, subjectTrait, range.first, range.last, subjectIndices)
            if (subjectIndices.isEmpty()) {
                return
            }

            resolve(map, subjectIndices)
        }

        // Casting nobody is the ordinary outcome — every candidate this tick sat on a crowded
        // line, or no fire was visible — not a failure. The next tick draws again.
        val cast = scene.tryCast(actors, subjects)
        if (cast.isNullOrEmpty()) {
            LOG.debug { "scene: ${scene.traitId} sat this tick out; ${actors.size} actors measured, room ${actors.map { "L${it.roomLeft}/R${it.roomRight}" }}" }
            return
        }

        val bounds = scene.bounds(cast)
        performance = Performance(scene, cast, Session.timeMs, bounds)
        LOG.debug { "scene: ${scene.traitId} playing at offset ${cast[0].position}, cast of ${cast.size}, bounds $bounds" }
        owner.editor.contentComponent.repaint(bounds)
        owner.wakeClock()
    }

    /**
     * Measures a handful of eligible braces, as a consecutive run. Consecutive rather than
     * independently random, and that is load-bearing for `swapplaces`: consecutive entries
     * in the brace map are usually neighbours on a line, whereas two braces chosen
     * independently from a screenful almost never share one.
     */
    private fun resolve(map: BraceMap, indices: List<Int>): List<SceneActor> {
        if (indices.isEmpty()) {
            return emptyList()
        }

        val start = SceneCasting.choose(indices, tick)
        val offset = indices.indexOf(start).coerceAtLeast(0)
        val actors = ArrayList<SceneActor>(CANDIDATES_PER_TICK)

        for (i in 0 until minOf(CANDIDATES_PER_TICK, indices.size)) {
            resolveActor(map, indices[(offset + i) % indices.size])?.let { actors.add(it) }
        }

        return actors
    }

    /**
     * Turns a brace index into everything a scene needs to know about where it is. Read
     * from live geometry at the moment of playing, never cached.
     */
    private fun resolveActor(map: BraceMap, index: Int): SceneActor? {
        val brace = map[index]
        val document = owner.document
        if (brace.position < 0 || brace.position >= document.textLength) {
            return null
        }

        val geometry = owner.geometryOf(brace) ?: return null
        val line = document.getLineNumber(brace.position)
        val lineStart = document.getLineStartOffset(line)
        val lineEnd = document.getLineEndOffset(line)
        val lineText = document.immutableCharSequence.subSequence(lineStart, lineEnd)
        val column = brace.position - lineStart

        return SceneActor(
            position = brace.position,
            lineStart = lineStart,
            column = column,
            character = brace.character,
            cellLeft = geometry.cellLeft,
            cellWidth = geometry.cellWidth,
            textTop = geometry.lineTop,
            textHeight = geometry.lineHeight,
            baselineY = geometry.baselineY,
            roomRight = SceneCasting.roomRightOf(lineText, column, ROOM_LIMIT),
            roomLeft = SceneCasting.roomLeftOf(lineText, column, ROOM_LIMIT),
            color = owner.baseColor(brace),
            identity = brace.identity,
        )
    }

    /** Paints the current performance, if any, and ends it once its time is up. */
    fun paint(g: Graphics2D) {
        val current = performance ?: return
        val elapsed = Session.timeMs - current.startedAt
        if (elapsed >= current.scene.durationMs) {
            performance = null
            return
        }

        if (!IdentityBracesSettings.instance.enableMotion) {
            performance = null
            return
        }

        val progress = elapsed.toDouble() / current.scene.durationMs
        try {
            current.scene.paint(g, current.cast, progress, owner.glyphFont())
        } catch (e: Exception) {
            LOG.debug("scene: ${current.scene.traitId} failed to paint and was struck", e)
            performance = null
        }
    }

    companion object {
        private val LOG = Logger.getInstance(SceneDirector::class.java)

        /** Longest run of blank columns worth counting either side of a brace. */
        private const val ROOM_LIMIT = 24

        /**
         * How many eligible braces to measure before giving up on a tick. Measuring every
         * eligible brace on screen to then use one is work thrown away, but measuring only
         * one means a tick fails whenever that one happens to sit on a crowded line.
         */
        private const val CANDIDATES_PER_TICK = 4
    }
}
