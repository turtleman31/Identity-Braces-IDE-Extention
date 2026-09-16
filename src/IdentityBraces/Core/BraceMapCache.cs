using IdentityBraces.Options;
using Microsoft.VisualStudio.Text;

namespace IdentityBraces.Core
{
    /// <summary>
    /// Caches the brace map for the most recent snapshot of one buffer.
    /// </summary>
    /// <remarks>
    /// Both the tagger and the adornment manager derive their answers from this, so they
    /// stay in agreement without talking to each other: given the same snapshot and the
    /// same settings, <see cref="BraceScanner"/> is a pure function.
    /// <para>
    /// A snapshot is immutable, so reference equality is a sound cache key. Typing produces
    /// a new snapshot per keystroke and therefore a full rescan; that is a linear pass over
    /// the file, which is why <see cref="IdentityBracesSettings.MaxFileLength"/> exists.
    /// </para>
    /// </remarks>
    internal sealed class BraceMapCache
    {
        private readonly object _gate = new object();

        // Two entries, not one. The tagger, the adornment manager and the line transform
        // source all read this, and during an edit they can briefly be looking at different
        // snapshots. With a single slot those callers evict each other on every access and
        // the file is rescanned per call instead of per change — which in the layout pass
        // means a full rescan for every visible line.
        private ITextSnapshot _snapshot;
        private BraceMap _map = BraceMap.Empty;
        private ITextSnapshot _previousSnapshot;
        private BraceMap _previousMap = BraceMap.Empty;
        private int _settingsVersion = -1;

        public static BraceMapCache GetOrCreate(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                typeof(BraceMapCache),
                delegate { return new BraceMapCache(); });
        }

        public BraceMap Get(ITextSnapshot snapshot)
        {
            IdentityBracesSettings settings = IdentityBracesSettings.Current;

            lock (_gate)
            {
                if (_settingsVersion == settings.Version)
                {
                    if (ReferenceEquals(_snapshot, snapshot))
                    {
                        return _map;
                    }

                    if (ReferenceEquals(_previousSnapshot, snapshot))
                    {
                        return _previousMap;
                    }
                }
            }

            // Scan outside the lock. Two threads may briefly duplicate the work; that is
            // cheaper than serialising every reader behind a multi-millisecond scan.
            BraceMap map;
            if (!settings.Enabled || snapshot.Length > settings.MaxFileLength)
            {
                map = BraceMap.Empty;
            }
            else
            {
                map = new BraceMap(BraceScanner.Scan(snapshot.GetText(), settings.ToScanSettings()));
            }

            lock (_gate)
            {
                if (_settingsVersion != settings.Version)
                {
                    // Settings changed: every cached map is stale, not just the older one.
                    _previousSnapshot = null;
                    _previousMap = BraceMap.Empty;
                }
                else
                {
                    _previousSnapshot = _snapshot;
                    _previousMap = _map;
                }

                _snapshot = snapshot;
                _map = map;
                _settingsVersion = settings.Version;
            }

            return map;
        }
    }
}
