using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

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
                Services.LoggerHelper.LogError($"Error adding leave request for {request?.EmployeeName}", ex);
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

                        if (result > 0 && !string.IsNullOrWhiteSpace(request.Status))
                        {
                            var employeeEmail = await GetEmployeeEmailAsync(request.EmployeeID);
                            if (!string.IsNullOrWhiteSpace(employeeEmail))
                            {
                                await NotificationService.SendLeaveApprovalAsync(
                                    request.EmployeeName, employeeEmail, request.Status,
                                    request.StartDate, request.EndDate, request.LeaveOption ?? "Annual Leave"
                                );
                            }
                        }

                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error updating leave request for {request?.EmployeeName}", ex);
                throw new DataException("Error updating leave request", ex);
            }
        }

        private async Task<string> GetEmployeeEmailAsync(string employeeId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT Email FROM Employee WHERE employmentID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                return "";
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error deleting leave request with ID {leaveId}", ex);
                return false; 
            }
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
                Services.LoggerHelper.LogError("Error retrieving all leave requests table", ex);
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
                Services.LoggerHelper.LogError($"Error retrieving leave requests with status {status}", ex);
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving leave history for employee {employeeId}", ex);
            }
            return history;
        }

        public async Task<int> GetApprovedDaysInRangeAsync(string employeeId, DateTime termStart, DateTime termEnd)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT COALESCE(SUM(DATEDIFF(day, [Start_Date], [End_Date]) + 1), 0)
                        FROM {LEAVE_TABLE}
                        WHERE [employmentID] = ?
                          AND UPPER([status]) = 'APPROVED'
                          AND [Start_Date] >= ?
                          AND [End_Date] <= ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        command.Parameters.AddWithValue("?", termStart);
                        command.Parameters.AddWithValue("?", termEnd);
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error computing approved leave days for {employeeId}", ex);
                return 0;
            }
        }

        public async Task<bool> HasApprovedOverlapAsync(string employeeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT COUNT(*)
                        FROM {LEAVE_TABLE}
                        WHERE [employmentID] = ?
                          AND UPPER([status]) = 'APPROVED'
                          AND [Start_Date] <= ?
                          AND [End_Date] >= ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        command.Parameters.AddWithValue("?", endDate);
                        command.Parameters.AddWithValue("?", startDate);
                        var result = await command.ExecuteScalarAsync();
                        return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error checking leave overlap for {employeeId}", ex);
                return false;
            }
        }

        public async Task<DataTable> GetLeaveBalanceTableAsync(DateTime termStart, DateTime termEnd, int entitlement)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT
                            e.[employmentID] AS [ID],
                            e.[fullName] AS [NAME],
                            e.[position] AS [POSITION],
                            {entitlement} AS [ENTITLEMENT],
                            COALESCE(SUM(DATEDIFF(day, l.[Start_Date], l.[End_Date]) + 1), 0) AS [USED],
                            {entitlement} - COALESCE(SUM(DATEDIFF(day, l.[Start_Date], l.[End_Date]) + 1), 0) AS [REMAINING]
                        FROM Employee e
                        LEFT JOIN {LEAVE_TABLE} l
                            ON l.[employmentID] = e.[employmentID]
                            AND UPPER(l.[status]) = 'APPROVED'
                            AND l.[Start_Date] >= ?
                            AND l.[End_Date] <= ?
                        GROUP BY e.[employmentID], e.[fullName], e.[position]
                        ORDER BY e.[fullName]";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", termStart);
                        command.Parameters.AddWithValue("?", termEnd);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error building leave balance table", ex);
                throw new DataException("Error building leave balance table", ex);
            }
            return table;
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
