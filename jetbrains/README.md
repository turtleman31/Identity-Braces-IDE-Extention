# Identity Braces for JetBrains IDEs

> **Transparency:** made by [Claude](https://claude.ai), Anthropic's AI model, from
> [turtleman31](https://github.com/turtleman31)'s idea. The full note is at the top of the
> [main README](https://github.com/turtleman31/Identity-Braces-IDE-Extention#readme).

A port of **Identity Braces**, the Visual Studio extension, to the IntelliJ Platform — one
plugin for **IntelliJ IDEA, Rider, CLion, WebStorm, PyCharm, GoLand, PhpStorm, RubyMine** and
the rest, because it depends on nothing but the platform editor. The fuller account of what
it is and why — the identity hashing, the trait catalogue — is in the Visual Studio
extension's own README, one directory up at `../README.md`.

Rainbow-brace plugins make a matching pair share a colour, so code is easier to read.
Identity Braces does the opposite. Every brace gets its own colour out of 32, and a few of
them develop personalities.

| Personality | Default rate | What happens |
|---|---|---|
| **Plain** | ~82% | One of 32 colours, stable forever. |
| **Animated** | 8% | Cycles through the colour wheel and never settles. |
| **Questioning** | 6% | Renders as `?` instead of its real glyph. |
| **Catgirl** | 4% | Cat ears above, thigh highs below. |

And by default a closing brace does not inherit its opener's identity, so `{` and its `}` are
different colours and may be different creatures entirely.

> **The file on disk is never modified.** A brace drawn as `?` is still a `{` in the document.
> It still compiles, the caret still lands on it, Find still finds it, and Git sees nothing.
> Everything here is a rendering-layer illusion — a highlighter and a painter, not an edit.

---

## The same braces, in every editor

The point of a port is not that it looks similar. It is that a brace is *the same brace*.

A brace's colour, name and personality are a pure function of a 64-bit hash of its declaring
line. Open the same file in Rider, in Visual Studio and in VS Code and the same `{` has to
come out the same colour, wearing the same hat, answering to the same name.

That is only true if the three implementations agree bit for bit on 64-bit FNV-1a, on the
SplitMix64 finaliser, on every one of the salts, on the order the trait table is walked in,
and on the lexer's idea of where a string ends. So it is asserted rather than hoped for:
`src/test/kotlin/identitybraces/core/ParityTest.kt` runs the Kotlin core over
`../vscode/test/fixtures/sample.txt` in five configurations and compares every field of every
brace — identity, colour index, depth, partner, parent, all five trait layers and the
generated name — against `csharp-core.json`, which the C# core produced. One fixture, one
generator, three consumers. 400 braces, zero mismatches.

Unlike the TypeScript port this needed no 64-bit arithmetic on 32-bit halves: the JVM has
`ULong`, so `Hash.kt` is a straight transliteration of the C#, and a 1.17 MB file scans in
about the same time the C# takes.

---

## Install

Download `identity-braces-<version>.zip` from the
[latest release](https://github.com/turtleman31/Identity-Braces-IDE-Extention/releases/latest),
then in any JetBrains IDE: **Settings → Plugins → ⚙ → Install Plugin from Disk…** and pick the
zip. Restart when asked. It needs a 2024.1 or newer IDE.

To build it yourself, from the repository root:

```bash
cd jetbrains
```

```bash
./gradlew buildPlugin
```

The zip lands in `build/distributions/`. The first build downloads the IntelliJ Platform SDK
(about a gigabyte) into the Gradle cache, once. Gradle needs to run on JDK 17 or newer — set
`JAVA_HOME` if the default `java` on your path is older.

To try it without touching your own IDE, `./gradlew runIde` opens a sandboxed IntelliJ IDEA
Community with the plugin loaded (`-PopenPath=path/to/a/file.cs` opens a file straight
away), and `./gradlew runRider` does the same in Rider, which is a large separate download
the first time.

Open any C-family file. Braces should be colourful immediately.

---

## Tests

```bash
cd jetbrains
```

```bash
./gradlew test
```

Forty-seven checks in two kinds:

- **The core**, on a bare JVM: the FNV vectors, the unsigned arithmetic that a signed `Long`
  would get quietly wrong, the lexer's hazards (strings, comments, verbatim and raw strings,
  template literals, the unterminated-quote and Rust-lifetime traps), pair matching, identity
  stability under insertion, body edits, reformatting and renaming, the independent-closer
  rules, colour distribution, a performance floor, and the C# parity fixture.
- **The editor**, headless but real: the platform test framework opens an actual `EditorImpl`
  and the tests check that a highlighter lands on every brace and nowhere else, that they
  follow the text as it is edited, that a personality brace is painted transparent and then
  actually drawn by the overlay, that a table flip is cast beside a brace with room and
  painted mid-flight, and that switching the plugin off leaves the editor as it found it. The
  two settings pages are built and painted to `build/reports/settings/` for looking at.

---

## Settings

**Settings → Editor → Identity Braces** is the scalars; **→ Traits** is the catalogue of 86.

| Setting | Default | Notes |
|---|---|---|
| Enabled | on | Master switch. Also **Tools → Identity Braces → Enabled**. |
| **Every brace is its own person** | **on** | A closing brace gets its own colour and personality instead of inheriting its opener's. Turn off to make pairs match again. |
| Colour curly braces / parentheses / square brackets | all on | Turn off parens if the density is too much. |
| Unmatched braces question themselves | on | A brace with no partner always renders `?`. Doubles as a syntax-error hint. |
| Maximum file length | 1,000,000 | Files longer than this are left alone entirely. |
| **Enable motion** | on | **Turn this off** on battery, on a projector, or if movement in a text editor is unpleasant. Also **Tools → Identity Braces → Motion**. |
| Animation frame rate | 18 | The editor does not need 60. |
| Colour cycle seconds | 6 | Time to travel the whole wheel. |
| Scene interval (seconds) | 8 | How often an editor *tries* to play a multi-brace scene. Most attempts find nothing eligible and do nothing. |
| Thigh highs | Garter | `Off` / `TwoTone` / `Garter` / `Banded` — how much stripe detail the stockings carry. |
| Tail | on | A tail curling off the bottom right. Overhangs the next cell slightly; it's what makes the glyph read as a creature. |
| Decoration size (%) | 100 | Multiplier for everything drawn on a brace, independent of the font. |
| Render mode | Full | `Full` / `Text` (no drawn overlays) / `Plain` (colours only). A troubleshooting ladder. |
| **Colour mode** | Palette | `Palette` (32 identity colours) / `Monochrome` (the editor's text colour) / `Depth` (one colour per nesting level). |
| **Scope spotlight** | off | The pair enclosing the caret stays vivid; every other brace fades. |
| Spotlight dim (%) | 30 | How visible an out-of-scope brace stays. |
| **Complexity warning depth** | 0 (off) | Braces nested at least this deep are drawn sweating and unsteady. |
| **Coloured indent guides** | off | A dotted vertical guide down the inside of each multi-line pair, in that pair's colour, behind the text. |
| Indent guide strength (%) | 45 | How strong the guides are against the background. |

Changing any personality percentage reshuffles which braces have which personality, because
the roll is a function of the thresholds. Colours are unaffected.

The 32 palette entries are colour-scheme attributes, editable under **Settings → Editor →
Color Scheme → Identity Braces** — the equivalent of Visual Studio's Fonts and Colors. Retune
one there and every open editor picks it up.

### The Traits page

Eighty-six sliders grouped by layer, each next to the thing it turns on, with a search box and
the five presets. Two things about it are worth knowing, both inherited from the Visual Studio
options page. **The layer totals are live**: body, creature, costume and motion share a single
0–100 roll, so a layer summing past 100 gets scaled down on Apply and the traits at the
bottom of it become unreachable; the header shows `n / 100` and turns orange when you go
over. And **both previews are the real renderer** — they call `BraceRenderer`, the same code
the editor draws with, so they cannot drift from what you will actually get. The strip of 120
is deliberately held still; motion is previewed in the magnified panel beside it.

### Bringing your settings across

**Tools → Identity Braces → Import Settings from Visual Studio** reads
`%APPDATA%\IdentityBraces\settings.ini` — the plain key/value file the Visual Studio
extension keeps its settings in — and applies the equivalents here. It maps every setting,
including the ones that already match the default, and writes every trait including the
zeroes, for the same reasons the VS Code importer does. Then it tells you what did not come
across.

### Menu

**Tools → Identity Braces**:

| Action | What it does |
|---|---|
| Enabled | On or off. |
| Motion | Colours stay, movement stops. |
| Apply a Trait Preset… | Off / Default / Menagerie / Restless / Unusable. |
| Traits… | The eighty-six sliders. |
| Identify the Brace at the Caret | Name, identity hash, colour, depth and every trait it rolled. |
| Import Settings from Visual Studio | The above. |

---

## How this differs from the Visual Studio extension

Of the three editors this is the closest to the original, because Java2D is a canvas. The
port is a rebuild on top of the one thing that carried over unchanged from both — the
geometry — but nearly everything the WPF version could do, this can too.

**Visual Studio gives you a WPF canvas per brace and an animation clock.** This gives you a
`Graphics2D` and a repaint. So:

- **Everything is immediate-mode.** Nothing remembers where a brace was: on every repaint the
  drawn braces on screen are placed from live geometry and painted at the current time. The
  Visual Studio extension learned twice that a position trusted from creation time is a
  position that is wrong by the next layout; here there is no such position to trust.
- **Three highlighters do the work.** One per visible plain brace, carrying only a foreground
  colour — the equivalent of the classifier's tags, and only ever as many as fit on screen
  plus a margin. One over the whole document with a renderer painted *after the text*: the
  adornment layer, where personality braces are drawn (their real glyph is painted
  transparent by its plain highlighter, so nothing is ever drawn twice). And one painted
  *after the background*, for the indent guides, which belong behind the code.
- **The stockings are the same trick as the original.** In WPF they are clipped copies of the
  brace's own stroke; here they are the glyph's outline used as a clip, so a stocking is
  exactly as wide as the stroke it clothes at any font size, automatically.
- **The ink is measured, not assumed.** A glyph vector's visual bounds are the real outline,
  so ears sit on the top of the ink and tails start inside it, in any font. The VS Code port
  has no font metrics and assumes Consolas' proportions; this does not need its
  `overlayNudge`.
- **Animation is a Swing timer** at the configured frame rate, repainting only the rectangles
  of the braces that are actually moving. The clock stops when nothing on screen moves and
  costs one check per frame while the tab is hidden.
- **The scenes are back.** `tableflip`, `swapplaces` and `firebrigade` are absent from VS
  Code because an extension cannot ask where a character is on screen. Here it can, so a
  brace flips a table into the empty space beside it, two braces on a line hop over the code
  between them, and a fire truck drives to a burning brace, sprays it and goes home with the
  brace still alight. As in Visual Studio, a scene never survives a layout: typing, scrolling
  or folding strikes the set, and there will be another one along shortly.
- **`typewriter` works.** The renderer can remember when it first drew a brace, which a
  stateless decoration could not.
- **Colours come from the colour scheme.** Thirty-two `TextAttributesKey`s with the palette
  as their defaults, so the plain highlighters follow a retuned scheme without a repaint of
  ours.

### What did not carry over

| Feature | Why not |
|---|---|
| **Reserve space for cat ears** | The IntelliJ editor has no per-line height. The nearest thing is a block inlay above the line, which adds a gap rather than stretching it. The Visual Studio extension has this off by default anyway; the ears overhang the line above, as they do there. |
| **Brace scale** | Use the editor's own font size. |

`buildreactive` listens to both the project task system (Build in IntelliJ, CLion and Rider)
and run configurations finishing — a failing test run is as good an excuse to sulk as a
failing compile.

`named` goes through an editor hint rather than a tooltip on the drawn overlay, for the same
reason it goes through quick info in Visual Studio: the overlay sits above the text, and
making it hit-testable would have it swallow the clicks that place your caret.

---

## Limitations

- **Whole-file rescan per edit**, debounced by 30 ms, at roughly the C# version's speed.
  `Maximum file length` is the backstop.
- **The IDE's own matched-brace highlighting** still fires on the pair under the caret and
  will fight your colours there. Turn it off under **Settings → Editor → General → Code
  Editing** if it bothers you.
- **32 colours, not infinite.** Beyond about thirty hues they stop being distinguishable in a
  punctuation glyph anyway.
- **Only main editors and diff viewers** are coloured. Consoles, the commit message field and
  the like are left alone.
- **High contrast themes** are not special-cased. Set the colour mode to `Monochrome`, or
  retune the palette in the colour scheme.
- **The scanner is a C-family lexer**, not the IDE's parser. It understands `//`, `/* */`,
  `"…"`, `'…'`, `` `…` ``, `@"…"` and `"""…"""`. Interpolation holes are treated as opaque
  string content, and a single-quoted run that does not close on its line is treated as *not*
  a string — which is what stops a Rust lifetime from swallowing the rest of the file. Both
  approximations fail in the safe direction: skipping a brace we could have coloured rather
  than colouring one inside a string.

---

## Troubleshooting

**Nothing is coloured.** Check **Tools → Identity Braces → Enabled**, and that the file is under
`Maximum file length`.

**The drawn overlays misbehave.** Drop **Render mode** to `Text` — colours and glyph
substitutions, no overlays — or to `Plain`, and please report the font and IDE.

**Typing feels sluggish in one huge file.** Lower `Maximum file length` below that file's size.

**The motion is too much.** **Tools → Identity Braces → Motion**. Colours stay, movement stops.

---

## Layout

```
jetbrains/
  src/main/kotlin/identitybraces/
    core/       the scanner, the identity hashing, the trait roll   (no editor dependencies,
                and bit-identical to the Visual Studio extension's Core/)
    render/     the Java2D ink canvas, the trait painters, the brace style and renderer
    editor/     the per-editor controller, highlighters, guides, hover, scenes
    settings/   the settings store, the two pages, the colour keys, the importer
    actions/    the Tools menu
  src/test/kotlin/         the suite, including the C# parity test and the headless editor tests
  build.gradle.kts         IntelliJ Platform Gradle Plugin 2.x; Kotlin; since-build 241
```

`core/` is a straight port. `render/` is not — but the trait geometry inside it is, because
every shape is authored in ink units and knows nothing about the editor drawing it. That is
what made eighty-two creatures a copy-and-paste rather than a rewrite, for the second time.
