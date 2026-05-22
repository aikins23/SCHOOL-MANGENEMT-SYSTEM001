using System;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Authentication service with password hashing, validation, and Role-Based Access Control (RBAC)
    /// </summary>
    public static class AuthService
    {
        public enum UserRole { Administrator, Teacher, Accountant, Headmaster, Unknown }

        public class UserSession
        {
            public string Username { get; set; }
            public UserRole Role { get; set; }
            public bool IsAuthenticated => Role != UserRole.Unknown;
        }

        public static UserSession CurrentUser { get; private set; } = new UserSession { Role = UserRole.Unknown };

        private const int Iterations = 100000;
        private const int SaltSize = 8;
        private const int HashSize = 16;
        private const string Prefix = "P2";

        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }
            byte[] hash = DeriveHash(password, salt, Iterations);
            return $"{Prefix}${Iterations}${Encode(salt)}${Encode(hash)}";
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword)) return false;
            if (!IsHashedPassword(storedPassword)) return string.Equals(password, storedPassword, StringComparison.Ordinal);
            string[] parts = storedPassword.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations)) return false;
            try
            {
                byte[] salt = Decode(parts[2]);
                byte[] expectedHash = Decode(parts[3]);
                byte[] actualHash = DeriveHash(password, salt, iterations, expectedHash.Length);
                return FixedTimeEquals(actualHash, expectedHash);
            }
            catch { return false; }
        }

        public static bool IsHashedPassword(string storedPassword)
        {
            return !string.IsNullOrWhiteSpace(storedPassword) && storedPassword.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }

        public static string ValidateRegistration(string username, string password, string confirmPassword, string userType)
        {
            if (string.IsNullOrWhiteSpace(username)) return "Username cannot be empty.";
            if (string.IsNullOrWhiteSpace(password)) return "Password cannot be empty.";
            if (string.IsNullOrWhiteSpace(confirmPassword)) return "Please confirm your password.";
            if (!ValidationHelper.IsValidUsername(username)) return "Username must be 3-20 characters and contain only letters, numbers, and underscores.";
            if (!ValidationHelper.IsStrongPassword(password)) return "Password must be at least 8 characters with uppercase, lowercase, and digit.";
            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal)) return "Passwords do not match. Please re-enter.";
            if (string.IsNullOrWhiteSpace(userType) || userType.StartsWith("---", StringComparison.Ordinal)) return "Please select a valid user type.";
            return string.Empty;
        }

        public static string ValidateLoginCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) return "Username is required.";
            if (string.IsNullOrWhiteSpace(password)) return "Password is required.";
            if (username.Length < 3) return "Invalid username format.";
            return string.Empty;
        }

        public static async System.Threading.Tasks.Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            string validation = ValidateLoginCredentials(username, password);
            if (!string.IsNullOrEmpty(validation)) return (false, validation);

            try
            {
                using (var connection = new OleDbConnection(AppConfig.ConnectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT [Password], [User_Type] FROM Users WHERE Username = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add("?", OleDbType.VarChar).Value = username;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string storedPassword = reader["Password"].ToString();
                                string userType = reader["User_Type"].ToString();

                                if (!VerifyPassword(password, storedPassword))
                                {
                                    return (false, "Invalid username or password.");
                                }

                                CurrentUser = new UserSession { Username = username, Role = ParseRole(userType) };

                                if (!IsHashedPassword(storedPassword))
                                {
                                    await TryUpgradePasswordHashAsync(connection, username, password);
                                }

                                return (true, "Login successful.");
                            }
                        }
                        return (false, "Invalid username or password.");
                    }
                }
            }
            catch (Exception ex) { return (false, "Authentication error: " + ex.Message); }
        }

        public static void Logout() { CurrentUser = new UserSession { Role = UserRole.Unknown }; }

        private static UserRole ParseRole(string userType)
        {
            if (string.IsNullOrEmpty(userType)) return UserRole.Unknown;
            switch (userType.Trim().ToUpperInvariant())
            {
                case "ADMIN":
                case "ADMINISTRATOR": return UserRole.Administrator;
                case "TEACHER": return UserRole.Teacher;
                case "ACCOUNTANT": return UserRole.Accountant;
                case "HEADMASTER": return UserRole.Headmaster;
                default: return UserRole.Unknown;
            }
        }

        public static async System.Threading.Tasks.Task<(bool Success, string Message)> RegisterAsync(string username, string password, string confirmPassword, string userType)
        {
            string validation = ValidateRegistration(username, password, confirmPassword, userType);
            if (!string.IsNullOrEmpty(validation)) return (false, validation);

            try
            {
                using (var connection = new OleDbConnection(AppConfig.ConnectionString))
                {
                    await connection.OpenAsync();
                    var checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = ?";
                    using (var checkCmd = new OleDbCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add("?", OleDbType.VarChar).Value = username;
                        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0) return (false, "Username already exists.");
                    }

                    string passwordHash = HashPassword(password);
                    var insertQuery = "INSERT INTO Users (Username, [Password], Con_Password, User_Type) VALUES (?, ?, ?, ?)";
                    using (var command = new OleDbCommand(insertQuery, connection))
                    {
                        command.Parameters.Add("?", OleDbType.VarChar).Value = username;
                        command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                        command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                        command.Parameters.Add("?", OleDbType.VarChar).Value = userType;
                        await command.ExecuteNonQueryAsync();
                        return (true, "Registration successful.");
                    }
                }
            }
            catch (Exception ex) { return (false, "Registration error: " + ex.Message); }
        }

        private static async System.Threading.Tasks.Task TryUpgradePasswordHashAsync(OleDbConnection connection, string username, string password)
        {
            try
            {
                string passwordHash = HashPassword(password);
                var query = "UPDATE Users SET [Password] = ?, Con_Password = ? WHERE Username = ?";
                using (var command = new OleDbCommand(query, connection))
                {
                    command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("?", OleDbType.VarChar).Value = username;
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch { }
        }

        public static void EnsurePasswordColumns(OleDbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                var table = connection.GetSchema("Columns", new[] { null, null, "Users" });
                bool hasPassword = false, hasConPassword = false, hasUserType = false;
                foreach (DataRow row in table.Rows)
                {
                    string col = row["COLUMN_NAME"].ToString().ToUpperInvariant();
                    if (col == "PASSWORD") hasPassword = true;
                    if (col == "CON_PASSWORD") hasConPassword = true;
                    if (col == "USER_TYPE") hasUserType = true;
                }
                if (!hasPassword) Execute(connection, "ALTER TABLE Users ADD COLUMN [Password] MEMO");
                if (!hasConPassword) Execute(connection, "ALTER TABLE Users ADD COLUMN Con_Password MEMO");
                if (!hasUserType) Execute(connection, "ALTER TABLE Users ADD COLUMN User_Type VARCHAR(50)");
            }
            catch { }
        }

        private static void Execute(OleDbConnection con, string sql) { using (var cmd = new OleDbCommand(sql, con)) cmd.ExecuteNonQuery(); }

        private static byte[] DeriveHash(string password, byte[] salt, int iterations, int length = HashSize)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations)) return pbkdf2.GetBytes(length);
        }

        private static string Encode(byte[] data) => Convert.ToBase64String(data);
        private static byte[] Decode(string data) => Convert.FromBase64String(data);
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var result = 0;
            for (var i = 0; i < a.Length; i++) result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
