package identitybraces.render

import identitybraces.core.TraitIds
import java.awt.Color

typealias Painter = (InkCanvas) -> Unit

/**
 * Silhouettes built around the glyph.
 *
 * Every shape is authored in ink units — 0 is the ink's top-centre, 1 unit is its height —
 * so a creature drawn once holds its proportions at any font size or zoom. The coordinates
 * are the Visual Studio extension's, unchanged: they were tuned against real glyph ink and
 * there is nothing editor-specific about them.
 *
 * Nothing here is finer than about 0.15 units. At 10pt one unit is 12 px, so that is the
 * 2 px floor below which detail averages into its neighbours and turns to mush; it is the
 * constraint that killed the first attempt at the cat.
 */
object Creatures {
    val INNER_EAR: Color = Color(0xF7AED0)
    val PALE: Color = Color(0xEFE9F6)
    val DARK: Color = Color(0x23222C)
    private val GOLD = Color(0xF5D96B)
    private val AMBER = Color(0xE8A53C)
    private val RED = Color(0xD13B3B)

    private const val SLIME_SALT: ULong = 0x5117EuL
    private const val CTHULHU_SALT: ULong = 0xC7147EuL

    /** Creatures that make the glyph itself translucent rather than adding to it. */
    val glyphOpacity: Map<String, Double> = mapOf(TraitIds.GHOST to 0.55)

    fun register(map: MutableMap<String, Painter>) {
        map[TraitIds.CATGIRL] = ::cat
        map[TraitIds.BUNNY] = ::bunny
        map[TraitIds.DEVIL] = ::devil
        map[TraitIds.ANGEL] = ::angel
        map[TraitIds.FOX] = ::fox
        map[TraitIds.WOLF] = ::wolf
        map[TraitIds.FROG] = ::frog
        map[TraitIds.OWL] = ::owl
        map[TraitIds.MUSHROOM] = ::mushroom
        map[TraitIds.ROBOT] = ::robot
        map[TraitIds.BEE] = ::bee
        map[TraitIds.UNICORN] = ::unicorn
        map[TraitIds.VAMPIRE] = ::vampire
        map[TraitIds.CRAB] = ::crab
        map[TraitIds.BAT] = ::bat
        map[TraitIds.PENGUIN] = ::penguin
        map[TraitIds.CACTUS] = ::cactus
        map[TraitIds.SLIME] = ::slime
        map[TraitIds.SNAKE] = ::snake
        map[TraitIds.GHOST] = ::ghost
        map[TraitIds.WIZARD] = ::wizard
        map[TraitIds.DRAGON] = ::dragon
        map[TraitIds.SPIDER] = ::spider
        map[TraitIds.CTHULHU] = ::cthulhu
        map[TraitIds.PIRATE] = ::pirate
    }

    /** Two ears with a slanted base, so they read as ears rather than horns. */
    private fun cat(c: InkCanvas) {
        c.triangle(c.color, -0.44, 0.01, -0.31, -0.83, -0.06, -0.16)
        c.triangle(INNER_EAR, -0.33, -0.11, -0.26, -0.56, -0.13, -0.21)
        c.triangle(c.color, 0.44, 0.01, 0.31, -0.83, 0.06, -0.16)
        c.triangle(INNER_EAR, 0.33, -0.11, 0.26, -0.56, 0.13, -0.21)
    }

    /**
     * A tail curling off the bottom right. Kept out of [cat] because it is a setting of its
     * own: it overhangs the next cell slightly, and it is also the single thing that makes
     * the glyph read as a creature rather than as a brace wearing something.
     */
    fun catTail(c: InkCanvas) {
        c.curve(c.color, 0.12, c.p(0.16, 0.94), c.p(0.52, 1.02), c.p(0.58, 0.68), c.p(0.42, 0.58))
    }

    /** Tall and narrow, with the right ear folded at the tip. */
    private fun bunny(c: InkCanvas) {
        c.triangle(c.color, -0.3, 0.02, -0.24, -1.15, -0.05, 0.0)
        c.triangle(INNER_EAR, -0.24, -0.1, -0.21, -0.85, -0.12, -0.08)
        c.triangle(c.color, 0.3, 0.02, 0.2, -0.95, 0.05, 0.0)
        c.stroke(c.color, 0.13, c.p(0.2, -0.95), c.p(0.36, -1.08))
    }

