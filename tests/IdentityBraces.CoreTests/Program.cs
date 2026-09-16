using System;
using System.Collections.Generic;
using System.Linq;
using IdentityBraces.Adornments;
using IdentityBraces.Core;
using IdentityBraces.Options;

internal static class Program
{
    private static int _pass;
    private static int _fail;

    private static void Check(string name, bool condition, string detail = null)
    {
        if (condition)
        {
            _pass++;
            Console.WriteLine("  PASS  " + name);
        }
        else
        {
            _fail++;
            Console.WriteLine("  FAIL  " + name + (detail == null ? "" : "  -> " + detail));
        }
    }

    private static BraceInfo[] Scan(string text, ScanSettings? settings = null)
    {
        return BraceScanner.Scan(text, settings ?? ScanSettings.Default);
    }

    private static ulong IdentityAt(string text, int position, ScanSettings? s = null)
    {
        foreach (BraceInfo b in Scan(text, s))
        {
            if (b.Position == position)
            {
                return b.Identity;
            }
        }

        throw new InvalidOperationException("no brace at " + position);
    }

    private static bool _deferredOk = true;
    private static string _deferredWhy;

    /// <summary>Collapses a loop of assertions into one result, keeping the first failure.</summary>
    private static void Check2(bool condition, string why)
    {
        if (!condition && _deferredOk)
        {
            _deferredOk = false;
            _deferredWhy = why;
        }
    }

    private static string Enclosing(BraceMap map, int position)
    {
        int open, close;
        return map.TryGetEnclosingPair(position, out open, out close)
            ? open + "-" + close
            : "none";
    }

    private static bool HasEffect(BraceInfo brace, string id)
    {
        return CountEffect(brace, id) > 0;
    }

    private static int CountEffect(BraceInfo brace, string id)
    {
        int count = 0;
        string[] effects = brace.Traits.Effects;
        for (int i = 0; effects != null && i < effects.Length; i++)
        {
            if (effects[i] == id)
            {
                count++;
            }
        }

        return count;
    }

