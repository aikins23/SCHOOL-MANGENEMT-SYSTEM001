using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly string _connectionString;
        private const string ATTENDANCE_TABLE = "Attendance";

        public AttendanceRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task EnsureTableExistsAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM sys.tables WHERE name = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", ATTENDANCE_TABLE);
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
                            using (var createCmd = new OleDbCommand(createSql, connection))
                            {
                                await createCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch { /* Best effort schema management */ }
        }

        public async Task<DataTable> GetTargetListAsync(string type, string classId, DateTime date)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query;
                    if (type == "STUDENT")
                    {
                        string classFilter = (classId == "All Classes") ? "" : " AND s.ClassID = ?";
                        query = $@"
                            SELECT s.StudentID AS [ID], s.FirstName + ' ' + s.LastName AS [Full Name], s.ClassID AS [Class], 
                                   COALESCE(a.Status, 'PRESENT') AS [Status], COALESCE(a.Remarks, '') AS [Remarks]
                            FROM Students s LEFT JOIN {ATTENDANCE_TABLE} a ON s.StudentID = a.ReferenceID 
                                 AND a.ReferenceType = 'STUDENT' AND a.[Date] = ?
                            WHERE 1=1 {classFilter}
                            ORDER BY s.ClassID, s.FirstName";
                    }
                    else
                    {
                        query = $@"
                            SELECT e.employmentID AS [ID], e.fullName AS [Full Name], e.position AS [Position], 
                                   COALESCE(a.Status, 'PRESENT') AS [Status], COALESCE(a.Remarks, '') AS [Remarks]
                            FROM Employee e LEFT JOIN {ATTENDANCE_TABLE} a ON e.employmentID = a.ReferenceID 
                                 AND a.ReferenceType = 'STAFF' AND a.[Date] = ?
                            ORDER BY e.fullName";
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", date);
                        if (type == "STUDENT" && classId != "All Classes")
                        {
                            command.Parameters.AddWithValue("?", classId);
                        }
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error loading attendance target list", ex);
            }
            return table;
        }

        public async Task<DataTable> GetMonthlyAnalysisAsync(string type, int month, int year)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
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
                        WHERE ReferenceType = ? AND MONTH([Date]) = ? AND YEAR([Date]) = ?
                        GROUP BY ReferenceID, FullName
                        ORDER BY [Attendance %] ASC";
                    
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", type);
                        command.Parameters.AddWithValue("?", month);
                        command.Parameters.AddWithValue("?", year);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
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

        public async Task<bool> SaveAttendanceBatchAsync(IEnumerable<Models.AttendanceRecord> records)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    foreach (var record in records)
                    {
                        // Check if exists
                        var checkQuery = $"SELECT COUNT(*) FROM {ATTENDANCE_TABLE} WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
                        bool exists;
                        using (var checkCmd = new OleDbCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("?", record.ReferenceID);
                            checkCmd.Parameters.AddWithValue("?", record.ReferenceType);
                            checkCmd.Parameters.AddWithValue("?", record.Date);
                            exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                        }

                        if (exists)
                        {
                            var updateQuery = $"UPDATE {ATTENDANCE_TABLE} SET [Status] = ?, Remarks = ? WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
                            using (var upCmd = new OleDbCommand(updateQuery, connection))
                            {
                                upCmd.Parameters.AddWithValue("?", record.Status);
                                upCmd.Parameters.AddWithValue("?", record.Remarks ?? "");
                                upCmd.Parameters.AddWithValue("?", record.ReferenceID);
                                upCmd.Parameters.AddWithValue("?", record.ReferenceType);
                                upCmd.Parameters.AddWithValue("?", record.Date);
                                await upCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            var insertQuery = $"INSERT INTO {ATTENDANCE_TABLE} (ReferenceID, ReferenceType, FullName, [Date], [Status], Remarks) VALUES (?, ?, ?, ?, ?, ?)";
                            using (var insCmd = new OleDbCommand(insertQuery, connection))
                            {
                                insCmd.Parameters.AddWithValue("?", record.ReferenceID);
                                insCmd.Parameters.AddWithValue("?", record.ReferenceType);
                                insCmd.Parameters.AddWithValue("?", record.FullName);
                                insCmd.Parameters.AddWithValue("?", record.Date);
                                insCmd.Parameters.AddWithValue("?", record.Status);
                                insCmd.Parameters.AddWithValue("?", record.Remarks ?? "");
                                await insCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    return true;
                }
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT DISTINCT ClassID FROM Students ORDER BY ClassID";
                    using (var command = new OleDbCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            classes.Add(reader["ClassID"].ToString());
                        }
                    }
                }
            }
            catch { }
            return classes;
        }
    }
}
