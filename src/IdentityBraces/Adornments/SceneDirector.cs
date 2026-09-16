using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using IdentityBraces.Adornments.Scenes;
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
    /// <summary>Declares the layer scene props are drawn on.</summary>
    internal static class SceneLayer
    {
        public const string LayerName = "IdentityBracesScenes";

#pragma warning disable 649
        // Above the braces themselves: a thrown table passing behind the glyph that threw it
        // would look like a rendering fault rather than a throw.
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(After = IdentityBracesLayer.LayerName, Before = PredefinedAdornmentLayers.Caret)]
        internal static AdornmentLayerDefinition Definition;
#pragma warning restore 649
    }

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class SceneDirectorProvider : IWpfTextViewCreationListener
    {
        [Import]
        internal IClassificationFormatMapService FormatMapService = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView != null)
            {
                new SceneDirector(textView, FormatMapService);
            }
        }
    }

    /// <summary>
    /// Owns the performances that need more than one cell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The per-brace trait painters are handed a canvas exactly one character wide and know
    /// nothing about their neighbours or the text beside them. That is the right shape for
    /// eighty-odd traits and the wrong shape for a handful — a table needs somewhere clear to
    /// land, a fire brigade needs to find a burning brace and travel to it. This is the layer
    /// above the factory where those live.
    /// </para>
    /// <para>
    /// <b>A scene never survives a layout.</b> Its props are placed from line geometry that is
    /// only valid until the next pass, so scrolling, typing, resizing or re-theming strikes the
    /// set immediately and the performance is simply lost. That is a deliberate trade: the
    /// alternative is repositioning props on every layout, which is the exact problem that cost
    /// this project ten rounds of debugging on the brace adornments — and unlike a brace, a
    /// prop that vanishes mid-flight costs nothing, because a second later there will be
    /// another one.
    /// </para>
    /// <para>
    /// One scene at a time, per view, on a slow timer. These are meant to be caught out of the
    /// corner of an eye, not watched.
    /// </para>
    /// </remarks>
    internal sealed class SceneDirector
    {
        /// <summary>Longest run of blank columns worth counting either side of a brace.</summary>
        private const int RoomLimit = 24;

        /// <summary>
        /// How many eligible braces to measure before giving up on a tick.
        /// </summary>
        /// <remarks>
        /// Measuring every eligible brace on screen to then use one is work thrown away, but
        /// measuring only one means a tick fails whenever that one happens to sit on a crowded
        /// line. A handful is the compromise: a few line lookups, and the scene actually plays
        /// often enough to be noticed.
        /// </remarks>
        private const int CandidatesPerTick = 4;

        /// <summary>
        /// Ticks after which an open stage is struck regardless.
        /// </summary>
        /// <remarks>
        /// The safety net for the director wedging. A scene normally ends on its own timer or
        /// on the next layout, but if either were ever missed, <c>_stage</c> would stay
        /// non-null and no scene would play again for the life of the view — a feature that
        /// silently stops, which is the hardest kind of fault to notice.
        /// </remarks>
        private const ulong MaxStageTicks = 3;

        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private readonly IClassificationFormatMap _formatMap;
        private readonly BraceMapCache _cache;
        private readonly IScene[] _scenes = SceneCatalog.Create();

        private readonly List<int> _candidateIndices = new List<int>();
        private readonly List<SceneActor> _candidates = new List<SceneActor>();
        private readonly List<int> _subjectIndices = new List<int>();
        private readonly List<SceneActor> _subjects = new List<SceneActor>();
        private readonly List<SceneActor> _cast = new List<SceneActor>();

        private DispatcherTimer _timer;
        private SceneStage _stage;
        private Canvas _canvas;
        private ulong _tick;
        private ulong _stageOpenedAt;

        public SceneDirector(IWpfTextView view, IClassificationFormatMapService formatMapService)
        {
            _view = view;
            _layer = view.GetAdornmentLayer(SceneLayer.LayerName);
            _formatMap = formatMapService == null ? null : formatMapService.GetClassificationFormatMap(view);
            _cache = BraceMapCache.GetOrCreate(view.TextBuffer);

            if (_layer == null || _formatMap == null)
            {
                return;
            }

            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;
            _view.VisualElement.IsVisibleChanged += OnIsVisibleChanged;
            IdentityBracesSettings.Changed += OnSettingsChanged;

            Reschedule();
        }

        /// <summary>
        /// Starts or stops the clock according to whether any scene could play at all.
        /// </summary>
        /// <remarks>
        /// Every scene here is motion by definition, so <c>EnableMotion</c> off means the timer
        /// should not exist rather than tick and find nothing to do. A trait sitting at zero
        /// costs nothing either — the candidate scan simply never matches — but not running the
        /// timer at all is cheaper still, and the common case is that all of these are off.
        /// </remarks>
        private void Reschedule()
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;
            bool wanted = settings.Enabled && settings.EnableMotion && AnySceneTraitEnabled(settings);

            if (!wanted)
            {
                Strike();

                if (_timer != null)
                {
                    _timer.Stop();
                    _timer = null;
                }

                return;
            }

            if (_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Background);
                _timer.Tick += OnTick;
            }

            _timer.Interval = TimeSpan.FromSeconds(settings.SceneIntervalSeconds);
            _timer.Start();
        }

        private bool AnySceneTraitEnabled(IdentityBracesSettings settings)
        {
            for (int i = 0; i < _scenes.Length; i++)
            {
                if (settings.GetTraitWeight(_scenes[i].TraitId) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnTick(object sender, EventArgs e)
        {
            _tick++;

            if (_view.IsClosed || _view.InLayout)
            {
                return;
            }

            // One at a time. Two scenes running at once on the same screen stops reading as an
            // event and starts reading as a fault.
            if (_stage != null)
            {
                if (_tick - _stageOpenedAt >= MaxStageTicks)
                {
                    Diagnostics.Log("scene: striking a stage that outstayed its welcome");
                    Strike();
                }

                return;
            }

            if (!_view.VisualElement.IsVisible)
            {
                return;
            }

            try
            {
                TryPlay();
            }
            catch (InvalidOperationException)
            {
                // TextViewLines unavailable mid-layout. There will be another tick.
                Strike();
            }
            catch (ArgumentException)
            {
                // A snapshot moved between the map and the geometry.
                Strike();
            }
        }

        private void TryPlay()
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;

            IScene scene = _scenes[(int)(_tick % (ulong)_scenes.Length)];
            if (settings.GetTraitWeight(scene.TraitId) <= 0)
            {
                return;
            }

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

            SceneCasting.FindCandidates(map, scene.TraitId, viewStart, viewEnd, _candidateIndices);
            if (_candidateIndices.Count == 0)
            {
                return;
            }

            Resolve(map, _candidateIndices, _candidates, snapshot, lines, settings);

            _subjects.Clear();
            if (scene.SubjectTraitId != null)
            {
                SceneCasting.FindCandidates(map, scene.SubjectTraitId, viewStart, viewEnd, _subjectIndices);
                if (_subjectIndices.Count == 0)
                {
                    return;
                }

                Resolve(map, _subjectIndices, _subjects, snapshot, lines, settings);
            }

            _cast.Clear();

            // Casting nobody is the ordinary outcome — every candidate this tick sat on a
            // crowded line, or no fire was visible — not a failure. The next tick draws again.
            if (!scene.TryCast(_candidates, _subjects, _cast) || _cast.Count == 0)
            {
                return;
            }

            Open(settings, lines.FirstVisibleLine);
            if (_stage == null)
            {
                return;
            }

            scene.Play(_stage, _cast);
            _stage.WhenFinished(scene.Duration, Strike);

            Diagnostics.Log(
                "scene: {0} playing at position {1}, cast of {2}, room L{3}/R{4}",
                scene.TraitId,
                _cast[0].Position,
                _cast.Count,
                _cast[0].RoomLeft,
                _cast[0].RoomRight);
        }

        /// <summary>
        /// Measures a handful of eligible braces, as a consecutive run.
        /// </summary>
        /// <remarks>
        /// Consecutive rather than independently random, and that is load-bearing for
        /// <c>swapplaces</c>: consecutive entries in the brace map are usually neighbours on a
        /// line, whereas two braces chosen independently from a screenful almost never share
        /// one. The starting point still varies per tick, so it is not always the same braces.
        /// </remarks>
        private void Resolve(
            BraceMap map,
            List<int> indices,
            List<SceneActor> into,
            ITextSnapshot snapshot,
            IWpfTextViewLineCollection lines,
            IdentityBracesSettings settings)
        {
            into.Clear();

            if (indices.Count == 0)
            {
                return;
            }

            int start = SceneCasting.Choose(indices, _tick);
            int offset = indices.IndexOf(start);
            if (offset < 0)
            {
                offset = 0;
            }

            for (int i = 0; i < CandidatesPerTick && i < indices.Count; i++)
            {
                SceneActor actor;
                if (TryResolve(map, indices[(offset + i) % indices.Count], snapshot, lines, settings, out actor))
                {
                    into.Add(actor);
                }
            }
        }

        /// <summary>
        /// Turns a brace index into everything a scene needs to know about where it is.
        /// </summary>
        /// <remarks>
        /// Read from live geometry at the moment of playing, never cached. The room either side
        /// comes from the line's text rather than from anything on screen, so it is unaffected
        /// by where the view happens to be scrolled to.
        /// </remarks>
        private bool TryResolve(
            BraceMap map,
            int index,
            ITextSnapshot snapshot,
            IWpfTextViewLineCollection lines,
            IdentityBracesSettings settings,
            out SceneActor actor)
        {
            actor = default(SceneActor);

            BraceInfo brace = map[index];
            if (brace.Position < 0 || brace.Position >= snapshot.Length)
            {
                return false;
            }

            var point = new SnapshotPoint(snapshot, brace.Position);
            ITextViewLine line = lines.GetTextViewLineContainingBufferPosition(point);
            if (line == null || !line.IsValid)
            {
                return false;
            }

            TextBounds bounds = line.GetCharacterBounds(point);
            if (bounds.Width <= 0)
            {
                return false;
            }

            ITextSnapshotLine textLine = snapshot.GetLineFromPosition(brace.Position);
            string lineText = textLine.GetText();
            int column = brace.Position - textLine.Start.Position;

            actor = new SceneActor
            {
                Position = brace.Position,
                LineStart = textLine.Start.Position,
                Column = column,
                CellLeft = bounds.Left,
                CellWidth = bounds.Width,
                TextTop = line.TextTop,
                TextHeight = line.TextHeight,
                RoomRight = SceneCasting.RoomRightOf(lineText, column, RoomLimit),
                RoomLeft = SceneCasting.RoomLeftOf(lineText, column, RoomLimit),
                Color = BraceColors.Resolve(brace.ColorIndex, settings, BraceColors.IsDarkTheme(_formatMap)),
                Identity = brace.Identity,
            };

            return true;
        }

        /// <summary>
        /// Puts a fresh, empty stage on the layer.
        /// </summary>
        /// <remarks>
        /// Nothing is parented while no scene is playing, which is the usual state — an idle
        /// director owns no adornment at all. The canvas sits at text coordinate zero and props
        /// carry absolute text coordinates, the same discipline as
        /// <see cref="IndentGuideManager"/>: no viewport term anywhere, so nothing can be one
        /// scroll step stale by the time it renders.
        /// </remarks>
        private void Open(IdentityBracesSettings settings, ITextViewLine reference)
        {
            Strike();

            _canvas = new Canvas { IsHitTestVisible = false };
            Canvas.SetLeft(_canvas, 0);
            Canvas.SetTop(_canvas, 0);

            bool added = _layer.AddAdornment(
                AdornmentPositioningBehavior.OwnerControlled,
                null,
                this,
                _canvas,
                null);

            if (!added)
            {
                _canvas = null;
                return;
            }

            _stage = new SceneStage(
                _canvas,
                settings,
                GlyphContextFactory.Build(_formatMap, _view, reference));

            _stageOpenedAt = _tick;
        }

        /// <summary>Ends whatever is playing and clears the layer.</summary>
        private void Strike()
        {
            if (_stage != null)
            {
                _stage.Strike();
                _stage = null;
            }

            if (_canvas != null)
            {
                _layer.RemoveAllAdornments();
                _canvas = null;
            }
        }

        /// <remarks>
        /// Every prop on the stage was placed from the geometry of a line that this pass may
        /// have just moved, resized or reformatted. Rather than chase them, the set is struck:
        /// a scene interrupted by a scroll is a scene nobody was watching anyway.
        /// </remarks>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_stage != null)
            {
                Strike();
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            Reschedule();
        }

        /// <summary>Nothing performs to an empty room.</summary>
        private void OnIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            bool visible = e.NewValue is bool && (bool)e.NewValue;
            if (!visible)
            {
                Strike();
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
            _view.VisualElement.IsVisibleChanged -= OnIsVisibleChanged;
            IdentityBracesSettings.Changed -= OnSettingsChanged;

            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            Strike();
        }
    }
}
