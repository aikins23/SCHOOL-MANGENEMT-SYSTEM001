using System;
using System.Collections.Generic;

namespace KingdomPrep.Web.Data;

public class ChartDataPoint
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public decimal Value2 { get; set; } // For comparative charts (e.g. Income vs Expense, Pass vs Fail)
}

public class WebAnalyticsData
{
    // Core summary statistics
    public int StudentCount { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetFund { get; set; }
    public decimal AverageExamScore { get; set; }
    public decimal AttendanceRateToday { get; set; }
    public int PendingLeaveCount { get; set; }

    // Chart Series corresponding to Desktop Analytics
    public List<ChartDataPoint> FeeCollectionStatus { get; set; } = new();
    public List<ChartDataPoint> EnrollmentByClass { get; set; } = new();
    public List<ChartDataPoint> ExamScoreBySubject { get; set; } = new();
    public List<ChartDataPoint> MonthlyFeeTrend { get; set; } = new();
    public List<ChartDataPoint> MonthlyAttendanceRate { get; set; } = new();
    public List<ChartDataPoint> IncomeVsExpenses { get; set; } = new();
    public List<ChartDataPoint> StaffLeaveStatus { get; set; } = new();
    public List<ChartDataPoint> GradeDistribution { get; set; } = new();
    public List<ChartDataPoint> AttendanceByClass { get; set; } = new();
    public List<ChartDataPoint> OutstandingByClass { get; set; } = new();
    public List<ChartDataPoint> PaymentMethodBreakdown { get; set; } = new();
    public List<ChartDataPoint> StaffByDepartment { get; set; } = new();
    public List<ChartDataPoint> ExpenseByCategory { get; set; } = new();
    public List<ChartDataPoint> TopAbsentStudents { get; set; } = new();
    public List<ChartDataPoint> ClassAverageScore { get; set; } = new();
    public List<ChartDataPoint> GenderDistribution { get; set; } = new();
    public List<ChartDataPoint> AdmissionsPerYear { get; set; } = new();
    public List<ChartDataPoint> ActiveVsRolledOut { get; set; } = new();
    public List<ChartDataPoint> SalaryByDepartment { get; set; } = new();
    public List<ChartDataPoint> SubjectPassFail { get; set; } = new();
}
