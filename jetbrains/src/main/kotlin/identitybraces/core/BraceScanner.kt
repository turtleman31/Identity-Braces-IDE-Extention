package identitybraces.core

/**
 * Finds braces in C-family source and gives each pair a stable identity.
 *
 * A deliberately small, self-contained lexer rather than a query against the IDE's PSI: an
 * approximate lexer is predictable, allocation-light, needs no language plugin, and covers
 * every C-family file type at once — which is what lets one plugin serve Rider, CLion,
 * WebStorm and IntelliJ alike.
 *
 * Known approximations, all of which fail in the safe direction (skipping a brace we could
 * have coloured, rather than colouring one inside a string): interpolation holes in
 * `$"{expr}"` and template literals are treated as opaque string content, so braces inside
 * them are ignored.
 *
 * Bit-identical to the Visual Studio extension's `BraceScanner` and the VS Code port's
 * `braceScanner.ts`; the parity test holds all three to the same fixture.
 */
object BraceScanner {
    /** Separates a closing brace's identity from its opener's. */
    private const val CLOSER_SALT: ULong = 0x5EA1EDFA7ECAFEuL

    private const val MAX_HEADER_LENGTH = 200
    private const val MAX_HEADER_LOOKBACK_LINES = 4

    private class RawBrace(val position: Int, val character: Char, val kind: BraceKind, val isOpen: Boolean)

    private class Pending(val index: Int, val expected: Char)

    fun scan(text: CharSequence, settings: ScanSettings): Array<BraceInfo> {
        if (text.isEmpty()) {
            return emptyArray()
        }

        return pair(text, tokenize(text, settings), settings)
    }

    /** Pass one: walk the text, skipping comments and strings, collecting braces. */
    private fun tokenize(text: CharSequence, settings: ScanSettings): List<RawBrace> {
        val raw = ArrayList<RawBrace>()
        val n = text.length
        var i = 0

        while (i < n) {
            val c = text[i]

            // Comments.
            if (c == '/' && i + 1 < n) {
                val next = text[i + 1]
                if (next == '/') {
                    i = skipToLineEnd(text, i + 2)
                    continue
                }

                if (next == '*') {
                    i = skipBlockComment(text, i + 2)
                    continue
                }
            }

            // Verbatim / interpolated / raw string prefixes: @" $" $@" @$"
            if (c == '@' || c == '$') {
                var q = i
                var verbatim = false
                while (q < n && (text[q] == '@' || text[q] == '$')) {
                    if (text[q] == '@') {
                        verbatim = true
                    }

                    q++
                }

                if (q < n && text[q] == '"') {
                    i = skipQuoted(text, q, verbatim)
                    continue
                }
            }

            if (c == '"') {
                i = skipQuoted(text, i, false)
                continue
            }

            // JS/TS template literal. Spans lines; holes are treated as opaque.
            if (c == '`') {
                i = skipTemplate(text, i)
                continue
            }

            // Char literal, or a single-quoted JS string. Must close on the same line,
            // otherwise it is something else entirely (a Rust lifetime, an apostrophe in
            // prose) and we must not swallow the rest of the file.
            if (c == '\'') {
                val end = trySkipSingleQuoted(text, i)
                if (end > 0) {
                    i = end
                    continue
                }
            }

            val classified = BraceKind.classify(c)
            if (classified != null && settings.includes(classified.first)) {
                raw.add(RawBrace(i, c, classified.first, classified.second))
            }

            i++
        }

        return raw
    }

    /** Pass two: match pairs with a stack and derive each pair's identity. */
    private fun pair(text: CharSequence, raw: List<RawBrace>, settings: ScanSettings): Array<BraceInfo> {
        val stack = ArrayList<Pending>()
        val builder = StringBuilder(MAX_HEADER_LENGTH)

        // Flattened once for the whole file, not per brace: see TraitTable.
        val table = TraitTable(settings.traitWeights)

        val result = Array(raw.size) { i ->
            val brace = raw[i]
            BraceInfo(brace.position, brace.character, brace.kind, brace.isOpen)
        }

        for (i in raw.indices) {
            val brace = raw[i]
            val info = result[i]
            info.depth = stack.size

            // Whatever is on top of the stack is by definition the innermost block still open
            // here, which is exactly what encloses this brace.
            info.parentIndex = if (stack.isEmpty()) -1 else stack[stack.size - 1].index

            if (brace.isOpen) {
                info.identity = computeIdentity(text, brace.position, brace.kind, stack.size, builder)
                stack.add(Pending(i, brace.kind.closer))
            } else if (stack.isNotEmpty() && stack[stack.size - 1].expected == brace.character) {
                val open = stack.removeAt(stack.size - 1)
                val opener = result[open.index]

                opener.isMatched = true
                info.isMatched = true

                opener.partnerIndex = i
                info.partnerIndex = open.index

                // A closer sits one level shallower than the body it closes, and its parent
                // is its opener's parent — it is a sibling of its opener, not a child of it.
                info.depth = opener.depth
                info.parentIndex = opener.parentIndex

                // Either the pair is one entity and the closer inherits everything, or the
                // closer is its own person.
                //
                // The closer does NOT hash its own declaring line. A closing brace usually
                // sits alone on one, so there is almost nothing to hash — every '}' in the
                // file would collide on the same near-empty header. Deriving from the opener
                // inherits all of its stability while guaranteeing the two halves never land
                // on the same value.
                info.identity = if (settings.independentBraces) Hash.mix(opener.identity, CLOSER_SALT) else opener.identity
            } else {
                // A stray closer. Do not unwind the stack — one typo should not recolour
                // every brace below it.
                info.identity = computeIdentity(text, brace.position, brace.kind, stack.size, builder)
            }
        }

        // Anything left on the stack never found a partner; isMatched stays false.
        val warnOnDepth = settings.complexityWarningDepth > 0

        for (info in result) {
            // Depth mode indexes the palette directly. The palette's hues are spaced by the
            // golden angle, so consecutive depths — which are always adjacent on screen —
            // land far apart on the wheel rather than shading into each other.
            info.colorIndex = if (settings.colorByDepth) {
                info.depth % Palette.COUNT
            } else {
                Hash.toIndex(info.identity, Palette.COUNT)
            }

            info.traits = TraitRoll.roll(
                info.identity,
                table,
                settings.questionUnmatched && !info.isMatched,
                warnOnDepth && info.depth >= settings.complexityWarningDepth,
            )
        }

        return result
    }

