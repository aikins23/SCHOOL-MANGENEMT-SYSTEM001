using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Interface for student data repository
    /// </summary>
    public interface IStudentRepository
    {
        Task<KingdomPrep.Shared.Models.Student> GetByIdAsync(string studentId);
        Task<IEnumerable<KingdomPrep.Shared.Models.Student>> GetAllAsync();
        Task<IEnumerable<KingdomPrep.Shared.Models.Student>> GetByClassAsync(string classId);
        Task<bool> AddAsync(KingdomPrep.Shared.Models.Student student);
        Task<bool> UpdateAsync(KingdomPrep.Shared.Models.Student student);
        Task<bool> DeleteAsync(string studentId);
        Task<bool> ExistsAsync(string studentId);
        Task<string> GenerateNextStudentIdAsync();
        Task<DataTable> GetAsTableAsync(string filterId = null, string filterClass = null);
        Task<(DataTable Items, int TotalCount)> GetPageAsTableAsync(int page, int pageSize, string filterId = null, string filterClass = null, string search = null);
        Task<bool> UpdateStudentClassBatchAsync(IEnumerable<string> studentIds, string newClassId);
        Task<bool> RollOutAsync(string studentId);
        Task<bool> RestoreAsync(string studentId);
        Task<DataTable> GetRolledOutAsTableAsync();
        Task<DataTable> GetGraduatedAsTableAsync();
    }
}
