package identitybraces.core

/**
 * One brace, with the identity derived for it.
 *
 * Whether a matched pair shares its [identity], [colorIndex] and [traits] depends on
 * [ScanSettings.independentBraces]. Sharing them was the last thing here that still helped
 * you read code, so it is off by default.
 */
class BraceInfo(
    /** Offset into the document text. */
    val position: Int,

    /** The literal character in the buffer. */
    val character: Char,

    val kind: BraceKind,

    val isOpen: Boolean,
) {
    /** False when this brace has no partner — a real syntax error. */
    var isMatched: Boolean = false

    /** Stable hash of the pair's declaring text. See [BraceScanner]. */
    var identity: ULong = 0uL

    /** Index into [Palette]. */
    var colorIndex: Int = 0

    /**
     * How many pairs enclose this one. Zero at the outermost level.
     *
     * A closing brace reports its *opener's* depth, not the depth of the position it sits at,
     * so a pair agrees with itself under [ScanSettings.colorByDepth].
     */
    var depth: Int = 0

    /**
     * Index of the matching brace in the same map, or -1 when there is none. Set on both
     * halves, so given either brace you can reach the other in one step.
     */
    var partnerIndex: Int = -1

    /**
     * Index of the opening brace that encloses this one, or -1 at the outermost level.
     *
     * The scan already has this on its stack, so recording it is free — and it turns "which
     * pairs enclose this position?" from a walk back through the file into a walk up a chain
     * as long as the nesting is deep. It can point at an *unmatched* opener, which stays on
     * the stack and legitimately encloses everything after it; callers wanting a real pair
     * must skip those — see [BraceMap.enclosingPair].
     */
    var parentIndex: Int = -1

    /** What this brace turned out to be, across every layer. */
    var traits: BraceTraits = BraceTraits.NONE

    /** True when this brace is drawn by the overlay rather than by a plain colour. */
    val isAdorned: Boolean
        get() = traits.isDrawn
}
