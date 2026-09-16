/**
 * Every trait's stable identifier.
 *
 * These strings are the persistence key — they appear in `identityBraces.traits` as
 * `"wizard": 4` — so renaming one silently resets that trait's weight for everyone who had
 * customised it. Add freely; rename never.
 *
 * Generated from the Visual Studio extension's `Core/TraitIds.cs`. The two lists have to
 * agree: a brace's traits are a pure function of its identity and these ids, and the whole
 * point is that the same file looks the same in both editors.
 */
export const TraitIds = {
    // ---- Body: what the glyph is. One per brace. ----
    Question: 'question',
    UnicodeVariant: 'unicode',
    WrongBracket: 'wrongbracket',
    Mirrored: 'mirrored',
    UpsideDown: 'upsidedown',
    Emoji: 'emoji',
    ForeignFont: 'foreignfont',
    Subscript: 'subscript',

    // ---- Creature: ears, horns, silhouettes. One per brace. ----
    Catgirl: 'catgirl',
    Bunny: 'bunny',
    Devil: 'devil',
    Angel: 'angel',
    Ghost: 'ghost',
    Wizard: 'wizard',
    Vampire: 'vampire',
    Bee: 'bee',
    Frog: 'frog',
    Snake: 'snake',
    Crab: 'crab',
    Bat: 'bat',
    Spider: 'spider',
    Fox: 'fox',
    Wolf: 'wolf',
    Dragon: 'dragon',
    Unicorn: 'unicorn',
    Penguin: 'penguin',
    Owl: 'owl',
    Cthulhu: 'cthulhu',
    Slime: 'slime',
    Mushroom: 'mushroom',
    Cactus: 'cactus',
    Robot: 'robot',
    Pirate: 'pirate',

    // ---- Costume: worn over the glyph. One per brace. ----
    ThighHighs: 'thighhighs',
    TopHat: 'tophat',
    Crown: 'crown',
    Sunglasses: 'sunglasses',
    Scarf: 'scarf',
    Bowtie: 'bowtie',
    Cape: 'cape',
    PartyHat: 'partyhat',
    Headphones: 'headphones',
    FlowerCrown: 'flowercrown',
    Beanie: 'beanie',
    Monocle: 'monocle',
    Necktie: 'necktie',
    Backpack: 'backpack',
    Wings: 'wings',
    Armour: 'armour',
    Bandage: 'bandage',
    Moustache: 'moustache',

    // ---- Motion: one per brace, or they fight over the transform. ----
    ColourCycle: 'cycle',
    Wobble: 'wobble',
    Bounce: 'bounce',
    Breathe: 'breathe',
    Heartbeat: 'heartbeat',
    Shiver: 'shiver',
    Blink: 'blink',
    Spin: 'spin',
    Flip: 'flip',
    Glitch: 'glitch',
    Sparkle: 'sparkle',
    Drift: 'drift',
    Shimmer: 'shimmer',
    Flicker: 'flicker',
    Wave: 'wave',
    Typewriter: 'typewriter',

    // ---- Effect: any number, rolled independently. ----
    Fire: 'fire',
    GradientFill: 'gradient',
    BoldItalic: 'bolditalic',
    Tilted: 'tilted',
    Underline: 'underline',
    Shadow: 'shadow',
    Distressed: 'distressed',

    // ---- Chaos: behaviours that react to the world rather than sit still. ----
    FleeCursor: 'fleecursor',
    Named: 'named',
    Nocturnal: 'nocturnal',
    Seasonal: 'seasonal',
    BuildReactive: 'buildreactive',
    SwapPlaces: 'swapplaces',
    Drunk: 'drunk',
    Gravity: 'gravity',
    StageFright: 'stagefright',
    Mitosis: 'mitosis',
    TableFlip: 'tableflip',
    FireBrigade: 'firebrigade',
} as const;

export type TraitId = (typeof TraitIds)[keyof typeof TraitIds];
