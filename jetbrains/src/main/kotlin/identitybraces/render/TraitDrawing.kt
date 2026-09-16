package identitybraces.render

import identitybraces.core.TraitIds

/**
 * Maps a trait id to the code that draws it.
 *
 * The other half of the registry: `TraitCatalog` owns what a trait is called and how often
 * it appears; this owns what it looks like. Adding a creature is one row there and one entry
 * here — no new enum member, no new settings field, no new branch anywhere else.
 *
 * A trait with no entry here simply does not draw, so the catalogue can list things ahead of
 * their geometry without breaking anything.
 */
object TraitDrawing {
    private val painters: Map<String, Painter> = LinkedHashMap<String, Painter>().also {
        Creatures.register(it)
        Costumes.register(it)
        Motions.register(it)
        Effects.register(it)
    }

    /**
     * Traits that are real here without going through a painter: substitutions, transforms,
     * fades and font changes are applied to the glyph itself in [BraceStyle] and
     * [BraceRenderer], and the scenes are played by the scene director. Listing them
     * explicitly is the only way the settings page can say honestly which sliders do
     * something.
     */
    private val styled: Set<String> = setOf(
        TraitIds.QUESTION,
        TraitIds.UNICODE_VARIANT,
        TraitIds.WRONG_BRACKET,
        TraitIds.MIRRORED,
        TraitIds.UPSIDE_DOWN,
        TraitIds.EMOJI,
        TraitIds.FOREIGN_FONT,
        TraitIds.SUBSCRIPT,
        TraitIds.THIGH_HIGHS,
        TraitIds.COLOUR_CYCLE,
        TraitIds.WOBBLE,
        TraitIds.BOUNCE,
        TraitIds.BREATHE,
        TraitIds.HEARTBEAT,
        TraitIds.SHIVER,
        TraitIds.BLINK,
        TraitIds.SPIN,
        TraitIds.FLIP,
        TraitIds.GLITCH,
        TraitIds.DRIFT,
        TraitIds.FLICKER,
        TraitIds.WAVE,
        TraitIds.TYPEWRITER,
        TraitIds.GRADIENT_FILL,
        TraitIds.BOLD_ITALIC,
        TraitIds.TILTED,
        TraitIds.SHADOW,
        TraitIds.GHOST,
        TraitIds.NOCTURNAL,
        TraitIds.DRUNK,
        TraitIds.GRAVITY,
        TraitIds.FLEE_CURSOR,
        TraitIds.STAGE_FRIGHT,

        // Not drawn at all: the name arrives through an editor hint, because a drawn overlay
        // sits above the text and making it hit-testable would have it swallow the clicks
        // that place your caret.
        TraitIds.NAMED,
    )

    /** The scene traits, implemented by the director rather than by a painter. */
    private val scenes: Set<String> = setOf(TraitIds.SWAP_PLACES, TraitIds.TABLE_FLIP, TraitIds.FIRE_BRIGADE)

    fun hasPainter(id: String?): Boolean = id != null && painters.containsKey(id)

    /** Whether this trait actually does anything in this port. */
    fun isImplemented(id: String): Boolean = painters.containsKey(id) || styled.contains(id) || scenes.contains(id)

    fun paint(id: String?, canvas: InkCanvas) {
        if (id == null) {
            return
        }

        val painter = painters[id] ?: return
        try {
            painter(canvas)
        } catch (e: Exception) {
            // One malformed trait must not cost the brace its glyph, nor take down the paint
            // pass that is drawing forty others.
        }
    }

    /**
     * The text a body trait renders instead of the real character.
     *
     * Body traits are a substitution rather than an overlay, so they resolve to a string here
     * rather than to a painter. The document is untouched either way — a brace drawn as a
     * question mark is still a brace to the compiler, to the caret, to Find and to Git.
     */
    fun resolveBody(bodyId: String?, character: Char): String = when (bodyId) {
        TraitIds.QUESTION -> "?"
        TraitIds.UNICODE_VARIANT -> unicodeVariantFor(character)
        TraitIds.WRONG_BRACKET -> wrongBracketFor(character)
        TraitIds.EMOJI -> if (character == '{' || character == '(' || character == '[') "👉" else "👈"
        else -> character.toString()
    }

    private fun unicodeVariantFor(character: Char): String = when (character) {
        '{' -> "｛"
        '}' -> "｝"
        '(' -> "（"
        ')' -> "）"
        '[' -> "【"
        ']' -> "】"
        else -> character.toString()
    }

    /** Swaps the bracket family while keeping the direction. Deliberately hostile. */
    private fun wrongBracketFor(character: Char): String = when (character) {
        '{' -> "("
        '}' -> ")"
        '(' -> "["
        ')' -> "]"
        '[' -> "{"
        ']' -> "}"
        else -> character.toString()
    }
}
