import * as assert from 'node:assert/strict';
import { test } from 'node:test';
import { BraceInfo } from '../core/braceInfo';
import { scanBraces } from '../core/braceScanner';
import { PALETTE_COUNT } from '../core/palette';
import { defaultScanSettings, ScanSettings } from '../core/scanSettings';

/**
 * Identity stability — the whole point.
 *
 * Get this wrong and every keystroke reshuffles every colour, and the extension is unusable
 * within about four seconds. Identity is the hash of the brace's *declaring line*, which is
 * what makes it survive edits elsewhere, survive a reformat, and change only when the thing
 * it belongs to changes.
 */

function scan(text: string, overrides: Partial<ScanSettings> = {}): BraceInfo[] {
    return scanBraces(text, { ...defaultScanSettings(), ...overrides });
}

function firstOpener(text: string, overrides: Partial<ScanSettings> = {}): BraceInfo {
    const brace = scan(text, overrides).find((b) => b.isOpen && b.kind === 0);
    assert.ok(brace, 'no opening curly brace found');
    return brace;
}

function sameIdentity(a: BraceInfo, b: BraceInfo): boolean {
    return a.idHi === b.idHi && a.idLo === b.idLo;
}

test('an Allman brace resolves to the signature above it', () => {
    // A brace alone on its own line has nothing to hash, so it walks back to the nearest
    // preceding non-blank line — which is the declaration it belongs to.
    const allman = firstOpener('public void Foo()\n{\n    Bar();\n}');
    const kAndR = firstOpener('public void Foo() {\n    Bar();\n}');

    assert.ok(sameIdentity(allman, kAndR), 'both brace styles give the same method the same brace');
});

test('identity survives an insertion above', () => {
    const before = firstOpener('void Alpha()\n{\n  A();\n}');
    const after = firstOpener('using System;\n\nvoid Alpha()\n{\n  A();\n}');

    assert.ok(sameIdentity(before, after));
    assert.notEqual(before.position, after.position, 'the brace really did move');
});

test('identity survives an edit inside the block', () => {
    const before = firstOpener('void Alpha()\n{\n  A();\n}');
    const after = firstOpener('void Alpha()\n{\n  A();\n  B();\n  C();\n}');

    assert.ok(sameIdentity(before, after));
});

test('identity survives a reformat', () => {
    // Whitespace is dropped entirely rather than collapsed, so re-indenting, wrapping a long
    // signature and tightening `( )` to `()` all leave the identity alone.
    const before = firstOpener('void Alpha()\n{\n  A();\n}');
    const after = firstOpener('void   Alpha( )\n{\n\tA();\n}');

    assert.ok(sameIdentity(before, after));
});

test('identity changes with a rename', () => {
    const alpha = firstOpener('void Alpha()\n{\n  A();\n}');
    const gamma = firstOpener('void Gamma()\n{\n  A();\n}');

    assert.ok(!sameIdentity(alpha, gamma), 'it is a different method now');
});

test('a closer does not inherit its opener when braces are independent', () => {
    const text = 'void F() { if (x) { g(); } }';
    const braces = scan(text, { round: false });

    for (const brace of braces) {
        if (brace.isOpen && brace.isMatched) {
            assert.ok(!sameIdentity(brace, braces[brace.partnerIndex]), 'the halves disagree');
            assert.ok(braces[brace.partnerIndex].isMatched, 'they are still a pair');
        }
    }
});

test('turning independence off restores a shared identity', () => {
    const text = 'void F() { g(); }';
    const braces = scan(text, { round: false, independentBraces: false });

    assert.equal(braces.length, 2);
    assert.ok(sameIdentity(braces[0], braces[1]));
    assert.equal(braces[0].colorIndex, braces[1].colorIndex);
});

test('a closer identity is as stable as its opener s', () => {
    const closerOf = (text: string) => {
        const braces = scan(text, { round: false });
        const opener = braces.find((b) => b.isOpen && b.isMatched)!;
        return braces[opener.partnerIndex];
    };

    const before = closerOf('void Alpha()\n{\n  A();\n}');
    const inserted = closerOf('using System;\n\nvoid Alpha()\n{\n  A();\n}');
    const reformatted = closerOf('void   Alpha( )\n{\n\tA();\n}');
    const renamed = closerOf('void Gamma()\n{\n  A();\n}');

    assert.ok(sameIdentity(before, inserted), 'survives an insertion above');
    assert.ok(sameIdentity(before, reformatted), 'survives a reformat');
    assert.ok(!sameIdentity(before, renamed), 'changes with a rename');
});

test('stacked closers all differ from one another', () => {
    // A `}` usually sits alone on its line, so hashing its own declaring text would collide
    // every closer in the file onto the same near-empty header.
    const text = 'void F()\n{\n    if (a)\n    {\n        if (b)\n        {\n            g();\n        }\n    }\n}';
    const closers = scan(text, { round: false }).filter((b) => !b.isOpen);

    const seen = new Set(closers.map((b) => `${b.idHi}:${b.idLo}`));
    assert.equal(seen.size, closers.length, `${closers.length} closers, ${seen.size} identities`);
});

test('colours and personalities are spread across the palette', () => {
    const source = Array.from({ length: 400 }, (_, i) => `void Method${i}()\n{\n    Body${i}();\n}`).join('\n\n');
    const braces = scan(source, { round: false });

    const counts = new Array<number>(PALETTE_COUNT).fill(0);
    for (const brace of braces) {
        counts[brace.colorIndex]++;
    }

    assert.ok(counts.every((c) => c > 0), 'every palette entry is used at least once');

    const drawn = braces.filter((b) => b.traits.isDrawn).length / braces.length;
    // The shipped defaults are 6% question, 8% cycle, 4% cat, 4% stockings, rolled on
    // independent layers — call it a fifth of all braces, and allow a wide band because this
    // is a distribution, not an arithmetic identity.
    assert.ok(drawn > 0.1 && drawn < 0.35, `${(drawn * 100).toFixed(1)}% of braces are drawn`);
});

test('the scan is a pure function of its input', () => {
    const text = 'void F()\n{\n    if (a) { g(); }\n}';
    const first = scan(text);
    const second = scan(text);

    assert.deepEqual(first, second);
});

test('a large file scans fast enough to run on every keystroke', () => {
    // The Visual Studio extension does 1.17 MB in about 18 ms. JavaScript is not going to
    // match that, but a whole-file rescan still has to fit inside a keystroke — hence the
    // debounce in the decorator and the maxFileLength guard behind it.
    const block = 'public void Method()\n{\n    if (a) { for (int i = 0; i < n; i++) { work(i); } }\n}\n\n';
    const source = block.repeat(6000);

    const started = process.hrtime.bigint();
    const braces = scan(source);
    const elapsedMs = Number(process.hrtime.bigint() - started) / 1e6;

    assert.ok(braces.length > 50000, `${braces.length} braces`);
    assert.ok(elapsedMs < 1500, `scanned ${source.length} characters in ${elapsedMs.toFixed(0)} ms`);
});