    /**
     * The load-bearing decision of the whole extension.
     *
     * Identity is the hash of the text that declares the brace — its line up to the brace,
     * whitespace-normalised, falling back to the nearest preceding non-blank line so that
     * Allman style (an opening brace alone on its own line) resolves to the signature above
     * it. Hashing the position instead would reincarnate every brace below any inserted
     * line; hashing the enclosed body would change a block's identity as you type inside it.
     */
    private fun computeIdentity(text: CharSequence, offset: Int, kind: BraceKind, depth: Int, builder: StringBuilder): ULong {
        buildHeader(text, offset, builder)
        val hash = Hash.fnv1a(builder)
        return Hash.mix(hash, depth.toULong() * 31uL + kind.ordinal.toULong())
    }

    private fun buildHeader(text: CharSequence, offset: Int, builder: StringBuilder) {
        var lineEnd = offset

        for (back in 0 until MAX_HEADER_LOOKBACK_LINES) {
            val lineStart = lineStartAt(text, lineEnd)
            normalize(text, lineStart, lineEnd, builder)
            if (builder.isNotEmpty() || lineStart == 0) {
                return
            }

            // Step back over the newline onto the previous line.
            lineEnd = lineStart - 1
            if (lineEnd > 0 && text[lineEnd] == '\n' && text[lineEnd - 1] == '\r') {
                lineEnd--
            }

            if (lineEnd <= 0) {
                return
            }
        }
    }

    /**
     * Copies [start, end) into the builder with all whitespace removed.
     *
     * Dropping whitespace entirely rather than collapsing runs to a single space is what makes
     * a colour survive a reformat: re-indenting, wrapping a long signature, or tightening
     * `( )` to `()` all leave the identity untouched.
     */
    private fun normalize(text: CharSequence, start: Int, end: Int, builder: StringBuilder) {
        builder.setLength(0)

        var i = start
        while (i < end && builder.length < MAX_HEADER_LENGTH) {
            val c = text[i]
            if (c != ' ' && c != '\t' && c != '\r' && c != '\n') {
                builder.append(c)
            }

            i++
        }
    }

    private fun lineStartAt(text: CharSequence, position: Int): Int {
        var i = position - 1
        while (i >= 0) {
            if (text[i] == '\n') {
                return i + 1
            }

            i--
        }

        return 0
    }

    private fun skipToLineEnd(text: CharSequence, from: Int): Int {
        var i = from
        while (i < text.length && text[i] != '\n') {
            i++
        }

        return i
    }

    private fun skipBlockComment(text: CharSequence, from: Int): Int {
        var i = from
        while (i + 1 < text.length) {
            if (text[i] == '*' && text[i + 1] == '/') {
                return i + 2
            }

            i++
        }

        return text.length
    }

    /**
     * Skips a double-quoted run starting at [i]. Handles raw strings (three or more quotes,
     * any number of lines) and verbatim strings (doubled-quote escapes). A plain string stops
     * at end of line, because an unterminated one must not eat the rest of the file.
     */
    private fun skipQuoted(text: CharSequence, i: Int, verbatim: Boolean): Int {
        val n = text.length

        var quotes = 0
        while (i + quotes < n && text[i + quotes] == '"') {
            quotes++
        }

        if (quotes >= 3) {
            var j = i + quotes
            while (j < n) {
                if (text[j] == '"') {
                    var run = 0
                    while (j + run < n && text[j + run] == '"') {
                        run++
                    }

                    if (run >= quotes) {
                        return j + run
                    }

                    j += run
                } else {
                    j++
                }
            }

            return n
        }

        if (quotes == 2) {
            return i + 2
        }

        var k = i + 1
        while (k < n) {
            val c = text[k]

            if (verbatim) {
                if (c == '"') {
                    if (k + 1 < n && text[k + 1] == '"') {
                        k += 2
                        continue
                    }

                    return k + 1
                }

                k++
                continue
            }

            if (c == '\\') {
                k += 2
                continue
            }

            if (c == '"') {
                return k + 1
            }

            if (c == '\n') {
                return k
            }

            k++
        }

        return n
    }

    private fun skipTemplate(text: CharSequence, i: Int): Int {
        val n = text.length
        var k = i + 1

        while (k < n) {
            val c = text[k]
            if (c == '\\') {
                k += 2
                continue
            }

            if (c == '`') {
                return k + 1
            }

            k++
        }

        return n
    }

    /**
     * Returns the index past a single-quoted run, or -1 if it does not close on this line.
     * The -1 case matters: Rust lifetimes and stray apostrophes look exactly like an opening
     * quote, and treating them as one would blank the rest of the file.
     */
    private fun trySkipSingleQuoted(text: CharSequence, i: Int): Int {
        val n = text.length
        var k = i + 1

        while (k < n) {
            val c = text[k]
            if (c == '\\') {
                k += 2
                continue
            }

            if (c == '\n') {
                return -1
            }

            if (c == '\'') {
                return k + 1
            }

            k++
        }

        return -1
    }
}
