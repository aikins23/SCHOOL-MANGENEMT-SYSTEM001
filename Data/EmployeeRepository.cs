using System;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<DataTable> GetAllEmployeesAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var adapter = new OleDbDataAdapter("SELECT * FROM Employee", connection))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch { }
            return table;
        }

        public async Task<bool> AddEmployeeAsync(Models.Employee employee)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO Employee (emp_name, emp_id, emp_phone, emp_email, emp_position, emp_salary)
                                VALUES (?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employee.Name ?? "");
                        command.Parameters.AddWithValue("?", employee.Id ?? "");
                        command.Parameters.AddWithValue("?", employee.Phone ?? "");
                        command.Parameters.AddWithValue("?", employee.Email ?? "");
                        command.Parameters.AddWithValue("?", employee.Position ?? "");
                        command.Parameters.AddWithValue("?", employee.Salary);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch { return false; }
        }

        public async Task<bool> UpdateEmployeeAsync(Models.Employee employee)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE Employee SET emp_name = ?, emp_phone = ?, emp_email = ?, emp_position = ?, emp_salary = ?
                                WHERE emp_id = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employee.Name ?? "");
                        command.Parameters.AddWithValue("?", employee.Phone ?? "");
                        command.Parameters.AddWithValue("?", employee.Email ?? "");
                        command.Parameters.AddWithValue("?", employee.Position ?? "");
                        command.Parameters.AddWithValue("?", employee.Salary);
                        command.Parameters.AddWithValue("?", employee.Id ?? "");
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch { return false; }
        }

        public async Task<bool> DeleteEmployeeAsync(string employeeId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM Employee WHERE emp_id = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", employeeId);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch { return false; }
        }
    }
}
