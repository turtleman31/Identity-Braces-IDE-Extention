package identitybraces.render

import identitybraces.core.BraceInfo
import identitybraces.core.BraceTraits
import identitybraces.core.ColorRing
import identitybraces.core.Hash
import identitybraces.core.TraitIds
import identitybraces.settings.RenderMode
import identitybraces.settings.StockingStyle
import java.awt.Color
import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.floor
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin

/** One step of a brace's motion transform, applied around the glyph's origin. */
sealed class Op {
    class Rotate(val degrees: Double) : Op()
    class Scale(val factor: Double) : Op()

    /** A shift in ink units — the same at every font size. */
    class TranslateUnits(val dx: Double, val dy: Double) : Op()

    /** A shift in device pixels, for the jitters that are meant to be sub-pixel. */
    class TranslatePx(val dx: Double, val dy: Double) : Op()

    /** A shift in character cells, for the trait that edges along its own line. */
    class TranslateCells(val cells: Double) : Op()
}

/** How a body trait reshapes the glyph itself, separately from the motion of the whole. */
enum class BodyTransform { Mirrored, UpsideDown, Subscript }

/** A recolouring of part of the glyph's own stroke, in ink units from the ink's top. */
class Band(val color: Color, val from: Double, val to: Double)

/** The settings a frame needs, snapshotted so a paint pass reads one consistent set. */
class StyleSettings(
    val enableMotion: Boolean,
    val cycleSeconds: Int,
    val spotlightDim: Double,
    val stocking: StockingStyle,
    val tail: Boolean,
    val renderMode: RenderMode,
    val decorScale: Double,
)

/** What the world looks like at the moment of drawing. */
class RenderContext(
    val settings: StyleSettings,

    /** Milliseconds on the animation clock. */
    val timeMs: Long,

    /** Minutes since the IDE opened, for the traits that wear down over a session. */
    val sessionMinutes: Double,

    /** Where the caret is, for the two traits that react to it. -1 when there is none. */
    val caretLine: Int,
    val caretColumn: Int,

    /** This brace's own line and column, so the caret traits can compare. */
    val line: Int,
    val column: Int,

    /** Dimmed by the scope spotlight. */
    val dim: Boolean,

    /** Milliseconds since this brace was first drawn in this editor, for `typewriter`. */
    val ageMs: Long,

    /** Ink height in device pixels, for the stripe-thickness floors. */
    val unitPx: Double,
)

/** The complete appearance of one drawn brace, before any pixels. */
class GlyphAppearance(
    val color: Color,
    val opacity: Double,
    val ops: List<Op>,

    /** Where rotation and scale pivot, as a fraction of the ink's height below its top. */
    val originY: Double,

    val bodyText: String,
    val bodyTransform: BodyTransform?,
    val foreignFont: Boolean,
    val bold: Boolean,
    val italic: Boolean,
    val shadow: Boolean,
    val bands: List<Band>,

    /** True when this appearance depends on the clock and has to be redrawn next frame. */
    val animated: Boolean,
)

/**
 * Turns a scanned brace into the appearance the renderer paints.
 *
 * The same decisions as the VS Code port's `computeStyle`, minus the quantisation: a CSS
 * decoration had to repeat a small set of rules, but an immediate-mode canvas can simply be
 * asked again next frame. The motion here is therefore smooth, like the WPF original's.
 */
object BraceStyle {
    private val SULK_GREY = Color(0x8A8A92)
    private val STOCKING_BODY = Color(0x7B7490)
    private val STOCKING_WELT = Color(0xC6BFD4)

    private const val TILT_SALT: ULong = 0x7117EDuL
    private const val DRUNK_SALT: ULong = 0xD204070uL
    private const val DRIFT_SALT: ULong = 0x0D21F7uL
    private const val WAVE_SALT: ULong = 0x7AFE12uL
    private const val CYCLE_SALT: ULong = 0xC17C1EuL
    private const val EMPHASIS_SALT: ULong = 0xB01D17uL

