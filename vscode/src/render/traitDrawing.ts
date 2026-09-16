import { TraitIds } from '../core/traitIds';
import { Painter, registerCreatures } from './creatures';
import { registerCostumes } from './costumes';
import { registerEffects } from './effects';
import { registerMotions } from './motions';
import { SvgContext } from './svgContext';

/**
 * Maps a trait id to the code that draws it.
 *
 * The other half of the registry: {@link TRAIT_CATALOG} owns what a trait is called and how
 * often it appears; this owns what it looks like. Adding a creature is one row there and one
 * entry here — no new enum member, no new settings field, no new branch anywhere else.
 *
 * A trait with no entry here simply does not draw, so the catalogue can list things ahead of
 * their geometry without breaking anything. That is also how the traits this port does not
 * implement stay harmless: they roll, they are named in the gallery, and they draw nothing.
 */
const PAINTERS = new Map<string, Painter>();

registerCreatures(PAINTERS);
registerCostumes(PAINTERS);
registerMotions(PAINTERS);
registerEffects(PAINTERS);

export function hasPainter(id: string | null): boolean {
    return id !== null && PAINTERS.has(id);
}

/**
 * Traits that are real here without going through a painter.
 *
 * Substitutions, transforms, fades and font changes are CSS on the brace itself rather than
 * shapes drawn beside it, so they never reach the registry — see
 * {@link module:render/braceStyle}. Listing them explicitly is the only way the gallery can
 * tell you honestly which sliders do something.
 */
const STYLED = new Set<string>([
    TraitIds.Question,
    TraitIds.UnicodeVariant,
    TraitIds.WrongBracket,
    TraitIds.Mirrored,
    TraitIds.UpsideDown,
    TraitIds.Emoji,
    TraitIds.ForeignFont,
    TraitIds.Subscript,
    TraitIds.ThighHighs,
    TraitIds.ColourCycle,
    TraitIds.Wobble,
    TraitIds.Bounce,
    TraitIds.Breathe,
    TraitIds.Heartbeat,
    TraitIds.Shiver,
    TraitIds.Blink,
    TraitIds.Spin,
    TraitIds.Flip,
    TraitIds.Glitch,
    TraitIds.Drift,
    TraitIds.Flicker,
    TraitIds.Wave,
    TraitIds.GradientFill,
    TraitIds.BoldItalic,
    TraitIds.Tilted,
    TraitIds.Shadow,
    TraitIds.Ghost,
    TraitIds.Nocturnal,
    TraitIds.Drunk,
    TraitIds.Gravity,
    TraitIds.FleeCursor,
    TraitIds.StageFright,

    // Not drawn at all: the name arrives through the editor's own hover, because a drawn
    // overlay sits above the text and making it hit-testable would have it swallow the
    // clicks that place your caret.
    TraitIds.Named,
]);

/**
 * Whether this trait actually does anything in this port.
 *
 * Four do not. `typewriter` needs to know when a brace was first seen, which a stateless
 * renderer does not; `swapplaces`, `tableflip` and `firebrigade` are the scene system, which
 * places props from live line geometry that VS Code does not hand out. They still roll, so
 * turning one up quietly costs you the braces it lands on — which is why the gallery says so
 * rather than leaving you to work it out.
 */
export function isImplemented(id: string): boolean {
    return PAINTERS.has(id) || STYLED.has(id);
}

export function paint(id: string | null, context: SvgContext): void {
    if (id === null) {
        return;
    }

    const painter = PAINTERS.get(id);
    if (!painter) {
        return;
    }

    try {
        painter(context);
    } catch {
        // One malformed trait must not cost the brace its glyph, nor take down the pass that
        // is drawing forty others.
    }
}

/**
 * The text a body trait renders instead of the real character.
 *
 * Body traits are a substitution rather than an overlay, so they resolve to a string here
 * rather than to a painter. The buffer is untouched either way — a brace drawn as a question
 * mark is still a brace to the compiler, to the caret, to Find and to Git.
 */
export function resolveBody(bodyId: string | null, character: string): string {
    switch (bodyId) {
        case TraitIds.Question:
            return '?';
        case TraitIds.UnicodeVariant:
            return unicodeVariantFor(character);
        case TraitIds.WrongBracket:
            return wrongBracketFor(character);
        case TraitIds.Emoji:
            return emojiFor(character);
        default:
            return character;
    }
}

function unicodeVariantFor(character: string): string {
    switch (character) {
        case '{':
            return '｛';
        case '}':
            return '｝';
        case '(':
            return '（';
        case ')':
            return '）';
        case '[':
            return '【';
        case ']':
            return '】';
        default:
            return character;
    }
}

/** Swaps the bracket family while keeping the direction. Deliberately hostile. */
function wrongBracketFor(character: string): string {
    switch (character) {
        case '{':
            return '(';
        case '}':
            return ')';
        case '(':
            return '[';
        case ')':
            return ']';
        case '[':
            return '{';
        case ']':
            return '}';
        default:
            return character;
    }
}

function emojiFor(character: string): string {
    const opening = character === '{' || character === '(' || character === '[';
    return opening ? '👉' : '👈';
}
