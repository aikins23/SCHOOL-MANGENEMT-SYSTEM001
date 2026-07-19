using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly string _connectionString;

        public PermissionRepository() : this(AppConfig.ConnectionString) { }

        public PermissionRepository(string connectionString)
        {
            _connectionString = SqlCommandExtensions.StripProvider(connectionString);
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            var list = new List<Permission>();
            const string sql = @"SELECT PermissionId, Code, Name, Description, Module, Category, IsSystemPermission, CreatedDate
                                 FROM Permissions ORDER BY Module, Category, Name";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(MapPermission(reader));
                }
            }
            return list;
        }

        public async Task<List<Permission>> GetPermissionsByModuleAsync(string module)
        {
            var list = new List<Permission>();
            const string sql = @"SELECT PermissionId, Code, Name, Description, Module, Category, IsSystemPermission, CreatedDate
                                 FROM Permissions WHERE Module = @module ORDER BY Category, Name";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@module", module);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            list.Add(MapPermission(reader));
                    }
                }
            }
            return list;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            var list = new List<Role>();
            const string sql = @"SELECT RoleId, Name, Description, SchoolId, IsSystemRole, IsActive,
                                        CreatedDate, ModifiedDate, CreatedBy
                                 FROM Roles WHERE IsActive = 1 ORDER BY IsSystemRole DESC, Name";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(MapRole(reader));
                }
            }
            return list;
        }

        public async Task<Role> GetRoleByIdAsync(int roleId)
        {
            const string sql = @"SELECT RoleId, Name, Description, SchoolId, IsSystemRole, IsActive,
                                        CreatedDate, ModifiedDate, CreatedBy
                                 FROM Roles WHERE RoleId = @id";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", roleId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) return MapRole(reader);
                    }
                }
            }
            return null;
        }

        public async Task<Role> GetRoleByNameAsync(string name)
        {
            const string sql = @"SELECT TOP 1 RoleId, Name, Description, SchoolId, IsSystemRole, IsActive,
                                              CreatedDate, ModifiedDate, CreatedBy
                                 FROM Roles WHERE Name = @name AND IsActive = 1";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) return MapRole(reader);
                    }
                }
            }
            return null;
        }

        public async Task<int> CreateRoleAsync(Role role)
        {
            const string sql = @"INSERT INTO Roles (Name, Description, SchoolId, IsSystemRole, IsActive, CreatedBy)
                                 VALUES (@name, @desc, @school, 0, 1, @by);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", role.Name);
                    cmd.Parameters.AddWithValue("@desc", (object)role.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@school", (object)role.SchoolId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@by", (object)role.CreatedBy ?? DBNull.Value);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<bool> UpdateRoleAsync(Role role)
        {
            const string sql = @"UPDATE Roles SET Name=@name, Description=@desc, IsActive=@active,
                                                 ModifiedDate=GETDATE()
                                 WHERE RoleId=@id AND IsSystemRole = 0";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", role.Name);
                    cmd.Parameters.AddWithValue("@desc", (object)role.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@active", role.IsActive);
                    cmd.Parameters.AddWithValue("@id", role.RoleId);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            const string sql = @"UPDATE Roles SET IsActive = 0, ModifiedDate = GETDATE()
                                 WHERE RoleId = @id AND IsSystemRole = 0";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", roleId);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
        {
            var list = new List<Permission>();
            const string sql = @"SELECT p.PermissionId, p.Code, p.Name, p.Description, p.Module, p.Category,
                                        p.IsSystemPermission, p.CreatedDate
                                 FROM Permissions p
                                 INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
                                 WHERE rp.RoleId = @id AND rp.IsGranted = 1
                                 ORDER BY p.Module, p.Category, p.Name";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", roleId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            list.Add(MapPermission(reader));
                    }
                }
            }
            return list;
        }

        public async Task<bool> GrantPermissionAsync(int roleId, int permissionId, string assignedBy)
        {
            const string sql = @"
IF EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId=@r AND PermissionId=@p)
    UPDATE RolePermissions SET IsGranted = 1, AssignedDate = GETDATE(), AssignedBy = @by
    WHERE RoleId=@r AND PermissionId=@p
