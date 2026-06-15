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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    var query = $"SELECT * FROM {EMPLOYEE_TABLE} WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    query += " ORDER BY employmentID";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", department);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    var columns = @"fullName, gender, dOB, conatct, email, department, position, homeTown, residence,
                         date_of_Emplyment, employment_Mode, employment_Status, emergency_Contact_Person,
                         emergency_contact, Employees_Reviews, salary, pic";
                    var values = "?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?";
                    if (tenant)
                    {
                        columns += ", SchoolId";
                        values += ", ?";
                    }

                    var query = $@"
                        INSERT INTO {EMPLOYEE_TABLE}
                        ({columns})
                        VALUES ({values})";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddEmployeeParameters(command, employee);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddEmployeeParameters(command, employee);
                        command.Parameters.AddWithValue("?", employee.EmployeeID ?? "");
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }
                        
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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                    var query = $"SELECT ISNULL(MAX(employmentID), 0) + 1 FROM {EMPLOYEE_TABLE}";
                    if (tenant)
                    {
                        query += " WHERE SchoolId = ?";
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
                        var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                        if (tenant)
                        {
                            query += TenantContext.FilterClause();
                            TenantContext.AddSchoolParameter(command);
                        }

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
                    using (var transaction = connection.BeginTransaction())
                    {
                        var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                        var archiveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Employees");
                        var employee = await GetByIdAsync(employeeId);
                        if (employee == null) return false;

                        // 1. Insert into archive
                        var insColumns = "employmentID, fullName, gender, DOB, homeTown, residence, position, department, mobile, email, [date]";
                        var insValues = "?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?";
                        if (archiveTenant)
                        {
                            insColumns += ", SchoolId";
                            insValues += ", ?";
                        }

                        var insSql = $@"INSERT INTO Rolled_Out_Employees 
                                       ({insColumns})
                                       VALUES ({insValues})";
                        using (var insCmd = new OleDbCommand(insSql, connection, transaction))
                        {
                            insCmd.Parameters.AddWithValue("?", employee.EmployeeID);
                            insCmd.Parameters.AddWithValue("?", employee.FullName ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Gender ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.DateOfBirth);
                            insCmd.Parameters.AddWithValue("?", employee.HomeTown ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Residence ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Position ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Department ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Contact ?? "");
                            insCmd.Parameters.AddWithValue("?", employee.Email ?? "");
                            insCmd.Parameters.AddWithValue("?", terminationDate);
                            if (archiveTenant)
                            {
                                TenantContext.AddSchoolParameter(insCmd);
                            }

                            await insCmd.ExecuteNonQueryAsync();
                        }

                        // 2. Delete from main
                        var delSql = "DELETE FROM Employee WHERE employmentID = ?";
                        if (employeeTenant)
                        {
                            delSql += TenantContext.FilterClause();
                        }

                        using (var delCmd = new OleDbCommand(delSql, connection, transaction))
                        {
                            delCmd.Parameters.AddWithValue("?", employeeId);
                            if (employeeTenant)
                            {
                                TenantContext.AddSchoolParameter(delCmd);
                            }

                            await delCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to terminate employee: {employeeId}", ex);
                return false;
            }
        }

        public async Task<bool> RestoreAsync(string employeeId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(connection, EMPLOYEE_TABLE);
                            var archiveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Employees");
                            // 1. Move back to main table
                            var targetColumns = "fullName, gender, dOB, conatct, email, department, position, homeTown, residence";
                            var selectColumns = "fullName, gender, DOB, mobile, email, department, position, homeTown, residence";
                            if (employeeTenant)
                            {
                                targetColumns += ", SchoolId";
                                selectColumns += ", ?";
                            }

                            var moveSql = $@"INSERT INTO Employee 
                                            ({targetColumns})
                                            SELECT {selectColumns}
                                            FROM Rolled_Out_Employees WHERE employmentID = ?";
                            if (archiveTenant)
                            {
                                moveSql += TenantContext.FilterClause();
                            }

                            using (var moveCmd = new OleDbCommand(moveSql, connection, transaction))
                            {
                                if (employeeTenant)
                                {
                                    TenantContext.AddSchoolParameter(moveCmd);
                                }

                                moveCmd.Parameters.AddWithValue("?", employeeId);
                                if (archiveTenant)
                                {
                                    TenantContext.AddSchoolParameter(moveCmd);
                                }

                                int rows = await moveCmd.ExecuteNonQueryAsync();
                                if (rows == 0) return false;
                            }

                            // 2. Delete from archive
                            var delSql = "DELETE FROM Rolled_Out_Employees WHERE employmentID = ?";
                            if (archiveTenant)
                            {
                                delSql += TenantContext.FilterClause();
                            }

                            using (var delCmd = new OleDbCommand(delSql, connection, transaction))
                            {
                                delCmd.Parameters.AddWithValue("?", employeeId);
                                if (archiveTenant)
                                {
                                    TenantContext.AddSchoolParameter(delCmd);
                                }

                                await delCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Error restoring employee {employeeId}", ex);
                return false;
            }
        }

        public async Task<DataTable> GetRolledOutAsTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var archiveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Employees");
                    var query = @"SELECT employmentID AS ID, fullName AS [FULL NAME], 
                                         department AS DEPARTMENT, position AS POSITION, [date] AS [EXIT DATE] 
                                  FROM Rolled_Out_Employees 
                                  WHERE 1=1";
                    if (archiveTenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    query += " ORDER BY [date] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (archiveTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
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
                Services.LoggerHelper.LogError("Error retrieving rolled out employees table", ex);
            }
            return table;
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
