package identitybraces.core

/** The three bracket families we colour. The ordinal is part of the identity hash. */
enum class BraceKind(val opener: Char, val closer: Char) {
    Curly('{', '}'),
    Round('(', ')'),
    Square('[', ']');

    companion object {
        /** The family and side of [c], or null for anything that is not a bracket. */
        fun classify(c: Char): Pair<BraceKind, Boolean>? = when (c) {
            '{' -> Curly to true
            '}' -> Curly to false
            '(' -> Round to true
            ')' -> Round to false
            '[' -> Square to true
            ']' -> Square to false
            else -> null
        }
    }
}
