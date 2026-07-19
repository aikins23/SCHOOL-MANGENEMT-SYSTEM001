using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.IO;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Application configuration and constants
    /// </summary>
    public static class AppConfig
    {
        private const string FirstRunTestDatabaseName = "Nyansapo_FirstRun_Test";

        public static string ConnectionString
        {
            get
            {
                string configured = Properties.Settings.Default.ConnectionString;
                string resolved = DatabaseConnectionSettings.Resolve(configured);
                string cs = IsFirstRunTestDatabase
                    ? WithDatabase(resolved, FirstRunTestDatabaseName)
                    : resolved;

                var builder = new DbConnectionStringBuilder { ConnectionString = cs };
                builder.Remove("Provider");
                if (!builder.ContainsKey("Connect Timeout") && !builder.ContainsKey("Connection Timeout"))
                {
                    builder["Connect Timeout"] = "8";
                }
                return builder.ConnectionString;
            }
        }

        public static bool IsFirstRunTestDatabase =>
            HasArg("--first-run-test-db") ||
            string.Equals(Environment.GetEnvironmentVariable("NYANSAPO_FIRST_RUN_TEST_DB"), "1", StringComparison.OrdinalIgnoreCase);

        public static bool ResetFirstRunTestDatabase =>
            HasArg("--reset-first-run-test-db") ||
            string.Equals(Environment.GetEnvironmentVariable("NYANSAPO_RESET_FIRST_RUN_TEST_DB"), "1", StringComparison.OrdinalIgnoreCase);

        public static string FirstRunTestDatabase => FirstRunTestDatabaseName;

        public static string MasterConnectionString => WithDatabase(ConnectionString, "master");

        private static bool HasArg(string arg)
        {
            foreach (var value in Environment.GetCommandLineArgs())
            {
                if (string.Equals(value, arg, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string WithDatabase(string connectionString, string databaseName)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString ?? "");
                if (builder.ContainsKey("Initial Catalog"))
                {
                    builder["Initial Catalog"] = databaseName;
                }
                else if (builder.ContainsKey("Database"))
                {
                    builder["Database"] = databaseName;
                }
                else
                {
                    builder["Initial Catalog"] = databaseName;
                }
                return builder.ConnectionString;
            }
            catch
            {
                string cs = connectionString ?? "";
                if (!cs.EndsWith(";")) cs += ";";
                return cs + "Initial Catalog=" + databaseName;
            }
        }

        /// <summary>
        /// The product (software) brand — shown in app chrome only (window titles, login, splash).
        /// Customer/parent-facing text (SMS, email, report cards, receipts) should use the buyer's
        /// configured school name via <see cref="SchoolProfile.DisplayName"/>, NOT this.
        /// </summary>
        public const string ProductName = "Nyansapo School ERP";

        // Class names
        public static readonly string[] ClassNames = new[]
        {
            "CRECHE",
            "NURSERY 1",
            "NURSERY 2",
            "KINDERGARTEN 1",
            "KINDERGARTEN 2",
            "BASIC 1",
            "BASIC 2",
            "BASIC 3",
            "BASIC 4",
            "BASIC 5",
            "BASIC 6",
            "BASIC 7",
            "BASIC 8",
            "BASIC 9"
        };

        // Gender options
        public static readonly string[] GenderOptions = new[] { "MALE", "FEMALE" };

        // File upload settings
        public const int MaxPhotoSizeMB = 5;
        public const long MaxPhotoSizeBytes = MaxPhotoSizeMB * 1024 * 1024;
        public static readonly string[] AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
        public static string PhotoUploadPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");

        public static class Sync
        {
            public static string EndpointBaseUrl
            {
                get
                {
                    var configured = Environment.GetEnvironmentVariable("NYANSAPO_SYNC_URL") ?? "";
                    if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().TrimEnd('/');
                    try
                    {
                        configured = Properties.Settings.Default.SyncEndpointBaseUrl ?? "";
                        if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().TrimEnd('/');
                    }
                    catch { }
                    return (SchoolProfile.PortalUrl ?? "").Trim().TrimEnd('/');
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.SyncEndpointBaseUrl = (value ?? "").Trim().TrimEnd('/');
                        Properties.Settings.Default.Save();
                    }
                    catch { }
                }
            }

            public static string ApiKey
            {
                get
                {
                    var configured = Environment.GetEnvironmentVariable("NYANSAPO_SYNC_KEY") ?? "";
                    if (!string.IsNullOrWhiteSpace(configured)) return configured;
                    try { return SecretStorage.Unprotect(Properties.Settings.Default.SyncApiKey ?? ""); }
                    catch { return ""; }
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.SyncApiKey = SecretStorage.Protect(value ?? "");
                        Properties.Settings.Default.Save();
                    }
                    catch { }
                }
            }

            public static bool IsConfigured =>
                !string.IsNullOrWhiteSpace(EndpointBaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
        }

        // Validation
        public const int MinStudentAge = 2;
        public const int MaxStudentAge = 25;

        // Email/SMTP Settings
        public static class Email
        {
            public static string SmtpServer
            {
                get
                {
                    try
                    {
                        return Properties.Settings.Default.SmtpServer ?? "smtp.gmail.com";
                    }
                    catch
                    {
                        return "smtp.gmail.com";
                    }
                }
            }

            public static int SmtpPort
            {
                get
                {
                    try
                    {
                        int port = Properties.Settings.Default.SmtpPort;
                        return port > 0 ? port : 587;
                    }
                    catch
                    {
                        return 587;
                    }
                }
            }

            public static string SmtpUsername
            {
                get
                {
                    try
                    {
                        return Properties.Settings.Default.SmtpUsername ?? "";
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            public static string SmtpPassword
            {
                get
                {
                    try
                    {
                        return SecretStorage.Unprotect(Properties.Settings.Default.SmtpPassword ?? "");
                    }
                    catch
                    {
                        return "";
                    }
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.SmtpPassword = SecretStorage.Protect(value ?? "");
                        Properties.Settings.Default.Save();
                    }
                    catch { }
                }
            }

            public static string FromEmail
            {
                get
                {
                    try
                    {
                        return Properties.Settings.Default.FromEmail ?? "noreply@nyansapoerp.edu.gh";
                        }
                        catch { return "noreply@nyansapoerp.edu.gh"; }
                        }
                        }

                        public static string FromName => SchoolProfile.DisplayName;

            public static bool UseSSL
            {
                get
                {
                    try
                    {
                        return Properties.Settings.Default.UseSSL;
                    }
                    catch
                    {
                        return true;
                    }
                }
            }

            public static bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword);
        }

        // SMS Settings
        public static class Sms
        {
            public static string Provider
            {
                get
                {
                    try { return Properties.Settings.Default.SmsProvider ?? "LogOnly"; }
                    catch { return "LogOnly"; }
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.SmsProvider = value;
                        Properties.Settings.Default.Save();
                    }
                    catch { }
                }
            }

            public static string ApiKey
            {
                get
                {
                    try { return SecretStorage.Unprotect(Properties.Settings.Default.SmsApiKey ?? ""); }
                    catch { return ""; }
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.SmsApiKey = SecretStorage.Protect(value ?? "");
                        Properties.Settings.Default.Save();
                    }
                    catch { }
                }
            }

            public static string FromNumber
            {
                get
                {
                    try { return Properties.Settings.Default.SmsFromNumber ?? "NYANSAPO"; }
                    catch { return "NYANSAPO"; }
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.SmsFromNumber = value;
                        Properties.Settings.Default.Save();
                    }
                    catch { }
                }
            }

            public static bool Enabled
            {
                get
                {
                    try { return Properties.Settings.Default.SmsEnabled; }
                    catch { return false; }
                }
                set
                {
                    try { Properties.Settings.Default.SmsEnabled = value; Properties.Settings.Default.Save(); }
                    catch { }
                }
            }

            public static string SchoolAbbreviation
            {
                get
                {
                    try
                    {
                        string v = Properties.Settings.Default.SmsSchoolAbbreviation;
                        return string.IsNullOrWhiteSpace(v) ? "NS" : v.Trim().ToUpperInvariant();
                    }
                    catch { return "NS"; }
                }
                set
                {
                    try { Properties.Settings.Default.SmsSchoolAbbreviation = value; Properties.Settings.Default.Save(); }
                    catch { }
                }
            }
        }

        // Notification recipients (HR leave alerts, etc.)
        public static class Notify
        {
            public static string HrEmail
            {
                get { try { return Properties.Settings.Default.HrNotifyEmail ?? ""; } catch { return ""; } }
                set { try { Properties.Settings.Default.HrNotifyEmail = value; Properties.Settings.Default.Save(); } catch { } }
            }

            public static string HrPhone
            {
                get { try { return Properties.Settings.Default.HrNotifyPhone ?? ""; } catch { return ""; } }
                set { try { Properties.Settings.Default.HrNotifyPhone = value; Properties.Settings.Default.Save(); } catch { } }
            }
        }

        // Leave Management
        public static class Leave
        {
            public static int DaysPerTerm
            {
                get
                {
                    try
                    {
                        int v = Properties.Settings.Default.LeaveDaysPerTerm;
                        return v > 0 ? v : 7;
                    }
                    catch { return 7; }
                }
                set
                {
                    try
                    {
                        Properties.Settings.Default.LeaveDaysPerTerm = value;
                        Properties.Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        Services.LoggerHelper.LogError("Failed to save LeaveDaysPerTerm setting", ex);
                    }
                }
            }

            /// <summary>
            /// Resolves the school term containing the given date.
            /// Ghana school calendar: T1 = Sep-Dec, T2 = Jan-Apr, T3 = May-Aug.
            /// </summary>
            public static (string TermName, DateTime Start, DateTime End) GetTerm(DateTime forDate)
            {
                int year = forDate.Year;
                int m = forDate.Month;

                if (m >= 9)
                    return ($"Term 1 {year}/{year + 1}",
                            new DateTime(year, 9, 1),
                            new DateTime(year, 12, 31));

                if (m <= 4)
                    return ($"Term 2 {year - 1}/{year}",
                            new DateTime(year, 1, 1),
                            new DateTime(year, 4, 30));

                return ($"Term 3 {year - 1}/{year}",
                        new DateTime(year, 5, 1),
                        new DateTime(year, 8, 31));
            }

            public static (string TermName, DateTime Start, DateTime End) CurrentTerm =>
                GetTerm(DateTime.Today);
        }

        // UI Colors
        public static class Colors
        {
            public static System.Drawing.Color PageBackColor => System.Drawing.Color.FromArgb(246, 248, 251);
            public static System.Drawing.Color SurfaceColor => System.Drawing.Color.White;
            public static System.Drawing.Color PrimaryColor => System.Drawing.Color.FromArgb(17, 20, 106);
            public static System.Drawing.Color AccentColor => System.Drawing.Color.FromArgb(212, 175, 55);
            public static System.Drawing.Color DangerColor => System.Drawing.Color.FromArgb(225, 29, 72);
            public static System.Drawing.Color SuccessColor => System.Drawing.Color.FromArgb(16, 185, 129);
            public static System.Drawing.Color WarningColor => System.Drawing.Color.FromArgb(255, 244, 194);
            public static System.Drawing.Color TextColor => System.Drawing.Color.FromArgb(17, 24, 39);
            public static System.Drawing.Color MutedTextColor => System.Drawing.Color.FromArgb(100, 116, 139);
            public static System.Drawing.Color BorderColor => System.Drawing.Color.FromArgb(226, 232, 240);
        }
    }
}
