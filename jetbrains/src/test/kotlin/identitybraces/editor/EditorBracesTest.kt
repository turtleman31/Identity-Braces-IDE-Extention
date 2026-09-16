package identitybraces.editor

import com.intellij.openapi.editor.markup.RangeHighlighter
import com.intellij.testFramework.EditorTestUtil
import com.intellij.testFramework.fixtures.BasePlatformTestCase
import identitybraces.core.TraitCatalog
import identitybraces.core.TraitIds
import identitybraces.settings.IdentityBracesSettings
import java.awt.Color
import java.awt.image.BufferedImage

/**
 * The plugin against a real editor — headless, but the same `EditorImpl`, markup model and
 * painter the IDE uses.
 *
 * These are the checks that matter and that no unit test can make: that a highlighter lands
 * on every brace and nowhere else, that they follow the text when it moves, that a
 * personality brace is hidden and then actually painted by the overlay, and that switching
 * the plugin off leaves the editor exactly as it found it.
 */
class EditorBracesTest : BasePlatformTestCase() {
    private val sample = """
        namespace Sample
        {
            public sealed class Service
            {
                public int Get(int id)
                {
                    if (items.TryGetValue(id, out var item)) { return item; }
                    return values[id];
                }
            }
        }
    """.trimIndent()

    override fun tearDown() {
        try {
            IdentityBracesSettings.instance.update(IdentityBracesSettings.State())
        } finally {
            super.tearDown()
        }
    }

    private fun open(text: String = sample): EditorBraces {
        myFixture.configureByText("Sample.cs", text)
        EditorTestUtil.setEditorVisibleSize(myFixture.editor, 120, 60)
        // The fixture's editors are untyped, which the factory listener deliberately skips.
        val controller = EditorBraces.attachForTest(myFixture.editor)!!
        controller.refreshNow()
        return controller
    }

    private fun plainHighlighters(): List<RangeHighlighter> {
        // The editor's own brace matcher marks single characters on the same layer; ours are tagged.
        return myFixture.editor.markupModel.allHighlighters.filter { it.getUserData(EditorBraces.OWNED) == true }
    }

    fun `test every brace gets a highlighter and nothing else does`() {
        val controller = open()
        val map = controller.map()
        assertTrue("the sample has braces", map.size > 10)

        val highlighters = plainHighlighters()
        assertEquals(map.size, highlighters.size)

        val bracePositions = (0 until map.size).map { map[it].position }.toSet()
        for (highlighter in highlighters) {
            assertTrue("highlighter at ${highlighter.startOffset} sits on a brace", bracePositions.contains(highlighter.startOffset))
            assertTrue("the character is a bracket", "{}()[]".contains(myFixture.editor.document.charsSequence[highlighter.startOffset]))
        }
    }

    fun `test a drawn brace is painted transparent and a plain one in a palette colour`() {
        // Force a creature onto every brace: the plain highlighter must then hide the glyph.
        IdentityBracesSettings.instance.edit { state ->
            state.traitWeights = TraitCatalog.all.associate { it.id to 0 }.toMutableMap().also { it[TraitIds.CATGIRL] = 100 }
        }

        val controller = open()
        val map = controller.map()
        assertTrue((0 until map.size).all { map[it].traits.creature == TraitIds.CATGIRL })

        for (highlighter in plainHighlighters()) {
            val color = highlighter.getTextAttributes(myFixture.editor.colorsScheme)?.foregroundColor
            assertNotNull(color)
            assertEquals("a drawn brace is hidden", 0, color!!.alpha)
        }

        // And with nothing rolled, every brace is a visible palette colour.
        IdentityBracesSettings.instance.applyPreset("off")
        controller.refreshNow()
        for (highlighter in plainHighlighters()) {
            val color = highlighter.getTextAttributes(myFixture.editor.colorsScheme)?.foregroundColor
            assertNotNull(color)
            assertEquals(255, color!!.alpha)
        }
    }

    fun `test highlighters follow the text as it is edited`() {
        val controller = open()
        val before = plainHighlighters().map { it.startOffset }.sorted()

        // Insert a line at the top: every brace moves, and every highlighter must move with
        // it rather than sitting on the character that now occupies its old offset.
        myFixture.editor.caretModel.moveToOffset(0)
        myFixture.type("using System;\n")
        controller.refreshNow()

        val map = controller.map()
        val after = plainHighlighters().map { it.startOffset }.sorted()
        assertEquals(before.size, after.size)
        assertEquals((0 until map.size).map { map[it].position }.sorted(), after)

        // A brace typed at the end gets a highlighter of its own on the next refresh — and
        // so does the partner the editor auto-closes it with, so the map is the truth.
        myFixture.editor.caretModel.moveToOffset(myFixture.editor.document.textLength)
        myFixture.type("\n{")
        controller.refreshNow()

        val grown = controller.map()
        assertTrue(grown.size > before.size)
        assertEquals((0 until grown.size).map { grown[it].position }.sorted(), plainHighlighters().map { it.startOffset }.sorted())
    }

