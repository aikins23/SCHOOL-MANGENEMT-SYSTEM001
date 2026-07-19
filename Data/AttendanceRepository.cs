using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly string _connectionString;
        private const string ATTENDANCE_TABLE = "Attendance";
        private const string STUDENTS_TABLE = "Students";
        private const string EMPLOYEE_TABLE = "Employee";

        public AttendanceRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task EnsureTableExistsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM sys.tables WHERE name = ?";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(ATTENDANCE_TABLE);
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            var createSql = $@"
                                CREATE TABLE {ATTENDANCE_TABLE}
                                (
                                    AttendanceID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    ReferenceID varchar(50) NOT NULL,
                                    ReferenceType varchar(20) NOT NULL,
                                    FullName varchar(120) NOT NULL,
                                    [Date] date NOT NULL,
                                    [Status] varchar(20) NOT NULL,
                                    Remarks varchar(200) NULL,
                                    [CreatedDate] datetime NOT NULL DEFAULT GETDATE()
                                )";
                            using (var createCmd = new SqlCommand(createSql, connection))
                            {
                                await createCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Failed to ensure attendance table exists", ex);
            }
        }

        public async Task<DataTable> GetTargetListAsync(string type, string classId, DateTime date)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var attendanceTenant = await TenantContext.HasSchoolIdColumnAsync(connection, ATTENDANCE_TABLE);
                    var studentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    string query;
                    if (type == "STUDENT")
                    {
                        string classFilter = (classId == "All Classes") ? "" : " AND s.ClassID = ?";
                        string studentFilter = studentTenant ? TenantContext.FilterClauseSql("s") : "";
                        string attendanceFilter = attendanceTenant ? TenantContext.FilterClauseSql("a") : "";
                        query = $@"
                            SELECT s.StudentID AS [ID], s.FirstName + ' ' + s.LastName AS [Full Name], s.ClassID AS [Class],
                                   COALESCE(a.Status, 'PRESENT') AS [Status], COALESCE(a.Remarks, '') AS [Remarks]
                            FROM Students s LEFT JOIN {ATTENDANCE_TABLE} a ON s.StudentID = a.ReferenceID
                                 AND a.ReferenceType = 'STUDENT' AND a.[Date] = ? {attendanceFilter}
                            WHERE 1=1 {studentFilter} {classFilter}
                            ORDER BY s.ClassID, s.FirstName";
                    }
                    else
                    {
                        string employeeFilter = employeeTenant ? TenantContext.FilterClauseSql("e") : "";
                        string attendanceFilter = attendanceTenant ? TenantContext.FilterClauseSql("a") : "";
                        query = $@"
                            SELECT e.employmentID AS [ID], e.fullName AS [Full Name], e.position AS [Position],
                                   COALESCE(a.Status, 'PRESENT') AS [Status], COALESCE(a.Remarks, '') AS [Remarks]
                            FROM Employee e LEFT JOIN {ATTENDANCE_TABLE} a ON e.employmentID = a.ReferenceID
                                 AND a.ReferenceType = 'STAFF' AND a.[Date] = ? {attendanceFilter}
                            WHERE 1=1 {employeeFilter}
                            ORDER BY e.fullName";
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(date);
                        if (attendanceTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        if (type == "STUDENT" && studentTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        if (type != "STUDENT" && employeeTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        if (type == "STUDENT" && classId != "All Classes")
                        {
                            command.AddPositionalParameter(classId);
                        }
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error loading attendance target list", ex);
                throw new DataException("Error loading attendance target list", ex);
            }
            return table;
        }

        public async Task<DataTable> GetMonthlyAnalysisAsync(string type, int month, int year)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var attendanceTenant = await TenantContext.HasSchoolIdColumnAsync(connection, ATTENDANCE_TABLE);
                    var query = $@"
                        SELECT
                            ReferenceID AS [ID],
                            FullName AS [Name],
                            COUNT(*) AS [Total Days],
                            SUM(CASE WHEN Status = 'PRESENT' THEN 1 ELSE 0 END) AS [Present],
                            SUM(CASE WHEN Status = 'LATE' THEN 1 ELSE 0 END) AS [Late],
                            SUM(CASE WHEN Status = 'ABSENT' THEN 1 ELSE 0 END) AS [Absent],
                            CAST((SUM(CASE WHEN Status = 'PRESENT' THEN 1.0 ELSE 0.5 END) / COUNT(*)) * 100 AS DECIMAL(10,2)) AS [Attendance %]
                        FROM {ATTENDANCE_TABLE}
                        WHERE ReferenceType = ? AND MONTH([Date]) = ? AND YEAR([Date]) = ? {(attendanceTenant ? TenantContext.FilterClauseSql() : "")}
                        GROUP BY ReferenceID, FullName
                        ORDER BY [Attendance %] ASC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(type);
                        command.AddPositionalParameter(month);
                        command.AddPositionalParameter(year);
                        if (attendanceTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error loading attendance monthly analysis", ex);
            }
            return table;
        }

        public async Task<bool> SaveAttendanceBatchAsync(IEnumerable<KingdomPrep.Shared.Models.AttendanceRecord> records)
        {
            try
            {
                var touchedMonths = new HashSet<DateTime>();
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var attendanceTenant = await TenantContext.HasSchoolIdColumnAsync(connection, ATTENDANCE_TABLE);
                    foreach (var record in records)
                    {
                        touchedMonths.Add(new DateTime(record.Date.Year, record.Date.Month, 1));
                        // Check if exists
                        var checkQuery = $"SELECT COUNT(*) FROM {ATTENDANCE_TABLE} WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
                        if (attendanceTenant)
                        {
                            checkQuery += TenantContext.FilterClauseSql();
                        }

                        bool exists;
                        using (var checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.AddPositionalParameter(record.ReferenceID);
                            checkCmd.AddPositionalParameter(record.ReferenceType);
                            checkCmd.AddPositionalParameter(record.Date);
                            if (attendanceTenant)
                            {
                                TenantContext.AddSchoolParameter(checkCmd);
                            }

                            exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                        }

                        if (exists)
                        {
                            var updateQuery = $"UPDATE {ATTENDANCE_TABLE} SET [Status] = ?, Remarks = ? WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
                            if (attendanceTenant)
                            {
                                updateQuery += TenantContext.FilterClauseSql();
                            }

                            using (var upCmd = new SqlCommand(updateQuery, connection))
                            {
                                upCmd.AddPositionalParameter(record.Status);
                                upCmd.AddPositionalParameter(record.Remarks ?? "");
                                upCmd.AddPositionalParameter(record.ReferenceID);
                                upCmd.AddPositionalParameter(record.ReferenceType);
                                upCmd.AddPositionalParameter(record.Date);
                                if (attendanceTenant)
                                {
                                    TenantContext.AddSchoolParameter(upCmd);
                                }

                                await upCmd.ExecuteNonQueryAsync();
                            }

                            var attendanceId = await GetAttendanceIdAsync(connection, record, attendanceTenant);
                            await TryRecordSyncUpsertAsync(attendanceId, "Update");
                        }
                        else
                        {
                            var insertQuery = attendanceTenant
                                ? $"INSERT INTO {ATTENDANCE_TABLE} (ReferenceID, ReferenceType, FullName, [Date], [Status], Remarks, SchoolId) VALUES (?, ?, ?, ?, ?, ?, ?)"
                                : $"INSERT INTO {ATTENDANCE_TABLE} (ReferenceID, ReferenceType, FullName, [Date], [Status], Remarks) VALUES (?, ?, ?, ?, ?, ?)";
                            using (var insCmd = new SqlCommand(insertQuery, connection))
                            {
                                insCmd.AddPositionalParameter(record.ReferenceID);
                                insCmd.AddPositionalParameter(record.ReferenceType);
                                insCmd.AddPositionalParameter(record.FullName);
                                insCmd.AddPositionalParameter(record.Date);
                                insCmd.AddPositionalParameter(record.Status);
                                insCmd.AddPositionalParameter(record.Remarks ?? "");
                                if (attendanceTenant)
                                {
                                    TenantContext.AddSchoolParameter(insCmd);
                                }

                                await insCmd.ExecuteNonQueryAsync();
                            }

                            var attendanceId = await GetAttendanceIdAsync(connection, record, attendanceTenant);
                            await TryRecordSyncUpsertAsync(attendanceId, "Insert");
                        }
                    }
                }

                foreach (var month in touchedMonths)
                {
                    DashboardSummaryRepository.RefreshMonthBestEffort(_connectionString, month);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new DataException("Error saving attendance batch", ex);
            }
        }

        public async Task<IEnumerable<string>> GetActiveClassesAsync()
        {
            var classes = new List<string>();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var studentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = "SELECT DISTINCT ClassID FROM Students WHERE 1=1";
                    if (studentTenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    query += " ORDER BY ClassID";
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (studentTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                classes.Add(reader["ClassID"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error getting unique classes from Students table", ex);
            }
            return classes;
        }

        private static async Task<object> GetAttendanceIdAsync(SqlConnection connection, KingdomPrep.Shared.Models.AttendanceRecord record, bool tenant)
        {
            var query = $"SELECT TOP 1 AttendanceID FROM {ATTENDANCE_TABLE} WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
            if (tenant) query += TenantContext.FilterClauseSql();
            query += " ORDER BY AttendanceID DESC";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.AddPositionalParameter(record.ReferenceID);
                cmd.AddPositionalParameter(record.ReferenceType);
                cmd.AddPositionalParameter(record.Date);
                if (tenant) TenantContext.AddSchoolParameter(cmd);
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : value;
            }
        }

        private async Task TryRecordSyncUpsertAsync(object attendanceId, string operation)
        {
            try
            {
                if (attendanceId == null || string.IsNullOrWhiteSpace(Convert.ToString(attendanceId))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(ATTENDANCE_TABLE, "AttendanceID", attendanceId, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Attendance sync capture skipped: " + ex.Message);
            }
        }
    }
}
