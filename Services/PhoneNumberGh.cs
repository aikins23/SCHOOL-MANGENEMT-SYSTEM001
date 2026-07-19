using KingdomPrep.Shared.Models;
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

            string candidate = null;
            if (digits.Length == 10 && digits[0] == '0')
                candidate = "233" + digits.Substring(1);   // 0XXXXXXXXX
            else if (digits.Length == 12 && digits.StartsWith("233"))
                candidate = digits;                          // 233XXXXXXXXX
            else if (digits.Length == 9)
                candidate = "233" + digits;                  // XXXXXXXXX (no leading 0)

            // A valid GH mobile is 233 + a network digit (2 or 5) + 8 more digits.
            // This rejects 9/10-digit junk that would otherwise get a bogus 233 prefix.
            if (candidate != null && candidate.Length == 12 &&
                (candidate[3] == '2' || candidate[3] == '5'))
                return candidate;

            return null;
        }
    }
}
