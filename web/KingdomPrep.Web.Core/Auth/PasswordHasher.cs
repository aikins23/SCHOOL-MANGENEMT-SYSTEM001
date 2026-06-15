using System.Security.Cryptography;

namespace KingdomPrep.Web.Core.Auth;

/// <summary>
/// Verifies/creates passwords compatible with the desktop AuthService:
/// current hashes use PBKDF2/HMAC-SHA256 and are stored as
/// "P3$&lt;iterations&gt;$&lt;base64Salt&gt;$&lt;base64Hash&gt;".
/// P2 HMAC-SHA1 hashes and legacy plaintext are still accepted for migration.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 150000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string LegacyPrefix = "P2";
    private const string CurrentPrefix = "P3";

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Derive(password, salt, Iterations, HashSize, HashAlgorithmName.SHA256);
        return $"{CurrentPrefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        if (!stored.StartsWith(CurrentPrefix + "$", StringComparison.Ordinal)
            && !stored.StartsWith(LegacyPrefix + "$", StringComparison.Ordinal))
            return stored == password; // legacy plaintext

        string[] parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations)) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException) { return false; }

        var algorithm = parts[0] == CurrentPrefix ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA1;
        byte[] actual = Derive(password, salt, iterations, expected.Length, algorithm);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length, HashAlgorithmName algorithm)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, length);
    }
}
