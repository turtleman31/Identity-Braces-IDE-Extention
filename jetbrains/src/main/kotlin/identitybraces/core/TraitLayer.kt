package identitybraces.core

/**
 * The independent layers a brace is assembled from.
 *
 * A wizard that is also on fire and also wobbling is three separate choices, so they are
 * rolled separately — each from the identity hash with its own salt, so the layers are
 * statistically independent while every one of them stays stable for the life of the brace —
 * and drawn in a fixed back-to-front order.
 */
enum class TraitLayer {
    /** What the glyph itself is. Exactly one, defaulting to the real character. */
    Body,

    /** Ears, horns, halos. At most one. */
    Creature,

    /** Hats, scarves, stockings. At most one. */
    Costume,

    /** Wobble, bounce, spin. At most one, or they fight each other. */
    Motion,

    /** Fire, shadow, tilt. Any number, rolled independently. */
    Effect,
}
