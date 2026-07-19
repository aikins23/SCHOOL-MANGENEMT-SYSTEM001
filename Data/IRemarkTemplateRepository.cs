using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IRemarkTemplateRepository
    {
        Task<List<RemarkTemplate>> GetAllAsync();
        Task<List<RemarkTemplate>> GetByCategoryAsync(string category);
        Task<List<RemarkTemplate>> GetByCategoryAndSubCategoryAsync(string category, string subCategory);
        Task<int> CreateAsync(RemarkTemplate template);
        Task<bool> UpdateAsync(RemarkTemplate template);
        Task<bool> DeleteAsync(int remarkTemplateId);
        Task EnsureTableAsync();
        Task SeedDefaultsAsync();
    }
}
