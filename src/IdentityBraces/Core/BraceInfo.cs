namespace IdentityBraces.Core
{
    /// <summary>
    /// One brace, with the identity derived for it.
    /// </summary>
    /// <remarks>
    /// Whether a matched pair shares its <see cref="Identity"/>, <see cref="ColorIndex"/> and
    /// <see cref="Traits"/> depends on
    /// <see cref="ScanSettings.IndependentBraces"/>. Sharing them was the last thing here that
    /// still helped you read code, so it is off by default.
    /// </remarks>
    internal struct BraceInfo
    {
        /// <summary>Offset into the snapshot.</summary>
        public int Position;

        /// <summary>The literal character in the buffer.</summary>
        public char Character;

        public BraceKind Kind;

        public bool IsOpen;

        /// <summary>False when this brace has no partner — a real syntax error.</summary>
        public bool IsMatched;

        /// <summary>Stable hash of the pair's declaring text. See <see cref="BraceScanner"/>.</summary>
        public ulong Identity;

        /// <summary>Index into <see cref="Classification.BracePalette"/>.</summary>
        public int ColorIndex;

        /// <summary>
        /// How many pairs enclose this one. Zero at the outermost level.
        /// </summary>
        /// <remarks>
        /// A closing brace reports its <em>opener's</em> depth, not the depth of the position
        /// it sits at, so a pair agrees with itself. Without that, <c>{</c> at depth 2 would
        /// pair with a <c>}</c> at depth 3 and the two halves of one block would colour
        /// differently under <see cref="ScanSettings.ColorByDepth"/>.
        /// </remarks>
        public int Depth;

        /// <summary>
        /// Index of the matching brace in the same map, or -1 when there is none.
        /// </summary>
        /// <remarks>
        /// Set on both halves, so it is a genuine pairing rather than a forward link: given
        /// either brace you can reach the other in one step. The indent guides need the
        /// closer's position from the opener; the scope spotlight needs the opener's from the
        /// closer.
        /// </remarks>
        public int PartnerIndex;

        /// <summary>
        /// Index of the opening brace that encloses this one, or -1 at the outermost level.
        /// </summary>
        /// <remarks>
        /// The scan already has this on its stack, so recording it is free — and it turns
        /// "which pairs enclose this position?" from a walk back through the file into a walk
        /// up a chain that is as long as the nesting is deep. On a 240,000-brace file that is
        /// the difference between a quarter of a million iterations per caret move and about
        /// ten.
        /// <para>
        /// It can point at an <em>unmatched</em> opener — one that never found its partner
        /// stays on the stack and legitimately encloses everything after it. Callers wanting a
        /// real pair must skip those; see <see cref="BraceMap.TryGetEnclosingPair"/>.
        /// </para>
        /// </remarks>
        public int ParentIndex;

        /// <summary>What this brace turned out to be, across every layer.</summary>
        public BraceTraits Traits;

        /// <summary>True when this brace is drawn by the adornment layer rather than by classification.</summary>
        public bool IsAdorned
        {
            get { return Traits.IsDrawn; }
        }
    }
}