ELSE
    INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted, AssignedBy)
    VALUES (@r, @p, 1, @by)";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@r", roleId);
                    cmd.Parameters.AddWithValue("@p", permissionId);
                    cmd.Parameters.AddWithValue("@by", (object)assignedBy ?? DBNull.Value);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> RevokePermissionAsync(int roleId, int permissionId)
        {
            const string sql = @"DELETE FROM RolePermissions WHERE RoleId=@r AND PermissionId=@p";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@r", roleId);
                    cmd.Parameters.AddWithValue("@p", permissionId);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> SetRolePermissionsAsync(int roleId, List<int> permissionIds, string assignedBy)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new SqlCommand("DELETE FROM RolePermissions WHERE RoleId=@r", conn, tx))
                        {
                            del.Parameters.AddWithValue("@r", roleId);
                            await del.ExecuteNonQueryAsync();
                        }
                        foreach (var pid in permissionIds)
                        {
                            using (var ins = new SqlCommand(
                                "INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted, AssignedBy) VALUES (@r,@p,1,@by)",
                                conn, tx))
                            {
                                ins.Parameters.AddWithValue("@r", roleId);
                                ins.Parameters.AddWithValue("@p", pid);
                                ins.Parameters.AddWithValue("@by", (object)assignedBy ?? DBNull.Value);
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<List<Role>> GetUserRolesAsync(string username)
        {
            var list = new List<Role>();
            const string sql = @"SELECT r.RoleId, r.Name, r.Description, r.SchoolId, r.IsSystemRole,
                                        r.IsActive, r.CreatedDate, r.ModifiedDate, r.CreatedBy
                                 FROM Roles r
                                 INNER JOIN UserRoleAssignments ura ON r.RoleId = ura.RoleId
                                 WHERE ura.Username = @u AND ura.IsActive = 1 AND r.IsActive = 1";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            list.Add(MapRole(reader));
                    }
                }
            }
            return list;
        }

        public async Task<List<string>> GetUserPermissionCodesAsync(string username)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const string sql = @"SELECT DISTINCT p.Code
                                 FROM Permissions p
                                 INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
                                 INNER JOIN UserRoleAssignments ura ON rp.RoleId = ura.RoleId
                                 WHERE ura.Username = @u AND ura.IsActive = 1 AND rp.IsGranted = 1";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            set.Add(reader.GetString(0));
                    }
                }
            }
            return new List<string>(set);
        }

        public async Task<bool> AssignUserToRoleAsync(string username, int roleId, string assignedBy)
        {
            const string sql = @"
IF EXISTS (SELECT 1 FROM UserRoleAssignments WHERE Username=@u AND RoleId=@r)
    UPDATE UserRoleAssignments SET IsActive=1, AssignedDate=GETDATE(), AssignedBy=@by
    WHERE Username=@u AND RoleId=@r
ELSE
    INSERT INTO UserRoleAssignments (Username, RoleId, AssignedBy)
    VALUES (@u, @r, @by)";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@r", roleId);
                    cmd.Parameters.AddWithValue("@by", (object)assignedBy ?? DBNull.Value);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(string username, int roleId)
        {
            const string sql = @"UPDATE UserRoleAssignments SET IsActive=0
                                 WHERE Username=@u AND RoleId=@r";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@r", roleId);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> UserHasPermissionAsync(string username, string permissionCode)
        {
            const string sql = @"SELECT COUNT(1) FROM Permissions p
                                 INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
                                 INNER JOIN UserRoleAssignments ura ON rp.RoleId = ura.RoleId
                                 WHERE ura.Username = @u AND ura.IsActive = 1
                                   AND rp.IsGranted = 1 AND p.Code = @code";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@code", permissionCode);
                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        private static Permission MapPermission(SqlDataReader r)
        {
            return new Permission
            {
                PermissionId = r.GetInt32(0),
                Code = r.GetString(1),
                Name = r.GetString(2),
                Description = r.IsDBNull(3) ? null : r.GetString(3),
                Module = r.GetString(4),
                Category = r.GetString(5),
                IsSystemPermission = r.GetBoolean(6),
                CreatedDate = r.GetDateTime(7)
            };
        }

        private static Role MapRole(SqlDataReader r)
        {
            return new Role
            {
                RoleId = r.GetInt32(0),
                Name = r.GetString(1),
                Description = r.IsDBNull(2) ? null : r.GetString(2),
                SchoolId = r.IsDBNull(3) ? (int?)null : r.GetInt32(3),
                IsSystemRole = r.GetBoolean(4),
                IsActive = r.GetBoolean(5),
                CreatedDate = r.GetDateTime(6),
                ModifiedDate = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
                CreatedBy = r.IsDBNull(8) ? null : r.GetString(8)
            };
        }
    }
}
