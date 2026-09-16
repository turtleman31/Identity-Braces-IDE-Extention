import * as assert from 'node:assert/strict';
import { test } from 'node:test';
import { fnv1a, mix, R, toIndex, toUnitInterval, u64 } from '../core/hash';

/**
 * The 64-bit arithmetic underneath everything else.
 *
 * JavaScript has no 64-bit integers, so this is hand-rolled on pairs of 32-bit halves. The
 * risk is not that it is wrong in an obvious way — the parity test would catch that — but
 * that it is wrong only in the top bits, or only on carry, where the failure would look like
 * a mildly uneven colour distribution rather than a broken hash.
 */

test('FNV-1a matches the published vectors', () => {
    // The standard 64-bit FNV-1a test vectors, which is what the C# implements.
    const cases: [string, string][] = [
        ['', 'cbf29ce484222325'],
        ['a', 'af63dc4c8601ec8c'],
        ['foobar', '85944171f73967e8'],
    ];

    for (const [input, expected] of cases) {
        const h = fnv1a(input);
        assert.equal(hex(h.hi, h.lo), expected, `fnv1a(${JSON.stringify(input)})`);
    }
});

test('mix avalanches and stays inside 64 bits', () => {
    const salt = u64('0x9E3779B97F4A7C15');

    mix(0, 1, salt.hi, salt.lo);
    const a = { hi: R.hi, lo: R.lo };

    mix(0, 2, salt.hi, salt.lo);
    const b = { hi: R.hi, lo: R.lo };

    assert.ok(a.hi >= 0 && a.hi <= 0xffffffff && a.lo >= 0 && a.lo <= 0xffffffff, 'halves stay unsigned');
    assert.notEqual(hex(a.hi, a.lo), hex(b.hi, b.lo));

    // One bit of input difference should move roughly half the output bits. Anything under a
    // quarter means the finaliser is not finalising.
    const differing = countDifferingBits(a, b);
    assert.ok(differing > 16 && differing < 48, `${differing} of 64 bits differ`);
});

test('mix is deterministic', () => {
    const salt = u64('0x5EA1EDFA7ECAFE');
    mix(0x12345678, 0x9abcdef0, salt.hi, salt.lo);
    const first = hex(R.hi, R.lo);

    mix(0x12345678, 0x9abcdef0, salt.hi, salt.lo);
    assert.equal(hex(R.hi, R.lo), first);
});

test('toUnitInterval covers [0, 1) without losing the top bits', () => {
    assert.equal(toUnitInterval(0, 0), 0);
    assert.ok(toUnitInterval(0xffffffff, 0xffffffff) < 1);
    assert.ok(toUnitInterval(0xffffffff, 0xffffffff) > 0.9999999);

    // The high half must actually reach the result: a bug that dropped it would make every
    // roll depend on the low 32 bits alone, which is invisible until two braces collide.
    assert.notEqual(toUnitInterval(0x80000000, 0), toUnitInterval(0, 0));
    assert.ok(Math.abs(toUnitInterval(0x80000000, 0) - 0.5) < 1e-12);
});

test('toIndex is a true modulo of the full 64-bit value', () => {
    // 2^32 mod 7 is 4, so this is the case that catches an implementation that only reduces
    // the low half.
    assert.equal(toIndex(1, 0, 7), 4);
    assert.equal(toIndex(0, 33, 32), 1);
    assert.equal(toIndex(0xffffffff, 0xffffffff, 32), 31);

    for (let i = 0; i < 64; i++) {
        const index = toIndex(i * 2654435761, i, 32);
        assert.ok(index >= 0 && index < 32);
    }
});

test('u64 parses a hex literal the way the C# writes it', () => {
    assert.deepEqual(u64('0xB0D1E5A17C0FFEE'), { hi: 0x0b0d1e5a, lo: 0x17c0ffee });
    assert.deepEqual(u64('0xED17BE7F'), { hi: 0, lo: 0xed17be7f });
    assert.deepEqual(u64('0xCBF29CE484222325'), { hi: 0xcbf29ce4, lo: 0x84222325 });
});

function hex(hi: number, lo: number): string {
    return `${(hi >>> 0).toString(16).padStart(8, '0')}${(lo >>> 0).toString(16).padStart(8, '0')}`;
}

function countDifferingBits(a: { hi: number; lo: number }, b: { hi: number; lo: number }): number {
    return popcount(a.hi ^ b.hi) + popcount(a.lo ^ b.lo);
}

function popcount(value: number): number {
    let count = 0;
    for (let v = value >>> 0; v !== 0; v >>>= 1) {
        count += v & 1;
    }

    return count;
}
