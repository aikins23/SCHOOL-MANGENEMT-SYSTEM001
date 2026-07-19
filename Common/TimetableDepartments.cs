using System;
using System.Collections.Generic;
using System.Linq;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public static class TimetableDepartments
    {
        public const string Preschool = "Preschool";
        public const string Kindergarten = "Kindergarten";
        public const string LowerPrimary = "Lower Primary";
        public const string UpperPrimary = "Upper Primary";
        public const string JuniorHighSchool = "Junior High School";

        private static readonly Dictionary<string, string[]> DepartmentClasses =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [Preschool] = new[] { "CRECHE", "NURSERY", "NURSERY 1", "NURSERY 2" },
                [Kindergarten] = new[] { "KG1", "KG2", "K.G. 1", "K.G. 2", "KINDERGARTEN 1", "KINDERGARTEN 2" },
                [LowerPrimary] = new[] { "BASIC 1", "BASIC 2", "BASIC 3" },
                [UpperPrimary] = new[] { "BASIC 4", "BASIC 5", "BASIC 6" },
                [JuniorHighSchool] = new[] { "BASIC 7", "BASIC 8", "BASIC 9", "JHS 1", "JHS 2", "JHS 3" }
            };

        public static IReadOnlyList<string> Names { get; } = DepartmentClasses.Keys.ToList();

        public static string GetDepartmentForClass(string className)
        {
            var normalized = Normalize(className);
            foreach (var item in DepartmentClasses)
            {
                if (item.Value.Any(c => Normalize(c) == normalized))
                    return item.Key;
            }

            return "";
        }

        public static IReadOnlyList<string> GetClasses(string departmentName)
        {
            return DepartmentClasses.TryGetValue(departmentName ?? "", out var classes)
                ? classes
                : new string[0];
        }

        public static bool BelongsToDepartment(string className, string departmentName)
        {
            return GetClasses(departmentName).Any(c => Normalize(c) == Normalize(className));
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Trim().Replace(".", "").Replace("-", " ").ToUpperInvariant();
        }
    }
}
