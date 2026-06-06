using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Formats the student ID for display as "{abbrev}{number}" (e.g. KPS9016) and parses
    /// that form back to the numeric string used for database queries. Display-only — the
    /// stored ID stays numeric everywhere. The abbreviation is the configurable school
    /// abbreviation (AppConfig.Sms.SchoolAbbreviation).
    /// </summary>
    public static class StudentId
    {
        public static string Abbrev => AppConfig.Sms.SchoolAbbreviation;

        /// <summary>"9016" -> "KPS9016". Null/blank -> "". Idempotent (won't double-prefix).</summary>
        public static string Display(object id)
        {
            if (id == null) return "";
            string s = id.ToString().Trim();
            if (s.Length == 0) return "";
            string ab = Abbrev;
            if (!string.IsNullOrEmpty(ab) && s.StartsWith(ab, StringComparison.OrdinalIgnoreCase))
                return s;
            return ab + s;
        }

        /// <summary>"KPS9016"/" kps9016 "/"9016" -> "9016". Strips a leading abbreviation.</summary>
        public static string Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string s = input.Trim();
            string ab = Abbrev;
            if (!string.IsNullOrEmpty(ab) && s.StartsWith(ab, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(ab.Length);
            return s.Trim();
        }

        /// <summary>
        /// Prefixes the named grid columns' DISPLAYED text via CellFormatting. The underlying
        /// cell value is untouched, so readbacks (row["ID"]) and numeric sort/filter still work.
        /// </summary>
        public static void AttachGridFormatting(DataGridView grid, params string[] idColumns)
        {
            if (grid == null || idColumns == null || idColumns.Length == 0) return;
            var cols = new HashSet<string>(idColumns, StringComparer.OrdinalIgnoreCase);
            grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.Value == null || e.Value == DBNull.Value) return;
                var col = grid.Columns[e.ColumnIndex];
                if (cols.Contains(col.Name) || cols.Contains(col.HeaderText))
                {
                    e.Value = Display(e.Value);
                    e.FormattingApplied = true;
                }
            };
        }
    }
}
