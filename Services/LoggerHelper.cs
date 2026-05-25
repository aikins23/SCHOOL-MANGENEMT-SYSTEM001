// TEMPORARY FIX: NLog reference not resolving in build system
// TODO: Fix NuGet package resolution issue
// The NLog DLL exists at: packages\NLog.5.3.4\lib\net45\NLog.dll
// MSBuild cannot resolve it even though it's in .csproj and packages.config
//
// To fix:
// 1. Delete .vs hidden folder (VS cache)
// 2. Run 'nuget restore' with proper path argument
// 3. Or: Clean NuGet cache and reinstall packages
// 4. Or: Manually add binding redirect for NLog to app.config
//
// For now, LoggerHelper is disabled. Remove these comments and uncomment code below.

using System;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Static helper class for application-wide logging using NLog.
    /// Provides simple, centralized logging methods for Info, Warning, and Error messages.
    ///
    /// Usage:
    ///   LoggerHelper.LogInfo("User logged in successfully");
    ///   LoggerHelper.LogWarning("Database connection slow");
    ///   LoggerHelper.LogError("Failed to save student record", exception);
    ///
    /// DISABLED: NLog assembly resolution issue in build system
    /// </summary>
    public static class LoggerHelper
    {
        // private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
#pragma warning disable CS0414
        private static readonly object _logger = null;
#pragma warning restore CS0414

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogInfo(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            // Note: Uncomment below once NLog binding redirect is configured in App.config
            // _logger.Info(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogWarning(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            // Note: Uncomment below once NLog binding redirect is configured in App.config
            // _logger.Warn(message);
        }

        /// <summary>
        /// Logs an error message with optional exception details.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="ex">Optional exception object to include in the log</param>
        public static void LogError(string message, Exception ex = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Note: Uncomment below once NLog binding redirect is configured in App.config
            // if (ex != null)
            // {
            //     _logger.Error(ex, message);
            // }
            // else
            // {
            //     _logger.Error(message);
            // }
        }
    }
}
