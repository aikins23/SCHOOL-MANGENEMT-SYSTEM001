using System;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Maps a route's payment term to the current billing period. Monthly = calendar month;
    /// Weekly = a 2-week (fortnight) block anchored to a fixed Monday; Daily = a single day.
    /// Pure and deterministic (no DB / no clock) so it is unit-testable.
    /// </summary>
    public static class TransportPeriod
    {
        private static readonly DateTime FortnightEpoch = new DateTime(2024, 1, 1); // a Monday

        private static bool Is(string term, string name) =>
            string.Equals((term ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase);

        /// <summary>Monthly and Weekly routes get auto-reminders; Daily routes do not.</summary>
        public static bool SupportsReminders(string term) => Is(term, "Monthly") || Is(term, "Weekly");

        /// <summary>(Key, Start, End) for the period containing <paramref name="today"/>.</summary>
        public static (string Key, DateTime Start, DateTime End) Current(string term, DateTime today)
        {
            today = today.Date;

            if (Is(term, "Monthly"))
            {
                var start = new DateTime(today.Year, today.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                return (today.ToString("yyyy-MM"), start, end);
            }

            if (Is(term, "Weekly"))
            {
                int days = (int)(today - FortnightEpoch).TotalDays;
                if (days < 0) days = 0;
                var start = FortnightEpoch.AddDays((days / 14) * 14);
                var end = start.AddDays(13);
                return (start.ToString("yyyy-MM-dd") + "/FN", start, end);
            }

            // Daily (and any unknown term) -> a single day.
            return (today.ToString("yyyy-MM-dd"), today, today);
        }
    }
}
