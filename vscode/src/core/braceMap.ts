import { BraceInfo } from './braceInfo';

/** A matched pair, as indices into the map. */
export interface BracePair {
    openIndex: number;
    closeIndex: number;
}

/** Every brace in one version of a document, ordered by position. */
export class BraceMap {
    static readonly empty = new BraceMap([]);

    constructor(readonly braces: readonly BraceInfo[]) {}

    get count(): number {
        return this.braces.length;
    }

    at(index: number): BraceInfo {
        return this.braces[index];
    }

    /**
     * Index of the first brace at or after `position`, or {@link count} if there is none.
     * Binary search: the renderer asks this once per visible range and then walks forward.
     */
    firstIndexAtOrAfter(position: number): number {
        let lo = 0;
        let hi = this.braces.length;

        while (lo < hi) {
            const mid = (lo + hi) >>> 1;
            if (this.braces[mid].position < position) {
                lo = mid + 1;
            } else {
                hi = mid;
            }
        }

        return lo;
    }

    /** Index of the brace exactly at `position`, or -1. */
    indexAt(position: number): number {
        const i = this.firstIndexAtOrAfter(position);
        return i < this.braces.length && this.braces[i].position === position ? i : -1;
    }

    /**
     * The innermost matched pair whose span contains `position`, with both brace characters
     * counting as inside it.
     *
     * Runs in time proportional to the *nesting depth*, not the file: one binary search to
     * find the brace the caret is sitting against, then a walk up `parentIndex`. The caret
     * moves on every keystroke and every arrow key, so a scan back through a quarter of a
     * million braces would be felt.
     */
    enclosingPair(position: number): BracePair | null {
        const j = this.firstIndexAtOrAfter(position);
        if (j >= this.braces.length) {
            // Nothing at or after the caret, so no pair can still be open across it.
            return null;
        }

        const next = this.braces[j];

        // The first brace at or after the caret is a closer: the caret is inside the block
        // that closer ends, because its opener necessarily precedes the caret. Likewise a
        // brace sitting exactly under the caret belongs to the pair the caret is on, which
        // is what makes clicking a brace spotlight its own pair rather than its parent.
        if (next.isMatched && (!next.isOpen || next.position === position)) {
            const openIndex = next.isOpen ? j : next.partnerIndex;
            const closeIndex = next.isOpen ? next.partnerIndex : j;
            return openIndex >= 0 && closeIndex >= 0 ? { openIndex, closeIndex } : null;
        }

        return this.walkOut(next.parentIndex);
    }

    /** The next matched pair outward, for peeling off one nesting level at a time. */
    parentPair(openIndex: number): BracePair | null {
        if (openIndex < 0 || openIndex >= this.braces.length) {
            return null;
        }

        return this.walkOut(this.braces[openIndex].parentIndex);
    }

    /**
     * Follows the parent chain outward until it reaches an opener that actually found a
     * partner.
     *
     * An opener that never closed stays on the scanner's stack for the rest of the file, so
     * it is the recorded parent of everything below it while not being a pair at all.
     * Skipping those is what keeps one unbalanced brace from erasing the spotlight for the
     * whole file below it.
     */
    private walkOut(index: number): BracePair | null {
        while (index >= 0 && index < this.braces.length) {
            const candidate = this.braces[index];
            if (candidate.isMatched && candidate.partnerIndex >= 0) {
                return { openIndex: index, closeIndex: candidate.partnerIndex };
            }

            index = candidate.parentIndex;
        }

        return null;
    }
}