    private static int Main()
    {
        Console.WriteLine("== strings and comments ==");
        {
            var r = Scan("var s = \"{ not a brace }\"; { real }");
            Check("braces inside a string are ignored", r.Length == 2, "found " + r.Length);
            Check("the real pair is found", r.Length == 2 && r[0].Character == '{' && r[1].Character == '}');

            r = Scan("// { comment brace }\n{ real }");
            Check("line comment ignored", r.Length == 2, "found " + r.Length);

            r = Scan("/* { block } */ { real }");
            Check("block comment ignored", r.Length == 2, "found " + r.Length);

            r = Scan("var v = @\"line1 { \nline2 }\"; { real }");
            Check("verbatim string spans lines", r.Length == 2, "found " + r.Length);

            r = Scan("var v = \"\"\"\n raw { body }\n\"\"\"; { real }");
            Check("raw string spans lines", r.Length == 2, "found " + r.Length);

            r = Scan("var t = `tpl { hole }`; { real }");
            Check("template literal ignored", r.Length == 2, "found " + r.Length);

            r = Scan("var c = '{'; { real }");
            Check("char literal ignored", r.Length == 2, "found " + r.Length);
        }

        Console.WriteLine();
        Console.WriteLine("== the unterminated-quote hazard ==");
        {
            // A Rust lifetime looks exactly like an opening quote. If the lexer treated it
            // as one it would swallow everything after it.
            var r = Scan("impl Foo {\n  fn a(x: &'a str) { bar(); }\n}");
            Check("lifetime does not swallow the file", r.Length >= 6, "found " + r.Length);

            r = Scan("var s = \"unterminated { \n{ real }");
            Check("unterminated string stops at end of line", r.Length == 2, "found " + r.Length);
        }

        Console.WriteLine();
        Console.WriteLine("== pairing ==");
        {
            var paired = ScanSettings.Default;
            paired.IndependentBraces = false;

            var r = Scan("void F() { if (x) { g(); } }", paired);
            var curly = r.Where(b => b.Kind == BraceKind.Curly).ToArray();
            Check("all curly braces matched", curly.All(b => b.IsMatched), "unmatched: " + curly.Count(b => !b.IsMatched));
            Check("paired mode: outer pair shares identity", curly[0].Identity == curly[3].Identity);
            Check("paired mode: inner pair shares identity", curly[1].Identity == curly[2].Identity);
            Check("nested pairs differ", curly[0].Identity != curly[1].Identity);
            Check("paired mode: a pair shares its colour", curly[0].ColorIndex == curly[3].ColorIndex);

            r = Scan("void F() { g(); ");
            var open = r.First(b => b.Character == '{');
            Check("unmatched open is flagged", !open.IsMatched);
            Check("unmatched brace questions itself", open.Traits.Body == TraitIds.Question);

            // A stray closer must not recolour everything below it.
            string strayed = "void A() { }\n}\nvoid B() { }";
            string clean = "void A() { }\nvoid B() { }";
            var withStray = Scan(strayed).Where(b => b.Kind == BraceKind.Curly).ToArray();
            var withoutStray = Scan(clean).Where(b => b.Kind == BraceKind.Curly).ToArray();
            Check("stray closer does not cascade",
                withStray[withStray.Length - 1].Identity == withoutStray[withoutStray.Length - 1].Identity);
        }

        Console.WriteLine();
        Console.WriteLine("== independent braces (default: on) ==");
        {
            // Default is now that a closer refuses to match its opener.
            var r = Scan("void F() { if (x) { g(); } }");
            var curly = r.Where(b => b.Kind == BraceKind.Curly).ToArray();

            Check("closer does not inherit its opener's identity", curly[0].Identity != curly[3].Identity);
            Check("inner pair also disagrees", curly[1].Identity != curly[2].Identity);
            Check("pairs still marked matched", curly.All(b => b.IsMatched));

            // The derived identity has to be as stable as the one it derives from, or the
            // closers reshuffle on every edit while the openers hold still.
            string before = "void Alpha()\n{\n  A();\n}";
            string after = "using System;\n\nvoid Alpha()\n{\n  A();\n}";
            ulong closerBefore = Scan(before).First(b => b.Character == '}').Identity;
            ulong closerAfter = Scan(after).First(b => b.Character == '}').Identity;
            Check("closer identity survives an insertion above", closerBefore == closerAfter);

            string reformatted = "void   Alpha( )\n{\n\tA();\n}";
            Check("closer identity survives a reformat",
                closerBefore == Scan(reformatted).First(b => b.Character == '}').Identity);

            Check("closer identity changes with a rename",
                closerBefore != Scan("void Gamma()\n{\n  A();\n}").First(b => b.Character == '}').Identity);

            // Stacked closers sit alone on their lines with almost no text to hash. Deriving
            // from the opener is what stops them all collapsing onto one identity.
            var stacked = Scan("void F()\n{\n    if (a)\n    {\n        if (b)\n        {\n            g();\n        }\n    }\n}");
            var closers = stacked.Where(b => b.Character == '}').Select(b => b.Identity).ToArray();
            Check("stacked closers all differ", closers.Distinct().Count() == closers.Length,
                closers.Length + " closers, " + closers.Distinct().Count() + " distinct");

            // Turning it off must restore the old behaviour exactly.
            var off = ScanSettings.Default;
            off.IndependentBraces = false;
            var pairedAgain = Scan("void F() { g(); }", off).Where(b => b.Kind == BraceKind.Curly).ToArray();
            Check("toggling it off restores shared identity",
                pairedAgain[0].Identity == pairedAgain[1].Identity);
        }

        Console.WriteLine();
        Console.WriteLine("== identity stability (the whole point) ==");
        {
            string allman = "public void Foo()\n{\n    Bar();\n}";
            var r = Scan(allman);
            var open = r.First(b => b.Character == '{');
            Check("Allman brace resolves to the signature above it",
                open.Identity == IdentityAt("public void Foo() {\n    Bar();\n}", 18),
                "Allman and K&R disagree");

            // Two different methods must not collide.
            string two = "void Alpha()\n{\n  A();\n}\nvoid Beta()\n{\n  B();\n}";
            var braces = Scan(two).Where(b => b.Character == '{').ToArray();
            Check("different signatures get different identities", braces[0].Identity != braces[1].Identity);

            // Inserting a line above must not reincarnate anything.
            string before = "void Alpha()\n{\n  A();\n}";
            string after = "using System;\n\nvoid Alpha()\n{\n  A();\n}";
            var idBefore = Scan(before).First(b => b.Character == '{').Identity;
            var idAfter = Scan(after).First(b => b.Character == '{').Identity;
            Check("inserting a line above preserves identity", idBefore == idAfter);

            // Typing inside the body must not change the block's identity.
            string edited = "void Alpha()\n{\n  A(); B(); C();\n}";
            Check("editing the body preserves identity",
                idBefore == Scan(edited).First(b => b.Character == '{').Identity);

            // Reformatting must not reshuffle colours.
            string reformatted = "void   Alpha( )\n{\n\tA();\n}";
            Check("whitespace-only reformat preserves identity",
                idBefore == Scan(reformatted).First(b => b.Character == '{').Identity);

            // Renaming is the one thing that SHOULD change identity.
            string renamed = "void Gamma()\n{\n  A();\n}";
            Check("renaming the method changes identity",
                idBefore != Scan(renamed).First(b => b.Character == '{').Identity);
        }

        Console.WriteLine();
        Console.WriteLine("== distribution ==");
        {
            // Build a realistic-ish corpus of distinct declarations.
            var lines = new List<string>();
            for (int i = 0; i < 4000; i++)
            {
                lines.Add("void Method" + i + "(int a" + i + ")\n{\n    Call" + i + "();\n}");
            }

            var settings = ScanSettings.Default;
            settings.QuestionUnmatched = false;
            BraceInfo[] all = Scan(string.Join("\n", lines), settings);
            BraceInfo[] opens = all.Where(b => b.IsOpen && b.Kind == BraceKind.Curly).ToArray();

            var colorCounts = new int[32];
            foreach (BraceInfo b in opens)
            {
                colorCounts[b.ColorIndex]++;
            }

            double expected = opens.Length / 32.0;
            double worst = colorCounts.Max(c => Math.Abs(c - expected) / expected);
            Check("colours spread evenly across 32 buckets (max deviation < 25%)", worst < 0.25,
                "worst deviation " + worst.ToString("P1"));

            // Closers are a salted re-mix of their openers, so their distribution has to be
            // checked separately — a weak mix would cluster them.
            BraceInfo[] closes = all.Where(b => !b.IsOpen && b.Kind == BraceKind.Curly).ToArray();
            var closeCounts = new int[32];
            foreach (BraceInfo b in closes)
            {
                closeCounts[b.ColorIndex]++;
            }

            double expectedClose = closes.Length / 32.0;
            double worstClose = closeCounts.Max(c => Math.Abs(c - expectedClose) / expectedClose);
            Check("closer colours spread evenly too (max deviation < 25%)", worstClose < 0.25,
                "worst deviation " + worstClose.ToString("P1"));

            int agree = 0;
            for (int i = 0; i < opens.Length; i++)
            {
                if (opens[i].ColorIndex == closes[i].ColorIndex)
                {
                    agree++;
                }
            }

            // With 32 buckets a pair should agree by luck about 1 time in 32.
            double agreement = agree / (double)opens.Length;
            Console.WriteLine("        pairs that agree by chance " + agreement.ToString("P2") + " (want ~3.1%)");
            Check("pairs agree only by coincidence", Math.Abs(agreement - 1.0 / 32.0) < 0.02,
                agreement.ToString("P2"));

            double animated = opens.Count(b => b.Traits.Motion == TraitIds.ColourCycle) / (double)opens.Length;
            double questioning = opens.Count(b => b.Traits.Body == TraitIds.Question) / (double)opens.Length;
            double catgirl = opens.Count(b => b.Traits.Creature == TraitIds.Catgirl) / (double)opens.Length;

            Console.WriteLine("        animated " + animated.ToString("P2") + " (want 8%)");
            Console.WriteLine("        questioning " + questioning.ToString("P2") + " (want 6%)");
            Console.WriteLine("        catgirl " + catgirl.ToString("P2") + " (want 4%)");

            Check("animated rate near 8%", Math.Abs(animated - 0.08) < 0.015);
            Check("questioning rate near 6%", Math.Abs(questioning - 0.06) < 0.015);
            Check("catgirl rate near 4%", Math.Abs(catgirl - 0.04) < 0.015);
        }

        Console.WriteLine();
        Console.WriteLine("== settings ==");
        {
            var onlyCurly = ScanSettings.Default;
            onlyCurly.Round = false;
            onlyCurly.Square = false;
            var r = Scan("void F(int[] a) { }", onlyCurly);
            Check("disabled bracket kinds are skipped", r.All(b => b.Kind == BraceKind.Curly), "got kinds: " +
                string.Join(",", r.Select(b => b.Kind.ToString()).Distinct()));
        }

        Console.WriteLine();
        Console.WriteLine("== depth, partner and parent ==");
        {
            //          0123456789012
            var text = "{ a { b } c }";
            BraceInfo[] r = Scan(text);

            Check("four braces found", r.Length == 4, "found " + r.Length);
            Check("outer opener is depth 0", r[0].Depth == 0, "" + r[0].Depth);
            Check("inner opener is depth 1", r[1].Depth == 1, "" + r[1].Depth);
            Check("a closer reports its opener's depth", r[2].Depth == 1 && r[3].Depth == 0,
                r[2].Depth + "," + r[3].Depth);
            Check("partners point at each other",
                r[0].PartnerIndex == 3 && r[3].PartnerIndex == 0
                && r[1].PartnerIndex == 2 && r[2].PartnerIndex == 1);
            Check("the outer pair has no parent", r[0].ParentIndex == -1 && r[3].ParentIndex == -1,
                r[0].ParentIndex + "," + r[3].ParentIndex);
            Check("the inner pair's parent is the outer opener",
                r[1].ParentIndex == 0 && r[2].ParentIndex == 0,
                r[1].ParentIndex + "," + r[2].ParentIndex);

            // Deep nesting: depth must keep counting, and every closer must agree with its
            // opener. Depth hue and the complexity warning both read this.
            r = Scan("{{{{{ x }}}}}");
            bool depthsOk = true;
            for (int i = 0; i < 5; i++)
            {
                if (r[i].Depth != i || r[r[i].PartnerIndex].Depth != i)
                {
                    depthsOk = false;
                }
            }

            Check("nested depth counts up and pairs agree", depthsOk);

            r = Scan("} { a }");
            Check("a stray closer still records a depth", r[0].Depth == 0 && !r[0].IsMatched);
            Check("a stray closer has no partner", r[0].PartnerIndex == -1);

            r = Scan("{ a\n{ b }\n");
            Check("an unclosed opener has no partner", r[0].PartnerIndex == -1 && !r[0].IsMatched);
            Check("braces below an unclosed opener still pair",
                r[1].PartnerIndex == 2 && r[2].PartnerIndex == 1);
        }

        Console.WriteLine();
        Console.WriteLine("== enclosing pair (scope spotlight) ==");
        {
            //          0123456789012
            var map = new BraceMap(Scan("{ a { b } c }"));

            Check("caret inside the inner block finds the inner pair", Enclosing(map, 6) == "1-2",
                Enclosing(map, 6));
            Check("caret on the inner opener finds the inner pair", Enclosing(map, 4) == "1-2",
                Enclosing(map, 4));
            Check("caret on the inner closer finds the inner pair", Enclosing(map, 8) == "1-2",
                Enclosing(map, 8));
            Check("caret between the two blocks finds the outer pair", Enclosing(map, 10) == "0-3",
                Enclosing(map, 10));
            Check("caret on the outer opener finds the outer pair", Enclosing(map, 0) == "0-3",
                Enclosing(map, 0));
            Check("caret on the outer closer finds the outer pair", Enclosing(map, 12) == "0-3",
                Enclosing(map, 12));
            Check("caret past every brace has no scope", Enclosing(map, 13) == "none",
                Enclosing(map, 13));

            int open, close;
            Check("the parent of the inner pair is the outer pair",
                map.TryGetEnclosingPair(6, out open, out close)
                && map.TryGetParentPair(open, out open, out close)
                && open == 0 && close == 3);
            Check("the outer pair has no parent",
                map.TryGetEnclosingPair(10, out open, out close)
                && !map.TryGetParentPair(open, out open, out close));

            // An opener that never closed is the recorded parent of everything after it while
            // not being a pair at all. Walking out must step over it, not stop on it.
            map = new BraceMap(Scan("[ b\n{ ( a ) }"));
            Check("an unmatched opener does not become the scope",
                Enclosing(map, 8) == "2-3", Enclosing(map, 8));

            map = new BraceMap(Scan("( { a } )"));
            Check("scope works across bracket families", Enclosing(map, 5) == "1-2",
                Enclosing(map, 5));

            Check("an empty map has no scope", Enclosing(new BraceMap(Scan("no braces")), 3) == "none");
        }

        Console.WriteLine();
        Console.WriteLine("== depth hue ==");
        {
            ScanSettings byDepth = ScanSettings.Default;
            byDepth.ColorByDepth = true;

            BraceInfo[] r = Scan("{{{{ x }}}}", byDepth);
            Check("colour index is the nesting depth",
                r[0].ColorIndex == 0 && r[1].ColorIndex == 1
                && r[2].ColorIndex == 2 && r[3].ColorIndex == 3);
            Check("a pair shares one depth colour",
                r[0].ColorIndex == r[r[0].PartnerIndex].ColorIndex
                && r[1].ColorIndex == r[r[1].PartnerIndex].ColorIndex);

            // Depth colour must not depend on the text at all, which is the whole difference
            // from identity colour: two identically-nested blocks look the same.
            BraceInfo[] a = Scan("void One() { if (x) { } }", byDepth);
            BraceInfo[] b = Scan("void Two() { if (y) { } }", byDepth);
            bool sameByDepth = a.Length == b.Length;
            for (int i = 0; sameByDepth && i < a.Length; i++)
            {
                sameByDepth = a[i].ColorIndex == b[i].ColorIndex;
            }

            Check("identically nested code colours identically by depth", sameByDepth);

            BraceInfo[] byIdentity = Scan("void Two() { if (y) { } }");
            bool anyDifferent = false;
            for (int i = 0; i < byIdentity.Length; i++)
            {
                if (byIdentity[i].ColorIndex != b[i].ColorIndex)
                {
                    anyDifferent = true;
                }
            }

            Check("identity mode still disagrees with depth mode", anyDifferent);

            // Deeper than the palette must wrap rather than throw or clamp.
            r = Scan(new string('{', 40) + "x" + new string('}', 40), byDepth);
            Check("depth wraps around the palette", r[32].ColorIndex == 0 && r[33].ColorIndex == 1,
                r[32].ColorIndex + "," + r[33].ColorIndex);
        }

        Console.WriteLine();
        Console.WriteLine("== complexity warning ==");
        {
            ScanSettings warn = ScanSettings.Default;
            warn.ComplexityWarningDepth = 3;

            BraceInfo[] r = Scan("{{{{{ x }}}}}", warn);
            Check("shallow braces are left alone",
                !HasEffect(r[0], TraitIds.Distressed) && !HasEffect(r[2], TraitIds.Distressed));
            Check("braces at the threshold are distressed", HasEffect(r[3], TraitIds.Distressed));
            Check("braces past the threshold are distressed", HasEffect(r[4], TraitIds.Distressed));
            Check("a distressed closer matches its opener",
                HasEffect(r[r[3].PartnerIndex], TraitIds.Distressed));
            Check("distress makes the brace drawn", r[3].IsAdorned);

            // Off by default, and off means off however deep the file goes.
            r = Scan("{{{{{ x }}}}}");
            bool anyDistress = false;
            for (int i = 0; i < r.Length; i++)
            {
                anyDistress |= HasEffect(r[i], TraitIds.Distressed);
            }

            Check("the warning is off by default", !anyDistress);

            // Forced on top of a rolled effect, never instead of it, and never twice.
            warn.TraitWeights = ScanSettings.DefaultWeights();
            for (int i = 0; i < warn.TraitWeights.Count; i++)
            {
                TraitWeight w = warn.TraitWeights[i];
                if (w.Id == TraitIds.Distressed || w.Id == TraitIds.Shadow)
                {
                    w.Percent = 100;
                    warn.TraitWeights[i] = w;
                }
            }

            r = Scan("{{{{{ x }}}}}", warn);
            Check("a rolled effect survives being distressed", HasEffect(r[4], TraitIds.Shadow));
            Check("distress is never applied twice", CountEffect(r[4], TraitIds.Distressed) == 1,
                "" + CountEffect(r[4], TraitIds.Distressed));

            Check("the catalogue carries the distress trait",
                TraitCatalog.All.Any(t => t.Id == TraitIds.Distressed));
        }

        Console.WriteLine();
        Console.WriteLine("== settings reach the scanner ==");
        {
            // Constructed directly, never loaded or saved: IdentityBracesSettings.Save writes
            // to the real %APPDATA% file, and a test suite must not edit the settings of
            // whoever is running it.
            var settings = new IdentityBracesSettings();

            Check("depth mode is off by default", !settings.ToScanSettings().ColorByDepth);
            Check("the complexity warning is off by default",
                settings.ToScanSettings().ComplexityWarningDepth == 0);

            settings.ColorMode = BraceColorMode.Depth;
            Check("depth mode reaches the scanner", settings.ToScanSettings().ColorByDepth);

            settings.ColorMode = BraceColorMode.Monochrome;
            Check("monochrome does not colour by depth", !settings.ToScanSettings().ColorByDepth);

            settings.ComplexityWarningDepth = 5;
            Check("the warning depth reaches the scanner",
                settings.ToScanSettings().ComplexityWarningDepth == 5);

            // The clone must be deep, or the options page would edit live settings before OK
            // and the edit would survive Cancel.
            settings.SetTraitWeight(TraitIds.Wizard, 11);
            IdentityBracesSettings copy = settings.Clone();
            copy.SetTraitWeight(TraitIds.Wizard, 77);
            copy.ComplexityWarningDepth = 9;

            Check("Clone deep-copies the weight table", settings.GetTraitWeight(TraitIds.Wizard) == 11,
                "" + settings.GetTraitWeight(TraitIds.Wizard));
            Check("Clone carries the new fields", copy.ScopeSpotlight == settings.ScopeSpotlight
                && copy.IndentGuides == settings.IndentGuides);
            Check("editing the clone leaves the original alone", settings.ComplexityWarningDepth == 5);

            // Every trait id in the catalogue must be a settings key, or a trait would be
            // rollable but impossible to configure.
            bool everyTraitHasAWeight = true;
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                if (!IdentityBracesSettings.DefaultTraitWeights().ContainsKey(TraitCatalog.All[i].Id))
                {
                    everyTraitHasAWeight = false;
                }
            }

            Check("every catalogued trait has a settings key", everyTraitHasAWeight);
        }

