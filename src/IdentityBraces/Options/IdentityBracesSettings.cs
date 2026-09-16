using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using IdentityBraces.Core;

namespace IdentityBraces.Options
{
    /// <summary>
    /// The extension's settings, backed by a small key/value file under %APPDATA%.
    /// </summary>
    /// <remarks>
    /// Deliberately not a <c>DialogPage</c>-backed registry read. MEF components here are
    /// created on whatever thread the editor happens to be using, long before any options
    /// page exists; a plain file has no service dependency, no UI-thread affinity and no
    /// package auto-load. <see cref="GeneralOptionsPage"/> is only the UI on top of it.
    /// </remarks>
    internal sealed class IdentityBracesSettings
    {
        private static readonly object Gate = new object();
        private static IdentityBracesSettings _current;

        /// <summary>Raised after settings change so open views can refresh.</summary>
        public static event EventHandler Changed;

        public bool Enabled = true;

        public bool ColorCurlyBraces = true;
        public bool ColorParentheses = true;
        public bool ColorSquareBrackets = true;

        /// <summary>
        /// How often each trait comes up, keyed by trait id. Anything absent is zero.
        /// </summary>
        /// <remarks>
        /// Replaces the three fixed percentages. Those were a hard-coded enum with one slider
        /// each, which could not grow past three; this is a table, so a new creature is a row
        /// in the catalogue rather than a new field here, a new enum member, a new options
        /// property and a new branch in the drawing factory.
        /// </remarks>
        public Dictionary<string, int> TraitWeights = DefaultTraitWeights();

        /// <summary>
        /// Master switch for movement. Off leaves animated braces on a static colour and
        /// stops the questioning wobble — for battery, for a projector, or for anyone who
        /// finds motion in a text editor genuinely unpleasant.
        /// </summary>
        public bool EnableMotion = true;

        /// <summary>Frames per second for colour cycling. The editor does not need 60.</summary>
        public int AnimationFrameRate = 18;

        /// <summary>Seconds for an animated brace to travel the full colour wheel.</summary>
        public int CycleSeconds = 6;

        /// <summary>
        /// Seconds between attempts to play a multi-brace scene, per view.
        /// </summary>
        /// <remarks>
        /// An attempt, not a performance: most ticks find no eligible brace on screen, or none
        /// with room to perform, and do nothing. Scenes are meant to be caught out of the
        /// corner of an eye rather than watched, so the default is deliberately slow.
        /// </remarks>
        public int SceneIntervalSeconds = 8;

        /// <summary>A brace with no partner renders as '?'. Doubles as a syntax-error hint.</summary>
        public bool QuestionUnmatched = true;

        /// <summary>
        /// A closing brace gets its own identity rather than inheriting its opener's, so
        /// <c>{</c> and its <c>}</c> disagree about colour and about what they are.
        /// </summary>
        public bool IndependentBraces = true;

        /// <summary>Palette of 32, one theme-following colour, or one colour per nesting level.</summary>
        public BraceColorMode ColorMode = BraceColorMode.Palette;

        /// <summary>
        /// The pair enclosing the caret stays vivid; every other brace drops to
        /// <see cref="SpotlightDimPercent"/>.
        /// </summary>
        /// <remarks>
        /// The counterweight to the whole extension: with thirty-two colours competing, this
        /// is the one thing that tells you which block you are actually in.
        /// </remarks>
        public bool ScopeSpotlight = false;

        /// <summary>How visible an out-of-scope brace is while the spotlight is on. 5-100.</summary>
        public int SpotlightDimPercent = 30;

        /// <summary>
        /// Braces nested at least this deep are drawn visibly distressed. Zero switches it off.
        /// </summary>
        /// <remarks>
        /// The only trait in the catalogue driven by a predicate rather than by its weight —
        /// it is a warning about the code, not a costume, so it either applies or it does not.
        /// </remarks>
        public int ComplexityWarningDepth = 0;

