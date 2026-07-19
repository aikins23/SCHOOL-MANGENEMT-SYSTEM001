using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Per-class subject lists. Created and seeded (every class gets department defaults) on
    /// first use. SQL Server via Microsoft.Data.SqlClient.
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string create = @"IF OBJECT_ID(N'ClassSubjects', N'U') IS NULL
                    CREATE TABLE ClassSubjects (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        ClassName NVARCHAR(50) NOT NULL,
                        Subject NVARCHAR(80) NOT NULL,
                        SortOrder INT NOT NULL DEFAULT (0));";
                using (var cmd = new SqlCommand(create, c)) await cmd.ExecuteNonQueryAsync();
                await EnsureSchoolColumnAsync(c);
                await EnsureSyncColumnsAsync(c);
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);

                // Seed each class that has no rows yet (idempotent + respects admin edits).
                foreach (var className in AppConfig.ClassNames)
                {
                    bool has;
                    var checkSql = "SELECT COUNT(*) FROM ClassSubjects WHERE ClassName = ?";
                    if (tenant) checkSql += TenantContext.FilterClauseSql();
                    using (var cmd = new SqlCommand(checkSql, c))
                    {
                        cmd.AddPositionalParameter(className);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        has = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (has) continue;
                    var defaultSubjects = SubjectCatalog.StandardSubjectsForClass(className);
                    for (int i = 0; i < defaultSubjects.Count; i++)
                    {
                        var insertSql = tenant
                            ? "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder, SchoolId) VALUES (?, ?, ?, ?)"
                            : "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)";
                        using (var cmd = new SqlCommand(insertSql, c))
                        {
                            cmd.AddPositionalParameter(className);
                            cmd.AddPositionalParameter(defaultSubjects[i]);
                            cmd.AddPositionalParameter(i);
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
            _ = Task.Run(async () =>
            {
                try { await EnsureTableAsync(); } catch { /* accessor falls back */ }
            });
        }

        public async Task<List<string>> GetSubjectsForClassAsync(string className)
        {
            var list = new List<string>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);
                var query = "SELECT Subject FROM ClassSubjects WHERE ClassName = ?";
                if (tenant) query += TenantContext.FilterClauseSql();
                query += " ORDER BY SortOrder";
                using (var cmd = new SqlCommand(query, c))
                {
                    cmd.AddPositionalParameter(className ?? "");
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);
                var query = "SELECT ClassName, Subject FROM ClassSubjects WHERE 1=1";
                if (tenant) query += TenantContext.FilterClauseSql();
                query += " ORDER BY ClassName, SortOrder";
                using (var cmd = new SqlCommand(query, c))
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, SUBJECTS_TABLE);
                var existingSyncIds = new List<Guid>();
                var existingSql = "SELECT SyncId FROM ClassSubjects WHERE ClassName = ?";
                if (tenant) existingSql += TenantContext.FilterClauseSql();
                using (var existing = new SqlCommand(existingSql, c))
                {
                    existing.AddPositionalParameter(className ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(existing);
                    using (var reader = await existing.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader["SyncId"] != DBNull.Value && Guid.TryParse(reader["SyncId"].ToString(), out var id))
                                existingSyncIds.Add(id);
                        }
                    }
                }

                foreach (var id in existingSyncIds)
                {
                    await TryRecordSubjectDeleteBySyncIdAsync(id);
                }

                var deleteSql = "DELETE FROM ClassSubjects WHERE ClassName = ?";
                if (tenant) deleteSql += TenantContext.FilterClauseSql();
                using (var del = new SqlCommand(deleteSql, c))
                {
                    del.AddPositionalParameter(className ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(del);
                    await del.ExecuteNonQueryAsync();
                }
                int order = 0;
                foreach (var subject in subjects)
                {
                    var insertSql = tenant
                        ? "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder, SchoolId) VALUES (?, ?, ?, ?)"
                        : "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)";
                    using (var ins = new SqlCommand(insertSql, c))
                    {
                        ins.AddPositionalParameter(className ?? "");
                        ins.AddPositionalParameter(subject ?? "");
                        ins.AddPositionalParameter(order++);
                        if (tenant) TenantContext.AddSchoolParameter(ins);
                        await ins.ExecuteNonQueryAsync();
                        var id = await GetLastIdentityAsync(c);
                        await TryRecordSubjectUpsertAsync(id, "Insert");
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

        private static async Task EnsureSchoolColumnAsync(SqlConnection c)
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
            using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureSyncColumnsAsync(SqlConnection c)
        {
            const string sql = @"
IF OBJECT_ID(N'ClassSubjects', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('ClassSubjects','SyncId') IS NULL
        ALTER TABLE [ClassSubjects] ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ClassSubjects_SyncId] DEFAULT NEWID();
    IF COL_LENGTH('ClassSubjects','UpdatedAt') IS NULL
        ALTER TABLE [ClassSubjects] ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT [DF_ClassSubjects_UpdatedAt] DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('ClassSubjects','RowVersion') IS NULL
        ALTER TABLE [ClassSubjects] ADD RowVersion ROWVERSION;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ClassSubjects_SyncId' AND object_id = OBJECT_ID(N'ClassSubjects'))
        CREATE UNIQUE INDEX [UX_ClassSubjects_SyncId] ON [ClassSubjects](SyncId);
END";
            using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        private async Task TryRecordSubjectUpsertAsync(object id, string operation)
        {
            try
            {
                if (id == null || string.IsNullOrWhiteSpace(Convert.ToString(id))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(SUBJECTS_TABLE, "Id", id, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Class subject sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSubjectDeleteBySyncIdAsync(Guid syncId)
        {
            try
            {
                if (syncId == Guid.Empty) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(SUBJECTS_TABLE, "SyncId", syncId);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Class subject delete sync capture skipped: " + ex.Message);
            }
        }

        private static async Task<object> GetLastIdentityAsync(SqlConnection connection)
        {
            using (var cmd = new SqlCommand("SELECT @@IDENTITY", connection))
            {
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : value;
            }
        }
    }
}
