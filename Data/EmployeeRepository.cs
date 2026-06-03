using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// OleDb implementation of Employee Repository - Updated for OLE DB compatibility and correct schema
    /// Matches database typos from legacy schema: employmentID, conatct, date_of_Emplyment, Employees_Reviews, pic
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;
        private const string EMPLOYEE_TABLE = "Employee";

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
                    var query = $"SELECT * FROM {EMPLOYEE_TABLE} WHERE employmentID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapReaderToEmployee(reader);
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
                    using (var command = new OleDbCommand($"SELECT * FROM {EMPLOYEE_TABLE} ORDER BY employmentID", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                employees.Add(MapReaderToEmployee(reader));
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
                    var query = $"SELECT * FROM {EMPLOYEE_TABLE} WHERE department = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", department);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                employees.Add(MapReaderToEmployee(reader));
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
                    // employmentID is INT IDENTITY — never include it in INSERT.
                    var query = $@"
                        INSERT INTO {EMPLOYEE_TABLE}
                        (fullName, gender, dOB, conatct, email, department, position, homeTown, residence,
                         date_of_Emplyment, employment_Mode, employment_Status, emergency_Contact_Person,
                         emergency_contact, Employees_Reviews, salary, pic)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddEmployeeParameters(command, employee);

                        var rows = await command.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            // Retrieve the auto-generated identity value
                            using (var idCmd = new OleDbCommand("SELECT @@IDENTITY", connection))
                            {
                                var newId = await idCmd.ExecuteScalarAsync();
                                if (newId != null && newId != DBNull.Value)
                                    employee.EmployeeID = newId.ToString();
                            }
                            return true;
                        }
                        return false;
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
                    var query = $@"
                        UPDATE {EMPLOYEE_TABLE} 
                        SET fullName = ?, gender = ?, dOB = ?, conatct = ?, email = ?, department = ?, 
                            position = ?, homeTown = ?, residence = ?, date_of_Emplyment = ?, 
                            employment_Mode = ?, employment_Status = ?, emergency_Contact_Person = ?, 
                            emergency_contact = ?, Employees_Reviews = ?, salary = ?, pic = ?
                        WHERE employmentID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddEmployeeParameters(command, employee);
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
                    var query = $"DELETE FROM {EMPLOYEE_TABLE} WHERE employmentID = ?";
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
                    var query = $"SELECT COUNT(*) FROM {EMPLOYEE_TABLE} WHERE employmentID = ?";
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
            // employmentID is INT IDENTITY — the DB assigns it on INSERT.
            // We return a best-guess preview for display only; the real value
            // comes from SELECT @@IDENTITY after the INSERT.
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT ISNULL(MAX(employmentID), 0) + 1 FROM {EMPLOYEE_TABLE}";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                            return result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to preview next employee ID", ex);
            }
            return "(auto)";
        }

        public async Task<DataTable> GetAsTableAsync(string filterId = null, string filterDepartment = null)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT 
                            employmentID AS [ID], 
                            fullName AS [Full Name], 
                            department AS [Department], 
                            position AS [Position], 
                            employment_Status AS [Status],
                            conatct AS [Contact]
                        FROM {EMPLOYEE_TABLE} 
                        WHERE 1=1";

                    using (var command = new OleDbCommand())
                    {
                        command.Connection = connection;
                        if (!string.IsNullOrEmpty(filterId))
                        {
                            command.Parameters.AddWithValue("?", "%" + filterId + "%");
                            query += " AND employmentID LIKE ?";
                        }

                        if (!string.IsNullOrEmpty(filterDepartment))
                        {
                            command.Parameters.AddWithValue("?", filterDepartment);
                            query += " AND department = ?";
                        }

                        command.CommandText = query;
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to retrieve employees as table with filters", ex);
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
                    // Move to Rolled_Out_Employees or just update status?
                    // The modern requirement seems to be just updating status here
                    var query = $"UPDATE {EMPLOYEE_TABLE} SET employment_Status = 'Terminated' WHERE employmentID = ?";
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

        private Models.Employee MapReaderToEmployee(IDataReader reader)
        {
            return new Models.Employee
            {
                EmployeeID = reader["employmentID"].ToString(),
                FullName = reader["fullName"].ToString(),
                Gender = reader["gender"]?.ToString() ?? "",
                DateOfBirth = reader["dOB"] != DBNull.Value ? (DateTime)reader["dOB"] : DateTime.MinValue,
                Contact = reader["conatct"]?.ToString() ?? "",
                Email = reader["email"]?.ToString() ?? "",
                Department = reader["department"]?.ToString() ?? "",
                Position = reader["position"]?.ToString() ?? "",
                HomeTown = reader["homeTown"]?.ToString() ?? "",
                Residence = reader["residence"]?.ToString() ?? "",
                EmploymentDate = reader["date_of_Emplyment"] != DBNull.Value ? (DateTime)reader["date_of_Emplyment"] : DateTime.MinValue,
                EmploymentMode = reader["employment_Mode"]?.ToString() ?? "",
                EmploymentStatus = reader["employment_Status"]?.ToString() ?? "",
                EmergencyContactPerson = reader["emergency_Contact_Person"]?.ToString() ?? "",
                EmergencyContact = reader["emergency_contact"]?.ToString() ?? "",
                PerformanceReview = reader["Employees_Reviews"]?.ToString() ?? "",
                Salary = reader["salary"] != DBNull.Value ? Convert.ToDecimal(reader["salary"]) : 0m,
                ProfilePhoto = reader["pic"] != DBNull.Value ? (byte[])reader["pic"] : null
            };
        }

        private void AddEmployeeParameters(OleDbCommand command, Models.Employee employee)
        {
            command.Parameters.AddWithValue("?", employee.FullName ?? "");
            command.Parameters.AddWithValue("?", employee.Gender ?? "");
            command.Parameters.AddWithValue("?", employee.DateOfBirth);
            command.Parameters.AddWithValue("?", employee.Contact ?? "");
            command.Parameters.AddWithValue("?", employee.Email ?? "");
            command.Parameters.AddWithValue("?", employee.Department ?? "");
            command.Parameters.AddWithValue("?", employee.Position ?? "");
            command.Parameters.AddWithValue("?", employee.HomeTown ?? "");
            command.Parameters.AddWithValue("?", employee.Residence ?? "");
            command.Parameters.AddWithValue("?", employee.EmploymentDate);
            command.Parameters.AddWithValue("?", employee.EmploymentMode ?? "");
            command.Parameters.AddWithValue("?", employee.EmploymentStatus ?? "");
            command.Parameters.AddWithValue("?", employee.EmergencyContactPerson ?? "");
            command.Parameters.AddWithValue("?", employee.EmergencyContact ?? "");
            command.Parameters.AddWithValue("?", employee.PerformanceReview ?? "");
            command.Parameters.AddWithValue("?", employee.Salary);
            command.Parameters.AddWithValue("?", employee.ProfilePhoto ?? new byte[0]);
        }
    }
}
