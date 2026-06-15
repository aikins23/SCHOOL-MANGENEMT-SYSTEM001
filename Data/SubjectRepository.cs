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
        private const string SUBJECTS_TABLE = "ClassSubjects";

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
                await EnsureSchoolColumnAsync(c);
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);

                // Seed each class that has no rows yet (idempotent + respects admin edits).
                foreach (var className in AppConfig.ClassNames)
                {
                    bool has;
                    var checkSql = "SELECT COUNT(*) FROM ClassSubjects WHERE ClassName = ?";
                    if (tenant) checkSql += TenantContext.FilterClause();
                    using (var cmd = new OleDbCommand(checkSql, c))
                    {
                        cmd.Parameters.AddWithValue("?", className);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        has = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (has) continue;
                    for (int i = 0; i < LegacySubjects.Length; i++)
                    {
                        var insertSql = tenant
                            ? "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder, SchoolId) VALUES (?, ?, ?, ?)"
                            : "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)";
                        using (var cmd = new OleDbCommand(insertSql, c))
                        {
                            cmd.Parameters.AddWithValue("?", className);
                            cmd.Parameters.AddWithValue("?", LegacySubjects[i]);
                            cmd.Parameters.AddWithValue("?", i);
                            if (tenant) TenantContext.AddSchoolParameter(cmd);
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
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);
                var query = "SELECT Subject FROM ClassSubjects WHERE ClassName = ?";
                if (tenant) query += TenantContext.FilterClause();
                query += " ORDER BY SortOrder";
                using (var cmd = new OleDbCommand(query, c))
                {
                    cmd.Parameters.AddWithValue("?", className ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
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
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);
                var query = "SELECT ClassName, Subject FROM ClassSubjects WHERE 1=1";
                if (tenant) query += TenantContext.FilterClause();
                query += " ORDER BY ClassName, SortOrder";
                using (var cmd = new OleDbCommand(query, c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            string cls = r["ClassName"]?.ToString() ?? "";
                            if (!map.TryGetValue(cls, out var list)) { list = new List<string>(); map[cls] = list; }
                            list.Add(r["Subject"]?.ToString() ?? "");
                        }
                    }
                }
            }
            return map;
        }

        public async Task SetSubjectsForClassAsync(string className, IEnumerable<string> subjects)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);
                var deleteSql = "DELETE FROM ClassSubjects WHERE ClassName = ?";
                if (tenant) deleteSql += TenantContext.FilterClause();
                using (var del = new OleDbCommand(deleteSql, c))
                {
                    del.Parameters.AddWithValue("?", className ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(del);
                    await del.ExecuteNonQueryAsync();
                }
                int order = 0;
                foreach (var subject in subjects)
                {
                    var insertSql = tenant
                        ? "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder, SchoolId) VALUES (?, ?, ?, ?)"
                        : "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)";
                    using (var ins = new OleDbCommand(insertSql, c))
                    {
                        ins.Parameters.AddWithValue("?", className ?? "");
                        ins.Parameters.AddWithValue("?", subject ?? "");
                        ins.Parameters.AddWithValue("?", order++);
                        if (tenant) TenantContext.AddSchoolParameter(ins);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>The legacy hardcoded subjects — used to seed and as the fail-safe default.</summary>
        public Task SaveSubjectsForClassAsync(string className, IEnumerable<string> subjects)
        {
            return SetSubjectsForClassAsync(className, subjects);
        }

        public static readonly string[] LegacySubjects =
        {
            "MATHEMATICS", "INT. SCIENCE", "ENGLISH LANGUAGE", "SOCIAL STUDIES",
            "COMPUTING", "REL. & MORAL EDU.", "CARRER TECH.", "CREATIVE ART", "GHANAIAN LANG."
        };

        private static async Task EnsureSchoolColumnAsync(OleDbConnection c)
        {
            var schoolId = TenantContext.CurrentSchoolId;
            if (schoolId == Guid.Empty) return;
            var school = schoolId.ToString("D");
            var sql = $@"
IF OBJECT_ID(N'ClassSubjects', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('ClassSubjects','SchoolId') IS NULL
        EXEC('ALTER TABLE [ClassSubjects] ADD SchoolId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ClassSubjects_SchoolId] DEFAULT (''{school}'')');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassSubjects_SchoolId' AND object_id = OBJECT_ID(N'ClassSubjects'))
        EXEC('CREATE INDEX [IX_ClassSubjects_SchoolId] ON [ClassSubjects](SchoolId)');
END";
            using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }
    }
}
