package identitybraces.editor

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.colors.EditorColorsScheme
import com.intellij.openapi.editor.colors.EditorFontType
import com.intellij.openapi.editor.impl.ComplementaryFontsRegistry
import com.intellij.openapi.editor.impl.FontInfo
import identitybraces.render.FontSource
import java.awt.Component
import java.awt.Font
import java.awt.GraphicsEnvironment
import java.awt.font.FontRenderContext

/**
 * Resolves the fonts a frame draws with from a colour scheme — an editor's own, zoom
 * included, or the global one for a preview drawn outside any editor.
 */
class EditorFonts(
    private val scheme: () -> EditorColorsScheme,
    private val component: () -> Component,

    /** A size multiplier, for the magnified preview. */
    private val magnification: Float = 1f,
) : FontSource {
    constructor(editor: Editor) : this({ editor.colorsScheme }, { editor.contentComponent })

    val renderContext: FontRenderContext
        get() = FontInfo.getFontRenderContext(component())

    override fun editorFont(bold: Boolean, italic: Boolean): Font {
        var style = Font.PLAIN
        if (bold) style = style or Font.BOLD
        if (italic) style = style or Font.ITALIC

        val font = scheme().getFont(EditorFontType.forJavaStyle(style))
        return if (magnification == 1f) font else font.deriveFont(font.size2D * magnification)
    }

    override fun fontFor(text: String, base: Font): Font {
        if (base.canDisplayUpTo(text) < 0) {
            return base
        }

        // The editor's own fallback chain, so an emoji comes out in whatever the editor
        // itself would use for one — sized to match the current zoom.
        val codePoint = text.codePointAt(0)
        val info = ComplementaryFontsRegistry.getFontAbleToDisplay(codePoint, base.style, scheme().fontPreferences, renderContext)
        return info.font.deriveFont(base.size2D)
    }

    override fun foreignFont(base: Font): Font {
        val family = FOREIGN_FAMILIES.firstOrNull { installed.contains(it) } ?: Font.SERIF
        return Font(family, base.style, base.size)
    }

    companion object {
        private val FOREIGN_FAMILIES = listOf("Comic Sans MS", "Comic Neue", "Chalkboard", "Comic Relief")

        private val installed: Set<String> by lazy {
            try {
                GraphicsEnvironment.getLocalGraphicsEnvironment().availableFontFamilyNames.toHashSet()
            } catch (e: Exception) {
                emptySet()
            }
        }
    }
}