        Console.WriteLine();
        Console.WriteLine("== preview sampler ==");
        {
            // The options page draws whatever this produces, so a preview that lies about the
            // catalogue starts here. Every claim below is one the page relies on.
            var settings = new IdentityBracesSettings();
            TraitSampler.Sample[] samples = TraitSampler.Take(settings.ToTraitWeights(), 120);

            Check("the sampler returns what was asked for", samples.Length == 120, "" + samples.Length);

            // Stability is the whole reason for a fixed seed: nudge one weight and only the
            // braces that weight governs may change.
            TraitSampler.Sample[] again = TraitSampler.Take(settings.ToTraitWeights(), 120);
            bool identitiesStable = true;
            for (int i = 0; i < samples.Length; i++)
            {
                identitiesStable &= samples[i].Identity == again[i].Identity;
            }

            Check("identities are stable across rolls", identitiesStable);

            settings.SetTraitWeight(TraitIds.Wizard, 40);
            TraitSampler.Sample[] tweaked = TraitSampler.Take(settings.ToTraitWeights(), 120);
            bool stillStable = true;
            for (int i = 0; i < samples.Length; i++)
            {
                stillStable &= samples[i].Identity == tweaked[i].Identity;
            }

            Check("changing a weight does not reshuffle the sample set", stillStable);

            bool anyWizard = false;
            for (int i = 0; i < tweaked.Length; i++)
            {
                anyWizard |= tweaked[i].Traits.Creature == TraitIds.Wizard;
            }

            Check("turning a weight up makes it show up in the preview", anyWizard);

            // The strip has to be big enough to represent a 4% trait, or every slider below
            // about a tenth reads as "nothing happens".
            var shipped = new IdentityBracesSettings();
            TraitSampler.Sample[] atDefaults = TraitSampler.Take(shipped.ToTraitWeights(), 120);
            int drawn = 0;
            for (int i = 0; i < atDefaults.Length; i++)
            {
                if (atDefaults[i].Traits.IsDrawn)
                {
                    drawn++;
                }
            }

            Check("the default sample is mostly plain, as a real file is", drawn > 5 && drawn < 60,
                drawn + " of 120 drawn");

            Check("an empty request is not an error", TraitSampler.Take(shipped.ToTraitWeights(), 0).Length == 0);
            Check("a negative request is not an error", TraitSampler.Take(shipped.ToTraitWeights(), -3).Length == 0);
            Check("no weights at all yields plain braces",
                TraitSampler.Take(new List<TraitWeight>(), 8).All(x => !x.Traits.IsDrawn));
        }

