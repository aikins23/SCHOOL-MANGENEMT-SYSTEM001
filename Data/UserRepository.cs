using KingdomPrep.Shared.Models;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
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
                        query += " AND e.SchoolId = @EmployeeSchoolId";
                    }
                    query += @"
                        WHERE 1=1";
                    if (userTenant)
                    {
                        query += TenantContext.FilterClauseSql("u");
                    }
                    query += @"
                        ORDER BY u.Username";

                    using (var command = new SqlCommand(query, connection))
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        if (employeeTenant) command.Parameters.AddWithValue("@EmployeeSchoolId", TenantContext.RequireSchoolId());
                        if (userTenant) TenantContext.AddSchoolParameter(command);
                        await Task.Run(() => adapter.Fill(table));
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
                await TryRecordUserDeleteAsync(username);
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    var query = "DELETE FROM Users WHERE Username = ?";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(username);
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    var query = "UPDATE Users SET Password = ? WHERE Username = ?";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(hashedPassword);
                        command.AddPositionalParameter(username);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0) await TryRecordUserUpsertAsync(username, "Update");
                        return result > 0;
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Users");
                    var query = "UPDATE Users SET User_Type = ? WHERE Username = ?";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(newRole);
                        command.AddPositionalParameter(username);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0) await TryRecordUserUpsertAsync(username, "Update");
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error updating role for {username}", ex);
                return false;
            }
        }

        private async Task TryRecordUserUpsertAsync(string username, string operation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync("Users", "Username", username, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("User sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordUserDeleteAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync("Users", "Username", username);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("User delete sync capture skipped: " + ex.Message);
            }
        }
    }
}
