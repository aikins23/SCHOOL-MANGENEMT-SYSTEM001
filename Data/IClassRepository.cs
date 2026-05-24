using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IClassRepository
    {
        Task EnsureTableExistsAsync();
        Task<DataTable> GetAllClassesTableAsync();
        Task<bool> SaveClassAsync(Models.ClassConfig config);
        Task<bool> DeleteClassAsync(string className);
        Task<Models.ClassConfig> GetByClassNameAsync(string className);
    }
}
