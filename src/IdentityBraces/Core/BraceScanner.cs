using System.Collections.Generic;
using System.Text;

namespace IdentityBraces.Core
{
    /// <summary>
    /// Finds braces in C-family source and gives each pair a stable identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a deliberately small, self-contained lexer rather than a query against the
    /// language service's classifier. Asking the aggregate classifier for a whole document
    /// from inside a tagger is slow and invites re-entrancy; an approximate lexer is
    /// predictable, allocation-light, and covers every C-family content type at once.
    /// </para>
    /// <para>
    /// Known approximations, all of which fail in the safe direction (skipping a brace we
    /// could have coloured, rather than colouring one inside a string): interpolation holes
    /// in $"{expr}" and template literals are treated as opaque string content, so braces
    /// inside them are ignored.
    /// </para>
    /// </remarks>
    internal static class BraceScanner
    {

        /// <summary>Separates a closing brace's identity from its opener's.</summary>
        private const ulong CloserSalt = 0x5EA1EDFA7ECAFEUL;

        private const int MaxHeaderLength = 200;
        private const int MaxHeaderLookbackLines = 4;

        private struct RawBrace
        {
            public int Position;
            public char Character;
            public BraceKind Kind;
            public bool IsOpen;
        }

        private struct Pending
        {
            public int Index;
            public char Expected;
        }

        public static BraceInfo[] Scan(string text, ScanSettings settings)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new BraceInfo[0];
            }

