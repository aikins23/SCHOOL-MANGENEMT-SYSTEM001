using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Authentication service with password hashing, validation, and Role-Based Access Control (RBAC)
    /// </summary>
    public static class AuthService
    {
        private const int LoginCommandTimeoutSeconds = 12;

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
        // may OPEN the form. Read-only screens are treated as "can open"; per-form
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
            ["frmClassManager"]           = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmAcademicSessionManager"] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmAcademicCalendar"]       = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmTimetable"]              = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["GenerateReportCardsForm"]   = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            // Finance
            ["frmFess"]                   = new[] { UserRole.Director, UserRole.Administrator, UserRole.Accountant },
            ["frmFessPayment"]            = new[] { UserRole.Accountant },
            ["frmPendingApprovals"]       = new[] { UserRole.Accountant, UserRole.Director, UserRole.Administrator },
            ["frmPerformanceReports"]     = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["frmOutstandingFees"]        = new[] { UserRole.Director, UserRole.Administrator, UserRole.Teacher, UserRole.Accountant },
            ["frmScholarships"]           = new[] { UserRole.Director, UserRole.Administrator, UserRole.Accountant },
            ["frmAdditionalFees"]         = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Accountant },
            // System / Admin
            ["frmRegistration"]           = new[] { UserRole.Director, UserRole.Administrator },
            ["frmBackupManager"]          = new[] { UserRole.Director, UserRole.Administrator },
            ["frmEmailSettings"]          = new[] { UserRole.Director, UserRole.Administrator },
            ["frmNotice"]                 = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["frmSendNotice"]             = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["frmSchoolInfo"]             = new[] { UserRole.Director, UserRole.Administrator },
            ["frmGradingScheme"]          = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmSubjects"]               = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmExamSetup"]              = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["frmLibrary"]                = new[] { UserRole.Director, UserRole.Administrator },
            ["frmTransport"]              = new[] { UserRole.Director, UserRole.Administrator },
            ["frmSyncStatus"]             = new[] { UserRole.Director, UserRole.Administrator },
            ["frmSyncSettings"]           = new[] { UserRole.Director, UserRole.Administrator },
            ["frmAdminDashboard"]         = new[] { UserRole.Director, UserRole.Administrator },
            ["frmDatabaseCoverageAudit"]  = new[] { UserRole.Director, UserRole.Administrator },
            ["frmPaymentHistory"]         = new[] { UserRole.Accountant, UserRole.Director, UserRole.Administrator },
            ["frmTransportPayments"]      = new[] { UserRole.Accountant, UserRole.Administrator },
            ["frmExpenses"]               = new[] { UserRole.Accountant, UserRole.Administrator, UserRole.Director },
        };

        private static readonly Dictionary<string, UserRole[]> _writeAccess = new Dictionary<string, UserRole[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Finance.FeePayment.Record"]       = new[] { UserRole.Accountant },
            ["Finance.TransportPayment.Record"] = new[] { UserRole.Accountant },
            ["Finance.Expense.Manage"]          = new[] { UserRole.Director, UserRole.Accountant },
            ["Finance.Scholarship.Manage"]      = new[] { UserRole.Director, UserRole.Administrator, UserRole.Accountant },
            ["Finance.Scholarship.Approve"]     = new[] { UserRole.Director },
            ["Finance.AdditionalFees.View"]      = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Accountant },
            ["Finance.AdditionalFees.Create"]    = new[] { UserRole.Director, UserRole.Administrator, UserRole.Accountant },
            ["Finance.AdditionalFees.Approve"]   = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Settings.SchoolProfile.Manage"]    = new[] { UserRole.Director, UserRole.Administrator },
            ["Settings.GradingScheme.Manage"]    = new[] { UserRole.Director, UserRole.Administrator },
            ["Settings.Subjects.Manage"]         = new[] { UserRole.Director, UserRole.Administrator },
            ["Settings.ExamSetup.Manage"]        = new[] { UserRole.Director, UserRole.Administrator },
            ["Settings.Email.Manage"]            = new[] { UserRole.Director, UserRole.Administrator },
            ["Settings.Sync.Manage"]             = new[] { UserRole.Director, UserRole.Administrator },
            ["Students.Register"]                = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Students.Edit"]                    = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Students.Import"]                  = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Students.Promote"]                 = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Students.RollOut"]                 = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Students.TransportAssignment"]     = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Staff.Register"]                   = new[] { UserRole.Director, UserRole.Administrator },
            ["Staff.Edit"]                       = new[] { UserRole.Director, UserRole.Administrator },
            ["Staff.Delete"]                     = new[] { UserRole.Director, UserRole.Administrator },
            ["Staff.Terminate"]                  = new[] { UserRole.Director, UserRole.Administrator },
            ["Leave.Submit"]                     = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            ["Leave.Approve"]                    = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Academics.ExamResults.Manage"]     = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["Academics.Attendance.Record"]      = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            ["Academics.ClassStructure.Manage"]  = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Academics.Calendar.Manage"]        = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Academics.Session.Manage"]         = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Academics.Timetable.Manage"]       = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Admin.Users.Manage"]               = new[] { UserRole.Director, UserRole.Administrator },
            ["Admin.Archive.Restore"]            = new[] { UserRole.Director, UserRole.Administrator },
            ["Admin.Backup.Manage"]              = new[] { UserRole.Director, UserRole.Administrator },
            ["Admin.Notice.Send"]                = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            ["Admin.Sync.Run"]                   = new[] { UserRole.Director, UserRole.Administrator },
        };

        /// <summary>
        /// True if the current logged-in user's role is allowed to open the given form.
        /// Used by sidebar / nav code to hide buttons the user can't open.
        /// Unknown form keys are denied; protected screens must be registered in _formAccess.
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

        public static bool CanWrite(string actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey)) return false;
            if (CurrentUser == null || CurrentUser.Role == UserRole.Unknown) return false;
            if (CurrentUser.Role == UserRole.Parent) return false;
            if (!_writeAccess.TryGetValue(actionKey, out var allowedRoles)) return false;
            return Array.IndexOf(allowedRoles, CurrentUser.Role) >= 0;
        }

        public static bool CanRenderForm(string formKey) => CanAccess(formKey);

        public static bool CanRenderAction(string actionKey) => DynamicPermissionService.HasPermission(actionKey);

        public static bool RequireWriteAccess(string actionKey, string actionName = null)
        {
            if (CanWrite(actionKey)) return true;
            string roleName = CurrentUser == null ? "Unknown" : CurrentUser.Role.ToString();
            UIHelper.ShowWarning(
                $"You do not have permission to perform this action.\n\nLogged in as: {roleName}\nAction: {actionName ?? actionKey}",
                "Access Denied");
            return false;
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

        private const int Iterations = 150000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const string LegacyPrefix = "P2";
        private const string CurrentPrefix = "P3";

        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }
            byte[] hash = DeriveHash(password, salt, Iterations, HashSize, HashAlgorithmName.SHA256);
            return $"{CurrentPrefix}${Iterations}${Encode(salt)}${Encode(hash)}";
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword)) return false;
            if (!IsHashedPassword(storedPassword)) return string.Equals(password, storedPassword, StringComparison.Ordinal);
            string[] parts = storedPassword.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations)) return false;
            try
            {
                string prefix = parts[0];
                byte[] salt = Decode(parts[2]);
                byte[] expectedHash = Decode(parts[3]);
                var algorithm = string.Equals(prefix, CurrentPrefix, StringComparison.Ordinal)
                    ? HashAlgorithmName.SHA256
                    : HashAlgorithmName.SHA1;
                byte[] actualHash = DeriveHash(password, salt, iterations, expectedHash.Length, algorithm);
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
            return !string.IsNullOrWhiteSpace(storedPassword)
                && (storedPassword.StartsWith(CurrentPrefix + "$", StringComparison.Ordinal)
                    || storedPassword.StartsWith(LegacyPrefix + "$", StringComparison.Ordinal));
        }

        private static bool IsCurrentPasswordHash(string storedPassword)
        {
            return !string.IsNullOrWhiteSpace(storedPassword)
                && storedPassword.StartsWith(CurrentPrefix + "$", StringComparison.Ordinal);
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

        public static async System.Threading.Tasks.Task<(bool Success, string Message)> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string validation = ValidateLoginCredentials(username, password);
            if (!string.IsNullOrEmpty(validation)) return (false, validation);

            try
            {
                LoggerHelper.LogInfo($"Login stage: setup start for {username}");
                await EnsureDatabaseSetupAsync(cancellationToken);
                LoggerHelper.LogInfo($"Login stage: opening connection for {username}");
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await connection.OpenAsync(cancellationToken);
                    LoggerHelper.LogInfo($"Login stage: connection open for {username}");
                    var query = "SELECT [Password], [User_Type], [EmploymentID] FROM Users WHERE Username = @p0";
                    var tenant = await Data.TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    LoggerHelper.LogInfo($"Login stage: tenant check complete for {username}");
                    if (tenant)
                    {
                        query += Data.TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = LoginCommandTimeoutSeconds;
                        command.Parameters.Add("@p0", SqlDbType.VarChar).Value = username;
                        if (tenant)
                        {
                            Data.TenantContext.AddSchoolParameter(command);
                        }

                        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                        {
                            LoggerHelper.LogInfo($"Login stage: user query complete for {username}");
                            if (await reader.ReadAsync(cancellationToken))
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
                                LoggerHelper.LogInfo($"Login stage: password accepted for {username}");

                                // Resolve the employee full name so the UI (bursar field,
                                // receipts) shows a real name instead of the login username.
                                CurrentUser.FullName = await ResolveEmployeeFullNameAsync(employmentId, cancellationToken);
                                LoggerHelper.LogInfo($"Login stage: display name resolved for {username}");

                                if (!IsCurrentPasswordHash(storedPassword))
                                {
                                    await TryUpgradePasswordHashAsync(connection, username, password);
                                }

                                return (true, "Login successful.");
                            }
                        }
                        return (false, await BuildLoginFailureMessageAsync(username));
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                LoggerHelper.LogWarning($"Login timed out for user {username}: {ex.Message}");
                return (false, "The database took too long to respond. Please check SQL Server and try again.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Login failed for user {username}", ex);
                return (false, BuildAuthenticationUnavailableMessage(ex));
            }
        }

        private static string BuildAuthenticationUnavailableMessage(Exception ex)
        {
            string details = ex == null ? "" : ex.ToString();

            if (details.IndexOf("Cannot generate SSPI context", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("target principal name is incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "SQL Server Windows authentication failed on this computer. Use a SQL login connection string, or repair the local SQL Server Windows authentication/SPN setup.";
            }

            if (details.IndexOf("network-related or instance-specific", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("server was not found or was not accessible", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "SQL Server is not reachable. Confirm SQL Server is running and the connection string points to the correct server.";
            }

            if (details.IndexOf("login database could not be prepared", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "The login database could not be prepared. Check SQL Server permissions and database schema access.";
            }

            return "Authentication is temporarily unavailable. Please try again or contact the administrator.";
        }

        public static void Logout() { CurrentUser = new UserSession { Role = UserRole.Unknown }; }

        private static async System.Threading.Tasks.Task<string> BuildLoginFailureMessageAsync(string username = null)
        {
            try
            {
                var normalizedUsername = (username ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(normalizedUsername))
                {
                    var userStatus = await GetUserLoginStatusAsync(normalizedUsername);
                    if (userStatus.ExistsOutsideCurrentSchool)
                    {
                        return $"The account '{normalizedUsername}' exists, but it is not linked to the current school profile. Open School Information and run Repair Data, then try again.";
                    }

                    if (!userStatus.Exists)
                    {
                        var hint = userStatus.AdminUsernames.Count == 0
                            ? ""
                            : " Available administrator account(s): " + string.Join(", ", userStatus.AdminUsernames) + ".";
                        return $"No account was found for username '{normalizedUsername}'.{hint}";
                    }
                }

                var health = await new Data.SchoolInfoRepository(AppConfig.ConnectionString).GetIdentityHealthAsync();
                if (health.SetupAuditFound && health.UserCount == 0)
                {
                    return "No user accounts were found, but first-time setup is already locked. Restore a backup or contact the system administrator.";
                }

                if (health.TenantIssueRows > 0)
                {
                    return "This account could not be opened under the current School ID. Ask an administrator to run School Information > Repair Data.";
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Login health check skipped: " + ex.Message);
            }

            return "Invalid username or password.";
        }

        private sealed class UserLoginStatus
        {
            public bool Exists { get; set; }
            public bool ExistsOutsideCurrentSchool { get; set; }
            public List<string> AdminUsernames { get; } = new List<string>();
        }

        private static async System.Threading.Tasks.Task<UserLoginStatus> GetUserLoginStatusAsync(string username)
        {
            var status = new UserLoginStatus();
            using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await connection.OpenAsync();
                var tenant = await Data.TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                var schoolId = Data.TenantContext.CurrentSchoolId;

                using (var command = new SqlCommand("SELECT SchoolId FROM Users WHERE Username = @Username", connection))
                {
                    command.CommandTimeout = LoginCommandTimeoutSeconds;
                    command.Parameters.Add("@Username", SqlDbType.VarChar).Value = username;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (!tenant || schoolId == Guid.Empty)
                            {
                                status.Exists = true;
                                continue;
                            }

                            var rowSchoolId = reader["SchoolId"] == DBNull.Value
                                ? Guid.Empty
                                : (Guid)reader["SchoolId"];
                            if (rowSchoolId == schoolId)
                            {
                                status.Exists = true;
                            }
                            else
                            {
                                status.ExistsOutsideCurrentSchool = true;
                            }
                        }
                    }
                }

                using (var command = new SqlCommand(@"
SELECT TOP 5 Username
FROM Users
WHERE UPPER(ISNULL(User_Type, '')) IN ('ADMIN', 'ADMINISTRATOR', 'DIRECTOR', 'DIRECTORS')
ORDER BY Username", connection))
                {
                    command.CommandTimeout = LoginCommandTimeoutSeconds;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            status.AdminUsernames.Add(reader["Username"]?.ToString() ?? "");
                        }
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Looks up the Employee full name for a login's EmploymentID. Returns "" when there
        /// is no linked employee (Director/Parent/unlinked) or on any error — callers fall
        /// back to the username via <see cref="UserSession.DisplayName"/>.
        /// </summary>
        private static async System.Threading.Tasks.Task<string> ResolveEmployeeFullNameAsync(
            int? employmentId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!employmentId.HasValue) return "";
            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await connection.OpenAsync(cancellationToken);
                    var query = "SELECT fullName FROM Employee WHERE employmentID = @p0";
                    var tenant = await Data.TenantContext.HasSchoolIdColumnAsync(connection, "Employee");
                    if (tenant)
                    {
                        query += Data.TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = LoginCommandTimeoutSeconds;
                        command.Parameters.AddWithValue("@p0", employmentId.Value);
                        if (tenant)
                        {
                            Data.TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync(cancellationToken);
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await connection.OpenAsync();
                    var checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @p0";
                    var tenant = await Data.TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    if (tenant)
                    {
                        checkQuery += Data.TenantContext.FilterClauseSql();
                    }

                    using (var checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add("@p0", SqlDbType.VarChar).Value = username;
                        if (tenant)
                        {
                            Data.TenantContext.AddSchoolParameter(checkCmd);
                        }

                        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0) return (false, "Username already exists.");
                    }

                    string passwordHash = HashPassword(password);
                    var insertQuery = tenant
                        ? "INSERT INTO Users (Username, [Password], Con_Password, User_Type, EmploymentID, SchoolId) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)"
                        : "INSERT INTO Users (Username, [Password], Con_Password, User_Type, EmploymentID) VALUES (@p0, @p1, @p2, @p3, @p4)";
                    using (var command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.Add("@p0", SqlDbType.VarChar).Value = username;
                        command.Parameters.Add("@p1", SqlDbType.VarChar).Value = passwordHash;
                        command.Parameters.Add("@p2", SqlDbType.VarChar).Value = passwordHash;
                        command.Parameters.Add("@p3", SqlDbType.VarChar).Value = userType;
                        command.Parameters.Add("@p4", SqlDbType.Int).Value = (object)employmentId ?? DBNull.Value;
                        if (tenant)
                        {
                            command.Parameters.Add("@p5", SqlDbType.UniqueIdentifier).Value = Data.TenantContext.CurrentSchoolId;
                        }

                        await command.ExecuteNonQueryAsync();
                        await TryRecordUserSyncAsync(username, "Insert");
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

        private static async System.Threading.Tasks.Task TryRecordUserSyncAsync(string username, string operation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) return;
                await new Data.SyncChangeRecorder(AppConfig.ConnectionString).RecordUpsertAsync("Users", "Username", username, operation);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("User registration sync capture skipped: " + ex.Message);
            }
        }

        private static async System.Threading.Tasks.Task TryUpgradePasswordHashAsync(SqlConnection connection, string username, string password)
        {
            try
            {
                string passwordHash = HashPassword(password);
                var query = "UPDATE Users SET [Password] = @p0, Con_Password = @p1 WHERE Username = @p2";
                var tenant = await Data.TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                if (tenant)
                {
                    query += Data.TenantContext.FilterClauseSql();
                }

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@p0", SqlDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("@p1", SqlDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("@p2", SqlDbType.VarChar).Value = username;
                    if (tenant)
                    {
                        Data.TenantContext.AddSchoolParameter(command);
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to upgrade password hash for user {username}", ex);
            }
        }

        public static async System.Threading.Tasks.Task EnsureDatabaseSetupAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await connection.OpenAsync(cancellationToken);

                    bool tableExists = false;
                    using (var cmd = new SqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'", connection))
                    {
                        cmd.CommandTimeout = LoginCommandTimeoutSeconds;
                        var result = await cmd.ExecuteScalarAsync(cancellationToken);
                        tableExists = result != null;
                    }

                    if (!tableExists)
                    {
                        string sql = $@"CREATE TABLE Users (
                            Username VARCHAR(50) NOT NULL PRIMARY KEY,
                            [Password] NVARCHAR(MAX) NOT NULL,
                            Con_Password NVARCHAR(MAX) NULL,
                            User_Type VARCHAR(50) NULL,
                            EmploymentID INT NULL
                        )";
                        Execute(connection, sql);
                    }
                    else
                    {
                        // Check columns
                        var columns = new HashSet<string>();
                        using (var cmd = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users'", connection))
                        {
                            cmd.CommandTimeout = LoginCommandTimeoutSeconds;
                            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                            {
                                while (await reader.ReadAsync(cancellationToken))
                                {
                                    columns.Add(reader.GetString(0).ToUpperInvariant());
                                }
                            }
                        }

                        if (!columns.Contains("PASSWORD")) Execute(connection, $"ALTER TABLE Users ADD [Password] NVARCHAR(MAX)");
                        if (!columns.Contains("CON_PASSWORD")) Execute(connection, $"ALTER TABLE Users ADD Con_Password NVARCHAR(MAX)");
                        if (!columns.Contains("USER_TYPE")) Execute(connection, "ALTER TABLE Users ADD User_Type VARCHAR(50)");
                        if (!columns.Contains("EMPLOYMENTID")) Execute(connection, "ALTER TABLE Users ADD EmploymentID INT NULL");
                    }
                }

                await Data.TenantSchema.EnsureTenantColumnsAsync(AppConfig.ConnectionString);
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                LoggerHelper.LogError("Auth database setup error", ex);
                throw new InvalidOperationException("The login database could not be prepared. " + ex.Message, ex);
            }
        }

        public static void EnsurePasswordColumns(SqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();

                var columns = new HashSet<string>();
                using (var cmd = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users'", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(0).ToUpperInvariant());
                    }
                }

                if (!columns.Contains("PASSWORD")) Execute(connection, $"ALTER TABLE Users ADD [Password] NVARCHAR(MAX)");
                if (!columns.Contains("CON_PASSWORD")) Execute(connection, $"ALTER TABLE Users ADD Con_Password NVARCHAR(MAX)");
                if (!columns.Contains("USER_TYPE")) Execute(connection, "ALTER TABLE Users ADD User_Type VARCHAR(50)");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error ensuring password columns in Users table", ex);
            }
        }

        private static void Execute(SqlConnection con, string sql)
        {
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.CommandTimeout = LoginCommandTimeoutSeconds;
                cmd.ExecuteNonQuery();
            }
        }

        private static byte[] DeriveHash(string password, byte[] salt, int iterations, int length, HashAlgorithmName algorithm)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, algorithm)) return pbkdf2.GetBytes(length);
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
