using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using IdentityBraces.Adornments;
using IdentityBraces.Core;
using IdentityBraces.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace IdentityBraces.Classification
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class BraceTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                return null;
            }

            // A tagger per consumer rather than a buffer singleton: each one owns its event
            // subscriptions and can unhook them on Dispose without stranding another view.
            // The expensive part, the brace map, is shared through the buffer's property bag.
            return new BraceTagger(buffer, ClassificationRegistry) as ITagger<T>;
        }
    }

    /// <summary>
    /// Assigns each brace its palette entry, or marks it transparent when the adornment
    /// layer is going to draw it instead.
    /// </summary>
    internal sealed class BraceTagger : ITagger<ClassificationTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly BraceMapCache _cache;
        private readonly ClassificationTag[] _paletteTags;
        private readonly ClassificationTag _hiddenTag;
        private readonly ClassificationTag _dimTag;

        private bool _disposed;

        public BraceTagger(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _buffer = buffer;
            _cache = BraceMapCache.GetOrCreate(buffer);

            _paletteTags = new ClassificationTag[BracePalette.Count];
            if (registry != null)
            {
                for (int i = 0; i < _paletteTags.Length; i++)
                {
                    _paletteTags[i] = CreateTag(registry, BraceClassificationNames.Get(i));
                }

                _hiddenTag = CreateTag(registry, BraceClassificationNames.Hidden);
                _dimTag = CreateTag(registry, BraceClassificationNames.Dim);
            }

            _buffer.ChangedLowPriority += OnBufferChanged;
            IdentityBracesSettings.Changed += OnSettingsChanged;
            AdornedBuffers.Changed += OnAdornedBuffersChanged;
            CaretScopes.Changed += OnCaretScopeChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;
            if (spans == null || spans.Count == 0 || !settings.Enabled)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            BraceMap map = _cache.Get(snapshot);
            if (map.Count == 0)
            {
                yield break;
            }

            bool adorned = AdornedBuffers.IsAdorned(_buffer);

            // Resolved once per request rather than per brace: the scope is a property of the
            // buffer, and re-reading it mid-enumeration could see it change underneath and
            // leave half the screen dimmed against the other half.
            int scopeStart = 0;
            int scopeEnd = int.MaxValue;
            bool spotlight = false;

            if (settings.ScopeSpotlight && _dimTag != null)
            {
                int start;
                int end;
                if (CaretScopes.TryGet(_buffer, out start, out end))
                {
                    spotlight = true;
                    scopeStart = start;
                    scopeEnd = end;
                }
            }

            foreach (SnapshotSpan span in spans)
            {
                int end = span.End.Position;

                for (int i = map.FirstIndexAtOrAfter(span.Start.Position); i < map.Count; i++)
                {
                    BraceInfo brace = map[i];
                    if (brace.Position >= end)
                    {
                        break;
                    }

                    // Hide the real glyph only when something is actually going to redraw it.
                    ClassificationTag tag = brace.IsAdorned && adorned
                        ? _hiddenTag
                        : _paletteTags[brace.ColorIndex];

                    if (tag == null)
                    {
                        continue;
                    }

                    if (brace.Position + 1 > snapshot.Length)
                    {
                        break;
                    }

                    var braceSpan = new SnapshotSpan(snapshot, brace.Position, 1);
                    yield return new TagSpan<ClassificationTag>(braceSpan, tag);

                    // A second tag on the same character, carrying opacity and nothing else.
                    // The format map merges the two, so the brace keeps its palette colour and
                    // simply recedes. Braces the adornment layer draws are skipped: their
                    // classification is already transparent, and the layer dims their glyph
                    // itself.
                    if (spotlight
                        && !ReferenceEquals(tag, _hiddenTag)
                        && (brace.Position < scopeStart || brace.Position > scopeEnd))
                    {
                        yield return new TagSpan<ClassificationTag>(braceSpan, _dimTag);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _buffer.ChangedLowPriority -= OnBufferChanged;
            IdentityBracesSettings.Changed -= OnSettingsChanged;
            AdornedBuffers.Changed -= OnAdornedBuffersChanged;
            CaretScopes.Changed -= OnCaretScopeChanged;
        }

        private static ClassificationTag CreateTag(IClassificationTypeRegistryService registry, string name)
        {
            IClassificationType type = registry.GetClassificationType(name);
            return type == null ? null : new ClassificationTag(type);
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.Changes.Count == 0)
            {
                return;
            }

            // Editing a declaring line re-identifies that pair, and changing nesting depth
            // re-identifies everything under it, so invalidate from the edited line to the
            // end of the buffer. The editor only re-queries what is actually on screen, so
            // a wide invalidation span is cheap.
            ITextSnapshot after = e.After;
            int start = after.GetLineFromPosition(e.Changes[0].NewPosition).Start.Position;
            RaiseTagsChanged(new SnapshotSpan(after, start, after.Length - start));
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            RaiseWholeBuffer();
        }

        private void OnAdornedBuffersChanged(ITextBuffer buffer)
        {
            if (buffer == _buffer)
            {
                RaiseWholeBuffer();
            }
        }

        /// <remarks>
        /// Invalidating the whole buffer on a caret move sounds expensive and is not: the
        /// editor only re-queries the spans it is actually displaying, so the real cost is one
        /// re-classification of the visible lines. <see cref="CaretScopes"/> only raises this
        /// when the enclosing pair genuinely changed, which for ordinary typing is rare.
        /// </remarks>
        private void OnCaretScopeChanged(ITextBuffer buffer)
        {
            if (buffer == _buffer)
            {
                RaiseWholeBuffer();
            }
        }

        private void RaiseWholeBuffer()
        {
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
        }

        private void RaiseTagsChanged(SnapshotSpan span)
        {
            EventHandler<SnapshotSpanEventArgs> handler = TagsChanged;
            if (handler != null && !_disposed)
            {
                handler(this, new SnapshotSpanEventArgs(span));
            }
        }
    }
}
