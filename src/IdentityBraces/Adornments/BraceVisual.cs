using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace IdentityBraces.Adornments
{
    /// <summary>One drawn brace, plus any clocks the view needs to pause when it loses focus.</summary>
    internal sealed class BraceVisual
    {
        private List<ClockController> _clocks;

        public UIElement Element;

        /// <summary>
        /// How far above the line's text top this element starts. Non-zero only for cat
        /// ears, which live in the space <see cref="BraceLineTransformSource"/> reserves.
        /// </summary>
        public double TopOffset;

        /// <summary>
        /// Buffer offset of the brace this draws, so its position can be re-derived from
        /// live line geometry on every layout instead of being trusted from creation time.
        /// </summary>
        public int Position;

        /// <summary>A short tag naming what drew this, so diagnostics can separate them.</summary>
        public string Kind;

        /// <summary>Edges away when the caret comes near. See <c>fleecursor</c>.</summary>
        /// <remarks>
        /// Recorded here rather than looked up per caret move: the manager walks the layer's
        /// elements, which hand back a <see cref="BraceVisual"/> and not a
        /// <see cref="Core.BraceInfo"/>, and re-deriving the traits would mean a map lookup per
        /// element per keystroke.
        /// </remarks>
        public bool FleesCursor;

        /// <summary>Fades while the caret is on its line. See <c>stagefright</c>.</summary>
        public bool HasStageFright;

        /// <summary>
        /// Horizontal nudge applied on top of the character cell, in device pixels.
        /// </summary>
        /// <remarks>
        /// Applied through the placement rather than a <c>RenderTransform</c>, because the
        /// transform is already contested — several effects and every motion trait compose into
        /// it. Placement has exactly one owner, so there is nothing to fight.
        /// </remarks>
        public double FleeOffsetX;

        public bool HasClocks
        {
            get { return _clocks != null && _clocks.Count > 0; }
        }

        public void AddClock(ClockController controller)
        {
            if (controller == null)
            {
                return;
            }

            if (_clocks == null)
            {
                _clocks = new List<ClockController>(2);
            }

            _clocks.Add(controller);
        }

        public void Pause()
        {
            if (_clocks == null)
            {
                return;
            }

            for (int i = 0; i < _clocks.Count; i++)
            {
                _clocks[i].Pause();
            }
        }

        public void Resume()
        {
            if (_clocks == null)
            {
                return;
            }

            for (int i = 0; i < _clocks.Count; i++)
            {
                _clocks[i].Resume();
            }
        }

        public void Stop()
        {
            if (_clocks == null)
            {
                return;
            }

            for (int i = 0; i < _clocks.Count; i++)
            {
                _clocks[i].Stop();
            }

            _clocks = null;
        }
    }
}