        Console.WriteLine();
        Console.WriteLine("== preview depth ramp ==");
        {
            bool inRange = true;
            for (int i = 0; i < 500; i++)
            {
                int d = TraitSampler.DepthOf(i);
                inRange &= d >= 0 && d <= TraitSampler.DeepestSample;
            }

            Check("the depth ramp stays within its bounds", inRange);
            Check("the ramp descends and comes back out",
                TraitSampler.DepthOf(0) == 0
                && TraitSampler.DepthOf(5) == 5
                && TraitSampler.DepthOf(10) == 0,
                TraitSampler.DepthOf(0) + "," + TraitSampler.DepthOf(5) + "," + TraitSampler.DepthOf(10));

            Check("consecutive samples differ by one level",
                Enumerable.Range(0, 60).All(i => Math.Abs(TraitSampler.DepthOf(i + 1) - TraitSampler.DepthOf(i)) == 1));

            // Fed straight into the palette index, so it must never be negative.
            Check("a negative index does not produce a negative depth", TraitSampler.DepthOf(-7) >= 0);
        }

        Console.WriteLine();
        Console.WriteLine("== preview of a single trait ==");
        {
            // Exhaustive on purpose. This is the mapping that decides what the options page
            // shows when you click a trait, and a layer wired to the wrong field would show
            // the wrong thing for a quarter of the catalogue without ever throwing.
            bool everyTraitLandsOnItsOwnLayer = true;
            bool everyTraitIsDrawable = true;
            bool nothingElseIsSet = true;

            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];
                BraceTraits traits = TraitSampler.Single(info.Layer, info.Id);

