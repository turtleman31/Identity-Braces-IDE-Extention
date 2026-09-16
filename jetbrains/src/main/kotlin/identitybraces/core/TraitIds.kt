package identitybraces.core

/**
 * Every trait's stable identifier.
 *
 * These strings are the persistence key — they appear in the settings as `wizard=4`, and in
 * the Visual Studio extension's settings.ini as `Trait.wizard=4` — so renaming one silently
 * resets that trait's weight for everyone who had customised it. Add freely; rename never.
 */
object TraitIds {
    // ---- Body: what the glyph is. One per brace. ----
    const val QUESTION = "question"
    const val UNICODE_VARIANT = "unicode"
    const val WRONG_BRACKET = "wrongbracket"
    const val MIRRORED = "mirrored"
    const val UPSIDE_DOWN = "upsidedown"
    const val EMOJI = "emoji"
    const val FOREIGN_FONT = "foreignfont"
    const val SUBSCRIPT = "subscript"

    // ---- Creature: ears, horns, silhouettes. One per brace. ----
    const val CATGIRL = "catgirl"
    const val BUNNY = "bunny"
    const val DEVIL = "devil"
    const val ANGEL = "angel"
    const val GHOST = "ghost"
    const val WIZARD = "wizard"
    const val VAMPIRE = "vampire"
    const val BEE = "bee"
    const val FROG = "frog"
    const val SNAKE = "snake"
    const val CRAB = "crab"
    const val BAT = "bat"
    const val SPIDER = "spider"
    const val FOX = "fox"
    const val WOLF = "wolf"
    const val DRAGON = "dragon"
    const val UNICORN = "unicorn"
    const val PENGUIN = "penguin"
    const val OWL = "owl"
    const val CTHULHU = "cthulhu"
    const val SLIME = "slime"
    const val MUSHROOM = "mushroom"
    const val CACTUS = "cactus"
    const val ROBOT = "robot"
    const val PIRATE = "pirate"

    // ---- Costume: worn over the glyph. One per brace. ----
    const val THIGH_HIGHS = "thighhighs"
    const val TOP_HAT = "tophat"
    const val CROWN = "crown"
    const val SUNGLASSES = "sunglasses"
    const val SCARF = "scarf"
    const val BOWTIE = "bowtie"
    const val CAPE = "cape"
    const val PARTY_HAT = "partyhat"
    const val HEADPHONES = "headphones"
    const val FLOWER_CROWN = "flowercrown"
    const val BEANIE = "beanie"
    const val MONOCLE = "monocle"
    const val NECKTIE = "necktie"
    const val BACKPACK = "backpack"
    const val WINGS = "wings"
    const val ARMOUR = "armour"
    const val BANDAGE = "bandage"
    const val MOUSTACHE = "moustache"

    // ---- Motion: one per brace, or they fight over the transform. ----
    const val COLOUR_CYCLE = "cycle"
    const val WOBBLE = "wobble"
    const val BOUNCE = "bounce"
    const val BREATHE = "breathe"
    const val HEARTBEAT = "heartbeat"
    const val SHIVER = "shiver"
    const val BLINK = "blink"
    const val SPIN = "spin"
    const val FLIP = "flip"
    const val GLITCH = "glitch"
    const val SPARKLE = "sparkle"
    const val DRIFT = "drift"
    const val SHIMMER = "shimmer"
    const val FLICKER = "flicker"
    const val WAVE = "wave"
    const val TYPEWRITER = "typewriter"

    // ---- Effect: any number, rolled independently. ----
    const val FIRE = "fire"
    const val GRADIENT_FILL = "gradient"
    const val BOLD_ITALIC = "bolditalic"
    const val TILTED = "tilted"
    const val UNDERLINE = "underline"
    const val SHADOW = "shadow"

    /**
     * Sweating and unsteady. Rollable like any other effect, but also forced on by the
     * complexity warning, which is the only trait in the catalogue with a predicate behind
     * it as well as a weight.
     */
    const val DISTRESSED = "distressed"

    // ---- Chaos: behaviours that react to the world rather than sit still. ----
    const val FLEE_CURSOR = "fleecursor"
    const val NAMED = "named"
    const val NOCTURNAL = "nocturnal"
    const val SEASONAL = "seasonal"
    const val BUILD_REACTIVE = "buildreactive"
    const val SWAP_PLACES = "swapplaces"
    const val DRUNK = "drunk"
    const val GRAVITY = "gravity"
    const val STAGE_FRIGHT = "stagefright"
    const val MITOSIS = "mitosis"
    const val TABLE_FLIP = "tableflip"
    const val FIRE_BRIGADE = "firebrigade"
}
