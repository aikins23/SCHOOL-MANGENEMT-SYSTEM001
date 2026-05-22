using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IExamRepository
    {
        Task<bool> ResultExistsAsync(string studentId, string subject, string term, string year);
        Task<bool> AddResultAsync(Models.ExamResult result);
        Task<bool> UpdateResultAsync(Models.ExamResult result);
        Task<DataTable> GetAllResultsTableAsync();
        Task<DataTable> GetStudentResultsAsync(string studentId, string term, string year);
    }
}