    fun `test the overlay paints a personality brace`() {
        IdentityBracesSettings.instance.edit { state ->
            state.traitWeights = TraitCatalog.all.associate { it.id to 0 }.toMutableMap().also { it[TraitIds.WIZARD] = 100 }
        }

        val controller = open("void F() { }")
        val editor = myFixture.editor
        val map = controller.map()
        val brace = map[map.firstIndexAtOrAfter("void F() {".length - 1)]
        assertEquals('{', brace.character)
        assertEquals(TraitIds.WIZARD, brace.traits.creature)

        val image = paintEditor()
        val geometry = controller.geometryOf(brace)!!

        // The hat is purple, drawn above the ink. Somewhere in the region above the brace
        // there must be a pixel that is neither background nor the brace's own colour.
        val background = editor.colorsScheme.defaultBackground
        var painted = 0
        val top = (geometry.inkTop - geometry.unit * 1.3).toInt().coerceAtLeast(0)
        val bottom = geometry.inkTop.toInt()
        for (y in top until bottom) {
            for (x in (geometry.centerX - geometry.unit).toInt().coerceAtLeast(0) until (geometry.centerX + geometry.unit).toInt()) {
                val pixel = Color(image.getRGB(x, y), true)
                if (pixel.alpha > 0 && distance(pixel, background) > 60) {
                    painted++
                }
            }
        }

        assertTrue("the wizard hat should leave ink above the brace, found $painted pixels", painted > 4)
    }

    fun `test a table flip is cast beside a brace with room and painted mid-flight`() {
        IdentityBracesSettings.instance.edit { state ->
            state.traitWeights = TraitCatalog.all.associate { it.id to 0 }.toMutableMap().also { it[TraitIds.TABLE_FLIP] = 100 }
        }

        // The closer ends its line, so it has the whole rest of the row to throw into.
        val controller = open("void F()\n{\n    work();\n}\n")
        assertTrue("the director should find a brace with room", controller.director.playNow(TraitIds.TABLE_FLIP))

        val bounds = controller.director.activeBounds
        assertNotNull(bounds)

        // Painting mid-performance must draw the table somewhere inside the scene's bounds
        // and to the right of the throwing brace, where nothing else is drawn.
        val image = paintEditor()
        val map = controller.map()
        val closer = map[map.size - 1]
        val geometry = controller.geometryOf(closer)!!
        val background = myFixture.editor.colorsScheme.defaultBackground

        var painted = 0
        for (y in bounds!!.y.coerceAtLeast(0) until (bounds.y + bounds.height).coerceAtMost(image.height)) {
            for (x in (geometry.cellLeft + geometry.cellWidth * 1.5).toInt() until (bounds.x + bounds.width).coerceAtMost(image.width)) {
                val pixel = Color(image.getRGB(x, y), true)
                if (pixel.alpha > 0 && distance(pixel, background) > 60) {
                    painted++
                }
            }
        }

        assertTrue("the table should be in flight to the right of the brace, found $painted pixels", painted > 4)
    }

    fun `test switching off removes every highlighter`() {
        val controller = open()
        assertTrue(plainHighlighters().isNotEmpty())

        IdentityBracesSettings.instance.edit { it.enabled = false }
        controller.refreshNow()

        assertEquals(0, plainHighlighters().size)
        assertEquals(0, controller.plainHighlighterCount())
        assertTrue(
            "no whole-document highlighters remain either",
            myFixture.editor.markupModel.allHighlighters.none { it.customRenderer != null },
        )
    }

    private fun paintEditor(): BufferedImage {
        val component = myFixture.editor.contentComponent
        val width = 800
        val height = 200
        component.setSize(width, height)
        val image = BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB)
        val g = image.createGraphics()
        try {
            component.paint(g)
        } finally {
            g.dispose()
        }

        return image
    }

    private fun distance(a: Color, b: Color): Int {
        return kotlin.math.abs(a.red - b.red) + kotlin.math.abs(a.green - b.green) + kotlin.math.abs(a.blue - b.blue)
    }
}
