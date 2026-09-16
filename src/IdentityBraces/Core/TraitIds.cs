namespace IdentityBraces.Core
{
    /// <summary>
    /// Every trait's stable identifier.
    /// </summary>
    /// <remarks>
    /// These strings are the persistence key — they appear in settings.ini as
    /// <c>Trait.wizard=4</c> — so renaming one silently resets that trait's weight for
    /// everyone who had customised it. Add freely; rename never.
    /// </remarks>
    internal static class TraitIds
    {
        // ---- Body: what the glyph is. One per brace. ----
        public const string Question = "question";
        public const string UnicodeVariant = "unicode";
        public const string WrongBracket = "wrongbracket";
        public const string Mirrored = "mirrored";
        public const string UpsideDown = "upsidedown";
        public const string Emoji = "emoji";
        public const string ForeignFont = "foreignfont";
        public const string Subscript = "subscript";

        // ---- Creature: ears, horns, silhouettes. One per brace. ----
        public const string Catgirl = "catgirl";
        public const string Bunny = "bunny";
        public const string Devil = "devil";
        public const string Angel = "angel";
        public const string Ghost = "ghost";
        public const string Wizard = "wizard";
        public const string Vampire = "vampire";
        public const string Bee = "bee";
        public const string Frog = "frog";
        public const string Snake = "snake";
        public const string Crab = "crab";
        public const string Bat = "bat";
        public const string Spider = "spider";
        public const string Fox = "fox";
        public const string Wolf = "wolf";
        public const string Dragon = "dragon";
        public const string Unicorn = "unicorn";
        public const string Penguin = "penguin";
        public const string Owl = "owl";
        public const string Cthulhu = "cthulhu";
        public const string Slime = "slime";
        public const string Mushroom = "mushroom";
        public const string Cactus = "cactus";
        public const string Robot = "robot";
        public const string Pirate = "pirate";

        // ---- Costume: worn over the glyph. One per brace. ----
        public const string ThighHighs = "thighhighs";
        public const string TopHat = "tophat";
        public const string Crown = "crown";
        public const string Sunglasses = "sunglasses";
        public const string Scarf = "scarf";
        public const string Bowtie = "bowtie";
        public const string Cape = "cape";
        public const string PartyHat = "partyhat";
        public const string Headphones = "headphones";
        public const string FlowerCrown = "flowercrown";
        public const string Beanie = "beanie";
        public const string Monocle = "monocle";
        public const string Necktie = "necktie";
        public const string Backpack = "backpack";
        public const string Wings = "wings";
        public const string Armour = "armour";
        public const string Bandage = "bandage";
        public const string Moustache = "moustache";

        // ---- Motion: one per brace, or they fight over the transform. ----
        public const string ColourCycle = "cycle";
        public const string Wobble = "wobble";
        public const string Bounce = "bounce";
        public const string Breathe = "breathe";
        public const string Heartbeat = "heartbeat";
        public const string Shiver = "shiver";
        public const string Blink = "blink";
        public const string Spin = "spin";
        public const string Flip = "flip";
        public const string Glitch = "glitch";
        public const string Sparkle = "sparkle";
        public const string Drift = "drift";
        public const string Shimmer = "shimmer";
        public const string Flicker = "flicker";
        public const string Wave = "wave";
        public const string Typewriter = "typewriter";

        // ---- Effect: any number, rolled independently. ----
        public const string Fire = "fire";
        public const string GradientFill = "gradient";
        public const string BoldItalic = "bolditalic";
        public const string Tilted = "tilted";
        public const string Underline = "underline";
        public const string Shadow = "shadow";

        /// <summary>
        /// Sweating and unsteady. Rollable like any other effect, but also forced on by the
        /// complexity warning, which is the only trait in the catalogue with a predicate
        /// behind it as well as a weight.
        /// </summary>
        public const string Distressed = "distressed";

        // ---- Chaos: behaviours that react to the world rather than sit still. ----
        public const string FleeCursor = "fleecursor";
        public const string Named = "named";
        public const string Nocturnal = "nocturnal";
        public const string Seasonal = "seasonal";
        public const string BuildReactive = "buildreactive";
        public const string SwapPlaces = "swapplaces";
        public const string Drunk = "drunk";
        public const string Gravity = "gravity";
        public const string StageFright = "stagefright";
        public const string Mitosis = "mitosis";
        public const string TableFlip = "tableflip";
        public const string FireBrigade = "firebrigade";
    }
}