    /** How long a `typewriter` brace takes to arrive, once. */
    private const val TYPEWRITER_MS = 700.0

    fun compute(brace: BraceInfo, baseColor: Color, context: RenderContext): GlyphAppearance {
        val settings = context.settings
        val traits = brace.traits
        val motion = settings.enableMotion

        val ops = ArrayList<Op>(4)
        var animated = false
        var color = baseColor
        var opacity = 1.0
        var originY = 0.75

        // ---- motion that recolours ----

        if (traits.motion == TraitIds.COLOUR_CYCLE && motion) {
            val offset = roll(brace, CYCLE_SALT)
            val turns = context.timeMs / (settings.cycleSeconds * 1000.0) + offset
            color = Color(ColorRing.at(turns % 1.0), false)
            animated = true
        }

        // ---- effects that recolour or wear the glyph down ----

        if (traits.hasEffect(TraitIds.NOCTURNAL)) {
            val tired = min(1.0, context.sessionMinutes / 20)
            opacity *= 1 - tired * 0.45
            ops.add(Op.Rotate(tired * 10))
        }

        if (traits.hasEffect(TraitIds.BUILD_REACTIVE) && BuildReactions.current() == BuildReaction.Failed) {
            color = SULK_GREY
            ops.add(Op.Rotate(9.0))
            ops.add(Op.TranslateUnits(0.0, 0.1))
        }

        val creatureOpacity = traits.creature?.let { Creatures.glyphOpacity[it] }
        if (creatureOpacity != null) {
            opacity *= creatureOpacity
        }

        // ---- motion that moves ----

        if (motion) {
            animated = applyMotion(traits.motion, brace, context, ops) || animated
            if (traits.motion == TraitIds.SPIN || traits.motion == TraitIds.FLIP) {
                originY = 0.5
            }

            val fade = motionOpacity(traits.motion, context.timeMs)
            if (fade < 1) {
                opacity *= fade
                animated = true
            } else if (traits.motion == TraitIds.BLINK || traits.motion == TraitIds.FLICKER) {
                // Full brightness is a frame of the animation like any other, and the
                // appearance still has to be recomputed on the next one.
                animated = true
            }

            if (traits.motion == TraitIds.TYPEWRITER && context.ageMs < TYPEWRITER_MS) {
                // Fades in once on first appearance. The one trait that needs the renderer
                // to remember something — which VS Code's stateless decorations could not.
                opacity *= (context.ageMs / TYPEWRITER_MS).coerceIn(0.0, 1.0)
                animated = true
            }
        }

        // ---- effects that lean or shake ----

        if (traits.hasEffect(TraitIds.TILTED)) {
            ops.add(Op.Rotate(-16 + roll(brace, TILT_SALT) * 32))
        }

        if (traits.hasEffect(TraitIds.DRUNK)) {
            val lean = min(24.0, context.sessionMinutes * 1.2)
            val direction = if (roll(brace, DRUNK_SALT) < 0.5) -1 else 1
            ops.add(Op.Rotate(lean * direction))
        }

        if (traits.hasEffect(TraitIds.GRAVITY)) {
            ops.add(Op.TranslateUnits(0.0, min(0.3, context.sessionMinutes * 0.05)))
        }

        if (traits.hasEffect(TraitIds.DISTRESSED) && motion) {
            // Small and fast. A wide, slow wobble reads as a personality; a nervous vibration
            // reads as a brace that is not coping, which is the point.
            ops.add(Op.Rotate(if (floor(context.timeMs / 110.0).toLong() % 2 == 0L) -4.5 else 4.5))
            animated = true
        }

        // ---- the two traits that watch the caret ----

        if (traits.hasEffect(TraitIds.STAGE_FRIGHT) && context.caretLine == context.line) {
            // Almost out, not out: a brace that is genuinely invisible is the one failure
            // this extension treats as unacceptable.
            opacity *= 0.18
        }

        if (traits.hasEffect(TraitIds.FLEE_CURSOR) && context.caretLine == context.line) {
            val distance = context.column - context.caretColumn
            if (abs(distance) <= 6) {
                val push = (if (distance >= 0) 1 else -1) * (1 - abs(distance) / 6.0) * 0.55
                ops.add(Op.TranslateCells(push))
            }
        }

        // ---- the glyph itself ----

        val bodyText = TraitDrawing.resolveBody(traits.body, brace.character)
        val bodyTransform = when (traits.body) {
            TraitIds.MIRRORED -> BodyTransform.Mirrored
            TraitIds.UPSIDE_DOWN -> BodyTransform.UpsideDown
            TraitIds.SUBSCRIPT -> BodyTransform.Subscript
            else -> null
        }

        var bold = false
        var italic = false
        if (traits.hasEffect(TraitIds.BOLD_ITALIC)) {
            if (roll(brace, EMPHASIS_SALT) < 0.5) bold = true else italic = true
        }

        val bands = if (settings.renderMode == RenderMode.Plain) emptyList() else bands(traits, color, settings, context.unitPx)

        if (context.dim) {
            opacity *= settings.spotlightDim
        }

        return GlyphAppearance(
            color = color,
            opacity = opacity.coerceIn(0.0, 1.0),
            ops = ops,
            originY = originY,
            bodyText = bodyText,
            bodyTransform = bodyTransform,
            foreignFont = traits.body == TraitIds.FOREIGN_FONT,
            bold = bold,
            italic = italic,
            shadow = traits.hasEffect(TraitIds.SHADOW),
            bands = bands,
            animated = animated,
        )
    }

