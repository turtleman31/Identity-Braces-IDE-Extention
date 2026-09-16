/**
 * Deterministic 64-bit hashing, bit-identical to the Visual Studio extension's
 * `IdentityBraces.Core.Hash`.
 *
 * Two things are load-bearing here.
 *
 * The first is that it is not JavaScript's own string hashing or anything derived from
 * `Math.random`: a brace's identity has to outlive the session, the machine and the editor.
 * The same file opened in VS Code and in Visual Studio must hand the same brace the same
 * colour and the same personality, which means the same FNV-1a and the same SplitMix64
 * finaliser, down to the wrapping arithmetic.
 *
 * The second is that JavaScript has no 64-bit integers and `BigInt` is far too slow for
 * this — a 1&nbsp;MB file is a quarter of a million braces and a few million multiplies.
 * Everything below therefore works on a pair of 32-bit halves, and results land in the
 * shared {@link R} scratch rather than in a fresh object, so a whole scan allocates nothing.
 */

/** A 64-bit unsigned value, split into two 32-bit halves. */
export interface U64 {
    readonly hi: number;
    readonly lo: number;
}

/**
 * The result of the last 64-bit operation.
 *
 * Shared and mutable on purpose: the alternative is one small object per multiply, and the
 * scan does millions of them. Read it immediately, never hold onto it.
 */
export const R = { hi: 0, lo: 0 };

/** Splits a hex literal — written exactly as it appears in the C# — into halves. */
export function u64(hex: string): U64 {
    const digits = hex.replace(/^0x/i, '').padStart(16, '0');
    return {
        hi: parseInt(digits.slice(0, 8), 16) >>> 0,
        lo: parseInt(digits.slice(8), 16) >>> 0,
    };
}

const FNV_OFFSET_BASIS = u64('0xCBF29CE484222325');
const FNV_PRIME = u64('0x100000001B3');
const GOLDEN = u64('0x9E3779B97F4A7C15');
const SPLITMIX_A = u64('0xBF58476D1CE4E5B9');
const SPLITMIX_B = u64('0x94D049BB133111EB');

/**
 * 64-bit multiply, into {@link R}.
 *
 * Sixteen bits at a time, because the products of 32-bit halves do not fit in a double
 * without losing the low bits — which is exactly the half the hash depends on.
 */
function mul(aHi: number, aLo: number, bHi: number, bLo: number): void {
    const a00 = aLo & 0xffff;
    const a16 = aLo >>> 16;
    const a32 = aHi & 0xffff;
    const a48 = aHi >>> 16;

    const b00 = bLo & 0xffff;
    const b16 = bLo >>> 16;
    const b32 = bHi & 0xffff;
    const b48 = bHi >>> 16;

    let c00 = a00 * b00;
    let c16 = c00 >>> 16;
    c00 &= 0xffff;

    c16 += a16 * b00;
    let c32 = c16 >>> 16;
    c16 &= 0xffff;

    c16 += a00 * b16;
    c32 += c16 >>> 16;
    c16 &= 0xffff;

    c32 += a32 * b00;
    let c48 = c32 >>> 16;
    c32 &= 0xffff;

    c32 += a16 * b16;
    c48 += c32 >>> 16;
    c32 &= 0xffff;

    c32 += a00 * b32;
    c48 += c32 >>> 16;
    c32 &= 0xffff;

    c48 += a48 * b00 + a32 * b16 + a16 * b32 + a00 * b48;
    c48 &= 0xffff;

    R.hi = ((c48 << 16) | c32) >>> 0;
    R.lo = ((c16 << 16) | c00) >>> 0;
}

/** 64-bit add, into {@link R}. */
function add(aHi: number, aLo: number, bHi: number, bLo: number): void {
    const lo = (aLo >>> 0) + (bLo >>> 0);
    R.lo = lo >>> 0;
    R.hi = (aHi + bHi + (lo > 0xffffffff ? 1 : 0)) >>> 0;
}

