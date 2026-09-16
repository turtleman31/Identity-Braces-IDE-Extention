using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Classification
{
    /// <summary>
    /// Declares the 32 brace classification types to MEF. Fields are assigned by the
    /// composition container, never by us, hence the suppressed "never assigned" warning.
    /// </summary>
    internal static class BraceClassificationTypes
    {
#pragma warning disable 649
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(BraceClassificationNames.Hidden)]
        internal static ClassificationTypeDefinition Hidden;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(BraceClassificationNames.Dim)]
        internal static ClassificationTypeDefinition Dim;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace00")]
        internal static ClassificationTypeDefinition Brace00;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace01")]
        internal static ClassificationTypeDefinition Brace01;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace02")]
        internal static ClassificationTypeDefinition Brace02;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace03")]
        internal static ClassificationTypeDefinition Brace03;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace04")]
        internal static ClassificationTypeDefinition Brace04;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace05")]
        internal static ClassificationTypeDefinition Brace05;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace06")]
        internal static ClassificationTypeDefinition Brace06;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace07")]
        internal static ClassificationTypeDefinition Brace07;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace08")]
        internal static ClassificationTypeDefinition Brace08;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace09")]
        internal static ClassificationTypeDefinition Brace09;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace10")]
        internal static ClassificationTypeDefinition Brace10;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace11")]
        internal static ClassificationTypeDefinition Brace11;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace12")]
        internal static ClassificationTypeDefinition Brace12;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace13")]
        internal static ClassificationTypeDefinition Brace13;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace14")]
        internal static ClassificationTypeDefinition Brace14;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace15")]
        internal static ClassificationTypeDefinition Brace15;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace16")]
        internal static ClassificationTypeDefinition Brace16;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace17")]
        internal static ClassificationTypeDefinition Brace17;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace18")]
        internal static ClassificationTypeDefinition Brace18;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace19")]
        internal static ClassificationTypeDefinition Brace19;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace20")]
        internal static ClassificationTypeDefinition Brace20;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace21")]
        internal static ClassificationTypeDefinition Brace21;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace22")]
        internal static ClassificationTypeDefinition Brace22;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace23")]
        internal static ClassificationTypeDefinition Brace23;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace24")]
        internal static ClassificationTypeDefinition Brace24;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace25")]
        internal static ClassificationTypeDefinition Brace25;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace26")]
        internal static ClassificationTypeDefinition Brace26;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace27")]
        internal static ClassificationTypeDefinition Brace27;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace28")]
        internal static ClassificationTypeDefinition Brace28;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace29")]
        internal static ClassificationTypeDefinition Brace29;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace30")]
        internal static ClassificationTypeDefinition Brace30;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("IdentityBrace31")]
        internal static ClassificationTypeDefinition Brace31;
#pragma warning restore 649
    }
}
