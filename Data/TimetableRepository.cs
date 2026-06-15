using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

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
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, PeriodsTable);
                var query = "SELECT * FROM TimePeriods WHERE 1=1";
                if (tenant) query += TenantContext.FilterClause();
                query += " ORDER BY SortOrder";
                using (var cmd = new OleDbCommand(query, conn))
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
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var allocationTenant = await TenantContext.HasSchoolIdColumnAsync(conn, AllocationsTable);
                var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EmployeesTable);
                var query = @"SELECT sa.*, e.fullName as TeacherName 
                             FROM SubjectAllocations sa 
                             LEFT JOIN Employee e ON sa.TeacherID = e.employmentID";
                if (employeeTenant) query += TenantContext.FilterClause("e");
                query += " WHERE 1=1";
                if (allocationTenant) query += TenantContext.FilterClause("sa");
                if (!string.IsNullOrEmpty(classId)) query += " AND sa.ClassID = ?";
                
                using (var cmd = new OleDbCommand(query, conn))
                {
                    if (employeeTenant) TenantContext.AddSchoolParameter(cmd);
                    if (allocationTenant) TenantContext.AddSchoolParameter(cmd);
                    if (!string.IsNullOrEmpty(classId)) cmd.Parameters.AddWithValue("?", classId);
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
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AllocationsTable);
                string query = alloc.AllocationID == 0 
                    ? (tenant
                        ? "INSERT INTO SubjectAllocations (ClassID, SubjectName, TeacherID, PeriodsPerWeek, SchoolId) VALUES (?, ?, ?, ?, ?)"
                        : "INSERT INTO SubjectAllocations (ClassID, SubjectName, TeacherID, PeriodsPerWeek) VALUES (?, ?, ?, ?)")
                    : "UPDATE SubjectAllocations SET ClassID=?, SubjectName=?, TeacherID=?, PeriodsPerWeek=? WHERE AllocationID=?";
                if (alloc.AllocationID != 0 && tenant) query += TenantContext.FilterClause();
                
                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", alloc.ClassID);
                    cmd.Parameters.AddWithValue("?", alloc.SubjectName);
                    cmd.Parameters.AddWithValue("?", (object)alloc.TeacherID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("?", alloc.PeriodsPerWeek);
                    if (alloc.AllocationID == 0 && tenant) TenantContext.AddSchoolParameter(cmd);
                    if (alloc.AllocationID != 0) cmd.Parameters.AddWithValue("?", alloc.AllocationID);
                    if (alloc.AllocationID != 0 && tenant) TenantContext.AddSchoolParameter(cmd);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task SaveTimetableBatchAsync(string classId, List<TimetableEntry> entries)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                        // 1. Clear existing for this class
                        var delQuery = "DELETE FROM TimetableEntries WHERE ClassID = ?";
                        if (tenant) delQuery += TenantContext.FilterClause();
                        using (var delCmd = new OleDbCommand(delQuery, conn, trans))
                        {
                            delCmd.Parameters.AddWithValue("?", classId);
                            if (tenant) TenantContext.AddSchoolParameter(delCmd);
                            await delCmd.ExecuteNonQueryAsync();
                        }

                        // 2. Insert new
                        var insQuery = tenant
                            ? "INSERT INTO TimetableEntries (ClassID, PeriodID, DayOfWeek, SubjectName, TeacherID, SchoolId) VALUES (?, ?, ?, ?, ?, ?)"
                            : "INSERT INTO TimetableEntries (ClassID, PeriodID, DayOfWeek, SubjectName, TeacherID) VALUES (?, ?, ?, ?, ?)";
                        foreach (var entry in entries)
                        {
                            using (var insCmd = new OleDbCommand(insQuery, conn, trans))
                            {
                                insCmd.Parameters.AddWithValue("?", classId);
                                insCmd.Parameters.AddWithValue("?", entry.PeriodID);
                                insCmd.Parameters.AddWithValue("?", entry.DayOfWeek);
                                insCmd.Parameters.AddWithValue("?", entry.SubjectName);
                                insCmd.Parameters.AddWithValue("?", (object)entry.TeacherID ?? DBNull.Value);
                                if (tenant) TenantContext.AddSchoolParameter(insCmd);
                                await insCmd.ExecuteNonQueryAsync();
                            }
                        }
                        trans.Commit();
                    }
                    catch { trans.Rollback(); throw; }
                }
            }
        }

        public async Task<List<TimetableEntry>> GetTimetableAsync(string classId)
        {
            var list = new List<TimetableEntry>();
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var entryTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                var periodTenant = await TenantContext.HasSchoolIdColumnAsync(conn, PeriodsTable);
                var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EmployeesTable);
                var query = @"SELECT t.*, p.PeriodName, e.fullName as TeacherName 
                             FROM TimetableEntries t
                             INNER JOIN TimePeriods p ON t.PeriodID = p.PeriodID";
                if (periodTenant) query += TenantContext.FilterClause("p");
                query += " LEFT JOIN Employee e ON t.TeacherID = e.employmentID";
                if (employeeTenant) query += TenantContext.FilterClause("e");
                query += @" WHERE t.ClassID = ?
                             ORDER BY t.DayOfWeek, p.SortOrder";
                if (entryTenant)
                {
                    query = query.Replace("ORDER BY", TenantContext.FilterClause("t") + " ORDER BY");
                }

                using (var cmd = new OleDbCommand(query, conn))
                {
                    if (periodTenant) TenantContext.AddSchoolParameter(cmd);
                    if (employeeTenant) TenantContext.AddSchoolParameter(cmd);
                    cmd.Parameters.AddWithValue("?", classId);
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

        public async Task SetPeriodsAsync(IEnumerable<TimePeriod> periods)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var entriesTenant = await TenantContext.HasSchoolIdColumnAsync(conn, EntriesTable);
                        var periodsTenant = await TenantContext.HasSchoolIdColumnAsync(conn, PeriodsTable);
                        var clearEntriesSql = "DELETE FROM TimetableEntries";
                        if (entriesTenant) clearEntriesSql += " WHERE 1=1" + TenantContext.FilterClause();
                        using (var clearEntries = new OleDbCommand(clearEntriesSql, conn, trans))
                        {
                            if (entriesTenant) TenantContext.AddSchoolParameter(clearEntries);
                            await clearEntries.ExecuteNonQueryAsync();
                        }

                        var clearPeriodsSql = "DELETE FROM TimePeriods";
                        if (periodsTenant) clearPeriodsSql += " WHERE 1=1" + TenantContext.FilterClause();
                        using (var clearPeriods = new OleDbCommand(clearPeriodsSql, conn, trans))
                        {
                            if (periodsTenant) TenantContext.AddSchoolParameter(clearPeriods);
                            await clearPeriods.ExecuteNonQueryAsync();
                        }

                        var insert = periodsTenant
                            ? "INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder, SchoolId) VALUES (?, ?, ?, ?, ?, ?)"
                            : "INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder) VALUES (?, ?, ?, ?, ?)";
                        foreach (var period in periods)
                        {
                            using (var cmd = new OleDbCommand(insert, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("?", period.PeriodName ?? "");
                                cmd.Parameters.AddWithValue("?", period.StartTime);
                                cmd.Parameters.AddWithValue("?", period.EndTime);
                                cmd.Parameters.AddWithValue("?", period.IsBreak);
                                cmd.Parameters.AddWithValue("?", period.SortOrder);
                                if (periodsTenant) TenantContext.AddSchoolParameter(cmd);
                                await cmd.ExecuteNonQueryAsync();
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
    }
}
