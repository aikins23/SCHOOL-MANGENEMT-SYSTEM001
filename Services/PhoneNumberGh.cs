using System.Linq;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Normalizes Ghana mobile numbers to international "233XXXXXXXXX" (12 digits)
    /// for SMS gateways. Returns null when the input is not a plausible GH mobile.
    /// </summary>
    public static class PhoneNumberGh
    {
        public static string NormalizeGh(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Keep digits only (drops +, spaces, dashes, parens).
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return null;

            // 0XXXXXXXXX (10) -> 233XXXXXXXXX
            if (digits.Length == 10 && digits[0] == '0')
                return "233" + digits.Substring(1);

            // 233XXXXXXXXX (12)
            if (digits.Length == 12 && digits.StartsWith("233"))
                return digits;

            // 9-digit local without leading zero (e.g. 24XXXXXXX) -> 233XXXXXXXXX
            if (digits.Length == 9)
                return "233" + digits;

            return null;
        }
    }
}
