package identitybraces.core

/** A matched opener and its closer, as indices into a [BraceMap]. */
class BracePair(val openIndex: Int, val closeIndex: Int)

/** Every brace in one document snapshot, ordered by position. */
class BraceMap(private val braces: Array<BraceInfo>) {
    val size: Int
        get() = braces.size

    operator fun get(index: Int): BraceInfo = braces[index]

    /**
     * Index of the first brace at or after [position], or [size] if there is none. Binary
     * search: the painter asks this once per visible span and then walks forward.
     */
    fun firstIndexAtOrAfter(position: Int): Int {
        var lo = 0
        var hi = braces.size

        while (lo < hi) {
            val mid = lo + ((hi - lo) shr 1)
            if (braces[mid].position < position) {
                lo = mid + 1
            } else {
                hi = mid
            }
        }

        return lo
    }

    /** Every brace whose position lies in [start, end), in order. */
    inline fun forEachIn(start: Int, end: Int, action: (index: Int, brace: BraceInfo) -> Unit) {
        var i = firstIndexAtOrAfter(start)
        while (i < size) {
            val brace = this[i]
            if (brace.position >= end) {
                return
            }

            action(i, brace)
            i++
        }
    }

    /**
     * The innermost matched pair whose span contains [position], with both brace characters
     * counting as inside it, or null.
     *
     * Runs in time proportional to the *nesting depth*, not the file: one binary search to
     * find the brace the caret is sitting against, then a walk up [BraceInfo.parentIndex].
     * The caret moves on every keystroke and every arrow key, so a scan back through a
     * quarter of a million braces would be felt.
     */
    fun enclosingPair(position: Int): BracePair? {
        val j = firstIndexAtOrAfter(position)
        if (j >= braces.size) {
            // Nothing at or after the caret, so no pair can still be open across it.
            return null
        }

        val next = braces[j]

        // The first brace at or after the caret is a closer: the caret is inside the block
        // that closer ends, because its opener necessarily precedes the caret. Likewise a
        // brace sitting exactly under the caret belongs to the pair the caret is on, which is
        // what makes clicking a brace select its own pair rather than its parent.
        if (next.isMatched && (!next.isOpen || next.position == position)) {
            val open = if (next.isOpen) j else next.partnerIndex
            val close = if (next.isOpen) next.partnerIndex else j
            return if (open >= 0 && close >= 0) BracePair(open, close) else null
        }

        return walkOut(next.parentIndex)
    }

    /** The next matched pair outward from [openIndex], for peeling off one nesting level at a time. */
    fun parentPair(openIndex: Int): BracePair? {
        if (openIndex < 0 || openIndex >= braces.size) {
            return null
        }

        return walkOut(braces[openIndex].parentIndex)
    }

    /**
     * Follows the parent chain outward until it reaches an opener that actually found a
     * partner. An opener that never closed stays on the scanner's stack for the rest of the
     * file, so it is the recorded parent of everything below it while not being a pair at
     * all; skipping those is what keeps one unbalanced brace from erasing the spotlight for
     * the whole file below it.
     */
    private fun walkOut(start: Int): BracePair? {
        var index = start
        while (index >= 0 && index < braces.size) {
            val candidate = braces[index]
            if (candidate.isMatched && candidate.partnerIndex >= 0) {
                return BracePair(index, candidate.partnerIndex)
            }

            index = candidate.parentIndex
        }

        return null
    }

    /**
     * True if any brace in [start, end) wants extra headroom above its line — which today
     * means a creature or a costume, whose ears and hats live above the text.
     */
    fun needsHeadroom(start: Int, end: Int): Boolean {
        var i = firstIndexAtOrAfter(start)
        while (i < braces.size) {
            val brace = braces[i]
            if (brace.position >= end) {
                return false
            }

            if (brace.traits.wantsHeadroom) {
                return true
            }

            i++
        }

        return false
    }

    companion object {
        val EMPTY = BraceMap(emptyArray())
    }
}
