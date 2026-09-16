import { BraceInfo, BraceKind } from '../core/braceInfo';
import { fnv1a } from '../core/hash';
import { findTrait } from '../core/traitCatalog';
import { TraitLayer } from '../core/traitLayer';
import { BraceTraits, PLAIN_TRAITS } from '../core/traitRoll';
import { Settings } from '../settings';
import { computeStyle } from './braceStyle';
import { GLYPH_CSS, overlayCss } from './css';

/**
 * One brace, drawn wearing exactly one trait, as HTML.
 *
 * The gallery calls the real renderer rather than illustrating it — same
 * {@link computeStyle}, same SVG, same CSS — so a preview cannot drift from what you will
 * actually get. Held still, and that is deliberate: a hundred and twenty simultaneous
 * animations is not a preview, it is a stress test.
 */
export function previewHtml(traitId: string, character: string, settings: Settings): string {
    const info = findTrait(traitId);
    if (!info) {
        return escapeHtml(character);
    }

    const brace = previewBrace(character, wearing(traitId, info.layer));
    const style = computeStyle(brace, {
        settings,
        timeMs: 0,
        sessionMinutes: 8,
        caretLine: -1,
        caretColumn: -1,
        line: 0,
        column: 0,
        dim: false,
    });

    const parts: string[] = [];
    const glyph = style.glyph ?? { text: character, color: style.color ?? style.baseColor, css: '' };

    parts.push(
        `<span class="ib-glyph" style="${escapeAttr(`${GLYPH_CSS}; color: ${glyph.color}; ${glyph.css}`)}">` +
            `${escapeHtml(glyph.text)}</span>`,
    );

    // The transparent character holds the cell open, exactly as it does in the editor — and
    // the overlay follows it rather than preceding it, because in the editor it is an
    // `::after` and is positioned relative to where that lands. Getting this order wrong is
    // how a preview comes to disagree with the thing it is previewing.
    parts.push(`<span style="color: transparent">${escapeHtml(character)}</span>`);

    if (style.overlay) {
        parts.push(`<span class="ib-overlay" style="${escapeAttr(overlayCss(style.overlay, settings))}"></span>`);
    }

    return `<span class="ib-brace" style="${escapeAttr(style.spanCss)}">${parts.join('')}</span>`;
}

/** A brace with one trait switched on and everything else left plain. */
function wearing(traitId: string, layer: TraitLayer): BraceTraits {
    const traits: BraceTraits = { ...PLAIN_TRAITS, effects: [], isDrawn: true };

    switch (layer) {
        case TraitLayer.Body:
            traits.body = traitId;
            break;
        case TraitLayer.Creature:
            traits.creature = traitId;
            break;
        case TraitLayer.Costume:
            traits.costume = traitId;
            break;
        case TraitLayer.Motion:
            traits.motion = traitId;
            break;
        default:
            traits.effects = [traitId];
            break;
    }

    return traits;
}

/**
 * A fixed identity, so every preview of a given trait looks the same every time the gallery
 * is opened — and so the traits that draw on their identity for variety (a lean angle, a
 * drift phase) show a representative one rather than a new one per render.
 */
const PREVIEW_IDENTITY = fnv1a('identitybraces.preview');

function previewBrace(character: string, traits: BraceTraits): BraceInfo {
    const isOpen = character === '{' || character === '(' || character === '[';
    return {
        position: 0,
        character,
        kind: BraceKind.Curly,
        isOpen,
        isMatched: true,
        idHi: PREVIEW_IDENTITY.hi,
        idLo: PREVIEW_IDENTITY.lo,
        colorIndex: isOpen ? 12 : 26,
        depth: 1,
        partnerIndex: -1,
        parentIndex: -1,
        traits,
    };
}

export function escapeHtml(value: string): string {
    return value.replace(/[&<>]/g, (c) => (c === '&' ? '&amp;' : c === '<' ? '&lt;' : '&gt;'));
}

export function escapeAttr(value: string): string {
    return escapeHtml(value).replace(/"/g, '&quot;');
}
