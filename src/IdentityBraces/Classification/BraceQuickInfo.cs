using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using IdentityBraces.Core;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Classification
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Identity Braces")]
    [ContentType("code")]
    [Order(After = "Default Quick Info Presenter")]
    internal sealed class BraceQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer == null ? null : new BraceQuickInfoSource(textBuffer);
        }
    }

    /// <summary>
    /// Introduces a named brace when you hover over it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Through the editor's own quick-info rather than a WPF <c>ToolTip</c> on the adornment,
    /// and that is the whole design. Brace adornments are
    /// <c>IsHitTestVisible = false</c> for a reason: they sit above the text, so making them
    /// hit-testable would have them swallow the clicks that place the caret. A tooltip is not
    /// worth breaking clicking on your own code.
    /// </para>
    /// <para>
    /// It also means the name appears in the same surface as everything else the editor has to
    /// say about that character, positioned by the editor, dismissed by the editor, and themed
    /// like the rest of Visual Studio.
    /// </para>
    /// </remarks>
    internal sealed class BraceQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _buffer;
        private readonly BraceMapCache _cache;

        public BraceQuickInfoSource(ITextBuffer buffer)
        {
            _buffer = buffer;
            _cache = BraceMapCache.GetOrCreate(buffer);
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Describe(session));
        }

        private QuickInfoItem Describe(IAsyncQuickInfoSession session)
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;

            // The trait is off by default, and when it is off this must cost nothing: quick
            // info runs on every hover anywhere in the file.
            if (session == null
                || !settings.Enabled
                || settings.GetTraitWeight(TraitIds.Named) <= 0)
            {
                return null;
            }

            try
            {
                SnapshotPoint? trigger = session.GetTriggerPoint(_buffer.CurrentSnapshot);
                if (trigger == null)
                {
                    return null;
                }

                SnapshotPoint point = trigger.Value;
                BraceMap map = _cache.Get(point.Snapshot);
                if (map.Count == 0)
                {
                    return null;
                }

                int index = map.FirstIndexAtOrAfter(point.Position);
                if (index >= map.Count)
                {
                    return null;
                }

                BraceInfo brace = map[index];

                // Only the character actually under the pointer. FirstIndexAtOrAfter finds the
                // next brace, which for a hover in open space could be most of a line away.
                if (brace.Position != point.Position
                    || !SceneCasting.HasEffect(brace.Traits, TraitIds.Named))
                {
                    return null;
                }

                var span = _buffer.CurrentSnapshot.CreateTrackingSpan(
                    brace.Position,
                    1,
                    SpanTrackingMode.EdgeInclusive);

                return new QuickInfoItem(span, Introduce(brace));
            }
            catch (ArgumentException)
            {
                // A snapshot that moved between the trigger point and the map.
                return null;
            }
        }

        /// <remarks>
        /// Two lines: who this is, and what it is. The second is what stops it reading as an
        /// error message from a tool you did not know you had installed.
        /// </remarks>
        private static ContainerElement Introduce(BraceInfo brace)
        {
            return new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, BraceNames.Of(brace.Identity))),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Comment,
                        brace.IsMatched ? "an Identity Brace" : "an Identity Brace, unmatched")));
        }

        public void Dispose()
        {
        }
    }
}
