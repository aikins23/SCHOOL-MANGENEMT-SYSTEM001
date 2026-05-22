using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Interface for student data repository
    /// </summary>
    public interface IStudentRepository
    {
        Task<Models.Student> GetByIdAsync(string studentId);
        Task<IEnumerable<Models.Student>> GetAllAsync();
        Task<IEnumerable<Models.Student>> GetByClassAsync(string classId);
        Task<bool> AddAsync(Models.Student student);
        Task<bool> UpdateAsync(Models.Student student);
        Task<bool> DeleteAsync(string studentId);
        Task<bool> ExistsAsync(string studentId);
        Task<string> GenerateNextStudentIdAsync();
        Task<DataTable> GetAsTableAsync(string filterId = null, string filterClass = null);
        Task<bool> UpdateStudentClassBatchAsync(IEnumerable<string> studentIds, string newClassId);
        Task<bool> RollOutAsync(string studentId);
        Task<DataTable> GetRolledOutAsTableAsync();
    }
}
