package identitybraces.render

import identitybraces.core.TraitIds
import java.awt.Color
import java.time.LocalDate
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.sin

/** What a build did recently, for [TraitIds.BUILD_REACTIVE]. */
enum class BuildReaction { Succeeded, Failed }

/**
 * The most recent build's verdict, for a short while afterwards.
 *
 * Null outside the reaction window, which is what keeps this from being a permanent change
 * of costume — a brace scrolled into view after the window has passed simply draws nothing.
 * Application-wide, because a build is not a property of one editor.
 */
object BuildReactions {
    private const val WINDOW_MS = 25_000L

    @Volatile
    private var reaction: BuildReaction? = null

    @Volatile
    private var until: Long = 0

    fun set(reaction: BuildReaction) {
        this.reaction = reaction
        until = System.currentTimeMillis() + WINDOW_MS
    }

    fun current(): BuildReaction? {
        return if (System.currentTimeMillis() < until) reaction else null
    }

    /** When the current reaction ends, so a repaint can be scheduled for the moment it does. */
    val endsAt: Long
        get() = until
}

/**
 * Effects that add something to the glyph.
 *
 * The other half of the effect layer — the ones that lean, dim or recolour the glyph rather
 * than drawing beside it — are not here. Those are transforms on the brace itself and live
 * in [BraceStyle], because a transform applied to the overlay alone would move the ears and
 * leave the brace behind.
 */
object Effects {
    private val SWEAT_BEAD = Color(0x8FD4F2)
    private val FLAME_TIP = Color(0xF2C14E)
    private val FLAME_MID = Color(0xF08B33)
    private val FLAME_CORE = Color(0xE8542B)
    private val CELEBRATION_GREEN = Color(0x4CC25E)

    private const val MITOSIS_SALT: ULong = 0x5D1177EuL

    fun register(map: MutableMap<String, Painter>) {
        map[TraitIds.FIRE] = ::fire
        map[TraitIds.UNDERLINE] = ::underline
        map[TraitIds.DISTRESSED] = ::distressed
        map[TraitIds.SEASONAL] = ::seasonal
        map[TraitIds.MITOSIS] = ::mitosis
        map[TraitIds.BUILD_REACTIVE] = ::buildReactive
    }

    /**
     * Flames licking up from the glyph, three tongues at different rates.
     *
     * Layered darkest-to-brightest so the core reads even when the whole thing is only a few
     * pixels across, and each tongue gets its own period so they never pulse in lockstep —
     * synchronised flames read as a flashing light rather than as fire.
     */
    private fun fire(c: InkCanvas) {
        val xs = doubleArrayOf(-0.26, 0.02, 0.28)
        val colors = arrayOf(FLAME_TIP, FLAME_MID, FLAME_CORE)
        val heights = doubleArrayOf(0.62, 0.86, 0.54)

        for (i in xs.indices) {
            val scale = 0.62 + c.swing(0.84 + i * 0.34) * 0.53
            c.triangle(colors[i], xs[i] - 0.17, 0.1, xs[i], 0.1 - heights[i] * scale, xs[i] + 0.17, 0.1)
        }
    }

    private fun underline(c: InkCanvas) {
        c.stroke(
            Color(0xD13B3B),
            0.08,
            c.p(-0.4, 1.12),
            c.p(-0.2, 1.04),
            c.p(0.0, 1.12),
            c.p(0.2, 1.04),
            c.p(0.4, 1.12),
        )
    }

    /**
     * Sweating and unsteady: two beads running off the glyph. The shake that goes with them
     * is applied to the brace, not here.
     *
     * The only trait that can arrive by predicate as well as by roll — the complexity warning
     * forces it onto anything nested past its threshold — so it has to read as *distress*
     * rather than as one more costume. The beads run downward and fade rather than pulsing
     * in place: at this size a shape that grows and shrinks reads as a blinking indicator
     * light, not as sweat.
     */
    private fun distressed(c: InkCanvas) {
        bead(c, 0.52, -0.04, 0.14, 0.4, 0.62)
        bead(c, -0.5, 0.12, 0.11, 0.3, 0.83)
    }

