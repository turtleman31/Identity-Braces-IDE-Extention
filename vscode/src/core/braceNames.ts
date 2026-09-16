import { rollIndex, roll, u64, U64 } from './hash';

/**
 * Gives a brace a name, derived from its identity.
 *
 * Stable for the life of the brace, and stable across machines and editors, because it is a
 * pure function of the same identity hash that decides the brace's colour and traits. Two
 * people looking at the same file agree about which one is Reginald.
 *
 * Built from three independent draws rather than one list of names, so the catalogue is a
 * few dozen words instead of thousands and still rarely repeats: with 24 titles, 32 given
 * names and 24 epithets there are around eighteen thousand combinations, and a file with a
 * hundred named braces has a coin-flip's chance of any collision at all.
 */

const TITLE_SALT = u64('0x717DE5A17');
const GIVEN_SALT = u64('0x91BE0DA17');
const EPITHET_SALT = u64('0xED17BE7F');
const SHAPE_SALT = u64('0x5AAFE5A17');

const TITLES = [
    'Sir', 'Dame', 'Captain', 'Doctor', 'Professor', 'Admiral', 'Baron', 'Duchess',
    'Chief', 'Judge', 'Marshal', 'Bishop', 'Colonel', 'Commodore', 'Vicar', 'Warden',
    'Lord', 'Lady', 'Sergeant', 'Inspector', 'Abbot', 'Regent', 'Consul', 'Steward',
];

const GIVEN = [
    'Reginald', 'Beatrice', 'Mortimer', 'Winifred', 'Cuthbert', 'Prudence', 'Barnaby',
    'Millicent', 'Horace', 'Agatha', 'Percival', 'Edwina', 'Clarence', 'Hyacinth',
    'Ambrose', 'Theodora', 'Gideon', 'Rosalind', 'Alistair', 'Cordelia', 'Bartholomew',
    'Philippa', 'Montgomery', 'Griselda', 'Rupert', 'Josephine', 'Egbert', 'Marigold',
    'Fitzwilliam', 'Henrietta', 'Archibald', 'Wilhelmina',
];

const EPITHETS = [
    'the Unclosed', 'the Patient', 'the Unmatched', 'of the Third Nesting',
    'the Indented', 'the Verbose', 'the Deprecated', 'the Refactored', 'the Unreachable',
    'the Well-Formed', 'the Dangling', 'the Terse', 'the Overloaded', 'the Immutable',
    'the Recursive', 'the Deeply Nested', 'the Uncommented', 'the Legacy',
    'the Escaped', 'the Trailing', 'the Orphaned', 'the Idempotent', 'the Volatile',
    'the Considered Harmful',
];

/**
 * This brace's name.
 *
 * Three shapes, so a screenful is not a wall of identically-structured names: some braces
 * get a title, some an epithet, some both, some just a name. The shape is drawn from the
 * identity too, so it is as stable as the rest of it.
 */
export function nameOf(hi: number, lo: number): string {
    const title = pick(TITLES, hi, lo, TITLE_SALT);
    const given = pick(GIVEN, hi, lo, GIVEN_SALT);
    const epithet = pick(EPITHETS, hi, lo, EPITHET_SALT);

    switch (Math.trunc(roll(hi, lo, SHAPE_SALT) * 4) % 4) {
        case 0:
            return given;
        case 1:
            return `${title} ${given}`;
        case 2:
            return `${given} ${epithet}`;
        default:
            return `${title} ${given} ${epithet}`;
    }
}

function pick(from: readonly string[], hi: number, lo: number, salt: U64): string {
    return from[rollIndex(hi, lo, salt, from.length)];
}
