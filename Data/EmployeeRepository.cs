using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// OleDb implementation of Employee Repository
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
                            if (reader.Read())
                            {
                                return MapReaderToEmployee(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error retrieving employee with ID {employeeId}", ex);
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
                    var query = $"SELECT * FROM {EMPLOYEE_TABLE} ORDER BY employmentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                employees.Add(MapReaderToEmployee(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving all employees", ex);
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
                    var query = $"SELECT * FROM {EMPLOYEE_TABLE} WHERE department = ? ORDER BY fullName";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", department);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                employees.Add(MapReaderToEmployee(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error retrieving employees for department {department}", ex);
            }

            return employees;
        }

        public async Task<bool> AddAsync(Models.Employee employee)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"
                        INSERT INTO {EMPLOYEE_TABLE} 
                        (fullName, gender, dOB, conatct, department, position, homeTown, 
                         residence, date_of_Emplyment, employment_Mode, employment_Status, 
                         emergency_Contact_Person, emergency_contact, employees_Reviews, 
                         salary, pic) 
                        VALUES 
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddEmployeeParameters(command, employee);

                        var result = await command.ExecuteNonQueryAsync();
                        
                        if (result > 0)
                        {
                            using (var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection))
                            {
                                var id = await idCommand.ExecuteScalarAsync();
                                if (id != null && id != DBNull.Value)
                                {
                                    employee.EmployeeID = id.ToString();
                                }
                            }
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error adding employee", ex);
            }
        }

        public async Task<bool> UpdateAsync(Models.Employee employee)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"
                        UPDATE {EMPLOYEE_TABLE} 
                        SET fullName = ?, gender = ?, dOB = ?, conatct = ?, 
                            department = ?, position = ?, homeTown = ?, residence = ?, 
                            date_of_Emplyment = ?, employment_Mode = ?, employment_Status = ?, 
                            emergency_Contact_Person = ?, emergency_contact = ?, 
                            employees_Reviews = ?, salary = ?, pic = ? 
                        WHERE employmentID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddEmployeeParameters(command, employee);
                        command.Parameters.AddWithValue("?", employee.EmployeeID);

                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error updating employee", ex);
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
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error deleting employee with ID {employeeId}", ex);
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
                throw new DataException($"Error checking if employee exists", ex);
            }
        }

        public async Task<string> GenerateNextEmployeeIdAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT ISNULL(MAX(employmentID), 0) + 1 FROM {EMPLOYEE_TABLE}";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "1";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error generating next employee ID", ex);
            }
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
                            'EMP' + RIGHT('0000' + CAST(employmentID AS VARCHAR), 4) AS [EMPLOYEE ID],
                            fullName AS [FULL NAME],
                            dOB AS [DATE OF BIRTH],
                            gender AS [GENDER],
                            conatct AS [CONTACT],
                            department AS [DEPARTMENT],
                            position AS [POSITION],
                            homeTown AS [HOME TOWN],
                            residence AS [RESIDENCE],
                            date_of_Emplyment AS [DATE OF EMPLOYMENT],
                            employment_Mode AS [EMPLOYMENT MODE],
                            employment_Status AS [EMPLOYMENT STATUS],
                            emergency_Contact_Person AS [EMERGENCY CONTACT PERSON],
                            emergency_contact AS [EMERGENCY CONTACT],
                            employees_Reviews AS [EMPLOYEE REVIEWS],
                            salary AS [SALARY],
                            pic AS [PICTURE]
                        FROM {EMPLOYEE_TABLE}
                        WHERE 1=1";

                    if (!string.IsNullOrWhiteSpace(filterId)) query += " AND employmentID = ?";
                    if (!string.IsNullOrWhiteSpace(filterDepartment)) query += " AND department = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(filterId)) command.Parameters.AddWithValue("?", filterId);
                        if (!string.IsNullOrWhiteSpace(filterDepartment)) command.Parameters.AddWithValue("?", filterDepartment);

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving employee table", ex);
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
                    using (var transaction = connection.BeginTransaction())
                    {
                        var employee = await GetByIdAsync(employeeId);
                        if (employee == null) return false;

                        var insertQuery = @"
                            INSERT INTO Rolled_Out_Employees
                            (employmentID, fullName, gender, dOB, conatct, department, position, homeTown, residence, date_of_Emplyment, employment_Mode, employment_Status, emergency_Contact_Person, emergency_contact, Employees_Reviews, salary, pic, [DATE])
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        
                        using (var insCmd = new OleDbCommand(insertQuery, connection, transaction))
                        {
                            insCmd.Parameters.AddWithValue("?", employee.EmployeeID);
                            insCmd.Parameters.AddWithValue("?", employee.FullName ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Gender ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.DateOfBirth);
                            insCmd.Parameters.AddWithValue("?", employee.Contact ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Department ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Position ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.HomeTown ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Residence ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.EmploymentDate);
                            insCmd.Parameters.AddWithValue("?", employee.EmploymentMode ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.EmploymentStatus ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.EmergencyContactPerson ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.EmergencyContact ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.PerformanceReview ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Salary);
                            insCmd.Parameters.AddWithValue("?", employee.ProfilePhoto ?? new byte[0]);
                            insCmd.Parameters.AddWithValue("?", terminationDate);
                            await insCmd.ExecuteNonQueryAsync();
                        }

                        var deleteQuery = $"DELETE FROM {EMPLOYEE_TABLE} WHERE employmentID = ?";
                        using (var delCmd = new OleDbCommand(deleteQuery, connection, transaction))
                        {
                            delCmd.Parameters.AddWithValue("?", employeeId);
                            await delCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error terminating employee {employeeId}", ex);
            }
        }

        private Models.Employee MapReaderToEmployee(IDataReader reader)
        {
            return new Models.Employee
            {
                EmployeeID = reader["employmentID"].ToString(),
                FullName = reader["fullName"].ToString(),
                Gender = reader["gender"].ToString(),
                DateOfBirth = (DateTime)reader["dOB"],
                Contact = reader["conatct"].ToString(),
                Department = reader["department"].ToString(),
                Position = reader["position"].ToString(),
                HomeTown = reader["homeTown"].ToString(),
                Residence = reader["residence"].ToString(),
                EmploymentDate = (DateTime)reader["date_of_Emplyment"],
                EmploymentMode = reader["employment_Mode"].ToString(),
                EmploymentStatus = reader["employment_Status"].ToString(),
                EmergencyContactPerson = reader["emergency_Contact_Person"].ToString(),
                EmergencyContact = reader["emergency_contact"].ToString(),
                PerformanceReview = reader["employees_Reviews"].ToString(),
                Salary = Convert.ToDecimal(reader["salary"]),
                ProfilePhoto = reader["pic"] != DBNull.Value ? (byte[])reader["pic"] : null
            };
        }

        private void AddEmployeeParameters(OleDbCommand command, Models.Employee employee)
        {
            command.Parameters.AddWithValue("?", employee.FullName ?? "");
            command.Parameters.AddWithValue("?", employee.Gender ?? "");
            command.Parameters.AddWithValue("?", employee.DateOfBirth);
            command.Parameters.AddWithValue("?", employee.Contact ?? "");
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
