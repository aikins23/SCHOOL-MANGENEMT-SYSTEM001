using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IUserRepository
    {
        Task<DataTable> GetAllUsersAsTableAsync();
        Task<bool> DeleteUserAsync(string username);
        Task<bool> ResetPasswordAsync(string username, string hashedPassword);
        Task<bool> UpdateUserRoleAsync(string username, string newRole);
    }
}