        /// <summary>
        /// Draw a vertical guide down the inside of every multi-line pair, in that pair's own
        /// colour.
        /// </summary>
        /// <remarks>
        /// Visual Studio already draws structure guides; this replaces their uniform grey with
        /// the colour of the pair each one belongs to, so a guide and the braces it joins
        /// agree. Combined with depth colour it is an ordinary — even useful — rainbow indent.
        /// </remarks>
        public bool IndentGuides = false;

        /// <summary>How strong the indent guides are. 5-100.</summary>
        public int IndentGuideOpacityPercent = 45;

        /// <summary>Percentage size of every brace glyph, plain and drawn alike. 25-400.</summary>
        public int BraceScalePercent = 100;

        /// <summary>Percentage size of the cat ears, independent of the brace. 25-400.</summary>
        public int EarScalePercent = 100;

        /// <summary>How much stripe detail the thigh highs carry.</summary>
        public StockingStyle Stocking = StockingStyle.Garter;

        /// <summary>A tail curling off the bottom right. What makes it read as a creature.</summary>
        public bool CatgirlTail = true;

        /// <summary>
        /// Make lines carrying a catgirl brace physically taller so the ears have room.
        /// </summary>
        /// <remarks>
        /// Off by default. This is the only setting that changes the document's height, which
        /// makes it the only one that can disturb the editor's layout, and a clean fallback
        /// exists: with it off the ears simply overhang the line above. That costs a little
        /// tidiness on a few lines per screen and nothing else.
        /// </remarks>
        public bool ReserveEarSpace = false;

        /// <summary>
        /// Files longer than this are left alone. Every keystroke produces a new snapshot
        /// and therefore a full rescan, so this is the guard that keeps typing responsive
        /// in a generated monster of a file.
        /// </summary>
        public int MaxFileLength = 1000000;

        /// <summary>
        /// Write a line per layout pass to %APPDATA%\IdentityBraces\diagnostic.log.
        /// </summary>
        /// <remarks>
        /// Off by default. Kept because it is the only way to diagnose a positioning fault
        /// without a debugger attached; enable it in Tools &gt; Options if one ever recurs.
        /// </remarks>
        public bool DiagnosticLog = false;

        /// <summary>Bumped on every save; used as part of the brace-map cache key.</summary>
        public int Version;

        public static IdentityBracesSettings Current
        {
            get
            {
                IdentityBracesSettings current = Volatile.Read(ref _current);
                if (current != null)
                {
                    return current;
                }

                lock (Gate)
                {
                    if (_current == null)
                    {
                        Volatile.Write(ref _current, Load());
                    }

                    return _current;
                }
            }
        }

