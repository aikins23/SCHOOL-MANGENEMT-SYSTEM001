using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Interface for employee data repository
    /// </summary>
    public interface IEmployeeRepository
    {
        Task<KingdomPrep.Shared.Models.Employee> GetByIdAsync(string employeeId);
        Task<IEnumerable<KingdomPrep.Shared.Models.Employee>> GetAllAsync();
        Task<IEnumerable<KingdomPrep.Shared.Models.Employee>> GetByDepartmentAsync(string department);
        Task<bool> AddAsync(KingdomPrep.Shared.Models.Employee employee);
        Task<bool> UpdateAsync(KingdomPrep.Shared.Models.Employee employee);
        Task<bool> DeleteAsync(string employeeId);
        Task<bool> ExistsAsync(string employeeId);
        Task<string> GenerateNextEmployeeIdAsync();
        Task<DataTable> GetAsTableAsync(string filterId = null, string filterDepartment = null);
        Task<bool> TerminateAsync(string employeeId, DateTime terminationDate);
        Task<bool> RestoreAsync(string employeeId);
        Task<DataTable> GetRolledOutAsTableAsync();
    }
}
