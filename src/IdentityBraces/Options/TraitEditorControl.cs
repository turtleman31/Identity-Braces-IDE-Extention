using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using IdentityBraces.Adornments;
using IdentityBraces.Adornments.Scenes;
using IdentityBraces.Core;
using Microsoft.VisualStudio.Shell;

namespace IdentityBraces.Options
{
    /// <summary>
    /// The trait weight editor, with a live preview.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Eighty-six traits were reachable only by hand-editing <c>settings.ini</c> before this
    /// existed, and even then you had to turn one on and go hunting through a file to find out
    /// what it was. A catalogue that large is not usable through a property grid: it needs
    /// search, it needs grouping, and above all it needs to show you what a thing looks like
    /// before you commit to it.
    /// </para>
    /// <para>
    /// Everything here edits a <em>clone</em> of the settings. The live preview is drawn from
    /// that clone, so it updates as sliders move while nothing reaches the editor until OK or
    /// Apply — which is what makes Cancel actually cancel. See
    /// <see cref="IdentityBracesSettings.Clone"/>, whose deep copy of the weight table is
    /// load-bearing for exactly this reason.
    /// </para>
    /// </remarks>
    internal sealed class TraitEditorControl : Grid
    {
        /// <summary>
        /// How many braces the density strip rolls.
        /// </summary>
        /// <remarks>
        /// Chosen against the defaults, where roughly four braces in five are plain. Far fewer
        /// than this and a trait sitting at 4% shows up zero or one times, so the strip reads
        /// as "nothing happens" whatever you do to the slider.
        /// </remarks>
        private const int SampleCount = 120;

        /// <summary>
        /// Pixel size for the magnified single-trait preview.
        /// </summary>
        /// <remarks>
        /// Absolute rather than a multiple of the editor's font, so the preview box is the same
        /// height whatever size the user reads code at. Large enough that a 0.15-unit detail —
        /// the finest anything in the catalogue is authored at — lands on about eight pixels
        /// instead of two.
        /// </remarks>
        private const double FocusFontSize = 58.0;

        /// <summary>Height of both preview frames, in pixels.</summary>
        /// <remarks>
        /// Fixed rather than grown to fit, so selecting a trait with a tall hat does not resize
        /// the panel and shove the trait list down the dialog. Clipped at the top end because a
        /// 400% ear scale would otherwise take the whole page.
        /// </remarks>
        private const double PreviewHeight = 112.0;

        private readonly List<TraitRow> _rows = new List<TraitRow>();
        private readonly Dictionary<TraitLayer, LayerHeader> _headers = new Dictionary<TraitLayer, LayerHeader>();
        private readonly List<BraceVisual> _live = new List<BraceVisual>();
        private readonly List<BraceVisual> _liveFocus = new List<BraceVisual>();

        private readonly WrapPanel _strip;
        private readonly Border _stripFrame;
        private readonly WrapPanel _focusStrip;
        private readonly Border _focusFrame;
        private readonly TextBlock _focusName;
        private readonly TextBlock _focusDescription;
        private readonly TextBox _search;
        private readonly TextBlock _footer;
        private readonly StackPanel _list;
        private readonly DispatcherTimer _debounce;

        private IdentityBracesSettings _working;
        private Brush _editorBackground;
        private string _focusTraitId;
        private bool _syncing;

        public TraitEditorControl(IdentityBracesSettings working)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _working = working;
            _editorBackground = BracePreview.EditorBackground();

            Margin = new Thickness(6);
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Re-rendering 120 brace visuals on every tick of a slider drag would make the
            // slider feel like it was stuck. One render shortly after the last movement is
            // indistinguishable from live and costs a fraction as much.
            _debounce = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(90),
            };
            _debounce.Tick += OnDebounceTick;

            AddRow(BuildPresetBar(), 0);

            _search = new TextBox { MinWidth = 220, Margin = new Thickness(0, 0, 8, 0) };
            _search.TextChanged += delegate { ApplyFilter(); };
            AddRow(BuildSearchBar(), 1);

            _strip = new WrapPanel { Orientation = Orientation.Horizontal };
            _stripFrame = Framed(_strip, _editorBackground, PreviewHeight);

