using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Tracks which buffers currently have a live adornment manager.
    /// </summary>
    /// <remarks>
    /// This exists to enforce one safety property: <b>a brace must never be invisible</b>.
    /// <para>
    /// Personality braces are painted transparent by the classifier and redrawn by the
    /// adornment layer. If the adornment layer is not attached — a diff view, a peek window,
    /// an embedded editor with a different set of roles, or simply a view created before its
    /// manager — the transparent classification alone would blank the character out. So the
    /// tagger asks here first and falls back to a plain coloured tag when nothing is drawing.
    /// </para>
    /// <para>
    /// Refcounted, because several views can share one buffer.
    /// </para>
    /// </remarks>
    internal static class AdornedBuffers
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<ITextBuffer, int> Counts = new Dictionary<ITextBuffer, int>();

        /// <summary>Raised when a buffer starts or stops being adorned, so taggers can re-tag.</summary>
        public static event Action<ITextBuffer> Changed;

        public static void Register(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            bool transitioned;
            lock (Gate)
            {
                int count;
                transitioned = !Counts.TryGetValue(buffer, out count);
                Counts[buffer] = count + 1;
            }

            if (transitioned)
            {
                Notify(buffer);
            }
        }

        public static void Unregister(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            bool transitioned = false;
            lock (Gate)
            {
                int count;
                if (Counts.TryGetValue(buffer, out count))
                {
                    if (count <= 1)
                    {
                        Counts.Remove(buffer);
                        transitioned = true;
                    }
                    else
                    {
                        Counts[buffer] = count - 1;
                    }
                }
            }

            if (transitioned)
            {
                Notify(buffer);
            }
        }

        public static bool IsAdorned(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return false;
            }

            lock (Gate)
            {
                return Counts.ContainsKey(buffer);
            }
        }

        private static void Notify(ITextBuffer buffer)
        {
            Action<ITextBuffer> handler = Changed;
            if (handler != null)
            {
                handler(buffer);
            }
        }
    }
}
