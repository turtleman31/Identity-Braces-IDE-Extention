using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>
    /// The two questions a scene director asks that are pure arithmetic: who can act, and is
    /// there room to act in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Split out of the director so it can be tested. Everything else a scene needs — line
    /// geometry, an adornment layer, a dispatcher — only exists inside a running editor, but
    /// deciding <em>whether</em> a brace can flip a table is a question about a string and a
    /// column number.
    /// </para>
    /// <para>
    /// This is the part that goes wrong quietly. A director that casts a brace with no room
    /// beside it throws a prop through the middle of somebody's code, and the failure looks
    /// like a rendering bug rather than a casting one.
    /// </para>
    /// </remarks>
    internal static class SceneCasting
    {
        /// <summary>
        /// Blank columns to the right of <paramref name="column"/>, up to <paramref name="limit"/>.
        /// </summary>
        /// <remarks>
        /// Everything past the end of the line counts as blank, which is not a special case so
        /// much as the main one: a closing brace usually sits at the end of its line, so the
        /// whole rest of the row is empty and it has the most room of anyone.
        /// </remarks>
        public static int RoomRightOf(string lineText, int column, int limit)
        {
            if (limit <= 0)
            {
                return 0;
            }

            // A line we cannot read is not a line we should throw anything across.
            if (lineText == null)
            {
                return 0;
            }

            int room = 0;

            for (int i = column + 1; room < limit; i++)
            {
                // Past the end of the text is open space, not a wall. This is the main case
                // rather than an edge one: a closing brace usually ends its line, so the whole
                // rest of the row is clear.
                if (i >= lineText.Length)
                {
                    return limit;
                }

                if (!IsBlank(lineText[i]))
                {
                    return room;
                }

                room++;
            }

            return room;
        }

        /// <summary>
        /// Blank columns to the left of <paramref name="column"/>, up to <paramref name="limit"/>.
        /// </summary>
        /// <remarks>
        /// Unlike the right, the start of the line is a hard wall: column zero is the edge of
        /// the text area, and a prop thrown past it would land in the margin among the line
        /// numbers and the breakpoint glyphs.
        /// </remarks>
        public static int RoomLeftOf(string lineText, int column, int limit)
        {
            if (limit <= 0 || lineText == null)
            {
                return 0;
            }

            int room = 0;

            for (int i = column - 1; i >= 0 && room < limit; i--)
            {
                if (i >= lineText.Length)
                {
                    room++;
                    continue;
                }

                if (!IsBlank(lineText[i]))
                {
                    return room;
                }

                room++;
            }

            return room;
        }

        /// <summary>
        /// Every brace in [start, end) carrying <paramref name="traitId"/> as an effect.
        /// </summary>
        /// <remarks>
        /// Bounded by the requested span rather than the file: the director only ever casts
        /// from what is on screen, because a scene played on a line nobody is looking at is
        /// just heat.
        /// </remarks>
        public static void FindCandidates(
            BraceMap map,
            string traitId,
            int start,
            int end,
            List<int> into)
        {
            if (into == null)
            {
                return;
            }

            into.Clear();

            if (map == null || traitId == null)
            {
                return;
            }

            for (int i = map.FirstIndexAtOrAfter(start); i < map.Count; i++)
            {
                BraceInfo brace = map[i];
                if (brace.Position >= end)
                {
                    return;
                }

                if (HasEffect(brace.Traits, traitId))
                {
                    into.Add(i);
                }
            }
        }

        public static bool HasEffect(BraceTraits traits, string traitId)
        {
            string[] effects = traits.Effects;
            if (effects == null || traitId == null)
            {
                return false;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                if (string.Equals(effects[i], traitId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Picks one of <paramref name="candidates"/> from a seed.
        /// </summary>
        /// <remarks>
        /// Deterministic rather than <c>Random</c> so a scene that misbehaves can be reproduced
        /// from the seed that produced it. The seed is the tick count, so it still varies.
        /// </remarks>
        public static int Choose(IList<int> candidates, ulong seed)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return -1;
            }

            return candidates[(int)(Hash.ToUnitInterval(Hash.Mix(seed, 0x5CE7EUL)) * candidates.Count) % candidates.Count];
        }

        private static bool IsBlank(char c)
        {
            return c == ' ' || c == '\t';
        }
    }
}
