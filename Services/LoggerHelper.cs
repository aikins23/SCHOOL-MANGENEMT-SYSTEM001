using KingdomPrep.Shared.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Application-wide logging helper.
    /// Writes timestamped lines to a dated file in the application's logs\ folder
    /// and mirrors every entry to Debug output.  All methods are safe to call from
    /// any thread and will never throw — errors are silently swallowed so that a
    /// logging failure can never crash the application.
    /// </summary>
    public static class LoggerHelper
    {
        private static readonly string _logPath;
        private static readonly object _lock = new object();

        static LoggerHelper()
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                _logPath = Path.Combine(logDir, $"kps_{DateTime.Now:yyyyMMdd}.log");
            }
            catch
            {
                // If we can't create the logs folder, _logPath stays null.
                // All subsequent writes will fall through to Debug.WriteLine only.
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void LogInfo(string message)    => Write("INFO",  message, null);
        public static void LogWarning(string message) => Write("WARN",  message, null);

        public static void LogError(string message, Exception ex = null)
            => Write("ERROR", message, ex);

        // ── Internal ──────────────────────────────────────────────────────────

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                string body = message ?? "(null)";
                if (ex != null)
                    body += $" | {ex.GetType().Name}: {ex.Message}";

                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level,-5}] {body}";

                Debug.WriteLine(line);

                if (_logPath == null) return;

                lock (_lock)
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Never let a logging failure propagate.
            }
        }
    }
}
