import * as assert from 'node:assert/strict';
import { test } from 'node:test';
import { BraceInfo } from '../core/braceInfo';
import { scanBraces } from '../core/braceScanner';
import { defaultScanSettings, ScanSettings } from '../core/scanSettings';
import { TraitIds } from '../core/traitIds';

/**
 * The lexer, ported from the Visual Studio extension's own suite.
 *
 * Every approximation here is meant to fail in the safe direction: skipping a brace we could
 * have coloured rather than colouring one inside a string. The tests are written to catch a
 * change of direction, not just a change of answer.
 */

function scan(text: string, overrides: Partial<ScanSettings> = {}): BraceInfo[] {
    return scanBraces(text, { ...defaultScanSettings(), ...overrides });
}

function at(braces: BraceInfo[], text: string, needle: string): BraceInfo {
    const offset = text.indexOf(needle);
    const found = braces.find((b) => b.position === offset);
    assert.ok(found, `no brace at "${needle}"`);
    return found;
}

test('braces inside a string are ignored', () => {
    const text = 'var s = "{ not a brace }"; { real }';
    const braces = scan(text);
    assert.equal(braces.length, 2, 'only the real pair is found');
    assert.equal(braces[0].position, text.indexOf('{ real'));
});

test('braces inside a line comment are ignored', () => {
    const braces = scan('// { comment brace }\n{ real }');
    assert.equal(braces.length, 2);
});

test('braces inside a block comment are ignored', () => {
    const braces = scan('/* { block } */ { real }');
    assert.equal(braces.length, 2);
});

test('a verbatim string spans lines', () => {
    const braces = scan('var v = @"line one {\nline two }"; { real }');
    assert.equal(braces.length, 2);
});

test('a raw string spans lines', () => {
    const braces = scan('var r = """\n raw { body }\n"""; { real }');
    assert.equal(braces.length, 2);
});

test('a template literal is opaque', () => {
    const braces = scan('var t = `tpl { hole }`; { real }');
    assert.equal(braces.length, 2);
});

test('a char literal is skipped', () => {
    const braces = scan("var c = '{'; { real }");
    assert.equal(braces.length, 2);
});

test('a lifetime does not swallow the rest of the file', () => {
    // The hazard: `'a` looks exactly like an opening quote, and treating it as one would
    // blank every brace after it. A single-quoted run that does not close on its own line is
    // therefore not a string at all.
    const text = "impl Foo {\n  fn a(x: &'a str) { bar(); }\n}";
    const braces = scan(text);

    assert.equal(braces.length, 8, 'two curly pairs and two paren pairs');
    assert.equal(braces[braces.length - 1].position, text.lastIndexOf('}'), 'the last brace is still found');
    assert.ok(braces.every((b) => b.isMatched), 'and everything still pairs up');
});

test('an unterminated string stops at end of line', () => {
    const braces = scan('var s = "unterminated { \n{ real }');
    assert.equal(braces.length, 2);
});

test('an interpolation hole is treated as string content', () => {
    // Deliberate: the brace inside `$"{x}"` is not coloured. Failing this way costs a colour;
    // failing the other way colours the middle of a string literal.
    const braces = scan('var s = $"a {x} b"; { real }');
    assert.equal(braces.length, 2);
});

test('pairs are matched and both halves know each other', () => {
    const text = 'void F() { if (x) { g(); } }';
    const braces = scan(text);

    assert.ok(braces.every((b) => b.isMatched), 'every brace found a partner');

    const outer = at(braces, text, '{ if');
    assert.equal(braces[outer.partnerIndex].position, text.lastIndexOf('}'));
    assert.equal(braces[braces[outer.partnerIndex].partnerIndex].position, outer.position);
});

test('an unmatched opener is flagged and questions itself', () => {
    const text = 'void F() { g(); ';
    const braces = scan(text);
    const open = at(braces, text, '{');

    assert.equal(open.isMatched, false);
    assert.equal(open.traits.body, TraitIds.Question, 'an unmatched brace questions itself');
});

test('a stray closer does not cascade', () => {
    // One typo must not recolour every brace below it, so the scan does not unwind its stack
    // on a closer it was not expecting.
    const withStray = scan('void A() { }\n}\nvoid B() { }');
    const without = scan('void A() { }\nvoid B() { }');

    const strayB = withStray[withStray.length - 1];
    const cleanB = without[without.length - 1];

    assert.equal(strayB.idHi, cleanB.idHi);
    assert.equal(strayB.idLo, cleanB.idLo);
    assert.equal(strayB.colorIndex, cleanB.colorIndex);
});

test('a closer reports its opener s depth, so a pair agrees with itself', () => {
    const text = 'void F()\n{\n    if (a)\n    {\n        g();\n    }\n}';
    const braces = scan(text, { round: false });

    for (const brace of braces) {
        if (brace.isMatched && !brace.isOpen) {
            assert.equal(brace.depth, braces[brace.partnerIndex].depth);
        }
    }
});

test('depth colouring gives a pair one colour and neighbours different ones', () => {
    const text = 'void F()\n{\n    if (a)\n    {\n        g();\n    }\n}';
    const braces = scan(text, { round: false, colorByDepth: true });

    const outer = at(braces, text, '{');
    const inner = braces.find((b) => b.isOpen && b.depth === 1)!;

    assert.equal(outer.colorIndex, braces[outer.partnerIndex].colorIndex, 'a pair agrees');
    assert.notEqual(outer.colorIndex, inner.colorIndex, 'neighbouring depths differ');
});

test('brace kinds can be switched off independently', () => {
    const text = 'void F(int a) { b[0]; }';
    assert.equal(scan(text, { round: false, square: false }).length, 2);
    assert.equal(scan(text, { curly: false, square: false }).length, 2);
    assert.equal(scan(text, { curly: false, round: false }).length, 2);
});

test('the complexity warning is a predicate, not a roll', () => {
    const text = 'a { b { c { d { e } } } }';
    const braces = scan(text, { complexityWarningDepth: 2 });

    for (const brace of braces) {
        const distressed = brace.traits.effects.includes(TraitIds.Distressed);
        assert.equal(distressed, brace.depth >= 2, `depth ${brace.depth}`);
    }
});
