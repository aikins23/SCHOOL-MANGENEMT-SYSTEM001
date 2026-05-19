using System;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Authentication service with password hashing and validation
    /// Uses centralized ValidationHelper for input validation
    /// </summary>
    public static class AuthService
    {
        private const int Iterations = 100000;
        private const int SaltSize = 8;
        private const int HashSize = 16;
        private const string Prefix = "P2";

        public static string HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = DeriveHash(password, salt, Iterations);
            return $"{Prefix}${Iterations}${Encode(salt)}${Encode(hash)}";
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            if (!IsHashedPassword(storedPassword))
            {
                return string.Equals(password, storedPassword, StringComparison.Ordinal);
            }

            string[] parts = storedPassword.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations))
            {
                return false;
            }

            try
            {
                byte[] salt = Decode(parts[2]);
                byte[] expectedHash = Decode(parts[3]);
                byte[] actualHash = DeriveHash(password, salt, iterations, expectedHash.Length);
                return FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHashedPassword(string storedPassword)
        {
            return !string.IsNullOrWhiteSpace(storedPassword)
                && storedPassword.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }

        public static string ValidateRegistration(string username, string password, string confirmPassword, string userType)
        {
            // Check for empty values
            if (string.IsNullOrWhiteSpace(username))
                return "Username cannot be empty.";

            if (string.IsNullOrWhiteSpace(password))
                return "Password cannot be empty.";

            if (string.IsNullOrWhiteSpace(confirmPassword))
                return "Please confirm your password.";

            // Validate username format
            if (!ValidationHelper.IsValidUsername(username))
                return "Username must be 3-20 characters and contain only letters, numbers, and underscores.";

            // Validate password strength
            if (!ValidationHelper.IsStrongPassword(password))
                return "Password must be at least 8 characters with uppercase, lowercase, and digit.";

            // Validate passwords match
            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                return "Passwords do not match. Please re-enter.";

            // Validate user type
            if (string.IsNullOrWhiteSpace(userType) || userType.StartsWith("---", StringComparison.Ordinal))
                return "Please select a valid user type.";

            return string.Empty;
        }

        /// <summary>
        /// Validates login credentials
        /// </summary>
        public static string ValidateLoginCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "Username is required.";

            if (string.IsNullOrWhiteSpace(password))
                return "Password is required.";

            if (username.Length < 3)
                return "Invalid username format.";

            return string.Empty;
        }

        public static void EnsurePasswordColumns(OleDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            EnsurePasswordColumn(connection, "Password");
            EnsurePasswordColumn(connection, "Con_Password");
        }

        private static byte[] DeriveHash(string password, byte[] salt, int iterations, int hashSize = HashSize)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        private static string Encode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Decode(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }

            return Convert.FromBase64String(padded);
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        private static void EnsurePasswordColumn(OleDbConnection connection, string columnName)
        {
            int length = GetColumnLength(connection, columnName);
            if (length == -1 || length >= 128)
            {
                return;
            }

            using (var command = new OleDbCommand($"ALTER TABLE Users ALTER COLUMN [{columnName}] VARCHAR(128) NULL", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static int GetColumnLength(OleDbConnection connection, string columnName)
        {
            const string query = @"
SELECT CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = ?";

            using (var command = new OleDbCommand(query, connection))
            {
                command.Parameters.Add("?", OleDbType.VarChar).Value = columnName;
                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    throw new DataException($"Users.{columnName} column was not found.");
                }

                return Convert.ToInt32(result);
            }
        }
    }
}
