using System;

namespace kingdom_Preparatory_School_Management_System.Common
{
    internal static class SyncRuntimeState
    {
        private static readonly object Gate = new object();

        public static TimeSpan DefaultInterval { get; } = TimeSpan.FromMinutes(2);
        public static TimeSpan InitialDelay { get; } = TimeSpan.FromSeconds(20);

        public static DateTime? LastAttemptAt { get; private set; }
        public static DateTime? LastStartedAt { get; private set; }
        public static DateTime? NextScheduledAt { get; private set; }
        public static string LastMessage { get; private set; } = "Sync has not run in this session.";
        public static bool LastAttemptSucceeded { get; private set; } = true;
        public static bool IsRunning { get; private set; }

        public static void RecordNextScheduled(DateTime? nextScheduledAt)
        {
            lock (Gate)
            {
                NextScheduledAt = nextScheduledAt;
            }
        }

        public static void RecordRunStarted()
        {
            lock (Gate)
            {
                LastStartedAt = DateTime.Now;
                IsRunning = true;
                LastMessage = "Sync is running...";
            }
        }

        public static void RecordSuccess(string message)
        {
            lock (Gate)
            {
                LastAttemptAt = DateTime.Now;
                LastMessage = string.IsNullOrWhiteSpace(message) ? "Sync completed." : message;
                LastAttemptSucceeded = true;
                IsRunning = false;
            }
        }

        public static void RecordFailure(string message)
        {
            lock (Gate)
            {
                LastAttemptAt = DateTime.Now;
                LastMessage = string.IsNullOrWhiteSpace(message) ? "Sync failed." : message;
                LastAttemptSucceeded = false;
                IsRunning = false;
            }
        }
    }
}
