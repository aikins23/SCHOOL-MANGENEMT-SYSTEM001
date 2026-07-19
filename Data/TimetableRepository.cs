using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class TimetableRepository
    {
        private readonly string _connectionString;
        private const string PeriodsTable = "TimePeriods";
        private const string AllocationsTable = "SubjectAllocations";
        private const string EntriesTable = "TimetableEntries";
        private const string EmployeesTable = "Employee";

        public TimetableRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<TimePeriod>> GetPeriodsAsync()
        {
            var list = new List<TimePeriod>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, PeriodsTable);
                var query = "SELECT * FROM TimePeriods WHERE 1=1";
                if (tenant) query += TenantContext.FilterClauseSql();
                query += " ORDER BY SortOrder";
                using (var cmd = new SqlCommand(query, conn))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new TimePeriod
                            {
                                PeriodID = Convert.ToInt32(reader["PeriodID"]),
                                PeriodName = reader["PeriodName"].ToString(),
                                StartTime = ToTimeSpan(reader["StartTime"]),
                                EndTime = ToTimeSpan(reader["EndTime"]),
                                IsBreak = ToBool(reader["IsBreak"]),
                                SortOrder = Convert.ToInt32(reader["SortOrder"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<SubjectAllocation>> GetAllocationsAsync(string classId = null)
        {
            var list = new List<SubjectAllocation>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var allocationTenant = await TenantContext.HasSchoolIdColumnAsync(conn, AllocationsTable);
                var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EmployeesTable);
                var query = @"SELECT sa.*, e.fullName as TeacherName
                             FROM SubjectAllocations sa
                             LEFT JOIN Employee e ON sa.TeacherID = e.employmentID";
                if (employeeTenant) query += TenantContext.FilterClauseSql("e");
                query += " WHERE 1=1";
                if (allocationTenant) query += TenantContext.FilterClauseSql("sa");
                if (!string.IsNullOrEmpty(classId)) query += " AND sa.ClassID = ?";

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (employeeTenant) TenantContext.AddSchoolParameter(cmd);
                    if (allocationTenant) TenantContext.AddSchoolParameter(cmd);
                    if (!string.IsNullOrEmpty(classId)) cmd.AddPositionalParameter(classId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SubjectAllocation
                            {
                                AllocationID = Convert.ToInt32(reader["AllocationID"]),
                                ClassID = reader["ClassID"].ToString(),
                                SubjectName = reader["SubjectName"].ToString(),
                                TeacherID = reader["TeacherID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TeacherID"]),
                                PeriodsPerWeek = Convert.ToInt32(reader["PeriodsPerWeek"]),
                                TeacherName = reader["TeacherName"]?.ToString() ?? "Not Assigned"
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<bool> SaveAllocationAsync(SubjectAllocation alloc)
        {
            int recordId = alloc.AllocationID;
            string operation = alloc.AllocationID == 0 ? "Insert" : "Update";
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AllocationsTable);
                string query = alloc.AllocationID == 0
                    ? (tenant
                        ? "INSERT INTO SubjectAllocations (ClassID, SubjectName, TeacherID, PeriodsPerWeek, SchoolId) VALUES (?, ?, ?, ?, ?)"
                        : "INSERT INTO SubjectAllocations (ClassID, SubjectName, TeacherID, PeriodsPerWeek) VALUES (?, ?, ?, ?)")
                    : "UPDATE SubjectAllocations SET ClassID=?, SubjectName=?, TeacherID=?, PeriodsPerWeek=? WHERE AllocationID=?";
                if (alloc.AllocationID != 0 && tenant) query += TenantContext.FilterClauseSql();

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(alloc.ClassID);
                    cmd.AddPositionalParameter(alloc.SubjectName);
                    cmd.AddPositionalParameter((object)alloc.TeacherID ?? DBNull.Value);
                    cmd.AddPositionalParameter(alloc.PeriodsPerWeek);
                    if (alloc.AllocationID == 0 && tenant) TenantContext.AddSchoolParameter(cmd);
                    if (alloc.AllocationID != 0) cmd.AddPositionalParameter(alloc.AllocationID);
                    if (alloc.AllocationID != 0 && tenant) TenantContext.AddSchoolParameter(cmd);
                    var saved = await cmd.ExecuteNonQueryAsync() > 0;
                    if (saved && alloc.AllocationID == 0)
                    {
                        using (var idCmd = new SqlCommand("SELECT @@IDENTITY", conn))
                        {
                            var id = await idCmd.ExecuteScalarAsync();
                            recordId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                        }
                    }

                    if (saved)
                    {
                        await TryRecordSyncUpsertAsync(AllocationsTable, "AllocationID", recordId, operation);
                    }

                    return saved;
                }
            }
        }

        public async Task<bool> DeleteAllocationAsync(int allocationId)
        {
            if (allocationId <= 0) return false;

            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AllocationsTable);
                var deletedSyncIds = new List<Guid>();

                try
                {
                    await SyncSchema.EnsureSyncInfrastructureAsync(conn, TenantContext.RequireSchoolId());
                    var syncSql = "SELECT SyncId FROM SubjectAllocations WHERE AllocationID = ?";
                    if (tenant) syncSql += TenantContext.FilterClauseSql();
                    using (var syncCmd = new SqlCommand(syncSql, conn))
                    {
                        syncCmd.AddPositionalParameter(allocationId);
                        if (tenant) TenantContext.AddSchoolParameter(syncCmd);
                        var syncId = await syncCmd.ExecuteScalarAsync();
                        if (syncId != null && syncId != DBNull.Value)
                            deletedSyncIds.Add((Guid)syncId);
                    }
                }
                catch (Exception ex)
                {
                    Services.LoggerHelper.LogWarning("Subject allocation sync scan skipped: " + ex.Message);
                }

                var deleteSql = "DELETE FROM SubjectAllocations WHERE AllocationID = ?";
                if (tenant) deleteSql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(deleteSql, conn))
                {
                    cmd.AddPositionalParameter(allocationId);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var deleted = await cmd.ExecuteNonQueryAsync() > 0;
                    if (deleted)
                        await TryRecordSyncDeletesAsync(AllocationsTable, deletedSyncIds);
                    return deleted;
                }
            }
        }

        public async Task SaveTimetableBatchAsync(string classId, List<TimetableEntry> entries)
        {
            var batches = new Dictionary<string, List<TimetableEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                [classId] = entries ?? new List<TimetableEntry>()
            };
            await SaveTimetableBatchesAsync(batches);
        }

        public async Task SaveTimetableBatchesAsync(Dictionary<string, List<TimetableEntry>> batches)
        {
            batches = batches ?? new Dictionary<string, List<TimetableEntry>>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var deletedSyncIds = new List<Guid>();
                foreach (var classId in batches.Keys)
                {
                    deletedSyncIds.AddRange(await TryGetTimetableEntrySyncIdsAsync(conn, classId));
                }
                await TryRecordSyncDeletesAsync(EntriesTable, deletedSyncIds);
                var insertedIds = new List<int>();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var classId in batches.Keys)
                        {
                            var delQuery = "DELETE FROM TimetableEntries WHERE ClassID = ?";
                            if (tenant) delQuery += TenantContext.FilterClauseSql();
                            using (var delCmd = new SqlCommand(delQuery, conn, trans))
                            {
                                delCmd.AddPositionalParameter(classId);
                                if (tenant) TenantContext.AddSchoolParameter(delCmd);
                                await delCmd.ExecuteNonQueryAsync();
                            }
                        }

                        var insQuery = tenant
                            ? "INSERT INTO TimetableEntries (ClassID, PeriodID, DayOfWeek, SubjectName, TeacherID, SchoolId) VALUES (?, ?, ?, ?, ?, ?)"
                            : "INSERT INTO TimetableEntries (ClassID, PeriodID, DayOfWeek, SubjectName, TeacherID) VALUES (?, ?, ?, ?, ?)";
                        foreach (var batch in batches)
                        {
                            foreach (var entry in batch.Value ?? new List<TimetableEntry>())
                            {
                                using (var insCmd = new SqlCommand(insQuery, conn, trans))
                                {
                                    insCmd.AddPositionalParameter(batch.Key);
                                    insCmd.AddPositionalParameter(entry.PeriodID);
                                    insCmd.AddPositionalParameter(entry.DayOfWeek);
                                    insCmd.AddPositionalParameter(entry.SubjectName);
                                    insCmd.AddPositionalParameter((object)entry.TeacherID ?? DBNull.Value);
                                    if (tenant) TenantContext.AddSchoolParameter(insCmd);
                                    await insCmd.ExecuteNonQueryAsync();
                                    using (var idCmd = new SqlCommand("SELECT @@IDENTITY", conn, trans))
                                    {
                                        var id = await idCmd.ExecuteScalarAsync();
                                        if (id != null && id != DBNull.Value)
                                        {
                                            insertedIds.Add(Convert.ToInt32(id));
                                        }
                                    }
                                }
                            }
                        }
                        trans.Commit();
                    }
                    catch { trans.Rollback(); throw; }
                }
                foreach (var id in insertedIds)
                {
                    await TryRecordSyncUpsertAsync(EntriesTable, "EntryID", id, "Insert");
                }
            }
        }

        public async Task<List<TimetableEntry>> GetTimetableAsync(string classId)
        {
            var list = new List<TimetableEntry>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var entryTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                var periodTenant = await TenantContext.HasSchoolIdColumnAsync(conn, PeriodsTable);
                var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EmployeesTable);
                var query = @"SELECT t.*, p.PeriodName, e.fullName as TeacherName
                             FROM TimetableEntries t
                             INNER JOIN TimePeriods p ON t.PeriodID = p.PeriodID";
                if (periodTenant) query += TenantContext.FilterClauseSql("p");
                query += " LEFT JOIN Employee e ON t.TeacherID = e.employmentID";
                if (employeeTenant) query += TenantContext.FilterClauseSql("e");
                query += @" WHERE t.ClassID = ?
                             ORDER BY t.DayOfWeek, p.SortOrder";
                if (entryTenant)
                {
                    query = query.Replace("ORDER BY", TenantContext.FilterClauseSql("t") + " ORDER BY");
                }

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (periodTenant) TenantContext.AddSchoolParameter(cmd);
                    if (employeeTenant) TenantContext.AddSchoolParameter(cmd);
                    cmd.AddPositionalParameter(classId);
                    if (entryTenant) TenantContext.AddSchoolParameter(cmd);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new TimetableEntry
                            {
                                EntryID = Convert.ToInt32(reader["EntryID"]),
                                ClassID = reader["ClassID"].ToString(),
                                PeriodID = Convert.ToInt32(reader["PeriodID"]),
                                DayOfWeek = Convert.ToInt32(reader["DayOfWeek"]),
                                SubjectName = reader["SubjectName"].ToString(),
                                TeacherID = reader["TeacherID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TeacherID"]),
                                PeriodName = reader["PeriodName"].ToString(),
                                TeacherName = reader["TeacherName"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<HashSet<string>> GetTeacherBusySlotsExcludingClassAsync(string classId)
        {
            var slots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                var query = @"SELECT TeacherID, DayOfWeek, PeriodID
                              FROM TimetableEntries
                              WHERE TeacherID IS NOT NULL AND ClassID <> ?";
                if (tenant) query += TenantContext.FilterClauseSql();

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(classId ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var teacherId = Convert.ToInt32(reader["TeacherID"]);
                            var day = Convert.ToInt32(reader["DayOfWeek"]);
                            var periodId = Convert.ToInt32(reader["PeriodID"]);
                            slots.Add(BuildTeacherSlotKey(teacherId, day, periodId));
                        }
                    }
                }
            }
            return slots;
        }

        public async Task<HashSet<string>> GetTeacherBusySlotsExcludingClassesAsync(IEnumerable<string> classIds)
        {
            var excluded = new HashSet<string>(classIds ?? new string[0], StringComparer.OrdinalIgnoreCase);
            var slots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                var query = @"SELECT TeacherID, DayOfWeek, PeriodID, ClassID
                              FROM TimetableEntries
                              WHERE TeacherID IS NOT NULL";
                if (tenant) query += TenantContext.FilterClauseSql();

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var classId = reader["ClassID"]?.ToString() ?? "";
                            if (excluded.Contains(classId)) continue;

                            var teacherId = Convert.ToInt32(reader["TeacherID"]);
                            var day = Convert.ToInt32(reader["DayOfWeek"]);
                            var periodId = Convert.ToInt32(reader["PeriodID"]);
                            slots.Add(BuildTeacherSlotKey(teacherId, day, periodId));
                        }
                    }
                }
            }
            return slots;
        }

        public static string BuildTeacherSlotKey(int teacherId, int day, int periodId)
        {
            return teacherId + "|" + day + "|" + periodId;
        }

        public async Task SetPeriodsAsync(IEnumerable<TimePeriod> periods)
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var deletedEntrySyncIds = await TryGetAllSyncIdsAsync(conn, EntriesTable);
                var deletedPeriodSyncIds = await TryGetAllSyncIdsAsync(conn, PeriodsTable);
                await TryRecordSyncDeletesAsync(EntriesTable, deletedEntrySyncIds);
                await TryRecordSyncDeletesAsync(PeriodsTable, deletedPeriodSyncIds);
                var insertedPeriodIds = new List<int>();
                var entriesTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                var periodsTenant = await TenantContext.HasSchoolIdColumnAsync(conn, PeriodsTable);
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var clearEntriesSql = "DELETE FROM TimetableEntries";
                        if (entriesTenant) clearEntriesSql += " WHERE 1=1" + TenantContext.FilterClauseSql();
                        using (var clearEntries = new SqlCommand(clearEntriesSql, conn, trans))
                        {
                            if (entriesTenant) TenantContext.AddSchoolParameter(clearEntries);
                            await clearEntries.ExecuteNonQueryAsync();
                        }

                        var clearPeriodsSql = "DELETE FROM TimePeriods";
                        if (periodsTenant) clearPeriodsSql += " WHERE 1=1" + TenantContext.FilterClauseSql();
                        using (var clearPeriods = new SqlCommand(clearPeriodsSql, conn, trans))
                        {
                            if (periodsTenant) TenantContext.AddSchoolParameter(clearPeriods);
                            await clearPeriods.ExecuteNonQueryAsync();
                        }

                        var insert = periodsTenant
                            ? "INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder, SchoolId) VALUES (?, ?, ?, ?, ?, ?)"
                            : "INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder) VALUES (?, ?, ?, ?, ?)";
                        foreach (var period in periods)
                        {
                            using (var cmd = new SqlCommand(insert, conn, trans))
                            {
                                cmd.AddPositionalParameter(period.PeriodName ?? "");
                                cmd.AddPositionalParameter(period.StartTime);
                                cmd.AddPositionalParameter(period.EndTime);
                                cmd.AddPositionalParameter(period.IsBreak);
                                cmd.AddPositionalParameter(period.SortOrder);
                                if (periodsTenant) TenantContext.AddSchoolParameter(cmd);
                                await cmd.ExecuteNonQueryAsync();
                                using (var idCmd = new SqlCommand("SELECT @@IDENTITY", conn, trans))
                                {
                                    var id = await idCmd.ExecuteScalarAsync();
                                    if (id != null && id != DBNull.Value)
                                    {
                                        insertedPeriodIds.Add(Convert.ToInt32(id));
                                    }
                                }
                            }
                        }

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
                foreach (var id in insertedPeriodIds)
                {
                    await TryRecordSyncUpsertAsync(PeriodsTable, "PeriodID", id, "Insert");
                }
            }
        }

        private static TimeSpan ToTimeSpan(object value)
        {
            if (value == null || value == DBNull.Value) return TimeSpan.Zero;
            if (value is TimeSpan span) return span;
            if (value is DateTime date) return date.TimeOfDay;

            var text = value.ToString();
            if (TimeSpan.TryParse(text, out var parsedSpan)) return parsedSpan;
            if (DateTime.TryParse(text, out var parsedDate)) return parsedDate.TimeOfDay;
            return TimeSpan.Zero;
        }

        private static bool ToBool(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool flag) return flag;
            if (value is byte b) return b != 0;
            if (value is short s) return s != 0;
            if (value is int i) return i != 0;

            var text = value.ToString();
            if (bool.TryParse(text, out var parsedBool)) return parsedBool;
            if (int.TryParse(text, out var parsedInt)) return parsedInt != 0;
            return false;
        }

        private async Task<List<Guid>> TryGetTimetableEntrySyncIdsAsync(SqlConnection conn, string classId)
        {
            var syncIds = new List<Guid>();
            try
            {
                await SyncSchema.EnsureSyncInfrastructureAsync(conn, TenantContext.RequireSchoolId());
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                var sql = "SELECT SyncId FROM TimetableEntries WHERE ClassID = ?";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.AddPositionalParameter(classId);
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
                Services.LoggerHelper.LogWarning("Timetable entry sync scan skipped: " + ex.Message);
            }
            return syncIds;
        }

        private async Task<List<Guid>> TryGetAllSyncIdsAsync(SqlConnection conn, string tableName)
        {
            var syncIds = new List<Guid>();
            try
            {
                await SyncSchema.EnsureSyncInfrastructureAsync(conn, TenantContext.RequireSchoolId());
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, tableName);
                var sql = "SELECT SyncId FROM " + tableName + " WHERE 1=1";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(sql, conn))
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
                Services.LoggerHelper.LogWarning(tableName + " sync scan skipped: " + ex.Message);
            }
            return syncIds;
        }

        private async Task TryRecordSyncUpsertAsync(string tableName, string primaryKeyName, int primaryKeyValue, string operation)
        {
            if (primaryKeyValue <= 0) return;
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(tableName, primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning(tableName + " sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeletesAsync(string tableName, IEnumerable<Guid> syncIds)
        {
            if (syncIds == null) return;
            foreach (var syncId in syncIds)
            {
                if (syncId == Guid.Empty) continue;
                try
                {
                    await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(tableName, "SyncId", syncId);
                }
                catch (Exception ex)
                {
                    Services.LoggerHelper.LogWarning(tableName + " delete sync capture skipped: " + ex.Message);
                }
            }
        }
    }
}
