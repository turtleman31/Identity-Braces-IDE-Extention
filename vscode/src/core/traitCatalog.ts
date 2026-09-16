import { TraitIds } from './traitIds';
import { TraitLayer } from './traitLayer';

/** One trait's metadata: what it is called, which layer it sits on, how often. */
export interface TraitInfo {
    readonly id: string;
    readonly layer: TraitLayer;
    readonly name: string;
    readonly defaultPercent: number;
    readonly description: string;
}

/**
 * Every trait the extension knows about.
 *
 * The single list driving the roll, the settings, the presets and the gallery. Adding a
 * trait is one row here plus one entry in the drawing registry, and nothing else changes.
 *
 * Most default to zero. A fresh install should look like the extension people were shown
 * rather than every switch at once; the presets are there to turn the rest on.
 *
 * Generated from the Visual Studio extension's `Core/TraitCatalog.cs`.
 */
export const TRAIT_CATALOG: readonly TraitInfo[] = [
    { id: TraitIds.Question, layer: TraitLayer.Body, name: "Question mark", defaultPercent: 6, description: "Renders ? instead of the brace." },
    { id: TraitIds.UnicodeVariant, layer: TraitLayer.Body, name: "Unicode variant", defaultPercent: 0, description: "Picks an exotic bracket from the same family." },
    { id: TraitIds.WrongBracket, layer: TraitLayer.Body, name: "Wrong bracket", defaultPercent: 0, description: "Draws a different bracket type entirely." },
    { id: TraitIds.Mirrored, layer: TraitLayer.Body, name: "Mirrored", defaultPercent: 0, description: "Flipped horizontally, so openers close." },
    { id: TraitIds.UpsideDown, layer: TraitLayer.Body, name: "Upside down", defaultPercent: 0, description: "Rotated a half turn in place." },
    { id: TraitIds.Emoji, layer: TraitLayer.Body, name: "Emoji", defaultPercent: 0, description: "Substitutes an emoji. Ignores the palette." },
    { id: TraitIds.ForeignFont, layer: TraitLayer.Body, name: "Foreign font", defaultPercent: 0, description: "Drawn in Comic Sans while everything else stays monospaced." },
    { id: TraitIds.Subscript, layer: TraitLayer.Body, name: "Subscript", defaultPercent: 0, description: "Small, and dropped below the baseline." },
    { id: TraitIds.Catgirl, layer: TraitLayer.Creature, name: "Cat", defaultPercent: 4, description: "Two ears above the glyph." },
    { id: TraitIds.Bunny, layer: TraitLayer.Creature, name: "Bunny", defaultPercent: 0, description: "Tall narrow ears, one flopped at the tip." },
    { id: TraitIds.Devil, layer: TraitLayer.Creature, name: "Devil", defaultPercent: 0, description: "Curved horns and a barbed tail." },
    { id: TraitIds.Angel, layer: TraitLayer.Creature, name: "Angel", defaultPercent: 0, description: "A floating halo ring." },
    { id: TraitIds.Ghost, layer: TraitLayer.Creature, name: "Ghost", defaultPercent: 0, description: "Scalloped lower edge, drawn faint." },
    { id: TraitIds.Wizard, layer: TraitLayer.Creature, name: "Wizard", defaultPercent: 0, description: "A pointed hat with a brim and a star." },
    { id: TraitIds.Vampire, layer: TraitLayer.Creature, name: "Vampire", defaultPercent: 0, description: "Two fangs below the glyph." },
    { id: TraitIds.Bee, layer: TraitLayer.Creature, name: "Bee", defaultPercent: 0, description: "Banded body with a pair of wings." },
    { id: TraitIds.Frog, layer: TraitLayer.Creature, name: "Frog", defaultPercent: 0, description: "Two bulging eyes on the top curve." },
    { id: TraitIds.Snake, layer: TraitLayer.Creature, name: "Snake", defaultPercent: 0, description: "A forked tongue flicking sideways." },
    { id: TraitIds.Crab, layer: TraitLayer.Creature, name: "Crab", defaultPercent: 0, description: "A pincer on each flank." },
    { id: TraitIds.Bat, layer: TraitLayer.Creature, name: "Bat", defaultPercent: 0, description: "Scalloped wings either side." },
    { id: TraitIds.Spider, layer: TraitLayer.Creature, name: "Spider", defaultPercent: 0, description: "Legs radiating outward." },
    { id: TraitIds.Fox, layer: TraitLayer.Creature, name: "Fox", defaultPercent: 0, description: "Sharp ears and a heavy tail." },
    { id: TraitIds.Wolf, layer: TraitLayer.Creature, name: "Wolf", defaultPercent: 0, description: "Outswept ears and a snout." },
    { id: TraitIds.Dragon, layer: TraitLayer.Creature, name: "Dragon", defaultPercent: 0, description: "Swept horns and a folded wing." },
    { id: TraitIds.Unicorn, layer: TraitLayer.Creature, name: "Unicorn", defaultPercent: 0, description: "A single horn, dead centre." },
    { id: TraitIds.Penguin, layer: TraitLayer.Creature, name: "Penguin", defaultPercent: 0, description: "A beak and two flippers." },
    { id: TraitIds.Owl, layer: TraitLayer.Creature, name: "Owl", defaultPercent: 0, description: "Two oversized concentric eyes." },
    { id: TraitIds.Cthulhu, layer: TraitLayer.Creature, name: "Cthulhu", defaultPercent: 0, description: "Tentacles drooping and slowly waving." },
    { id: TraitIds.Slime, layer: TraitLayer.Creature, name: "Slime", defaultPercent: 0, description: "A drip that forms, falls and resets." },
    { id: TraitIds.Mushroom, layer: TraitLayer.Creature, name: "Mushroom", defaultPercent: 0, description: "A spotted cap sitting on top." },
    { id: TraitIds.Cactus, layer: TraitLayer.Creature, name: "Cactus", defaultPercent: 0, description: "Spines along both flanks and a flower." },
    { id: TraitIds.Robot, layer: TraitLayer.Creature, name: "Robot", defaultPercent: 0, description: "An antenna with a blinking bulb." },
    { id: TraitIds.Pirate, layer: TraitLayer.Creature, name: "Pirate", defaultPercent: 0, description: "An eyepatch band and a bandana knot." },
    { id: TraitIds.ThighHighs, layer: TraitLayer.Costume, name: "Thigh highs", defaultPercent: 4, description: "The lower stroke recoloured, with a welt stripe." },
    { id: TraitIds.TopHat, layer: TraitLayer.Costume, name: "Top hat", defaultPercent: 0, description: "A brim and a block above it." },
    { id: TraitIds.Crown, layer: TraitLayer.Costume, name: "Crown", defaultPercent: 0, description: "A three-point zigzag." },
    { id: TraitIds.Sunglasses, layer: TraitLayer.Costume, name: "Sunglasses", defaultPercent: 0, description: "A dark bar with a bridge notch." },
    { id: TraitIds.Scarf, layer: TraitLayer.Costume, name: "Scarf", defaultPercent: 0, description: "A band with one tail streaming out." },
    { id: TraitIds.Bowtie, layer: TraitLayer.Costume, name: "Bow tie", defaultPercent: 0, description: "Two triangles meeting at the waist." },
    { id: TraitIds.Cape, layer: TraitLayer.Costume, name: "Cape", defaultPercent: 0, description: "A shape behind the stroke." },
    { id: TraitIds.PartyHat, layer: TraitLayer.Costume, name: "Party hat", defaultPercent: 0, description: "A striped cone with a pom." },
    { id: TraitIds.Headphones, layer: TraitLayer.Costume, name: "Headphones", defaultPercent: 0, description: "An arc over the top with two pads." },
    { id: TraitIds.FlowerCrown, layer: TraitLayer.Costume, name: "Flower crown", defaultPercent: 0, description: "Three dots of differing hues." },
    { id: TraitIds.Beanie, layer: TraitLayer.Costume, name: "Beanie", defaultPercent: 0, description: "A rounded cap with a rolled brim." },
    { id: TraitIds.Monocle, layer: TraitLayer.Costume, name: "Monocle", defaultPercent: 0, description: "A ring with a chain hanging down." },
    { id: TraitIds.Necktie, layer: TraitLayer.Costume, name: "Necktie", defaultPercent: 0, description: "A knot and a taper down the front." },
    { id: TraitIds.Backpack, layer: TraitLayer.Costume, name: "Backpack", defaultPercent: 0, description: "A rounded box behind, with straps." },
    { id: TraitIds.Wings, layer: TraitLayer.Costume, name: "Wings", defaultPercent: 0, description: "Feathered arcs on both flanks." },
    { id: TraitIds.Armour, layer: TraitLayer.Costume, name: "Armour", defaultPercent: 0, description: "A plated band with a rivet." },
    { id: TraitIds.Bandage, layer: TraitLayer.Costume, name: "Bandage", defaultPercent: 0, description: "A crossed plaster over the stroke." },
    { id: TraitIds.Moustache, layer: TraitLayer.Costume, name: "Moustache", defaultPercent: 0, description: "A curled bar across the middle." },
    { id: TraitIds.ColourCycle, layer: TraitLayer.Motion, name: "Colour cycle", defaultPercent: 8, description: "Travels the hue wheel forever." },
    { id: TraitIds.Wobble, layer: TraitLayer.Motion, name: "Wobble", defaultPercent: 0, description: "Rotates a few degrees either side." },
    { id: TraitIds.Bounce, layer: TraitLayer.Motion, name: "Bounce", defaultPercent: 0, description: "A short vertical hop on a loop." },
    { id: TraitIds.Breathe, layer: TraitLayer.Motion, name: "Breathe", defaultPercent: 0, description: "Scales slowly between 95 and 105 percent." },
    { id: TraitIds.Heartbeat, layer: TraitLayer.Motion, name: "Heartbeat", defaultPercent: 0, description: "Two quick pulses, then a rest." },
    { id: TraitIds.Shiver, layer: TraitLayer.Motion, name: "Shiver", defaultPercent: 0, description: "Sub-pixel jitter on both axes." },
    { id: TraitIds.Blink, layer: TraitLayer.Motion, name: "Blink", defaultPercent: 0, description: "Fades out and back at irregular intervals." },
    { id: TraitIds.Spin, layer: TraitLayer.Motion, name: "Spin", defaultPercent: 0, description: "Continuous slow rotation." },
    { id: TraitIds.Flip, layer: TraitLayer.Motion, name: "Flip", defaultPercent: 0, description: "Snaps a half turn every few seconds." },
    { id: TraitIds.Glitch, layer: TraitLayer.Motion, name: "Glitch", defaultPercent: 0, description: "Random small offsets with colour fringing." },
    { id: TraitIds.Sparkle, layer: TraitLayer.Motion, name: "Sparkle", defaultPercent: 0, description: "Four-point stars appearing and fading." },
    { id: TraitIds.Drift, layer: TraitLayer.Motion, name: "Drift", defaultPercent: 0, description: "Wanders off its cell and slowly returns." },
    { id: TraitIds.Shimmer, layer: TraitLayer.Motion, name: "Shimmer", defaultPercent: 0, description: "A bright band sweeping down the stroke." },
    { id: TraitIds.Flicker, layer: TraitLayer.Motion, name: "Flicker", defaultPercent: 0, description: "Firelight brightness noise." },
    { id: TraitIds.Wave, layer: TraitLayer.Motion, name: "Wave", defaultPercent: 0, description: "Neighbours bounce in sequence along the line." },
    { id: TraitIds.Typewriter, layer: TraitLayer.Motion, name: "Typewriter", defaultPercent: 0, description: "Fades in once on first appearance." },
    { id: TraitIds.Fire, layer: TraitLayer.Effect, name: "On fire", defaultPercent: 0, description: "Flames licking up from the glyph." },
    { id: TraitIds.GradientFill, layer: TraitLayer.Effect, name: "Gradient fill", defaultPercent: 0, description: "A vertical ramp through the stroke." },
    { id: TraitIds.BoldItalic, layer: TraitLayer.Effect, name: "Bold or italic", defaultPercent: 0, description: "Per-brace weight and slant." },
    { id: TraitIds.Tilted, layer: TraitLayer.Effect, name: "Tilted", defaultPercent: 0, description: "A fixed random lean, stable per brace." },
    { id: TraitIds.Underline, layer: TraitLayer.Effect, name: "Underline", defaultPercent: 0, description: "A squiggle beneath, like a spelling error." },
    { id: TraitIds.Shadow, layer: TraitLayer.Effect, name: "Shadow", defaultPercent: 0, description: "A hard offset duplicate behind the glyph." },
    { id: TraitIds.Distressed, layer: TraitLayer.Effect, name: "Distressed", defaultPercent: 0, description: "Sweat beads and an unsteady shake. Also forced on by the complexity warning." },
    { id: TraitIds.FleeCursor, layer: TraitLayer.Effect, name: "Flee the cursor", defaultPercent: 0, description: "Edges away as the caret nears." },
    { id: TraitIds.Named, layer: TraitLayer.Effect, name: "Named", defaultPercent: 0, description: "A stable generated name, shown on hover." },
    { id: TraitIds.Nocturnal, layer: TraitLayer.Effect, name: "Nocturnal", defaultPercent: 0, description: "Droops as the session wears on." },
    { id: TraitIds.Seasonal, layer: TraitLayer.Effect, name: "Seasonal", defaultPercent: 0, description: "Dresses for the time of year." },
    { id: TraitIds.BuildReactive, layer: TraitLayer.Effect, name: "Build reactive", defaultPercent: 0, description: "Celebrates a green build, sulks at a red one." },
    { id: TraitIds.SwapPlaces, layer: TraitLayer.Effect, name: "Swap places", defaultPercent: 0, description: "Two braces on a line trade positions, briefly." },
    { id: TraitIds.Drunk, layer: TraitLayer.Effect, name: "Drunk", defaultPercent: 0, description: "The lean grows over the session, then resets." },
    { id: TraitIds.Gravity, layer: TraitLayer.Effect, name: "Gravity", defaultPercent: 0, description: "Sags toward the bottom of its cell over time." },
    { id: TraitIds.StageFright, layer: TraitLayer.Effect, name: "Stage fright", defaultPercent: 0, description: "Hides while the caret is on its line." },
    { id: TraitIds.Mitosis, layer: TraitLayer.Effect, name: "Mitosis", defaultPercent: 0, description: "Rarely splits in two; one half drifts away." },
    { id: TraitIds.TableFlip, layer: TraitLayer.Effect, name: "Table flip", defaultPercent: 0, description: "Flips a table into adjacent whitespace, then tidies up." },
    { id: TraitIds.FireBrigade, layer: TraitLayer.Effect, name: "Fire brigade", defaultPercent: 0, description: "Braces crew a fire truck and put out a burning brace." },
];

const BY_ID = new Map<string, TraitInfo>(TRAIT_CATALOG.map((t) => [t.id, t]));

export function findTrait(id: string): TraitInfo | undefined {
    return BY_ID.get(id);
}

export function traitsInLayer(layer: TraitLayer): readonly TraitInfo[] {
    return TRAIT_CATALOG.filter((t) => t.layer === layer);
}
