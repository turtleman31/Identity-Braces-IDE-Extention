using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Classification
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class BraceFormatOverrideProvider : IWpfTextViewCreationListener
    {
        [Import]
        internal IClassificationFormatMapService FormatMapService = null;

        [Import]
        internal IClassificationTypeRegistryService Registry = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView != null)
            {
                new BraceFormatOverrides(textView, FormatMapService, Registry);
            }
        }
    }

    /// <summary>
    /// Applies monochrome colour and brace scaling to the classification formats.
    /// </summary>
    /// <remarks>
    /// The adornment layer can only restyle the ~20% of braces that have personalities. Plain
    /// braces are drawn by the editor from their classification, so a setting that claims to
    /// affect "the braces" has to reach the format map, or four braces in five would ignore it.
    /// <para>
    /// Scaling through the classification is also what keeps the two paths consistent: a
    /// larger font on the classification widens the character cell, so
    /// <c>GetCharacterBounds</c> reports the larger cell and the drawn glyphs line up with the
    /// plain ones automatically.
    /// </para>
    /// <para>
    /// Nothing is touched while the settings are at their defaults, so the common case carries
    /// no risk of disturbing anyone's Fonts and Colors customisations.
    /// </para>
    /// </remarks>
    internal sealed class BraceFormatOverrides
    {
        private readonly IWpfTextView _view;
        private readonly IClassificationFormatMap _formatMap;
        private readonly IClassificationTypeRegistryService _registry;

        private bool _applying;
        private bool _hasOverrides;
        private bool _hasDimOverride;

        public BraceFormatOverrides(
            IWpfTextView view,
            IClassificationFormatMapService formatMapService,
            IClassificationTypeRegistryService registry)
        {
            _view = view;
            _registry = registry;
            _formatMap = formatMapService == null ? null : formatMapService.GetClassificationFormatMap(view);

            if (_formatMap == null || _registry == null)
            {
                return;
            }

            _view.Closed += OnClosed;
            IdentityBracesSettings.Changed += OnSettingsChanged;
            _formatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;

            Apply();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            Apply();
        }

        private void OnFormatMappingChanged(object sender, EventArgs e)
        {
            // The theme or the editor font changed. Re-derive, unless this is our own write
            // coming back around.
            if (!_applying)
            {
                Apply();
            }
        }

        private void Apply()
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;

            // Depth mode is deliberately absent: the scanner has already resolved each
            // brace's index from its nesting level, so the 32 formats still hold the 32
            // palette colours and nothing here needs rewriting. Overriding them anyway would
            // stamp on a user's Fonts and Colors customisations to write back the values they
            // already had.
            bool wanted = settings.Enabled
                && (settings.ColorMode == BraceColorMode.Monochrome || settings.BraceScalePercent != 100);

            bool dimWanted = settings.Enabled && settings.ScopeSpotlight;

            if (!wanted && !_hasOverrides && !dimWanted && !_hasDimOverride)
            {
                return;
            }

            bool isDark = BraceColors.IsDarkTheme(_formatMap);
            double baseSize = BaseFontSize();
            double scale = settings.BraceScalePercent / 100.0;

            _applying = true;
            try
            {
                _formatMap.BeginBatchUpdate();

                // Only when the palette formats are being changed, or were changed before and
                // are now being put back. The spotlight reaches the format map too, but
                // through a classification of its own — running this loop on its account would
                // write the stock 32 colours over anyone's Fonts and Colors customisations
                // every time they moved the caret into a different block.
                if (wanted || _hasOverrides)
                {
                    for (int i = 0; i < BracePalette.Count; i++)
                    {
                        IClassificationType type = _registry.GetClassificationType(BraceClassificationNames.Get(i));
                        if (type == null)
                        {
                            continue;
                        }

                        Color color = wanted
                            ? BraceColors.Resolve(i, settings, isDark)
                            : BracePalette.GetColor(i);

                        var brush = new SolidColorBrush(color);
                        brush.Freeze();

                        TextFormattingRunProperties properties = _formatMap.GetTextProperties(type)
                            .SetForegroundBrush(brush)
                            .SetFontRenderingEmSize(wanted ? baseSize * scale : baseSize);

                        _formatMap.SetTextProperties(type, properties);
                    }

                    _hasOverrides = wanted;
                }

                ApplyDimOpacity(dimWanted, settings);
            }
            catch (Exception)
            {
                // A format map that refuses an update must not cost the view its colours.
            }
            finally
            {
                try
                {
                    _formatMap.EndBatchUpdate();
                }
                catch (Exception)
                {
                }

                _applying = false;
            }
        }

        /// <summary>
        /// Writes the spotlight's dim level onto the one classification that carries opacity.
        /// </summary>
        /// <remarks>
        /// Runs inside the same batch update as the palette rewrite. The dim classification is
        /// layered over a palette one rather than replacing it, so this must set opacity and
        /// leave every other property empty — anything else set here would win the merge and
        /// override the colour underneath.
        /// </remarks>
        private void ApplyDimOpacity(bool dimWanted, IdentityBracesSettings settings)
        {
            if (!dimWanted && !_hasDimOverride)
            {
                return;
            }

            IClassificationType dim = _registry.GetClassificationType(BraceClassificationNames.Dim);
            if (dim == null)
            {
                return;
            }

            double opacity = dimWanted ? settings.SpotlightDimPercent / 100.0 : 1.0;

            _formatMap.SetTextProperties(
                dim,
                _formatMap.GetTextProperties(dim).SetForegroundOpacity(opacity));

            _hasDimOverride = dimWanted;
        }

        private double BaseFontSize()
        {
            try
            {
                TextFormattingRunProperties properties = _formatMap.DefaultTextProperties;
                if (!properties.FontRenderingEmSizeEmpty)
                {
                    return properties.FontRenderingEmSize;
                }
            }
            catch (InvalidOperationException)
            {
            }

            return 12.0;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.Closed -= OnClosed;
            IdentityBracesSettings.Changed -= OnSettingsChanged;
            _formatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
        }
    }
}
