# Identity Braces for VS Code

> **Transparency:** made by [Claude](https://claude.ai), Anthropic's AI model, from
> [turtleman31](https://github.com/turtleman31)'s idea. The full note is at the top of the
> [main README](https://github.com/turtleman31/Identity-Braces-IDE-Extention#readme).

A port of **Identity Braces**, the Visual Studio extension, to VS Code. The fuller account of
what it is and why — the identity hashing, the trait catalogue, the WPF adornment layer — is
in that extension's own README, one directory up at `../README.md`.

Rainbow-brace extensions make a matching pair share a colour, so code is easier to read.
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

> **The file on disk is never modified.** A brace drawn as `?` is still a `{` in the buffer.
> It still compiles, the caret still lands on it, Find still finds it, and Git sees nothing.
> Everything here is a rendering-layer illusion — a decoration, not an edit.

---

## The same braces, in both editors

The point of a port is not that it looks similar. It is that a brace is *the same brace*.

A brace's colour, name and personality are a pure function of a 64-bit hash of its declaring
line. Open the same file in Visual Studio and in VS Code and the same `{` has to come out the
same colour, wearing the same hat, answering to the same name — otherwise "every brace is a
person" is just a lighting effect.

That is only true if the two implementations agree bit for bit on 64-bit FNV-1a, on the
SplitMix64 finaliser, on every one of the salts, on the order the trait table is walked in,
and on the lexer's idea of where a string ends. So it is asserted rather than hoped for:

- `test/fixtures/csharp-core.json` is what the C# `IdentityBraces.Core` makes of
  `test/fixtures/sample.txt`, produced by `tools/parity`, which **links the C# sources
  directly** rather than copying them.
- `src/test/parity.test.ts` runs the TypeScript core over the same file in five
  configurations and asserts every field of every brace — identity, colour index, depth,
  partner, parent, all five trait layers, and the generated name.

400 braces across five configurations, zero mismatches. If that test ever fails, the two
extensions have diverged and everyone's braces have been reincarnated.

Regenerate the fixture after any change to either core:

```bash
cd vscode/tools/parity && dotnet run -c Release
```

---

## Install

The quick way: download `IdentityBraces-<version>-VSCode.vsix` from the
[latest release](https://github.com/turtleman31/Identity-Braces-IDE-Extention/releases/latest)
and run `code --install-extension <that file>`.

To build it yourself, from the repository root:

```bash
cd vscode && npm install && npm run compile
```

Then either press <kbd>F5</kbd> in VS Code with this folder open — which launches an
Extension Development Host with it loaded, leaving your real editor untouched — or build a
VSIX and install it:

```bash
npx @vscode/vsce package
```

```bash
code --install-extension identity-braces-1.2.3.vsix
```

Open any C-family file. Braces should be colourful immediately.

To try it without touching your own profile at all:

```bash
code --extensions-dir /tmp/ib-ext --user-data-dir /tmp/ib-user --new-window .
```

---

## Tests

```bash
cd vscode && npm test
```

Forty checks: the 64-bit arithmetic, the lexer's hazards (strings, comments, verbatim
and raw strings, template literals, the unterminated-quote and Rust-lifetime traps), pair
matching, identity stability under insertion, body edits, reformatting and renaming, the
independent-closer rules, colour distribution, and the C# parity fixture.

Plus a performance floor: a 1.17 MB corpus with 200,000 braces scans in about **150 ms**.
The C# does the same corpus in 18 ms. The gap is the price of doing 64-bit arithmetic on
pairs of 32-bit halves, because JavaScript has no 64-bit integers and `BigInt` is an order of
magnitude slower again. It is why the rescan is debounced, and why the debounce gets longer
on a large file.

---

## Settings

All under `identityBraces.` in Settings, or in `settings.json`.

| Setting | Default | Notes |
|---|---|---|
| `enabled` | on | Master switch. Also `Identity Braces: Toggle`. |
| `independentBraces` | **on** | A closing brace gets its own colour and personality instead of inheriting its opener's. Turn off to make pairs match again. |
| `curly` / `round` / `square` | all on | Turn off parens if the density is too much. |
| `questionUnmatched` | on | A brace with no partner always renders `?`. Doubles as a syntax-error hint. |
| `colorMode` | `palette` | `palette` (32 identity colours) / `monochrome` / `depth` (one colour per nesting level). |
| `monochromeColor` | *(theme)* | `#RRGGBB` for monochrome mode. |
| `palette` | *(built-in)* | Override the 32 colours. Short arrays fill from the built-in palette. |
| `scopeSpotlight` | off | The pair enclosing the caret stays vivid; every other brace fades. |
| `spotlightDim` | 30 | How visible an out-of-scope brace stays, as a percentage. |
| `complexityWarningDepth` | 0 (off) | Braces nested at least this deep are drawn sweating and unsteady. |
| `indentGuides` | off | A vertical guide down the inside of each multi-line pair, in that pair's colour. |
| `indentGuideStrength` | 45 | How strong the guides are against the background. |
| `enableMotion` | on | **Turn this off** on battery, on a projector, or if movement in a text editor is unpleasant. |
| `animationFrameRate` | 15 | The editor does not need 60. |
| `colorCycleSeconds` | 6 | Time to travel the whole wheel. |
| `maxFileLength` | 1,000,000 | Files longer than this are left alone entirely. |
| `decorScale` | 1 | Multiplier for creature and costume geometry. |
| `thighHighStyle` | `garter` | `off` / `twotone` / `garter` / `banded`. |
| `tail` | on | A tail curling off the bottom right of a cat brace. |
| `overlayNudge` | 0 | Moves everything drawn on a brace up or down, in `em`. See [Fonts](#fonts). |
| `renderMode` | `full` | `full` / `text` (no drawn overlays) / `plain` (colours only). |
| `languages` | `["*"]` | Language ids to colour. |
| `traits` | *(catalogue defaults)* | Per-trait percentages by id. Edit through the gallery. |

Changing any personality percentage reshuffles which braces have which personality, because
the roll is a function of the thresholds. Colours are unaffected.

### The trait gallery

**Identity Braces: Open the trait gallery** — eighty-six sliders grouped by layer, each next
to the thing it turns on, with a search box and the five presets.

Two things about it are worth knowing, both inherited from the Visual Studio options page.
**The layer totals are live**: body, creature, costume and motion share a single 0–100 roll,
so a layer summing past 100 makes the traits at the bottom of it unreachable, and the header
turns orange rather than failing silently. And **the previews are the real renderer** — they
call `computeStyle` and emit the same CSS and the same SVG the editor draws with, so they
cannot drift from what you will actually get.

They are also held still on purpose. Motion is a slider you can turn up and go and look at;
a hundred and seventy simultaneous animations in a panel is not a preview, it is a stress
test.

### Bringing your settings across

**Identity Braces: Import settings from Visual Studio** reads
`%APPDATA%\IdentityBraces\settings.ini` — the plain key/value file the Visual Studio
extension keeps its settings in — and writes the equivalents into your user settings. Both
extensions describe the same braces, so nobody should have to decide twice which of the
eighty-six traits they want.

Two things it does deliberately.

**It writes every mapped setting, including the ones that already match the default.** An
import that only wrote the differences would leave whatever was in `settings.json`
beforehand in charge of the rest, and "port my settings" would quietly mean "port some of
them".

**It writes every trait, including the zeroes.** Visual Studio only records the traits you
have turned *on* and treats everything absent as zero; VS Code falls back to the catalogue
default instead. Copying only what was in the file would switch `question`, `catgirl`,
`cycle` and `thighhighs` back on for anyone who had deliberately switched them off.

Then it tells you what did not come across — `ReserveEarSpace` and `SceneIntervalSeconds`
have no counterpart here, and a trait you had set to 63 that draws nothing in this port is
worth hearing about rather than discovering.

### Commands

| Command | What it does |
|---|---|
| `Identity Braces: Toggle` | On or off. |
| `Identity Braces: Toggle motion` | Colours stay, movement stops. |
| `Identity Braces: Apply a trait preset` | Off / Default / Menagerie / Restless / Unusable. |
| `Identity Braces: Open the trait gallery` | The eighty-six sliders. |
| `Identity Braces: Identify the brace at the cursor` | Name, identity hash, colour, depth and every trait it rolled. |
| `Identity Braces: Import settings from Visual Studio` | The above. |

---

## How this differs from the Visual Studio extension

The two editors give you completely different tools, and the port is not a translation so
much as a rebuild on top of the one thing that carried over unchanged: the geometry.

**Visual Studio gives you a WPF canvas per brace.** You draw shapes into it, you animate a
brush, and the render thread does the rest. **VS Code gives you a stylesheet.** A decoration
is a CSS rule plus a list of ranges; there is nothing to animate in place, and nothing to
draw into. So:

- **A drawn brace is three layers.** The real character goes `transparent`; a `::before`
  pseudo-element redraws the glyph — substituted, mirrored, in Comic Sans, whatever it
  rolled; and an absolutely-positioned `::after` carries the creature and the costume as an
  inline SVG data URI. Transforms and opacity go on the range's own span, so they move all
  three together.
- **The stockings are the same trick as the original**, arrived at differently. In WPF they
  are clipped copies of the brace's own stroke. Here they are a hard-stopped
  `linear-gradient` under `background-clip: text`, which is the same idea — the paint is
  clipped to the glyph, never drawn behind it — and it means a stocking is exactly as wide
  as the stroke it clothes at every font size, automatically.
- **Animation is quantisation.** Every phase, colour and transform is rounded to a fixed
  number of steps, so an animated brace cycles through a small fixed set of rules rather than
  minting a new one every frame. One decoration type per distinct *appearance*, cached, with
  an LRU cap; a frame with nothing new on screen costs one pass over the visible braces.
  The clock only runs while something visible is actually moving.
- **The ears have no headroom to reserve.** `ILineTransformSource` — the mechanism that made
  lines physically taller for cat ears, and that broke the editor's layout twice before it
  was got right — has no VS Code equivalent. The ears simply overhang the line above, which
  is what the Visual Studio extension does by default anyway.
- **Colours come from the palette, not from the theme.** There is no Fonts and Colors here,
  so the 32 entries are a setting (`identityBraces.palette`) instead of 32 registered
  classifications.

### Traits

82 of the 86 traits are real here. Four are not, and the gallery labels them **not in this
port** rather than leaving you to work it out from a slider that does nothing:

| Trait | Why not |
|---|---|
| `typewriter` | Fades in once on first appearance. The renderer is stateless — it does not know when it first saw a brace. |
| `swapplaces`, `tableflip`, `firebrigade` | The scene system. It places props from live line geometry, and an extension cannot ask VS Code where a character is on screen. |

They still roll, so turning one up quietly costs you the braces it lands on.

`buildreactive` is here but reads task processes rather than builds — `onDidEndTaskProcess`,
exit code zero or not, for 25 seconds afterwards. A task is not quite a build, but it is the
closest thing every project type agrees on, and a failing test run is as good an excuse to
sulk as a failing compile.

`named` goes through the editor's hover rather than a tooltip on the decoration, for the same
reason it goes through quick info in Visual Studio: the drawn overlay sits above the text, and
making it hit-testable would have it swallow the clicks that place your caret.

---

## Fonts

The Visual Studio extension measures a brace's ink from the real typeface through
`FormattedText`. An extension host has no font metrics at all, so this one uses the measured
Consolas proportions — ink height 0.9 em, ink top a fifth of an em below the top of the box —
expressed relative to the font and therefore correct for any monospace face within a few
percent.

If your font is one of the ones where a few percent is not close enough, and the ears sit too
high or too low, `identityBraces.overlayNudge` moves everything drawn on a brace up or down
in `em`. Start at ±0.05.

---

## Design reference

```bash
npm run bestiary
```

Writes `design/bestiary.html` — every one of the 86 traits on an opener and a closer, drawn
by the extension's own renderer, plus the same slab of code at editor size and magnified.
It is the fastest way to see what a change to the geometry did, and it opens in any browser.

---

## Limitations

- **Whole-file rescan per edit**, at about eight times the cost of the C# version. Debounced,
  and the debounce lengthens past 200,000 characters. `maxFileLength` is the backstop.
- **VS Code's own bracket-pair colourisation and bracket guides** will fight your colours.
  Turn off `editor.bracketPairColorization.enabled` and `editor.guides.bracketPairs`.
- **32 colours, not infinite.** Beyond about thirty hues they stop being distinguishable in a
  punctuation glyph anyway.
- **The drawn overlays are pseudo-elements**, so they cannot be selected, and a brace under a
  selection highlight keeps its own colour rather than inverting with the text.
- **High contrast themes** are not special-cased. Set `colorMode` to `monochrome`, or retune
  `palette`.
- **The scanner is a C-family lexer**, not a language server. It understands `//`, `/* */`,
  `"…"`, `'…'`, `` `…` ``, `@"…"` and `"""…"""`. Interpolation holes are treated as opaque
  string content, and a single-quoted run that does not close on its line is treated as *not*
  a string — which is what stops a Rust lifetime from swallowing the rest of the file. Both
  approximations fail in the safe direction: skipping a brace we could have coloured rather
  than colouring one inside a string.

---

## Troubleshooting

**Nothing is coloured.** Check the language is covered by `identityBraces.languages`, and
that the file is under `maxFileLength`.

**The drawn overlays are in the wrong place.** Try `overlayNudge` first. If they are wrong
horizontally as well, or they misbehave in some other way, drop `renderMode` to `text` —
colours and glyph substitutions, no overlays — or to `plain`, and please report the font.

**Typing feels sluggish in one huge file.** Lower `maxFileLength` below that file's size.

**The motion is too much.** `Identity Braces: Toggle motion`. Colours stay, movement stops.

---

## Layout

```
vscode/
  src/core/      the scanner, the identity hashing, the trait roll   (no editor dependencies,
                 and bit-identical to the Visual Studio extension's Core/)
  src/render/    the SVG drawing context, the trait painters, the CSS
  src/ui/        the hover and the trait gallery
  src/test/      the suite, including the C# parity fixture
  tools/parity/  the C# generator behind that fixture
  tools/         the bestiary page generator
```

`src/core/` is a straight port. `src/render/` is not — but the trait geometry inside it is,
because every shape is authored in ink units and knows nothing about the editor drawing it.
That is what made eighty-two creatures a copy-and-paste rather than a rewrite.
