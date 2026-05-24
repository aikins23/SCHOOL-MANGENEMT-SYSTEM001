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
        Task<Models.Employee> GetByIdAsync(string employeeId);
        Task<IEnumerable<Models.Employee>> GetAllAsync();
        Task<IEnumerable<Models.Employee>> GetByDepartmentAsync(string department);
        Task<bool> AddAsync(Models.Employee employee);
        Task<bool> UpdateAsync(Models.Employee employee);
        Task<bool> DeleteAsync(string employeeId);
        Task<bool> ExistsAsync(string employeeId);
        Task<string> GenerateNextEmployeeIdAsync();
        Task<DataTable> GetAsTableAsync(string filterId = null, string filterDepartment = null);
        Task<bool> TerminateAsync(string employeeId, DateTime terminationDate);
    }
}