    private fun roll(brace: BraceInfo, salt: ULong): Double = Hash.toUnitInterval(Hash.mix(brace.identity, salt))

    private fun phase(timeMs: Long, seconds: Double, offset: Double = 0.0): Double {
        val raw = (timeMs / (seconds * 1000.0) + offset) % 1.0
        return if (raw < 0) raw + 1 else raw
    }

    private fun applyMotion(motion: String?, brace: BraceInfo, context: RenderContext, ops: MutableList<Op>): Boolean {
        val t = context.timeMs

        when (motion) {
            TraitIds.WOBBLE -> {
                ops.add(Op.Rotate(sin(phase(t, 1.6) * PI * 2) * 9))
                return true
            }

            TraitIds.BOUNCE -> {
                // Up sharply, down slowly, which is what makes it read as a hop rather than
                // as a float.
                val p = phase(t, 1.4)
                val lift = if (p < 0.5) easeOut(p * 2) else 1 - easeOut((p - 0.5) * 2)
                ops.add(Op.TranslateUnits(0.0, -0.28 * lift))
                return true
            }

            TraitIds.BREATHE -> {
                ops.add(Op.Scale(1 + sin(phase(t, 2.6) * PI * 2) * 0.06))
                return true
            }

            TraitIds.HEARTBEAT -> {
                val p = phase(t, 1.4)
                val scale = when {
                    p < 0.1 -> 1 + p * 1.8
                    p < 0.2 -> 1.18 - (p - 0.1) * 1.8
                    p < 0.3 -> 1 + (p - 0.2) * 1.4
                    p < 0.42 -> 1.14 - (p - 0.3) * 1.167
                    else -> 1.0
                }
                ops.add(Op.Scale(scale))
                return true
            }

            TraitIds.SHIVER -> {
                // Discrete steps, not a smooth path: sub-pixel jitter is the point.
                val offset = when ((t / 80) % 3) {
                    0L -> -0.6
                    1L -> 0.7
                    else -> -0.3
                }
                ops.add(Op.TranslatePx(offset, offset))
                return true
            }

            TraitIds.SPIN -> {
                ops.add(Op.Rotate(phase(t, 4.5) * 360))
                return true
            }

            TraitIds.FLIP -> {
                ops.add(Op.Rotate(if (phase(t, 3.4) < 0.5) 0.0 else 180.0))
                return true
            }

            TraitIds.GLITCH -> {
                val p = phase(t, 2.0)
                val jump = when {
                    p < 0.82 -> 0.0
                    p < 0.86 -> 2.0
                    p < 0.9 -> -1.5
                    else -> 0.0
                }
                ops.add(Op.TranslatePx(jump, 0.0))
                return true
            }

            TraitIds.DRIFT -> {
                val period = 3 + roll(brace, DRIFT_SALT) * 2
                val x = sin(phase(t, period) * PI * 2) * 1.6
                val y = kotlin.math.cos(phase(t, period * 1.37) * PI * 2) * 1.2
                ops.add(Op.TranslatePx(x, y))
                return true
            }

            TraitIds.WAVE -> {
                // The phase comes from the brace's own identity, so neighbours move in
                // sequence and the motion appears to travel along the line.
                val p = phase(t, 0.9, roll(brace, WAVE_SALT))
                ops.add(Op.TranslateUnits(0.0, -0.24 * abs(sin(p * PI))))
                return true
            }

            else -> return false
        }
    }

