using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace IdentityBraces.Options
{
    /// <summary>
    /// Tools &gt; Options &gt; Identity Braces &gt; General.
    /// </summary>
    /// <remarks>
    /// This is only the UI. The values live in <see cref="IdentityBracesSettings"/>, which
    /// owns persistence, so the editor components never have to wait for a package to load
    /// or marshal to the UI thread to read a setting.
    /// </remarks>
    [Guid("4CBC24FA-96EB-487A-9E5E-9A9833FF0B2F")]
    [ComVisible(true)]
    public sealed class GeneralOptionsPage : DialogPage
    {
        private const string CategoryGeneral = "General";
        private const string CategoryBrackets = "Brackets";
        private const string CategoryPersonalities = "Personalities";
        private const string CategoryMotion = "Motion";
        private const string CategoryPerformance = "Performance";
        private const string CategoryAppearance = "Appearance";
        private const string CategoryStructure = "Structure";

        public GeneralOptionsPage()
        {
            CopyFrom(IdentityBracesSettings.Current);
        }

        [Category(CategoryGeneral)]
        [DisplayName("Enabled")]
        [Description("Turns Identity Braces off entirely, leaving the language service's own colours in place.")]
        public bool Enabled { get; set; }

        [Category(CategoryBrackets)]
        [DisplayName("Colour curly braces")]
        [Description("Give { and } identities.")]
        public bool ColorCurlyBraces { get; set; }

        [Category(CategoryBrackets)]
        [DisplayName("Colour parentheses")]
        [Description("Give ( and ) identities.")]
        public bool ColorParentheses { get; set; }

        [Category(CategoryBrackets)]
        [DisplayName("Colour square brackets")]
        [Description("Give [ and ] identities.")]
        public bool ColorSquareBrackets { get; set; }

        [Category(CategoryGeneral)]
        [DisplayName("Colour mode")]
        [Description("Palette gives every brace one of 32 identity colours. Monochrome gives them all a single colour that follows the editor theme: white on a dark background, black on a light one. Depth colours by nesting level instead, so a pair agrees with itself and the file reads like an ordinary rainbow-brace extension. Personalities are unaffected by all three.")]
        public BraceColorMode ColorMode { get; set; }

        [Category(CategoryStructure)]
        [DisplayName("Scope spotlight")]
        [Description("The pair enclosing the caret stays vivid and every other brace fades back. With 32 colours competing for attention this is the one thing that still tells you which block you are in.")]
        public bool ScopeSpotlight { get; set; }

        [Category(CategoryStructure)]
        [DisplayName("Spotlight dim (%)")]
        [Description("How visible an out-of-scope brace stays while the spotlight is on. 5 to 100; 30 is faint but still legible.")]
        public int SpotlightDimPercent { get; set; }

        [Category(CategoryStructure)]
        [DisplayName("Complexity warning depth")]
        [Description("Braces nested at least this deep are drawn visibly distressed — sweating and unsteady. 0 turns it off; 2 is the shallowest threshold that means anything. This is the one trait applied by a rule about your code rather than by a dice roll, so it ignores the weight table entirely.")]
        public int ComplexityWarningDepth { get; set; }

        [Category(CategoryStructure)]
        [DisplayName("Coloured indent guides")]
        [Description("Draws a vertical guide down the inside of every multi-line pair, in that pair's own colour, so a guide and the braces it joins agree. Combined with Depth colour mode this is an ordinary, even useful, rainbow indent.")]
        public bool IndentGuides { get; set; }

        [Category(CategoryStructure)]
        [DisplayName("Indent guide strength (%)")]
        [Description("How strong the coloured indent guides are against the background. 5 to 100.")]
        public int IndentGuideOpacityPercent { get; set; }

        [Category(CategoryAppearance)]
        [DisplayName("Brace size (%)")]
        [Description("Scales every brace, plain and drawn alike, from 25 to 400 percent. Applied through the classification, so the character cell grows with the glyph and surrounding code reflows around it.")]
        public int BraceScalePercent { get; set; }

        [Category(CategoryAppearance)]
        [DisplayName("Cat ear size (%)")]
        [Description("Scales the cat ears independently of the brace, from 25 to 400 percent. Larger ears reserve or overhang proportionally more space above the line.")]
        public int EarScalePercent { get; set; }

        [Category(CategoryGeneral)]
        [DisplayName("Every brace is its own person")]
        [Description("A closing brace gets its own identity instead of inheriting its opener's, so { and its } are different colours and may be different creatures entirely. Matching pairs sharing a colour was the last thing here that still helped you read code. Turn this off to get it back.")]
        public bool IndependentBraces { get; set; }

        [Category(CategoryPersonalities)]
        [DisplayName("Thigh highs")]
        [Description("How much stripe detail the stockings carry. They are painted onto the brace's own stroke, so they are never wider than the glyph. Garter is one welt stripe at the 2px legibility floor; Banded is the proper two-stripe welt, which only resolves at larger font sizes.")]
        public StockingStyle Stocking { get; set; }

        [Category(CategoryPersonalities)]
        [DisplayName("Tail")]
        [Description("A tail curling off the bottom right of a catgirl brace. It overhangs the next character cell slightly, but it is what makes the glyph read as a creature rather than a decorated bracket.")]
        public bool CatgirlTail { get; set; }

        [Category(CategoryPersonalities)]
        [DisplayName("Reserve space for cat ears")]
        [Description("Makes lines carrying a catgirl brace physically taller so the ears have room. Off by default: this is the only setting that changes the document's height, so it is the only one that can disturb the editor's layout. With it off the ears simply overhang the line above.")]
        public bool ReserveEarSpace { get; set; }

        [Category(CategoryPersonalities)]
        [DisplayName("Unmatched braces question themselves")]
        [Description("A brace with no partner is always drawn as '?'. Doubles as a syntax-error hint.")]
        public bool QuestionUnmatched { get; set; }

        [Category(CategoryMotion)]
        [DisplayName("Enable motion")]
        [Description("Off leaves animated braces on a static colour and stops the questioning wobble. Turn this off on battery, on a projector, or if movement in a text editor is unpleasant.")]
        public bool EnableMotion { get; set; }

        [Category(CategoryMotion)]
        [DisplayName("Animation frame rate")]
        [Description("Frames per second for colour cycling. 1-60. Lower is cheaper; 18 is smooth enough for a slow fade.")]
        public int AnimationFrameRate { get; set; }

        [Category(CategoryMotion)]
        [DisplayName("Colour cycle seconds")]
        [Description("Seconds for an animated brace to travel the whole colour wheel. 1-120.")]
        public int CycleSeconds { get; set; }

        [Category(CategoryMotion)]
        [DisplayName("Scene interval (seconds)")]
        [Description("How often a view tries to play a multi-brace scene, such as a table flip. Most attempts find nothing eligible on screen and do nothing, so this is an upper bound on how often anything happens rather than a schedule. 1-600. Scenes need Enable motion, and need the relevant trait turned up on the Traits page.")]
        public int SceneIntervalSeconds { get; set; }

        [Category(CategoryPerformance)]
        [DisplayName("Write diagnostic log")]
        [Description("Appends a line per layout pass to %APPDATA%\\IdentityBraces\\diagnostic.log. Off by default; enable it only when diagnosing a rendering fault, and turn it off again afterwards.")]
        public bool DiagnosticLog { get; set; }

        [Category(CategoryPerformance)]
        [DisplayName("Maximum file length")]
        [Description("Files longer than this many characters are left alone. Every edit rescans the file, so this keeps typing responsive in very large documents.")]
        public int MaxFileLength { get; set; }

        public override void LoadSettingsFromStorage()
        {
            CopyFrom(IdentityBracesSettings.Current);
        }

        public override void SaveSettingsToStorage()
        {
            IdentityBracesSettings.Save(ToSettings());
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            CopyFrom(IdentityBracesSettings.Current);
            base.OnActivate(e);
        }

        private void CopyFrom(IdentityBracesSettings settings)
        {
            Enabled = settings.Enabled;
            ColorCurlyBraces = settings.ColorCurlyBraces;
            ColorParentheses = settings.ColorParentheses;
            ColorSquareBrackets = settings.ColorSquareBrackets;
            QuestionUnmatched = settings.QuestionUnmatched;
            IndependentBraces = settings.IndependentBraces;
            ColorMode = settings.ColorMode;
            ScopeSpotlight = settings.ScopeSpotlight;
            SpotlightDimPercent = settings.SpotlightDimPercent;
            ComplexityWarningDepth = settings.ComplexityWarningDepth;
            IndentGuides = settings.IndentGuides;
            IndentGuideOpacityPercent = settings.IndentGuideOpacityPercent;
            BraceScalePercent = settings.BraceScalePercent;
            EarScalePercent = settings.EarScalePercent;
            Stocking = settings.Stocking;
            CatgirlTail = settings.CatgirlTail;
            ReserveEarSpace = settings.ReserveEarSpace;
            DiagnosticLog = settings.DiagnosticLog;
            EnableMotion = settings.EnableMotion;
            AnimationFrameRate = settings.AnimationFrameRate;
            CycleSeconds = settings.CycleSeconds;
            SceneIntervalSeconds = settings.SceneIntervalSeconds;
            MaxFileLength = settings.MaxFileLength;
        }

        private IdentityBracesSettings ToSettings()
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current.Clone();
            settings.Enabled = Enabled;
            settings.ColorCurlyBraces = ColorCurlyBraces;
            settings.ColorParentheses = ColorParentheses;
            settings.ColorSquareBrackets = ColorSquareBrackets;
            settings.QuestionUnmatched = QuestionUnmatched;
            settings.IndependentBraces = IndependentBraces;
            settings.ColorMode = ColorMode;
            settings.ScopeSpotlight = ScopeSpotlight;
            settings.SpotlightDimPercent = SpotlightDimPercent;
            settings.ComplexityWarningDepth = ComplexityWarningDepth;
            settings.IndentGuides = IndentGuides;
            settings.IndentGuideOpacityPercent = IndentGuideOpacityPercent;
            settings.BraceScalePercent = BraceScalePercent;
            settings.EarScalePercent = EarScalePercent;
            settings.Stocking = Stocking;
            settings.CatgirlTail = CatgirlTail;
            settings.ReserveEarSpace = ReserveEarSpace;
            settings.DiagnosticLog = DiagnosticLog;
            settings.EnableMotion = EnableMotion;
            settings.AnimationFrameRate = AnimationFrameRate;
            settings.CycleSeconds = CycleSeconds;
            settings.SceneIntervalSeconds = SceneIntervalSeconds;
            settings.MaxFileLength = MaxFileLength;
            return settings;
        }
    }
}
