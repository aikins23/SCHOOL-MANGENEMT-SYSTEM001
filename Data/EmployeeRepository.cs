using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<Models.Employee> GetByIdAsync(string employeeId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT * FROM Employee WHERE EmployeeID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Models.Employee
                                {
                                    EmployeeID = reader["EmployeeID"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Contact = reader["Contact"]?.ToString() ?? "",
                                    Department = reader["Department"]?.ToString() ?? "",
                                    Position = reader["Position"]?.ToString() ?? "",
                                    Salary = Convert.ToDecimal(reader["Salary"] ?? 0)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to retrieve employee by ID: {employeeId}", ex);
            }
            return null;
        }

        public async Task<IEnumerable<Models.Employee>> GetAllAsync()
        {
            var employees = new List<Models.Employee>();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand("SELECT * FROM Employee", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                employees.Add(new Models.Employee
                                {
                                    EmployeeID = reader["EmployeeID"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Contact = reader["Contact"]?.ToString() ?? "",
                                    Department = reader["Department"]?.ToString() ?? "",
                                    Position = reader["Position"]?.ToString() ?? "",
                                    Salary = Convert.ToDecimal(reader["Salary"] ?? 0)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to retrieve all employees", ex);
            }
            return employees;
        }

        public async Task<IEnumerable<Models.Employee>> GetByDepartmentAsync(string department)
        {
            var employees = new List<Models.Employee>();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT * FROM Employee WHERE Department = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", department);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                employees.Add(new Models.Employee
                                {
                                    EmployeeID = reader["EmployeeID"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Contact = reader["Contact"]?.ToString() ?? "",
                                    Department = reader["Department"]?.ToString() ?? "",
                                    Position = reader["Position"]?.ToString() ?? "",
                                    Salary = Convert.ToDecimal(reader["Salary"] ?? 0)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to retrieve employees by department: {department}", ex);
            }
            return employees;
        }

        public async Task<bool> AddAsync(Models.Employee employee)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO Employee (EmployeeID, FullName, Contact, Department, Position, Salary)
                                VALUES (?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employee.EmployeeID ?? "");
                        command.Parameters.AddWithValue("?", employee.FullName ?? "");
                        command.Parameters.AddWithValue("?", employee.Contact ?? "");
                        command.Parameters.AddWithValue("?", employee.Department ?? "");
                        command.Parameters.AddWithValue("?", employee.Position ?? "");
                        command.Parameters.AddWithValue("?", employee.Salary);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to add employee: {employee?.FullName ?? "unknown"}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Models.Employee employee)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE Employee SET FullName = ?, Contact = ?, Department = ?, Position = ?, Salary = ?
                                WHERE EmployeeID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employee.FullName ?? "");
                        command.Parameters.AddWithValue("?", employee.Contact ?? "");
                        command.Parameters.AddWithValue("?", employee.Department ?? "");
                        command.Parameters.AddWithValue("?", employee.Position ?? "");
                        command.Parameters.AddWithValue("?", employee.Salary);
                        command.Parameters.AddWithValue("?", employee.EmployeeID ?? "");
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to update employee: {employee?.EmployeeID ?? "unknown"}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string employeeId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM Employee WHERE EmployeeID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to delete employee: {employeeId}", ex);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string employeeId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM Employee WHERE EmployeeID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to check if employee exists: {employeeId}", ex);
                return false;
            }
        }

        public async Task<string> GenerateNextEmployeeIdAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT MAX(EmployeeID) FROM Employee";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        if (result == null || result == DBNull.Value)
                            return "EMP001";

                        var lastId = result.ToString();
                        if (int.TryParse(lastId.Substring(3), out int num))
                        {
                            return $"EMP{(num + 1):D3}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to generate next employee ID, using default", ex);
            }
            return "EMP001";
        }

        public async Task<DataTable> GetAsTableAsync(string filterId = null, string filterDepartment = null)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT * FROM Employee WHERE 1=1";
                    if (!string.IsNullOrEmpty(filterId))
                        query += " AND EmployeeID LIKE '%" + filterId.Replace("'", "''") + "%'";
                    if (!string.IsNullOrEmpty(filterDepartment))
                        query += " AND Department = '" + filterDepartment.Replace("'", "''") + "'";

                    using (var adapter = new OleDbDataAdapter(query, connection))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to retrieve employees as table with filters (ID: {filterId}, Department: {filterDepartment})", ex);
            }
            return table;
        }

        public async Task<bool> TerminateAsync(string employeeId, DateTime terminationDate)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "UPDATE Employee SET EmploymentStatus = 'Terminated' WHERE EmployeeID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to terminate employee: {employeeId}", ex);
                return false;
            }
        }
    }
}
