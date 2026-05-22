using System;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class DashboardService
    {
        private readonly IDashboardRepository _repository;

        public DashboardService(IDashboardRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<DashboardMetrics> GetMetricsAsync()
        {
            var metrics = new DashboardMetrics();
            int currentYear = DateTime.Now.Year;
            
            var studentTask = _repository.GetStudentCountAsync();
            var employeeTask = _repository.GetEmployeeCountAsync();
            var leaveTask = _repository.GetPendingLeaveCountAsync();
            var collectedTask = _repository.GetTotalFeesCollectedAsync();
            var balanceTask = _repository.GetTotalFeesBalanceAsync();
            var examTask = _repository.GetAverageExamScoreAsync();
            var topClassTask = _repository.GetTopClassByEnrollmentAsync();
            var recentPaymentsTask = _repository.GetRecentPaymentsAsync(14);
            var classSummaryTask = _repository.GetClassEnrollmentSummaryAsync();
            var leaveSummaryTask = _repository.GetLeaveStatusSummaryAsync();
            var subjectScoresTask = _repository.GetAverageScoreBySubjectAsync();
            var collectionTrendTask = _repository.GetMonthlyFeeCollectionTrendAsync(currentYear);

            await Task.WhenAll(studentTask, employeeTask, leaveTask, collectedTask, balanceTask, examTask, topClassTask, recentPaymentsTask, classSummaryTask, leaveSummaryTask, subjectScoresTask, collectionTrendTask);

            metrics.StudentCount = await studentTask;
            metrics.EmployeeCount = await employeeTask;
            metrics.PendingLeaveCount = await leaveTask;
            metrics.TotalFeesCollected = await collectedTask;
            metrics.TotalFeesBalance = await balanceTask;
            metrics.AverageExamScore = await examTask;
            metrics.TopClass = await topClassTask;
            metrics.RecentPayments = await recentPaymentsTask;
            metrics.ClassSummary = await classSummaryTask;
            metrics.LeaveSummary = await leaveSummaryTask;
            metrics.AverageScoresBySubject = await subjectScoresTask;
            metrics.CollectionTrend = await collectionTrendTask;

            return metrics;
        }
    }

    public class DashboardMetrics
    {
        public int StudentCount { get; set; }
        public int EmployeeCount { get; set; }
        public int PendingLeaveCount { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalFeesBalance { get; set; }
        public decimal AverageExamScore { get; set; }
        public string TopClass { get; set; }
        public DataTable RecentPayments { get; set; }
        public DataTable ClassSummary { get; set; }
        public DataTable LeaveSummary { get; set; }
        public DataTable AverageScoresBySubject { get; set; }
        public DataTable CollectionTrend { get; set; }
    }
}