                everyTraitIsDrawable &= traits.IsDrawn;

                int set = 0;
                set += traits.Body == null ? 0 : 1;
                set += traits.Creature == null ? 0 : 1;
                set += traits.Costume == null ? 0 : 1;
                set += traits.Motion == null ? 0 : 1;
                set += traits.Effects.Length;
                nothingElseIsSet &= set == 1;

                string landed =
                    traits.Body ?? traits.Creature ?? traits.Costume ?? traits.Motion
                    ?? (traits.Effects.Length > 0 ? traits.Effects[0] : null);

                bool right;
                switch (info.Layer)
                {
                    case TraitLayer.Body: right = traits.Body == info.Id; break;
                    case TraitLayer.Creature: right = traits.Creature == info.Id; break;
                    case TraitLayer.Costume: right = traits.Costume == info.Id; break;
                    case TraitLayer.Motion: right = traits.Motion == info.Id; break;
                    default: right = traits.Effects.Length == 1 && traits.Effects[0] == info.Id; break;
                }

                if (!right || landed != info.Id)
                {
                    everyTraitLandsOnItsOwnLayer = false;
                    Console.WriteLine("        misrouted: " + info.Id + " (" + info.Layer + ") -> " + landed);
                }
            }

            Check("every catalogued trait previews on its own layer", everyTraitLandsOnItsOwnLayer);
            Check("a single-trait preview sets exactly one trait", nothingElseIsSet);
            Check("a single-trait preview is always drawn", everyTraitIsDrawable);
        }

        Console.WriteLine();
        Console.WriteLine("== preview geometry ==");
        {
            var consolas = new System.Windows.Media.Typeface("Consolas");

            GlyphContext editorSize = PreviewMetrics.Build(consolas, 13.33, 1.0, true, 1.0);
            Console.WriteLine("        Consolas 13.33px cell = "
                + editorSize.CellWidth.ToString("0.00") + " x " + editorSize.TextHeight.ToString("0.00"));

            // A zero cell width stacks every sample on the last one, and the strip renders as a
            // single smear that looks exactly like the feature being broken.
            Check("the cell has a real width", editorSize.CellWidth > 1, "" + editorSize.CellWidth);
            Check("the cell has a real height", editorSize.TextHeight > 1, "" + editorSize.TextHeight);
            Check("the baseline sits inside the cell",
                editorSize.BaselineOffset > 0 && editorSize.BaselineOffset <= editorSize.TextHeight,
                editorSize.BaselineOffset + " of " + editorSize.TextHeight);

            // Consolas is monospace at 0.55 em. Anything wildly off that means the advance is
            // being read as ink, which would pack the samples tighter than real code.
            Check("the cell is about half an em wide",
                editorSize.CellWidth > 13.33 * 0.4 && editorSize.CellWidth < 13.33 * 0.8,
                "" + editorSize.CellWidth);

            GlyphContext magnified = PreviewMetrics.Build(consolas, 58.0, 1.0, true, 1.0);
            Check("the magnified context scales up",
                magnified.CellWidth > editorSize.CellWidth * 3
                && magnified.TextHeight > editorSize.TextHeight * 3,
                magnified.CellWidth + " x " + magnified.TextHeight);

            // The magnified preview sits in a fixed 112px frame. If a 58px font needs more than
            // that even before ear headroom, the frame will clip every hat in the catalogue.
            Check("a 58px sample fits the preview frame", magnified.TextHeight < 100,
                "" + magnified.TextHeight);

            Check("settings carry through", PreviewMetrics.Build(consolas, 20, 1.0, false, 2.5).BraceScale == 2.5);
            Check("the theme carries through", !PreviewMetrics.Build(consolas, 20, 1.0, false, 1).IsDarkTheme);

            // Every one of these has reached a renderer at some point in this project's life.
            Check("a zero font size is repaired", PreviewMetrics.Build(consolas, 0, 1.0, true, 1).CellWidth > 1);
            Check("a negative font size is repaired", PreviewMetrics.Build(consolas, -9, 1.0, true, 1).CellWidth > 1);
            Check("a zero DPI is repaired", PreviewMetrics.Build(consolas, 13.33, 0, true, 1).PixelsPerDip > 0);
            Check("a null typeface is repaired", PreviewMetrics.Build(null, 13.33, 1.0, true, 1).CellWidth > 1);

            GlyphContext missing = PreviewMetrics.Build(
                new System.Windows.Media.Typeface("NoSuchFontExistsAnywhere"), 13.33, 1.0, true, 1);
            Check("a missing font still yields a usable cell",
                missing.CellWidth > 1 && missing.TextHeight > 1,
                missing.CellWidth + " x " + missing.TextHeight);

            // The preview and the adornment layer measure the same glyph two different ways.
            // They have to agree, or a trait tuned in the dialog would not look the same in the
            // editor.
            GlyphInk ink = GlyphMetrics.Measure(consolas, 13.33, '{', 1.0);
            Check("preview cell is wider than the glyph's ink, as a cell should be",
                editorSize.CellWidth >= ink.Right - ink.Left,
                editorSize.CellWidth + " vs ink " + (ink.Right - ink.Left));
            Check("preview height covers the glyph's ink",
                editorSize.TextHeight >= ink.Height,
                editorSize.TextHeight + " vs ink " + ink.Height);
        }

        Console.WriteLine();
        Console.WriteLine("== scene casting: room ==");
        {
            const int Limit = 24;

            //                    0123456789012345
            const string Dense = "if (x) { y(); } z";

            // The brace at 7 is followed by a space then 'y'. One column of room is not a
            // stage; a table thrown there lands on the code.
            Check("a crowded brace has almost no room right",
                SceneCasting.RoomRightOf(Dense, 7, Limit) == 1,
                "" + SceneCasting.RoomRightOf(Dense, 7, Limit));

            // The commonest case by far: a brace at the end of its line has the whole rest of
            // the row, and end-of-text is open space rather than a wall.
            Check("a brace at the end of its line has the run of it",
                SceneCasting.RoomRightOf("    }", 4, Limit) == Limit,
                "" + SceneCasting.RoomRightOf("    }", 4, Limit));

            Check("trailing blanks then end of line still count as open",
                SceneCasting.RoomRightOf("}   ", 0, Limit) == Limit);

            Check("room stops at the next character",
                SceneCasting.RoomRightOf("}    x", 0, Limit) == 4,
                "" + SceneCasting.RoomRightOf("}    x", 0, Limit));

            Check("tabs are room too", SceneCasting.RoomRightOf("}\t\tx", 0, Limit) == 2,
                "" + SceneCasting.RoomRightOf("}\t\tx", 0, Limit));

            Check("the limit is respected", SceneCasting.RoomRightOf("}         x", 0, 3) == 3);
            Check("a zero limit yields no room", SceneCasting.RoomRightOf("}      ", 0, 0) == 0);

            // Leading indentation is the usual source of room on the left.
            Check("an indented brace has room to its left",
                SceneCasting.RoomLeftOf("    }", 4, Limit) == 4,
                "" + SceneCasting.RoomLeftOf("    }", 4, Limit));

            // The start of the line is a hard wall: past it is the margin, where the line
            // numbers and breakpoint glyphs live.
            Check("the start of the line is a wall",
                SceneCasting.RoomLeftOf("}", 0, Limit) == 0);
            Check("room left stops at the previous character",
                SceneCasting.RoomLeftOf("x   }", 4, Limit) == 3,
                "" + SceneCasting.RoomLeftOf("x   }", 4, Limit));

            // Defensive: a line we cannot read must not read as wide open, or the director
            // would throw a prop across text it never looked at.
            Check("an unreadable line offers no room right", SceneCasting.RoomRightOf(null, 3, Limit) == 0);
            Check("an unreadable line offers no room left", SceneCasting.RoomLeftOf(null, 3, Limit) == 0);
            Check("an empty line is open to the right", SceneCasting.RoomRightOf("", 0, Limit) == Limit);
        }

        Console.WriteLine();
        Console.WriteLine("== scene casting: eligibility ==");
        {
            ScanSettings flippy = ScanSettings.Default;
            flippy.TraitWeights = ScanSettings.DefaultWeights();
            for (int i = 0; i < flippy.TraitWeights.Count; i++)
            {
                TraitWeight w = flippy.TraitWeights[i];
                if (w.Id == TraitIds.TableFlip)
                {
                    w.Percent = 100;
                    flippy.TraitWeights[i] = w;
                }
            }

            var text = "{ a } { b } { c }";
            var map = new BraceMap(Scan(text, flippy));
            var found = new List<int>();

            SceneCasting.FindCandidates(map, TraitIds.TableFlip, 0, text.Length, found);
            Check("every brace is eligible when the trait is certain", found.Count == map.Count,
                found.Count + " of " + map.Count);

            // Only what is on screen. A scene played on a line nobody is looking at is heat.
            SceneCasting.FindCandidates(map, TraitIds.TableFlip, 0, 5, found);
            Check("casting is bounded by the visible span", found.Count == 2, "" + found.Count);

            SceneCasting.FindCandidates(map, TraitIds.Fire, 0, text.Length, found);
            Check("a trait nobody carries casts nobody", found.Count == 0, "" + found.Count);

            var stale = new List<int> { 99 };
            SceneCasting.FindCandidates(map, TraitIds.Fire, 0, text.Length, stale);
            Check("the candidate list is cleared before use", stale.Count == 0);

            SceneCasting.FindCandidates(null, TraitIds.TableFlip, 0, 10, found);
            Check("a missing map casts nobody", found.Count == 0);

            var plain = new BraceMap(Scan(text));
            var plain2 = new List<int>();
            SceneCasting.FindCandidates(plain, TraitIds.TableFlip, 0, text.Length, plain2);
            Check("the trait is off by default, so nothing is cast", plain2.Count == 0, "" + plain2.Count);
        }

        Console.WriteLine();
        Console.WriteLine("== scene casting: choosing ==");
        {
            var three = new List<int> { 4, 9, 14 };

            bool alwaysFromTheList = true;
            var seen = new HashSet<int>();
            for (ulong t = 0; t < 400; t++)
            {
                int picked = SceneCasting.Choose(three, t);
                alwaysFromTheList &= three.Contains(picked);
                seen.Add(picked);
            }

            Check("the choice always comes from the candidates", alwaysFromTheList);
            Check("every candidate gets a turn", seen.Count == 3, "" + seen.Count);

            // Deterministic, so a scene that misbehaves can be reproduced from its tick.
            Check("the same seed picks the same actor",
                SceneCasting.Choose(three, 77) == SceneCasting.Choose(three, 77));

            Check("an empty cast returns nothing", SceneCasting.Choose(new List<int>(), 3) == -1);
            Check("a missing cast returns nothing", SceneCasting.Choose(null, 3) == -1);
            Check("a single candidate is always chosen", SceneCasting.Choose(new List<int> { 7 }, 12345) == 7);
        }

        Console.WriteLine();
        Console.WriteLine("== brace names ==");
        {
            // Same property the colours and traits have, and for the same reason: two people
            // looking at the same file have to agree about which one is Reginald.
            var text = "void Method() { if (x) { y(); } }";
            BraceInfo[] first = Scan(text);
            BraceInfo[] second = Scan(text);

            bool stable = true;
            for (int i = 0; i < first.Length; i++)
            {
                stable &= BraceNames.Of(first[i].Identity) == BraceNames.Of(second[i].Identity);
            }

            Check("a brace keeps its name across scans", stable);
            Check("the same identity always gives the same name",
                BraceNames.Of(1234567UL) == BraceNames.Of(1234567UL));

            // Reformatting must not rename anyone, for exactly the reason identity is hashed
            // from the declaring text in the first place.
            BraceInfo[] reformatted = Scan("void Method()\n{\n    if (x)\n    {\n        y();\n    }\n}");
            Check("reformatting does not rename the outer brace",
                BraceNames.Of(first[1].Identity) == BraceNames.Of(reformatted[1].Identity),
                BraceNames.Of(first[1].Identity) + " vs " + BraceNames.Of(reformatted[1].Identity));

            var names = new HashSet<string>();
            for (ulong i = 0; i < 400; i++)
            {
                string name = BraceNames.Of(Hash.Mix(0xABCDEFUL, i));
                Check2(name != null && name.Length > 0, "every brace gets a non-empty name");
                Check2(name.Trim() == name, "names carry no stray whitespace: '" + name + "'");
                names.Add(name);
            }

            // A screenful of identical names would make the whole trait pointless.
            Check("400 braces produce plenty of distinct names", names.Count > 300, "" + names.Count);
            Check("every brace gets a non-empty, tidy name", _deferredOk, _deferredWhy);
        }

        Console.WriteLine();
        Console.WriteLine("== line transform purity ==");
        {
            // Regression test for the bug that broke the editor's layout: RequiredHeadroom
            // once derived from `line.Baseline - line.TextTop`, the geometry of the very line
            // it was sizing. The transform moved TextTop, which changed the input, which
            // changed the transform — layout never converged and documents came out too long
            // with gaps and missing lines.
            var consolas = new System.Windows.Media.Typeface("Consolas");

            double first = IdentityBraces.Adornments.CatgirlLayout.RequiredHeadroom(consolas, 13.33, 1.0, '{', 1.0);
            bool stable = true;
            for (int i = 0; i < 200; i++)
            {
                if (IdentityBraces.Adornments.CatgirlLayout.RequiredHeadroom(consolas, 13.33, 1.0, '{', 1.0) != first)
                {
                    stable = false;
                    break;
                }
            }

            Console.WriteLine("        Consolas 13.33px headroom = " + first + " px");
            Check("headroom is bit-identical across 200 calls", stable);
            Check("headroom is a whole number of pixels", first == Math.Floor(first));
            Check("headroom is positive for Consolas", first > 0);

            // Scaling the font must scale the headroom, not produce something erratic.
            double atDouble = IdentityBraces.Adornments.CatgirlLayout.RequiredHeadroom(consolas, 26.66, 1.0, '{', 1.0);
            Console.WriteLine("        Consolas 26.66px headroom = " + atDouble + " px");
            Check("headroom grows with font size", atDouble > first);
            Check("headroom scales roughly linearly", Math.Abs(atDouble - (first * 2)) <= 2,
                first + " -> " + atDouble);

            // Every bracket must fit under one reserved number, since the transform source
            // caches a single value per font.
            double worst = 0;
            foreach (char c in new[] { '{', '}', '(', ')', '[', ']' })
            {
                double h = IdentityBraces.Adornments.CatgirlLayout.RequiredHeadroom(consolas, 13.33, 1.0, c, 1.0);
                if (h > worst)
                {
                    worst = h;
                }
            }

            Check("all bracket characters fit within the cached maximum", worst >= first, "worst " + worst);

            // A font that cannot be measured must not return something absurd.
            var nonsense = new System.Windows.Media.Typeface("ThisFontDoesNotExist_XYZZY");
            double fallback = IdentityBraces.Adornments.CatgirlLayout.RequiredHeadroom(nonsense, 13.33, 1.0, '{', 1.0);
            Check("missing font yields a sane headroom", fallback >= 0 && fallback < 100, fallback.ToString());
        }

        Console.WriteLine();
        Console.WriteLine("== performance ==");
        {
            var big = string.Join("\n", Enumerable.Range(0, 20000)
                .Select(i => "void M" + i + "()\n{\n    if (x" + i + " > 0) { Do" + i + "(a[" + i + "]); }\n}"));
            Console.WriteLine("        corpus: " + big.Length.ToString("N0") + " chars");

            Scan(big); // warm
            var sw = System.Diagnostics.Stopwatch.StartNew();
            BraceInfo[] r = Scan(big);
            sw.Stop();
            Console.WriteLine("        " + r.Length.ToString("N0") + " braces in " + sw.ElapsedMilliseconds + " ms");
            Check("full rescan of a ~1MB file stays under 100 ms", sw.ElapsedMilliseconds < 100,
                sw.ElapsedMilliseconds + " ms");
        }

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? string.Format("ALL {0} CHECKS PASSED", _pass)
            : string.Format("{0} passed, {1} FAILED", _pass, _fail));
        return _fail == 0 ? 0 : 1;
    }
}
