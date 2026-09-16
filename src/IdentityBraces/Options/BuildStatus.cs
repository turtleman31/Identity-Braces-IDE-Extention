using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IdentityBraces.Options
{
    internal enum BuildResult
    {
        /// <summary>No build has finished since Visual Studio started.</summary>
        Unknown = 0,

        Building = 1,
        Succeeded = 2,
        Failed = 3,
    }

    /// <summary>
    /// How the last build went, for the traits that react to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Advised lazily from a text view rather than from package initialisation, because this
    /// extension deliberately has no <c>ProvideAutoLoad</c> — it must not add anything to
    /// Visual Studio's start-up time. A view being created is already on the UI thread and
    /// already means somebody is looking at code, which is the only time any of this matters.
    /// </para>
    /// <para>
    /// Everything here degrades to <see cref="BuildResult.Unknown"/> if the shell service is
    /// unavailable, and <c>buildreactive</c> then simply draws nothing. A brace failing to
    /// celebrate is not worth an exception out of an editor.
    /// </para>
    /// </remarks>
    internal static class BuildStatus
    {
        /// <summary>
        /// How long a brace goes on reacting to a finished build.
        /// </summary>
        /// <remarks>
        /// Bounded so the reaction is to the build rather than a permanent change of costume,
        /// and so a brace scrolled into view an hour later is not still celebrating. It also
        /// means nothing needs to invalidate the adornments a second time when the window
        /// closes — a brace drawn after it has passed simply draws nothing.
        /// </remarks>
        public static readonly TimeSpan ReactionWindow = TimeSpan.FromSeconds(25);

        private static readonly object Gate = new object();
        private static Listener _listener;
        private static BuildResult _result;
        private static DateTime _changedUtc = DateTime.MinValue;

        /// <summary>Raised when a build starts or finishes, so views can redraw.</summary>
        public static event Action Changed;

        public static BuildResult Result
        {
            get
            {
                lock (Gate)
                {
                    return _result;
                }
            }
        }

        /// <summary>
        /// The result, but only while it is still fresh enough to react to.
        /// </summary>
        public static BuildResult Recent
        {
            get
            {
                lock (Gate)
                {
                    if (_result == BuildResult.Unknown || _result == BuildResult.Building)
                    {
                        return _result;
                    }

                    return DateTime.UtcNow - _changedUtc <= ReactionWindow
                        ? _result
                        : BuildResult.Unknown;
                }
            }
        }

        /// <summary>
        /// Starts listening, once per session. Must be called on the UI thread.
        /// </summary>
        public static void EnsureListening()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            lock (Gate)
            {
                if (_listener != null)
                {
                    return;
                }

                try
                {
                    var manager = Package.GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
                    if (manager == null)
                    {
                        return;
                    }

                    var listener = new Listener();

                    uint cookie;
                    if (ErrorHandler.Succeeded(manager.AdviseUpdateSolutionEvents(listener, out cookie)))
                    {
                        listener.Manager = manager;
                        listener.Cookie = cookie;
                        _listener = listener;
                    }
                }
                catch (Exception)
                {
                    // No build events. buildreactive draws nothing, everything else is fine.
                }
            }
        }

        private static void Record(BuildResult result)
        {
            lock (Gate)
            {
                _result = result;
                _changedUtc = DateTime.UtcNow;
            }

            Action handler = Changed;
            if (handler != null)
            {
                handler();
            }
        }

        /// <remarks>
        /// Never unadvised. The listener lives for the session, which is the same lifetime as
        /// the state it maintains, and there is nothing to release early — unadvising on the
        /// last view closing would mean missing the build that view was opened to fix.
        /// </remarks>
        private sealed class Listener : IVsUpdateSolutionEvents
        {
            public IVsSolutionBuildManager Manager;
            public uint Cookie;

            public int UpdateSolution_Begin(ref int pfCancelUpdate)
            {
                Record(BuildResult.Building);
                return VSConstants.S_OK;
            }

            public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
            {
                Record(fSucceeded != 0 && fCancelCommand == 0 ? BuildResult.Succeeded : BuildResult.Failed);
                return VSConstants.S_OK;
            }

            public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
            {
                return VSConstants.S_OK;
            }

            public int UpdateSolution_Cancel()
            {
                Record(BuildResult.Failed);
                return VSConstants.S_OK;
            }

            public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
