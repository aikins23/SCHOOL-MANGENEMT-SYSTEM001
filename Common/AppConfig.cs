using System;
using System.Collections.Generic;
using System.IO;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Application configuration and constants
    /// </summary>
    public static class AppConfig
    {
        public static string ConnectionString => Properties.Settings.Default.ConnectionString;

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
                        return Properties.Settings.Default.FromEmail ?? "noreply@kingdomprep.edu.gh";
                    }
                    catch
                    {
                        return "noreply@kingdomprep.edu.gh";
                    }
                }
            }

            public static string FromName => "Kingdom Preparatory School";

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
                    try { return Properties.Settings.Default.SmsFromNumber ?? "KPSchool"; }
                    catch { return "KPSchool"; }
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
                        return string.IsNullOrWhiteSpace(v) ? "KPS" : v.Trim().ToUpperInvariant();
                    }
                    catch { return "KPS"; }
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
            public static System.Drawing.Color PageBackColor => System.Drawing.Color.FromArgb(245, 247, 250);
            public static System.Drawing.Color SurfaceColor => System.Drawing.Color.White;
            public static System.Drawing.Color PrimaryColor => System.Drawing.Color.FromArgb(25, 25, 112);
            public static System.Drawing.Color AccentColor => System.Drawing.Color.FromArgb(255, 215, 0);
            public static System.Drawing.Color DangerColor => System.Drawing.Color.FromArgb(190, 18, 60);
            public static System.Drawing.Color SuccessColor => System.Drawing.Color.FromArgb(76, 175, 80);
            public static System.Drawing.Color WarningColor => System.Drawing.Color.FromArgb(255, 193, 7);
            public static System.Drawing.Color TextColor => System.Drawing.Color.FromArgb(25, 36, 49);
            public static System.Drawing.Color MutedTextColor => System.Drawing.Color.FromArgb(93, 108, 123);
            public static System.Drawing.Color BorderColor => System.Drawing.Color.FromArgb(219, 226, 236);
        }
    }
}
