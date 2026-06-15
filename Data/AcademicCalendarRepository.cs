using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

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
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var query = "SELECT * FROM AcademicCalendar ORDER BY StartDate DESC";
                using (var cmd = new OleDbCommand(query, conn))
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
            return list;
        }

        public async Task<bool> SaveTermAsync(AcademicTerm term)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query;
                if (term.TermID == 0)
                    query = "INSERT INTO AcademicCalendar (TermName, StartDate, EndDate, IsActive) VALUES (?, ?, ?, ?)";
                else
                    query = "UPDATE AcademicCalendar SET TermName=?, StartDate=?, EndDate=?, IsActive=? WHERE TermID=?";

                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", term.TermName);
                    cmd.Parameters.AddWithValue("?", term.StartDate);
                    cmd.Parameters.AddWithValue("?", term.EndDate);
                    cmd.Parameters.AddWithValue("?", term.IsActive);
                    if (term.TermID != 0) cmd.Parameters.AddWithValue("?", term.TermID);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<List<SchoolEvent>> GetEventsAsync(DateTime start, DateTime end)
        {
            var list = new List<SchoolEvent>();
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var query = "SELECT * FROM SchoolHolidays WHERE EventDate BETWEEN ? AND ? ORDER BY EventDate";
                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", start);
                    cmd.Parameters.AddWithValue("?", end);
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
    }
}
