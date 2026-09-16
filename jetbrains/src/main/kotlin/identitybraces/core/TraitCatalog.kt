package identitybraces.core

/** One trait's metadata: what it is called, which layer it sits on, how often. */
class TraitInfo(
    val id: String,
    val layer: TraitLayer,
    val name: String,
    val defaultPercent: Int,
    val description: String,
)

/** One trait's identity and how often it should come up. */
class TraitWeight(val id: String, val layer: TraitLayer, val percent: Int)

/**
 * Every trait the extension knows about, in the order the roll walks them.
 *
 * The single list driving the roll, the settings, the presets and the settings UI. Adding a
 * trait is one row here plus one draw function in the drawing registry, and nothing else
 * changes. The *order* is part of the identity contract — the flattened trait table walks it
 * in sequence, so a reshuffle would hand every brace a different personality — which is why
 * this is a list and not a map.
 */
object TraitCatalog {
    val all: List<TraitInfo> = listOf(
        TraitInfo(TraitIds.QUESTION, TraitLayer.Body, "Question mark", 6, "Renders ? instead of the brace."),
        TraitInfo(TraitIds.UNICODE_VARIANT, TraitLayer.Body, "Unicode variant", 0, "Picks an exotic bracket from the same family."),
        TraitInfo(TraitIds.WRONG_BRACKET, TraitLayer.Body, "Wrong bracket", 0, "Draws a different bracket type entirely."),
        TraitInfo(TraitIds.MIRRORED, TraitLayer.Body, "Mirrored", 0, "Flipped horizontally, so openers close."),
        TraitInfo(TraitIds.UPSIDE_DOWN, TraitLayer.Body, "Upside down", 0, "Rotated a half turn in place."),
        TraitInfo(TraitIds.EMOJI, TraitLayer.Body, "Emoji", 0, "Substitutes an emoji. Ignores the palette."),
        TraitInfo(TraitIds.FOREIGN_FONT, TraitLayer.Body, "Foreign font", 0, "Drawn in Comic Sans while everything else stays monospaced."),
        TraitInfo(TraitIds.SUBSCRIPT, TraitLayer.Body, "Subscript", 0, "Small, and dropped below the baseline."),
        TraitInfo(TraitIds.CATGIRL, TraitLayer.Creature, "Cat", 4, "Two ears above the glyph."),
        TraitInfo(TraitIds.BUNNY, TraitLayer.Creature, "Bunny", 0, "Tall narrow ears, one flopped at the tip."),
        TraitInfo(TraitIds.DEVIL, TraitLayer.Creature, "Devil", 0, "Curved horns and a barbed tail."),
        TraitInfo(TraitIds.ANGEL, TraitLayer.Creature, "Angel", 0, "A floating halo ring."),
        TraitInfo(TraitIds.GHOST, TraitLayer.Creature, "Ghost", 0, "Scalloped lower edge, drawn faint."),
        TraitInfo(TraitIds.WIZARD, TraitLayer.Creature, "Wizard", 0, "A pointed hat with a brim and a star."),
        TraitInfo(TraitIds.VAMPIRE, TraitLayer.Creature, "Vampire", 0, "Two fangs below the glyph."),
        TraitInfo(TraitIds.BEE, TraitLayer.Creature, "Bee", 0, "Banded body with a pair of wings."),
        TraitInfo(TraitIds.FROG, TraitLayer.Creature, "Frog", 0, "Two bulging eyes on the top curve."),
        TraitInfo(TraitIds.SNAKE, TraitLayer.Creature, "Snake", 0, "A forked tongue flicking sideways."),
        TraitInfo(TraitIds.CRAB, TraitLayer.Creature, "Crab", 0, "A pincer on each flank."),
        TraitInfo(TraitIds.BAT, TraitLayer.Creature, "Bat", 0, "Scalloped wings either side."),
        TraitInfo(TraitIds.SPIDER, TraitLayer.Creature, "Spider", 0, "Legs radiating outward."),
        TraitInfo(TraitIds.FOX, TraitLayer.Creature, "Fox", 0, "Sharp ears and a heavy tail."),
        TraitInfo(TraitIds.WOLF, TraitLayer.Creature, "Wolf", 0, "Outswept ears and a snout."),
        TraitInfo(TraitIds.DRAGON, TraitLayer.Creature, "Dragon", 0, "Swept horns and a folded wing."),
        TraitInfo(TraitIds.UNICORN, TraitLayer.Creature, "Unicorn", 0, "A single horn, dead centre."),
        TraitInfo(TraitIds.PENGUIN, TraitLayer.Creature, "Penguin", 0, "A beak and two flippers."),
        TraitInfo(TraitIds.OWL, TraitLayer.Creature, "Owl", 0, "Two oversized concentric eyes."),
        TraitInfo(TraitIds.CTHULHU, TraitLayer.Creature, "Cthulhu", 0, "Tentacles drooping and slowly waving."),
        TraitInfo(TraitIds.SLIME, TraitLayer.Creature, "Slime", 0, "A drip that forms, falls and resets."),
        TraitInfo(TraitIds.MUSHROOM, TraitLayer.Creature, "Mushroom", 0, "A spotted cap sitting on top."),
        TraitInfo(TraitIds.CACTUS, TraitLayer.Creature, "Cactus", 0, "Spines along both flanks and a flower."),
        TraitInfo(TraitIds.ROBOT, TraitLayer.Creature, "Robot", 0, "An antenna with a blinking bulb."),
        TraitInfo(TraitIds.PIRATE, TraitLayer.Creature, "Pirate", 0, "An eyepatch band and a bandana knot."),
        TraitInfo(TraitIds.THIGH_HIGHS, TraitLayer.Costume, "Thigh highs", 4, "The lower stroke recoloured, with a welt stripe."),
        TraitInfo(TraitIds.TOP_HAT, TraitLayer.Costume, "Top hat", 0, "A brim and a block above it."),
        TraitInfo(TraitIds.CROWN, TraitLayer.Costume, "Crown", 0, "A three-point zigzag."),
        TraitInfo(TraitIds.SUNGLASSES, TraitLayer.Costume, "Sunglasses", 0, "A dark bar with a bridge notch."),
        TraitInfo(TraitIds.SCARF, TraitLayer.Costume, "Scarf", 0, "A band with one tail streaming out."),
        TraitInfo(TraitIds.BOWTIE, TraitLayer.Costume, "Bow tie", 0, "Two triangles meeting at the waist."),
        TraitInfo(TraitIds.CAPE, TraitLayer.Costume, "Cape", 0, "A shape behind the stroke."),
        TraitInfo(TraitIds.PARTY_HAT, TraitLayer.Costume, "Party hat", 0, "A striped cone with a pom."),
        TraitInfo(TraitIds.HEADPHONES, TraitLayer.Costume, "Headphones", 0, "An arc over the top with two pads."),
        TraitInfo(TraitIds.FLOWER_CROWN, TraitLayer.Costume, "Flower crown", 0, "Three dots of differing hues."),
        TraitInfo(TraitIds.BEANIE, TraitLayer.Costume, "Beanie", 0, "A rounded cap with a rolled brim."),
        TraitInfo(TraitIds.MONOCLE, TraitLayer.Costume, "Monocle", 0, "A ring with a chain hanging down."),
        TraitInfo(TraitIds.NECKTIE, TraitLayer.Costume, "Necktie", 0, "A knot and a taper down the front."),
        TraitInfo(TraitIds.BACKPACK, TraitLayer.Costume, "Backpack", 0, "A rounded box behind, with straps."),
        TraitInfo(TraitIds.WINGS, TraitLayer.Costume, "Wings", 0, "Feathered arcs on both flanks."),
        TraitInfo(TraitIds.ARMOUR, TraitLayer.Costume, "Armour", 0, "A plated band with a rivet."),
        TraitInfo(TraitIds.BANDAGE, TraitLayer.Costume, "Bandage", 0, "A crossed plaster over the stroke."),
        TraitInfo(TraitIds.MOUSTACHE, TraitLayer.Costume, "Moustache", 0, "A curled bar across the middle."),
        TraitInfo(TraitIds.COLOUR_CYCLE, TraitLayer.Motion, "Colour cycle", 8, "Travels the hue wheel forever."),
        TraitInfo(TraitIds.WOBBLE, TraitLayer.Motion, "Wobble", 0, "Rotates a few degrees either side."),
        TraitInfo(TraitIds.BOUNCE, TraitLayer.Motion, "Bounce", 0, "A short vertical hop on a loop."),
        TraitInfo(TraitIds.BREATHE, TraitLayer.Motion, "Breathe", 0, "Scales slowly between 95 and 105 percent."),
        TraitInfo(TraitIds.HEARTBEAT, TraitLayer.Motion, "Heartbeat", 0, "Two quick pulses, then a rest."),
        TraitInfo(TraitIds.SHIVER, TraitLayer.Motion, "Shiver", 0, "Sub-pixel jitter on both axes."),
        TraitInfo(TraitIds.BLINK, TraitLayer.Motion, "Blink", 0, "Fades out and back at irregular intervals."),
        TraitInfo(TraitIds.SPIN, TraitLayer.Motion, "Spin", 0, "Continuous slow rotation."),
        TraitInfo(TraitIds.FLIP, TraitLayer.Motion, "Flip", 0, "Snaps a half turn every few seconds."),
        TraitInfo(TraitIds.GLITCH, TraitLayer.Motion, "Glitch", 0, "Random small offsets with colour fringing."),
        TraitInfo(TraitIds.SPARKLE, TraitLayer.Motion, "Sparkle", 0, "Four-point stars appearing and fading."),
        TraitInfo(TraitIds.DRIFT, TraitLayer.Motion, "Drift", 0, "Wanders off its cell and slowly returns."),
        TraitInfo(TraitIds.SHIMMER, TraitLayer.Motion, "Shimmer", 0, "A bright band sweeping down the stroke."),
        TraitInfo(TraitIds.FLICKER, TraitLayer.Motion, "Flicker", 0, "Firelight brightness noise."),
        TraitInfo(TraitIds.WAVE, TraitLayer.Motion, "Wave", 0, "Neighbours bounce in sequence along the line."),
        TraitInfo(TraitIds.TYPEWRITER, TraitLayer.Motion, "Typewriter", 0, "Fades in once on first appearance."),
        TraitInfo(TraitIds.FIRE, TraitLayer.Effect, "On fire", 0, "Flames licking up from the glyph."),
        TraitInfo(TraitIds.GRADIENT_FILL, TraitLayer.Effect, "Gradient fill", 0, "A vertical ramp through the stroke."),
        TraitInfo(TraitIds.BOLD_ITALIC, TraitLayer.Effect, "Bold or italic", 0, "Per-brace weight and slant."),
        TraitInfo(TraitIds.TILTED, TraitLayer.Effect, "Tilted", 0, "A fixed random lean, stable per brace."),
        TraitInfo(TraitIds.UNDERLINE, TraitLayer.Effect, "Underline", 0, "A squiggle beneath, like a spelling error."),
        TraitInfo(TraitIds.SHADOW, TraitLayer.Effect, "Shadow", 0, "A hard offset duplicate behind the glyph."),
        TraitInfo(TraitIds.DISTRESSED, TraitLayer.Effect, "Distressed", 0, "Sweat beads and an unsteady shake. Also forced on by the complexity warning."),
        TraitInfo(TraitIds.FLEE_CURSOR, TraitLayer.Effect, "Flee the cursor", 0, "Edges away as the caret nears."),
        TraitInfo(TraitIds.NAMED, TraitLayer.Effect, "Named", 0, "A stable generated name, shown on hover."),
        TraitInfo(TraitIds.NOCTURNAL, TraitLayer.Effect, "Nocturnal", 0, "Droops as the session wears on."),
        TraitInfo(TraitIds.SEASONAL, TraitLayer.Effect, "Seasonal", 0, "Dresses for the time of year."),
        TraitInfo(TraitIds.BUILD_REACTIVE, TraitLayer.Effect, "Build reactive", 0, "Celebrates a green build, sulks at a red one."),
        TraitInfo(TraitIds.SWAP_PLACES, TraitLayer.Effect, "Swap places", 0, "Two braces on a line trade positions, briefly."),
        TraitInfo(TraitIds.DRUNK, TraitLayer.Effect, "Drunk", 0, "The lean grows over the session, then resets."),
        TraitInfo(TraitIds.GRAVITY, TraitLayer.Effect, "Gravity", 0, "Sags toward the bottom of its cell over time."),
        TraitInfo(TraitIds.STAGE_FRIGHT, TraitLayer.Effect, "Stage fright", 0, "Hides while the caret is on its line."),
        TraitInfo(TraitIds.MITOSIS, TraitLayer.Effect, "Mitosis", 0, "Rarely splits in two; one half drifts away."),
        TraitInfo(TraitIds.TABLE_FLIP, TraitLayer.Effect, "Table flip", 0, "Flips a table into adjacent whitespace, then tidies up."),
        TraitInfo(TraitIds.FIRE_BRIGADE, TraitLayer.Effect, "Fire brigade", 0, "Braces crew a fire truck and put out a burning brace."),
    )

    private val byId: Map<String, TraitInfo> = all.associateBy { it.id }

    fun find(id: String): TraitInfo? = byId[id]

    /** The catalogue's own defaults, for tests and for a first run. */
    fun defaultWeights(): List<TraitWeight> = all.map { TraitWeight(it.id, it.layer, it.defaultPercent) }

    /**
     * Catalogue defaults with [overrides] applied by id, clamped to 0–100.
     *
     * Kept in catalogue order regardless of the order the overrides arrive in — see the note
     * on [all].
     */
    fun weightsFrom(overrides: Map<String, Int>?): List<TraitWeight> = all.map { info ->
        val percent = overrides?.get(info.id) ?: info.defaultPercent
        TraitWeight(info.id, info.layer, percent.coerceIn(0, 100))
    }
}
