package identitybraces.editor

/** The one clock every animation reads, so a screenful of braces agrees about the time. */
object Session {
    private val startedAt = System.currentTimeMillis()

    /** Milliseconds since the IDE loaded the plugin. */
    val timeMs: Long
        get() = System.currentTimeMillis() - startedAt

    /** Minutes since the IDE loaded the plugin, for the traits that wear down over a session. */
    val minutes: Double
        get() = timeMs / 60_000.0
}
