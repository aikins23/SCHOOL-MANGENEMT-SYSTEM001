using System;
using System.Collections.Generic;
using System.Drawing;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Single source of truth for school identity + fees, read synchronously by the
    /// (formerly hardcoded) call sites. Loads once from the database and caches; any DB
    /// error falls back to the model defaults so the app never breaks. Call Refresh()
    /// after saving settings.
    /// </summary>
    public static class SchoolProfile
    {
        private static readonly object _lock = new object();
        private static SchoolInformation _info;
        private static Dictionary<string, decimal> _fees;

        private static void EnsureLoaded()
        {
            if (_info != null && _fees != null) return;
            lock (_lock)
            {
                if (_info != null && _fees != null) return;
                try
                {
                    System.Threading.Tasks.Task.Run(async () => {
                        var repo = new SchoolInfoRepository(AppConfig.ConnectionString);
                        await repo.EnsureTablesAsync();
                        _info = await repo.GetAsync();
                        _fees = await repo.GetClassFeesAsync();
                    }).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("SchoolProfile load failed; using defaults", ex);
                    _info = _info ?? new SchoolInformation();
                    _fees = _fees ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock) { _info = null; _fees = null; }
        }

        private static SchoolInformation Info { get { EnsureLoaded(); return _info; } }

        public static Guid SchoolId => Info.SchoolId == Guid.Empty ? Guid.Empty : Info.SchoolId;
        public static string Name => Info.Name;
        /// <summary>The buyer's school name for all customer/parent-facing text (SMS, email,
        /// report cards, receipts). Falls back to the product name only when unconfigured.</summary>
        public static string DisplayName => string.IsNullOrWhiteSpace(Info.Name) ? AppConfig.ProductName : Info.Name;
        public static string Address => Info.Address;
        public static string PoBox => Info.PoBox;
        public static string GpsAddress => Info.GpsAddress;
        public static string Phone1 => Info.Phone1;
        public static string Phone2 => Info.Phone2;
        public static string Phones => Info.Phones;
        public static string Email => Info.Email;
        public static string PortalUrl => Info.PortalUrl;
        public static byte[] Logo => Info.Logo;
        public static decimal AdmissionFee => Info.AdmissionFee;

        /// <summary>
        /// Transient, in-memory colour override used by the settings Preview button so unsaved
        /// colours render without being persisted. Set it, generate, then clear it in a finally.
        /// </summary>
        public static (Color Primary, Color Accent, Color Secondary)? ReportColorOverride;

        public static Color ReportPrimaryColor =>
            ReportColorOverride?.Primary ?? Color.FromArgb(Info.PrimaryColorArgb);
        public static Color ReportAccentColor =>
            ReportColorOverride?.Accent ?? Color.FromArgb(Info.AccentColorArgb);
        public static Color ReportSecondaryColor =>
            ReportColorOverride?.Secondary ?? Color.FromArgb(Info.SecondaryColorArgb);

        public static decimal FeeForClass(string classId)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(classId) &&
                _fees.TryGetValue(classId.Trim(), out var fee))
                return fee;
            return SchoolInfoRepository.LegacyFeeForClass(classId); // fallback for unknown class
        }
    }
}
