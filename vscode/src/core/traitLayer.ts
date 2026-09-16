/**
 * The independent layers a brace is assembled from.
 *
 * A wizard that is also on fire and also wobbling is three separate choices, so they are
 * rolled separately and drawn in a fixed back-to-front order.
 *
 * Each layer rolls from the brace's identity hash with its own salt, so the layers are
 * statistically independent of one another while every one of them stays stable for the
 * life of that brace.
 */
export const enum TraitLayer {
    /** What the glyph itself is. Exactly one, defaulting to the real character. */
    Body = 0,

    /** Ears, horns, halos. At most one. */
    Creature = 1,

    /** Hats, scarves, stockings. At most one. */
    Costume = 2,

    /** Wobble, bounce, spin. At most one, or they fight each other. */
    Motion = 3,

    /** Fire, shadow, tilt. Any number, rolled independently. */
    Effect = 4,
}

export const LAYER_NAMES: readonly string[] = ['Body', 'Creature', 'Costume', 'Motion', 'Effect'];
