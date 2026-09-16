using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IdentityBraces.Classification;
using IdentityBraces.Core;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Adornments
{
    /// <summary>Declares the layer the indent guides are drawn on.</summary>
    internal static class IndentGuideLayer
    {
        public const string LayerName = "IdentityBracesGuides";

#pragma warning disable 649
        // Behind the text, unlike the personality layer. A guide is background structure; a
        // vertical line crossing over a glyph would be worse than no guide at all.
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        internal static AdornmentLayerDefinition Definition;
#pragma warning restore 649
    }

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class IndentGuideManagerProvider : IWpfTextViewCreationListener
    {
        [Import]
        internal IClassificationFormatMapService FormatMapService = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView != null)
            {
                new IndentGuideManager(textView, FormatMapService);
            }
        }
    }

    /// <summary>
    /// Draws a vertical guide down the inside of every multi-line pair, in that pair's colour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything here is derived from the geometry of <em>visible</em> lines only. A pair can
    /// easily span more screens than are on the monitor, and the line an off-screen brace sits
    /// on has no geometry to ask — <c>TextViewLines</c> simply does not contain it. Drawing one
    /// tall line from an opener to its closer would therefore need coordinates that do not
    /// exist. Instead each visible line contributes its own segment, and a guide that runs off
    /// the top of the screen is just a segment on every line down to the bottom of it.
    /// </para>
    /// <para>
    /// Which pairs are open at the top of the screen comes from
    /// <see cref="BraceInfo.ParentIndex"/> — a walk up the nesting rather than back through the
    /// file, so scrolling to the end of a large document costs the same as scrolling to the
    /// start of it.
    /// </para>
    /// <para>
    /// The x of a guide is the indent of the line its opener is on, measured in columns from
    /// the text and multiplied by the view's column width. Reading it from the opener's
    /// character bounds would have been more direct and is not available for the same reason
    /// as above.
    /// </para>
    /// </remarks>
    internal sealed class IndentGuideManager
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private readonly IClassificationFormatMap _formatMap;
        private readonly BraceMapCache _cache;
        private readonly GuideVisual _element = new GuideVisual();
        private readonly List<Guide> _guides = new List<Guide>();
        private readonly Dictionary<int, Pen> _pens = new Dictionary<int, Pen>();

        private bool _attached;
        private bool _wasEnabled;

        public IndentGuideManager(IWpfTextView view, IClassificationFormatMapService formatMapService)
        {
            _view = view;
            _layer = view.GetAdornmentLayer(IndentGuideLayer.LayerName);
            _formatMap = formatMapService == null ? null : formatMapService.GetClassificationFormatMap(view);
            _cache = BraceMapCache.GetOrCreate(view.TextBuffer);

            if (_layer == null || _formatMap == null)
            {
                return;
            }

            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;
            _formatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;
            IdentityBracesSettings.Changed += OnSettingsChanged;
        }

        private struct Guide
        {
            public int OpenLine;
            public int CloseLine;
            public int IndentColumn;
            public int ColorIndex;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            Render();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            // A palette or mode change invalidates every cached pen, not just the colours that
            // happen to be on screen.
            _pens.Clear();
            Render();
        }

        private void OnFormatMappingChanged(object sender, EventArgs e)
        {
            _pens.Clear();
            Render();
        }

        private void Render()
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;
            bool enabled = settings.Enabled && settings.IndentGuides;

            if (!enabled)
            {
                // Only touch the layer on the transition, so a view with guides switched off
                // does no work at all per layout.
                if (_wasEnabled || _attached)
                {
                    Detach();
                }

                _wasEnabled = false;
                return;
            }

            _wasEnabled = true;

            if (_view.IsClosed || _view.InLayout)
            {
                return;
            }

            try
            {
                Collect();
                Draw(settings);
            }
            catch (InvalidOperationException)
            {
                // TextViewLines is unavailable mid-layout; the next pass draws.
            }
            catch (ArgumentException)
            {
                // A snapshot moved under us between the map and the line geometry.
            }
        }

        /// <summary>
        /// Works out which pairs cross the visible region: the ones already open at the top of
        /// the screen, plus the ones that start on it.
        /// </summary>
        private void Collect()
        {
            _guides.Clear();

            IWpfTextViewLineCollection lines = _view.TextViewLines;
            if (lines == null || lines.Count == 0)
            {
                return;
            }

            ITextSnapshot snapshot = _view.TextSnapshot;
            BraceMap map = _cache.Get(snapshot);
            if (map.Count == 0)
            {
                return;
            }

            int viewStart = lines.FirstVisibleLine.Start.Position;
            int viewEnd = lines.LastVisibleLine.EndIncludingLineBreak.Position;
            int tabSize = TabSize();

            // Everything enclosing the top of the screen, outermost included: each of those is
            // a guide running off the top edge.
            int open;
            int close;
            int innermost = -1;

            if (map.TryGetEnclosingPair(viewStart, out open, out close))
            {
                innermost = open;

                do
                {
                    AddGuide(map, snapshot, open, close, tabSize);
                }
                while (map.TryGetParentPair(open, out open, out close));
            }

            // Then the pairs that open within the visible region. Bounded by what is on screen,
            // not by the size of the file.
            for (int i = map.FirstIndexAtOrAfter(viewStart); i < map.Count; i++)
            {
                BraceInfo brace = map[i];
                if (brace.Position >= viewEnd)
                {
                    break;
                }

                // Skipping the innermost enclosing pair: when the top of the screen falls
                // exactly on an opening brace, that pair both encloses the position and opens
                // within the range, and would otherwise be drawn twice.
                if (brace.IsOpen && brace.IsMatched && brace.PartnerIndex > i && i != innermost)
                {
                    AddGuide(map, snapshot, i, brace.PartnerIndex, tabSize);
                }
            }
        }

        private void AddGuide(BraceMap map, ITextSnapshot snapshot, int openIndex, int closeIndex, int tabSize)
        {
            BraceInfo opener = map[openIndex];
            BraceInfo closer = map[closeIndex];

            if (opener.Position >= snapshot.Length || closer.Position >= snapshot.Length)
            {
                return;
            }

            ITextSnapshotLine openLine = snapshot.GetLineFromPosition(opener.Position);
            ITextSnapshotLine closeLine = snapshot.GetLineFromPosition(closer.Position);

            // A pair that opens and closes on one line has no inside to draw down.
            if (closeLine.LineNumber <= openLine.LineNumber)
            {
                return;
            }

            _guides.Add(new Guide
            {
                OpenLine = openLine.LineNumber,
                CloseLine = closeLine.LineNumber,
                IndentColumn = IndentColumnOf(openLine, tabSize),
                ColorIndex = opener.ColorIndex,
            });
        }

        private void Draw(IdentityBracesSettings settings)
        {
            IWpfTextViewLineCollection lines = _view.TextViewLines;
            IFormattedLineSource source = _view.FormattedLineSource;

            _element.Segments.Clear();

            if (_guides.Count == 0 || lines == null || source == null || source.ColumnWidth <= 0)
            {
                Show(settings);
                return;
            }

            bool isDark = BraceColors.IsDarkTheme(_formatMap);
            double columnWidth = source.ColumnWidth;
            ITextSnapshot snapshot = _view.TextSnapshot;

            foreach (ITextViewLine line in lines)
            {
                if (!line.IsValid)
                {
                    continue;
                }

                int lineNumber = snapshot.GetLineNumberFromPosition(line.Start.Position);

                for (int g = 0; g < _guides.Count; g++)
                {
                    Guide guide = _guides[g];

                    // The guide covers the inside of the pair only: it starts below the line
                    // the opener is on and stops above the line the closer is on, so it never
                    // runs alongside either brace it belongs to.
                    if (lineNumber <= guide.OpenLine || lineNumber >= guide.CloseLine)
                    {
                        continue;
                    }

                    // Half a column in, so the guide sits inside its indent step rather than
                    // on the boundary shared with the level above.
                    double x = line.TextLeft + (guide.IndentColumn * columnWidth) + (columnWidth * 0.5);

                    _element.Segments.Add(new GuideSegment
                    {
                        // Snapped to a device pixel and offset by a half so a one-pixel stroke
                        // lands on one pixel instead of straddling two and rendering grey.
                        X = Math.Round(x) + 0.5,
                        Top = line.Top,
                        Bottom = line.Bottom,
                        Pen = PenFor(guide.ColorIndex, settings, isDark),
                    });
                }
            }

            Show(settings);
        }

        /// <summary>
        /// Parents the guide element to the layer, in the layer's own text coordinates.
        /// </summary>
        /// <remarks>
        /// The element sits at text coordinate zero and every segment carries an absolute text
        /// Y taken from a live line. That is the same discipline as
        /// <see cref="AdornmentManager"/>: there is no viewport term anywhere in the
        /// expression, so nothing here can be one scroll step stale by the time it renders.
        /// </remarks>
        private void Show(IdentityBracesSettings settings)
        {
            _element.Opacity = settings.IndentGuideOpacityPercent / 100.0;

            Canvas.SetLeft(_element, 0);
            Canvas.SetTop(_element, 0);

            // Re-parented rather than rebuilt, and the layer is ours alone, so clearing it is
            // never someone else's adornment being thrown away.
            _layer.RemoveAllAdornments();
            _attached = _layer.AddAdornment(
                AdornmentPositioningBehavior.OwnerControlled,
                null,
                this,
                _element,
                null);

            _element.InvalidateVisual();
        }

        private void Detach()
        {
            _element.Segments.Clear();
            _layer.RemoveAllAdornments();
            _attached = false;
        }

        private Pen PenFor(int colorIndex, IdentityBracesSettings settings, bool isDark)
        {
            Pen pen;
            if (_pens.TryGetValue(colorIndex, out pen))
            {
                return pen;
            }

            var brush = new SolidColorBrush(BraceColors.Resolve(colorIndex, settings, isDark));
            brush.Freeze();

            pen = new Pen(brush, 1.0)
            {
                // Dotted rather than solid. A solid line at full palette saturation competes
                // with the code for attention; the guide is meant to be found when looked for
                // and ignored otherwise.
                DashStyle = new DashStyle(new double[] { 1, 2 }, 0),
                DashCap = PenLineCap.Flat,
            };

            pen.Freeze();
            _pens[colorIndex] = pen;
            return pen;
        }

        /// <summary>Leading whitespace of a line, in columns, with tabs expanded.</summary>
        private static int IndentColumnOf(ITextSnapshotLine line, int tabSize)
        {
            string text = line.GetText();
            int column = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ')
                {
                    column++;
                }
                else if (c == '\t')
                {
                    column += tabSize - (column % tabSize);
                }
                else
                {
                    break;
                }
            }

            return column;
        }

        private int TabSize()
        {
            try
            {
                int size = _view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
                return size > 0 ? size : 4;
            }
            catch (Exception)
            {
                return 4;
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
            _formatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
            IdentityBracesSettings.Changed -= OnSettingsChanged;
        }
    }

    internal struct GuideSegment
    {
        public double X;
        public double Top;
        public double Bottom;
        public Pen Pen;
    }

    /// <summary>
    /// Draws every visible guide segment in one render pass.
    /// </summary>
    /// <remarks>
    /// One element for the whole view rather than one per segment. At five levels of nesting
    /// on forty visible lines that is the difference between a single <c>OnRender</c> and two
    /// hundred <c>UIElement</c>s created, measured, arranged and discarded on every scroll
    /// tick — and the guides have no state of their own to justify any of it.
    /// </remarks>
    internal sealed class GuideVisual : FrameworkElement
    {
        internal readonly List<GuideSegment> Segments = new List<GuideSegment>();

        internal GuideVisual()
        {
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            for (int i = 0; i < Segments.Count; i++)
            {
                GuideSegment segment = Segments[i];
                if (segment.Pen == null)
                {
                    continue;
                }

                drawingContext.DrawLine(
                    segment.Pen,
                    new Point(segment.X, segment.Top),
                    new Point(segment.X, segment.Bottom));
            }
        }
    }
}
