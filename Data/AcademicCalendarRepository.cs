using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class AcademicCalendarRepository
    {
        private readonly string _connectionString;

        public AcademicCalendarRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<AcademicTerm>> GetAllTermsAsync()
        {
            var list = new List<AcademicTerm>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, "AcademicCalendar");
                var query = "SELECT * FROM AcademicCalendar ORDER BY StartDate DESC";
                if (tenant) query = "SELECT * FROM AcademicCalendar WHERE 1=1" + TenantContext.FilterClauseSql() + " ORDER BY StartDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AcademicTerm
                            {
                                TermID = Convert.ToInt32(reader["TermID"]),
                                TermName = reader["TermName"].ToString(),
                                StartDate = (DateTime)reader["StartDate"],
                                EndDate = (DateTime)reader["EndDate"],
                                IsActive = ToBool(reader["IsActive"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<bool> SaveTermAsync(AcademicTerm term)
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, "AcademicCalendar");
                string query;
                if (term.TermID == 0)
                    query = tenant
                        ? "INSERT INTO AcademicCalendar (TermName, StartDate, EndDate, IsActive, SchoolId) VALUES (?, ?, ?, ?, ?)"
                        : "INSERT INTO AcademicCalendar (TermName, StartDate, EndDate, IsActive) VALUES (?, ?, ?, ?)";
                else
                {
                    query = "UPDATE AcademicCalendar SET TermName=?, StartDate=?, EndDate=?, IsActive=? WHERE TermID=?";
                    if (tenant) query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(term.TermName);
                    cmd.AddPositionalParameter(term.StartDate);
                    cmd.AddPositionalParameter(term.EndDate);
                    cmd.AddPositionalParameter(term.IsActive);
                    if (term.TermID == 0 && tenant) TenantContext.AddSchoolParameter(cmd);
                    if (term.TermID != 0) cmd.AddPositionalParameter(term.TermID);
                    if (term.TermID != 0 && tenant) TenantContext.AddSchoolParameter(cmd);
                    var saved = await cmd.ExecuteNonQueryAsync() > 0;
                    var termId = term.TermID;
                    if (saved && term.TermID == 0)
                    {
                        using (var idCmd = new SqlCommand("SELECT @@IDENTITY", conn))
                        {
                            var id = await idCmd.ExecuteScalarAsync();
                            termId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                        }
                    }

                    if (saved)
                    {
                        await TryRecordSyncUpsertAsync(termId, term.TermID == 0 ? "Insert" : "Update");
                    }

                    return saved;
                }
            }
        }

        public async Task<List<SchoolEvent>> GetEventsAsync(DateTime start, DateTime end)
        {
            var list = new List<SchoolEvent>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, "SchoolHolidays");
                var query = "SELECT * FROM SchoolHolidays WHERE EventDate BETWEEN ? AND ? ORDER BY EventDate";
                if (tenant)
                {
                    query = "SELECT * FROM SchoolHolidays WHERE EventDate BETWEEN ? AND ?" + TenantContext.FilterClauseSql() + " ORDER BY EventDate";
                }
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(start);
                    cmd.AddPositionalParameter(end);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SchoolEvent
                            {
                                EventID = Convert.ToInt32(reader["EventID"]),
                                EventName = reader["EventName"].ToString(),
                                EventDate = (DateTime)reader["EventDate"],
                                EventType = reader["EventType"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
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

        private async Task TryRecordSyncUpsertAsync(int termId, string operation)
        {
            if (termId <= 0) return;
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync("AcademicCalendar", "TermID", termId, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Academic calendar sync capture skipped: " + ex.Message);
            }
        }
    }
}
