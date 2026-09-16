using System;

namespace IdentityBraces.Core
{
    /// <summary>Every brace in one snapshot, ordered by position.</summary>
    internal sealed class BraceMap
    {
        public static readonly BraceMap Empty = new BraceMap(new BraceInfo[0]);

        private readonly BraceInfo[] _braces;

        public BraceMap(BraceInfo[] braces)
        {
            _braces = braces ?? new BraceInfo[0];
        }

        public int Count
        {
            get { return _braces.Length; }
        }

        public BraceInfo this[int index]
        {
            get { return _braces[index]; }
        }

        /// <summary>
        /// Index of the first brace at or after <paramref name="position"/>, or
        /// <see cref="Count"/> if there is none. Binary search: the tagger asks this once
        /// per requested span and then walks forward.
        /// </summary>
        public int FirstIndexAtOrAfter(int position)
        {
            int lo = 0;
            int hi = _braces.Length;

            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (_braces[mid].Position < position)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        /// <summary>
        /// The innermost matched pair whose span contains <paramref name="position"/>, with
        /// both brace characters counting as inside it.
        /// </summary>
        /// <remarks>
        /// Runs in time proportional to the <em>nesting depth</em>, not the file: one binary
        /// search to find the brace the caret is sitting against, then a walk up
        /// <see cref="BraceInfo.ParentIndex"/>. The caret moves on every keystroke and every
        /// arrow key, so a scan back through a quarter of a million braces would be felt.
        /// </remarks>
        public bool TryGetEnclosingPair(int position, out int openIndex, out int closeIndex)
        {
            openIndex = -1;
            closeIndex = -1;

            int j = FirstIndexAtOrAfter(position);
            if (j >= _braces.Length)
            {
                // Nothing at or after the caret, so no pair can still be open across it.
                return false;
            }

            BraceInfo next = _braces[j];

            // The first brace at or after the caret is a closer: the caret is inside the
            // block that closer ends, because its opener necessarily precedes the caret.
            // Likewise a brace sitting exactly under the caret belongs to the pair the caret
            // is on, which is what makes clicking a brace select its own pair rather than
            // its parent.
            if (next.IsMatched && (!next.IsOpen || next.Position == position))
            {
                openIndex = next.IsOpen ? j : next.PartnerIndex;
                closeIndex = next.IsOpen ? next.PartnerIndex : j;
                return openIndex >= 0 && closeIndex >= 0;
            }

            return TryWalkOut(next.ParentIndex, out openIndex, out closeIndex);
        }

        /// <summary>
        /// The next matched pair outward from <paramref name="openIndex"/>, for peeling off
        /// one nesting level at a time.
        /// </summary>
        public bool TryGetParentPair(int openIndex, out int parentOpen, out int parentClose)
        {
            parentOpen = -1;
            parentClose = -1;

            if (openIndex < 0 || openIndex >= _braces.Length)
            {
                return false;
            }

            return TryWalkOut(_braces[openIndex].ParentIndex, out parentOpen, out parentClose);
        }

        /// <summary>
        /// Follows the parent chain outward until it reaches an opener that actually found a
        /// partner.
        /// </summary>
        /// <remarks>
        /// An opener that never closed stays on the scanner's stack for the rest of the file,
        /// so it is the recorded parent of everything below it while not being a pair at all.
        /// Skipping those is what keeps one unbalanced brace from erasing the spotlight for
        /// the whole file below it.
        /// </remarks>
        private bool TryWalkOut(int index, out int openIndex, out int closeIndex)
        {
            openIndex = -1;
            closeIndex = -1;

            while (index >= 0 && index < _braces.Length)
            {
                BraceInfo candidate = _braces[index];
                if (candidate.IsMatched && candidate.PartnerIndex >= 0)
                {
                    openIndex = index;
                    closeIndex = candidate.PartnerIndex;
                    return true;
                }

                index = candidate.ParentIndex;
            }

            return false;
        }

        /// <summary>
        /// True if any brace in [start, end) wants extra headroom above its line — which
        /// today means a catgirl brace, whose ears live above the text.
        /// </summary>
        public bool NeedsHeadroom(int start, int end)
        {
            for (int i = FirstIndexAtOrAfter(start); i < _braces.Length; i++)
            {
                if (_braces[i].Position >= end)
                {
                    return false;
                }

                if (_braces[i].Traits.Creature != null || _braces[i].Traits.Costume != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