    /** Opacity-only motions, kept apart from the transforms because they multiply rather than compose. */
    private fun motionOpacity(motion: String?, timeMs: Long): Double = when (motion) {
        TraitIds.BLINK -> {
            val p = phase(timeMs, 3.1)
            if (p < 0.88) 1.0 else if (p < 0.94) 0.15 else 1.0
        }

        TraitIds.FLICKER -> {
            val p = phase(timeMs, 0.9)
            when {
                p < 0.22 -> 1 - p * 1.27
                p < 0.44 -> 0.72 + (p - 0.22) * 1.05
                p < 0.68 -> 0.95 - (p - 0.44) * 1.25
                else -> 0.65 + (p - 0.68) * 1.09
            }
        }

        else -> 1.0
    }

    /**
     * The recolourings of the brace's own stroke, clipped to the glyph so a stocking is
     * exactly as wide as the stroke it clothes at every font size — which is the whole reason
     * it reads as clothing.
     */
    private fun bands(traits: BraceTraits, color: Color, settings: StyleSettings, unitPx: Double): List<Band> {
        if (traits.costume == TraitIds.THIGH_HIGHS && settings.stocking != StockingStyle.Off) {
            // A stripe under two device pixels averages into its neighbours and reads as a
            // smear, so the welt has a floor expressed in ink units.
            val legTop = 0.55
            val welt = max(2 / max(unitPx, 1.0), 0.17)

            return when (settings.stocking) {
                StockingStyle.TwoTone -> listOf(Band(STOCKING_BODY, legTop, 1.6))
                StockingStyle.Banded -> {
                    val thin = max(1.3 / max(unitPx, 1.0), 0.11)
                    listOf(
                        Band(STOCKING_WELT, legTop, legTop + thin),
                        Band(STOCKING_BODY, legTop + thin, legTop + thin * 2),
                        Band(STOCKING_WELT, legTop + thin * 2, legTop + thin * 3),
                        Band(STOCKING_BODY, legTop + thin * 3, 1.6),
                    )
                }
                else -> listOf(
                    Band(STOCKING_WELT, legTop, legTop + welt),
                    Band(STOCKING_BODY, legTop + welt, 1.6),
                )
            }
        }

        if (traits.hasEffect(TraitIds.GRADIENT_FILL)) {
            return listOf(Band(darken(color, 0.45), 0.5, 1.6))
        }

        return emptyList()
    }

    private fun darken(color: Color, factor: Double): Color {
        return Color((color.red * factor).toInt(), (color.green * factor).toInt(), (color.blue * factor).toInt())
    }

    private fun easeOut(t: Double): Double = 1 - (1 - t) * (1 - t)
}
