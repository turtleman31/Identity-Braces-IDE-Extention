package identitybraces.core

/**
 * The 32 identities a brace can wear, as opaque ARGB.
 *
 * Hues are spaced by the golden angle (137.508 degrees) rather than evenly, so consecutive
 * indices — which frequently end up next to each other on screen — land far apart on the
 * colour wheel instead of shading into one another.
 *
 * Every entry was then lightness-solved to a relative luminance of 0.2072, the point at which
 * contrast against the dark editor background (#1E1E1E) and against white are equal. The
 * result is ~4.05:1 on both, so one palette serves every theme. In the IDE the 32 entries
 * are registered as colour-scheme attributes with these as their defaults, so they can be
 * retuned under Settings → Editor → Color Scheme.
 */
object Palette {
    const val COUNT = 32

    private val argb = intArrayOf(
        0xFFEE3333.toInt(), // 00
        0xFF19903C.toInt(), // 01
        0xFFB047FA.toInt(), // 02
        0xFF8C7E21.toInt(), // 03
        0xFF0D89A2.toInt(), // 04
        0xFFDD3C93.toInt(), // 05
        0xFF279204.toInt(), // 06
        0xFF7570DE.toInt(), // 07
        0xFFD75311.toInt(), // 08
        0xFF198E63.toInt(), // 09
        0xFFD806EB.toInt(), // 10
        0xFF718620.toInt(), // 11
        0xFF137DE9.toInt(), // 12
        0xFFDE4565.toInt(), // 13
        0xFF049310.toInt(), // 14
        0xFF9266DB.toInt(), // 15
        0xFFA7740E.toInt(), // 16
        0xFF198C88.toInt(), // 17
        0xFFEA06B0.toInt(), // 18
        0xFF528C21.toInt(), // 19
        0xFF5A73F2.toInt(), // 20
        0xFFDC4D38.toInt(), // 21
        0xFF04923F.toInt(), // 22
        0xFFB155D7.toInt(), // 23
        0xFF82820B.toInt(), // 24
        0xFF1F87B2.toInt(), // 25
        0xFFF60669.toInt(), // 26
        0xFF2F9022.toInt(), // 27
        0xFF7F68F3.toInt(), // 28
        0xFFBC6821.toInt(), // 29
        0xFF048E6C.toInt(), // 30
        0xFFD139CA.toInt(), // 31
    )

    /** The ARGB of palette entry [index], wrapping in both directions. */
    fun argb(index: Int): Int = argb[((index % COUNT) + COUNT) % COUNT]

    /** `#RRGGBB`, the form the VS Code port's `palette` setting uses. */
    fun hex(index: Int): String = "#" + (argb(index) and 0xFFFFFF).toString(16).padStart(6, '0').uppercase()
}
