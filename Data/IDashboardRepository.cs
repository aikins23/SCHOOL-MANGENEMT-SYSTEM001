using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IDashboardRepository
    {
        Task<DashboardCoreMetrics> GetCoreMetricsAsync();
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
        Task<decimal> GetTotalIncomeBetweenAsync(System.DateTime from, System.DateTime to);
        Task<decimal> GetTotalExpensesBetweenAsync(System.DateTime from, System.DateTime to);
    }

    public class DashboardCoreMetrics
    {
        public int StudentCount { get; set; }
        public int EmployeeCount { get; set; }
        public int PendingLeaveCount { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalFeesBalance { get; set; }
        public decimal AverageExamScore { get; set; }
        public string TopClass { get; set; }
        public string TopExpenseCategory { get; set; }
        public decimal TopExpenseAmount { get; set; }
        public string LargestExpenseItem { get; set; }
        public decimal LargestExpenseAmount { get; set; }
    }
}
