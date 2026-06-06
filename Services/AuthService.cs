using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Authentication service with password hashing, validation, and Role-Based Access Control (RBAC)
    /// </summary>
    public static class AuthService
    {
        public enum UserRole { Director, Administrator, Headmaster, Teacher, Accountant, Parent, Unknown }

        public class UserSession
        {
            public string Username { get; set; }
            /// <summary>Employee full name for this account, resolved at login. May be empty
            /// (e.g. Director/Parent/unlinked accounts) — use <see cref="DisplayName"/> for UI.</summary>
            public string FullName { get; set; }
            /// <summary>Full name when available, otherwise the username. Used for receipts,
            /// the bursar/cashier field, and any place that displays "who did this".</summary>
            public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Username : FullName;
            public UserRole Role { get; set; }
            /// <summary>
            /// Employee record linked to this account (null for Director, Parent,
            /// or unlinked accounts). Required for Teachers — drives "own class
            /// only" filtering throughout the app.
            /// </summary>
            public int? EmploymentID { get; set; }
            public bool IsAuthenticated => Role != UserRole.Unknown;
        }

        public static UserSession CurrentUser { get; private set; } = new UserSession { Role = UserRole.Unknown };

        // --- RBAC permission table (mirrors PERMISSIONS.md). Roles listed here
        // may OPEN the form. Read-only (👁) is treated as "can open"; per-form
        // read-only enforcement is layered on top later.
        private static readonly Dictionary<string, UserRole[]> _formAccess = new Dictionary<string, UserRole[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Dashboard & analytics
            ["frmDashboard"]              = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Accountant },
            ["frmTeacherDashboard"]       = new[] { UserRole.Teacher },
            ["frmDashboardCharts"]        = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            // Students
            ["frmAddStd"]                 = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmStdView"]                = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["frmStdDetails"]             = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["frmStudentPromotion"]       = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            // Employees / HR
            ["frmEmployee"]               = new[] { UserRole.Director, UserRole.Administrator },
            ["frmEmpView"]                = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Accountant },
            ["frmEmpDetails"]             = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Accountant },
            // Leave
            ["frmEmpLeave"]               = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["frmLeaveApproval"]          = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmLeaveDetails"]           = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["EmpleaveView"]              = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["frmLeaveBalanceReport"]     = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            // Academics
            ["EXAMS"]                     = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["EXAMSVIEW"]                 = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["examsviewdetails"]          = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["frmAttendance"]             = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["frmClassAdmin"]             = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["GenerateReportCardsForm"]   = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            // Finance
            ["frmFess"]                   = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Accountant },
            ["frmFessPayment"]            = new[] { UserRole.Accountant },
            ["frmPendingApprovals"]       = new[] { UserRole.Accountant, UserRole.Director, UserRole.Administrator },
            ["frmOutstandingFees"]        = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            // System / Admin
            ["frmRegistration"]           = new[] { UserRole.Director, UserRole.Administrator },
            ["frmBackupManager"]          = new[] { UserRole.Director, UserRole.Administrator },
            ["frmEmailSettings"]          = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmSchoolInfo"]             = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmGradingScheme"]          = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmSubjects"]               = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmLibrary"]                = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmTransport"]              = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmPaymentHistory"]         = new[] { UserRole.Accountant, UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
        };

        /// <summary>
        /// True if the current logged-in user's role is allowed to open the given form.
        /// Used by sidebar / nav code to hide buttons the user can't open.
        /// Unknown form keys default to "allowed" — better to under-restrict than block
        /// legitimate screens because a key wasn't registered.
        /// </summary>
        public static bool CanAccess(string formKey)
        {
            // Deny by default; every protected screen must be registered in _formAccess.
            if (string.IsNullOrWhiteSpace(formKey)) return false;
            if (CurrentUser == null || CurrentUser.Role == UserRole.Unknown) return false;
            if (CurrentUser.Role == UserRole.Parent) return false; // parents never use desktop
            if (!_formAccess.TryGetValue(formKey, out var allowedRoles)) return false;
            return Array.IndexOf(allowedRoles, CurrentUser.Role) >= 0;
        }

        /// <summary>
        /// Call this as the first line of a form's Load handler. If the current
        /// user isn't allowed, shows a "Permission denied" dialog and closes the
        /// form. Returns false if denied (caller can use this to abort further setup).
        /// </summary>
        public static bool RequireAccess(string formKey, Form form)
        {
            if (CanAccess(formKey)) return true;
            string roleName = CurrentUser == null ? "Unknown" : CurrentUser.Role.ToString();
            UIHelper.ShowWarning(
                $"You do not have permission to open this screen.\n\nLogged in as: {roleName}\nScreen: {formKey}",
                "Access Denied");
            if (form != null)
            {
                // Close once the form is actually shown — by then the window
                // handle exists and we can safely close it. Calling Close /
                // BeginInvoke inside the constructor throws because the handle
                // hasn't been created yet.
                form.Shown += (s, e) => { try { form.Close(); } catch { } };
            }
            return false;
        }

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
            catch (Exception ex)
            {
                LoggerHelper.LogError("Password verification failed due to format error", ex);
                return false;
            }
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
                await EnsureDatabaseSetupAsync();
                using (var connection = new OleDbConnection(AppConfig.ConnectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT [Password], [User_Type], [EmploymentID] FROM Users WHERE Username = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add("?", OleDbType.VarChar).Value = username;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string storedPassword = reader["Password"].ToString();
                                string userType = reader["User_Type"].ToString();
                                int? employmentId = reader["EmploymentID"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(reader["EmploymentID"]);

                                if (!VerifyPassword(password, storedPassword))
                                {
                                    return (false, "Invalid username or password.");
                                }

                                CurrentUser = new UserSession
                                {
                                    Username = username,
                                    Role = ParseRole(userType),
                                    EmploymentID = employmentId
                                };

                                // Resolve the employee full name so the UI (bursar field,
                                // receipts) shows a real name instead of the login username.
                                CurrentUser.FullName = await ResolveEmployeeFullNameAsync(employmentId);

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
            catch (Exception ex) 
            {
                LoggerHelper.LogError($"Login failed for user {username}", ex);
                return (false, "Authentication error: " + ex.Message); 
            }
        }

        public static void Logout() { CurrentUser = new UserSession { Role = UserRole.Unknown }; }

        /// <summary>
        /// Looks up the Employee full name for a login's EmploymentID. Returns "" when there
        /// is no linked employee (Director/Parent/unlinked) or on any error — callers fall
        /// back to the username via <see cref="UserSession.DisplayName"/>.
        /// </summary>
        private static async System.Threading.Tasks.Task<string> ResolveEmployeeFullNameAsync(int? employmentId)
        {
            if (!employmentId.HasValue) return "";
            try
            {
                using (var connection = new OleDbConnection(AppConfig.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand("SELECT fullName FROM Employee WHERE employmentID = ?", connection))
                    {
                        command.Parameters.AddWithValue("?", employmentId.Value);
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? "" : result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Could not resolve employee full name for login: " + ex.Message);
                return "";
            }
        }

        /// <summary>True if the current logged-in user is a Teacher with an EmploymentID linked.</summary>
        public static bool IsTeacher =>
            CurrentUser != null
            && CurrentUser.Role == UserRole.Teacher
            && CurrentUser.EmploymentID.HasValue;

        /// <summary>
        /// Returns the assigned class of the currently logged-in Teacher (null for
        /// non-teachers or unassigned teachers). Used by data forms to apply the
        /// "own class only" filter described in PERMISSIONS.md.
        /// </summary>
        public static async System.Threading.Tasks.Task<string> GetCurrentTeacherClassAsync()
        {
            if (!IsTeacher) return null;
            try
            {
                var repo = new Data.ClassRepository(AppConfig.ConnectionString);
                var classes = await repo.GetClassesForTeacherAsync(CurrentUser.EmploymentID.Value);
                return System.Linq.Enumerable.FirstOrDefault(classes);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("GetCurrentTeacherClassAsync failed", ex);
                return null;
            }
        }

        private static UserRole ParseRole(string userType)
        {
            if (string.IsNullOrEmpty(userType)) return UserRole.Unknown;
            switch (userType.Trim().ToUpperInvariant())
            {
                case "DIRECTOR":
                case "DIRECTORS": return UserRole.Director;
                case "ADMIN":
                case "ADMINISTRATOR": return UserRole.Administrator;
                case "TEACHER": return UserRole.Teacher;
                case "ACCOUNTANT": return UserRole.Accountant;
                case "HEADMASTER": return UserRole.Headmaster;
                case "PARENT":
                case "GUARDIAN": return UserRole.Parent;
                default: return UserRole.Unknown;
            }
        }

        public static async System.Threading.Tasks.Task<(bool Success, string Message)> RegisterAsync(string username, string password, string confirmPassword, string userType, int? employmentId = null)
        {
            string validation = ValidateRegistration(username, password, confirmPassword, userType);
            if (!string.IsNullOrEmpty(validation)) return (false, validation);

            // Teachers must be linked to an Employee — the "own class only" filter
            // depends on this link.
            string upper = (userType ?? string.Empty).Trim().ToUpperInvariant();
            if (upper == "TEACHER" && !employmentId.HasValue)
            {
                return (false, "Teacher accounts must be linked to an employee record.");
            }

            try
            {
                await EnsureDatabaseSetupAsync();
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
                    var insertQuery = "INSERT INTO Users (Username, [Password], Con_Password, User_Type, EmploymentID) VALUES (?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(insertQuery, connection))
                    {
                        command.Parameters.Add("?", OleDbType.VarChar).Value = username;
                        command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                        command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                        command.Parameters.Add("?", OleDbType.VarChar).Value = userType;
                        command.Parameters.Add("?", OleDbType.Integer).Value = (object)employmentId ?? DBNull.Value;
                        await command.ExecuteNonQueryAsync();
                        return (true, "Registration successful.");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Registration failed for user {username}", ex);
                return (false, "Registration error: " + ex.Message);
            }
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
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to upgrade password hash for user {username}", ex);
            }
        }

        public static async System.Threading.Tasks.Task EnsureDatabaseSetupAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(AppConfig.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // 1. Ensure Users table exists
                    bool tableExists = false;
                    var schema = connection.GetSchema("Tables", new[] { null, null, "Users", "TABLE" });
                    if (schema.Rows.Count > 0) tableExists = true;

                    if (!tableExists)
                    {
                        // Use NVARCHAR(MAX) for SQL Server compatibility, or MEMO for Access
                        string textType = IsSqlServer(connection) ? "NVARCHAR(MAX)" : "MEMO";
                        string sql = $@"CREATE TABLE Users (
                            Username VARCHAR(50) NOT NULL PRIMARY KEY,
                            [Password] {textType} NOT NULL,
                            Con_Password {textType} NULL,
                            User_Type VARCHAR(50) NULL,
                            EmploymentID INT NULL
                        )";
                        Execute(connection, sql);
                    }
                    else
                    {
                        // 2. Ensure columns exist (for migration)
                        var columns = connection.GetSchema("Columns", new[] { null, null, "Users" });
                        bool hasPassword = false, hasConPassword = false, hasUserType = false, hasEmploymentId = false;

                        foreach (DataRow row in columns.Rows)
                        {
                            string col = row["COLUMN_NAME"].ToString().ToUpperInvariant();
                            if (col == "PASSWORD") hasPassword = true;
                            if (col == "CON_PASSWORD") hasConPassword = true;
                            if (col == "USER_TYPE") hasUserType = true;
                            if (col == "EMPLOYMENTID") hasEmploymentId = true;
                        }

                        string textType = IsSqlServer(connection) ? "NVARCHAR(MAX)" : "MEMO";
                        if (!hasPassword) Execute(connection, $"ALTER TABLE Users ADD COLUMN [Password] {textType}");
                        if (!hasConPassword) Execute(connection, $"ALTER TABLE Users ADD COLUMN Con_Password {textType}");
                        if (!hasUserType) Execute(connection, "ALTER TABLE Users ADD COLUMN User_Type VARCHAR(50)");
                        if (!hasEmploymentId) Execute(connection, "ALTER TABLE Users ADD COLUMN EmploymentID INT NULL");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Auth database setup error", ex);
            }
        }

        private static bool IsSqlServer(OleDbConnection connection)
        {
            string provider = connection.Provider.ToUpperInvariant();
            return provider.Contains("SQL") || provider.Contains("SQLNCLI") || provider.Contains("MSOLEDBSQL");
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

                string textType = IsSqlServer(connection) ? "NVARCHAR(MAX)" : "MEMO";
                if (!hasPassword) Execute(connection, $"ALTER TABLE Users ADD COLUMN [Password] {textType}");
                if (!hasConPassword) Execute(connection, $"ALTER TABLE Users ADD COLUMN Con_Password {textType}");
                if (!hasUserType) Execute(connection, "ALTER TABLE Users ADD COLUMN User_Type VARCHAR(50)");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error ensuring password columns in Users table", ex);
            }
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
