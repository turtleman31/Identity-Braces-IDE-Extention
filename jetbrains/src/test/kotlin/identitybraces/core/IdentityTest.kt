package identitybraces.core

import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNotEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test

/**
 * Identity stability — the whole point.
 *
 * Get this wrong and every keystroke reshuffles every colour, and the plugin is unusable
 * within about four seconds. Identity is the hash of the brace's *declaring line*, which is
 * what makes it survive edits elsewhere, survive a reformat, and change only when the thing
 * it belongs to changes.
 */
class IdentityTest {
    private fun scan(text: String, settings: ScanSettings = ScanSettings()): Array<BraceInfo> {
        return BraceScanner.scan(text, settings)
    }

    private fun firstOpener(text: String): BraceInfo {
        return scan(text).first { it.isOpen && it.kind == BraceKind.Curly }
    }

    @Test
    fun `an Allman brace resolves to the signature above it`() {
        // A brace alone on its own line has nothing to hash, so it walks back to the nearest
        // preceding non-blank line — which is the declaration it belongs to.
        val allman = firstOpener("public void Foo()\n{\n    Bar();\n}")
        val kAndR = firstOpener("public void Foo() {\n    Bar();\n}")

        assertEquals(kAndR.identity, allman.identity, "both brace styles give the same method the same brace")
    }

    @Test
    fun `identity survives an insertion above`() {
        val before = firstOpener("void Alpha()\n{\n  A();\n}")
        val after = firstOpener("using System;\n\nvoid Alpha()\n{\n  A();\n}")

        assertEquals(before.identity, after.identity)
        assertNotEquals(before.position, after.position, "the brace really did move")
    }

    @Test
    fun `identity survives an edit inside the block`() {
        val before = firstOpener("void Alpha()\n{\n  A();\n}")
        val after = firstOpener("void Alpha()\n{\n  A();\n  B();\n  C();\n}")

        assertEquals(before.identity, after.identity)
    }

    @Test
    fun `identity survives a reformat`() {
        // Whitespace is dropped entirely rather than collapsed, so re-indenting, wrapping a
        // long signature and tightening `( )` to `()` all leave the identity alone.
        val before = firstOpener("void Alpha()\n{\n  A();\n}")
        val after = firstOpener("void   Alpha( )\n{\n\tA();\n}")

        assertEquals(before.identity, after.identity)
    }

    @Test
    fun `identity changes with a rename`() {
        val alpha = firstOpener("void Alpha()\n{\n  A();\n}")
        val gamma = firstOpener("void Gamma()\n{\n  A();\n}")

        assertNotEquals(alpha.identity, gamma.identity, "it is a different method now")
    }

    @Test
    fun `a closer does not inherit its opener when braces are independent`() {
        val braces = scan("void F() { if (x) { g(); } }", ScanSettings(round = false))

        for (brace in braces) {
            if (brace.isOpen && brace.isMatched) {
                assertNotEquals(brace.identity, braces[brace.partnerIndex].identity, "the halves disagree")
                assertTrue(braces[brace.partnerIndex].isMatched, "they are still a pair")
            }
        }
    }

    @Test
    fun `turning independence off restores a shared identity`() {
        val braces = scan("void F() { g(); }", ScanSettings(round = false, independentBraces = false))

        assertEquals(2, braces.size)
        assertEquals(braces[0].identity, braces[1].identity)
        assertEquals(braces[0].colorIndex, braces[1].colorIndex)
    }

    @Test
    fun `a closer identity is as stable as its opener's`() {
        fun closerOf(text: String): BraceInfo {
            val braces = scan(text, ScanSettings(round = false))
            val opener = braces.first { it.isOpen && it.isMatched }
            return braces[opener.partnerIndex]
        }

        val before = closerOf("void Alpha()\n{\n  A();\n}")
        val inserted = closerOf("using System;\n\nvoid Alpha()\n{\n  A();\n}")
        val reformatted = closerOf("void   Alpha( )\n{\n\tA();\n}")
        val renamed = closerOf("void Gamma()\n{\n  A();\n}")

        assertEquals(before.identity, inserted.identity, "survives an insertion above")
        assertEquals(before.identity, reformatted.identity, "survives a reformat")
        assertNotEquals(before.identity, renamed.identity, "changes with a rename")
    }

    @Test
    fun `stacked closers all differ from one another`() {
        // A `}` usually sits alone on its line, so hashing its own declaring text would
        // collide every closer in the file onto the same near-empty header.
        val text = "void F()\n{\n    if (a)\n    {\n        if (b)\n        {\n            g();\n        }\n    }\n}"
        val closers = scan(text, ScanSettings(round = false)).filter { !it.isOpen }

        val seen = closers.map { it.identity }.toSet()
        assertEquals(closers.size, seen.size, "${closers.size} closers, ${seen.size} identities")
    }

    @Test
    fun `colours and personalities are spread across the palette`() {
        val source = (0 until 400).joinToString("\n\n") { i -> "void Method$i()\n{\n    Body$i();\n}" }
        val braces = scan(source, ScanSettings(round = false))

        val counts = IntArray(Palette.COUNT)
        for (brace in braces) {
            counts[brace.colorIndex]++
        }

        assertTrue(counts.all { it > 0 }, "every palette entry is used at least once")

        val drawn = braces.count { it.traits.isDrawn }.toDouble() / braces.size
        // The shipped defaults are 6% question, 8% cycle, 4% cat, 4% stockings, rolled on
        // independent layers — call it a fifth of all braces, and allow a wide band because
        // this is a distribution, not an arithmetic identity.
        assertTrue(drawn > 0.1 && drawn < 0.35, "${"%.1f".format(drawn * 100)}% of braces are drawn")
    }

    @Test
    fun `names are a pure function of identity`() {
        val name = BraceNames.of(0x83d6297633fcc0a5uL)
        assertEquals("Gideon", name, "the first brace of the parity fixture")
        assertEquals(name, BraceNames.of(0x83d6297633fcc0a5uL))
    }

    @Test
    fun `a large file scans fast enough to run on every keystroke`() {
        // The Visual Studio extension does 1.17 MB in about 18 ms; the JVM should be in the
        // same league once warm. A whole-file rescan still has to fit inside a keystroke,
        // hence the maxFileLength guard.
        val block = "public void Method()\n{\n    if (a) { for (int i = 0; i < n; i++) { work(i); } }\n}\n\n"
        val source = block.repeat(6000)

        // Warm the JIT once; the first pass measures class loading, not the scanner.
        BraceScanner.scan(source, ScanSettings())

        val started = System.nanoTime()
        val braces = BraceScanner.scan(source, ScanSettings())
        val elapsedMs = (System.nanoTime() - started) / 1e6

        assertTrue(braces.size > 50000, "${braces.size} braces")
        assertTrue(elapsedMs < 500, "scanned ${source.length} characters in ${"%.0f".format(elapsedMs)} ms")
    }
}
