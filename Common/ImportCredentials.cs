using System;
using System.Security.Cryptography;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Credentials for guardian (PARENT) accounts created by the student CSV import.
    /// Username derives from the ward's display ID (e.g. kps9016 — the abbreviation prefix
    /// guarantees the 3-char minimum). Passwords are crypto-random and satisfy
    /// ValidationHelper.IsStrongPassword (8+ chars, upper, lower, digit).
    /// </summary>
    public static class ImportCredentials
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static string UsernameFor(string studentId) =>
            StudentId.Display(studentId).ToLowerInvariant();

        public static string NewPassword()
        {
            // "Pw" + 4 digits + 2 lowercase letters => 8 chars with upper, lower, digit.
            int n  = Next(0, 10000);
            char a = (char)('a' + Next(0, 26));
            char b = (char)('a' + Next(0, 26));
            return "Pw" + n.ToString("0000") + a + b;
        }

        private static int Next(int minInclusive, int maxExclusive)
        {
            var buf = new byte[4];
            lock (Rng) Rng.GetBytes(buf);
            uint v = BitConverter.ToUInt32(buf, 0);
            return (int)(minInclusive + (v % (uint)(maxExclusive - minInclusive)));
        }
    }
}
