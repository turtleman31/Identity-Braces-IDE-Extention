import { BraceInfo, BraceKind } from './braceInfo';
import { fnv1aNormalized, mix, R, toIndex, u64 } from './hash';
import { ScanSettings } from './scanSettings';
import { rollTraits } from './traitRoll';
import { TraitTable } from './traitTable';

/**
 * Finds braces in C-family source and gives each pair a stable identity.
 *
 * This is a deliberately small, self-contained lexer rather than a query against a language
 * server. Asking for a full semantic tokenisation of the whole document on every keystroke
 * is slow and asynchronous; an approximate lexer is predictable, allocation-light, and
 * covers every C-family language at once.
 *
 * Known approximations, all of which fail in the safe direction — skipping a brace we could
 * have coloured rather than colouring one inside a string: interpolation holes in `$"{expr}"`
 * and template literals are treated as opaque string content, so braces inside them are
 * ignored. A single-quoted run that does not close on the same line is treated as *not* a
 * string, which is what stops a Rust lifetime or a stray apostrophe from swallowing the
 * rest of the file.
 */

/** Separates a closing brace's identity from its opener's. */
const CLOSER_SALT = u64('0x5EA1EDFA7ECAFE');

const MAX_HEADER_LENGTH = 200;
const MAX_HEADER_LOOKBACK_LINES = 4;

const enum Ch {
    Tab = 9,
    LF = 10,
    CR = 13,
    Space = 32,
    Dollar = 36,
    Quote = 34,
    Apostrophe = 39,
    LParen = 40,
    RParen = 41,
    Star = 42,
    Slash = 47,
    At = 64,
    LBracket = 91,
    Backslash = 92,
    RBracket = 93,
    Backtick = 96,
    LBrace = 123,
    RBrace = 125,
}

export function scanBraces(text: string, settings: ScanSettings): BraceInfo[] {
    if (!text) {
        return [];
    }

    return pair(text, tokenize(text, settings), settings);
}

/**
 * Pass one: walk the text, skipping comments and strings, collecting brace offsets.
 *
 * Offsets only — the kind and direction come back out of the buffer in pass two, which is
 * one array instead of three and one less object per brace.
 */
function tokenize(text: string, settings: ScanSettings): number[] {
    const found: number[] = [];
    const n = text.length;
    let i = 0;

    while (i < n) {
        const c = text.charCodeAt(i);

        // Comments.
        if (c === Ch.Slash && i + 1 < n) {
            const next = text.charCodeAt(i + 1);
            if (next === Ch.Slash) {
                i = skipToLineEnd(text, i + 2);
                continue;
            }

            if (next === Ch.Star) {
                i = skipBlockComment(text, i + 2);
                continue;
            }
        }

        // Verbatim / interpolated / raw string prefixes: @" $" $@" @$"
        if (c === Ch.At || c === Ch.Dollar) {
            let q = i;
            let verbatim = false;
            while (q < n) {
                const p = text.charCodeAt(q);
                if (p !== Ch.At && p !== Ch.Dollar) {
                    break;
                }

                if (p === Ch.At) {
                    verbatim = true;
                }

                q++;
            }

            if (q < n && text.charCodeAt(q) === Ch.Quote) {
                i = skipQuoted(text, q, verbatim);
                continue;
            }
        }

        if (c === Ch.Quote) {
            i = skipQuoted(text, i, false);
            continue;
        }

        // JS/TS template literal. Spans lines; holes are treated as opaque.
        if (c === Ch.Backtick) {
            i = skipTemplate(text, i);
            continue;
        }

        // Char literal, or a single-quoted JS string. Must close on the same line, otherwise
        // it is something else entirely and we must not swallow the rest of the file.
        if (c === Ch.Apostrophe) {
            const end = trySkipSingleQuoted(text, i);
            if (end > 0) {
                i = end;
                continue;
            }
        }

        const kind = kindOf(c);
        if (kind >= 0 && includes(settings, kind)) {
            found.push(i);
        }

        i++;
    }

    return found;
}

