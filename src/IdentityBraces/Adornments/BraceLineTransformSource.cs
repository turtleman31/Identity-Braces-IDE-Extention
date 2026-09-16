using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using IdentityBraces.Core;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Adornments
{
    [Export(typeof(ILineTransformSourceProvider))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class BraceLineTransformSourceProvider : ILineTransformSourceProvider
    {
        [Import]
        internal IClassificationFormatMapService FormatMapService = null;

        public ILineTransformSource Create(IWpfTextView textView)
        {
            return textView == null ? null : new BraceLineTransformSource(textView, FormatMapService);
        }
    }

    /// <summary>
    /// Reserves vertical space above lines that contain a catgirl brace, so the ears do not
    /// overlap the line above.
    /// </summary>
    /// <remarks>
    /// <b>This is the only component that can change the document's height, and it is
    /// therefore the only one that can break the editor's layout.</b> Two rules keep it safe,
    /// both learned the hard way:
    /// <list type="number">
    /// <item>
    /// The height it returns is a pure function of the <em>font</em>, computed once per view
    /// and cached. An earlier version derived it from <c>line.Baseline - line.TextTop</c> —
    /// the geometry of the very line it was sizing. That is a feedback loop, layout never
    /// converged, and the result was over-long documents, gaps, and lines pushed out of view.
    /// </item>
    /// <item>
    /// It does the cheapest possible work per line. It runs inside the layout pass for every
    /// visible line on every pass, so it must not measure fonts, walk the visual tree, or
    /// rescan the buffer.
    /// </item>
    /// </list>
    /// It also must never throw: an exception out of a layout callback takes the text view
    /// down for the rest of the session. Every failure path returns the default transform.
    /// </remarks>
    internal sealed class BraceLineTransformSource : ILineTransformSource
    {
        /// <summary>Every bracket we might have to clothe, so one cached number covers all of them.</summary>
        private static readonly char[] BraceCharacters = { '{', '}', '(', ')', '[', ']' };

        private readonly IWpfTextView _view;
        private readonly BraceMapCache _cache;
        private readonly IClassificationFormatMap _formatMap;

        private double _headroom = -1;

        public BraceLineTransformSource(IWpfTextView textView, IClassificationFormatMapService formatMapService)
        {
            _view = textView;
            _cache = BraceMapCache.GetOrCreate(textView.TextBuffer);
            _formatMap = formatMapService == null
                ? null
                : formatMapService.GetClassificationFormatMap(textView);

            if (_formatMap != null)
            {
                _formatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;
            }

            IdentityBracesSettings.Changed += OnSettingsChanged;
            textView.Closed += OnClosed;
        }

        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            try
            {
                // Cheapest possible early-out, first. Most lines and most users land here.
                IdentityBracesSettings settings = IdentityBracesSettings.Current;
                if (!settings.Enabled
                    || !settings.ReserveEarSpace
                    || !settings.AnyHeadroomTrait()
                    || _formatMap == null)
                {
                    return line.DefaultLineTransform;
                }

                double headroom = GetHeadroom();
                if (headroom <= 0)
                {
                    return line.DefaultLineTransform;
                }

                BraceMap map = _cache.Get(line.Snapshot);
                if (map.Count == 0 || !map.NeedsHeadroom(line.Start.Position, line.End.Position))
                {
                    return line.DefaultLineTransform;
                }

                return new LineTransform(headroom, 0.0, 1.0);
            }
            catch (Exception)
            {
                return line.DefaultLineTransform;
            }
        }

        /// <summary>
        /// The headroom for this view's font, measured once. Returns the largest any bracket
        /// needs, so the answer does not depend on which character happens to be on the line —
        /// one more thing that cannot vary between calls.
        /// </summary>
        private double GetHeadroom()
        {
            double cached = _headroom;
            if (cached >= 0)
            {
                return cached;
            }

            double headroom = 0;

            try
            {
                Typeface typeface = new Typeface("Consolas");
                double fontSize = 12.0;

                TextFormattingRunProperties properties = _formatMap.DefaultTextProperties;
                if (!properties.TypefaceEmpty)
                {
                    typeface = properties.Typeface;
                }

                if (!properties.FontRenderingEmSizeEmpty)
                {
                    fontSize = properties.FontRenderingEmSize;
                }

                double pixelsPerDip = 1.0;
                if (_view != null && _view.VisualElement != null)
                {
                    pixelsPerDip = VisualTreeHelper.GetDpi(_view.VisualElement).PixelsPerDip;
                }

                for (int i = 0; i < BraceCharacters.Length; i++)
                {
                    double needed = CatgirlLayout.RequiredHeadroom(
                        typeface, fontSize, pixelsPerDip, BraceCharacters[i],
                        IdentityBracesSettings.Current.EarScalePercent / 100.0);

                    if (needed > headroom)
                    {
                        headroom = needed;
                    }
                }
            }
            catch (Exception)
            {
                headroom = 0;
            }

            _headroom = headroom;
            return headroom;
        }

        private void OnFormatMappingChanged(object sender, EventArgs e)
        {
            _headroom = -1;
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            _headroom = -1;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (_formatMap != null)
            {
                _formatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
            }

            IdentityBracesSettings.Changed -= OnSettingsChanged;
            _view.Closed -= OnClosed;
        }
    }
}
