using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using IdentityBraces.Classification;
using IdentityBraces.Core;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Adornments
{
    /// <summary>Declares the layer the personalities are drawn on.</summary>
    internal static class IdentityBracesLayer
    {
        public const string LayerName = "IdentityBraces";

#pragma warning disable 649
        // Above the text so ears and stockings are not occluded, below the caret so the
        // caret stays visible on top of a drawn glyph.
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(After = PredefinedAdornmentLayers.Text, Before = PredefinedAdornmentLayers.Caret)]
        internal static AdornmentLayerDefinition Definition;
#pragma warning restore 649
    }

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class AdornmentManagerProvider : IWpfTextViewCreationListener
    {
        [Import]
        internal IClassificationFormatMapService FormatMapService = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView != null)
            {
                // The manager roots itself through the view's events and tears down on Closed.
                new AdornmentManager(textView, FormatMapService);
            }
        }
    }

    /// <summary>
    /// Draws the personality braces for one view.
    /// </summary>
    internal sealed class AdornmentManager
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private readonly IClassificationFormatMap _formatMap;
        private readonly BraceMapCache _cache;
        // Keyed by buffer position so a brace cannot end up with two adornments, and so a
        // visible brace that is missing one can be spotted cheaply.
        //
        // This is bookkeeping, NOT the source of truth. The correction pass drives off
        // _layer.Elements instead — see RepositionAll.
        private readonly Dictionary<int, BraceVisual> _byPosition = new Dictionary<int, BraceVisual>();

        private bool _paused;

        // The caret's enclosing pair, and the mask that fades everything outside it.
        private bool _spotlight;
        private int _scopeStart;
        private int _scopeEnd;
        private int _dimPercent = -1;

        // Alpha is quantised to a byte, so a handful of brushes covers every combination of
        // spotlight dimming and stage fright that can arise.
        private readonly Dictionary<byte, Brush> _masks = new Dictionary<byte, Brush>();

        private bool _caretActive;
        private bool _caretKnown;
        private CaretInfo _caret;

        // The layer's canvas, and where it currently sits inside the text view. Measured
        // rather than assumed — see UpdateLayerOffset.
        private FrameworkElement _layerElement;
        private Point _layerOffset;
        private bool _repassQueued;

        // Per-layout counters and samples, for the diagnostic log only.
        private int _repositioned;
        private int _removed;
        private int _created;
        private double _minScreenY;
        private double _maxScreenY;
        private BraceVisual _pendingLandedVisual;
        private ITextViewLine _pendingLandedLine;
        private TextBounds _pendingLandedBounds;
        private bool _fullSweep;
        private int _layoutCount;
        private readonly List<int> _landedYs = new List<int>();
        private readonly List<int> _intendedYs = new List<int>();
        private readonly List<char> _landedKinds = new List<char>();
        private readonly List<int> _screenYs = new List<int>();
        private readonly List<int> _glyphYs = new List<int>();

        // Why a brace that the classifier hid did not get an adornment drawn for it.
        private int _seenAdorned;
        private int _skipDuplicate;
        private int _skipBounds;
        private int _skipZeroWidth;
        private int _skipNullVisual;
        private int _skipAddFailed;

        public AdornmentManager(IWpfTextView view, IClassificationFormatMapService formatMapService)
        {
            _view = view;
            _layer = view.GetAdornmentLayer(IdentityBracesLayer.LayerName);
            _formatMap = formatMapService.GetClassificationFormatMap(view);
            _cache = BraceMapCache.GetOrCreate(view.TextBuffer);

            // Tell the tagger it is safe to paint these braces transparent.
            AdornedBuffers.Register(view.TextBuffer);

            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;
            _view.VisualElement.IsVisibleChanged += OnIsVisibleChanged;
            _formatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;
            IdentityBracesSettings.Changed += OnSettingsChanged;
            CaretScopes.Changed += OnCaretScopeChanged;
            Carets.Moved += OnCaretMoved;
            BuildStatus.Changed += OnBuildStatusChanged;

            // Lazily advised here rather than from package initialisation: this extension has
            // no auto-load on purpose, and a view being created is both on the UI thread and a
            // sign that somebody is actually looking at code. IWpfTextViewCreationListener is
            // documented as running on the UI thread, which is what the assertion states.
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IdentityBracesSettings.Current.GetTraitWeight(TraitIds.BuildReactive) > 0)
            {
                BuildStatus.EnsureListening();
            }

            RefreshScope();

            Diagnostics.Log(
                "manager created  view=#{0}  layer={1}  roles=[{2}]  buffer=#{3}",
                _view.GetHashCode(),
                _layer == null ? "NULL" : "#" + _layer.GetHashCode(),
                string.Join(",", new List<string>(_view.Roles).ToArray()),
                _view.TextBuffer.GetHashCode());
        }

        /// <remarks>
        /// Correct first, then create. The correction pass evicts anything that can no longer
        /// be placed, so creation never has to reason about leftovers.
        /// <para>
        /// Elements are added <see cref="AdornmentPositioningBehavior.OwnerControlled"/>: this
        /// manager owns their coordinates outright and rewrites them every pass, so there is no
        /// implicit layer behaviour left to misunderstand. They are repositioned in place
        /// rather than rebuilt, so animations keep running instead of restarting each tick.
        /// </para>
        /// </remarks>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (!IdentityBracesSettings.Current.Enabled)
            {
                return;
            }

            RefreshScope();

            int before = _layer.Elements.Count;
            _repositioned = 0;
            _removed = 0;
            _created = 0;
            _minScreenY = 0;
            _maxScreenY = 0;
            _pendingLandedVisual = null;

            // Sweep every few layouts: measuring all elements walks the visual tree per
            // element, which is fine occasionally and wasteful every frame.
            _layoutCount++;
            _fullSweep = Diagnostics.Enabled && (_layoutCount % 5 == 0);
            _landedYs.Clear();
            _intendedYs.Clear();
            _landedKinds.Clear();
            _screenYs.Clear();
            _glyphYs.Clear();
            _seenAdorned = 0;
            _skipDuplicate = 0;
            _skipBounds = 0;
            _skipZeroWidth = 0;
            _skipNullVisual = 0;
            _skipAddFailed = 0;

            RepositionAll();

            if (_fullSweep && _landedYs.Count > 0)
            {
                // Tagged by personality: Questioning and Animated must be separable from
                // Catgirl. At a 60% catgirl rate an aggregate sweep is ~70% catgirl, so if
                // only the other two misplace, the aggregate still looks healthy — which is
                // exactly how this bug stayed hidden through several rounds of measurement.
                Diagnostics.Log(
                    "  SWEEP n={0} distinctY={1}  landed={2}",
                    _landedYs.Count,
                    CountDistinct(_landedYs),
                    JoinTagged(_landedKinds, _landedYs));
                Diagnostics.Log("            intended={0}", JoinTagged(_landedKinds, _intendedYs));
                Diagnostics.Log("            SCREEN ={0}   distinctScreenY={1}", JoinTagged(_landedKinds, _screenYs), CountDistinct(_screenYs));
                Diagnostics.Log("            GLYPH  ={0}   distinctGlyphY={1}", JoinTagged(_landedKinds, _glyphYs), CountDistinct(_glyphYs));
                Diagnostics.Log("            byKind: {0}", PerKindSummary());
            }

            // Logged after the whole pass so the spread covers every element, not just the
            // ones seen before the sample was taken.
            if (_pendingLandedVisual != null)
            {
                LogWhereItActuallyLanded(_pendingLandedVisual, _pendingLandedLine, _pendingLandedBounds);
                _pendingLandedVisual = null;
            }

            // Creation runs after the correction pass, over every visible line rather than
            // only the reformatted ones. Duplicates are cheap to reject, and a brace that
            // scrolled back into view without being reported as reformatted still gets one.
            IWpfTextViewLineCollection lines = TryGetLines();
            if (lines != null)
            {
                foreach (ITextViewLine line in lines)
                {
                    CreateVisuals(line);
                }
            }

            Diagnostics.Log(
                "layout view=#{0} vpTop={1:F1} newLines={2} transLines={3} linesNull={4} "
                + "layerBefore={5} layerAfter={6} tracked={7} repositioned={8} removed={9} created={10}",
                _view.GetHashCode(),
                _view.ViewportTop,
                CountOf(e.NewOrReformattedLines),
                CountOf(e.TranslatedLines),
                lines == null,
                before,
                _layer.Elements.Count,
                _byPosition.Count,
                _repositioned,
                _removed,
                _created);

            // Every brace the classifier hid must get a glyph drawn for it. A shortfall here
            // means invisible braces on screen, which is a far worse failure than a
            // mispositioned one and looks nothing like it.
            int drawn = _skipDuplicate + _created;
            if (_seenAdorned != drawn)
            {
                Diagnostics.Log(
                    "  UNDRAWN {0} of {1} hidden braces got no glyph!  dup={2} created={3} "
                    + "boundsThrew={4} zeroWidth={5} nullVisual={6} addFailed={7}",
                    _seenAdorned - drawn,
                    _seenAdorned,
                    _skipDuplicate,
                    _created,
                    _skipBounds,
                    _skipZeroWidth,
                    _skipNullVisual,
                    _skipAddFailed);
            }

            // The layer moves its own canvas after this handler returns; correct against the
            // settled offset before the frame is drawn.
            QueueSettledRepass();
        }

        private static string JoinTagged(List<char> kinds, List<int> values)
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(kinds[i]).Append(values[i]);
            }

            return builder.ToString();
        }

        /// <summary>Per-personality: how many, and how many distinct Y values they landed on.</summary>
        private string PerKindSummary()
        {
            var builder = new System.Text.StringBuilder();

            foreach (char kind in new[] { 'A', 'Q', 'C' })
            {
                var ys = new List<int>();
                int wrong = 0;

                for (int i = 0; i < _landedKinds.Count; i++)
                {
                    if (_landedKinds[i] == kind)
                    {
                        ys.Add(_landedYs[i]);
                        if (_landedYs[i] != _intendedYs[i])
                        {
                            wrong++;
                        }
                    }
                }

                if (ys.Count == 0)
                {
                    continue;
                }

                builder.Append(kind).Append("(n=").Append(ys.Count)
                       .Append(" distinctY=").Append(CountDistinct(ys))
                       .Append(" misplaced=").Append(wrong).Append(") ");
            }

            return builder.ToString();
        }

        private static int CountDistinct(List<int> values)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < values.Count; i++)
            {
                seen.Add(values[i]);
            }

            return seen.Count;
        }

        private static string Join(List<int> values)
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(values[i]);
            }

            return builder.ToString();
        }

        private static int CountOf(System.Collections.Generic.IEnumerable<ITextViewLine> lines)
        {
            int count = 0;
            if (lines != null)
            {
                foreach (ITextViewLine unused in lines)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Re-derives the position of everything the layer is actually rendering, and evicts
        /// anything that can no longer be placed.
        /// </summary>
        /// <remarks>
        /// <b>This drives off <see cref="IAdornmentLayer.Elements"/>, not off our own
        /// dictionary, and that distinction is the whole point.</b> An earlier version walked
        /// the manager's own bookkeeping, so any element that made it into the layer but fell
        /// out of that collection became invisible to the correction pass and could never be
        /// fixed — it simply sat at a stale position for the rest of the session. The layer's
        /// own element list is the authoritative answer to "what is on screen", so nothing can
        /// hide from this.
        /// <para>
        /// An adornment whose brace is no longer visible is <em>removed</em> rather than
        /// skipped. Leaving it in the layer is what let stranded glyphs keep painting at
        /// coordinates that no longer meant anything.
        /// </para>
        /// <para>
        /// Each element is guarded individually. Wrapping the whole loop in one try meant a
        /// single bad adornment aborted the pass and every element after it kept its stale
        /// position too.
        /// </para>
        /// </remarks>
        private void RepositionAll()
        {
            if (_view.IsClosed || _view.InLayout || _layer.Elements.Count == 0)
            {
                Diagnostics.Log(
                    "  reposition SKIPPED  closed={0} inLayout={1} layerCount={2}",
                    _view.IsClosed,
                    _view.InLayout,
                    _layer.Elements.Count);
                return;
            }

            IWpfTextViewLineCollection lines = TryGetLines();
            if (lines == null)
            {
                Diagnostics.Log("  reposition SKIPPED  TextViewLines unavailable");
                return;
            }

            // Re-measure before placing anything: the layer moves as the view scrolls.
            UpdateLayerOffset();

            // Then evict anything the layer has forgotten but is still drawing. Must happen
            // before repositioning, so the pass below is not competing with stale visuals
            // sitting at coordinates nobody is maintaining.
            PurgeOrphans();

            ITextSnapshot snapshot = _view.TextSnapshot;
            BraceMap map = _cache.Get(snapshot);

            // Copy first: removing from the layer mutates this collection.
            var elements = new IAdornmentLayerElement[_layer.Elements.Count];
            _layer.Elements.CopyTo(elements, 0);

            List<UIElement> stale = null;

            for (int i = 0; i < elements.Length; i++)
            {
                var visual = elements[i].Tag as BraceVisual;
                if (visual == null)
                {
                    continue;
                }

                try
                {
                    ITextViewLine line = null;

                    if (visual.Position >= 0 && visual.Position < snapshot.Length)
                    {
                        var point = new SnapshotPoint(snapshot, visual.Position);
                        line = lines.GetTextViewLineContainingBufferPosition(point);

                        if (line != null && line.IsValid && StillAdorned(map, visual.Position))
                        {
                            TextBounds bounds = line.GetCharacterBounds(point);
                            ApplyFlee(visual, bounds.Width);
                            Place(visual, line, bounds);
                            ApplyScope(visual);
                            _repositioned++;

                            // Track the on-screen spread: if every glyph is landing on one
                            // row, min and max collapse to the same value and say so directly.
                            double screenY = line.TextTop - visual.TopOffset - _view.ViewportTop;
                            if (_repositioned == 1 || screenY < _minScreenY)
                            {
                                _minScreenY = screenY;
                            }

                            if (_repositioned == 1 || screenY > _maxScreenY)
                            {
                                _maxScreenY = screenY;
                            }

                            if (_repositioned == 1)
                            {
                                _pendingLandedVisual = visual;
                                _pendingLandedLine = line;
                                _pendingLandedBounds = bounds;
                            }

                            // Measure EVERY element, not just the first. Sampling one was what
                            // let "all glyphs on one row" read as healthy: a single correct
                            // element plus a wide intended spread looks identical to the bug.
                            if (_fullSweep && _landedYs.Count < 60)
                            {
                                try
                                {
                                    Point at = visual.Element
                                        .TransformToAncestor(_view.VisualElement)
                                        .Transform(new Point(0, 0));
                                    _landedYs.Add((int)Math.Round(at.Y));
                                    _intendedYs.Add((int)Math.Round(screenY));
                                    _landedKinds.Add(visual.Kind == null ? '.' : visual.Kind[0]);

                                    // True desktop coordinates. TransformToAncestor reports a
                                    // position relative to the view, which has agreed with
                                    // intent for several rounds while the glyphs were visibly
                                    // stacked — so some transform between the element and the
                                    // screen is unaccounted for. PointToScreen cannot be
                                    // fooled by one.
                                    Point onScreen = visual.Element.PointToScreen(new Point(0, 0));
                                    _screenYs.Add((int)Math.Round(onScreen.Y));

                                    // The GLYPH, not its container. Measuring the container
                                    // reported correct, spread positions for ten rounds while
                                    // the glyphs inside were collapsing onto one row — the
                                    // inner offset cancelled the container's placement, and no
                                    // probe aimed at the container could ever see it.
                                    var host = visual.Element as Canvas;
                                    if (host != null && host.Children.Count > 0)
                                    {
                                        Point glyphAt = host.Children[0].PointToScreen(new Point(0, 0));
                                        _glyphYs.Add((int)Math.Round(glyphAt.Y));
                                    }
                                    else
                                    {
                                        _glyphYs.Add(int.MinValue);
                                    }
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }

                            continue;
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (InvalidOperationException)
                {
                }

                if (stale == null)
                {
                    stale = new List<UIElement>();
                }

                stale.Add(elements[i].Adornment);
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                _layer.RemoveAdornment(stale[i]);
                _removed++;
            }
        }

        /// <summary>
        /// Reports where an adornment actually ended up on screen, versus where we asked.
        /// </summary>
        /// <remarks>
        /// Every previous round of instrumentation measured the <em>input</em> — the
        /// coordinates being written and read back — and every round it was correct while the
        /// glyphs were still visibly wrong. That proves the mistake is in the model of what
        /// the adornment layer's coordinate space means, which only an <em>output</em>
        /// measurement can settle: walking the visual tree gives the element's real offset
        /// inside the text view, and the delta against the requested position is the
        /// transform being applied on top of us.
        /// </remarks>
        private void LogWhereItActuallyLanded(BraceVisual visual, ITextViewLine line, TextBounds bounds)
        {
            if (!Diagnostics.Enabled)
            {
                return;
            }

            double wroteTop = line.TextTop - visual.TopOffset;
            double expectedScreenY = wroteTop - _view.ViewportTop;

            try
            {
                Point actual = visual.Element.TransformToAncestor(_view.VisualElement).Transform(new Point(0, 0));

                DependencyObject parent = VisualTreeHelper.GetParent(visual.Element);
                var parentElement = parent as FrameworkElement;
                double parentY = double.NaN;
                string parentTransform = "none";

                if (parentElement != null)
                {
                    parentY = parentElement.TransformToAncestor(_view.VisualElement)
                                           .Transform(new Point(0, 0)).Y;

                    if (parentElement.RenderTransform != null)
                    {
                        parentTransform = parentElement.RenderTransform.Value.ToString(
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                Diagnostics.Log(
                    "  LANDED pos={0}  wroteTop={1:F1}  expectedScreenY={2:F1}  actualY={3:F1}  "
                    + "ERROR={4:F1}  spread=[{5:F0}..{6:F0}]  parent={7} parentY={8:F1} xform={9}  "
                    + "vpTop={10:F1} vpHeight={11:F1} zoom={12:F0}%",
                    visual.Position,
                    wroteTop,
                    expectedScreenY,
                    actual.Y,
                    actual.Y - expectedScreenY,
                    _minScreenY,
                    _maxScreenY,
                    parent == null ? "NULL" : parent.GetType().Name,
                    parentY,
                    parentTransform,
                    _view.ViewportTop,
                    _view.ViewportHeight,
                    _view.ZoomLevel);
            }
            catch (InvalidOperationException)
            {
                Diagnostics.Log(
                    "  LANDED pos={0} wroteTop={1:F1}  NOT IN VISUAL TREE",
                    visual.Position,
                    wroteTop);
            }
        }

        /// <summary>
        /// True if the brace this adornment was drawn for is still there and still wants one.
        /// </summary>
        /// <remarks>
        /// Needed because <see cref="AdornmentPositioningBehavior.OwnerControlled"/> means the
        /// layer no longer evicts adornments when their text is edited. Without this check, a
        /// brace that was typed away — or whose personality changed — would leave its glyph
        /// hanging over whatever character now occupies that offset.
        /// </remarks>
        private static bool StillAdorned(BraceMap map, int position)
        {
            int index = map.FirstIndexAtOrAfter(position);
            return index < map.Count
                && map[index].Position == position
                && map[index].IsAdorned;
        }

        private IWpfTextViewLineCollection TryGetLines()
        {
            try
            {
                return _view.InLayout ? null : _view.TextViewLines;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Places an adornment, converting the editor's coordinates into the layer's.
        /// </summary>
        /// <remarks>
        /// <b><see cref="ITextViewLine.TextTop"/> and <see cref="TextBounds"/> are absolute
        /// document coordinates, not viewport coordinates.</b> Measured directly: one brace
        /// reported <c>TextTop=647</c> unchanged while the viewport scrolled from 91 to 672.
        /// The adornment layer's canvas is viewport space, so the viewport offset has to come
        /// off both axes.
        /// <para>
        /// Writing the absolute value straight into <c>Canvas.Top</c> pinned every glyph to a
        /// screen position equal to its document offset: braces near the start of the file sat
        /// at the top of the window permanently and never moved when scrolling, and braces
        /// deeper in the file rendered past the bottom edge and were never seen at all. The horizontal
        /// axis hid the bug, because <c>ViewportLeft</c> is 0 whenever the view is not
        /// scrolled sideways, so left happened to be right.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Places an adornment in the layer's own coordinate space, which is text space.
        /// </summary>
        /// <remarks>
        /// Under <see cref="AdornmentPositioningBehavior.OwnerControlled"/> the layer's canvas
        /// carries an offset of <c>-ViewportTop</c>, applied at render time. So the position
        /// that lands correctly on screen is simply the line's own text coordinate:
        /// <code>
        /// renderY = layerOffset + Canvas.Top
        ///         = -viewportTop  + (textTop - topOffset)
        ///         =  textTop - topOffset - viewportTop     // exactly right, for any scroll
        /// </code>
        /// The viewport term cancels, which is the important part: this expression contains no
        /// scroll-dependent value at all, so it cannot go stale between being written and
        /// being rendered.
        /// <para>
        /// Subtracting <c>ViewportTop</c> here double-counted the scroll and pushed glyphs off
        /// the bottom. Subtracting a <em>measured</em> layer offset was closer but still wrong:
        /// the layer's offset lags the viewport by one scroll step mid-gesture, and baking that
        /// stale value in produced a transient jump of exactly that step — three lines at 112%
        /// zoom — that corrected itself once scrolling stopped.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Places an adornment in text coordinates, which is the layer's own space.
        /// </summary>
        /// <remarks>
        /// Measured: the layer's canvas carries an offset of <c>-ViewportTop</c>, so
        /// <code>
        /// renderY = layerOffset + Canvas.Top = -viewportTop + (textTop - topOffset)
        /// </code>
        /// lands correctly at any scroll position — and contains no viewport term of its own,
        /// which is the property that matters. The layer updates its offset after layout
        /// handlers return, so any expression here that mentions the viewport is stale by the
        /// time it renders. Subtracting <c>ViewportTop</c> double-counted the scroll;
        /// subtracting a measured layer offset baked in a value one scroll step behind, and
        /// showed up as the whole set of glyphs displaced by a constant multiple of the line
        /// height in 57 of 97 sampled frames. This expression cannot go stale because there is
        /// nothing scroll-dependent in it to go stale.
        /// </remarks>
        private static void Place(BraceVisual visual, ITextViewLine line, TextBounds bounds)
        {
            // FleeOffsetX is zero for all but a handful of braces and only while the caret is
            // beside them, so this is the same expression it always was in the common case.
            Canvas.SetLeft(visual.Element, bounds.Left + visual.FleeOffsetX);
            Canvas.SetTop(visual.Element, line.TextTop - visual.TopOffset);
        }

        /// <summary>
        /// Re-places everything once more at render priority, after the layer has settled.
        /// </summary>
        /// <remarks>
        /// The adornment layer updates its own offset <em>after</em> the LayoutChanged
        /// handlers run: measured mid-scroll at <c>parentY=445</c> while
        /// <c>ViewportTop=-397</c>, converging one event later. Placing against the offset as
        /// it stands during the handler therefore bakes in a value that is one scroll step
        /// stale, and the glyphs render displaced by exactly that step until the gesture stops.
        /// <para>
        /// Running a second, placement-only pass at <see cref="DispatcherPriority.Render"/>
        /// re-measures after the layer has moved but before the frame is drawn, so the
        /// displacement never reaches the screen. It creates and removes nothing.
        /// </para>
        /// </remarks>
        private void QueueSettledRepass()
        {
            if (_repassQueued || _view.IsClosed)
            {
                return;
            }

            _repassQueued = true;

            // VSTHRD001: dispatcher priority is precisely the point here — the work has to run
            // after the layer repositions itself but before the frame is drawn, which is what
            // Render priority means. Switching to the main thread is not the goal; we are
            // already on it, inside a layout handler.
            // VSTHRD110: deliberately fire-and-forget. Nothing waits on a repaint, and the
            // next layout pass corrects anything this one misses.
#pragma warning disable VSTHRD001
#pragma warning disable VSTHRD110
            _view.VisualElement.Dispatcher.BeginInvoke(
                new Action(RunSettledRepass),
                DispatcherPriority.Render);
#pragma warning restore VSTHRD110
#pragma warning restore VSTHRD001
        }

        private void RunSettledRepass()
        {
            _repassQueued = false;

            if (_view.IsClosed || _view.InLayout || _layer.Elements.Count == 0)
            {
                return;
            }

            IWpfTextViewLineCollection lines = TryGetLines();
            if (lines == null)
            {
                return;
            }

            Point previous = _layerOffset;
            UpdateLayerOffset();

            if (_layerOffset == previous)
            {
                return;
            }

            ITextSnapshot snapshot = _view.TextSnapshot;

            var elements = new IAdornmentLayerElement[_layer.Elements.Count];
            _layer.Elements.CopyTo(elements, 0);

            for (int i = 0; i < elements.Length; i++)
            {
                var visual = elements[i].Tag as BraceVisual;
                if (visual == null || visual.Position < 0 || visual.Position >= snapshot.Length)
                {
                    continue;
                }

                try
                {
                    var point = new SnapshotPoint(snapshot, visual.Position);
                    ITextViewLine line = lines.GetTextViewLineContainingBufferPosition(point);

                    if (line != null && line.IsValid)
                    {
                        Place(visual, line, line.GetCharacterBounds(point));
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }

            Diagnostics.Log(
                "  settled re-pass: layerOffset {0:F1} -> {1:F1}  (vpTop={2:F1})",
                previous.Y,
                _layerOffset.Y,
                _view.ViewportTop);
        }

        /// <summary>
        /// Removes visuals still parented to the layer's canvas that the layer no longer owns.
        /// </summary>
        /// <remarks>
        /// <b>An adornment can be dropped from <see cref="IAdornmentLayer.Elements"/> while its
        /// UI element stays a child of the layer's canvas.</b> Such an orphan is still
        /// rendered, but it is invisible to every correction pass — those all iterate
        /// <c>Elements</c> — so it keeps the coordinates it was given when it was created, for
        /// the life of the view.
        /// <para>
        /// That is the bug that survived every positioning fix. The counts gave it away: 151
        /// adornments created while the layer never held more than 16. Because an orphan keeps
        /// its creation-time position, and most are created during the first layout, they
        /// collect on whichever line was on screen when the file opened — with their
        /// horizontal position perfectly correct, because only the vertical coordinate ever
        /// needed updating.
        /// </para>
        /// </remarks>
        private void PurgeOrphans()
        {
            if (_layerElement == null)
            {
                return;
            }

            // VisualTreeHelper, not a cast to Canvas. The previous version cast the layer to
            // Canvas and bailed out silently when that returned null, so a zero purge count
            // was indistinguishable from the check never running at all.
            int childCount = VisualTreeHelper.GetChildrenCount(_layerElement);
            int owned = _layer.Elements.Count;

            if (_fullSweep)
            {
                Diagnostics.Log(
                    "  LAYERTREE type={0} visualChildren={1} layerElements={2} isPanel={3}",
                    _layerElement.GetType().Name,
                    childCount,
                    owned,
                    _layerElement is Panel);
            }

            var panel = _layerElement as Panel;
            if (panel == null || childCount <= owned)
            {
                return;
            }

            var live = new HashSet<UIElement>();
            for (int i = 0; i < _layer.Elements.Count; i++)
            {
                live.Add(_layer.Elements[i].Adornment);
            }

            var orphans = new List<UIElement>();
            for (int i = 0; i < panel.Children.Count; i++)
            {
                UIElement child = panel.Children[i];
                if (!live.Contains(child))
                {
                    orphans.Add(child);
                }
            }

            for (int i = 0; i < orphans.Count; i++)
            {
                panel.Children.Remove(orphans[i]);
            }

            Diagnostics.Log(
                "  PURGED {0} orphaned visuals  (visualChildren={1} layerElements={2})",
                orphans.Count,
                childCount,
                owned);
        }

        /// <summary>
        /// Measures where the adornment layer's canvas currently sits inside the text view.
        /// </summary>
        /// <remarks>
        /// <b>The layer's offset is measured, never assumed, and that is the entire point.</b>
        /// <para>
        /// This offset is not a constant and not something the documentation pins down: with
        /// <see cref="AdornmentPositioningBehavior.TextRelative"/> the layer sat at zero, so
        /// absolute text coordinates were required and viewport coordinates froze; with
        /// <see cref="AdornmentPositioningBehavior.OwnerControlled"/> the layer offsets itself
        /// by the scroll position, so viewport coordinates get the scroll subtracted twice and
        /// the error grows at double the scroll rate. Every previous attempt here picked one
        /// model and hard-coded it, and each was wrong in a different way.
        /// </para>
        /// <para>
        /// Measuring removes the guess. <see cref="Place"/> computes the position it wants on
        /// screen and then subtracts wherever the layer happens to be, so it is correct under
        /// either behaviour — and stays correct if it changes again.
        /// </para>
        /// </remarks>
        private void UpdateLayerOffset()
        {
            if (_layerElement == null)
            {
                return;
            }

            try
            {
                _layerOffset = _layerElement
                    .TransformToAncestor(_view.VisualElement)
                    .Transform(new Point(0, 0));
            }
            catch (InvalidOperationException)
            {
                // Not currently connected to the view; keep the last known offset.
            }
        }

        private void CreateVisuals(ITextViewLine line)
        {
            // An invalidated line still answers property reads, but with meaningless values.
            // Placing an adornment from one is how braces ended up stranded at the top of the
            // file, so refuse rather than position from geometry that means nothing.
            if (line == null || !line.IsValid)
            {
                return;
            }

            ITextSnapshot snapshot = _view.TextSnapshot;
            BraceMap map = _cache.Get(snapshot);
            if (map.Count == 0)
            {
                return;
            }

            IdentityBracesSettings settings = IdentityBracesSettings.Current;
            int lineStart = line.Start.Position;
            int lineEnd = line.End.Position;

            GlyphContext context = default(GlyphContext);
            bool contextBuilt = false;

            for (int i = map.FirstIndexAtOrAfter(lineStart); i < map.Count; i++)
            {
                BraceInfo brace = map[i];
                if (brace.Position >= lineEnd)
                {
                    break;
                }

                if (!brace.IsAdorned || brace.Position + 1 > snapshot.Length)
                {
                    continue;
                }

                _seenAdorned++;

                // Already drawn. Creation now runs over every visible line on every layout,
                // so this rejection is what keeps that from stacking duplicates.
                if (_byPosition.ContainsKey(brace.Position))
                {
                    _skipDuplicate++;
                    continue;
                }

                var span = new SnapshotSpan(snapshot, brace.Position, 1);

                TextBounds bounds;
                try
                {
                    bounds = line.GetCharacterBounds(span.Start);
                }
                catch (ArgumentOutOfRangeException)
                {
                    _skipBounds++;
                    continue;
                }

                if (bounds.Width <= 0)
                {
                    _skipZeroWidth++;
                    continue;
                }

                if (!contextBuilt)
                {
                    context = BuildContext(line);
                    contextBuilt = true;
                }

                context.CellWidth = bounds.Width;

                BraceVisual visual = BraceVisualFactory.Create(
                    brace.Character,
                    brace.Traits,
                    brace.ColorIndex,
                    brace.Identity,
                    context,
                    settings);

                if (visual == null)
                {
                    _skipNullVisual++;
                    continue;
                }

                visual.Position = brace.Position;
                visual.Kind = brace.Traits.Creature ?? brace.Traits.Body ?? brace.Traits.Motion ?? "fx";
                ApplyFlee(visual, bounds.Width);
                Place(visual, line, bounds);
                ApplyScope(visual);

                if (_paused)
                {
                    visual.Pause();
                }

                // Track it only if the layer actually took it. AddAdornment can decline — a
                // span outside the current view, for instance — and the removal callback then
                // never fires, so an untracked visual would sit in the list forever with its
                // animation clocks still running.
                bool added = _layer.AddAdornment(
                    AdornmentPositioningBehavior.OwnerControlled,
                    span,
                    visual,
                    visual.Element,
                    OnAdornmentRemoved);

                if (added)
                {
                    _byPosition[visual.Position] = visual;
                    _created++;

                    if (_layerElement == null)
                    {
                        // The only handle on the layer's canvas: IAdornmentLayer is not a
                        // UIElement, so it has to come from an adornment's visual parent.
                        _layerElement = VisualTreeHelper.GetParent(visual.Element) as FrameworkElement;
                        UpdateLayerOffset();
                        Place(visual, line, bounds);

                        Diagnostics.Log(
                            "  layer element captured: {0}  offset=({1:F1},{2:F1})",
                            _layerElement == null ? "NULL" : _layerElement.GetType().Name,
                            _layerOffset.X,
                            _layerOffset.Y);
                    }
                }
                else
                {
                    _skipAddFailed++;
                    visual.Stop();
                }
            }
        }

        /// <summary>Reads the editor's real font so a drawn glyph matches the text around it.</summary>
        private GlyphContext BuildContext(ITextViewLine line)
        {
            return GlyphContextFactory.Build(_formatMap, _view, line);
        }

        private void OnAdornmentRemoved(object tag, UIElement element)
        {
            var visual = tag as BraceVisual;
            if (visual == null)
            {
                return;
            }

            visual.Stop();

            // Only drop the bookkeeping entry if it still refers to this visual. A brace that
            // was re-created before its old element's removal callback ran must keep the new
            // entry, or the next pass would draw a second adornment on top of it.
            BraceVisual tracked;
            if (_byPosition.TryGetValue(visual.Position, out tracked) && ReferenceEquals(tracked, visual))
            {
                _byPosition.Remove(visual.Position);
            }
        }

        /// <summary>
        /// Stops animation for a view the user cannot see — switching document tabs should
        /// not leave a dozen colour cycles burning on a hidden buffer.
        /// </summary>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool visible = e.NewValue is bool && (bool)e.NewValue;
            if (visible == !_paused)
            {
                return;
            }

            _paused = !visible;

            foreach (BraceVisual visual in _byPosition.Values)
            {
                if (_paused)
                {
                    visual.Pause();
                }
                else
                {
                    visual.Resume();
                }
            }

            if (!_paused)
            {
                // A settings change that arrived while this tab was hidden skipped its
                // refresh, and no layout is guaranteed on the way back in. Correct positions
                // on the way to being seen rather than waiting for the user to scroll.
                RepositionAll();
            }
        }

        /// <summary>
        /// Re-reads the caret's enclosing pair and the dim level.
        /// </summary>
        /// <remarks>
        /// Read once per pass and cached in fields rather than consulted per brace: the scope
        /// is a property of the buffer, and re-reading it while walking the layer's elements
        /// could see it move and leave half the screen dimmed against the other half.
        /// </remarks>
        private void RefreshScope()
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;

            _spotlight = false;
            _scopeStart = 0;
            _scopeEnd = int.MaxValue;
            _dimPercent = settings.SpotlightDimPercent;

            _caretActive = settings.Enabled && Carets.AnyReactiveTrait(settings);
            _caretKnown = _caretActive && Carets.TryGet(_view.TextBuffer, out _caret);

            if (!settings.Enabled || !settings.ScopeSpotlight)
            {
                return;
            }

            int start;
            int end;
            if (!CaretScopes.TryGet(_view.TextBuffer, out start, out end))
            {
                return;
            }

            _spotlight = true;
            _scopeStart = start;
            _scopeEnd = end;
        }

        /// <summary>
        /// The fraction of full strength a brace should be drawn at.
        /// </summary>
        /// <remarks>
        /// Spotlight and stage fright are resolved together, into one number, because they
        /// share a channel. Two features each assigning <c>OpacityMask</c> would mean whichever
        /// ran last won, and a brace that was both out of scope and hiding would be drawn at
        /// one of the two strengths rather than at both.
        /// </remarks>
        private double StrengthOf(BraceVisual visual)
        {
            double strength = 1.0;

            if (_spotlight && (visual.Position < _scopeStart || visual.Position > _scopeEnd))
            {
                strength *= _dimPercent / 100.0;
            }

            // Never to zero. A brace that is genuinely invisible is the one failure this
            // codebase treats as unacceptable — there is a diagnostic counter for it — and
            // "hiding" reads perfectly well at a tenth strength.
            if (_caretKnown && visual.HasStageFright && _caret.Contains(visual.Position))
            {
                strength *= StageFrightStrength;
            }

            return strength;
        }

        /// <summary>How visible a brace with stage fright stays while the caret is on its line.</summary>
        private const double StageFrightStrength = 0.12;

        /// <summary>Characters of separation at which a fleeing brace stops reacting.</summary>
        private const int FleeRadius = 5;

        /// <summary>
        /// Works out how far a fleeing brace has edged away from the caret.
        /// </summary>
        /// <remarks>
        /// Only along its own line: a brace bolting sideways because the caret is three rows
        /// above it reads as a glitch rather than as flinching. Capped below one cell, so a
        /// fleeing brace never quite reaches its neighbour's column and the line stays legible
        /// as code.
        /// </remarks>
        private void ApplyFlee(BraceVisual visual, double cellWidth)
        {
            if (!visual.FleesCursor || !_caretKnown || !_caret.Contains(visual.Position))
            {
                visual.FleeOffsetX = 0;
                return;
            }

            int distance = Math.Abs(_caret.Position - visual.Position);
            if (distance >= FleeRadius)
            {
                visual.FleeOffsetX = 0;
                return;
            }

            double closeness = (FleeRadius - distance) / (double)FleeRadius;
            double direction = visual.Position < _caret.Position ? -1.0 : 1.0;

            visual.FleeOffsetX = direction * closeness * cellWidth * 0.85;
        }

        /// <summary>
        /// Fades a drawn brace that sits outside the caret's pair.
        /// </summary>
        /// <remarks>
        /// Through <see cref="UIElement.OpacityMask"/> rather than
        /// <see cref="UIElement.Opacity"/>, which is already spoken for: <c>nocturnal</c>
        /// assigns the canvas a local opacity, and <c>blink</c>, <c>flicker</c> and
        /// <c>typewriter</c> animate it. A WPF animation outranks a local value, so a blinking
        /// brace would silently refuse to dim and sit at full strength in a faded field. The
        /// mask is a separate channel that multiplies with both, so the spotlight composes
        /// with every motion instead of fighting one.
        /// <para>
        /// A solid-colour brush has no bounds, so it masks the parts of a glyph that overhang
        /// its cell — a tail, a hat — at the same strength as the rest. A gradient or image
        /// brush would clip them away entirely.
        /// </para>
        /// </remarks>
        private void ApplyScope(BraceVisual visual)
        {
            if (visual == null || visual.Element == null)
            {
                return;
            }

            visual.Element.OpacityMask = MaskFor(StrengthOf(visual));
        }

        /// <summary>A frozen mask brush for a strength, or null at full strength.</summary>
        /// <remarks>
        /// Null rather than an opaque brush when nothing is dimming, so the common case carries
        /// no mask at all and WPF is not asked for an intermediate render surface per brace.
        /// </remarks>
        private Brush MaskFor(double strength)
        {
            if (strength >= 0.999)
            {
                return null;
            }

            var alpha = (byte)(strength * 255);

            Brush brush;
            if (_masks.TryGetValue(alpha, out brush))
            {
                return brush;
            }

            var solid = new SolidColorBrush(Color.FromArgb(alpha, 0xFF, 0xFF, 0xFF));
            solid.Freeze();

            _masks[alpha] = solid;
            return solid;
        }

        /// <summary>
        /// The caret moved. Only the traits that read it care, and only when they are on.
        /// </summary>
        /// <remarks>
        /// This fires on every arrow key, so it is gated hard: with <c>fleecursor</c> and
        /// <c>stagefright</c> both at zero — which is the default — the tracker publishes
        /// nothing and this is never even raised.
        /// </remarks>
        private void OnCaretMoved(ITextBuffer buffer)
        {
            if (buffer != _view.TextBuffer || _view.IsClosed || _view.InLayout)
            {
                return;
            }

            RefreshScope();

            if (!_caretActive)
            {
                return;
            }

            // Re-placed from live geometry rather than nudged in place: a fleeing brace changes
            // its own placement, and the reposition pass is the one path in this class that is
            // allowed to write coordinates.
            RepositionAll();
        }

        /// <summary>
        /// The caret moved into a different pair. Re-masks in place; nothing is rebuilt, so
        /// animations keep running and no layout pass is provoked.
        /// </summary>
        private void OnCaretScopeChanged(ITextBuffer buffer)
        {
            if (buffer != _view.TextBuffer || _view.IsClosed)
            {
                return;
            }

            RefreshScope();

            var elements = new IAdornmentLayerElement[_layer.Elements.Count];
            _layer.Elements.CopyTo(elements, 0);

            for (int i = 0; i < elements.Length; i++)
            {
                ApplyScope(elements[i].Tag as BraceVisual);
            }
        }

        private void OnFormatMappingChanged(object sender, EventArgs e)
        {
            // Font, size or theme changed: every cached glyph is now the wrong shape.
            RefreshAll();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            // Settings are only ever saved from an options page, which is the UI thread.
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IdentityBracesSettings.Current.GetTraitWeight(TraitIds.BuildReactive) > 0)
            {
                BuildStatus.EnsureListening();
            }

            RefreshAll();
        }

        /// <summary>
        /// A build started or finished, so every reacting brace needs redrawing.
        /// </summary>
        /// <remarks>
        /// A full rebuild rather than anything cleverer, because builds are rare and the
        /// reaction is baked into the glyph at draw time. It arrives on the UI thread from the
        /// shell's own event, which is the thread that owns these elements.
        /// </remarks>
        private void OnBuildStatusChanged()
        {
            if (_view.IsClosed || IdentityBracesSettings.Current.GetTraitWeight(TraitIds.BuildReactive) <= 0)
            {
                return;
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_view.IsClosed || _view.InLayout || !_view.VisualElement.IsVisible)
            {
                return;
            }

            try
            {
                _layer.RemoveAllAdornments();
                _byPosition.Clear();

                foreach (ITextViewLine line in _view.TextViewLines)
                {
                    CreateVisuals(line);
                }
            }
            catch (InvalidOperationException)
            {
                // TextViewLines is unavailable mid-layout; the next layout pass rebuilds.
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
            _view.VisualElement.IsVisibleChanged -= OnIsVisibleChanged;
            _formatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
            IdentityBracesSettings.Changed -= OnSettingsChanged;
            CaretScopes.Changed -= OnCaretScopeChanged;
            Carets.Moved -= OnCaretMoved;
            BuildStatus.Changed -= OnBuildStatusChanged;

            foreach (BraceVisual visual in _byPosition.Values)
            {
                visual.Stop();
            }

            _byPosition.Clear();
            AdornedBuffers.Unregister(_view.TextBuffer);
        }
    }
}
