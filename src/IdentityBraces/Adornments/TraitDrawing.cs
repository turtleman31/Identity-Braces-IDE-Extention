using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IdentityBraces.Core;

namespace IdentityBraces.Adornments
{
    /// <summary>
    /// Maps a trait id to the code that draws it.
    /// </summary>
    /// <remarks>
    /// The other half of the registry. <see cref="TraitCatalog"/> owns what a trait is called
    /// and how often it appears; this owns what it looks like. Adding a creature is one row
    /// there and one entry here — no new enum member, no new settings field, no new branch in
    /// the factory.
    /// <para>
    /// A trait with no entry here simply does not draw, so the catalogue can list things ahead
    /// of their geometry without breaking anything.
    /// </para>
    /// </remarks>
    internal static class TraitDrawing
    {
        private static readonly Dictionary<string, Action<BraceDrawContext>> Painters =
            new Dictionary<string, Action<BraceDrawContext>>(StringComparer.Ordinal);

        static TraitDrawing()
        {
            Creatures.Register(Painters);
            Costumes.Register(Painters);
            Motions.Register(Painters);
            Effects.Register(Painters);
        }

        public static bool Has(string id)
        {
            return id != null && Painters.ContainsKey(id);
        }

        public static void Draw(string id, BraceDrawContext context)
        {
            if (id == null)
            {
                return;
            }

            Action<BraceDrawContext> painter;
            if (!Painters.TryGetValue(id, out painter))
            {
                return;
            }

            try
            {
                painter(context);
            }
            catch (Exception)
            {
                // One malformed trait must not cost the brace its glyph, nor take down the
                // layout pass that is drawing forty others.
            }
        }

        /// <summary>
        /// The text a body trait renders instead of the real character.
        /// </summary>
        /// <remarks>
        /// Body traits are a substitution rather than an overlay, so they resolve to a string
        /// here rather than to a painter. The buffer is untouched either way — a brace drawn
        /// as a question mark is still a brace to the compiler, the caret and Git.
        /// </remarks>
        public static string ResolveBody(string bodyId, char character)
        {
            switch (bodyId)
            {
                case TraitIds.Question:
                    return "?";

                case TraitIds.UnicodeVariant:
                    return UnicodeVariantFor(character);

                case TraitIds.WrongBracket:
                    return WrongBracketFor(character);

                case TraitIds.Emoji:
                    return EmojiFor(character);

                default:
                    return character.ToString();
            }
        }

        private static string UnicodeVariantFor(char character)
        {
            switch (character)
            {
                case '{': return "｛";
                case '}': return "｝";
                case '(': return "（";
                case ')': return "）";
                case '[': return "【";
                case ']': return "】";
                default: return character.ToString();
            }
        }

        /// <summary>Swaps the bracket family while keeping the direction. Deliberately hostile.</summary>
        private static string WrongBracketFor(char character)
        {
            switch (character)
            {
                case '{': return "(";
                case '}': return ")";
                case '(': return "[";
                case ')': return "]";
                case '[': return "{";
                case ']': return "}";
                default: return character.ToString();
            }
        }

        private static string EmojiFor(char character)
        {
            bool opening = character == '{' || character == '(' || character == '[';
            return opening ? "👉" : "👈";
        }

        /// <summary>True for bodies that need the glyph mirrored or rotated rather than replaced.</summary>
        public static void ApplyBodyTransform(string bodyId, BraceDrawContext context)
        {
            if (context.GlyphElement == null)
            {
                return;
            }

            switch (bodyId)
            {
                case TraitIds.Mirrored:
                    context.GlyphElement.RenderTransformOrigin = new Point(0.5, 0.5);
                    context.GlyphElement.RenderTransform = new ScaleTransform(-1, 1);
                    break;

                case TraitIds.UpsideDown:
                    context.GlyphElement.RenderTransformOrigin = new Point(0.5, 0.5);
                    context.GlyphElement.RenderTransform = new RotateTransform(180);
                    break;

                case TraitIds.Subscript:
                    context.GlyphElement.RenderTransformOrigin = new Point(0.5, 1.0);
                    context.GlyphElement.RenderTransform = new ScaleTransform(0.6, 0.6);
                    break;
            }
        }
    }
}
