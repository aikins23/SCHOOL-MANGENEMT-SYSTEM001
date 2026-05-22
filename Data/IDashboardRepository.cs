using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IDashboardRepository
    {
        Task<int> GetStudentCountAsync();
        Task<int> GetEmployeeCountAsync();
        Task<int> GetPendingLeaveCountAsync();
        Task<decimal> GetTotalFeesCollectedAsync();
        Task<decimal> GetTotalFeesBalanceAsync();
        Task<decimal> GetAverageExamScoreAsync();
        Task<string> GetTopClassByEnrollmentAsync();
        Task<DataTable> GetRecentPaymentsAsync(int count);
        Task<DataTable> GetClassEnrollmentSummaryAsync();
        Task<DataTable> GetLeaveStatusSummaryAsync();
        Task<DataTable> GetAverageScoreBySubjectAsync();
        Task<DataTable> GetMonthlyFeeCollectionTrendAsync(int year);
    }
}