/** `value ^ (value >>> shift)`, into {@link R}. Shift is always under 32 here. */
function xorShiftRight(hi: number, lo: number, shift: number): void {
    const sHi = hi >>> shift;
    const sLo = shift === 0 ? lo : ((hi << (32 - shift)) | (lo >>> shift)) >>> 0;
    R.hi = (hi ^ sHi) >>> 0;
    R.lo = (lo ^ sLo) >>> 0;
}

/**
 * FNV-1a over the UTF-16 code units of `text[start, end)`, skipping whitespace, into
 * {@link R}. Returns how many code units were actually folded in.
 *
 * Hashing straight out of the buffer rather than building the normalised string first is
 * what keeps a full-file scan allocation-free; `limit` reproduces the C# builder's cap.
 */
export function fnv1aNormalized(text: string, start: number, end: number, limit: number): number {
    let hi = FNV_OFFSET_BASIS.hi;
    let lo = FNV_OFFSET_BASIS.lo;
    let count = 0;

    for (let i = start; i < end && count < limit; i++) {
        const c = text.charCodeAt(i);
        if (c === 32 || c === 9 || c === 13 || c === 10) {
            continue;
        }

        count++;
        lo = (lo ^ c) >>> 0;
        mul(hi, lo, FNV_PRIME.hi, FNV_PRIME.lo);
        hi = R.hi;
        lo = R.lo;
    }

    R.hi = hi;
    R.lo = lo;
    return count;
}

/** FNV-1a over a whole string, for callers outside the scanner's hot path. */
export function fnv1a(value: string): U64 {
    fnv1aNormalizedUnfiltered(value);
    return { hi: R.hi, lo: R.lo };
}

function fnv1aNormalizedUnfiltered(value: string): void {
    let hi = FNV_OFFSET_BASIS.hi;
    let lo = FNV_OFFSET_BASIS.lo;

    for (let i = 0; i < value.length; i++) {
        lo = (lo ^ value.charCodeAt(i)) >>> 0;
        mul(hi, lo, FNV_PRIME.hi, FNV_PRIME.lo);
        hi = R.hi;
        lo = R.lo;
    }

    R.hi = hi;
    R.lo = lo;
}

/**
 * SplitMix64 finaliser. Folds `salt` in and avalanches the result, so two hashes that
 * differ in one bit land far apart. Result in {@link R}.
 */
export function mix(hi: number, lo: number, saltHi: number, saltLo: number): void {
    mul(saltHi, saltLo, GOLDEN.hi, GOLDEN.lo);
    add(hi, lo, R.hi, R.lo);

    xorShiftRight(R.hi, R.lo, 30);
    mul(R.hi, R.lo, SPLITMIX_A.hi, SPLITMIX_A.lo);

    xorShiftRight(R.hi, R.lo, 27);
    mul(R.hi, R.lo, SPLITMIX_B.hi, SPLITMIX_B.lo);

    xorShiftRight(R.hi, R.lo, 31);
}

/** {@link mix} with the salt as a {@link U64}. */
export function mixBy(hi: number, lo: number, salt: U64): void {
    mix(hi, lo, salt.hi, salt.lo);
}

/** Maps a hash onto [0, 1) using the 53 bits a double can hold exactly. */
export function toUnitInterval(hi: number, lo: number): number {
    return (hi * 2097152 + (lo >>> 11)) * (1.0 / 9007199254740992.0);
}

/** Maps a hash onto [0, count). */
export function toIndex(hi: number, lo: number, count: number): number {
    // hi * 2^32 + lo, modulo count, without ever forming the 64-bit value.
    return ((hi % count) * (4294967296 % count) + (lo % count)) % count;
}

/** A stable value in [0, 1) drawn from an identity and a salt. Allocation-free. */
export function roll(hi: number, lo: number, salt: U64): number {
    mix(hi, lo, salt.hi, salt.lo);
    return toUnitInterval(R.hi, R.lo);
}

/** A stable index into a list of `count`, drawn from an identity and a salt. */
export function rollIndex(hi: number, lo: number, salt: U64, count: number): number {
    mix(hi, lo, salt.hi, salt.lo);
    return toIndex(R.hi, R.lo, count);
}
