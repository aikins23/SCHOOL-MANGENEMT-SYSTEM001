using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Per-class subject lists. Created and seeded (every class gets the legacy 9 subjects) on
    /// first use. SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class SubjectRepository : ISubjectRepository
    {
        private readonly string _connectionString;

        public SubjectRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string create = @"IF OBJECT_ID(N'ClassSubjects', N'U') IS NULL
                    CREATE TABLE ClassSubjects (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        ClassName NVARCHAR(50) NOT NULL,
                        Subject NVARCHAR(80) NOT NULL,
                        SortOrder INT NOT NULL DEFAULT (0));";
                using (var cmd = new OleDbCommand(create, c)) await cmd.ExecuteNonQueryAsync();

                // Seed each class that has no rows yet (idempotent + respects admin edits).
                foreach (var className in AppConfig.ClassNames)
                {
                    bool has;
                    using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM ClassSubjects WHERE ClassName = ?", c))
                    {
                        cmd.Parameters.AddWithValue("?", className);
                        has = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (has) continue;
                    for (int i = 0; i < LegacySubjects.Length; i++)
                    {
                        using (var cmd = new OleDbCommand(
                            "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", className);
                            cmd.Parameters.AddWithValue("?", LegacySubjects[i]);
                            cmd.Parameters.AddWithValue("?", i);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        /// <summary>EnsureTableAsync but swallows errors (used by the fail-safe accessor).</summary>
        public void EnsureTableAsyncSafe()
        {
            try { EnsureTableAsync().GetAwaiter().GetResult(); } catch { /* accessor falls back */ }
        }

        public async Task<List<string>> GetSubjectsForClassAsync(string className)
        {
            var list = new List<string>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT Subject FROM ClassSubjects WHERE ClassName = ? ORDER BY SortOrder", c))
                {
                    cmd.Parameters.AddWithValue("?", className ?? "");
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync())
                            list.Add(r["Subject"]?.ToString() ?? "");
                }
            }
            return list;
        }

        public async Task<Dictionary<string, List<string>>> GetAllAsync()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT ClassName, Subject FROM ClassSubjects ORDER BY ClassName, SortOrder", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                    {
                        string cls = r["ClassName"]?.ToString() ?? "";
                        if (!map.TryGetValue(cls, out var list)) { list = new List<string>(); map[cls] = list; }
                        list.Add(r["Subject"]?.ToString() ?? "");
                    }
            }
            return map;
        }

        public async Task SaveSubjectsForClassAsync(string className, IEnumerable<string> subjects)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var del = new OleDbCommand("DELETE FROM ClassSubjects WHERE ClassName = ?", c))
                {
                    del.Parameters.AddWithValue("?", className ?? "");
                    await del.ExecuteNonQueryAsync();
                }
                int order = 0;
                foreach (var subject in subjects)
                {
                    using (var ins = new OleDbCommand(
                        "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)", c))
                    {
                        ins.Parameters.AddWithValue("?", className ?? "");
                        ins.Parameters.AddWithValue("?", subject ?? "");
                        ins.Parameters.AddWithValue("?", order++);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>The legacy hardcoded subjects — used to seed and as the fail-safe default.</summary>
        public static readonly string[] LegacySubjects =
        {
            "MATHEMATICS", "INT. SCIENCE", "ENGLISH LANGUAGE", "SOCIAL STUDIES",
            "COMPUTING", "REL. & MORAL EDU.", "CARRER TECH.", "CREATIVE ART", "GHANAIAN LANG."
        };
    }
}