/** Pass two: match pairs with a stack and derive each pair's identity. */
function pair(text: string, positions: number[], settings: ScanSettings): BraceInfo[] {
    const result: BraceInfo[] = new Array(positions.length);

    // The open braces still waiting for a partner, as parallel stacks of map index and the
    // character that would close them.
    const stackIndex: number[] = [];
    const stackExpected: number[] = [];

    // Flattened once for the whole file, not per brace: see TraitTable.
    const table = new TraitTable(settings.traitWeights);

    for (let i = 0; i < positions.length; i++) {
        const position = positions[i];
        const code = text.charCodeAt(position);
        const kind = kindOf(code);
        const open = isOpener(code);

        const brace: BraceInfo = {
            position,
            character: text[position],
            kind,
            isOpen: open,
            isMatched: false,
            idHi: 0,
            idLo: 0,
            colorIndex: 0,
            depth: stackIndex.length,
            partnerIndex: -1,

            // Whatever is on top of the stack is by definition the innermost block still
            // open here, which is exactly what encloses this brace. Recording it costs one
            // field; recovering it afterwards costs a walk back through the file.
            parentIndex: stackIndex.length > 0 ? stackIndex[stackIndex.length - 1] : -1,

            traits: null!,
        };

        result[i] = brace;

        if (open) {
            computeIdentity(text, position, kind, stackIndex.length);
            brace.idHi = R.hi;
            brace.idLo = R.lo;
            stackIndex.push(i);
            stackExpected.push(closerFor(kind));
        } else if (stackIndex.length > 0 && stackExpected[stackExpected.length - 1] === code) {
            const openIndex = stackIndex.pop()!;
            stackExpected.pop();
            const opener = result[openIndex];

            opener.isMatched = true;
            brace.isMatched = true;

            opener.partnerIndex = i;
            brace.partnerIndex = openIndex;

            // A closer sits one level shallower than the body it closes, and its parent is
            // its opener's parent — it is a sibling of its opener, not a child of it.
            brace.depth = opener.depth;
            brace.parentIndex = opener.parentIndex;

            // Either the pair is one entity and the closer inherits everything, or the
            // closer is its own person.
            //
            // Note the closer does NOT hash its own declaring line. A closing brace usually
            // sits alone on one, so there is almost nothing to hash — every '}' in the file
            // would collide on the same near-empty header. Deriving from the opener instead
            // inherits all of its stability while guaranteeing the two halves never land on
            // the same value.
            if (settings.independentBraces) {
                mix(opener.idHi, opener.idLo, CLOSER_SALT.hi, CLOSER_SALT.lo);
                brace.idHi = R.hi;
                brace.idLo = R.lo;
            } else {
                brace.idHi = opener.idHi;
                brace.idLo = opener.idLo;
            }
        } else {
            // A stray closer. Do not unwind the stack — one typo should not recolour every
            // brace below it.
            computeIdentity(text, position, kind, stackIndex.length);
            brace.idHi = R.hi;
            brace.idLo = R.lo;
        }
    }

    // Anything left on the stack never found a partner; isMatched stays false.
    const warnOnDepth = settings.complexityWarningDepth > 0;
    const paletteCount = settings.paletteCount;

    for (let i = 0; i < result.length; i++) {
        const brace = result[i];

        // Depth mode indexes the palette directly. The palette's hues are spaced by the
        // golden angle, so consecutive depths — which are always adjacent on screen — land
        // far apart on the wheel rather than shading into each other.
        brace.colorIndex = settings.colorByDepth
            ? brace.depth % paletteCount
            : toIndex(brace.idHi, brace.idLo, paletteCount);

        brace.traits = rollTraits(
            brace.idHi,
            brace.idLo,
            table,
            settings.questionUnmatched && !brace.isMatched,
            warnOnDepth && brace.depth >= settings.complexityWarningDepth,
        );
    }

    return result;
}

/**
 * The load-bearing decision of the whole extension. Result in {@link R}.
 *
 * Identity is the hash of the text that declares the brace — its line up to the brace,
 * whitespace-normalised, falling back to the nearest preceding non-blank line so that
 * Allman style (an opening brace alone on its own line) resolves to the signature above it.
 *
 * Hashing the position instead would reincarnate every brace below any inserted line.
 * Hashing the enclosed body would change a block's identity as you type inside it. Hashing
 * the declaration is stable across edits elsewhere, stable across sessions and machines,
 * survives a reformat, and means "this method's braces" — which is the joke.
 */
function computeIdentity(text: string, offset: number, kind: BraceKind, depth: number): void {
    hashHeader(text, offset);

    const salt = depth * 31 + kind;
    mix(R.hi, R.lo, Math.floor(salt / 4294967296) >>> 0, salt % 4294967296 >>> 0);
}

/**
 * Hashes the brace's declaring line, walking back over blank ones. Result in {@link R}.
 *
 * Whitespace is dropped entirely rather than collapsed to a single space, which is what
 * makes a colour survive a reformat: re-indenting, wrapping a long signature, or tightening
 * `( )` to `()` all leave the identity untouched. The only collisions this can introduce
 * are between declarations that differ by nothing but whitespace — which are the same
 * declaration.
 */