    private fun devil(c: InkCanvas) {
        c.curve(c.color, 0.14, c.p(-0.34, 0.0), c.p(-0.4, -0.45), c.p(-0.3, -0.62), c.p(-0.16, -0.66))
        c.curve(c.color, 0.14, c.p(0.34, 0.0), c.p(0.4, -0.45), c.p(0.3, -0.62), c.p(0.16, -0.66))
        c.curve(c.color, 0.13, c.p(0.3, 0.98), c.p(0.62, 1.02), c.p(0.66, 0.72), c.p(0.5, 0.6))
        c.triangle(c.color, 0.4, 0.62, 0.62, 0.58, 0.48, 0.44)
    }

    private fun angel(c: InkCanvas) {
        c.ring(GOLD, 0.0, -0.62, 0.3, 0.11)
    }

    private fun fox(c: InkCanvas) {
        c.triangle(c.color, -0.42, 0.0, -0.36, -0.92, -0.04, -0.14)
        c.triangle(PALE, -0.32, -0.1, -0.29, -0.62, -0.14, -0.18)
        c.triangle(c.color, 0.42, 0.0, 0.36, -0.92, 0.04, -0.14)
        c.triangle(PALE, 0.32, -0.1, 0.29, -0.62, 0.14, -0.18)
        c.curve(c.color, 0.2, c.p(0.26, 1.0), c.p(0.7, 1.02), c.p(0.72, 0.62), c.p(0.46, 0.5))
    }

    private fun wolf(c: InkCanvas) {
        c.triangle(c.color, -0.46, 0.02, -0.44, -0.72, -0.1, -0.12)
        c.triangle(c.color, 0.46, 0.02, 0.44, -0.72, 0.1, -0.12)
        c.dot(c.color, 0.0, -0.2, 0.1)
    }

    private fun frog(c: InkCanvas) {
        c.dot(c.color, -0.24, -0.2, 0.2)
        c.dot(c.color, 0.24, -0.2, 0.2)
        c.dot(DARK, -0.24, -0.2, 0.08)
        c.dot(DARK, 0.24, -0.2, 0.08)
    }

    private fun owl(c: InkCanvas) {
        c.dot(PALE, -0.22, 0.28, 0.24)
        c.dot(PALE, 0.22, 0.28, 0.24)
        c.dot(DARK, -0.22, 0.28, 0.11)
        c.dot(DARK, 0.22, 0.28, 0.11)
        c.triangle(AMBER, -0.08, 0.44, 0.08, 0.44, 0.0, 0.62)
    }

    private fun mushroom(c: InkCanvas) {
        c.box(RED, -0.42, -0.44, 0.84, 0.34, 0.17)
        c.dot(PALE, -0.18, -0.3, 0.09)
        c.dot(PALE, 0.16, -0.24, 0.07)
    }

    private fun robot(c: InkCanvas) {
        c.stroke(c.color, 0.11, c.p(0.0, -0.1), c.p(0.0, -0.6))

        // A hard on/off rather than a fade: a bulb that dims smoothly reads as a glow, and at
        // this size a glow is just a smudge.
        val lit = c.phase(1.4) < 0.5
        c.group(if (lit) 1.0 else 0.2)
        c.dot(Color(0xE03B3B), 0.0, -0.7, 0.14)
        c.endGroup()

        c.box(c.color, -0.5, 0.3, 0.14, 0.14)
        c.box(c.color, 0.36, 0.3, 0.14, 0.14)
    }

    private fun bee(c: InkCanvas) {
        val stripe = Color(0xE8C030)
        c.box(stripe, -0.36, 0.34, 0.72, 0.14)
        c.box(stripe, -0.36, 0.62, 0.72, 0.14)
        c.dot(PALE, -0.46, 0.16, 0.16)
        c.dot(PALE, 0.46, 0.16, 0.16)
    }

    private fun unicorn(c: InkCanvas) {
        c.triangle(GOLD, -0.13, -0.02, 0.0, -0.92, 0.13, -0.02)
        c.stroke(INNER_EAR, 0.09, c.p(-0.07, -0.28), c.p(0.07, -0.4))
        c.stroke(INNER_EAR, 0.09, c.p(-0.04, -0.52), c.p(0.06, -0.62))
    }

    private fun vampire(c: InkCanvas) {
        c.triangle(PALE, -0.24, 0.86, -0.1, 0.86, -0.17, 1.14)
        c.triangle(PALE, 0.1, 0.86, 0.24, 0.86, 0.17, 1.14)
    }

