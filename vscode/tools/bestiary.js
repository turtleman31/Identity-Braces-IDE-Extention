// Renders every trait through the real renderer into a standalone page, so the SVG geometry
// and the overlay positioning can be checked without a VS Code window.
const fs = require('fs');
const path = require('path');

const OUT = path.join(__dirname, '..', 'out');
const { TRAIT_CATALOG } = require(path.join(OUT, 'core/traitCatalog'));
const { LAYER_NAMES } = require(path.join(OUT, 'core/traitLayer'));
const { weightsFrom } = require(path.join(OUT, 'core/scanSettings'));
const { PALETTE, PALETTE_COUNT } = require(path.join(OUT, 'core/palette'));
const { previewHtml } = require(path.join(OUT, 'render/preview'));
const { computeStyle } = require(path.join(OUT, 'render/braceStyle'));
const { overlayCss, GLYPH_CSS } = require(path.join(OUT, 'render/css'));
const { scanBraces } = require(path.join(OUT, 'core/braceScanner'));

const FONT = "Consolas, 'Cascadia Mono', 'Courier New', monospace";
const FONT_SIZE = 14;
const LINE_HEIGHT_EM = 1.35;

const settings = {
    enabled: true,
    scan: {
        curly: true, round: true, square: true,
        traitWeights: weightsFrom({}),
        questionUnmatched: true, independentBraces: true,
        colorByDepth: false, complexityWarningDepth: 0, paletteCount: PALETTE_COUNT,
    },
    palette: PALETTE,
    colorMode: 'palette',
    monochromeColor: '',
    scopeSpotlight: false,
    spotlightDim: 0.3,
    indentGuides: false,
    indentGuideStrength: 0.45,
    enableMotion: true,
    animationFrameRate: 15,
    colorCycleSeconds: 6,
    maxFileLength: 1000000,
    decorScale: 1,
    thighHighStyle: 'garter',
    tail: true,
    renderMode: 'full',
    languages: ['*'],
    fontSizePx: FONT_SIZE,
    lineHeightEm: LINE_HEIGHT_EM,
    fontFamily: FONT,
    overlayNudgeEm: 0,
};

// ---- the bestiary ----

let sections = '';
for (let layer = 0; layer < 5; layer++) {
    const members = TRAIT_CATALOG.filter((t) => t.layer === layer);
    const cells = members
        .map(
            (t) => `<div class="cell">
    <div class="pair">${previewHtml(t.id, '{', settings)}${previewHtml(t.id, '}', settings)}</div>
    <div class="label">${t.name}<br><span class="id">${t.id}</span></div>
  </div>`,
        )
        .join('\n');
    sections += `<h2>${LAYER_NAMES[layer]}</h2><div class="grid">${cells}</div>`;
}

// ---- a slab of code, laid out the way VS Code lays out a line ----

const SAMPLE = `public async Task<Order> GetOrderAsync(int id)
{
    if (id < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(id));
    }

    var items = new List<Item> { new Item { Sku = "a", Qty = 1 } };
    return await _store.FindAsync(id, items);
}`;

const braces = scanBraces(SAMPLE, settings.scan);
const byOffset = new Map(braces.map((b) => [b.position, b]));

let offset = 0;
const lines = [];
for (const line of SAMPLE.split('\n')) {
    let html = '';
    for (let i = 0; i < line.length; i++) {
        const brace = byOffset.get(offset + i);
        if (!brace) {
            html += escapeHtml(line[i]);
            continue;
        }

        const style = computeStyle(brace, {
            settings, timeMs: 0, sessionMinutes: 0,
            caretLine: -1, caretColumn: -1, line: 0, column: i, dim: false,
        });

        if (!style.glyph) {
            html += `<span style="color:${style.color}">${escapeHtml(line[i])}</span>`;
            continue;
        }

        // Same order as the editor's own pseudo-elements: the glyph, then the character
        // that holds the cell open, then the overlay.
        html +=
            `<span class="ib" style="${style.spanCss}">` +
            `<span style="${GLYPH_CSS}; color:${style.glyph.color}; ${style.glyph.css}">${escapeHtml(style.glyph.text)}</span>` +
            `<span style="color:transparent">${escapeHtml(line[i])}</span>` +
            (style.overlay ? `<span style="${overlayCss(style.overlay, settings)}"></span>` : '') +
            `</span>`;
    }

    lines.push(`<div class="view-line"><span>${html}</span></div>`);
    offset += line.length + 1;
}

function escapeHtml(v) {
    return v.replace(/[&<>]/g, (c) => (c === '&' ? '&amp;' : c === '<' ? '&lt;' : '&gt;'));
}

const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><style>
body { background:#1E1E1E; color:#D4D4D4; font-family:system-ui, sans-serif; margin:0; padding:24px 32px 64px; }
h1 { font-size:18px; font-weight:600; }
h2 { font-size:13px; text-transform:uppercase; letter-spacing:.08em; color:#9A9A9A; margin:32px 0 10px; }
.grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(140px, 1fr)); gap:14px 8px; }
.cell { text-align:center; padding:10px 4px 8px; border:1px solid #2E2E2E; border-radius:6px; }
.pair { font-family:${FONT}; font-size:34px; line-height:${LINE_HEIGHT_EM}; height:3em; display:flex; gap:.7em; justify-content:center; align-items:center; }
.ib-brace, .ib { position:relative; display:inline-block; }
.label { font-size:11px; color:#C8C8C8; margin-top:6px; line-height:1.35; }
.id { color:#7A7A7A; font-family:${FONT}; }
.code { margin-top:12px; background:#1E1E1E; border:1px solid #2E2E2E; border-radius:6px; padding:24px 18px; overflow:hidden; }
.view-line { font-family:${FONT}; font-size:${FONT_SIZE}px; line-height:${LINE_HEIGHT_EM}; white-space:pre; position:relative; height:${LINE_HEIGHT_EM}em; }
.zoom .view-line { font-size:34px; }
</style></head><body>
<h1>Identity Braces — every trait, drawn by the extension's own renderer</h1>
<h2>Alignment check — editor size (${FONT_SIZE}px)</h2>
<div class="code">${lines.join('\n')}</div>
<h2>Alignment check — magnified</h2>
<div class="code zoom">${lines.join('\n')}</div>
${sections}
</body></html>`;

const out = path.join(__dirname, '..', 'design', 'bestiary.html');
fs.writeFileSync(out, html);
console.log('wrote', out);