function hashHeader(text: string, offset: number): void {
    let lineEnd = offset;

    for (let back = 0; back < MAX_HEADER_LOOKBACK_LINES; back++) {
        const lineStart = lineStartAt(text, lineEnd);
        const count = fnv1aNormalized(text, lineStart, lineEnd, MAX_HEADER_LENGTH);
        if (count > 0 || lineStart === 0) {
            return;
        }

        // Step back over the newline onto the previous line.
        lineEnd = lineStart - 1;
        if (lineEnd > 0 && text.charCodeAt(lineEnd) === Ch.LF && text.charCodeAt(lineEnd - 1) === Ch.CR) {
            lineEnd--;
        }

        if (lineEnd <= 0) {
            return;
        }
    }
}

function kindOf(code: number): BraceKind {
    switch (code) {
        case Ch.LBrace:
        case Ch.RBrace:
            return BraceKind.Curly;
        case Ch.LParen:
        case Ch.RParen:
            return BraceKind.Round;
        case Ch.LBracket:
        case Ch.RBracket:
            return BraceKind.Square;
        default:
            return -1 as BraceKind;
    }
}

function isOpener(code: number): boolean {
    return code === Ch.LBrace || code === Ch.LParen || code === Ch.LBracket;
}

function closerFor(kind: BraceKind): number {
    switch (kind) {
        case BraceKind.Curly:
            return Ch.RBrace;
        case BraceKind.Round:
            return Ch.RParen;
        default:
            return Ch.RBracket;
    }
}

function includes(settings: ScanSettings, kind: BraceKind): boolean {
    switch (kind) {
        case BraceKind.Curly:
            return settings.curly;
        case BraceKind.Round:
            return settings.round;
        case BraceKind.Square:
            return settings.square;
        default:
            return false;
    }
}

function lineStartAt(text: string, position: number): number {
    for (let i = position - 1; i >= 0; i--) {
        if (text.charCodeAt(i) === Ch.LF) {
            return i + 1;
        }
    }

    return 0;
}

function skipToLineEnd(text: string, i: number): number {
    while (i < text.length && text.charCodeAt(i) !== Ch.LF) {
        i++;
    }

    return i;
}

function skipBlockComment(text: string, i: number): number {
    while (i + 1 < text.length) {
        if (text.charCodeAt(i) === Ch.Star && text.charCodeAt(i + 1) === Ch.Slash) {
            return i + 2;
        }

        i++;
    }

    return text.length;
}

/**
 * Skips a double-quoted run. Handles raw strings (three or more quotes, any number of
 * lines) and verbatim strings (doubled-quote escapes). A plain string stops at end of line,
 * because an unterminated one must not eat the rest of the file.
 */
function skipQuoted(text: string, i: number, verbatim: boolean): number {
    const n = text.length;

    let quotes = 0;
    while (i + quotes < n && text.charCodeAt(i + quotes) === Ch.Quote) {
        quotes++;
    }

    if (quotes >= 3) {
        let j = i + quotes;
        while (j < n) {
            if (text.charCodeAt(j) === Ch.Quote) {
                let run = 0;
                while (j + run < n && text.charCodeAt(j + run) === Ch.Quote) {
                    run++;
                }

                if (run >= quotes) {
                    return j + run;
                }

                j += run;
            } else {
                j++;
            }
        }

        return n;
    }

    if (quotes === 2) {
        return i + 2;
    }

    let k = i + 1;
    while (k < n) {
        const c = text.charCodeAt(k);

        if (verbatim) {
            if (c === Ch.Quote) {
                if (k + 1 < n && text.charCodeAt(k + 1) === Ch.Quote) {
                    k += 2;
                    continue;
                }

                return k + 1;
            }

            k++;
            continue;
        }

        if (c === Ch.Backslash) {
            k += 2;
            continue;
        }

        if (c === Ch.Quote) {
            return k + 1;
        }

        if (c === Ch.LF) {
            return k;
        }

        k++;
    }

    return n;
}

function skipTemplate(text: string, i: number): number {
    const n = text.length;
    let k = i + 1;

    while (k < n) {
        const c = text.charCodeAt(k);
        if (c === Ch.Backslash) {
            k += 2;
            continue;
        }

        if (c === Ch.Backtick) {
            return k + 1;
        }

        k++;
    }

    return n;
}

/**
 * Returns the index past a single-quoted run, or -1 if it does not close on this line. The
 * -1 case matters: Rust lifetimes and stray apostrophes look exactly like an opening quote,
 * and treating them as one would blank the rest of the file.
 */
function trySkipSingleQuoted(text: string, i: number): number {
    const n = text.length;
    let k = i + 1;

    while (k < n) {
        const c = text.charCodeAt(k);
        if (c === Ch.Backslash) {
            k += 2;
            continue;
        }

        if (c === Ch.LF) {
            return -1;
        }

        if (c === Ch.Apostrophe) {
            return k + 1;
        }

        k++;
    }

    return -1;
}
