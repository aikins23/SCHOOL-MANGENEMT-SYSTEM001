using NLog;
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
    /// </summary>
    public static class LoggerHelper
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogInfo(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _logger.Info(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogWarning(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _logger.Warn(message);
        }

        /// <summary>
        /// Logs an error message with optional exception details.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="ex">Optional exception object to include in the log</param>
        public static void LogError(string message, Exception ex = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (ex != null)
            {
                _logger.Error(ex, message);
            }
            else
            {
                _logger.Error(message);
            }
        }
    }
}
