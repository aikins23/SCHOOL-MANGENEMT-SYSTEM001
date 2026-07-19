using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class ExamService
    {
        private readonly IExamRepository _repository;

        public ExamService(IExamRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<(bool Success, string Message)> SaveResultsAsync(IEnumerable<KingdomPrep.Shared.Models.ExamResult> results)
        {
            try
            {
                int saved = 0;
                foreach (var result in results)
                {
                    result.Calculate();
                    result.Grade = GradingScheme.CodeForScore(result.TotalScore);
                    result.Remark = GradingScheme.LabelForScore(result.TotalScore);
                    bool exists = await _repository.ResultExistsAsync(result.StudentId, result.Subject, result.Term, result.Year);

                    bool success = exists
                        ? await _repository.UpdateResultAsync(result)
                        : await _repository.AddResultAsync(result);

                    if (success) saved++;
                }
                return (true, $"{saved} result(s) saved successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Error saving results: " + ex.Message);
            }
        }

        public async Task<DataTable> GetResultsReportTableAsync()
        {
            return await _repository.GetAllResultsTableAsync();
        }

        public async Task<DataTable> GetExistingResultsForStudentAsync(string studentId, string term, string year)
        {
            return await _repository.GetStudentResultsAsync(studentId, term, year);
        }

        public async Task<DataTable> GetExistingResultsForClassSubjectAsync(string classId, string subject, string term, string year)
        {
            return await _repository.GetClassSubjectResultsAsync(classId, subject, term, year);
        }
    }
}
