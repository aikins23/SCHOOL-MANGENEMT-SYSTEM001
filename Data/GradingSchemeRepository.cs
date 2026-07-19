using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Persists the school-wide grading scheme (ordered grade bands). Created and seeded
    /// from the legacy 5-band scheme on first use. SQL Server via Microsoft.Data.SqlClient.
    /// </summary>
    public class GradingSchemeRepository : IGradingSchemeRepository
    {
        private readonly string _connectionString;

        public GradingSchemeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                string schoolDefault = TenantContext.CurrentSchoolId == Guid.Empty
                    ? "NULL"
                    : "'" + TenantContext.CurrentSchoolId.ToString("D") + "'";
                string create = @"IF OBJECT_ID(N'GradingScheme', N'U') IS NULL
                    CREATE TABLE GradingScheme (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        SchoolId UNIQUEIDENTIFIER NULL CONSTRAINT DF_GradingScheme_SchoolId DEFAULT " + schoolDefault + @",
                        MinScore INT NOT NULL,
                        Code NVARCHAR(10),
                        Label NVARCHAR(80));";
                using (var cmd = new SqlCommand(create, c)) await cmd.ExecuteNonQueryAsync();

                await EnsureSchoolColumnAsync(c);
                bool tenant = await IsTenantScopedAsync(c);
                bool hasRows;
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM GradingScheme WHERE 1=1" + (tenant ? TenantContext.FilterClauseSql() : ""), c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    hasRows = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
                if (!hasRows)
                {
                    foreach (var b in LegacyBands())
                    {
                        string insertSql = tenant
                            ? "INSERT INTO GradingScheme (SchoolId, MinScore, Code, Label) VALUES (@SchoolId, ?, ?, ?)"
                            : "INSERT INTO GradingScheme (MinScore, Code, Label) VALUES (?, ?, ?)";
                        using (var cmd = new SqlCommand(insertSql, c))
                        {
                            if (tenant) TenantContext.AddSchoolParameter(cmd);
                            cmd.AddPositionalParameter(b.MinScore);
                            cmd.AddPositionalParameter(b.Code);
                            cmd.AddPositionalParameter(b.Label);
                            await cmd.ExecuteNonQueryAsync();
                            using (var idCmd = new SqlCommand("SELECT SCOPE_IDENTITY()", c))
                            {
                                var id = await idCmd.ExecuteScalarAsync();
                                if (id != null && id != DBNull.Value)
                                {
                                    await TryRecordSyncUpsertAsync(Convert.ToInt32(id), "Insert");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>EnsureTableAsync but swallows errors (used by the fail-safe accessor).</summary>
        public void EnsureTablesAsyncSafe()
        {
            _ = Task.Run(async () =>
            {
                try { await EnsureTableAsync(); } catch { /* accessor falls back */ }
            });
        }

        public async Task<List<GradeBand>> GetBandsAsync()
        {
            var list = new List<GradeBand>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                bool tenant = await IsTenantScopedAsync(c);
                using (var cmd = new SqlCommand(
                    "SELECT MinScore, Code, Label FROM GradingScheme WHERE 1=1"
                    + (tenant ? TenantContext.FilterClauseSql() : "")
                    + " ORDER BY MinScore DESC", c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new GradeBand
                        {
                            MinScore = Convert.ToInt32(r["MinScore"]),
                            Code = AsString(r["Code"]),
                            Label = AsString(r["Label"])
                        });
                }
            }
            return list;
        }

        public async Task SaveBandsAsync(IEnumerable<GradeBand> bands)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await EnsureSchoolColumnAsync(c);
                bool tenant = await IsTenantScopedAsync(c);
                var deletedSyncIds = await TryGetAllSyncIdsAsync(c);
                foreach (var syncId in deletedSyncIds)
                {
                    await TryRecordSyncDeleteAsync(syncId);
                }

                using (var del = new SqlCommand("DELETE FROM GradingScheme WHERE 1=1" + (tenant ? TenantContext.FilterClauseSql() : ""), c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(del);
                    await del.ExecuteNonQueryAsync();
                }
                foreach (var b in bands)
                {
                    string insertSql = tenant
                        ? "INSERT INTO GradingScheme (SchoolId, MinScore, Code, Label) VALUES (@SchoolId, ?, ?, ?)"
                        : "INSERT INTO GradingScheme (MinScore, Code, Label) VALUES (?, ?, ?)";
                    using (var cmd = new SqlCommand(insertSql, c))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        cmd.AddPositionalParameter(b.MinScore);
                        cmd.AddPositionalParameter(b.Code ?? "");
                        cmd.AddPositionalParameter(b.Label ?? "");
                        await cmd.ExecuteNonQueryAsync();
                        using (var idCmd = new SqlCommand("SELECT SCOPE_IDENTITY()", c))
                        {
                            var id = await idCmd.ExecuteScalarAsync();
                            if (id != null && id != DBNull.Value)
                            {
                                await TryRecordSyncUpsertAsync(Convert.ToInt32(id), "Insert");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>The hardcoded scheme used to seed the table and as the fail-safe default.</summary>
        public static List<GradeBand> LegacyBands() => new List<GradeBand>
        {
            new GradeBand { MinScore = 80, Code = "1", Label = "Advance(A)" },
            new GradeBand { MinScore = 75, Code = "2", Label = "Proficiency(P)" },
            new GradeBand { MinScore = 70, Code = "3", Label = "Approaching Proficiency(AP)" },
            new GradeBand { MinScore = 65, Code = "4", Label = "Developing" },
            new GradeBand { MinScore = 0,  Code = "5", Label = "Beginning" }
        };

        private static string AsString(object o) => o == null || o == DBNull.Value ? "" : o.ToString();

        private async Task EnsureSchoolColumnAsync(SqlConnection connection)
        {
            if (TenantContext.CurrentSchoolId == Guid.Empty) return;

            const string sql = @"
IF OBJECT_ID(N'GradingScheme', N'U') IS NOT NULL AND COL_LENGTH('GradingScheme','SchoolId') IS NULL
    ALTER TABLE GradingScheme ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'GradingScheme', N'U') IS NOT NULL
    UPDATE GradingScheme SET SchoolId = @SchoolId WHERE SchoolId IS NULL;";
            using (var cmd = new SqlCommand(sql, connection))
            {
                TenantContext.AddSchoolParameter(cmd);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<bool> IsTenantScopedAsync(SqlConnection connection)
        {
            return TenantContext.CurrentSchoolId != Guid.Empty
                && await TenantContext.HasSchoolIdColumnAsync(connection, "GradingScheme");
        }

        private async Task<List<Guid>> TryGetAllSyncIdsAsync(SqlConnection connection)
        {
            var syncIds = new List<Guid>();
            try
            {
                await SyncSchema.EnsureSyncInfrastructureAsync(connection, TenantContext.RequireSchoolId());
                bool tenant = await IsTenantScopedAsync(connection);
                using (var cmd = new SqlCommand("SELECT SyncId FROM GradingScheme WHERE 1=1" + (tenant ? TenantContext.FilterClauseSql() : ""), connection))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["SyncId"] != DBNull.Value)
                        {
                            syncIds.Add((Guid)reader["SyncId"]);
                        }
                    }
                }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Grading scheme delete sync scan skipped: " + ex.Message);
            }
            return syncIds;
        }

        private async Task TryRecordSyncUpsertAsync(int id, string operation)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync("GradingScheme", "Id", id, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Grading scheme sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(Guid syncId)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync("GradingScheme", "SyncId", syncId);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Grading scheme delete sync capture skipped: " + ex.Message);
            }
        }
    }
}