        public static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IdentityBraces",
                    "settings.ini");
            }
        }

        public ScanSettings ToScanSettings()
        {
            return new ScanSettings
            {
                Curly = ColorCurlyBraces,
                Round = ColorParentheses,
                Square = ColorSquareBrackets,
                TraitWeights = ToTraitWeights(),
                QuestionUnmatched = QuestionUnmatched,
                IndependentBraces = IndependentBraces,
                ColorByDepth = ColorMode == BraceColorMode.Depth,
                ComplexityWarningDepth = ComplexityWarningDepth,
            };
        }

        public IdentityBracesSettings Clone()
        {
            var copy = (IdentityBracesSettings)MemberwiseClone();

            // MemberwiseClone is shallow, so without this the clone and the original would
            // share one weight table and an edit in the options page would take effect before
            // the user pressed OK — and survive Cancel.
            copy.TraitWeights = new Dictionary<string, int>(TraitWeights);
            return copy;
        }

        public static Dictionary<string, int> DefaultTraitWeights()
        {
            var weights = new Dictionary<string, int>();
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];
                weights[info.Id] = info.DefaultPercent;
            }

            return weights;
        }

        /// <summary>Overwrites every weight with a preset's, leaving other settings alone.</summary>
        public void ApplyPreset(string presetId)
        {
            TraitPreset preset = TraitPresets.Find(presetId);
            if (preset == null)
            {
                return;
            }

            var weights = new Dictionary<string, int>();
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                string id = TraitCatalog.All[i].Id;
                int value;
                weights[id] = preset.Weights.TryGetValue(id, out value) ? value : 0;
            }

            TraitWeights = weights;
        }

        /// <summary>
        /// True if any trait that needs space above the line is enabled.
        /// </summary>
        /// <remarks>
        /// The line transform's cheapest early-out. It runs for every visible line on every
        /// layout pass, and it is the only component that can change document height — so
        /// when nobody has a hat on, it must cost as close to nothing as possible.
        /// </remarks>
        public bool AnyHeadroomTrait()
        {
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];
                if ((info.Layer == TraitLayer.Creature || info.Layer == TraitLayer.Costume)
                    && GetTraitWeight(info.Id) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetTraitWeight(string id)
        {
            int value;
            return TraitWeights.TryGetValue(id, out value) ? value : 0;
        }

        public void SetTraitWeight(string id, int percent)
        {
            TraitWeights[id] = percent < 0 ? 0 : (percent > 100 ? 100 : percent);
        }

        public IList<TraitWeight> ToTraitWeights()
        {
            var list = new List<TraitWeight>(TraitCatalog.All.Count);
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];
                list.Add(new TraitWeight
                {
                    Id = info.Id,
                    Layer = info.Layer,
                    Percent = GetTraitWeight(info.Id),
                });
            }

            return list;
        }

        /// <summary>
        /// Clamps each weight, and caps each shared-roll layer's total at 100.
        /// </summary>
        /// <remarks>
        /// Body, Creature, Costume and Motion each share one 0-100 roll, so weights summing
        /// past 100 would make the traits at the end of the list unreachable — silently, and
        /// only for whoever turned the sliders up. Effects roll independently and are exempt.
        /// </remarks>
        private void NormalizeTraitWeights()
        {
            if (TraitWeights == null)
            {
                TraitWeights = DefaultTraitWeights();
            }

            foreach (TraitLayer layer in new[] { TraitLayer.Body, TraitLayer.Creature, TraitLayer.Costume, TraitLayer.Motion })
            {
                int total = 0;
                var members = new List<string>();

                for (int i = 0; i < TraitCatalog.All.Count; i++)
                {
                    TraitInfo info = TraitCatalog.All[i];
                    if (info.Layer != layer)
                    {
                        continue;
                    }

                    int value = Clamp(GetTraitWeight(info.Id), 0, 100);
                    TraitWeights[info.Id] = value;
                    total += value;
                    members.Add(info.Id);
                }

                if (total <= 100)
                {
                    continue;
                }

                double scale = 100.0 / total;
                for (int i = 0; i < members.Count; i++)
                {
                    TraitWeights[members[i]] = (int)(TraitWeights[members[i]] * scale);
                }
            }

            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];
                if (info.Layer == TraitLayer.Effect)
                {
                    TraitWeights[info.Id] = Clamp(GetTraitWeight(info.Id), 0, 100);
                }
            }
        }

        /// <summary>
        /// Reads the weight table, falling back to the pre-1.2 percentages when it is absent.
        /// </summary>
        /// <remarks>
        /// Someone upgrading has <c>CatgirlPercent=60</c> in their file and no trait rows at
        /// all. Reading zero for everything would silently wipe their setup on first launch,
        /// so the three old keys are migrated onto their trait equivalents instead.
        /// </remarks>
        private static Dictionary<string, int> ReadTraitWeights(Dictionary<string, string> values)
        {
            var weights = new Dictionary<string, int>();
            bool sawAny = false;

            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                string id = TraitCatalog.All[i].Id;
                string raw;
                if (values.TryGetValue("Trait." + id, out raw))
                {
                    int parsed;
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        weights[id] = parsed;
                        sawAny = true;
                        continue;
                    }
                }

                weights[id] = 0;
            }

            if (sawAny)
            {
                return weights;
            }

            weights[TraitIds.ColourCycle] = ReadInt(values, "AnimatedPercent", 8);
            weights[TraitIds.Question] = ReadInt(values, "QuestioningPercent", 6);
            weights[TraitIds.Catgirl] = ReadInt(values, "CatgirlPercent", 4);
            weights[TraitIds.ThighHighs] = weights[TraitIds.Catgirl];
            return weights;
        }

        /// <summary>Persists <paramref name="settings"/> and notifies open views.</summary>
        public static void Save(IdentityBracesSettings settings)
        {
            settings.Normalize();
            settings.Version = Current.Version + 1;

            try
            {
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, settings.Serialize(), Encoding.UTF8);
            }
            catch (IOException)
            {
                // A read-only or roaming-profile failure must not take the editor down.
                // The in-memory value below still applies for this session.
            }
            catch (UnauthorizedAccessException)
            {
            }

            Volatile.Write(ref _current, settings);

            EventHandler handler = Changed;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        private void Normalize()
        {
            NormalizeTraitWeights();

            if (Stocking < StockingStyle.Off || Stocking > StockingStyle.Banded)
            {
                Stocking = StockingStyle.Garter;
            }

            if (ColorMode < BraceColorMode.Palette || ColorMode > BraceColorMode.Depth)
            {
                ColorMode = BraceColorMode.Palette;
            }

            BraceScalePercent = Clamp(BraceScalePercent, 25, 400);
            EarScalePercent = Clamp(EarScalePercent, 25, 400);
            AnimationFrameRate = Clamp(AnimationFrameRate, 1, 60);
            CycleSeconds = Clamp(CycleSeconds, 1, 120);
            SceneIntervalSeconds = Clamp(SceneIntervalSeconds, 1, 600);
            MaxFileLength = Clamp(MaxFileLength, 1000, 100000000);
            SpotlightDimPercent = Clamp(SpotlightDimPercent, 5, 100);
            IndentGuideOpacityPercent = Clamp(IndentGuideOpacityPercent, 5, 100);

            // Zero is off. One would distress every brace in the file, which is not a warning
            // about anything, so the shallowest meaningful threshold is two.
            ComplexityWarningDepth = ComplexityWarningDepth <= 0
                ? 0
                : Clamp(ComplexityWarningDepth, 2, 64);
        }

        private string Serialize()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Identity Braces settings. Edit via Tools > Options > Identity Braces.");
            Append(builder, "Enabled", Enabled);
            Append(builder, "ColorCurlyBraces", ColorCurlyBraces);
            Append(builder, "ColorParentheses", ColorParentheses);
            Append(builder, "ColorSquareBrackets", ColorSquareBrackets);
            foreach (KeyValuePair<string, int> pair in TraitWeights)
            {
                if (pair.Value > 0)
                {
                    Append(builder, "Trait." + pair.Key, pair.Value);
                }
            }
            Append(builder, "EnableMotion", EnableMotion);
            Append(builder, "AnimationFrameRate", AnimationFrameRate);
            Append(builder, "CycleSeconds", CycleSeconds);
            Append(builder, "SceneIntervalSeconds", SceneIntervalSeconds);
            Append(builder, "QuestionUnmatched", QuestionUnmatched);
            Append(builder, "IndependentBraces", IndependentBraces);
            Append(builder, "ColorMode", (int)ColorMode);
            Append(builder, "ScopeSpotlight", ScopeSpotlight);
            Append(builder, "SpotlightDimPercent", SpotlightDimPercent);
            Append(builder, "ComplexityWarningDepth", ComplexityWarningDepth);
            Append(builder, "IndentGuides", IndentGuides);
            Append(builder, "IndentGuideOpacityPercent", IndentGuideOpacityPercent);
            Append(builder, "BraceScalePercent", BraceScalePercent);
            Append(builder, "EarScalePercent", EarScalePercent);
            Append(builder, "Stocking", (int)Stocking);
            Append(builder, "CatgirlTail", CatgirlTail);
            Append(builder, "ReserveEarSpace", ReserveEarSpace);
            Append(builder, "DiagnosticLog", DiagnosticLog);
            Append(builder, "MaxFileLength", MaxFileLength);
            Append(builder, "Version", Version);
            return builder.ToString();
        }

        private static IdentityBracesSettings Load()
        {
            var settings = new IdentityBracesSettings();

            Dictionary<string, string> values;
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                {
                    return settings;
                }

                values = Parse(File.ReadAllLines(path));
            }
            catch (IOException)
            {
                return settings;
            }
            catch (UnauthorizedAccessException)
            {
                return settings;
            }

            settings.Enabled = ReadBool(values, "Enabled", settings.Enabled);
            settings.ColorCurlyBraces = ReadBool(values, "ColorCurlyBraces", settings.ColorCurlyBraces);
            settings.ColorParentheses = ReadBool(values, "ColorParentheses", settings.ColorParentheses);
            settings.ColorSquareBrackets = ReadBool(values, "ColorSquareBrackets", settings.ColorSquareBrackets);
            settings.TraitWeights = ReadTraitWeights(values);
            settings.EnableMotion = ReadBool(values, "EnableMotion", settings.EnableMotion);
            settings.AnimationFrameRate = ReadInt(values, "AnimationFrameRate", settings.AnimationFrameRate);
            settings.CycleSeconds = ReadInt(values, "CycleSeconds", settings.CycleSeconds);
            settings.SceneIntervalSeconds = ReadInt(values, "SceneIntervalSeconds", settings.SceneIntervalSeconds);
            settings.QuestionUnmatched = ReadBool(values, "QuestionUnmatched", settings.QuestionUnmatched);
            settings.IndependentBraces = ReadBool(values, "IndependentBraces", settings.IndependentBraces);
            settings.ColorMode = (BraceColorMode)ReadInt(values, "ColorMode", (int)settings.ColorMode);
            settings.ScopeSpotlight = ReadBool(values, "ScopeSpotlight", settings.ScopeSpotlight);
            settings.SpotlightDimPercent = ReadInt(values, "SpotlightDimPercent", settings.SpotlightDimPercent);
            settings.ComplexityWarningDepth = ReadInt(values, "ComplexityWarningDepth", settings.ComplexityWarningDepth);
            settings.IndentGuides = ReadBool(values, "IndentGuides", settings.IndentGuides);
            settings.IndentGuideOpacityPercent = ReadInt(values, "IndentGuideOpacityPercent", settings.IndentGuideOpacityPercent);
            settings.BraceScalePercent = ReadInt(values, "BraceScalePercent", settings.BraceScalePercent);
            settings.EarScalePercent = ReadInt(values, "EarScalePercent", settings.EarScalePercent);
            settings.Stocking = (StockingStyle)ReadInt(values, "Stocking", (int)settings.Stocking);
            settings.CatgirlTail = ReadBool(values, "CatgirlTail", settings.CatgirlTail);
            settings.ReserveEarSpace = ReadBool(values, "ReserveEarSpace", settings.ReserveEarSpace);
            settings.DiagnosticLog = ReadBool(values, "DiagnosticLog", settings.DiagnosticLog);
            settings.MaxFileLength = ReadInt(values, "MaxFileLength", settings.MaxFileLength);
            settings.Version = ReadInt(values, "Version", 0);

            settings.Normalize();
            return settings;
        }

        private static Dictionary<string, string> Parse(string[] lines)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#')
                {
                    continue;
                }

                int split = trimmed.IndexOf('=');
                if (split <= 0)
                {
                    continue;
                }

                values[trimmed.Substring(0, split).Trim()] = trimmed.Substring(split + 1).Trim();
            }

            return values;
        }

        private static void Append(StringBuilder builder, string key, bool value)
        {
            builder.Append(key).Append('=').AppendLine(value ? "true" : "false");
        }

        private static void Append(StringBuilder builder, string key, int value)
        {
            builder.Append(key).Append('=').AppendLine(value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool ReadBool(Dictionary<string, string> values, string key, bool fallback)
        {
            string raw;
            bool parsed;
            return values.TryGetValue(key, out raw) && bool.TryParse(raw, out parsed) ? parsed : fallback;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int fallback)
        {
            string raw;
            int parsed;
            return values.TryGetValue(key, out raw)
                && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private static double Clamp01(double value)
        {
            return value < 0 ? 0 : (value > 1 ? 1 : value);
        }
    }
}
