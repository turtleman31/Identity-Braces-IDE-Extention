# Identity Braces

> **Transparency:** this project was made by [Claude](https://claude.ai), Anthropic's AI model,
> working in Claude Code. The idea is [turtleman31](https://github.com/turtleman31)'s; the
> code, the tests, the VS Code and JetBrains ports and this README were written by Claude.

A Visual Studio extension for **VS 2022** and **VS 2026** — with a [VS Code port](vscode/README.md)
and a [JetBrains port](jetbrains/README.md) (IntelliJ IDEA, Rider, CLion, WebStorm, PyCharm,
GoLand and the rest) that give the same brace the same colour, name and personality in every
editor.

Rainbow-brace extensions make a matching pair share a colour, so code is easier to read.
Identity Braces does the opposite. Every brace gets its own colour out of 32, and a few
of them develop personalities:

| Personality | Default rate | What happens |
|---|---|---|
| **Plain** | ~82% | One of 32 colours, stable forever. |
| **Animated** | 8% | Cycles through the colour wheel and never settles. |
| **Questioning** | 6% | Renders as `?` instead of its real glyph. |
| **Catgirl** | 4% | Cat ears above, thigh highs below. |

And by default a closing brace does not inherit its opener's identity, so `{` and its `}` are
different colours and may be different creatures entirely.

Your code is not more readable. Your braces are people now.

If you would like *some* of it back, there is a whole category of settings for that now — see
[Reading the code again](#reading-the-code-again). Colour by nesting depth, spotlight the block
the caret is in, put coloured guides down the indents. You can keep the creatures and get a
readable file, which was never the plan but turns out to be the best way to use it.

> **The file on disk is never modified.** A brace drawn as `?` is still a `{` in the buffer.
> It still compiles, the caret still lands on it, Find still finds it, and Git sees nothing.
> Everything here is a rendering-layer illusion.

---

## Requirements

| To do this | You need |
|---|---|
| Install and use the extension | VS 2022 (17.x) or VS 2026 (18.x) — nothing else |
| Build the VSIX from the command line | MSBuild from either VS — **no extra workload** |
| Open the project in the VS IDE, or press F5 to debug | The **Visual Studio extension development** workload |

That middle row is worth knowing: the `Microsoft.VSSDK.BuildTools` NuGet package supplies the
whole VSIX build, so a command-line build works on a machine with no VSSDK workload installed.
The workload is only needed for the IDE to *understand the project type* (and to launch the
experimental instance for debugging).

---

## Install

### Option 1 — double-click the VSIX (easiest, handles both VS versions)

Download `IdentityBraces-<version>-VisualStudio.vsix` from the
[latest release](https://github.com/turtleman31/Identity-Braces-IDE-Extention/releases/latest),
or build it yourself (see below) and find it at:

```
src\IdentityBraces\bin\Release\IdentityBraces.vsix
```

Then double-click it. The VSIX Installer opens and lets you tick **which Visual Studio instances** to install into —
2022 and 2026 both appear in the same list. Close all Visual Studio windows first, or the
installer will ask you to.

### Option 2 — command line, targeting a specific instance

Find your instance IDs:

```bash
"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -all -prerelease -format value -property instanceId
```

Then install into one of them (from the repository root, or give the VSIX's full path):

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe" /instanceIds:YOUR_ID "src\IdentityBraces\bin\Release\IdentityBraces.vsix"
```

Run it once per instance you want it in. Use whichever `VSIXInstaller.exe` you like — it can
install into any instance, not just its own.

### Option 3 — F5 debugging (requires the VSSDK workload)

Open `IdentityBraces.sln` and press F5. This launches a separate **experimental instance** of
Visual Studio with the extension loaded, leaving your real IDE untouched. This is the right
way to iterate on it — you can set breakpoints in the tagger and the adornment manager.

To reset the experimental instance if you break it:

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CreateExpInstance.exe" /Reset /VSInstance=18.0 /RootSuffix=Exp
```

### Reinstalling over an existing copy

**Visual Studio keys an installed extension on identity + version.** Installing a VSIX whose
version matches the one already installed is a no-op: the old files stay on disk, the cached
MEF composition keeps serving the old assembly, and you carry on running the previous build
with no error and no warning.

That is a genuinely nasty failure mode — it looks exactly like "my fix didn't work".

The build therefore **stamps a unique version into the manifest on every build**
(`StampVsixVersion` in the csproj: build number is days since 2020-01-01, revision is minutes
since midnight UTC). You should never hit the stale-install problem, but if you suspect it:

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe" /uninstall:IdentityBraces.794AD149-2FA0-453B-94A6-92FB7F780C71
```

Then restart VS, install the new VSIX, and if it still looks stale, clear the MEF cache (see
[Troubleshooting](#troubleshooting)).

To check what is actually loaded, compare the installed assembly against your build:

```bash
find "$LOCALAPPDATA/Microsoft/VisualStudio/18.0_"*/Extensions -name IdentityBraces.dll -exec md5sum {} +
```

### After installing

Open any C-family file. Braces should be colourful immediately. If nothing happens, see
[Troubleshooting](#troubleshooting).

---

## Repository layout

```
IdentityBraces.sln               the Visual Studio extension
src/IdentityBraces/              its source — see Architecture below for what is inside
tests/IdentityBraces.CoreTests/  checks on the editor-independent core; runs on plain .NET
vscode/                          the VS Code port, with its own README, package.json and tests
jetbrains/                       the JetBrains port, with its own README, Gradle build and tests
.github/workflows/ci.yml         builds all three and runs every suite on every push
```

The three extensions share no build step, but they must agree bit for bit on what a brace's
identity is. [vscode/README.md](vscode/README.md#the-same-braces-in-both-editors) explains
how that is asserted rather than hoped for; the JetBrains port checks itself against the
same fixture.

---

## Build from source

```bash
git clone https://github.com/turtleman31/Identity-Braces-IDE-Extention.git
```

```bash
cd Identity-Braces-IDE-Extention
```

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" IdentityBraces.sln -t:Restore
```

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" IdentityBraces.sln -p:Configuration=Release
```

Output lands in `src\IdentityBraces\bin\Release\IdentityBraces.vsix`.

That path is VS 2026 Community. Adjust it to your install: VS 2022 lives under
`...\Microsoft Visual Studio\2022\...`, and `Community` may be `Professional` or `Enterprise`.
Or open a **Developer Command Prompt** for either version, where `msbuild` is already on the
path.

---

## Tests

`Core/` has no Visual Studio dependencies, so the scanner and the identity logic — the parts
most likely to be wrong — can be exercised on a plain .NET runtime with no Visual Studio and
no editor host involved:

```bash
cd tests\IdentityBraces.CoreTests
```

```bash
dotnet run -c Release
```

This needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and nothing
else. 154 checks covering string and comment skipping, the unterminated-quote hazard, pair matching,
independent closer identities, identity stability under insertion / body edits / reformatting /
renaming, colour and personality distribution, line-transform purity, and a performance floor. The last one scans a
1.17 MB corpus of 240,000 braces and asserts it completes in under 100 ms.

The tests link the `Core` sources directly rather than referencing the VSIX project, which is
what lets them run without the VSSDK.

---

## Settings

Two pages. **General** is the scalars below; **Traits** is the catalogue of 86, which needs a
page of its own — see [Picking traits](#picking-traits).

**Tools → Options → Identity Braces → General**

| Setting | Default | Notes |
|---|---|---|
| Enabled | on | Master switch. |
| **Every brace is its own person** | **on** | A closing brace gets its own colour and personality instead of inheriting its opener's. Turn off to make pairs match again. |
| Colour curly braces / parentheses / square brackets | all on | Turn off parens if the density is too much. |
| Animated (%) | 8 | Braces that cycle colour forever. |
| Questioning (%) | 6 | Braces drawn as `?`. |
| Catgirl (%) | 4 | Braces with ears and stockings. |
| Thigh highs | Garter | `Off` / `TwoTone` / `Garter` / `Banded` — how much stripe detail the stockings carry. See below. |
| Tail | on | A tail curling off the bottom right. Overhangs the next cell slightly; it's what makes the glyph read as a creature. |
| Reserve space for cat ears | **off** | Makes lines with a catgirl brace physically taller so the ears have room. The only setting that changes document height — see below. With it off the ears overhang the line above. |
| Unmatched braces question themselves | on | A brace with no partner always renders `?`. Doubles as a syntax-error hint. |
| **Enable motion** | on | **Turn this off** on battery, on a projector, or if movement in a text editor is unpleasant. Animated braces go static, the `?` stops wobbling. |
| Animation frame rate | 18 | fps for colour cycling. The editor does not need 60. |
| Colour cycle seconds | 6 | Time to travel the whole wheel. |
| Maximum file length | 1,000,000 | Files longer than this are left alone entirely. |
| Scene interval (seconds) | 8 | How often a view *tries* to play a multi-brace scene. Most attempts find nothing eligible and do nothing. |
| **Colour mode** | Palette | `Palette` (32 identity colours) / `Monochrome` (one theme-following colour) / `Depth` (one colour per nesting level). See below. |
| **Scope spotlight** | off | The pair enclosing the caret stays vivid; every other brace fades. |
| Spotlight dim (%) | 30 | How visible an out-of-scope brace stays. |
| **Complexity warning depth** | 0 (off) | Braces nested at least this deep are drawn sweating and unsteady. |
| **Coloured indent guides** | off | A vertical guide down the inside of each multi-line pair, in that pair's colour. |
| Indent guide strength (%) | 45 | How strong the guides are against the background. |

The three personality percentages partition a single roll, so they are scaled down if you set
them to more than 100 together.

Settings live in `%APPDATA%\IdentityBraces\settings.ini` — a plain key/value file you can edit
by hand. This is deliberate: the editor components read it directly, with no dependency on a
loaded VS package or the UI thread.

**Changing any personality percentage reshuffles which braces have which personality**, because
the roll is a function of the thresholds. Colours are unaffected.

### Thigh highs

The stockings are **clipped copies of the brace's own stroke**, not a shape drawn behind it.
Nothing is ever painted under the glyph, so a stocking is exactly as wide as the stroke it
clothes, at every font size, automatically. The first version drew a rounded rectangle instead
— 8.2px wide against a 6px stroke — which read as a bar with a brace lost inside it.

The four styles differ only in a table of horizontal bands, which is why all of them ship:

| Style | Welt | Reads at 10pt |
|---|---|---|
| `Off` | — | Ears only, no stockings |
| `TwoTone` | none | Clearest — nothing on it is under 5px |
| `Garter` *(default)* | one stripe, 2.0px | The legibility floor; the band at the top is what makes a two-tone leg read as clothing |
| `Banded` | two stripes, 1.3px | Correct at larger font sizes; at 10pt the stripes start averaging back into one band |

Two colours are fixed rather than drawn from the identity palette: the stocking `#7B7490` and
its welt `#C6BFD4`. Both sit at the palette's balanced luminance so they hold up on light and
dark themes, and the welt is chosen to contrast the *stocking*, not the background.

### Reserving space for ears (and why it's off)

`ILineTransformSource` is the only thing in this extension that can change the document's
height, which makes it the only thing that can break the editor's layout. It did, once, badly:
scrolling threw lines around, pages came out longer than their content, and lines went missing
behind stretches of white space.

The cause was that the headroom was derived from `line.Baseline - line.TextTop` — the geometry
of the very line it was sizing. That is a feedback loop. The transform moves `TextTop`, which
changes the input, which changes the transform; the editor re-asks during layout, gets a
different answer every time, and the height never converges.

It is now a pure function of the *font*, computed once per view and cached, and the math lives
in `CatgirlLayout.RequiredHeadroom`, whose signature accepts a typeface and nothing else — there
is no longer a way to express the mistake. A test asserts it returns a bit-identical value
across 200 calls.

It still defaults to **off**, because the fallback costs almost nothing: the ears simply
overhang the line above on a few lines per screen. Turn it on once you're satisfied the rest is
behaving.

**If the editor ever misbehaves again, this is the first switch to try.**

### Picking traits

**Tools → Options → Identity Braces → Traits.**

Eighty-six traits do not fit in a property grid, and until this page existed they were only
reachable by hand-editing `settings.ini` — where you also had to already know what `cthulhu`
looked like to decide whether you wanted it.

The page has a search box, the five presets, and one slider per trait grouped by layer. Two
things about it are worth knowing:

**The layer totals are live.** Body, creature, costume and motion share a single 0–100 roll, so
a layer summing past 100 gets scaled down when you press OK and the traits at the bottom of
that layer become unreachable. The header for each layer shows `n / 100` and turns orange when
you go over, which used to be a silent failure.

**Both previews are the real renderer.** They call `BraceVisualFactory` — the same code the
editor draws with — rather than illustrating it, so they cannot drift from what you will
actually get:

| Preview | Shows |
|---|---|
| **Selected trait**, magnified | What one trait *is*, on an opener and a closer, with its animation running. Click any row, or arrow through them with the keyboard. |
| **At these weights**, actual size | 120 braces rolled through the real trait table at your current weights, in your editor's font, on your editor's background. This is the density you will get. |

The strip holds its sample identities fixed, so moving one slider changes only the braces that
slider governs instead of reshuffling everything — and it is deliberately held still, because a
hundred and twenty simultaneous animations in a modal dialog is not a preview, it is a
stress test. Motion is previewed in the magnified panel beside it.

Everything on the page edits a copy. Cancel really does cancel.

### Scenes

Most traits are drawn into a canvas exactly one character wide, and know nothing about their
neighbours or the text beside them. That is the right shape for eighty-odd traits and the wrong
shape for a few. **Table flip** is the first of those: `(╯°□°)╯︵ ┻━┻`, a brace throwing a table
into the empty space beside it — which it cannot do without first checking there *is* empty
space, or the table lands in the middle of an identifier.

Three scenes ship. All need **Enable motion** on, and their trait turned up on the Traits page.

| Scene | What happens |
|---|---|
| `tableflip` | `(╯°□°)╯︵ ┻━┻` — a brace throws a table into the empty space beside it. |
| `swapplaces` | Two braces on the same line hop over the code between them, visit each other's column, and come back. |
| `firebrigade` | A brace drives a fire truck to a burning brace, hoses it down, and goes home. The fire does not go out. |

Three things worth knowing:

- **Nothing happens on a crowded line.** A brace needs three clear columns to throw into. It
  prefers the right, since a brace usually has the rest of its line free that way and code to
  the left. A tick that finds no eligible brace with room simply does nothing and tries again.
- **A scene never survives a layout.** Scroll, type, or resize during one and the table
  vanishes mid-flight. That is deliberate — props are placed from line geometry that is only
  valid until the next pass, and chasing them across layouts is the exact problem that cost
  this project ten rounds of debugging on the braces themselves. A prop that disappears costs
  nothing; there will be another one along shortly.
- **The braces themselves do not move.** Their glyphs belong to the adornment manager, which
  rewrites those coordinates every layout pass. A second owner reaching in to transform them is
  shared ownership, and this codebase has paid for that once already. So `swapplaces` sends
  *likenesses* — each brace's own glyph, copied — while the originals hold their ground, and
  the fire brigade cannot actually extinguish anything. Given what this extension is for, a
  brigade that always loses is the better ending.

One scene at a time per view, on a slow timer. They are meant to be caught out of the corner of
an eye rather than watched.

### Traits that react to something

Five traits do not just sit there. They are all off by default.

| Trait | Reacts to |
|---|---|
| `fleecursor` | The caret. Edges away along its own line as you approach, and settles back once you pass. |
| `stagefright` | The caret. Fades almost out while the caret is on its line — *almost*, because a brace that is genuinely invisible is the one failure this extension treats as unacceptable. |
| `named` | The mouse. Hover a named brace and the editor's own tooltip introduces it: *Sir Reginald the Unclosed*. The name is derived from the same identity hash as its colour, so it survives reformatting and everyone sees the same name. |
| `buildreactive` | Builds. Green sparks for a success, a grey droop for a failure, for 25 seconds afterwards and then never again. |
| `mitosis` | Time. Once every half minute or so a second brace buds off, drifts away, and dissolves. |

`fleecursor` and `stagefright` are applied when a brace is *placed*, not when it is drawn, which
is why they can follow the caret without anything being redrawn. `named` goes through the
editor's quick-info rather than a tooltip on the adornment — brace adornments sit above the
text, so making them hit-testable would have them swallow the clicks that place your caret.

`buildreactive` is the only thing here that talks to Visual Studio itself. It advises build
events lazily, the first time a view is created with the trait switched on, so the extension
still adds nothing to start-up.

### Reading the code again

Four settings, under **Structure**, that pull in the opposite direction from everything else
here. They compose: depth colour with a spotlight and guides is a genuinely pleasant way to
read a deeply nested file, and the creatures carry on regardless.

**Colour mode → Depth.** Colours by nesting level instead of by identity, so every brace at the
same depth shares a colour and a pair agrees with itself — which is what a rainbow-brace
extension does. It reuses the same 32 entries, indexed by depth rather than by hash, and wraps
past 32 levels. Because the palette's hues are spaced by the golden angle, consecutive depths
land far apart on the wheel rather than shading into one another.

The choice is resolved in the scanner, not at the two places that draw. That matters: a
personality brace and the plain brace beside it must never disagree about what colour that
depth is.

**Scope spotlight.** The innermost pair containing the caret stays at full strength; every other
brace on screen drops to 30%. With 32 colours competing this is the one thing that still tells
you which block you are in.

Plain braces fade through a second classification that carries opacity and nothing else,
layered over the palette one — so the brace keeps its own colour and simply recedes, and it
costs one extra classification type instead of a parallel dim palette of 32. Drawn braces fade
through `OpacityMask` rather than `Opacity`, because `Opacity` is already spoken for: the
`nocturnal` effect sets it and `blink`, `flicker` and `typewriter` animate it, and in WPF an
animation outranks a local value — a blinking brace would have refused to dim.

**Complexity warning.** Set a depth and anything nested at least that far is drawn sweating,
with a fast nervous shake. It is the only trait in the catalogue applied by a predicate rather
than by a dice roll, so it ignores the weight table entirely: a warning that fires on four
braces in a hundred is not a warning. `distressed` is still a normal trait as well, so you can
also just turn it on for everyone.

**Coloured indent guides.** A dotted vertical line down the inside of every pair that spans more
than one line, in that pair's colour, at the indent of the line its opener sits on. Combined
with depth colour this is an ordinary rainbow indent; combined with identity colour it is a
guide per block that matches the braces it joins.

Guides are assembled from visible lines only — each visible line contributes its own segment,
because a pair can easily span more screens than the monitor has and an off-screen line has no
geometry to ask. Which pairs are open at the top of the screen comes from a walk *up* the
nesting rather than back through the file, so scrolling to the end of a large document costs
the same as scrolling to the start.

### Retuning the colours

The 32 palette entries are registered as normal VS classifications, so they are editable in
**Tools → Options → Environment → Fonts and Colors** under `Identity Brace 00` … `31`.

---

## How identity works

The interesting problem here is not drawing cat ears. It is deciding what a brace's identity
*is*, such that it holds still.

Get this wrong and every keystroke reshuffles every colour, and the extension is unusable
within about four seconds.

- **Hashing the position** is the obvious choice and the wrong one — insert one line at the top
  of the file and every brace below it is reincarnated.
- **Hashing the enclosed body** is also wrong — typing inside a block changes that block's
  identity while you are looking at it.
- **Hashing the declaration** works. Identity is the hash of the brace's own line up to the
  brace, whitespace removed, falling back to the nearest preceding non-blank line so that
  Allman style resolves to the signature above it.

```csharp
public async Task<Order> GetOrderAsync(int id)   // <- this text is the identity
{                                                 // <- so this brace belongs to it
    ...
}                                                 // <- and so does its partner
```

That hash is stable across edits elsewhere in the file, stable across sessions, stable across
machines, and survives a reformat. It changes when you rename the method — which is arguably
correct: it is a different method now.

### Every brace is its own person

A matched pair *could* share one identity, so that `{` and its `}` always agree. That was the
only concession this extension made to readability, so it is **off by default**.

With `Every brace is its own person` on (the default), a closing brace gets its own colour and
its own personality roll. An opening brace may be a plain green `{` while the `}` that closes it
is a magenta catgirl. Nothing about a pair tells you it is a pair.

The closer does **not** hash its own declaring line, though. A `}` usually sits alone on one, so
there is almost nothing to hash — every closer in the file would collide on the same near-empty
header, and a stack of them would all come out identical. Instead the closer's identity is a
salted re-mix of its opener's:

```
closerIdentity = mix(openerIdentity, CLOSER_SALT)
```

That inherits every stability property of the opener — survives edits elsewhere, survives a
reformat, survives reopening the file — while guaranteeing the two halves never land on the
same value. Measured over 4,000 blocks, a pair ends up agreeing on colour 3.28% of the time
against the 3.1% you would expect from chance with 32 buckets.

Turn it off in Tools → Options if you ever need to actually read something.

Consequences worth knowing:

- Two blocks whose declaring text is *identical* at the same nesting depth share an identity,
  and therefore a colour and a personality. Two `if (x)` blocks at the same depth will match.
  This is intentional — same code, same soul — but it is the one place the illusion of
  uniqueness breaks.
- The personality roll is derived from the same hash with a different salt, so a given brace
  is reliably the same character every time you open the file.

---

## Architecture

Two mechanisms, because the feature list straddles both:

**Classification** (`BraceTagger`) handles the plain 90%. It emits `ClassificationTag`s into
one of 32 pre-registered classification types. Cheap, robust, respects Fonts and Colors. The
count is fixed at 32 because MEF composes format definitions once at start-up and attributes
take no runtime values.

**WPF adornments** (`AdornmentManager`) handle the personalities. The classifier paints those
braces `Transparent`, and the adornment layer redraws them — an animated glyph, a `?`, or a
full catgirl assembly. Personality braces are rare by default, so the adornment count stays
small and performance stays fine.

Both derive their answers from the same `BraceMapCache`, so they cannot disagree: given a
snapshot and settings, `BraceScanner` is a pure function.

A few decisions that carry weight:

- **A brace is never invisible.** `AdornedBuffers` tracks which buffers actually have a live
  adornment manager. If a view has no adornment layer — a diff view, a peek window, a view
  created before its manager — the tagger falls back to a plain coloured tag instead of a
  transparent one, so the character never disappears.
- **Adornments are added `TextRelative`**, and only for `NewOrReformattedLines`. The layer
  translates them itself as you scroll, so untouched lines keep their elements and their
  animations keep running instead of restarting every scroll tick.
- **The correction pass drives off `IAdornmentLayer.Elements`, not the manager's own
  bookkeeping.** Every layout, each element the layer is actually rendering is re-placed from
  live line geometry, and any element whose brace can no longer be located is removed from the
  layer outright. This is the single most important invariant in the adornment code, and it was
  learned twice: positions were originally set once at creation and trusted forever, and the
  first fix walked an internal collection — so an element that reached the layer but fell out
  of that collection was invisible to the correction pass and stayed stranded at the top of the
  file permanently. The layer's own element list is the authoritative answer to "what is on
  screen", so nothing can hide from it. Each element is guarded individually, because wrapping
  the loop in one `try` let a single bad adornment abort the pass for every element after it.
  Repositioning creates nothing, so animations survive it.
- **Animation drives `SolidColorBrush.Color`**, not element properties. A brush colour change
  is a render-thread operation, so it never triggers a measure or arrange pass in the editor.
  Frame rate is capped, and clocks are paused when the document tab is not visible.
- **Decorations anchor to glyph ink, not the em box.** For Consolas at 13.33px the cell is
  7.33 × 15 but a brace's ink is only 6 × 12, sitting 3.3px below the top of the em box and
  0.7px left of the cell centre (it carries right side bearing). `GlyphMetrics` measures this
  from `FormattedText` and caches it per font; everything drawn on a brace is positioned as a
  fraction of ink height, so the proportions hold at any size or zoom.
- **Cat ears get real headroom** via `ILineTransformSource`, the supported way to make specific
  lines physically taller — the same mechanism inline diffs and image previews use. Without it
  the ears would collide with the descenders of the line above. Both the transform source and
  the visual factory call `BraceVisualFactory.RequiredHeadroom`, so the space reserved and the
  space used are the same number by construction, not by two constants kept in sync by hand.
- **`BraceLineTransformSource` never throws.** It runs inside the layout pass, where an
  exception takes down the entire text view for the rest of the session. Every failure path
  returns the line's default transform.

```
src/IdentityBraces/
  Core/            BraceScanner, identity hashing, the brace map    (no VS dependencies)
  Classification/  32 classification types + formats, the tagger
  Adornments/      the layer, visual factory, manager, line transform
  Options/         settings store + the Tools > Options page
```

`Core/` is deliberately free of Visual Studio types, which is what makes the scanner testable
in isolation.

---

## Language support

The scanner is a small self-contained C-family lexer rather than a query against each language
service, so C#, C++, JavaScript, TypeScript, JSON, Java and friends all work out of the box.
It understands `//`, `/* */`, `"…"`, `'…'`, `` `…` ``, `@"…"` and `"""…"""`.

Known approximations, all of which fail in the safe direction — skipping a brace we could have
coloured rather than colouring one inside a string:

- Interpolation holes in `$"{expr}"` and `` `${expr}` `` are treated as opaque string content,
  so braces inside them are ignored.
- A single-quoted run that does not close on the same line is treated as *not* a string. This
  is what stops a Rust lifetime (`&'a str`) or a stray apostrophe from swallowing the rest of
  the file.

---

## Limitations

- **Whole-file rescan per keystroke.** Every edit produces a new snapshot and a full linear
  rescan. Measured at ~18 ms for a 1.17 MB file with 240,000 braces, which is fine, but it is
  linear — hence the `Maximum file length` guard. Incremental rescanning is the obvious future
  improvement.
- **VS's own brace-match highlighting still fires** on the pair under the caret and will fight
  your colours there.
- **32 colours, not infinite.** See Architecture. Beyond ~30 hues they stop being
  distinguishable in a punctuation glyph anyway.
- **Multiple VS instances share one settings file** and only read it at start-up. Change
  settings in one, restart the others.
- **High contrast themes** are not special-cased. Turn the extension off, or retune the
  palette in Fonts and Colors.

---

## Troubleshooting

**Nothing is coloured.** Confirm the extension is listed and enabled under
**Extensions → Manage Extensions → Installed**. MEF components are cached, and a stale cache
is the usual culprit — delete the `ComponentModelCache` folder and restart:

```bash
rmdir /s /q "%LOCALAPPDATA%\Microsoft\VisualStudio\18.0_ad2abeeb\ComponentModelCache"
```

Substitute your own instance folder from `%LOCALAPPDATA%\Microsoft\VisualStudio\`.

**Braces are invisible rather than colourful.** This should be impossible by design — report it
with the view type (diff? peek? embedded?). As a workaround set the three personality
percentages to 0, which stops the extension from ever painting a brace transparent.

**Typing feels sluggish in one huge file.** Lower `Maximum file length` below that file's size.

**The motion is too much.** Turn off `Enable motion`. Colours stay, movement stops.

---

## Uninstall

**Extensions → Manage Extensions → Installed → Identity Braces → Uninstall**, then restart.
Or:

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe" /uninstall:IdentityBraces.794AD149-2FA0-453B-94A6-92FB7F780C71
```

Settings are left behind at `%APPDATA%\IdentityBraces\settings.ini`; delete it if you want a
clean slate.

---

## License

[MIT](LICENSE). Both extensions, same terms.
