using System.Globalization;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>Bridges the legacy varchar Expenses.Amount column and decimal. Mirrors the
    /// dashboard SQL (REPLACE commas, TRY_CAST) so UI totals and chart totals agree.</summary>
    public static class ExpenseAmount
    {
        public static decimal Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            string cleaned = raw.Replace(",", "").Replace("GHS", "").Replace("₵", "").Trim();
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }

        public static string Store(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
