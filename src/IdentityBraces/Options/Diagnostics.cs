using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace IdentityBraces.Options
{
    /// <summary>
    /// Appends a line per layout pass to <c>%APPDATA%\IdentityBraces\diagnostic.log</c>.
    /// </summary>
    /// <remarks>
    /// Temporary instrumentation for an adornment-positioning bug that could not be
    /// reproduced on the development machine. It answers the questions guesswork could not:
    /// whether the manager is constructed at all, whether layout events arrive, what the
    /// adornment layer thinks it is holding, and what coordinates are actually being written.
    /// <para>
    /// Capped and throttled so an enabled log cannot grow without bound or turn a scroll into
    /// disk thrash.
    /// </para>
    /// </remarks>
    internal static class Diagnostics
    {
        private const int MaxLines = 4000;

        private static readonly object Gate = new object();
        private static int _lines;
        private static bool _headerWritten;

        public static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IdentityBraces",
                    "diagnostic.log");
            }
        }

        public static bool Enabled
        {
            get { return IdentityBracesSettings.Current.DiagnosticLog; }
        }

        public static void Log(string message)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                lock (Gate)
                {
                    if (_lines >= MaxLines)
                    {
                        return;
                    }

                    string path = FilePath;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    var builder = new StringBuilder();

                    if (!_headerWritten)
                    {
                        _headerWritten = true;
                        builder.Append("=== Identity Braces diagnostic session ")
                               .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                               .Append(" pid=")
                               .Append(System.Diagnostics.Process.GetCurrentProcess().Id)
                               .AppendLine(" ===");
                        _lines++;
                    }

                    builder.Append(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture))
                           .Append(' ')
                           .Append("t")
                           .Append(Thread.CurrentThread.ManagedThreadId)
                           .Append(' ')
                           .AppendLine(message);

                    _lines++;

                    File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
                }
            }
            catch (IOException)
            {
                // Diagnostics must never be the reason the editor misbehaves.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static void Log(string format, params object[] args)
        {
            if (!Enabled)
            {
                return;
            }

            Log(string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}
