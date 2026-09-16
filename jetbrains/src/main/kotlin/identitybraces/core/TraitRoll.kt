package identitybraces.core

/** What a single brace turned out to be. */
class BraceTraits(
    val body: String? = null,
    val creature: String? = null,
    val costume: String? = null,
    val motion: String? = null,
    val effects: List<String> = emptyList(),

    /**
     * True when anything at all was rolled, so the plain colouring must paint the real glyph
     * transparent and let the overlay draw it instead.
     */
    val isDrawn: Boolean = false,
) {
    fun hasEffect(id: String): Boolean = effects.contains(id)

    /** Whether anything sits above the glyph and wants headroom — ears, hats, halos. */
    val wantsHeadroom: Boolean
        get() = creature != null || costume != null

    companion object {
        val NONE = BraceTraits()
    }
}

/**
 * Turns a brace's identity hash into its traits.
 *
 * Deliberately free of the editor: the colouring needs to know whether a brace is drawn (so
 * it can hide the real one) and the overlay needs to know what to draw, and those two must
 * never disagree. Keeping the roll pure means both call the same function and get the same
 * answer, and it stays testable on a bare JVM.
 */
object TraitRoll {
    // One salt per layer. Without these the layers would correlate — every brace with a
    // wizard hat would also have the same motion, because both would read the same bits.
    private const val BODY_SALT: ULong = 0xB0D1E5A17C0FFEEuL
    private const val CREATURE_SALT: ULong = 0xC8EA7C0DE1DEA5uL
    private const val COSTUME_SALT: ULong = 0xC05715E5A1701uL
    private const val MOTION_SALT: ULong = 0x0713C0DE5EED17uL
    private const val EFFECT_SALT: ULong = 0xEFEC7B0DE5A1EDuL

    private const val GOLDEN: ULong = 0x9E3779B97F4A7C15uL

    /**
     * @param isDistressed Set by the complexity warning, which replaces this one trait's
     * roll with a predicate on nesting depth. It is forced on regardless of its weight: a
     * warning that only fires on four braces in a hundred is not a warning.
     */
    fun roll(identity: ULong, table: TraitTable, isUnmatched: Boolean, isDistressed: Boolean = false): BraceTraits {
        // A brace with no partner overrides whatever body it rolled: it genuinely does not
        // know what it is, and it doubles as a syntax hint.
        val body = if (isUnmatched) TraitIds.QUESTION else table.pickOne(TraitLayer.Body, roll100(identity, BODY_SALT))
        val creature = table.pickOne(TraitLayer.Creature, roll100(identity, CREATURE_SALT))
        val costume = table.pickOne(TraitLayer.Costume, roll100(identity, COSTUME_SALT))
        val motion = table.pickOne(TraitLayer.Motion, roll100(identity, MOTION_SALT))

        var effects = pickEffects(identity, table)
        if (isDistressed && !effects.contains(TraitIds.DISTRESSED)) {
            effects = effects + TraitIds.DISTRESSED
        }

        val isDrawn = body != null || creature != null || costume != null || motion != null || effects.isNotEmpty()
        return BraceTraits(body, creature, costume, motion, effects, isDrawn)
    }

    private fun roll100(identity: ULong, salt: ULong): Double {
        return Hash.toUnitInterval(Hash.mix(identity, salt)) * 100.0
    }

    /**
     * Rolls every effect independently, so a brace can be on fire and tilted and carry a
     * shadow all at once.
     */
    private fun pickEffects(identity: ULong, table: TraitTable): List<String> {
        val count = table.effectCount
        if (count == 0) {
            return emptyList()
        }

        var chosen: ArrayList<String>? = null
        var stream = EFFECT_SALT

        for (i in 0 until count) {
            // Advance the stream per effect so each gets its own independent roll.
            stream = Hash.mix(stream, GOLDEN)

            if (roll100(identity, stream) < table.effectChance(i)) {
                if (chosen == null) {
                    chosen = ArrayList(2)
                }

                chosen.add(table.effectAt(i))
            }
        }

        return chosen ?: emptyList()
    }
}
