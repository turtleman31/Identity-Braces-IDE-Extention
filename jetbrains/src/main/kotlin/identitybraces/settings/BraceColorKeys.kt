package identitybraces.settings

import com.intellij.openapi.editor.colors.EditorColorsScheme
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.fileTypes.PlainSyntaxHighlighter
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.options.colors.AttributesDescriptor
import com.intellij.openapi.options.colors.ColorDescriptor
import com.intellij.openapi.options.colors.ColorSettingsPage
import identitybraces.core.Palette
import java.awt.Color

/**
 * The 32 palette entries as colour-scheme attributes, so they are editable under
 * Settings → Editor → Color Scheme → Identity Braces — the equivalent of the Visual Studio
 * extension's Fonts and Colors entries.
 *
 * The count is fixed at 32 for the same reason it is in Visual Studio: keys are registered
 * once, statically, and the palette's hue spacing was designed for that many.
 */
object BraceColorKeys {
    // The overload with inline defaults is deprecated in favour of scheme XML files, but it
    // is the only one that guarantees a colour in a scheme that has never heard of us —
    // which is every scheme, the first time. Without a default the highlighter would resolve
    // to nothing and the brace would simply keep the syntax colour.
    @Suppress("DEPRECATION")
    val keys: List<TextAttributesKey> = List(Palette.COUNT) { i ->
        TextAttributesKey.createTextAttributesKey(
            "IDENTITY_BRACE_%02d".format(i),
            TextAttributes(Color(Palette.argb(i), false), null, null, null, 0),
        )
    }

    /**
     * The colour a palette index resolves to under [scheme]: the user's own if they retuned
     * it, the built-in entry otherwise.
     */
    fun resolve(scheme: EditorColorsScheme, index: Int): Color {
        val wrapped = ((index % Palette.COUNT) + Palette.COUNT) % Palette.COUNT
        return scheme.getAttributes(keys[wrapped])?.foregroundColor ?: Color(Palette.argb(wrapped), false)
    }
}

/** The Color Scheme page listing the 32 entries, with a demo that wears all of them. */
class IdentityBracesColorSettingsPage : ColorSettingsPage {
    private val descriptors: Array<AttributesDescriptor> = Array(Palette.COUNT) { i ->
        AttributesDescriptor("Identity brace %02d".format(i), BraceColorKeys.keys[i])
    }

    override fun getDisplayName(): String = "Identity Braces"

    override fun getIcon() = null

    override fun getAttributeDescriptors(): Array<AttributesDescriptor> = descriptors

    override fun getColorDescriptors(): Array<ColorDescriptor> = ColorDescriptor.EMPTY_ARRAY

    override fun getHighlighter(): SyntaxHighlighter = PlainSyntaxHighlighter()

    override fun getAdditionalHighlightingTagToDescriptorMap(): Map<String, TextAttributesKey> {
        val map = HashMap<String, TextAttributesKey>()
        for (i in 0 until Palette.COUNT) {
            map["ib%02d".format(i)] = BraceColorKeys.keys[i]
        }

        return map
    }

    override fun getDemoText(): String {
        val sb = StringBuilder()
        sb.append("// Every brace is its own person. Thirty-two of them:\n")
        for (i in 0 until Palette.COUNT) {
            val tag = "ib%02d".format(i)
            val open = if (i % 3 == 0) '{' else if (i % 3 == 1) '(' else '['
            val close = if (i % 3 == 0) '}' else if (i % 3 == 1) ')' else ']'
            sb.append("void Method").append(i).append("() <").append(tag).append('>').append(open).append("</").append(tag).append('>')
            sb.append(" work(); <").append(tag).append('>').append(close).append("</").append(tag).append(">\n")
        }

        return sb.toString()
    }
}
