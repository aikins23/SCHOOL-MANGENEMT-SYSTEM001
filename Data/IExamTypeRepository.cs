using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IExamTypeRepository
    {
        Task EnsureTableAsync();
        Task SeedSystemTypesAsync();
        Task<List<ExamType>> GetAllAsync();
        Task<List<ExamType>> GetActiveAsync();
        Task<List<ExamType>> GetReportCardTypesAsync();
        Task<ExamType> GetByIdAsync(int id);
        Task<ExamType> GetByCodeAsync(string code);
        Task<int> CreateAsync(ExamType examType);
        Task<bool> UpdateAsync(ExamType examType);
        Task<bool> DeactivateAsync(int id);
        Task<decimal> GetTotalWeightAsync();
    }
}
