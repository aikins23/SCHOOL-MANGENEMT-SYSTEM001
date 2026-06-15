using System;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<DataTable> GetAllUsersAsTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool userTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    bool employeeTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Employee");
                    // Join with Employee to show Full Name if linked
                    var query = @"
                        SELECT 
                            u.Username, 
                            u.User_Type AS [Role], 
                            e.fullName AS [Linked Employee],
                            u.EmploymentID AS [Link ID]
                        FROM Users u
                        LEFT JOIN Employee e ON u.EmploymentID = e.employmentID";
                    if (employeeTenant)
                    {
                        query += " AND e.SchoolId = ?";
                    }
                    query += @"
                        WHERE 1=1";
                    if (userTenant)
                    {
                        query += TenantContext.FilterClause("u");
                    }
                    query += @"
                        ORDER BY u.Username";

                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        if (employeeTenant) TenantContext.AddSchoolParameter(command);
                        if (userTenant) TenantContext.AddSchoolParameter(command);
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving users table", ex);
            }
            return table;
        }

        public async Task<bool> DeleteUserAsync(string username)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    var query = "DELETE FROM Users WHERE Username = ?";
                    if (tenant) query += TenantContext.FilterClause();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", username);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        return await command.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error deleting user {username}", ex);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string username, string hashedPassword)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    var query = "UPDATE Users SET Password = ? WHERE Username = ?";
                    if (tenant) query += TenantContext.FilterClause();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", hashedPassword);
                        command.Parameters.AddWithValue("?", username);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        return await command.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error resetting password for {username}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateUserRoleAsync(string username, string newRole)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    var query = "UPDATE Users SET User_Type = ? WHERE Username = ?";
                    if (tenant) query += TenantContext.FilterClause();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", newRole);
                        command.Parameters.AddWithValue("?", username);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        return await command.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error updating role for {username}", ex);
                return false;
            }
        }
    }
}
