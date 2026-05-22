using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly string _connectionString;
        private const string LEAVE_TABLE = "emp_leave";

        public LeaveRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<bool> AddLeaveRequestAsync(Models.LeaveRequest request)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        INSERT INTO {LEAVE_TABLE} 
                        ([employmentID], [name], [department], [position], [Leave_op], [Reasons], [Start_Date], [End_Date], [status]) 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddLeaveParameters(command, request);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error adding leave request", ex);
            }
        }

        public async Task<bool> UpdateLeaveRequestAsync(Models.LeaveRequest request)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        UPDATE {LEAVE_TABLE} 
                        SET [name] = ?, [department] = ?, [position] = ?, [Leave_op] = ?, 
                            [Reasons] = ?, [Start_Date] = ?, [End_Date] = ?, [status] = ? 
                        WHERE [employmentID] = ? AND [Start_Date] = ?";
                    
                    // Note: The original table doesn't seem to have a unique primary key for leave requests, 
                    // using employmentID + StartDate as a composite key for update.
                    
                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddLeaveParameters(command, request);
                        command.Parameters.AddWithValue("?", request.EmployeeID);
                        command.Parameters.AddWithValue("?", request.StartDate);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error updating leave request", ex);
            }
        }

        public async Task<bool> DeleteLeaveRequestAsync(int leaveId)
        {
            // The original table 'emp_leave' does not have an explicit primary key column 'LeaveID' in the schema seen in code,
            // but usually databases have an identity. If not, we'd need another way to delete.
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Assuming there might be an ID if we used a better schema, but for now:
                    var query = $"DELETE FROM {LEAVE_TABLE} WHERE employmentID = ? AND Start_Date = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        // This is a placeholder since we don't have the unique ID here
                        return false; 
                    }
                }
            }
            catch { return false; }
        }

        public async Task<DataTable> GetAllLeaveRequestsTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT 
                            [employmentID] AS [ID], 
                            [name] AS [NAME], 
                            [department] AS [DEPARTMENT], 
                            [position] AS [POSITION], 
                            [Leave_op] AS [LEAVE OPTION], 
                            [Reasons] AS [REASONS], 
                            [Start_Date] AS [START DATE], 
                            [End_Date] AS [END DATE], 
                            [status] AS [STATUS]
                        FROM {LEAVE_TABLE}
                        ORDER BY [Start_Date] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving leave requests table", ex);
            }
            return table;
        }

        public async Task<DataTable> GetLeaveRequestsByStatusAsync(string status)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT 
                            [employmentID] AS [ID], 
                            [name] AS [NAME], 
                            [department] AS [DEPARTMENT], 
                            [position] AS [POSITION], 
                            [Leave_op] AS [LEAVE OPTION], 
                            [Reasons] AS [REASONS], 
                            [Start_Date] AS [START DATE], 
                            [End_Date] AS [END DATE], 
                            [status] AS [STATUS]
                        FROM {LEAVE_TABLE}
                        WHERE UPPER([status]) = ?
                        ORDER BY [Start_Date] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", status.ToUpperInvariant());
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error retrieving {status} leave requests", ex);
            }
            return table;
        }

        public async Task<IEnumerable<Models.LeaveRequest>> GetEmployeeLeaveHistoryAsync(string employeeId)
        {
            var history = new List<Models.LeaveRequest>();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {LEAVE_TABLE} WHERE [employmentID] = ? ORDER BY [Start_Date] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                history.Add(new Models.LeaveRequest
                                {
                                    EmployeeID = reader["employmentID"].ToString(),
                                    EmployeeName = reader["name"].ToString(),
                                    Department = reader["department"].ToString(),
                                    Position = reader["position"].ToString(),
                                    LeaveOption = reader["Leave_op"].ToString(),
                                    Reason = reader["Reasons"].ToString(),
                                    StartDate = (DateTime)reader["Start_Date"],
                                    EndDate = (DateTime)reader["End_Date"],
                                    Status = reader["status"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return history;
        }

        private void AddLeaveParameters(OleDbCommand command, Models.LeaveRequest request)
        {
            command.Parameters.AddWithValue("?", request.EmployeeName ?? "");
            command.Parameters.AddWithValue("?", request.Department ?? "");
            command.Parameters.AddWithValue("?", request.Position ?? "");
            command.Parameters.AddWithValue("?", request.LeaveOption ?? "");
            command.Parameters.AddWithValue("?", request.Reason ?? "");
            command.Parameters.AddWithValue("?", request.StartDate);
            command.Parameters.AddWithValue("?", request.EndDate);
            command.Parameters.AddWithValue("?", request.Status ?? "PENDING");
        }
    }
}
