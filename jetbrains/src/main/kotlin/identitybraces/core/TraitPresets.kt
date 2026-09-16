package identitybraces.core

/** A named set of trait weights. Anything not listed in [weights] is zero. */
class TraitPreset(val id: String, val name: String, val description: String, val weights: Map<String, Int>)

/**
 * Bundles that set every weight at once.
 *
 * Eighty-six individual percentages is not a settings page, it is a spreadsheet. A preset is
 * how someone gets a coherent look without reading the whole catalogue, and picking one is
 * still just a write to the same weight table — so a preset can be applied and then
 * adjusted, rather than being a mode you are locked into.
 */
object TraitPresets {
    val all: List<TraitPreset> = listOf(
        TraitPreset("off", "Off", "Colours only. Every brace keeps its own glyph.", emptyMap()),
        TraitPreset(
            "default", "Default",
            "What the extension shipped with: a few questions, some colour cycling, the occasional cat.",
            mapOf(
                TraitIds.QUESTION to 6,
                TraitIds.COLOUR_CYCLE to 8,
                TraitIds.CATGIRL to 4,
                TraitIds.THIGH_HIGHS to 4,
            ),
        ),
        TraitPreset(
            "menagerie", "Menagerie",
            "Creatures and costumes across the board, but nothing that moves much.",
            buildMenagerie(),
        ),
        TraitPreset(
            "restless", "Restless",
            "Everything on the motion layer, spread evenly. Nothing on screen holds still.",
            layerSpread(TraitLayer.Motion, 90),
        ),
        TraitPreset(
            "unusable", "Unusable",
            "Every layer saturated. This is the setting the extension was designed for.",
            buildUnusable(),
        ),
    )

    fun find(id: String): TraitPreset? = all.firstOrNull { it.id == id }

    /**
     * A preset expressed as the full override map the settings store keeps.
     *
     * Every trait is listed explicitly, including the zeroes: a preset that only wrote its
     * non-zero entries would leave whatever the previous preset had turned on still running.
     */
    fun weightsOf(preset: TraitPreset): Map<String, Int> {
        val weights = LinkedHashMap<String, Int>()
        for (trait in TraitCatalog.all) {
            weights[trait.id] = preset.weights[trait.id] ?: 0
        }

        return weights
    }

    private fun buildMenagerie(): Map<String, Int> {
        val weights = LinkedHashMap<String, Int>()
        weights.putAll(layerSpread(TraitLayer.Creature, 70))
        weights.putAll(layerSpread(TraitLayer.Costume, 55))
        weights[TraitIds.QUESTION] = 5
        weights[TraitIds.COLOUR_CYCLE] = 10
        return weights
    }

    private fun buildUnusable(): Map<String, Int> {
        val weights = LinkedHashMap<String, Int>()
        weights.putAll(layerSpread(TraitLayer.Creature, 85))
        weights.putAll(layerSpread(TraitLayer.Costume, 85))
        weights.putAll(layerSpread(TraitLayer.Motion, 85))
        weights.putAll(layerSpread(TraitLayer.Body, 45))

        // Effects roll independently rather than sharing a band, so they are set per trait
        // rather than spread across a budget.
        weights[TraitIds.FIRE] = 8
        weights[TraitIds.TILTED] = 25
        weights[TraitIds.SHADOW] = 20
        weights[TraitIds.GRADIENT_FILL] = 20
        weights[TraitIds.BOLD_ITALIC] = 20
        weights[TraitIds.UNDERLINE] = 10
        weights[TraitIds.DISTRESSED] = 12
        return weights
    }

    /**
     * Divides a budget evenly across every trait on one layer.
     *
     * Layers other than Effect share a single 0–100 roll, so their weights have to sum to
     * the budget rather than each being an independent chance. Spreading rather than setting
     * each to the budget is what stops the first trait in the list swallowing every brace.
     */
    private fun layerSpread(layer: TraitLayer, budget: Int): Map<String, Int> {
        val members = TraitCatalog.all.filter { it.layer == layer }
        if (members.isEmpty()) {
            return emptyMap()
        }

        val each = maxOf(1, budget / members.size)
        val weights = LinkedHashMap<String, Int>()
        for (member in members) {
            weights[member.id] = each
        }

        return weights
    }
}