    private fun crab(c: InkCanvas) {
        c.stroke(c.color, 0.14, c.p(-0.36, 0.5), c.p(-0.62, 0.36))
        c.triangle(c.color, -0.62, 0.44, -0.86, 0.26, -0.6, 0.22)
        c.stroke(c.color, 0.14, c.p(0.36, 0.5), c.p(0.62, 0.36))
        c.triangle(c.color, 0.62, 0.44, 0.86, 0.26, 0.6, 0.22)
    }

    private fun bat(c: InkCanvas) {
        c.triangle(c.color, -0.3, 0.2, -0.86, 0.06, -0.62, 0.52)
        c.triangle(c.color, 0.3, 0.2, 0.86, 0.06, 0.62, 0.52)
    }

    private fun penguin(c: InkCanvas) {
        c.triangle(AMBER, -0.1, 0.26, 0.1, 0.26, 0.0, 0.46)
        c.triangle(c.color, -0.34, 0.52, -0.6, 0.74, -0.32, 0.8)
        c.triangle(c.color, 0.34, 0.52, 0.6, 0.74, 0.32, 0.8)
    }

    private fun cactus(c: InkCanvas) {
        for (i in 0 until 3) {
            val y = 0.16 + i * 0.28
            c.stroke(c.color, 0.09, c.p(-0.34, y), c.p(-0.52, y - 0.08))
            c.stroke(c.color, 0.09, c.p(0.34, y), c.p(0.52, y - 0.08))
        }

        c.dot(Color(0xE85C9A), 0.0, -0.16, 0.13)
    }

    /** A drip that forms, falls and resets. */
    private fun slime(c: InkCanvas) {
        val t = c.phase(2.2, SLIME_SALT)
        c.group(1 - t)
        c.dot(c.color, 0.1, 1.02 + t * 0.9, 0.13)
        c.endGroup()
    }

    private fun snake(c: InkCanvas) {
        val tongue = Color(0xE03B6B)
        val flick = 1 - c.swing(1.8) * 0.9
        c.group(flick)
        c.stroke(tongue, 0.09, c.p(0.3, 0.44), c.p(0.62, 0.44))
        c.endGroup()
        c.triangle(tongue, 0.62, 0.38, 0.78, 0.32, 0.62, 0.5)
    }

    private fun ghost(c: InkCanvas) {
        c.dot(c.color, -0.2, 1.02, 0.1)
        c.dot(c.color, 0.06, 1.06, 0.1)
        c.dot(c.color, 0.3, 1.02, 0.1)
    }

    private fun wizard(c: InkCanvas) {
        val felt = Color(0x5A3FA8)
        c.triangle(felt, -0.44, -0.14, 0.0, -1.24, 0.44, -0.14)
        c.box(felt, -0.56, -0.2, 1.12, 0.13, 0.06)
        c.dot(GOLD, 0.06, -0.72, 0.1)
    }

    private fun dragon(c: InkCanvas) {
        c.curve(c.color, 0.13, c.p(-0.34, 0.0), c.p(-0.52, -0.34), c.p(-0.36, -0.62), c.p(-0.1, -0.58))
        c.curve(c.color, 0.13, c.p(0.34, 0.0), c.p(0.52, -0.34), c.p(0.36, -0.62), c.p(0.1, -0.58))
        c.triangle(c.color, 0.3, 0.3, 0.8, 0.12, 0.66, 0.62)
    }

    private fun spider(c: InkCanvas) {
        for (i in 0 until 3) {
            val y = 0.24 + i * 0.26
            c.stroke(c.color, 0.08, c.p(-0.3, y), c.p(-0.72, y - 0.16))
            c.stroke(c.color, 0.08, c.p(0.3, y), c.p(0.72, y - 0.16))
        }
    }

    /** Four tentacles, each on its own period so they never wave in lockstep. */
    private fun cthulhu(c: InkCanvas) {
        for (i in 0 until 4) {
            val x = -0.3 + i * 0.2
            val sway = (c.swing(1.8 + i * 0.2, CTHULHU_SALT) - 0.5) * 0.12
            c.curve(
                c.color,
                0.1,
                c.p(x, 0.9),
                c.p(x - 0.06 + sway, 1.1),
                c.p(x + 0.08 + sway * 2, 1.2),
                c.p(x + sway * 3, 1.34),
            )
        }
    }

    private fun pirate(c: InkCanvas) {
        c.box(DARK, -0.46, 0.18, 0.92, 0.15, 0.04)
        c.stroke(DARK, 0.07, c.p(-0.46, 0.18), c.p(-0.62, 0.06))
        c.dot(RED, 0.0, -0.1, 0.12)
    }
}
