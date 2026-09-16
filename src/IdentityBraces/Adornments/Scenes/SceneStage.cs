using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IdentityBraces.Options;

namespace IdentityBraces.Adornments.Scenes
{
    /// <summary>
    /// One brace that a scene may act on, with everything it needs to know about where that
    /// brace is and what is around it.
    /// </summary>
    /// <remarks>
    /// Resolved by the director from live line geometry immediately before a scene is played,
    /// and never held across a layout. See <see cref="SceneDirector"/> for why that is the
    /// whole safety story.
    /// </remarks>
    internal struct SceneActor
    {
        /// <summary>Buffer position of the brace.</summary>
        public int Position;

        /// <summary>Start of the line this brace is on, so two actors can tell they share one.</summary>
        public int LineStart;

        /// <summary>Column of the brace within its line.</summary>
        public int Column;

        /// <summary>Left edge of its character cell, in the layer's text coordinates.</summary>
        public double CellLeft;

        /// <summary>Width of one character cell.</summary>
        public double CellWidth;

        /// <summary>Top of the line's text, in the layer's text coordinates.</summary>
        public double TextTop;

        /// <summary>Height of the line's text.</summary>
        public double TextHeight;

        /// <summary>Blank columns to the right before the next character.</summary>
        public int RoomRight;

        /// <summary>Blank columns to the left before the previous character.</summary>
        public int RoomLeft;

        /// <summary>The brace's own colour, so a prop can match its owner.</summary>
        public Color Color;

        /// <summary>Stable per-brace value, for scenes wanting repeatable variety.</summary>
        public ulong Identity;

        public double CellCenterX
        {
            get { return CellLeft + (CellWidth / 2.0); }
        }

        public double BaselineY
        {
            get { return TextTop + (TextHeight * 0.82); }
        }
    }

    /// <summary>
    /// What a scene is allowed to do: put transient props on the stage and animate them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately narrow. A scene cannot reach the view, the buffer, or anybody else's
    /// adornments — it gets a canvas of its own and a clock. Every prop it adds is tracked
    /// here, so the director can strike the whole set in one call without a scene having to
    /// remember to clean up after itself.
    /// </para>
    /// <para>
    /// Note there is no access to the <em>brace</em> visuals. Those belong to
    /// <see cref="AdornmentManager"/>, which rewrites their coordinates on every layout pass;
    /// a second owner reaching in to transform them is exactly the kind of shared ownership
    /// that cost this project ten rounds of debugging the first time round. A scene animates
    /// its own props and leaves the cast standing.
    /// </para>
    /// </remarks>
    internal sealed class SceneStage
    {
        private readonly Canvas _canvas;
        private readonly List<ClockController> _clocks = new List<ClockController>();

        public SceneStage(Canvas canvas, IdentityBracesSettings settings, GlyphContext glyph)
        {
            _canvas = canvas;
            Settings = settings;
            Glyph = glyph;
        }

        public IdentityBracesSettings Settings { get; private set; }

        /// <summary>Font metrics, so props are sized in the same ink units traits use.</summary>
        public GlyphContext Glyph { get; private set; }

        /// <summary>
        /// A copy of a brace's glyph, for scenes that need to move a brace that they are not
        /// allowed to move.
        /// </summary>
        /// <remarks>
        /// The original stays exactly where it is — see the note on this class about ownership.
        /// A scene that wants a brace to appear to go somewhere sends a likeness.
        /// </remarks>
        public TextBlock GlyphOf(char character, Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            return new TextBlock
            {
                Text = character.ToString(),
                FontFamily = Glyph.Typeface == null ? null : Glyph.Typeface.FontFamily,
                FontStyle = Glyph.Typeface == null ? FontStyles.Normal : Glyph.Typeface.Style,
                FontWeight = Glyph.Typeface == null ? FontWeights.Normal : Glyph.Typeface.Weight,
                FontSize = Glyph.FontSize,
                Foreground = brush,
                IsHitTestVisible = false,
            };
        }

