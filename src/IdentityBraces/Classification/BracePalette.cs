using System.Windows.Media;

namespace IdentityBraces.Classification
{
    /// <summary>
    /// The 32 identities a brace can wear.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hues are spaced by the golden angle (137.508 degrees) rather than evenly, so
    /// consecutive indices — which frequently end up next to each other on screen — land far
    /// apart on the colour wheel instead of shading into one another.
    /// </para>
    /// <para>
    /// Every entry was then lightness-solved to a relative luminance of 0.2072, the point at
    /// which contrast against the dark editor background (#1E1E1E) and against white are
    /// equal. The result is ~4.05:1 on both, so one palette serves every theme instead of
    /// looking correct on dark and vanishing on light. Retune any of them in
    /// Tools &gt; Options &gt; Environment &gt; Fonts and Colors.
    /// </para>
    /// </remarks>
    internal static class BracePalette
    {
        public const int Count = 32;

        private static readonly uint[] Argb =
        {
            0xFFEE3333, // 00
            0xFF19903C, // 01
            0xFFB047FA, // 02
            0xFF8C7E21, // 03
            0xFF0D89A2, // 04
            0xFFDD3C93, // 05
            0xFF279204, // 06
            0xFF7570DE, // 07
            0xFFD75311, // 08
            0xFF198E63, // 09
            0xFFD806EB, // 10
            0xFF718620, // 11
            0xFF137DE9, // 12
            0xFFDE4565, // 13
            0xFF049310, // 14
            0xFF9266DB, // 15
            0xFFA7740E, // 16
            0xFF198C88, // 17
            0xFFEA06B0, // 18
            0xFF528C21, // 19
            0xFF5A73F2, // 20
            0xFFDC4D38, // 21
            0xFF04923F, // 22
            0xFFB155D7, // 23
            0xFF82820B, // 24
            0xFF1F87B2, // 25
            0xFFF60669, // 26
            0xFF2F9022, // 27
            0xFF7F68F3, // 28
            0xFFBC6821, // 29
            0xFF048E6C, // 30
            0xFFD139CA, // 31
        };

        public static Color GetColor(int index)
        {
            uint value = Argb[((index % Count) + Count) % Count];
            return Color.FromArgb(
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value);
        }
    }
}
