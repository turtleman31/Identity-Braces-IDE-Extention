namespace IdentityBraces.Core
{
    /// <summary>
    /// The independent layers a brace is assembled from.
    /// </summary>
    /// <remarks>
    /// Replaces the single <c>Personality</c> enum, which allowed a brace to be exactly one
    /// thing. A wizard that is also on fire and also wobbling is three separate choices, so
    /// they are rolled separately and drawn in a fixed back-to-front order.
    /// <para>
    /// Each layer rolls from the brace's identity hash with its own salt, so the layers are
    /// statistically independent of one another while every one of them stays stable for the
    /// life of that brace.
    /// </para>
    /// </remarks>
    internal enum TraitLayer
    {
        /// <summary>What the glyph itself is. Exactly one, defaulting to the real character.</summary>
        Body = 0,

        /// <summary>Ears, horns, halos. At most one.</summary>
        Creature = 1,

        /// <summary>Hats, scarves, stockings. At most one.</summary>
        Costume = 2,

        /// <summary>Wobble, bounce, spin. At most one, or they fight each other.</summary>
        Motion = 3,

        /// <summary>Fire, shadow, tilt. Any number, rolled independently.</summary>
        Effect = 4,
    }
}
