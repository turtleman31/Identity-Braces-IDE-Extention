using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments.Scenes
{
    /// <summary>
    /// (╯°□°)╯︵ ┻━┻ — a brace throws a table into the empty space beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The simplest scene that genuinely needs a director: it is one brace, but it cannot be
    /// drawn by a per-brace trait painter because those are handed a canvas exactly one
    /// character wide and know nothing about what is next to them. A table thrown without
    /// checking would land in the middle of somebody's identifier.
    /// </para>
    /// <para>
    /// The brace itself does not move. Its visual belongs to <see cref="AdornmentManager"/>,
    /// which rewrites that element's coordinates on every layout pass, and a second owner
    /// reaching in to transform it is precisely the shared-ownership mistake this codebase has
    /// already paid for once. The table launching out of the brace's own cell carries the joke
    /// without needing the thrower to lean.
    /// </para>
    /// </remarks>
    internal sealed class TableFlipScene : IScene
    {
        /// <summary>
        /// Columns of clear space the table needs to land in.
        /// </summary>
        /// <remarks>
        /// Three is the point at which the table is clearly somewhere else rather than
        /// overlapping the brace that threw it. Below that the scene reads as a glitch.
        /// </remarks>
        private const int RequiredRoom = 3;

        /// <summary>How far it travels, in columns, when there is room to spare.</summary>
        private const int PreferredThrow = 4;

        public string TraitId
        {
            get { return TraitIds.TableFlip; }
        }

        /// <summary>Nothing is thrown <em>at</em> anybody. The empty space is the target.</summary>
        public string SubjectTraitId
        {
            get { return null; }
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(2.1); }
        }

        /// <summary>
        /// Casts one brace with somewhere to throw.
        /// </summary>
        /// <remarks>
        /// Right is preferred over left because a brace usually has the rest of its line free
        /// to the right and code to the left. Failing both is the ordinary outcome on a dense
        /// line — the director simply tries again next tick with a different brace.
        /// </remarks>
        public bool TryCast(IList<SceneActor> actors, IList<SceneActor> subjects, List<SceneActor> cast)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                SceneActor actor = actors[i];
                if (actor.RoomRight >= RequiredRoom || actor.RoomLeft >= RequiredRoom)
                {
                    cast.Add(actor);
                    return true;
                }
            }

            return false;
        }

        public void Play(SceneStage stage, IList<SceneActor> cast)
        {
            SceneActor actor = cast[0];

            bool throwRight = actor.RoomRight >= RequiredRoom;
            int room = throwRight ? actor.RoomRight : actor.RoomLeft;
            int columns = Math.Min(PreferredThrow, room);

            double width = actor.CellWidth * 1.5;
            double height = actor.TextHeight * 0.42;

            double startX = actor.CellCenterX - (width / 2.0);
            double landX = startX + (throwRight ? 1 : -1) * columns * actor.CellWidth;

            // Sitting on the baseline rather than centred in the cell, so the table stands on
            // the same line the text does.
            double restY = actor.BaselineY - height;
            double apexY = restY - (actor.TextHeight * 1.15);

            Path table = BuildTable(actor.Color, width, height);

            var spin = new RotateTransform(0);
            table.RenderTransformOrigin = new Point(0.5, 0.5);
            table.RenderTransform = spin;

            stage.Add(table, startX, restY);

            var flight = new Duration(TimeSpan.FromSeconds(0.95));

            // Thrown, not slid: X carries on at a constant rate while Y rises and falls, which
            // is what makes the arc read as a throw rather than a hop.
            stage.Animate(table, System.Windows.Controls.Canvas.LeftProperty,
                new DoubleAnimation(startX, landX, flight));

            var rise = new DoubleAnimationUsingKeyFrames { Duration = flight };
            rise.KeyFrames.Add(new EasingDoubleKeyFrame(restY, KeyTime.FromPercent(0)));
            rise.KeyFrames.Add(new EasingDoubleKeyFrame(apexY, KeyTime.FromPercent(0.42))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
            rise.KeyFrames.Add(new EasingDoubleKeyFrame(restY, KeyTime.FromPercent(1.0))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
            });

            stage.Animate(table, System.Windows.Controls.Canvas.TopProperty, rise);

            // Lands upside down and stays that way. A table that rights itself on landing is a
            // table nobody flipped.
            stage.Animate(spin, RotateTransform.AngleProperty,
                new DoubleAnimation(0, throwRight ? 200 : -200, flight)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                });

            // A beat lying there before it fades, so the punchline lands.
            var settle = new DoubleAnimationUsingKeyFrames { Duration = new Duration(Duration) };
            settle.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0)));
            settle.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.78)));
            settle.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(1.0)));

            stage.Animate(table, UIElement.OpacityProperty, settle);
        }

        /// <summary>
        /// A table top on two legs, as one filled shape.
        /// </summary>
        /// <remarks>
        /// Three rectangles in one geometry rather than three elements, so it is one shape to
        /// animate and it cannot come apart mid-flight. The legs are a fifth of the width
        /// each, which at a 7&#160;px cell is a shade over 2&#160;px — the floor below which a
        /// detail just averages into its neighbours.
        /// </remarks>
        private static Path BuildTable(Color color, double width, double height)
        {
            double topThickness = Math.Max(1.5, height * 0.3);
            double legWidth = Math.Max(1.5, width * 0.19);
            double legTop = topThickness;

            var geometry = new GeometryGroup();
            geometry.Children.Add(new RectangleGeometry(new Rect(0, 0, width, topThickness)));
            geometry.Children.Add(new RectangleGeometry(new Rect(
                width * 0.14, legTop, legWidth, height - legTop)));
            geometry.Children.Add(new RectangleGeometry(new Rect(
                width - (width * 0.14) - legWidth, legTop, legWidth, height - legTop)));
            geometry.Freeze();

            var brush = new SolidColorBrush(color);
            brush.Freeze();

            return new Path
            {
                Data = geometry,
                Fill = brush,
                Width = width,
                Height = height,
                IsHitTestVisible = false,
            };
        }
    }
}
