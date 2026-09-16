using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using IdentityBraces.Core;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Classification
{
    /// <summary>
    /// Which brace pair the caret is inside, per buffer.
    /// </summary>
    /// <remarks>
    /// The same shape as <see cref="Adornments.AdornedBuffers"/>, and for the same reason: the
    /// tagger is created per <em>buffer</em> and has no view, while the caret belongs to a
    /// <em>view</em>. Rather than convert the tagger into a view tagger — which would change
    /// how every brace in the extension is classified to add one optional feature — a small
    /// per-view component publishes here and the tagger reads it.
    /// <para>
    /// A buffer open in two views has one scope, last caret to move wins. Split views showing
    /// the same file therefore agree with each other rather than each dimming the other's
    /// block, which is the more useful of the two behaviours.
    /// </para>
    /// </remarks>
    internal static class CaretScopes
    {
        private struct Scope
        {
            public int Start;
            public int End;
            public int Views;
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<ITextBuffer, Scope> Scopes = new Dictionary<ITextBuffer, Scope>();

        /// <summary>Raised when a buffer's scope changes, so taggers can re-tag.</summary>
        public static event Action<ITextBuffer> Changed;

        /// <summary>
        /// The span of the pair enclosing the caret, inclusive of both brace characters.
        /// </summary>
        public static bool TryGet(ITextBuffer buffer, out int start, out int end)
        {
            start = -1;
            end = -1;

            if (buffer == null)
            {
                return false;
            }

            lock (Gate)
            {
                Scope scope;
                if (!Scopes.TryGetValue(buffer, out scope) || scope.Start < 0)
                {
                    return false;
                }

                start = scope.Start;
                end = scope.End;
                return true;
            }
        }

        /// <summary>Records a new scope, notifying only when it actually moved.</summary>
        /// <remarks>
        /// The caret raises its change event for every arrow key and every keystroke. Without
        /// this comparison each of those would invalidate the tags for the visible region,
        /// which is a re-classification of the screen per character typed.
        /// </remarks>
        public static void Set(ITextBuffer buffer, int start, int end)
        {
            if (buffer == null)
            {
                return;
            }

            lock (Gate)
            {
                Scope scope;
                if (!Scopes.TryGetValue(buffer, out scope))
                {
                    return;
                }

                if (scope.Start == start && scope.End == end)
                {
                    return;
                }

                scope.Start = start;
                scope.End = end;
                Scopes[buffer] = scope;
            }

            Notify(buffer);
        }

        public static void Register(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            lock (Gate)
            {
                Scope scope;
                if (!Scopes.TryGetValue(buffer, out scope))
                {
                    scope = new Scope { Start = -1, End = -1, Views = 0 };
                }

                scope.Views++;
                Scopes[buffer] = scope;
            }
        }

        public static void Unregister(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            bool cleared = false;

            lock (Gate)
            {
                Scope scope;
                if (Scopes.TryGetValue(buffer, out scope))
                {
                    scope.Views--;
                    if (scope.Views <= 0)
                    {
                        cleared = scope.Start >= 0;
                        Scopes.Remove(buffer);
                    }
                    else
                    {
                        Scopes[buffer] = scope;
                    }
                }
            }

            if (cleared)
            {
                Notify(buffer);
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

    /// <summary>Where the caret is, in the terms a trait cares about.</summary>
    internal struct CaretInfo
    {
        /// <summary>Buffer offset of the caret.</summary>
        public int Position;

        /// <summary>Start of the line the caret is on.</summary>
        public int LineStart;

        /// <summary>End of that line, excluding its break.</summary>
        public int LineEnd;

        /// <summary>Column of the caret within its line.</summary>
        public int Column;

        public bool Contains(int position)
        {
            return position >= LineStart && position <= LineEnd;
        }
    }

    /// <summary>
    /// The raw caret position per buffer, for the traits that react to it.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="CaretScopes"/> because the two answer different questions on
    /// different schedules. The scope changes rarely — only when the caret crosses into another
    /// pair — and re-tagging on it is cheap. This changes on every arrow key, and
    /// <c>fleecursor</c> and <c>stagefright</c> need every one of them.
    /// <para>
    /// Nothing subscribes unless a caret-reactive trait is switched on, so with the shipped
    /// defaults this costs one comparison per keystroke and no work at all.
    /// </para>
    /// </remarks>
    internal static class Carets
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<ITextBuffer, CaretInfo> Positions =
            new Dictionary<ITextBuffer, CaretInfo>();

        /// <summary>Raised whenever the caret moves within a buffer.</summary>
        public static event Action<ITextBuffer> Moved;

        public static bool TryGet(ITextBuffer buffer, out CaretInfo caret)
        {
            caret = default(CaretInfo);

            if (buffer == null)
            {
                return false;
            }

            lock (Gate)
            {
                return Positions.TryGetValue(buffer, out caret);
            }
        }

        public static void Set(ITextBuffer buffer, CaretInfo caret)
        {
            if (buffer == null)
            {
                return;
            }

            lock (Gate)
            {
                CaretInfo existing;
                if (Positions.TryGetValue(buffer, out existing) && existing.Position == caret.Position)
                {
                    return;
                }

                Positions[buffer] = caret;
            }

            Action<ITextBuffer> handler = Moved;
            if (handler != null)
            {
                handler(buffer);
            }
        }

        public static void Clear(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            lock (Gate)
            {
                Positions.Remove(buffer);
            }
        }

        /// <summary>
        /// True when any trait that reads the caret is switched on.
        /// </summary>
        /// <remarks>
        /// The gate on all of this. Both traits default to zero, so the common case must be a
        /// couple of dictionary lookups and nothing else — no per-keystroke repositioning of
        /// every adornment on screen.
        /// </remarks>
        public static bool AnyReactiveTrait(IdentityBracesSettings settings)
        {
            return settings.GetTraitWeight(TraitIds.FleeCursor) > 0
                || settings.GetTraitWeight(TraitIds.StageFright) > 0;
        }
    }

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CaretScopeTrackerProvider : IWpfTextViewCreationListener
    {
        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView != null)
            {
                // Roots itself through the view's events and tears down on Closed, exactly
                // like the adornment manager.
                new CaretScopeTracker(textView);
            }
        }
    }

    /// <summary>
    /// Watches one view's caret and publishes the enclosing pair to <see cref="CaretScopes"/>.
    /// </summary>
    /// <remarks>
    /// Attached to every code view whether the spotlight is on or not, because a view created
    /// while the setting is off must start tracking the moment it is switched on, and a
    /// component that only exists conditionally cannot do that. The work when it is off is one
    /// comparison per caret move.
    /// </remarks>
    internal sealed class CaretScopeTracker
    {
        private readonly IWpfTextView _view;
        private readonly BraceMapCache _cache;

        public CaretScopeTracker(IWpfTextView view)
        {
            _view = view;
            _cache = BraceMapCache.GetOrCreate(view.TextBuffer);

            CaretScopes.Register(view.TextBuffer);

            _view.Caret.PositionChanged += OnCaretPositionChanged;
            _view.TextBuffer.ChangedLowPriority += OnBufferChanged;
            _view.Closed += OnClosed;
            IdentityBracesSettings.Changed += OnSettingsChanged;

            Update();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            Update();
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // The caret may not have moved, but the pair around it has: inserting a line above
            // shifts both of its braces, and typing a '}' can close the block the caret is in.
            Update();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            Update();
        }

        private void Update()
        {
            if (_view.IsClosed)
            {
                return;
            }

            IdentityBracesSettings settings = IdentityBracesSettings.Current;
            bool wantScope = settings.Enabled && settings.ScopeSpotlight;
            bool wantCaret = settings.Enabled && Carets.AnyReactiveTrait(settings);

            if (!wantScope)
            {
                CaretScopes.Set(_view.TextBuffer, -1, -1);
            }

            if (!wantCaret)
            {
                Carets.Clear(_view.TextBuffer);
            }

            if (!wantScope && !wantCaret)
            {
                return;
            }

            try
            {
                ITextSnapshot snapshot = _view.TextSnapshot;
                SnapshotPoint caret = _view.Caret.Position.BufferPosition;
                if (caret.Snapshot != snapshot)
                {
                    // Mid-edit the caret can still be reporting against the previous snapshot.
                    // Translating is cheap and keeps the lookup on one coordinate system.
                    caret = caret.TranslateTo(snapshot, PointTrackingMode.Positive);
                }

                if (wantCaret)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromPosition(caret.Position);
                    Carets.Set(_view.TextBuffer, new CaretInfo
                    {
                        Position = caret.Position,
                        LineStart = line.Start.Position,
                        LineEnd = line.End.Position,
                        Column = caret.Position - line.Start.Position,
                    });
                }

                if (!wantScope)
                {
                    return;
                }

                BraceMap map = _cache.Get(snapshot);

                int open;
                int close;
                if (map.Count > 0 && map.TryGetEnclosingPair(caret.Position, out open, out close))
                {
                    CaretScopes.Set(_view.TextBuffer, map[open].Position, map[close].Position);
                }
                else
                {
                    CaretScopes.Set(_view.TextBuffer, -1, -1);
                }
            }
            catch (ArgumentException)
            {
                // A snapshot that moved under us. The next caret move recomputes.
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.Caret.PositionChanged -= OnCaretPositionChanged;
            _view.TextBuffer.ChangedLowPriority -= OnBufferChanged;
            _view.Closed -= OnClosed;
            IdentityBracesSettings.Changed -= OnSettingsChanged;

            CaretScopes.Unregister(_view.TextBuffer);
            Carets.Clear(_view.TextBuffer);
        }
    }
}
