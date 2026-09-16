using System.Windows.Media;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// A smooth hue wheel for animated braces, held at the same relative luminance as the
    /// static palette (0.2072 — the point of equal contrast against dark and light editor
    /// backgrounds).
    /// </summary>
    /// <remarks>
    /// The static palette cannot be reused for animation: its entries are spaced by the
    /// golden angle precisely so neighbours look unrelated, which is the opposite of what a
    /// smooth cycle needs. This ring is evenly spaced instead, so a brace sweeps the wheel
    /// without ever dimming out against the background as it passes through yellow or blue.
    /// </remarks>
    internal static class ColorRing
    {
        public const int Count = 36;
        private const double TargetLuminance = 0.2072;

        private static readonly Color[] Ring = Build();

        public static Color Get(int index)
        {
            return Ring[((index % Count) + Count) % Count];
        }

        private static Color[] Build()
        {
            var ring = new Color[Count];
            for (int i = 0; i < Count; i++)
            {
                ring[i] = SolveForLuminance(i * (360.0 / Count), 0.85);
            }

            return ring;
        }

        /// <summary>
        /// Binary-searches HSL lightness until the result hits <see cref="TargetLuminance"/>.
        /// Cheap enough to run once at type initialisation; hues differ wildly in how much
        /// lightness they need (yellow reaches the target far darker than blue does), so a
        /// fixed lightness would not do.
        /// </summary>
        private static Color SolveForLuminance(double hue, double saturation)
        {
            double lo = 0.0;
            double hi = 1.0;
            Color color = Colors.Gray;

            for (int i = 0; i < 24; i++)
            {
                double mid = (lo + hi) / 2.0;
                color = FromHsl(hue, saturation, mid);
                if (RelativeLuminance(color) < TargetLuminance)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return color;
        }

        private static Color FromHsl(double hue, double saturation, double lightness)
        {
            hue = ((hue % 360.0) + 360.0) % 360.0 / 360.0;
            double a = saturation * (lightness < 0.5 ? lightness : 1.0 - lightness);

            return Color.FromRgb(
                Channel(0, hue, lightness, a),
                Channel(8, hue, lightness, a),
                Channel(4, hue, lightness, a));
        }

        private static byte Channel(double n, double hue, double lightness, double a)
        {
            double k = (n + hue * 12.0) % 12.0;
            double min = k - 3.0;
            if (9.0 - k < min)
            {
                min = 9.0 - k;
            }

            if (1.0 < min)
            {
                min = 1.0;
            }

            if (min < -1.0)
            {
                min = -1.0;
            }

            double value = (lightness - a * min) * 255.0;
            return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
        }

        private static double RelativeLuminance(Color color)
        {
            return 0.2126 * Linearize(color.R)
                 + 0.7152 * Linearize(color.G)
                 + 0.0722 * Linearize(color.B);
        }

        private static double Linearize(byte channel)
        {
            double v = channel / 255.0;
            return v <= 0.03928 ? v / 12.92 : System.Math.Pow((v + 0.055) / 1.055, 2.4);
        }
    }
}