        /// <summary>Adds a prop at a position in the layer's text coordinates.</summary>
        public T Add<T>(T prop, double left, double top) where T : UIElement
        {
            prop.IsHitTestVisible = false;
            Canvas.SetLeft(prop, left);
            Canvas.SetTop(prop, top);
            _canvas.Children.Add(prop);
            return prop;
        }

        /// <summary>Registers an animation so the director can stop it when the scene ends.</summary>
        /// <remarks>
        /// A scene is struck mid-flight whenever the view scrolls, so its clocks outlive its
        /// props unless something holds them. Every clock started through here is stopped by
        /// <see cref="Strike"/>.
        /// </remarks>
        public void Animate(IAnimatable target, DependencyProperty property, AnimationTimeline animation)
        {
            if (!Settings.EnableMotion)
            {
                return;
            }

            Timeline.SetDesiredFrameRate(animation, Settings.AnimationFrameRate);
            AnimationClock clock = animation.CreateClock();
            target.ApplyAnimationClock(property, clock);

            if (clock.Controller != null)
            {
                _clocks.Add(clock.Controller);
            }
        }

        /// <summary>Runs <paramref name="action"/> once the scene has had its time.</summary>
        public void WhenFinished(TimeSpan after, Action action)
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = after };
            timer.Tick += delegate
            {
                timer.Stop();
                action();
            };

            timer.Start();
            _finishTimer = timer;
        }

        private System.Windows.Threading.DispatcherTimer _finishTimer;

        /// <summary>Tears the whole set down: clocks stopped, props removed.</summary>
        public void Strike()
        {
            if (_finishTimer != null)
            {
                _finishTimer.Stop();
                _finishTimer = null;
            }

            for (int i = 0; i < _clocks.Count; i++)
            {
                _clocks[i].Stop();
            }

            _clocks.Clear();
            _canvas.Children.Clear();
        }
    }

    /// <summary>
    /// A multi-brace performance: something above the per-brace factory that needs more than
    /// one glyph, or more than one cell, to make sense.
    /// </summary>
    /// <remarks>
    /// The per-brace draw functions in <c>Creatures</c>, <c>Costumes</c>, <c>Motions</c> and
    /// <c>Effects</c> are handed a canvas the size of one character and know nothing about
    /// their neighbours or the text around them. That is the right shape for eighty-odd traits
    /// and the wrong shape for a table flip, which needs to know there is empty space to throw
    /// a table into, and for a fire brigade, which needs to find a burning brace and travel to
    /// it.
    /// </remarks>
    internal interface IScene
    {
        /// <summary>The effect a brace must carry to perform in this scene.</summary>
        string TraitId { get; }

        /// <summary>
        /// A second effect identifying something the scene acts <em>upon</em>, or null.
        /// </summary>
        /// <remarks>
        /// The fire brigade is why this exists: its actors carry <c>firebrigade</c> but the
        /// thing they turn out for carries <c>fire</c>, and a scene that could only ask about
        /// one trait could not describe that. Scenes with a single cast leave it null.
        /// </remarks>
        string SubjectTraitId { get; }

        /// <summary>How long the performance runs.</summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Picks a cast, or returns false to sit this round out.
        /// </summary>
        /// <remarks>
        /// Returning false is the normal outcome for a scene with nothing to do — no eligible
        /// brace on screen, none with room to perform, or nothing to perform at. The director
        /// just tries again later.
        /// <para>
        /// What the cast means is the scene's own business; <see cref="Play"/> is the only
        /// thing that reads it. The convention is that <c>cast[0]</c> is whatever the scene is
        /// centred on.
        /// </para>
        /// </remarks>
        bool TryCast(IList<SceneActor> actors, IList<SceneActor> subjects, List<SceneActor> cast);

        /// <summary>Plays the scene. Everything drawn must go through <paramref name="stage"/>.</summary>
        void Play(SceneStage stage, IList<SceneActor> cast);
    }
}
