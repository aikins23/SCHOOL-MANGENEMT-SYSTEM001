using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Repository interface for StudentTermRemarks data access
    /// </summary>
    public interface IStudentTermRemarksRepository
    {
        Task<StudentTermRemarks> GetAsync(string studentId, string term, string year);
        Task<bool> AddAsync(StudentTermRemarks remarks);
        Task<bool> UpdateAsync(StudentTermRemarks remarks);
        Task<bool> DeleteAsync(string studentId, string term, string year);
    }
}
