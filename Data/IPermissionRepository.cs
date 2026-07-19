using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<Permission>> GetPermissionsByModuleAsync(string module);

        Task<List<Role>> GetAllRolesAsync();
        Task<Role> GetRoleByIdAsync(int roleId);
        Task<Role> GetRoleByNameAsync(string name);
        Task<int> CreateRoleAsync(Role role);
        Task<bool> UpdateRoleAsync(Role role);
        Task<bool> DeleteRoleAsync(int roleId);

        Task<List<Permission>> GetRolePermissionsAsync(int roleId);
        Task<bool> GrantPermissionAsync(int roleId, int permissionId, string assignedBy);
        Task<bool> RevokePermissionAsync(int roleId, int permissionId);
        Task<bool> SetRolePermissionsAsync(int roleId, List<int> permissionIds, string assignedBy);

        Task<List<Role>> GetUserRolesAsync(string username);
        Task<List<string>> GetUserPermissionCodesAsync(string username);
        Task<bool> AssignUserToRoleAsync(string username, int roleId, string assignedBy);
        Task<bool> RemoveUserFromRoleAsync(string username, int roleId);
        Task<bool> UserHasPermissionAsync(string username, string permissionCode);
    }
}
