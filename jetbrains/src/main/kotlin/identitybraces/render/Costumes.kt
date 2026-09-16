package identitybraces.render

import identitybraces.core.TraitIds
import java.awt.Color

/**
 * Things worn over the glyph rather than replacing it.
 *
 * The thigh highs are the exception, and they are not here: they are a recolouring of the
 * brace's own stroke rather than a shape drawn near it, so they live with the glyph in
 * [BraceRenderer]. Drawing a rectangle instead once meant an 8 px slab behind a 6 px stroke,
 * which read as a bar with a brace lost inside it — the same mistake is available here and
 * worth not making twice.
 */
object Costumes {
    private val DARK = Creatures.DARK
    private val PALE = Creatures.PALE
    private val GOLD = Color(0xF5D96B)
    private val RED = Color(0xD13B3B)

    fun register(map: MutableMap<String, Painter>) {
        map[TraitIds.TOP_HAT] = ::topHat
        map[TraitIds.CROWN] = ::crown
        map[TraitIds.SUNGLASSES] = ::sunglasses
        map[TraitIds.SCARF] = ::scarf
        map[TraitIds.BOWTIE] = ::bowtie
        map[TraitIds.PARTY_HAT] = ::partyHat
        map[TraitIds.MOUSTACHE] = ::moustache
        map[TraitIds.BEANIE] = ::beanie
        map[TraitIds.FLOWER_CROWN] = ::flowerCrown
        map[TraitIds.HEADPHONES] = ::headphones
        map[TraitIds.BANDAGE] = ::bandage
        map[TraitIds.NECKTIE] = ::necktie
        map[TraitIds.WINGS] = ::wings
        map[TraitIds.ARMOUR] = ::armour
        map[TraitIds.CAPE] = ::cape
        map[TraitIds.BACKPACK] = ::backpack
        map[TraitIds.MONOCLE] = ::monocle
    }

    private fun topHat(c: InkCanvas) {
        c.box(DARK, -0.56, -0.22, 1.12, 0.12, 0.05)
        c.box(DARK, -0.34, -0.78, 0.68, 0.58, 0.04)
        c.box(RED, -0.34, -0.36, 0.68, 0.12)
    }

    private fun crown(c: InkCanvas) {
        c.triangle(GOLD, -0.4, -0.06, -0.3, -0.52, -0.14, -0.06)
        c.triangle(GOLD, -0.14, -0.06, 0.0, -0.62, 0.14, -0.06)
        c.triangle(GOLD, 0.14, -0.06, 0.3, -0.52, 0.4, -0.06)
        c.box(GOLD, -0.42, -0.1, 0.84, 0.12)
    }

    private fun sunglasses(c: InkCanvas) {
        c.box(DARK, -0.5, 0.2, 0.4, 0.2, 0.05)
        c.box(DARK, 0.1, 0.2, 0.4, 0.2, 0.05)
        c.box(DARK, -0.12, 0.26, 0.24, 0.06)
    }

    private fun scarf(c: InkCanvas) {
        val wool = Color(0xD13B5A)
        c.box(wool, -0.44, 0.46, 0.88, 0.16, 0.04)
        c.curve(wool, 0.14, c.p(0.36, 0.54), c.p(0.66, 0.66), c.p(0.62, 0.9), c.p(0.44, 1.0))
    }

    private fun bowtie(c: InkCanvas) {
        c.triangle(RED, -0.44, 0.34, -0.44, 0.7, -0.06, 0.52)
        c.triangle(RED, 0.44, 0.34, 0.44, 0.7, 0.06, 0.52)
        c.dot(RED, 0.0, 0.52, 0.09)
    }

    private fun partyHat(c: InkCanvas) {
        c.triangle(Color(0xE03B9A), -0.34, -0.1, 0.0, -0.94, 0.34, -0.1)
        c.box(GOLD, -0.24, -0.44, 0.48, 0.09)
        c.dot(PALE, 0.0, -0.98, 0.11)
    }

    private fun moustache(c: InkCanvas) {
        c.curve(DARK, 0.16, c.p(-0.44, 0.48), c.p(-0.22, 0.34), c.p(-0.06, 0.5), c.p(0.0, 0.52))
        c.curve(DARK, 0.16, c.p(0.44, 0.48), c.p(0.22, 0.34), c.p(0.06, 0.5), c.p(0.0, 0.52))
    }

    private fun beanie(c: InkCanvas) {
        val wool = Color(0x3B7AD1)
        c.box(wool, -0.44, -0.58, 0.88, 0.44, 0.2)
        c.box(PALE, -0.48, -0.2, 0.96, 0.13, 0.05)
        c.dot(PALE, 0.0, -0.66, 0.13)
    }

    private fun flowerCrown(c: InkCanvas) {
        c.dot(Color(0xE85C9A), -0.28, -0.14, 0.13)
        c.dot(GOLD, 0.0, -0.22, 0.13)
        c.dot(Color(0x7BC4E8), 0.28, -0.14, 0.13)
    }

    private fun headphones(c: InkCanvas) {
        c.curve(DARK, 0.12, c.p(-0.48, 0.18), c.p(-0.44, -0.42), c.p(0.44, -0.42), c.p(0.48, 0.18))
        c.box(DARK, -0.58, 0.12, 0.2, 0.3, 0.07)
        c.box(DARK, 0.38, 0.12, 0.2, 0.3, 0.07)
    }

    private fun bandage(c: InkCanvas) {
        val tape = Color(0xE8C9A0)
        c.stroke(tape, 0.2, c.p(-0.38, 0.24), c.p(0.38, 0.66))
        c.stroke(tape, 0.2, c.p(-0.38, 0.66), c.p(0.38, 0.24))
    }

    private fun necktie(c: InkCanvas) {
        val silk = Color(0x2E5CA8)
        c.triangle(silk, -0.14, 0.36, 0.14, 0.36, 0.0, 0.52)
        c.triangle(silk, -0.13, 0.54, 0.13, 0.54, 0.0, 1.02)
    }

    private fun wings(c: InkCanvas) {
        c.triangle(PALE, -0.3, 0.24, -0.82, 0.02, -0.6, 0.56)
        c.triangle(PALE, 0.3, 0.24, 0.82, 0.02, 0.6, 0.56)
    }

    private fun armour(c: InkCanvas) {
        c.box(Color(0x8E96A6), -0.42, 0.4, 0.84, 0.26, 0.06)
        c.dot(PALE, 0.0, 0.53, 0.07)
    }

    private fun cape(c: InkCanvas) {
        c.triangle(Color(0x8A1F3C), -0.3, 0.16, 0.3, 0.16, 0.0, 1.2)
    }

    private fun backpack(c: InkCanvas) {
        val canvas = Color(0x5A7A4A)
        c.box(canvas, 0.3, 0.3, 0.4, 0.5, 0.1)
        c.stroke(canvas, 0.08, c.p(0.3, 0.38), c.p(0.06, 0.44))
    }

    private fun monocle(c: InkCanvas) {
        c.ring(GOLD, 0.26, 0.3, 0.24, 0.09)
        c.stroke(GOLD, 0.07, c.p(0.3, 0.52), c.p(0.36, 0.94))
    }
}
