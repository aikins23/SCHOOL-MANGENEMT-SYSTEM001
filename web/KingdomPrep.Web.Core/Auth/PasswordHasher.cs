using System.Security.Cryptography;

namespace KingdomPrep.Web.Core.Auth;

/// <summary>
/// Verifies/creates passwords compatible with the desktop AuthService:
/// PBKDF2/HMAC-SHA1, 100000 iterations, 8-byte salt, 16-byte hash, stored as
/// "P2$&lt;iterations&gt;$&lt;base64Salt&gt;$&lt;base64Hash&gt;".
/// A stored value that does not start with "P2$" is treated as legacy plaintext
/// (the desktop upgrades those on next login).
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100000;
    private const int SaltSize = 8;
    private const int HashSize = 16;
    private const string Prefix = "P2";

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Derive(password, salt, Iterations, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        if (!stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
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

        byte[] actual = Derive(password, salt, iterations, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA1);
        return pbkdf2.GetBytes(length);
    }
}
