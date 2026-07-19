using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IClassRepository
    {
        Task EnsureTableExistsAsync();
        Task<DataTable> GetAllClassesTableAsync();
        Task<bool> SaveClassAsync(KingdomPrep.Shared.Models.ClassConfig config, string originalClassName = null);
        Task<bool> DeleteClassAsync(string className);
        Task<KingdomPrep.Shared.Models.ClassConfig> GetByClassNameAsync(string className);
        Task<IEnumerable<string>> GetClassesForTeacherAsync(int employmentId);
        Task<IEnumerable<(string ClassName, int? CurrentTeacherID)>> GetAllClassAssignmentsAsync();
        Task SetClassAssignmentsForTeacherAsync(int employmentId, IEnumerable<string> classNames);
        Task<IEnumerable<ClassAssignment>> GetAllDetailedAssignmentsAsync();
        Task<bool> AssignTeacherToClassAsync(string className, int? employmentId);
    }
}
