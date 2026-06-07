using System.Security.Cryptography;
using System.Text;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>Stable content hash for SMS outbox de-duplication (no time component).</summary>
    public static class SmsOutboxKey
    {
        public static string Compute(string recipient, string senderId, string message)
        {
            string raw = (recipient ?? "") + "|" + (senderId ?? "") + "|" + (message ?? "");
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder(h.Length * 2);
                foreach (var b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
