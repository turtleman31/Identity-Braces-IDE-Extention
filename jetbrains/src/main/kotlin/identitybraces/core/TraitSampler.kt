package identitybraces.core

/**
 * Generates representative braces for a preview, without a document to scan.
 *
 * Deciding *what* to draw in a preview is arithmetic on the same weight table the scanner
 * uses, and a preview that disagrees with the scanner would be worse than no preview — it
 * would be confidently wrong about a catalogue nobody can otherwise inspect. So this rolls
 * through [TraitRoll] exactly as [BraceScanner] does. The only thing it invents is the
 * identities, which real braces get from their declaring text.
 */
object TraitSampler {
    /**
     * Fixed, so a sample set does not reshuffle every time a weight changes. With a fresh
     * seed per render, nudging one slider would redraw a completely different set of braces
     * and you could not tell what your change did.
     */
    const val SEED: ULong = 0x9E3779B97F4A7C15uL

    /** The deepest level [depthOf] descends to before coming back out. */
    const val DEEPEST_SAMPLE = 5

    class Sample(val identity: ULong, val depth: Int, val traits: BraceTraits)

    /** Rolls [count] braces against [weights]. */
    fun take(weights: List<TraitWeight>, count: Int): List<Sample> {
        val table = TraitTable(weights)
        return List(maxOf(0, count)) { i ->
            val identity = Hash.mix(SEED, i.toULong())
            Sample(identity, depthOf(i), TraitRoll.roll(identity, table, isUnmatched = false))
        }
    }

    /**
     * A nesting profile that descends and comes back out, so a depth-coloured preview shows
     * a ramp rather than a flat run of one colour.
     */
    fun depthOf(index: Int): Int {
        val i = if (index < 0) -index else index
        val period = DEEPEST_SAMPLE * 2
        val phase = i % period
        return if (phase <= DEEPEST_SAMPLE) phase else period - phase
    }

    /**
     * A brace wearing exactly one trait, whatever that trait's weight is.
     *
     * Built rather than rolled, because the question it answers is "what *is* a cthulhu
     * brace" — asked before deciding whether to enable it. `isDrawn` is forced rather than
     * derived so an unrecognised layer cannot produce a brace the renderer declines to draw.
     */
    fun single(layer: TraitLayer, id: String): BraceTraits = when (layer) {
        TraitLayer.Body -> BraceTraits(body = id, isDrawn = true)
        TraitLayer.Creature -> BraceTraits(creature = id, isDrawn = true)
        TraitLayer.Costume -> BraceTraits(costume = id, isDrawn = true)
        TraitLayer.Motion -> BraceTraits(motion = id, isDrawn = true)
        TraitLayer.Effect -> BraceTraits(effects = listOf(id), isDrawn = true)
    }
}
