package identitybraces.core

import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test

/**
 * The lexer, ported from the Visual Studio extension's own suite.
 *
 * Every approximation here is meant to fail in the safe direction: skipping a brace we could
 * have coloured rather than colouring one inside a string. The tests are written to catch a
 * change of direction, not just a change of answer.
 */
class ScannerTest {
    private fun scan(text: String, settings: ScanSettings = ScanSettings()): Array<BraceInfo> {
        return BraceScanner.scan(text, settings)
    }

    private fun at(braces: Array<BraceInfo>, text: String, needle: String): BraceInfo {
        val offset = text.indexOf(needle)
        return braces.first { it.position == offset }
    }

    @Test
    fun `braces inside a string are ignored`() {
        val text = "var s = \"{ not a brace }\"; { real }"
        val braces = scan(text)
        assertEquals(2, braces.size, "only the real pair is found")
        assertEquals(text.indexOf("{ real"), braces[0].position)
    }

    @Test
    fun `braces inside a line comment are ignored`() {
        assertEquals(2, scan("// { comment brace }\n{ real }").size)
    }

    @Test
    fun `braces inside a block comment are ignored`() {
        assertEquals(2, scan("/* { block } */ { real }").size)
    }

    @Test
    fun `a verbatim string spans lines`() {
        assertEquals(2, scan("var v = @\"line one {\nline two }\"; { real }").size)
    }

    @Test
    fun `a raw string spans lines`() {
        assertEquals(2, scan("var r = \"\"\"\n raw { body }\n\"\"\"; { real }").size)
    }

    @Test
    fun `a template literal is opaque`() {
        assertEquals(2, scan("var t = `tpl { hole }`; { real }").size)
    }

    @Test
    fun `a char literal is skipped`() {
        assertEquals(2, scan("var c = '{'; { real }").size)
    }

    @Test
    fun `a lifetime does not swallow the rest of the file`() {
        // The hazard: `'a` looks exactly like an opening quote, and treating it as one would
        // blank every brace after it. A single-quoted run that does not close on its own
        // line is therefore not a string at all.
        val text = "impl Foo {\n  fn a(x: &'a str) { bar(); }\n}"
        val braces = scan(text)

        assertEquals(8, braces.size, "two curly pairs and two paren pairs")
        assertEquals(text.lastIndexOf('}'), braces.last().position, "the last brace is still found")
        assertTrue(braces.all { it.isMatched }, "and everything still pairs up")
    }

    @Test
    fun `an unterminated string stops at end of line`() {
        assertEquals(2, scan("var s = \"unterminated { \n{ real }").size)
    }

    @Test
    fun `an interpolation hole is treated as string content`() {
        // Deliberate: the brace inside `$"{x}"` is not coloured. Failing this way costs a
        // colour; failing the other way colours the middle of a string literal.
        assertEquals(2, scan("var s = \$\"a {x} b\"; { real }").size)
    }

    @Test
    fun `pairs are matched and both halves know each other`() {
        val text = "void F() { if (x) { g(); } }"
        val braces = scan(text)

        assertTrue(braces.all { it.isMatched }, "every brace found a partner")

        val outer = at(braces, text, "{ if")
        assertEquals(text.lastIndexOf('}'), braces[outer.partnerIndex].position)
        assertEquals(outer.position, braces[braces[outer.partnerIndex].partnerIndex].position)
    }

    @Test
    fun `an unmatched opener is flagged and questions itself`() {
        val text = "void F() { g(); "
        val open = at(scan(text), text, "{")

        assertFalse(open.isMatched)
        assertEquals(TraitIds.QUESTION, open.traits.body, "an unmatched brace questions itself")
    }

    @Test
    fun `a stray closer does not cascade`() {
        // One typo must not recolour every brace below it, so the scan does not unwind its
        // stack on a closer it was not expecting.
        val withStray = scan("void A() { }\n}\nvoid B() { }")
        val without = scan("void A() { }\nvoid B() { }")

        assertEquals(without.last().identity, withStray.last().identity)
        assertEquals(without.last().colorIndex, withStray.last().colorIndex)
    }

    @Test
    fun `a closer reports its opener's depth, so a pair agrees with itself`() {
        val text = "void F()\n{\n    if (a)\n    {\n        g();\n    }\n}"
        val braces = scan(text, ScanSettings(round = false))

        for (brace in braces) {
            if (brace.isMatched && !brace.isOpen) {
                assertEquals(braces[brace.partnerIndex].depth, brace.depth)
            }
        }
    }

    @Test
    fun `depth colouring gives a pair one colour and neighbours different ones`() {
        val text = "void F()\n{\n    if (a)\n    {\n        g();\n    }\n}"
        val braces = scan(text, ScanSettings(round = false, colorByDepth = true))

        val outer = at(braces, text, "{")
        val inner = braces.first { it.isOpen && it.depth == 1 }

        assertEquals(braces[outer.partnerIndex].colorIndex, outer.colorIndex, "a pair agrees")
        assertNotEquals(inner.colorIndex, outer.colorIndex, "neighbouring depths differ")
    }

    @Test
    fun `brace kinds can be switched off independently`() {
        val text = "void F(int a) { b[0]; }"
        assertEquals(2, scan(text, ScanSettings(round = false, square = false)).size)
        assertEquals(2, scan(text, ScanSettings(curly = false, square = false)).size)
        assertEquals(2, scan(text, ScanSettings(curly = false, round = false)).size)
    }

    @Test
    fun `the complexity warning is a predicate, not a roll`() {
        val braces = scan("a { b { c { d { e } } } }", ScanSettings(complexityWarningDepth = 2))

        for (brace in braces) {
            assertEquals(brace.depth >= 2, brace.traits.hasEffect(TraitIds.DISTRESSED), "depth ${brace.depth}")
        }
    }

    @Test
    fun `an empty document has no braces`() {
        assertEquals(0, scan("").size)
    }

    @Test
    fun `the enclosing pair is found by walking up, and skips unmatched openers`() {
        val text = "void F() { if (x) { g(); } }"
        val map = BraceMap(scan(text))

        val inner = map.enclosingPair(text.indexOf("g()"))!!
        assertEquals(text.indexOf("{ g"), map[inner.openIndex].position)

        val outer = map.parentPair(inner.openIndex)!!
        assertEquals(text.indexOf("{ if"), map[outer.openIndex].position)
        assertEquals(null, map.parentPair(outer.openIndex))

        // An unbalanced opener between the caret and its real block must not erase the
        // spotlight for everything below it.
        val broken = "void F() { if (x) { g(); }"
        val brokenMap = BraceMap(scan(broken))
        val pair = brokenMap.enclosingPair(broken.indexOf("g()"))!!
        assertEquals(broken.indexOf("{ g"), brokenMap[pair.openIndex].position)
        assertEquals(null, brokenMap.parentPair(pair.openIndex), "the unmatched outer brace is not a pair")
    }
}
