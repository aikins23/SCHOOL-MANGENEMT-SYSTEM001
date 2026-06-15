using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class DashboardService
    {
        private readonly IDashboardRepository _repository;
        private static readonly TimeSpan MetricsCacheDuration = TimeSpan.FromSeconds(20);
        private readonly SemaphoreSlim _metricsLock = new SemaphoreSlim(1, 1);
        private DashboardMetrics _metricsCache;
        private DateTime _metricsCacheUtc;

        public DashboardService(IDashboardRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>Total income (payments) and expenses for a date window, plus net fund.</summary>
        public async Task<(decimal Income, decimal Expenses, decimal Fund)> GetFinanceSummaryAsync(DateTime from, DateTime to)
        {
            decimal income = await _repository.GetTotalIncomeBetweenAsync(from, to);
            decimal expenses = await _repository.GetTotalExpensesBetweenAsync(from, to);
            return (income, expenses, income - expenses);
        }

        public async Task<DashboardMetrics> GetMetricsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && IsMetricsCacheFresh())
            {
                return _metricsCache;
            }

            await _metricsLock.WaitAsync();
            try
            {
                if (!forceRefresh && IsMetricsCacheFresh())
                {
                    return _metricsCache;
                }

                var metrics = await LoadMetricsAsync();
                _metricsCache = metrics;
                _metricsCacheUtc = DateTime.UtcNow;
                return metrics;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public void InvalidateCache()
        {
            _metricsCache = null;
            _metricsCacheUtc = DateTime.MinValue;
        }

        private bool IsMetricsCacheFresh()
        {
            return _metricsCache != null && DateTime.UtcNow - _metricsCacheUtc < MetricsCacheDuration;
        }

        private async Task<DashboardMetrics> LoadMetricsAsync()
        {
            var metrics = new DashboardMetrics();
            int currentYear = DateTime.Now.Year;

            var coreTask = _repository.GetCoreMetricsAsync();
            var recentPaymentsTask = _repository.GetRecentPaymentsAsync(14);
            var classSummaryTask = _repository.GetClassEnrollmentSummaryAsync();
            var leaveSummaryTask = _repository.GetLeaveStatusSummaryAsync();
            var subjectScoresTask = _repository.GetAverageScoreBySubjectAsync();
            var collectionTrendTask = _repository.GetMonthlyFeeCollectionTrendAsync(currentYear);
            var attendanceTrendTask = _repository.GetMonthlyAttendanceRateAsync(currentYear);
            var incomeExpensesTask = _repository.GetMonthlyIncomeVsExpensesAsync(currentYear);
            var gradeDistributionTask = _repository.GetGradeDistributionAsync();
            var attendanceByClassTask = _repository.GetAttendanceRateByClassAsync();
            var outstandingByClassTask = _repository.GetOutstandingFeesByClassAsync();
            var paymentModeTask = _repository.GetPaymentModeBreakdownAsync();
            var staffByDeptTask = _repository.GetStaffByDepartmentAsync();
            var expenseByCategoryTask = _repository.GetExpenseByCategoryAsync();
            var topAbsentTask = _repository.GetTopAbsentStudentsAsync(10);
            var classAvgScoreTask = _repository.GetClassAverageScoreAsync();
            var genderDistTask = _repository.GetStudentGenderDistributionAsync();
            var termPerformanceTask = _repository.GetTermOverTermPerformanceAsync();
            var admissionsPerYearTask = _repository.GetAdmissionsPerYearAsync();
            var activeVsRolledOutTask = _repository.GetActiveVsRolledOutStudentsAsync();
            var salaryByDeptTask = _repository.GetSalarySpendByDepartmentAsync();
            var subjectPassFailTask = _repository.GetSubjectPassFailRateAsync();

            await Task.WhenAll(coreTask, recentPaymentsTask, classSummaryTask, leaveSummaryTask, subjectScoresTask, collectionTrendTask, attendanceTrendTask, incomeExpensesTask, gradeDistributionTask, attendanceByClassTask, outstandingByClassTask, paymentModeTask, staffByDeptTask, expenseByCategoryTask, topAbsentTask, classAvgScoreTask, genderDistTask, termPerformanceTask, admissionsPerYearTask, activeVsRolledOutTask, salaryByDeptTask, subjectPassFailTask);

            var core = await coreTask;
            metrics.StudentCount = core.StudentCount;
            metrics.EmployeeCount = core.EmployeeCount;
            metrics.PendingLeaveCount = core.PendingLeaveCount;
            metrics.TotalFeesCollected = core.TotalFeesCollected;
            metrics.TotalFeesBalance = core.TotalFeesBalance;
            metrics.AverageExamScore = core.AverageExamScore;
            metrics.TopClass = core.TopClass;
            metrics.TopExpenseCategory = core.TopExpenseCategory;
            metrics.TopExpenseAmount = core.TopExpenseAmount;
            metrics.LargestExpenseItem = core.LargestExpenseItem;
            metrics.LargestExpenseAmount = core.LargestExpenseAmount;
            metrics.RecentPayments = await recentPaymentsTask;
            metrics.ClassSummary = await classSummaryTask;
            metrics.LeaveSummary = await leaveSummaryTask;
            metrics.AverageScoresBySubject = await subjectScoresTask;
            metrics.CollectionTrend = await collectionTrendTask;
            metrics.AttendanceTrend = await attendanceTrendTask;
            metrics.IncomeVsExpenses = await incomeExpensesTask;
            metrics.GradeDistribution = await gradeDistributionTask;
            metrics.AttendanceByClass = await attendanceByClassTask;
            metrics.OutstandingByClass = await outstandingByClassTask;
            metrics.PaymentModeBreakdown = await paymentModeTask;
            metrics.StaffByDepartment = await staffByDeptTask;
            metrics.ExpenseByCategory = await expenseByCategoryTask;
            metrics.TopAbsentStudents = await topAbsentTask;
            metrics.ClassAverageScore = await classAvgScoreTask;
            metrics.StudentGenderDistribution = await genderDistTask;
            metrics.TermOverTermPerformance = await termPerformanceTask;
            metrics.AdmissionsPerYear = await admissionsPerYearTask;
            metrics.ActiveVsRolledOut = await activeVsRolledOutTask;
            metrics.SalaryByDepartment = await salaryByDeptTask;
            metrics.SubjectPassFail = await subjectPassFailTask;

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
        public string TopExpenseCategory { get; set; }
        public decimal TopExpenseAmount { get; set; }
        public string LargestExpenseItem { get; set; }
        public decimal LargestExpenseAmount { get; set; }
        public DataTable RecentPayments { get; set; }
        public DataTable ClassSummary { get; set; }
        public DataTable LeaveSummary { get; set; }
        public DataTable AverageScoresBySubject { get; set; }
        public DataTable CollectionTrend { get; set; }
        public DataTable AttendanceTrend { get; set; }
        public DataTable IncomeVsExpenses { get; set; }
        public DataTable GradeDistribution { get; set; }
        public DataTable AttendanceByClass { get; set; }
        public DataTable OutstandingByClass { get; set; }
        public DataTable PaymentModeBreakdown { get; set; }
        public DataTable StaffByDepartment { get; set; }
        public DataTable ExpenseByCategory { get; set; }
        public DataTable TopAbsentStudents { get; set; }
        public DataTable ClassAverageScore { get; set; }
        public DataTable StudentGenderDistribution { get; set; }
        public DataTable TermOverTermPerformance { get; set; }
        public DataTable AdmissionsPerYear { get; set; }
        public DataTable ActiveVsRolledOut { get; set; }
        public DataTable SalaryByDepartment { get; set; }
        public DataTable SubjectPassFail { get; set; }
    }
}
