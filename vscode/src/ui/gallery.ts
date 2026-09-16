import * as vscode from 'vscode';
import { TRAIT_CATALOG, TraitInfo } from '../core/traitCatalog';
import { LAYER_NAMES, TraitLayer } from '../core/traitLayer';
import { TRAIT_PRESETS, findPreset, presetWeights } from '../core/traitPresets';
import { escapeAttr, escapeHtml, previewHtml } from '../render/preview';
import { isImplemented } from '../render/traitDrawing';
import { readSettings } from '../settings';

/**
 * The trait gallery: eighty-six sliders, grouped by layer, each next to the thing it turns on.
 *
 * Eighty-six traits do not fit in a settings page, and until this existed they were only
 * reachable by hand-editing `identityBraces.traits` — where you also had to already know
 * what `cthulhu` looked like to decide whether you wanted it.
 *
 * Two things about it are worth knowing. The layer totals are live: body, creature, costume
 * and motion share a single 0-100 roll, so a layer summing past 100 makes the traits at the
 * bottom of that layer unreachable, and the header says so rather than failing silently.
 * And the previews are the real renderer, not an illustration of it.
 */
export class Gallery {
    private static current: Gallery | undefined;

    private readonly disposables: vscode.Disposable[] = [];

    static show(extensionUri: vscode.Uri): void {
        if (Gallery.current) {
            Gallery.current.panel.reveal();
            return;
        }

        const panel = vscode.window.createWebviewPanel(
            'identityBraces.gallery',
            'Identity Braces — Traits',
            vscode.ViewColumn.Active,
            { enableScripts: true, retainContextWhenHidden: true, localResourceRoots: [extensionUri] },
        );

        Gallery.current = new Gallery(panel);
    }

    static refresh(): void {
        Gallery.current?.render();
    }

    private constructor(private readonly panel: vscode.WebviewPanel) {
        this.render();

        this.disposables.push(
            panel.webview.onDidReceiveMessage((message) => this.onMessage(message)),
            panel.onDidDispose(() => this.dispose()),
        );
    }

    private dispose(): void {
        Gallery.current = undefined;
        for (const d of this.disposables) {
            d.dispose();
        }

        this.panel.dispose();
    }

    private async onMessage(message: { type: string; id?: string; value?: number }): Promise<void> {
        const config = vscode.workspace.getConfiguration('identityBraces');
        const weights = { ...config.get<Record<string, number>>('traits', {}) };

        if (message.type === 'set' && message.id) {
            weights[message.id] = Math.max(0, Math.min(100, Math.round(message.value ?? 0)));
            await config.update('traits', weights, vscode.ConfigurationTarget.Global);
            return;
        }

        if (message.type === 'preset' && message.id) {
            const preset = findPreset(message.id);
            if (preset) {
                await config.update('traits', presetWeights(preset), vscode.ConfigurationTarget.Global);
                this.render();
            }
        }
    }

    private render(): void {
        const settings = readSettings(vscode.window.activeTextEditor?.document.uri);
        const overrides = vscode.workspace.getConfiguration('identityBraces').get<Record<string, number>>('traits', {});
        const percentOf = (t: TraitInfo) => overrides[t.id] ?? t.defaultPercent;

        const sections = [
            TraitLayer.Body,
            TraitLayer.Creature,
            TraitLayer.Costume,
            TraitLayer.Motion,
            TraitLayer.Effect,
        ]
            .map((layer) => this.section(layer, percentOf, settings))
            .join('\n');

        const presets = TRAIT_PRESETS.map(
            (p) =>
                `<button class="preset" data-id="${escapeAttr(p.id)}" title="${escapeAttr(p.description)}">` +
                `${escapeHtml(p.name)}</button>`,
        ).join('');

        const nonce = String(Math.random()).slice(2);
        const csp =
            `default-src 'none'; img-src ${this.panel.webview.cspSource} data:; ` +
            `style-src ${this.panel.webview.cspSource} 'unsafe-inline'; script-src 'nonce-${nonce}';`;

        this.panel.webview.html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta http-equiv="Content-Security-Policy" content="${csp}">
<style>${STYLE}</style>
</head>
<body>
<header>
  <div class="presets">${presets}</div>
  <input id="search" type="search" placeholder="Search traits" autocomplete="off">
</header>
<p class="note">Percentages are shares of one roll per layer, except effects, which are rolled
independently. Changing any of them reshuffles which braces have which personality — colours
are unaffected.</p>
${sections}
<script nonce="${nonce}">${SCRIPT}</script>
</body>
</html>`;
    }

    private section(
        layer: TraitLayer,
        percentOf: (t: TraitInfo) => number,
        settings: ReturnType<typeof readSettings>,
    ): string {
        const members = TRAIT_CATALOG.filter((t) => t.layer === layer);
        const total = members.reduce((sum, t) => sum + percentOf(t), 0);
        const shared = layer !== TraitLayer.Effect;

        const rows = members
            .map((trait) => {
                const percent = percentOf(trait);
                const built = isImplemented(trait.id);

                return `<div class="row${percent > 0 ? ' on' : ''}" data-search="${escapeAttr(
                    `${trait.name} ${trait.id} ${trait.description}`.toLowerCase(),
                )}">
  <div class="preview">${previewHtml(trait.id, '{', settings)}${previewHtml(trait.id, '}', settings)}</div>
  <div class="meta">
    <div class="name">${escapeHtml(trait.name)}${built ? '' : '<span class="unbuilt" title="Rolls, but draws nothing in the VS Code port">not in this port</span>'}</div>
    <div class="desc">${escapeHtml(trait.description)}</div>
  </div>
  <input class="slider" type="range" min="0" max="100" value="${percent}" data-id="${escapeAttr(trait.id)}">
  <output class="value">${percent}</output>
