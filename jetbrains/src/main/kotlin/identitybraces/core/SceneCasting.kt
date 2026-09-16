package identitybraces.core

/**
 * The two questions a scene director asks that are pure arithmetic: who can act, and is
 * there room to act in.
 *
 * Everything else a scene needs — line geometry, a painter, a timer — only exists inside a
 * running editor, but deciding *whether* a brace can flip a table is a question about a
 * string and a column number. This is the part that goes wrong quietly: a director that
 * casts a brace with no room beside it throws a prop through the middle of somebody's code.
 */
object SceneCasting {
    /**
     * Blank columns to the right of [column], up to [limit]. Everything past the end of the
     * line counts as blank — a closing brace usually ends its line, so the whole rest of the
     * row is clear and it has the most room of anyone.
     */
    fun roomRightOf(lineText: CharSequence?, column: Int, limit: Int): Int {
        if (limit <= 0 || lineText == null) {
            return 0
        }

        var room = 0
        var i = column + 1
        while (room < limit) {
            if (i >= lineText.length) {
                return limit
            }

            if (!isBlank(lineText[i])) {
                return room
            }

            room++
            i++
        }

        return room
    }

    /**
     * Blank columns to the left of [column], up to [limit]. Unlike the right, the start of
     * the line is a hard wall: a prop thrown past column zero lands in the gutter.
     */
    fun roomLeftOf(lineText: CharSequence?, column: Int, limit: Int): Int {
        if (limit <= 0 || lineText == null) {
            return 0
        }

        var room = 0
        var i = column - 1
        while (i >= 0 && room < limit) {
            if (i < lineText.length && !isBlank(lineText[i])) {
                return room
            }

            room++
            i--
        }

        return room
    }

    /**
     * Every brace in [start, end) carrying [traitId] as an effect. Bounded by the requested
     * span rather than the file: the director only ever casts from what is on screen.
     */
    fun findCandidates(map: BraceMap, traitId: String, start: Int, end: Int, into: MutableList<Int>) {
        into.clear()
        map.forEachIn(start, end) { index, brace ->
            if (brace.traits.hasEffect(traitId)) {
                into.add(index)
            }
        }
    }

    /**
     * Picks one of [candidates] from a seed. Deterministic rather than `Random` so a scene
     * that misbehaves can be reproduced from the seed that produced it.
     */
    fun choose(candidates: List<Int>, seed: ULong): Int {
        if (candidates.isEmpty()) {
            return -1
        }

        return candidates[(Hash.toUnitInterval(Hash.mix(seed, 0x5CE7EuL)) * candidates.size).toInt() % candidates.size]
    }

    private fun isBlank(c: Char): Boolean = c == ' ' || c == '\t'
}
