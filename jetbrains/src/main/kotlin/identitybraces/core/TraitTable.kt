package identitybraces.core

/**
 * The weight list flattened into the form the per-brace roll actually needs.
 *
 * Built once per scan rather than consulted per brace. The naive version walked all
 * eighty-odd weights once per layer for every brace — on a 240,000-brace file that is a
 * hundred million iterations. Only enabled traits appear here, and their bands are
 * pre-summed, so a brace's roll is a short walk over the handful of traits actually switched
 * on. With the shipped defaults that is one or two entries per layer.
 */
class TraitTable(weights: List<TraitWeight>?) {
    private class Band(val id: String, val upper: Double)

    private val layers: Array<Array<Band>>
    private val effects: Array<Band>

    init {
        layers = Array(4) { layer ->
            val bands = ArrayList<Band>()
            var cursor = 0.0

            if (weights != null) {
                for (weight in weights) {
                    if (weight.layer.ordinal != layer || weight.percent <= 0) {
                        continue
                    }

                    cursor += weight.percent
                    bands.add(Band(weight.id, cursor))
                }
            }

            bands.toTypedArray()
        }

        val independent = ArrayList<Band>()
        if (weights != null) {
            for (weight in weights) {
                if (weight.layer == TraitLayer.Effect && weight.percent > 0) {
                    independent.add(Band(weight.id, weight.percent.toDouble()))
                }
            }
        }

        effects = independent.toTypedArray()
    }

    val isEmpty: Boolean
        get() = effects.isEmpty() && layers.all { it.isEmpty() }

    /** Picks at most one trait from a shared-roll layer. */
    fun pickOne(layer: TraitLayer, roll: Double): String? {
        for (band in layers[layer.ordinal]) {
            if (roll < band.upper) {
                return band.id
            }
        }

        return null
    }

    val effectCount: Int
        get() = effects.size

    fun effectAt(index: Int): String = effects[index].id

    fun effectChance(index: Int): Double = effects[index].upper
}
