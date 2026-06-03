using System;
using System.Security.Cryptography;
using System.Text;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Protects local user secrets with Windows DPAPI while preserving legacy plaintext reads.
    /// </summary>
    public static class SecretStorage
    {
        private const string Prefix = "dpapi:v1:";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("KingdomPrep.LocalSettings.Secrets.v1");

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            if (IsProtected(plainText)) return plainText;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(protectedBytes);
        }

        public static string Unprotect(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return string.Empty;
            if (!IsProtected(storedValue)) return storedValue;

            try
            {
                string payload = storedValue.Substring(Prefix.Length);
                byte[] protectedBytes = Convert.FromBase64String(payload);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool IsProtected(string value)
        {
            return value != null && value.StartsWith(Prefix, StringComparison.Ordinal);
        }
    }
}