</div>`;
            })
            .join('\n');

        const heading = shared
            ? `${LAYER_NAMES[layer]} <span class="total${total > 100 ? ' over' : ''}">${total} / 100</span>`
            : `${LAYER_NAMES[layer]} <span class="total">rolled independently</span>`;

        return `<section><h2>${heading}</h2>${rows}</section>`;
    }
}

const STYLE = `
body { font-family: var(--vscode-font-family); color: var(--vscode-foreground);
       background: var(--vscode-editor-background); padding: 0 16px 48px; }
header { position: sticky; top: 0; z-index: 2; padding: 12px 0 8px;
         background: var(--vscode-editor-background); display: flex; gap: 12px; align-items: center;
         flex-wrap: wrap; border-bottom: 1px solid var(--vscode-panel-border); }
.presets { display: flex; gap: 6px; }
button.preset { font: inherit; padding: 4px 10px; cursor: pointer; border: none; border-radius: 3px;
                color: var(--vscode-button-secondaryForeground, var(--vscode-button-foreground));
                background: var(--vscode-button-secondaryBackground, var(--vscode-button-background)); }
button.preset:hover { background: var(--vscode-button-hoverBackground); }
#search { font: inherit; flex: 1; min-width: 160px; padding: 4px 8px; border-radius: 3px;
          color: var(--vscode-input-foreground); background: var(--vscode-input-background);
          border: 1px solid var(--vscode-input-border, transparent); }
.note { color: var(--vscode-descriptionForeground); max-width: 70ch; font-size: 0.92em; }
h2 { font-size: 1em; text-transform: uppercase; letter-spacing: 0.08em; margin: 28px 0 8px;
     color: var(--vscode-descriptionForeground); }
.total { font-weight: normal; text-transform: none; letter-spacing: 0; opacity: 0.7; margin-left: 8px; }
.total.over { color: var(--vscode-editorWarning-foreground); opacity: 1; }
.row { display: grid; grid-template-columns: 6.5em 1fr 12em 3em; gap: 12px; align-items: center;
       padding: 4px 8px; border-radius: 4px; }
.row:hover { background: var(--vscode-list-hoverBackground); }
.row:not(.on) .preview { opacity: 0.45; }
/* Tall enough for the overlays. They are absolutely positioned and so contribute no height
   of their own — without a floor here, a wizard hat lands in the row above. */
.preview { font-family: var(--vscode-editor-font-family, monospace); font-size: 24px;
           line-height: 1.35; height: 2.9em; display: flex; align-items: center;
           gap: 0.6em; justify-content: center; }
.ib-brace { position: relative; display: inline-block; }
.name { font-weight: 600; }
.unbuilt { font-weight: normal; font-size: 0.8em; margin-left: 8px; padding: 1px 5px; border-radius: 3px;
           color: var(--vscode-descriptionForeground); border: 1px solid var(--vscode-panel-border); }
.desc { color: var(--vscode-descriptionForeground); font-size: 0.92em; }
.slider { width: 100%; }
.value { text-align: right; font-variant-numeric: tabular-nums; color: var(--vscode-descriptionForeground); }
.row.hidden { display: none; }
`;

const SCRIPT = `
const vscode = acquireVsCodeApi();

document.addEventListener('input', (e) => {
  const el = e.target;
  if (el.classList.contains('slider')) {
    const row = el.closest('.row');
    row.querySelector('.value').textContent = el.value;
    row.classList.toggle('on', Number(el.value) > 0);
    vscode.postMessage({ type: 'set', id: el.dataset.id, value: Number(el.value) });
    return;
  }

  if (el.id === 'search') {
    const needle = el.value.trim().toLowerCase();
    for (const row of document.querySelectorAll('.row')) {
      row.classList.toggle('hidden', needle !== '' && !row.dataset.search.includes(needle));
    }
  }
});

document.addEventListener('click', (e) => {
  const button = e.target.closest('button.preset');
  if (button) {
    vscode.postMessage({ type: 'preset', id: button.dataset.id });
  }
});
`;
