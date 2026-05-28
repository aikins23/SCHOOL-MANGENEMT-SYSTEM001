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
        Task<DataTable> GetMonthlyAttendanceRateAsync(int year);
        Task<DataTable> GetMonthlyIncomeVsExpensesAsync(int year);
        Task<DataTable> GetGradeDistributionAsync();
        Task<DataTable> GetAttendanceRateByClassAsync();
        Task<DataTable> GetOutstandingFeesByClassAsync();
        Task<DataTable> GetPaymentModeBreakdownAsync();
        Task<DataTable> GetStaffByDepartmentAsync();
        Task<DataTable> GetExpenseByCategoryAsync();
        Task<DataTable> GetTopAbsentStudentsAsync(int topN);
        Task<DataTable> GetClassAverageScoreAsync();
        Task<DataTable> GetStudentGenderDistributionAsync();
        Task<DataTable> GetTermOverTermPerformanceAsync();
        Task<DataTable> GetAdmissionsPerYearAsync();
        Task<DataTable> GetActiveVsRolledOutStudentsAsync();
        Task<DataTable> GetSalarySpendByDepartmentAsync();
        Task<DataTable> GetSubjectPassFailRateAsync();
    }
}
