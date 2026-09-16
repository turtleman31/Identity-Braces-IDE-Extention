using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments.Scenes
{
    /// <summary>
    /// Two braces on the same line visit each other's column, and come back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one scene where the ownership rule bites. The obvious staging is for the two braces
    /// themselves to slide past each other, and a scene may not do that: their visuals belong
    /// to <see cref="AdornmentManager"/>, which rewrites their coordinates on every layout
    /// pass. So each brace sends a likeness instead — a copy of its own glyph, in its own
    /// colour, which arcs across to the other's column and back while the originals hold
    /// their ground.
    /// </para>
    /// <para>
    /// It reads because the copies are the same glyph in the same colour: what you see is two
    /// braces briefly occupying each other's spot. The originals staying put is visible if you
    /// look for it, and the alternative was a mechanism where two components could each move
    /// the same element.
    /// </para>
    /// <para>
    /// Lifting the copies clear of the line as they cross is what stops them reading as a
    /// rendering fault. Two glyphs sliding <em>through</em> the text between them would look
    /// like a redraw bug; two glyphs hopping over it looks deliberate.
    /// </para>
    /// </remarks>
    internal sealed class SwapPlacesScene : IScene
    {
        /// <summary>Furthest apart, in columns, that two braces will bother to trade.</summary>
        /// <remarks>
        /// A swap across forty columns is two things moving independently at opposite ends of
        /// the screen, not a pair trading. Close enough to see both at once is the point.
        /// </remarks>
        private const int MaxSeparation = 24;

        /// <summary>Nearest, in columns. Adjacent braces just look like a wobble.</summary>
        private const int MinSeparation = 2;

        public string TraitId
        {
            get { return TraitIds.SwapPlaces; }
        }

        public string SubjectTraitId
        {
            get { return null; }
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(2.4); }
        }

        /// <summary>
        /// Finds two candidates sharing a line at a sensible distance.
        /// </summary>
        /// <remarks>
        /// The director resolves candidates as a consecutive run through the brace map rather
        /// than at random, which is what makes this likely to succeed: consecutive braces are
        /// usually neighbours on a line. Without that, two independently chosen braces on a
        /// screen would almost never share one.
        /// </remarks>
        public bool TryCast(IList<SceneActor> actors, IList<SceneActor> subjects, List<SceneActor> cast)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                for (int j = i + 1; j < actors.Count; j++)
                {
                    if (actors[i].LineStart != actors[j].LineStart)
                    {
                        continue;
                    }

                    int separation = Math.Abs(actors[i].Column - actors[j].Column);
                    if (separation < MinSeparation || separation > MaxSeparation)
                    {
                        continue;
                    }

                    cast.Add(actors[i]);
                    cast.Add(actors[j]);
                    return true;
                }
            }

            return false;
        }

        public void Play(SceneStage stage, IList<SceneActor> cast)
        {
            SceneActor left = cast[0];
            SceneActor right = cast[1];

            // Arcing in opposite directions, so the two never occupy the same air. Sent the
            // same way they would collide in the middle, which reads as one glyph rather than
            // as two passing.
            Send(stage, left, right, -1);
            Send(stage, right, left, 1);
        }

        /// <summary>Sends <paramref name="from"/>'s likeness to <paramref name="to"/> and back.</summary>
        private void Send(SceneStage stage, SceneActor from, SceneActor to, double lift)
        {
            char character = from.Column <= to.Column ? '{' : '}';

            TextBlock copy = stage.GlyphOf(character, from.Color);
            stage.Add(copy, from.CellLeft, from.TextTop);

            var move = new TranslateTransform();
            copy.RenderTransform = move;

            double travel = to.CellLeft - from.CellLeft;
            double height = from.TextHeight * 0.95 * lift;
            var period = new Duration(Duration);

            var across = new DoubleAnimationUsingKeyFrames { Duration = period };
            across.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.00)));
            across.KeyFrames.Add(new EasingDoubleKeyFrame(travel, KeyTime.FromPercent(0.40))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });
            across.KeyFrames.Add(new LinearDoubleKeyFrame(travel, KeyTime.FromPercent(0.60)));
            across.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.95))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });

            var over = new DoubleAnimationUsingKeyFrames { Duration = period };
            over.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.00)));
            over.KeyFrames.Add(new EasingDoubleKeyFrame(height, KeyTime.FromPercent(0.20))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
            });
            over.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.40))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn },
            });
            over.KeyFrames.Add(new EasingDoubleKeyFrame(height, KeyTime.FromPercent(0.77))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
            });
            over.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.95))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn },
            });

            // Faded at both ends, so a likeness never sits on top of the original it copied —
            // which would show as one brace suddenly rendering twice as boldly.
            var visible = new DoubleAnimationUsingKeyFrames { Duration = period };
            visible.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.00)));
            visible.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.12)));
            visible.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.85)));
            visible.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.96)));

            stage.Animate(move, TranslateTransform.XProperty, across);
            stage.Animate(move, TranslateTransform.YProperty, over);
            stage.Animate(copy, UIElement.OpacityProperty, visible);
        }
    }
}
