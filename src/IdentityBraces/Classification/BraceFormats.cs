using System.ComponentModel.Composition;
using System.Globalization;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Classification
{
    /// <summary>Shared construction for the 32 palette entries.</summary>
    internal abstract class BraceFormatBase : ClassificationFormatDefinition
    {
        protected BraceFormatBase(int index)
        {
            DisplayName = "Identity Brace " + index.ToString("00", CultureInfo.InvariantCulture);
            ForegroundColor = BracePalette.GetColor(index);
        }
    }

    /// <summary>
    /// Paints a brace with nothing at all. Used for braces the adornment layer takes over:
    /// the character still occupies its cell, so the caret, selection and word-wrap are
    /// untouched, but the glyph is drawn by us instead of by the editor.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = BraceClassificationNames.Hidden)]
    [Name(BraceClassificationNames.Hidden)]
    [UserVisible(false)]
    [Order(After = Priority.High)]
    internal sealed class HiddenBraceFormat : ClassificationFormatDefinition
    {
        public HiddenBraceFormat()
        {
            DisplayName = "Identity Brace (drawn by adornment)";
            ForegroundColor = Colors.Transparent;
        }
    }

    /// <summary>
    /// Carries opacity and nothing else, so it can be layered over any palette entry to fade
    /// a brace that is outside the caret's scope.
    /// </summary>
    /// <remarks>
    /// Ordered <em>after</em> the palette formats so its opacity wins the merge. It
    /// deliberately sets no foreground colour: leaving that property empty is what lets the
    /// palette entry underneath supply it.
    /// <para>
    /// The opacity here is the default. The live value comes from
    /// <c>SpotlightDimPercent</c> and is written to the format map by
    /// <see cref="BraceFormatOverrides"/>.
    /// </para>
    /// </remarks>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = BraceClassificationNames.Dim)]
    [Name(BraceClassificationNames.Dim)]
    [UserVisible(false)]
    [Order(After = Priority.High)]
    internal sealed class DimBraceFormat : ClassificationFormatDefinition
    {
        public DimBraceFormat()
        {
            DisplayName = "Identity Brace (outside the caret's scope)";
            ForegroundOpacity = 0.30;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace00")]
    [Name("IdentityBrace00")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat00 : BraceFormatBase
    {
        public BraceFormat00() : base(0)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace01")]
    [Name("IdentityBrace01")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat01 : BraceFormatBase
    {
        public BraceFormat01() : base(1)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace02")]
    [Name("IdentityBrace02")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat02 : BraceFormatBase
    {
        public BraceFormat02() : base(2)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace03")]
    [Name("IdentityBrace03")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat03 : BraceFormatBase
    {
        public BraceFormat03() : base(3)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace04")]
    [Name("IdentityBrace04")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat04 : BraceFormatBase
    {
        public BraceFormat04() : base(4)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace05")]
    [Name("IdentityBrace05")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat05 : BraceFormatBase
    {
        public BraceFormat05() : base(5)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace06")]
    [Name("IdentityBrace06")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat06 : BraceFormatBase
    {
        public BraceFormat06() : base(6)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace07")]
    [Name("IdentityBrace07")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat07 : BraceFormatBase
    {
        public BraceFormat07() : base(7)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace08")]
    [Name("IdentityBrace08")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat08 : BraceFormatBase
    {
        public BraceFormat08() : base(8)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace09")]
    [Name("IdentityBrace09")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat09 : BraceFormatBase
    {
        public BraceFormat09() : base(9)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace10")]
    [Name("IdentityBrace10")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat10 : BraceFormatBase
    {
        public BraceFormat10() : base(10)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace11")]
    [Name("IdentityBrace11")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat11 : BraceFormatBase
    {
        public BraceFormat11() : base(11)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace12")]
    [Name("IdentityBrace12")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat12 : BraceFormatBase
    {
        public BraceFormat12() : base(12)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace13")]
    [Name("IdentityBrace13")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat13 : BraceFormatBase
    {
        public BraceFormat13() : base(13)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace14")]
    [Name("IdentityBrace14")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat14 : BraceFormatBase
    {
        public BraceFormat14() : base(14)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace15")]
    [Name("IdentityBrace15")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat15 : BraceFormatBase
    {
        public BraceFormat15() : base(15)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace16")]
    [Name("IdentityBrace16")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat16 : BraceFormatBase
    {
        public BraceFormat16() : base(16)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace17")]
    [Name("IdentityBrace17")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat17 : BraceFormatBase
    {
        public BraceFormat17() : base(17)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace18")]
    [Name("IdentityBrace18")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat18 : BraceFormatBase
    {
        public BraceFormat18() : base(18)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace19")]
    [Name("IdentityBrace19")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat19 : BraceFormatBase
    {
        public BraceFormat19() : base(19)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace20")]
    [Name("IdentityBrace20")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat20 : BraceFormatBase
    {
        public BraceFormat20() : base(20)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace21")]
    [Name("IdentityBrace21")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat21 : BraceFormatBase
    {
        public BraceFormat21() : base(21)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace22")]
    [Name("IdentityBrace22")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat22 : BraceFormatBase
    {
        public BraceFormat22() : base(22)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace23")]
    [Name("IdentityBrace23")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat23 : BraceFormatBase
    {
        public BraceFormat23() : base(23)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace24")]
    [Name("IdentityBrace24")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat24 : BraceFormatBase
    {
        public BraceFormat24() : base(24)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace25")]
    [Name("IdentityBrace25")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat25 : BraceFormatBase
    {
        public BraceFormat25() : base(25)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace26")]
    [Name("IdentityBrace26")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat26 : BraceFormatBase
    {
        public BraceFormat26() : base(26)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace27")]
    [Name("IdentityBrace27")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat27 : BraceFormatBase
    {
        public BraceFormat27() : base(27)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace28")]
    [Name("IdentityBrace28")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat28 : BraceFormatBase
    {
        public BraceFormat28() : base(28)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace29")]
    [Name("IdentityBrace29")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat29 : BraceFormatBase
    {
        public BraceFormat29() : base(29)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace30")]
    [Name("IdentityBrace30")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat30 : BraceFormatBase
    {
        public BraceFormat30() : base(30)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "IdentityBrace31")]
    [Name("IdentityBrace31")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class BraceFormat31 : BraceFormatBase
    {
        public BraceFormat31() : base(31)
        {
        }
    }
}
