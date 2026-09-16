using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using IdentityBraces.Options;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Everything a trait's draw function needs, plus primitives to draw with.
    /// </summary>
    /// <remarks>
    /// Coordinates are supplied in <em>ink units</em>: 0 is the horizontal centre of the
    /// glyph's ink and the top of it vertically, and 1 unit is the ink's height. So a shape
    /// authored once holds its proportions at any font, size or zoom — which is the only way
    /// eighty-five traits stay maintainable.
    /// </remarks>
    internal sealed class BraceDrawContext
    {
        public Canvas Canvas;
        public GlyphContext Glyph;
        public GlyphInk Ink;
        public BraceVisual Visual;
        public IdentityBracesSettings Settings;

        public char Character;
        public ulong Identity;
        public Color Color;

        /// <summary>Canvas Y of the ink's top edge.</summary>
        public double InkTop;

        /// <summary>Canvas Y of the ink's bottom edge.</summary>
        public double InkBottom;

        /// <summary>Ink height in device pixels. The unit for every authored coordinate.</summary>
        public double Unit;

        /// <summary>Canvas X of the ink's horizontal centre.</summary>
        public double CenterX;

        /// <summary>Multiplier for creature and costume geometry.</summary>
        public double DecorScale = 1.0;

        /// <summary>The glyph element itself, for traits that transform rather than add.</summary>
        public FrameworkElement GlyphElement;

        // ---- coordinate conversion ----

        public double X(double units)
        {
            return CenterX + (units * Unit * DecorScale);
        }

        public double Y(double units)
        {
            return InkTop + (units * Unit * DecorScale);
        }

        public Point P(double x, double y)
        {
            return new Point(X(x), Y(y));
        }

        // ---- primitives ----

        public Polygon Triangle(Color color, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            var polygon = new Polygon { Fill = Frozen(color), IsHitTestVisible = false };
            polygon.Points.Add(P(x1, y1));
            polygon.Points.Add(P(x2, y2));
            polygon.Points.Add(P(x3, y3));
            Canvas.Children.Add(polygon);
            return polygon;
        }

        public Ellipse Dot(Color color, double cx, double cy, double radius)
        {
            double r = radius * Unit * DecorScale;
            var ellipse = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Fill = Frozen(color),
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(ellipse, X(cx) - r);
            Canvas.SetTop(ellipse, Y(cy) - r);
            Canvas.Children.Add(ellipse);
            return ellipse;
        }

        public Ellipse Ring(Color color, double cx, double cy, double radius, double thickness)
        {
            double r = radius * Unit * DecorScale;
            var ellipse = new Ellipse
            {
                Width = r * 2,
                Height = r * 1.1,
                Stroke = Frozen(color),
                StrokeThickness = Math.Max(1.0, thickness * Unit * DecorScale),
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(ellipse, X(cx) - r);
            Canvas.SetTop(ellipse, Y(cy) - (r * 0.55));
            Canvas.Children.Add(ellipse);
            return ellipse;
        }

        public Rectangle Box(Color color, double x, double y, double w, double h, double radius = 0)
        {
            var rect = new Rectangle
            {
                Width = Math.Max(1.0, w * Unit * DecorScale),
                Height = Math.Max(1.0, h * Unit * DecorScale),
                Fill = Frozen(color),
                RadiusX = radius * Unit * DecorScale,
                RadiusY = radius * Unit * DecorScale,
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(rect, X(x));
            Canvas.SetTop(rect, Y(y));
            Canvas.Children.Add(rect);
            return rect;
        }

        public Path Stroke(Color color, double thickness, params Point[] points)
        {
            if (points.Length < 2)
            {
                return null;
            }

            var figure = new PathFigure { StartPoint = points[0] };
            for (int i = 1; i < points.Length; i++)
            {
                figure.Segments.Add(new LineSegment(points[i], true));
            }

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();

            var path = new Path
            {
                Data = geometry,
                Stroke = Frozen(color),
                StrokeThickness = Math.Max(1.0, thickness * Unit * DecorScale),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false,
            };

            Canvas.Children.Add(path);
            return path;
        }

        public Path Curve(Color color, double thickness, Point start, Point c1, Point c2, Point end)
        {
            var figure = new PathFigure { StartPoint = start };
            figure.Segments.Add(new BezierSegment(c1, c2, end, true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();

            var path = new Path
            {
                Data = geometry,
                Stroke = Frozen(color),
                StrokeThickness = Math.Max(1.0, thickness * Unit * DecorScale),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
            };

            Canvas.Children.Add(path);
            return path;
        }

        /// <summary>
        /// Supplied by the factory: draws another copy of the glyph in a colour, cropped to a
        /// horizontal band given in ink units.
        /// </summary>
        public Action<Color, double, double> BandPainter;

        /// <summary>
        /// Supplied by the factory: a whole second copy of the glyph, placed exactly over the
        /// original and returned for the caller to move.
        /// </summary>
        public Func<Color, FrameworkElement> GlyphCopier;

        /// <summary>
        /// Another complete glyph, sitting on top of this one.
        /// </summary>
        /// <remarks>
        /// For traits that need a whole second brace rather than a decoration —
        /// <c>mitosis</c> buds one off and lets it drift away. It goes through the factory
        /// rather than being built here so it is the same glyph: same body substitution, same
        /// font, same emphasis. A hand-rolled <c>TextBlock</c> would silently stop matching the
        /// moment a body trait was rolled on the same brace.
        /// <para>
        /// The canvas does not clip, so a copy may be moved outside the character cell.
        /// </para>
        /// </remarks>
        public FrameworkElement CopyGlyph(Color color)
        {
            return GlyphCopier == null ? null : GlyphCopier(color);
        }

        /// <summary>
        /// Recolours part of the glyph's own stroke.
        /// </summary>
        /// <remarks>
        /// The band is a clipped copy of the glyph, never a shape drawn behind it, so it is
        /// exactly as wide as the stroke at any font size. This is what makes the thigh highs
        /// work; anything that wants to paint "part of the brace" should use it rather than
        /// drawing a rectangle and hoping the widths agree.
        /// </remarks>
        public void ClipGlyphBand(Color color, double topUnits, double bottomUnits)
        {
            if (BandPainter != null)
            {
                BandPainter(color, topUnits, bottomUnits);
            }
        }

        /// <summary>Registers an animation so the view can pause it when the tab is hidden.</summary>
        public void Animate(IAnimatable target, DependencyProperty property, AnimationTimeline animation)
        {
            if (!Settings.EnableMotion)
            {
                return;
            }

            Timeline.SetDesiredFrameRate(animation, Settings.AnimationFrameRate);
            AnimationClock clock = animation.CreateClock();
            target.ApplyAnimationClock(property, clock);
            Visual.AddClock(clock.Controller);
        }

        /// <summary>
        /// A deterministic value in [0,1) from this brace's identity, for traits that want
        /// stable variety — a lean angle, a colour pick, a phase offset.
        /// </summary>
        public double Roll(ulong salt)
        {
            return Core.Hash.ToUnitInterval(Core.Hash.Mix(Identity, salt));
        }

        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