            _focusStrip = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _focusFrame = Framed(_focusStrip, _editorBackground, PreviewHeight);
            _focusFrame.MinWidth = 190;

            _focusName = Label(string.Empty, true);
            _focusDescription = Dimmed(string.Empty);
            _focusDescription.TextWrapping = TextWrapping.Wrap;

            AddRow(BuildPreviewArea(), 2);

            _list = new StackPanel();
            var scroller = new ScrollViewer
            {
                Content = _list,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 6, 0, 6),
            };

            AddRow(scroller, 3);

            _footer = Dimmed(string.Empty);
            _footer.TextWrapping = TextWrapping.Wrap;
            AddRow(_footer, 4);

            BuildRows();
            SyncFromWorking();
        }

        /// <summary>The edited copy. The page reads this when the user presses OK or Apply.</summary>
        public IdentityBracesSettings Working
        {
            get { return _working; }
        }

        /// <summary>Rebinds to a fresh clone, for when the page is reopened.</summary>
        public void Reload(IdentityBracesSettings working)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _working = working;
            _editorBackground = BracePreview.EditorBackground();
            _stripFrame.Background = _editorBackground;
            _focusFrame.Background = _editorBackground;
            SyncFromWorking();
        }

        /// <summary>Stops every animation the preview started.</summary>
        /// <remarks>
        /// Preview braces carry the same animation clocks the real ones do. Dropping the
        /// elements without stopping the clocks leaves them running against a detached visual
        /// tree for the life of the process — an options page that quietly costs a few percent
        /// of a core every time it is opened.
        /// </remarks>
        public void Shutdown()
        {
            _debounce.Stop();
            StopLive();
        }

        // ---------------------------------------------------------------- layout

        private void AddRow(UIElement child, int row)
        {
            SetRow(child, row);
            Children.Add(child);
        }

        private UIElement BuildPresetBar()
        {
            var bar = new StackPanel { Orientation = Orientation.Horizontal };
            bar.Children.Add(Label("Presets:", true));

            ThreadHelper.ThrowIfNotOnUIThread();

            for (int i = 0; i < TraitPresets.All.Count; i++)
            {
                TraitPreset preset = TraitPresets.All[i];
                string id = preset.Id;

                var button = new Button
                {
                    Content = preset.Name,
                    Margin = new Thickness(6, 0, 0, 0),
                    Padding = new Thickness(10, 2, 10, 2),
                    MinWidth = 74,
                    ToolTip = preset.Description,
                };

                button.Click += delegate
                {
                    _working.ApplyPreset(id);
                    SyncFromWorking();
                };

                bar.Children.Add(button);
            }

            return bar;
        }

        private UIElement BuildSearchBar()
        {
            var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            bar.Children.Add(Label("Search:", false));
            bar.Children.Add(_search);
            bar.Children.Add(Dimmed("Type a name, or a layer like \"creature\"."));
            return bar;
        }

        private UIElement BuildPreviewArea()
        {
            var area = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            area.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            area.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            left.Children.Add(Label("Selected trait", true));
            left.Children.Add(_focusFrame);
            left.Children.Add(_focusName);

            _focusDescription.MaxWidth = 220;
            left.Children.Add(_focusDescription);

            SetColumn(left, 0);
            area.Children.Add(left);

            var right = new StackPanel();
            right.Children.Add(Label("At these weights, in your editor's font", true));
            right.Children.Add(_stripFrame);
            right.Children.Add(Dimmed(
                "Actual size, rolled with the real trait table — this is the density you will "
                + "get, not an illustration. Held still: motion is previewed on the left."));

            SetColumn(right, 1);
            area.Children.Add(right);

            return area;
        }

        private void BuildRows()
        {
            TraitLayer? current = null;

            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                TraitInfo info = TraitCatalog.All[i];

                if (current == null || current.Value != info.Layer)
                {
                    current = info.Layer;
                    var header = new LayerHeader(info.Layer);
                    _headers[info.Layer] = header;
                    _list.Children.Add(header.Element);
                }

                var row = new TraitRow(info);

                row.Slider.ValueChanged += delegate
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    OnWeightChanged(row);
                };

                // Clicking anywhere on the row selects it, so the whole row is a target rather
                // than just the slider's thumb.
                row.Element.PreviewMouseLeftButtonDown += delegate
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    SelectTrait(row.Info);
                };

                // And arrowing through the list with the keyboard selects as it goes, so the
                // preview follows a keyboard user the same way it follows the mouse.
                row.Slider.GotKeyboardFocus += delegate
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    SelectTrait(row.Info);
                };

                _rows.Add(row);
                _list.Children.Add(row.Element);
            }
        }

        // ---------------------------------------------------------------- behaviour

        private void OnWeightChanged(TraitRow row)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            row.ValueLabel.Text = ((int)row.Slider.Value).ToString(CultureInfo.CurrentCulture) + " %";

            if (_syncing)
            {
                return;
            }

            _working.SetTraitWeight(row.Info.Id, (int)row.Slider.Value);
            SelectTrait(row.Info);
            UpdateTotals();
            QueuePreview();
        }

        /// <summary>Pushes the working settings into every slider, without echoing back.</summary>
        private void SyncFromWorking()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _syncing = true;
            try
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    _rows[i].Slider.Value = _working.GetTraitWeight(_rows[i].Info.Id);
                }
            }
            finally
            {
                _syncing = false;
            }

            UpdateTotals();
            ApplyFilter();
            SelectSomething();
            RenderPreview();
        }

        /// <summary>
        /// Puts something in the magnified panel before the user has clicked anything.
        /// </summary>
        /// <remarks>
        /// Preferring a trait that is actually switched on, so the panel opens showing
        /// something the user will recognise from their own editor rather than the first row of
        /// a catalogue they have never enabled. An empty box under a "Selected trait" heading
        /// reads as broken.
        /// </remarks>
        private void SelectSomething()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_focusTraitId != null)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                if (_working.GetTraitWeight(_rows[i].Info.Id) > 0)
                {
                    SelectTrait(_rows[i].Info);
                    return;
                }
            }

            if (_rows.Count > 0)
            {
                SelectTrait(_rows[0].Info);
            }
        }

        /// <summary>
        /// Shows each shared-roll layer's total against its budget.
        /// </summary>
        /// <remarks>
        /// Body, Creature, Costume and Motion share one 0-100 roll, so a layer summing past 100
        /// gets scaled down on save and the traits at the end of the list become unreachable.
        /// That used to happen silently to whoever turned the sliders up. Effects are separate
        /// coin flips and have no budget to overrun.
        /// </remarks>
        private void UpdateTotals()
        {
            bool anyOver = false;

            foreach (KeyValuePair<TraitLayer, LayerHeader> pair in _headers)
            {
                int total = 0;
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Info.Layer == pair.Key)
                    {
                        total += (int)_rows[i].Slider.Value;
                    }
                }

                bool over = pair.Key != TraitLayer.Effect && total > 100;
                anyOver |= over;
                pair.Value.SetTotal(total, over);
            }

            _footer.Text = anyOver
                ? "A layer is over its budget. Body, creature, costume and motion share a single "
                  + "0-100 roll, so these weights will be scaled down proportionally when you "
                  + "press OK — and the traits lowest in the list would otherwise never come up."
                : "Body, creature, costume and motion share one 0-100 roll per brace, so their "
                  + "weights are absolute percentages of all braces. Effects roll independently, "
                  + "so several can land on the same brace.";
        }

        private void ApplyFilter()
        {
            string needle = (_search.Text ?? string.Empty).Trim();
            var populated = new Dictionary<TraitLayer, bool>();

            for (int i = 0; i < _rows.Count; i++)
            {
                TraitRow row = _rows[i];
                bool visible = Matches(row.Info, needle);
                row.Element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

                if (visible)
                {
                    populated[row.Info.Layer] = true;
                }
            }

            // A header with nothing under it is worse than no header: it reads as a layer whose
            // traits have all been hidden by a bug rather than by the filter.
            foreach (KeyValuePair<TraitLayer, LayerHeader> pair in _headers)
            {
                pair.Value.Element.Visibility = populated.ContainsKey(pair.Key)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private static bool Matches(TraitInfo info, string needle)
        {
            if (needle.Length == 0)
            {
                return true;
            }

            return Contains(info.Name, needle)
                || Contains(info.Id, needle)
                || Contains(info.Layer.ToString(), needle)
                || Contains(info.Description, needle);
        }

        private static bool Contains(string haystack, string needle)
        {
            return haystack != null
                && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectTrait(TraitInfo info)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.Equals(_focusTraitId, info.Id, StringComparison.Ordinal))
            {
                return;
            }

            _focusTraitId = info.Id;
            _focusName.Text = info.Name + "  ·  " + info.Layer;
            // Painter, scene, or something else entirely — see TraitImplementation. Checking
            // only for a painter would label tableflip undrawn while it was busy throwing
            // tables around the editor.
            bool implemented = TraitImplementation.IsImplemented(info.Id);

            _focusDescription.Text = implemented
                ? info.Description
                : info.Description + "  (catalogued, not yet drawn)";

            RenderFocus();
        }

        private void QueuePreview()
        {
            _debounce.Stop();
            _debounce.Start();
        }

        private void OnDebounceTick(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _debounce.Stop();
            RenderPreview();
        }

        // ---------------------------------------------------------------- rendering

        private void RenderPreview()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            RenderStrip();
            RenderFocus();
        }

        /// <summary>
        /// Draws the density strip: the real roll, at the real size, with motion off.
        /// </summary>
        /// <remarks>
        /// Motion is disabled for this one deliberately. It answers "how much of my file will
        /// be decorated, and in what", which is a question about proportion — and a hundred and
        /// twenty simultaneous animation clocks in a modal dialog would answer it while making
        /// the dialog stutter. The single-trait preview beside it runs its animation, which is
        /// where movement is actually the thing being previewed.
        /// </remarks>
        private void RenderStrip()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            StopAll(_live);
            _strip.Children.Clear();

            IdentityBracesSettings still = _working.Clone();
            still.EnableMotion = false;

            GlyphContext context = BracePreview.BuildContext(still, PixelsPerDip(), 0);
            List<BracePreview.Sample> samples = BracePreview.Roll(still, SampleCount);

            for (int i = 0; i < samples.Count; i++)
            {
                BraceVisual visual;
                FrameworkElement element = BracePreview.Draw(samples[i], context, still, out visual);

                if (element == null)
                {
                    continue;
                }

                if (visual != null)
                {
                    _live.Add(visual);
                }

                _strip.Children.Add(element);
            }
        }

        /// <summary>Draws the selected trait alone, magnified, with its animation running.</summary>
        private void RenderFocus()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            StopAll(_liveFocus);
            _focusStrip.Children.Clear();

            if (_focusTraitId == null)
            {
                return;
            }

            TraitInfo info = Find(_focusTraitId);
            if (info == null)
            {
                return;
            }

            GlyphContext context = BracePreview.BuildContext(_working, PixelsPerDip(), FocusFontSize);

            // An opener and a closer, because several traits are directional — a tail curls one
            // way, a scarf streams one way — and one sample would hide that.
            char[] pair = { '{', '}' };

            for (int i = 0; i < pair.Length; i++)
            {
                BraceTraits traits = TraitSampler.Single(info.Layer, info.Id);
                ulong identity = Hash.Mix(0xF0C05EDUL, (ulong)i);

                var sample = new BracePreview.Sample
                {
                    Character = pair[i],
                    Identity = identity,
                    ColorIndex = Hash.ToIndex(identity, Classification.BracePalette.Count),
                    Traits = traits,
                };

                BraceVisual visual;
                FrameworkElement element = BracePreview.Draw(sample, context, _working, out visual);

                if (element == null)
                {
                    continue;
                }

                if (visual != null)
                {
                    _liveFocus.Add(visual);
                }

                _focusStrip.Children.Add(element);
            }
        }

        private static TraitInfo Find(string id)
        {
            for (int i = 0; i < TraitCatalog.All.Count; i++)
            {
                if (string.Equals(TraitCatalog.All[i].Id, id, StringComparison.Ordinal))
                {
                    return TraitCatalog.All[i];
                }
            }

            return null;
        }

        private void StopLive()
        {
            StopAll(_live);
            StopAll(_liveFocus);
        }

        private static void StopAll(List<BraceVisual> visuals)
        {
            for (int i = 0; i < visuals.Count; i++)
            {
                visuals[i].Stop();
            }

            visuals.Clear();
        }

        private double PixelsPerDip()
        {
            try
            {
                return VisualTreeHelper.GetDpi(this).PixelsPerDip;
            }
            catch (Exception)
            {
                return 1.0;
            }
        }

        // ---------------------------------------------------------------- small pieces

        private static Border Framed(UIElement content, Brush background, double height)
        {
            var border = new Border
            {
                Background = background,
                Padding = new Thickness(6),
                Height = height,
                Margin = new Thickness(0, 2, 0, 2),
                BorderThickness = new Thickness(1),

                // A brace can legitimately overhang its cell — a tail, a hat, ears reaching
                // above the line — and at a 400% ear scale it overhangs a long way. Clipping
                // to the frame keeps that inside the preview instead of painting over the
                // controls around it.
                ClipToBounds = true,
                Child = content,
            };

            border.SetResourceReference(Border.BorderBrushProperty, VsBrushes.ActiveBorderKey);
            return border;
        }

        private static TextBlock Label(string text, bool bold)
        {
            var block = new TextBlock
            {
                Text = text,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
            };

            block.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.WindowTextKey);
            return block;
        }

        private static TextBlock Dimmed(string text)
        {
            var block = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0),
            };

            block.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.GrayTextKey);
            return block;
        }

        /// <summary>One layer's heading, carrying its running total.</summary>
        private sealed class LayerHeader
        {
            private readonly TraitLayer _layer;
            private readonly TextBlock _text;

            public LayerHeader(TraitLayer layer)
            {
                _layer = layer;
                _text = Label(string.Empty, true);
                _text.Margin = new Thickness(0, 10, 0, 2);
                Element = _text;
            }

            public FrameworkElement Element { get; private set; }

            public void SetTotal(int total, bool over)
            {
                _text.Text = _layer == TraitLayer.Effect
                    ? _layer + "   —   " + total + " % across " + Count() + ", rolled independently"
                    : _layer + "   —   " + total + " / 100 %";

                if (over)
                {
                    _text.Foreground = Brushes.OrangeRed;
                }
                else
                {
                    _text.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.WindowTextKey);
                }
            }

            private int Count()
            {
                int n = 0;
                for (int i = 0; i < TraitCatalog.All.Count; i++)
                {
                    if (TraitCatalog.All[i].Layer == _layer)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>One trait: name, slider, value, description.</summary>
        private sealed class TraitRow
        {
            public TraitRow(TraitInfo info)
            {
                Info = info;

                Slider = new Slider
                {
                    Minimum = 0,
                    Maximum = 100,
                    Width = 150,
                    IsSnapToTickEnabled = true,
                    TickFrequency = 1,
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = info.Description,
                };

                ValueLabel = Dimmed("0 %");
                ValueLabel.Width = 42;
                ValueLabel.TextAlignment = TextAlignment.Right;

                TextBlock name = Label(info.Name, false);
                name.Width = 130;

                TextBlock description = Dimmed(info.Description);
                description.Margin = new Thickness(10, 0, 0, 0);
                description.TextTrimming = TextTrimming.CharacterEllipsis;

                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(name);
                panel.Children.Add(Slider);
                panel.Children.Add(ValueLabel);
                panel.Children.Add(description);

                // Transparent rather than unset, so the whole row is a hit-test target and
                // clicking anywhere on it selects the trait for the preview.
                Element = new Border
                {
                    Background = Brushes.Transparent,
                    Padding = new Thickness(2, 1, 2, 1),
                    Cursor = Cursors.Hand,
                    Child = panel,
                };
            }

            public TraitInfo Info { get; private set; }

            public Slider Slider { get; private set; }

            public TextBlock ValueLabel { get; private set; }

            public Border Element { get; private set; }
        }
    }
}
