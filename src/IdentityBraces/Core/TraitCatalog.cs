using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>One trait's metadata: what it is called, which layer it sits on, how often.</summary>
    internal sealed class TraitInfo
    {
        public string Id;
        public TraitLayer Layer;
        public string Name;
        public int DefaultPercent;
        public string Description;
    }

    /// <summary>
    /// Every trait the extension knows about.
    /// </summary>
    /// <remarks>
    /// The single list driving the roll, the settings file, the presets and the options UI.
    /// Adding a trait is one row here plus one draw function in the drawing registry, and
    /// nothing else changes — which is the entire reason the registry exists.
    /// <para>
    /// Most default to zero. A fresh install should look like the extension people were
    /// shown rather than every switch at once; the presets are there to turn the rest on.
    /// </para>
    /// </remarks>
    internal static class TraitCatalog
    {
        public static readonly IList<TraitInfo> All = new List<TraitInfo>
        {
            new TraitInfo { Id = TraitIds.Question, Layer = TraitLayer.Body, Name = "Question mark", DefaultPercent = 6, Description = "Renders ? instead of the brace." },
            new TraitInfo { Id = TraitIds.UnicodeVariant, Layer = TraitLayer.Body, Name = "Unicode variant", DefaultPercent = 0, Description = "Picks an exotic bracket from the same family." },
            new TraitInfo { Id = TraitIds.WrongBracket, Layer = TraitLayer.Body, Name = "Wrong bracket", DefaultPercent = 0, Description = "Draws a different bracket type entirely." },
            new TraitInfo { Id = TraitIds.Mirrored, Layer = TraitLayer.Body, Name = "Mirrored", DefaultPercent = 0, Description = "Flipped horizontally, so openers close." },
            new TraitInfo { Id = TraitIds.UpsideDown, Layer = TraitLayer.Body, Name = "Upside down", DefaultPercent = 0, Description = "Rotated a half turn in place." },
            new TraitInfo { Id = TraitIds.Emoji, Layer = TraitLayer.Body, Name = "Emoji", DefaultPercent = 0, Description = "Substitutes an emoji. Ignores the palette." },
            new TraitInfo { Id = TraitIds.ForeignFont, Layer = TraitLayer.Body, Name = "Foreign font", DefaultPercent = 0, Description = "Drawn in Comic Sans while everything else stays monospaced." },
            new TraitInfo { Id = TraitIds.Subscript, Layer = TraitLayer.Body, Name = "Subscript", DefaultPercent = 0, Description = "Small, and dropped below the baseline." },
            new TraitInfo { Id = TraitIds.Catgirl, Layer = TraitLayer.Creature, Name = "Cat", DefaultPercent = 4, Description = "Two ears above the glyph." },
            new TraitInfo { Id = TraitIds.Bunny, Layer = TraitLayer.Creature, Name = "Bunny", DefaultPercent = 0, Description = "Tall narrow ears, one flopped at the tip." },
            new TraitInfo { Id = TraitIds.Devil, Layer = TraitLayer.Creature, Name = "Devil", DefaultPercent = 0, Description = "Curved horns and a barbed tail." },
            new TraitInfo { Id = TraitIds.Angel, Layer = TraitLayer.Creature, Name = "Angel", DefaultPercent = 0, Description = "A floating halo ring." },
            new TraitInfo { Id = TraitIds.Ghost, Layer = TraitLayer.Creature, Name = "Ghost", DefaultPercent = 0, Description = "Scalloped lower edge, drawn faint." },
            new TraitInfo { Id = TraitIds.Wizard, Layer = TraitLayer.Creature, Name = "Wizard", DefaultPercent = 0, Description = "A pointed hat with a brim and a star." },
            new TraitInfo { Id = TraitIds.Vampire, Layer = TraitLayer.Creature, Name = "Vampire", DefaultPercent = 0, Description = "Two fangs below the glyph." },
            new TraitInfo { Id = TraitIds.Bee, Layer = TraitLayer.Creature, Name = "Bee", DefaultPercent = 0, Description = "Banded body with a pair of wings." },
            new TraitInfo { Id = TraitIds.Frog, Layer = TraitLayer.Creature, Name = "Frog", DefaultPercent = 0, Description = "Two bulging eyes on the top curve." },
            new TraitInfo { Id = TraitIds.Snake, Layer = TraitLayer.Creature, Name = "Snake", DefaultPercent = 0, Description = "A forked tongue flicking sideways." },
            new TraitInfo { Id = TraitIds.Crab, Layer = TraitLayer.Creature, Name = "Crab", DefaultPercent = 0, Description = "A pincer on each flank." },
            new TraitInfo { Id = TraitIds.Bat, Layer = TraitLayer.Creature, Name = "Bat", DefaultPercent = 0, Description = "Scalloped wings either side." },
            new TraitInfo { Id = TraitIds.Spider, Layer = TraitLayer.Creature, Name = "Spider", DefaultPercent = 0, Description = "Legs radiating outward." },
            new TraitInfo { Id = TraitIds.Fox, Layer = TraitLayer.Creature, Name = "Fox", DefaultPercent = 0, Description = "Sharp ears and a heavy tail." },
            new TraitInfo { Id = TraitIds.Wolf, Layer = TraitLayer.Creature, Name = "Wolf", DefaultPercent = 0, Description = "Outswept ears and a snout." },
            new TraitInfo { Id = TraitIds.Dragon, Layer = TraitLayer.Creature, Name = "Dragon", DefaultPercent = 0, Description = "Swept horns and a folded wing." },
            new TraitInfo { Id = TraitIds.Unicorn, Layer = TraitLayer.Creature, Name = "Unicorn", DefaultPercent = 0, Description = "A single horn, dead centre." },
            new TraitInfo { Id = TraitIds.Penguin, Layer = TraitLayer.Creature, Name = "Penguin", DefaultPercent = 0, Description = "A beak and two flippers." },
            new TraitInfo { Id = TraitIds.Owl, Layer = TraitLayer.Creature, Name = "Owl", DefaultPercent = 0, Description = "Two oversized concentric eyes." },
            new TraitInfo { Id = TraitIds.Cthulhu, Layer = TraitLayer.Creature, Name = "Cthulhu", DefaultPercent = 0, Description = "Tentacles drooping and slowly waving." },
            new TraitInfo { Id = TraitIds.Slime, Layer = TraitLayer.Creature, Name = "Slime", DefaultPercent = 0, Description = "A drip that forms, falls and resets." },
            new TraitInfo { Id = TraitIds.Mushroom, Layer = TraitLayer.Creature, Name = "Mushroom", DefaultPercent = 0, Description = "A spotted cap sitting on top." },
            new TraitInfo { Id = TraitIds.Cactus, Layer = TraitLayer.Creature, Name = "Cactus", DefaultPercent = 0, Description = "Spines along both flanks and a flower." },
            new TraitInfo { Id = TraitIds.Robot, Layer = TraitLayer.Creature, Name = "Robot", DefaultPercent = 0, Description = "An antenna with a blinking bulb." },
            new TraitInfo { Id = TraitIds.Pirate, Layer = TraitLayer.Creature, Name = "Pirate", DefaultPercent = 0, Description = "An eyepatch band and a bandana knot." },
            new TraitInfo { Id = TraitIds.ThighHighs, Layer = TraitLayer.Costume, Name = "Thigh highs", DefaultPercent = 4, Description = "The lower stroke recoloured, with a welt stripe." },
            new TraitInfo { Id = TraitIds.TopHat, Layer = TraitLayer.Costume, Name = "Top hat", DefaultPercent = 0, Description = "A brim and a block above it." },
            new TraitInfo { Id = TraitIds.Crown, Layer = TraitLayer.Costume, Name = "Crown", DefaultPercent = 0, Description = "A three-point zigzag." },
            new TraitInfo { Id = TraitIds.Sunglasses, Layer = TraitLayer.Costume, Name = "Sunglasses", DefaultPercent = 0, Description = "A dark bar with a bridge notch." },
            new TraitInfo { Id = TraitIds.Scarf, Layer = TraitLayer.Costume, Name = "Scarf", DefaultPercent = 0, Description = "A band with one tail streaming out." },
            new TraitInfo { Id = TraitIds.Bowtie, Layer = TraitLayer.Costume, Name = "Bow tie", DefaultPercent = 0, Description = "Two triangles meeting at the waist." },
            new TraitInfo { Id = TraitIds.Cape, Layer = TraitLayer.Costume, Name = "Cape", DefaultPercent = 0, Description = "A shape behind the stroke." },
            new TraitInfo { Id = TraitIds.PartyHat, Layer = TraitLayer.Costume, Name = "Party hat", DefaultPercent = 0, Description = "A striped cone with a pom." },
            new TraitInfo { Id = TraitIds.Headphones, Layer = TraitLayer.Costume, Name = "Headphones", DefaultPercent = 0, Description = "An arc over the top with two pads." },
            new TraitInfo { Id = TraitIds.FlowerCrown, Layer = TraitLayer.Costume, Name = "Flower crown", DefaultPercent = 0, Description = "Three dots of differing hues." },
            new TraitInfo { Id = TraitIds.Beanie, Layer = TraitLayer.Costume, Name = "Beanie", DefaultPercent = 0, Description = "A rounded cap with a rolled brim." },
            new TraitInfo { Id = TraitIds.Monocle, Layer = TraitLayer.Costume, Name = "Monocle", DefaultPercent = 0, Description = "A ring with a chain hanging down." },
            new TraitInfo { Id = TraitIds.Necktie, Layer = TraitLayer.Costume, Name = "Necktie", DefaultPercent = 0, Description = "A knot and a taper down the front." },
            new TraitInfo { Id = TraitIds.Backpack, Layer = TraitLayer.Costume, Name = "Backpack", DefaultPercent = 0, Description = "A rounded box behind, with straps." },
            new TraitInfo { Id = TraitIds.Wings, Layer = TraitLayer.Costume, Name = "Wings", DefaultPercent = 0, Description = "Feathered arcs on both flanks." },
            new TraitInfo { Id = TraitIds.Armour, Layer = TraitLayer.Costume, Name = "Armour", DefaultPercent = 0, Description = "A plated band with a rivet." },
            new TraitInfo { Id = TraitIds.Bandage, Layer = TraitLayer.Costume, Name = "Bandage", DefaultPercent = 0, Description = "A crossed plaster over the stroke." },
            new TraitInfo { Id = TraitIds.Moustache, Layer = TraitLayer.Costume, Name = "Moustache", DefaultPercent = 0, Description = "A curled bar across the middle." },
            new TraitInfo { Id = TraitIds.ColourCycle, Layer = TraitLayer.Motion, Name = "Colour cycle", DefaultPercent = 8, Description = "Travels the hue wheel forever." },
            new TraitInfo { Id = TraitIds.Wobble, Layer = TraitLayer.Motion, Name = "Wobble", DefaultPercent = 0, Description = "Rotates a few degrees either side." },
            new TraitInfo { Id = TraitIds.Bounce, Layer = TraitLayer.Motion, Name = "Bounce", DefaultPercent = 0, Description = "A short vertical hop on a loop." },
            new TraitInfo { Id = TraitIds.Breathe, Layer = TraitLayer.Motion, Name = "Breathe", DefaultPercent = 0, Description = "Scales slowly between 95 and 105 percent." },
            new TraitInfo { Id = TraitIds.Heartbeat, Layer = TraitLayer.Motion, Name = "Heartbeat", DefaultPercent = 0, Description = "Two quick pulses, then a rest." },
            new TraitInfo { Id = TraitIds.Shiver, Layer = TraitLayer.Motion, Name = "Shiver", DefaultPercent = 0, Description = "Sub-pixel jitter on both axes." },
            new TraitInfo { Id = TraitIds.Blink, Layer = TraitLayer.Motion, Name = "Blink", DefaultPercent = 0, Description = "Fades out and back at irregular intervals." },
            new TraitInfo { Id = TraitIds.Spin, Layer = TraitLayer.Motion, Name = "Spin", DefaultPercent = 0, Description = "Continuous slow rotation." },
            new TraitInfo { Id = TraitIds.Flip, Layer = TraitLayer.Motion, Name = "Flip", DefaultPercent = 0, Description = "Snaps a half turn every few seconds." },
            new TraitInfo { Id = TraitIds.Glitch, Layer = TraitLayer.Motion, Name = "Glitch", DefaultPercent = 0, Description = "Random small offsets with colour fringing." },
            new TraitInfo { Id = TraitIds.Sparkle, Layer = TraitLayer.Motion, Name = "Sparkle", DefaultPercent = 0, Description = "Four-point stars appearing and fading." },
            new TraitInfo { Id = TraitIds.Drift, Layer = TraitLayer.Motion, Name = "Drift", DefaultPercent = 0, Description = "Wanders off its cell and slowly returns." },
            new TraitInfo { Id = TraitIds.Shimmer, Layer = TraitLayer.Motion, Name = "Shimmer", DefaultPercent = 0, Description = "A bright band sweeping down the stroke." },
            new TraitInfo { Id = TraitIds.Flicker, Layer = TraitLayer.Motion, Name = "Flicker", DefaultPercent = 0, Description = "Firelight brightness noise." },
            new TraitInfo { Id = TraitIds.Wave, Layer = TraitLayer.Motion, Name = "Wave", DefaultPercent = 0, Description = "Neighbours bounce in sequence along the line." },
            new TraitInfo { Id = TraitIds.Typewriter, Layer = TraitLayer.Motion, Name = "Typewriter", DefaultPercent = 0, Description = "Fades in once on first appearance." },
            new TraitInfo { Id = TraitIds.Fire, Layer = TraitLayer.Effect, Name = "On fire", DefaultPercent = 0, Description = "Flames licking up from the glyph." },
            new TraitInfo { Id = TraitIds.GradientFill, Layer = TraitLayer.Effect, Name = "Gradient fill", DefaultPercent = 0, Description = "A vertical ramp through the stroke." },
            new TraitInfo { Id = TraitIds.BoldItalic, Layer = TraitLayer.Effect, Name = "Bold or italic", DefaultPercent = 0, Description = "Per-brace weight and slant." },
            new TraitInfo { Id = TraitIds.Tilted, Layer = TraitLayer.Effect, Name = "Tilted", DefaultPercent = 0, Description = "A fixed random lean, stable per brace." },
            new TraitInfo { Id = TraitIds.Underline, Layer = TraitLayer.Effect, Name = "Underline", DefaultPercent = 0, Description = "A squiggle beneath, like a spelling error." },
            new TraitInfo { Id = TraitIds.Shadow, Layer = TraitLayer.Effect, Name = "Shadow", DefaultPercent = 0, Description = "A hard offset duplicate behind the glyph." },
            new TraitInfo { Id = TraitIds.Distressed, Layer = TraitLayer.Effect, Name = "Distressed", DefaultPercent = 0, Description = "Sweat beads and an unsteady shake. Also forced on by the complexity warning." },
            new TraitInfo { Id = TraitIds.FleeCursor, Layer = TraitLayer.Effect, Name = "Flee the cursor", DefaultPercent = 0, Description = "Edges away as the caret nears." },
            new TraitInfo { Id = TraitIds.Named, Layer = TraitLayer.Effect, Name = "Named", DefaultPercent = 0, Description = "A stable generated name, shown on hover." },
            new TraitInfo { Id = TraitIds.Nocturnal, Layer = TraitLayer.Effect, Name = "Nocturnal", DefaultPercent = 0, Description = "Droops as the session wears on." },
            new TraitInfo { Id = TraitIds.Seasonal, Layer = TraitLayer.Effect, Name = "Seasonal", DefaultPercent = 0, Description = "Dresses for the time of year." },
            new TraitInfo { Id = TraitIds.BuildReactive, Layer = TraitLayer.Effect, Name = "Build reactive", DefaultPercent = 0, Description = "Celebrates a green build, sulks at a red one." },
            new TraitInfo { Id = TraitIds.SwapPlaces, Layer = TraitLayer.Effect, Name = "Swap places", DefaultPercent = 0, Description = "Two braces on a line trade positions, briefly." },
            new TraitInfo { Id = TraitIds.Drunk, Layer = TraitLayer.Effect, Name = "Drunk", DefaultPercent = 0, Description = "The lean grows over the session, then resets." },
            new TraitInfo { Id = TraitIds.Gravity, Layer = TraitLayer.Effect, Name = "Gravity", DefaultPercent = 0, Description = "Sags toward the bottom of its cell over time." },
            new TraitInfo { Id = TraitIds.StageFright, Layer = TraitLayer.Effect, Name = "Stage fright", DefaultPercent = 0, Description = "Hides while the caret is on its line." },
            new TraitInfo { Id = TraitIds.Mitosis, Layer = TraitLayer.Effect, Name = "Mitosis", DefaultPercent = 0, Description = "Rarely splits in two; one half drifts away." },
            new TraitInfo { Id = TraitIds.TableFlip, Layer = TraitLayer.Effect, Name = "Table flip", DefaultPercent = 0, Description = "Flips a table into adjacent whitespace, then tidies up." },
            new TraitInfo { Id = TraitIds.FireBrigade, Layer = TraitLayer.Effect, Name = "Fire brigade", DefaultPercent = 0, Description = "Braces crew a fire truck and put out a burning brace." },
        };

        public static TraitInfo Find(string id)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }
    }
}
