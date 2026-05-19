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
