package identitybraces.core

/** Everything [BraceScanner] needs, with no reference to the editor. */
data class ScanSettings(
    val curly: Boolean = true,
    val round: Boolean = true,
    val square: Boolean = true,

    /**
     * Every trait and how often it comes up, in catalogue order — the order decides which
     * trait a roll lands on, so it is part of the identity contract, not a detail.
     */
    val traitWeights: List<TraitWeight> = TraitCatalog.defaultWeights(),

    /**
     * When true, a brace with no partner always renders as '?'. A brace that has lost its
     * other half genuinely does not know who it is, and it doubles as a syntax hint.
     */
    val questionUnmatched: Boolean = true,

    /**
     * When true, a closing brace gets its own identity instead of inheriting its opener's —
     * so `{` and its `}` are different colours, and may be different creatures entirely.
     */
    val independentBraces: Boolean = true,

    /**
     * Colour by nesting depth instead of by identity — the one setting here that makes code
     * *easier* to read.
     *
     * Resolved in the scanner rather than at either consumer, because a personality brace a
     * different colour from the plain brace beside it looks like a bug in the palette. One
     * value, one place, both paths.
     */
    val colorByDepth: Boolean = false,

    /** Braces nested at least this deep are visibly distressed. Zero switches it off. */
    val complexityWarningDepth: Int = 0,
) {
    fun includes(kind: BraceKind): Boolean = when (kind) {
        BraceKind.Curly -> curly
        BraceKind.Round -> round
        BraceKind.Square -> square
    }
}
