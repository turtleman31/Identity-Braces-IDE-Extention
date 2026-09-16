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
    /// A brace crews a fire truck, drives to a burning brace, hoses it down, and goes home.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scene the director was built for. It is the only thing in the catalogue that needs
    /// two braces that do not know about each other and are not adjacent: the crew carries
    /// <c>firebrigade</c>, the casualty carries <c>fire</c>, and neither trait can see the
    /// other from inside a one-character canvas. <see cref="IScene.SubjectTraitId"/> exists
    /// entirely for this.
    /// </para>
    /// <para>
    /// <b>The fire does not go out.</b> It is drawn by the <c>fire</c> effect on the casualty's
    /// own visual, which belongs to <see cref="AdornmentManager"/> and which a scene may not
    /// touch — so the brigade turns out, sprays, raises some steam, and drives home with the
    /// brace still merrily alight. Given what this extension is for, that is the better
    /// ending anyway.
    /// </para>
    /// </remarks>
    internal sealed class FireBrigadeScene : IScene
    {
        /// <summary>Furthest the truck will drive, in columns.</summary>
        /// <remarks>
        /// Both ends have to be on screen at once or the scene is a prop wandering off one edge.
        /// Beyond about this far apart, the crew and the fire cannot be taken in together.
        /// </remarks>
        private const int MaxCallOutDistance = 40;

        private static readonly Color TruckRed = Color.FromRgb(0xC8, 0x2F, 0x2F);
        private static readonly Color TruckTrim = Color.FromRgb(0xF0, 0xE6, 0xD2);
        private static readonly Color Water = Color.FromRgb(0x5A, 0xB8, 0xE8);
        private static readonly Color Steam = Color.FromRgb(0xD8, 0xDE, 0xE4);

        public string TraitId
        {
            get { return TraitIds.FireBrigade; }
        }

        public string SubjectTraitId
        {
            get { return TraitIds.Fire; }
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(3.4); }
        }

        /// <summary>
        /// Casts the nearest crew to a fire, on the same line.
        /// </summary>
        /// <remarks>
        /// Same line only. Driving between rows would mean pathing a prop through the text
        /// vertically, which at this size reads as something falling rather than as a journey —
        /// and the whole joke depends on the truck visibly travelling <em>along</em> the code.
        /// <para>
        /// <c>cast[0]</c> is the fire; <c>cast[1]</c> is the crew turning out to it.
        /// </para>
        /// </remarks>
        public bool TryCast(IList<SceneActor> actors, IList<SceneActor> subjects, List<SceneActor> cast)
        {
            for (int f = 0; f < subjects.Count; f++)
            {
                SceneActor fire = subjects[f];

                int bestIndex = -1;
                int bestDistance = int.MaxValue;

                for (int c = 0; c < actors.Count; c++)
                {
                    if (actors[c].LineStart != fire.LineStart || actors[c].Position == fire.Position)
                    {
                        continue;
                    }

                    int distance = Math.Abs(actors[c].Column - fire.Column);
                    if (distance > 0 && distance <= MaxCallOutDistance && distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = c;
                    }
                }

                if (bestIndex >= 0)
                {
                    cast.Add(fire);
                    cast.Add(actors[bestIndex]);
                    return true;
                }
            }

            return false;
        }

        public void Play(SceneStage stage, IList<SceneActor> cast)
        {
            SceneActor fire = cast[0];
            SceneActor crew = cast[1];

            double width = crew.CellWidth * 1.7;
            double height = crew.TextHeight * 0.40;

            // Stopping a little short, so the truck parks beside the fire rather than on top of
            // it. Parked over the casualty, the whole scene would be one indistinct blob.
            double approach = fire.CellCenterX > crew.CellCenterX ? -width * 0.8 : width * 0.8;

            double startX = crew.CellCenterX - (width / 2.0);
            double stopX = fire.CellCenterX - (width / 2.0) + approach;

            double roadY = crew.BaselineY - height;

            Path truck = BuildTruck(width, height, fire.CellCenterX > crew.CellCenterX);
            stage.Add(truck, startX, roadY);

            var period = new Duration(Duration);

            // Out, hold while working, back. One clock for the whole call-out so the phases
            // cannot drift apart from each other.
            var drive = new DoubleAnimationUsingKeyFrames { Duration = period };
            drive.KeyFrames.Add(new LinearDoubleKeyFrame(startX, KeyTime.FromPercent(0.00)));
            drive.KeyFrames.Add(new EasingDoubleKeyFrame(stopX, KeyTime.FromPercent(0.28))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
            drive.KeyFrames.Add(new LinearDoubleKeyFrame(stopX, KeyTime.FromPercent(0.70)));
            drive.KeyFrames.Add(new EasingDoubleKeyFrame(startX, KeyTime.FromPercent(0.97))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
            });

            stage.Animate(truck, System.Windows.Controls.Canvas.LeftProperty, drive);

            var arriving = new DoubleAnimationUsingKeyFrames { Duration = period };
            arriving.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.00)));
            arriving.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.08)));
            arriving.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.92)));
            arriving.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(1.00)));

            stage.Animate(truck, UIElement.OpacityProperty, arriving);

            Spray(stage, fire, crew, height);
            Puff(stage, fire);
        }

        /// <summary>Three jets of water, arcing from the truck onto the casualty.</summary>
        private static void Spray(SceneStage stage, SceneActor fire, SceneActor crew, double truckHeight)
        {
            bool rightwards = fire.CellCenterX > crew.CellCenterX;

            for (int i = 0; i < 3; i++)
            {
                Ellipse drop = new Ellipse
                {
                    Width = Math.Max(1.5, fire.CellWidth * 0.22),
                    Height = Math.Max(1.5, fire.CellWidth * 0.22),
                    Fill = Frozen(Water),
                    IsHitTestVisible = false,
                    Opacity = 0,
                };

                double from = fire.CellCenterX + (rightwards ? -1 : 1) * fire.CellWidth * 1.3;
                stage.Add(drop, from, fire.BaselineY - truckHeight);

                var move = new TranslateTransform();
                drop.RenderTransform = move;

                var period = new Duration(TimeSpan.FromSeconds(3.4));
                double reach = (fire.CellCenterX - from) * 0.95;

                // Staggered, so the jets read as a stream rather than as three things thrown at
                // once. All three are on the same clock, offset by their key times.
                double open = 0.34 + (i * 0.09);

                var across = new DoubleAnimationUsingKeyFrames { Duration = period };
                across.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
                across.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(open)));
                across.KeyFrames.Add(new LinearDoubleKeyFrame(reach, KeyTime.FromPercent(open + 0.16)));

                var arc = new DoubleAnimationUsingKeyFrames { Duration = period };
                arc.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
                arc.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(open)));
                arc.KeyFrames.Add(new EasingDoubleKeyFrame(-fire.TextHeight * 0.30, KeyTime.FromPercent(open + 0.08))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });
                arc.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(open + 0.16))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
                });

                var visible = new DoubleAnimationUsingKeyFrames { Duration = period };
                visible.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0)));
                visible.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.95, KeyTime.FromPercent(open)));
                visible.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(open + 0.17)));

                stage.Animate(move, TranslateTransform.XProperty, across);
                stage.Animate(move, TranslateTransform.YProperty, arc);
                stage.Animate(drop, UIElement.OpacityProperty, visible);
            }
        }

        /// <summary>Steam off the casualty, which goes on burning regardless.</summary>
        private static void Puff(SceneStage stage, SceneActor fire)
        {
            Ellipse puff = new Ellipse
            {
                Width = fire.CellWidth * 0.9,
                Height = fire.CellWidth * 0.9,
                Fill = Frozen(Steam),
                IsHitTestVisible = false,
                Opacity = 0,
            };

            stage.Add(puff, fire.CellCenterX - (fire.CellWidth * 0.45), fire.TextTop);

            var rise = new TranslateTransform();
            puff.RenderTransform = rise;

            var period = new Duration(TimeSpan.FromSeconds(3.4));

            var up = new DoubleAnimationUsingKeyFrames { Duration = period };
            up.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.00)));
            up.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.42)));
            up.KeyFrames.Add(new LinearDoubleKeyFrame(-fire.TextHeight * 0.8, KeyTime.FromPercent(0.75)));

            var fade = new DoubleAnimationUsingKeyFrames { Duration = period };
            fade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromPercent(0.00)));
            fade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.55, KeyTime.FromPercent(0.42)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.76)));

            stage.Animate(rise, TranslateTransform.YProperty, up);
            stage.Animate(puff, UIElement.OpacityProperty, fade);
        }

        /// <summary>
        /// A truck: body, cab, and two wheels, as one shape.
        /// </summary>
        /// <remarks>
        /// The cab goes at the front, which is how you can tell it is driving somewhere rather
        /// than reversing. At a 7&#160;px cell the whole thing is about twelve pixels across,
        /// so the wheels are the only detail that survives — hence two of them, hard against
        /// the ends, rather than anything finer.
        /// </remarks>
        private static Path BuildTruck(double width, double height, bool facingRight)
        {
            double bodyTop = height * 0.30;
            double wheelSize = Math.Max(2.0, height * 0.42);
            double wheelTop = height - wheelSize;

            var geometry = new GeometryGroup();
            geometry.Children.Add(new RectangleGeometry(new Rect(0, bodyTop, width, height * 0.45)));

            double cabWidth = width * 0.34;
            double cabLeft = facingRight ? width - cabWidth : 0;
            geometry.Children.Add(new RectangleGeometry(new Rect(cabLeft, 0, cabWidth, bodyTop + 1)));

            geometry.Children.Add(new EllipseGeometry(new Rect(width * 0.08, wheelTop, wheelSize, wheelSize)));
            geometry.Children.Add(new EllipseGeometry(new Rect(
                width - (width * 0.08) - wheelSize, wheelTop, wheelSize, wheelSize)));

            geometry.Freeze();

            var truck = new Path
            {
                Data = geometry,
                Fill = Frozen(TruckRed),
                Stroke = Frozen(TruckTrim),
                StrokeThickness = 0.6,
                Width = width,
                Height = height,
                IsHitTestVisible = false,
            };

            return truck;
        }

        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
