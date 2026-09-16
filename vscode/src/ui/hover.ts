import * as vscode from 'vscode';
import { BraceInfo } from '../core/braceInfo';
import { nameOf } from '../core/braceNames';
import { findTrait } from '../core/traitCatalog';
import { TraitIds } from '../core/traitIds';
import { Decorator } from '../decorator';
import { colorFor } from '../render/braceStyle';

/**
 * Introduces a named brace on hover: *Sir Reginald the Unclosed*.
 *
 * A hover provider rather than anything attached to the decoration, because the drawn
 * overlay sits above the text — making it hit-testable would have it swallow the clicks that
 * place your caret.
 */
export function registerHover(decorator: Decorator): vscode.Disposable {
    return vscode.languages.registerHoverProvider(
        { scheme: '*', language: '*' },
        {
            provideHover(document, position) {
                const brace = braceAt(decorator, document, position);
                if (!brace || !brace.traits.effects.includes(TraitIds.Named)) {
                    return undefined;
                }

                return new vscode.Hover(nameOf(brace.idHi, brace.idLo));
            },
        },
    );
}

/**
 * The full dossier on one brace, for the identify command.
 *
 * Everything here is derived from the identity hash, so two people running the command on
 * the same brace in the same file get the same answer — including in the other editor.
 */
export function describe(decorator: Decorator, document: vscode.TextDocument, position: vscode.Position): string | null {
    const brace = braceAt(decorator, document, position);
    if (!brace) {
        return null;
    }

    const settings = decorator.currentSettings;
    const traits = brace.traits;

    const rolled = [
        label(traits.body),
        label(traits.creature),
        label(traits.costume),
        label(traits.motion),
        ...traits.effects.map(label),
    ].filter((t): t is string => t !== null);

    const parts = [
        `${brace.character}  ${nameOf(brace.idHi, brace.idLo)}`,
        `identity ${hex(brace.idHi, brace.idLo)}`,
        `colour ${colorFor(brace, settings)} (${brace.colorIndex})`,
        `depth ${brace.depth}`,
        brace.isMatched ? 'matched' : 'unmatched',
        rolled.length > 0 ? `traits: ${rolled.join(', ')}` : 'traits: plain',
    ];

    return parts.join('  ·  ');
}

function braceAt(
    decorator: Decorator,
    document: vscode.TextDocument,
    position: vscode.Position,
): BraceInfo | null {
    const map = decorator.mapFor(document);
    if (!map) {
        return null;
    }

    // A hover lands between characters, so both the brace under the caret and the one just
    // before it are fair game — which is what makes hovering the glyph itself work.
    const offset = document.offsetAt(position);
    const index = map.indexAt(offset);
    if (index >= 0) {
        return map.at(index);
    }

    const before = map.indexAt(offset - 1);
    return before >= 0 ? map.at(before) : null;
}

function label(id: string | null): string | null {
    if (id === null) {
        return null;
    }

    return findTrait(id)?.name ?? id;
}

function hex(hi: number, lo: number): string {
    return `${(hi >>> 0).toString(16).padStart(8, '0')}${(lo >>> 0).toString(16).padStart(8, '0')}`;
}