            List<RawBrace> raw = Tokenize(text, settings);
            return Pair(text, raw, settings);
        }

        /// <summary>Pass one: walk the text, skipping comments and strings, collecting braces.</summary>
        private static List<RawBrace> Tokenize(string text, ScanSettings settings)
        {
            var raw = new List<RawBrace>();
            int n = text.Length;
            int i = 0;

            while (i < n)
            {
                char c = text[i];

                // Comments.
                if (c == '/' && i + 1 < n)
                {
                    char next = text[i + 1];
                    if (next == '/')
                    {
                        i = SkipToLineEnd(text, i + 2);
                        continue;
                    }

                    if (next == '*')
                    {
                        i = SkipBlockComment(text, i + 2);
                        continue;
                    }
                }

                // Verbatim / interpolated / raw string prefixes: @" $" $@" @$"
                if (c == '@' || c == '$')
                {
                    int q = i;
                    bool verbatim = false;
                    while (q < n && (text[q] == '@' || text[q] == '$'))
                    {
                        if (text[q] == '@')
                        {
                            verbatim = true;
                        }

                        q++;
                    }

                    if (q < n && text[q] == '"')
                    {
                        i = SkipQuoted(text, q, verbatim);
                        continue;
                    }
                }

                if (c == '"')
                {
                    i = SkipQuoted(text, i, false);
                    continue;
                }

                // JS/TS template literal. Spans lines; holes are treated as opaque.
                if (c == '`')
                {
                    i = SkipTemplate(text, i);
                    continue;
                }

                // Char literal, or a single-quoted JS string. Must close on the same line,
                // otherwise it is something else entirely (a Rust lifetime, an apostrophe
                // in prose) and we must not swallow the rest of the file.
                if (c == '\'')
                {
                    int end = TrySkipSingleQuoted(text, i);
                    if (end > 0)
                    {
                        i = end;
                        continue;
                    }
                }

                BraceKind kind;
                bool isOpen;
                if (TryClassifyBrace(c, out kind, out isOpen) && settings.Includes(kind))
                {
                    raw.Add(new RawBrace { Position = i, Character = c, Kind = kind, IsOpen = isOpen });
                }

                i++;
            }

            return raw;
        }

        /// <summary>Pass two: match pairs with a stack and derive each pair's identity.</summary>
        private static BraceInfo[] Pair(string text, List<RawBrace> raw, ScanSettings settings)
        {
            var result = new BraceInfo[raw.Count];
            var stack = new Stack<Pending>();
            var builder = new StringBuilder(MaxHeaderLength);

            // Flattened once for the whole file, not per brace: see TraitTable.
            var table = new TraitTable(settings.TraitWeights);

            for (int i = 0; i < raw.Count; i++)
            {
                RawBrace brace = raw[i];
                result[i] = new BraceInfo
                {
                    Position = brace.Position,
                    Character = brace.Character,
                    Kind = brace.Kind,
                    IsOpen = brace.IsOpen,
                    IsMatched = false,
                    Depth = stack.Count,
                    PartnerIndex = -1,

                    // Whatever is on top of the stack is by definition the innermost block
                    // still open here, which is exactly what encloses this brace. Recording it
                    // costs one field; recovering it afterwards costs a walk back through the
                    // file.
                    ParentIndex = stack.Count > 0 ? stack.Peek().Index : -1,
                };

                if (brace.IsOpen)
                {
                    int depth = stack.Count;
                    result[i].Identity = ComputeIdentity(text, brace.Position, brace.Kind, depth, builder);
                    stack.Push(new Pending { Index = i, Expected = CloserFor(brace.Kind) });
                }
                else if (stack.Count > 0 && stack.Peek().Expected == brace.Character)
                {
                    Pending open = stack.Pop();

                    result[open.Index].IsMatched = true;
                    result[i].IsMatched = true;

                    result[open.Index].PartnerIndex = i;
                    result[i].PartnerIndex = open.Index;

                    // A closer sits one level shallower than the body it closes, and its
                    // parent is its opener's parent — it is a sibling of its opener, not a
                    // child of it.
                    result[i].Depth = result[open.Index].Depth;
                    result[i].ParentIndex = result[open.Index].ParentIndex;

                    // Either the pair is one entity and the closer inherits everything, or
                    // the closer is its own person.
                    //
                    // Note the closer does NOT hash its own declaring line. A closing brace
                    // usually sits alone on one, so there is almost nothing to hash — every
                    // '}' in the file would collide on the same near-empty header. Deriving
                    // from the opener instead inherits all of its stability (survives edits
                    // elsewhere, reformatting, reopening the file) while guaranteeing the two
                    // halves never land on the same value.
                    result[i].Identity = settings.IndependentBraces
                        ? Hash.Mix(result[open.Index].Identity, CloserSalt)
                        : result[open.Index].Identity;
                }
                else
                {
                    // A stray closer. Do not unwind the stack — one typo should not
                    // recolour every brace below it.
                    result[i].Identity = ComputeIdentity(text, brace.Position, brace.Kind, stack.Count, builder);
                }
            }

            // Anything left on the stack never found a partner; IsMatched stays false.
            bool warnOnDepth = settings.ComplexityWarningDepth > 0;

            for (int i = 0; i < result.Length; i++)
            {
                // Depth mode indexes the palette directly. The palette's hues are spaced by
                // the golden angle, so consecutive depths — which are always adjacent on
                // screen — land far apart on the wheel rather than shading into each other.
                result[i].ColorIndex = settings.ColorByDepth
                    ? result[i].Depth % Classification.BracePalette.Count
                    : Hash.ToIndex(result[i].Identity, Classification.BracePalette.Count);

                result[i].Traits = TraitRoll.Roll(
                    result[i].Identity,
                    table,
                    settings.QuestionUnmatched && !result[i].IsMatched,
                    warnOnDepth && result[i].Depth >= settings.ComplexityWarningDepth);
            }

            return result;
        }

        /// <summary>
        /// The load-bearing decision of the whole extension.
        /// <para>
        /// Identity is the hash of the text that declares the brace — its line up to the
        /// brace, whitespace-normalised, falling back to the nearest preceding non-blank
        /// line so that Allman style (an opening brace alone on its own line) resolves to
        /// the signature above it.
        /// </para>
        /// <para>
        /// Hashing the position instead would reincarnate every brace below any inserted
        /// line. Hashing the enclosed body would change a block's identity as you type
        /// inside it. Hashing the declaration is stable across edits elsewhere, stable
        /// across sessions and machines, survives a reformat, and means "this method's
        /// braces" — which is the joke.
        /// </para>
        /// </summary>
        private static ulong ComputeIdentity(string text, int offset, BraceKind kind, int depth, StringBuilder builder)
        {
            BuildHeader(text, offset, builder);
            ulong hash = Hash.Fnv1a(builder);
            return Hash.Mix(hash, (ulong)depth * 31UL + (ulong)kind);
        }

        private static void BuildHeader(string text, int offset, StringBuilder builder)
        {
            int lineEnd = offset;

            for (int back = 0; back < MaxHeaderLookbackLines; back++)
            {
                int lineStart = LineStartAt(text, lineEnd);
                Normalize(text, lineStart, lineEnd, builder);
                if (builder.Length > 0 || lineStart == 0)
                {
                    return;
                }

                // Step back over the newline onto the previous line.
                lineEnd = lineStart - 1;
                if (lineEnd > 0 && text[lineEnd] == '\n' && text[lineEnd - 1] == '\r')
                {
                    lineEnd--;
                }

                if (lineEnd <= 0)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Copies [start, end) into the builder with all whitespace removed.
        /// </summary>
        /// <remarks>
        /// Dropping whitespace entirely rather than collapsing runs to a single space is
        /// what makes a colour survive a reformat: re-indenting, wrapping a long signature,
        /// or tightening <c>( )</c> to <c>()</c> all leave the identity untouched. The only
        /// collisions this can introduce are between declarations that differ by nothing but
        /// whitespace — which are the same declaration.
        /// </remarks>
        private static void Normalize(string text, int start, int end, StringBuilder builder)
        {
            builder.Length = 0;

            for (int i = start; i < end && builder.Length < MaxHeaderLength; i++)
            {
                char c = text[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    continue;
                }

                builder.Append(c);
            }
        }

        private static bool TryClassifyBrace(char c, out BraceKind kind, out bool isOpen)
        {
            switch (c)
            {
                case '{': kind = BraceKind.Curly; isOpen = true; return true;
                case '}': kind = BraceKind.Curly; isOpen = false; return true;
                case '(': kind = BraceKind.Round; isOpen = true; return true;
                case ')': kind = BraceKind.Round; isOpen = false; return true;
                case '[': kind = BraceKind.Square; isOpen = true; return true;
                case ']': kind = BraceKind.Square; isOpen = false; return true;
                default: kind = BraceKind.Curly; isOpen = false; return false;
            }
        }

        private static char CloserFor(BraceKind kind)
        {
            switch (kind)
            {
                case BraceKind.Curly: return '}';
                case BraceKind.Round: return ')';
                default: return ']';
            }
        }

        private static int LineStartAt(string text, int position)
        {
            for (int i = position - 1; i >= 0; i--)
            {
                if (text[i] == '\n')
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static int SkipToLineEnd(string text, int i)
        {
            while (i < text.Length && text[i] != '\n')
            {
                i++;
            }

            return i;
        }

        private static int SkipBlockComment(string text, int i)
        {
            while (i + 1 < text.Length)
            {
                if (text[i] == '*' && text[i + 1] == '/')
                {
                    return i + 2;
                }

                i++;
            }

            return text.Length;
        }

        /// <summary>
        /// Skips a double-quoted run starting at <paramref name="i"/>. Handles raw strings
        /// (three or more quotes, any number of lines) and verbatim strings (doubled-quote
        /// escapes). A plain string stops at end of line, because an unterminated one must
        /// not eat the rest of the file.
        /// </summary>
        private static int SkipQuoted(string text, int i, bool verbatim)
        {
            int n = text.Length;

            int quotes = 0;
            while (i + quotes < n && text[i + quotes] == '"')
            {
                quotes++;
            }

            if (quotes >= 3)
            {
                int j = i + quotes;
                while (j < n)
                {
                    if (text[j] == '"')
                    {
                        int run = 0;
                        while (j + run < n && text[j + run] == '"')
                        {
                            run++;
                        }

                        if (run >= quotes)
                        {
                            return j + run;
                        }

                        j += run;
                    }
                    else
                    {
                        j++;
                    }
                }

                return n;
            }

            if (quotes == 2)
            {
                return i + 2;
            }

            int k = i + 1;
            while (k < n)
            {
                char c = text[k];

                if (verbatim)
                {
                    if (c == '"')
                    {
                        if (k + 1 < n && text[k + 1] == '"')
                        {
                            k += 2;
                            continue;
                        }

                        return k + 1;
                    }

                    k++;
                    continue;
                }

                if (c == '\\')
                {
                    k += 2;
                    continue;
                }

                if (c == '"')
                {
                    return k + 1;
                }

                if (c == '\n')
                {
                    return k;
                }

                k++;
            }

            return n;
        }

        private static int SkipTemplate(string text, int i)
        {
            int n = text.Length;
            int k = i + 1;

            while (k < n)
            {
                char c = text[k];
                if (c == '\\')
                {
                    k += 2;
                    continue;
                }

                if (c == '`')
                {
                    return k + 1;
                }

                k++;
            }

            return n;
        }

        /// <summary>
        /// Returns the index past a single-quoted run, or -1 if it does not close on this
        /// line. The -1 case matters: Rust lifetimes and stray apostrophes look exactly like
        /// an opening quote, and treating them as one would blank the rest of the file.
        /// </summary>
        private static int TrySkipSingleQuoted(string text, int i)
        {
            int n = text.Length;
            int k = i + 1;

            while (k < n)
            {
                char c = text[k];
                if (c == '\\')
                {
                    k += 2;
                    continue;
                }

                if (c == '\n')
                {
                    return -1;
                }

                if (c == '\'')
                {
                    return k + 1;
                }

                k++;
            }

            return -1;
        }
    }
}
