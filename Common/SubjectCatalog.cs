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

        public static event EventHandler<SubjectCatalogChangedEventArgs> Changed;

        public static string[] LegacySubjects => SubjectRepository.LegacySubjects;

        public static IReadOnlyList<string> StandardSubjectsForClass(string className)
        {
            var department = TimetableDepartments.GetDepartmentForClass(className);
            switch (department)
            {
                case TimetableDepartments.Preschool:
                case TimetableDepartments.Kindergarten:
                    return KindergartenSubjects;
                case TimetableDepartments.LowerPrimary:
                    return LowerPrimarySubjects;
                case TimetableDepartments.UpperPrimary:
                    return UpperPrimarySubjects;
                case TimetableDepartments.JuniorHighSchool:
                    return JuniorHighSubjects;
                default:
                    return LegacySubjects;
            }
        }

        public static bool IsLegacyDefaultList(IReadOnlyList<string> subjects)
        {
            if (subjects == null || subjects.Count != LegacySubjects.Length)
                return false;

            for (int i = 0; i < LegacySubjects.Length; i++)
            {
                if (!string.Equals(NormalizeSubject(subjects[i]), NormalizeSubject(LegacySubjects[i]), StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

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
                    System.Threading.Tasks.Task.Run(async () => {
                        _byClass = await repo.GetAllAsync()
                                   ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    }).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("SubjectCatalog load failed; using legacy subjects", ex);
                    _byClass = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public static void Refresh(string className = null)
        {
            lock (_lock) { _byClass = null; }
            Changed?.Invoke(null, new SubjectCatalogChangedEventArgs(className));
        }

        /// <summary>The class's ordered subjects; falls back to NaCCA-aligned department defaults when unconfigured.</summary>
        public static IReadOnlyList<string> SubjectsForClass(string className)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(className) &&
                _byClass.TryGetValue(className.Trim(), out var list) && list.Count > 0)
            {
                if (IsLegacyDefaultList(list))
                    return StandardSubjectsForClass(className);
                return list;
            }
            return StandardSubjectsForClass(className);
        }

        private static string NormalizeSubject(string value)
        {
            return (value ?? "").Trim().Replace(".", "").Replace("&", "AND").ToUpperInvariant();
        }

        private static readonly string[] KindergartenSubjects =
        {
            "LANGUAGE AND LITERACY",
            "NUMERACY",
            "CREATIVE ARTS",
            "OUR WORLD AND OUR PEOPLE"
        };

        private static readonly string[] LowerPrimarySubjects =
        {
            "ENGLISH LANGUAGE",
            "GHANAIAN LANGUAGE",
            "MATHEMATICS",
            "SCIENCE",
            "OUR WORLD AND OUR PEOPLE",
            "HISTORY",
            "RELIGIOUS AND MORAL EDUCATION",
            "CREATIVE ARTS",
            "PHYSICAL EDUCATION"
        };

        private static readonly string[] UpperPrimarySubjects =
        {
            "ENGLISH LANGUAGE",
            "GHANAIAN LANGUAGE",
            "MATHEMATICS",
            "SCIENCE",
            "OUR WORLD AND OUR PEOPLE",
            "HISTORY",
            "RELIGIOUS AND MORAL EDUCATION",
            "CREATIVE ARTS",
            "PHYSICAL EDUCATION",
            "FRENCH",
            "COMPUTING"
        };

        private static readonly string[] JuniorHighSubjects =
        {
            "ENGLISH LANGUAGE",
            "GHANAIAN LANGUAGE",
            "FRENCH",
            "ARABIC",
            "MATHEMATICS",
            "SCIENCE",
            "SOCIAL STUDIES",
            "RELIGIOUS AND MORAL EDUCATION",
            "CREATIVE ARTS AND DESIGN",
            "CAREER TECHNOLOGY",
            "COMPUTING",
            "PHYSICAL EDUCATION AND HEALTH"
        };
    }

    public sealed class SubjectCatalogChangedEventArgs : EventArgs
    {
        public SubjectCatalogChangedEventArgs(string className)
        {
            ClassName = string.IsNullOrWhiteSpace(className) ? null : className.Trim();
        }

        public string ClassName { get; }

        public bool AppliesTo(string className)
        {
            return string.IsNullOrWhiteSpace(ClassName) ||
                   string.Equals(ClassName, className?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
