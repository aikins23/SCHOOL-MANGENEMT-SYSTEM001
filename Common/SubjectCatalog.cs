using System;
using System.Collections.Generic;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Single source of truth for per-class subjects, read synchronously by the exam-entry screen.
    /// Loads once from the database and caches; any DB error / unconfigured class falls back to the
    /// legacy 9 subjects so exam entry never breaks. Call Refresh() after a save.
    /// </summary>
    public static class SubjectCatalog
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, List<string>> _byClass;

        public static string[] LegacySubjects => SubjectRepository.LegacySubjects;

        private static void EnsureLoaded()
        {
            if (_byClass != null) return;
            lock (_lock)
            {
                if (_byClass != null) return;
                try
                {
                    var repo = new SubjectRepository(AppConfig.ConnectionString);
                    repo.EnsureTableAsyncSafe();
                    _byClass = repo.GetAllAsync().GetAwaiter().GetResult()
                               ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("SubjectCatalog load failed; using legacy subjects", ex);
                    _byClass = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock) { _byClass = null; }
        }

        /// <summary>The class's ordered subjects; falls back to the legacy 9 when unconfigured.</summary>
        public static IReadOnlyList<string> SubjectsForClass(string className)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(className) &&
                _byClass.TryGetValue(className.Trim(), out var list) && list.Count > 0)
                return list;
            return LegacySubjects;
        }
    }
}