    private fun bead(c: InkCanvas, x: Double, y: Double, radius: Double, distance: Double, seconds: Double) {
        // With motion off the phase is zero, which leaves both beads sitting where they were
        // drawn at full opacity. That is the intended still frame: the warning still reads
        // without anything moving.
        val t = c.phase(seconds)
        c.group(0.95 * (1 - t))
        c.dot(SWEAT_BEAD, x, y + distance * t, radius)
        c.endGroup()
    }

    /** A seasonal accent: a pumpkin dot, a snowflake, a heart. */
    private fun seasonal(c: InkCanvas) {
        val accent = when (LocalDate.now().monthValue) {
            10 -> Color(0xF27A1A)
            12, 1 -> Color(0xCFE8F7)
            2 -> Color(0xE83B6B)
            else -> Color(0x6BC45A)
        }

        c.dot(accent, 0.42, -0.16, 0.13)
    }

    /**
     * Occasionally buds off a second brace, which drifts away and dissolves.
     *
     * Rare on purpose. The whole cycle is half a minute and the twin is invisible for nine
     * tenths of it: mitosis that happened every second would be a brace with two heads, not
     * a brace that occasionally divides. The phase is drawn from the identity so a screenful
     * of them do not all divide in unison.
     */
    private fun mitosis(c: InkCanvas) {
        val t = c.phase(29.0, MITOSIS_SALT)
        if (t < 0.86) {
            return
        }

        val progress = (t - 0.86) / 0.14
        val eased = 1 - (1 - progress) * (1 - progress)
        val angle = c.roll(MITOSIS_SALT) * PI * 2
        val reach = 0.85 * eased
        val opacity = if (progress < 0.3) progress / 0.3 else (1 - progress) / 0.7

        c.glyph(c.color, cos(angle) * reach, sin(angle) * reach, max(0.0, opacity) * 0.9)
    }

    /**
     * Celebrates a green build, for a short while afterwards. The sulk at a red one is a
     * lean and a grey applied to the brace in [BraceStyle]; only the sparks are drawn here.
     * Deliberately small: a build failing is already loud enough.
     */
    private fun buildReactive(c: InkCanvas) {
        if (BuildReactions.current() != BuildReaction.Succeeded) {
            return
        }

        val xs = doubleArrayOf(-0.34, 0.06, 0.4)
        for (i in xs.indices) {
            val t = c.phase(1.1 + i * 0.13)
            val eased = 1 - (1 - t) * (1 - t)
            c.group(1 - t)
            c.dot(CELEBRATION_GREEN, xs[i], -0.1 - 0.75 * eased, 0.11)
            c.endGroup()
        }
    }
}

/**
 * The two motion traits that draw something.
 *
 * Every other motion is a transform or an opacity on the brace as a whole, and those are in
 * [BraceStyle]. Put a spin here and the ears would rotate around a stationary brace.
 */
object Motions {
    fun register(map: MutableMap<String, Painter>) {
        map[TraitIds.SPARKLE] = ::sparkle
        map[TraitIds.SHIMMER] = ::shimmer
    }

    /** Four-point stars appearing and fading, on two different periods. */
    private fun sparkle(c: InkCanvas) {
        for (i in 0 until 2) {
            val x = if (i == 0) -0.44 else 0.44
            val y = if (i == 0) -0.1 else 0.62
            val twinkle = c.swing(2.2 + i * 1.0)

            c.group(twinkle)
            c.dot(Color(0xFFF3C4), x, y, 0.1)
            c.endGroup()
        }
    }

    /** A bright band sweeping down the stroke. */
    private fun shimmer(c: InkCanvas) {
        val t = c.phase(2.4)
        c.group(0.56)
        c.box(Color.WHITE, -0.5, -0.1 + t * 1.2, 1.0, 0.22)
        c.endGroup()
    }
}
