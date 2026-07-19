using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public class HeadmasterDashboardStatsDto
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalClasses { get; set; }
    public double StudentAttendancePercentage { get; set; }
    public double FeesPaidPercentage { get; set; }
    public double FeesOutstandingPercentage { get; set; }
    public int PendingApprovalsCount { get; set; }
}

public class ApprovalItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DateSubmitted { get; set; }
    public string SubmittedBy { get; set; } = "";
}

public interface IHeadmasterService
{
    Task<HeadmasterDashboardStatsDto> GetDashboardOverviewAsync();
    Task<List<ApprovalItemDto>> GetPendingApprovalsAsync();
    Task<bool> ApproveItemAsync(Guid itemId, string headmasterName);
}

public interface IHeadmasterTenantProvider
{
    Task<Guid?> GetSchoolIdAsync();
}

public class HeadmasterService(
    IDbContextFactory<AppDbContext> dbFactory,
    IHeadmasterTenantProvider? tenantProvider = null) : IHeadmasterService
{
    public async Task<HeadmasterDashboardStatsDto> GetDashboardOverviewAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var schoolId = tenantProvider == null ? null : await tenantProvider.GetSchoolIdAsync();
        var students = db.Students.AsNoTracking().AsQueryable();
        var employees = db.Employees.AsNoTracking().AsQueryable();
        var feeLedgers = db.StudentFeeLedgers.AsNoTracking().AsQueryable();
        var leaves = db.Leaves.AsNoTracking().AsQueryable();
        var performanceReports =
            from report in db.ClassPerformanceReports.AsNoTracking()
            join workflow in db.ApprovalWorkflows.AsNoTracking() on report.WorkflowId equals workflow.WorkflowId
            where workflow.CurrentStatus == "Submitted"
            select report;

        if (schoolId.HasValue)
        {
            students = students.Where(s => s.SchoolId == schoolId.Value);
            employees = employees.Where(e => e.SchoolId == schoolId.Value);
            feeLedgers = feeLedgers.Where(f => f.SchoolId == schoolId.Value);
            leaves = leaves.Where(l => l.SchoolId == schoolId.Value);
            performanceReports = performanceReports.Where(r => r.SchoolId == schoolId.Value);
        }

        var stats = new HeadmasterDashboardStatsDto();

        stats.TotalStudents = await students.CountAsync();
        stats.TotalTeachers = await employees.CountAsync(e => e.Position == "Teacher");

        // Count distinct classes based on student assignments
        stats.TotalClasses = await students.Where(s => !string.IsNullOrEmpty(s.ClassID)).Select(s => s.ClassID).Distinct().CountAsync();

        // Mock Attendance Percentage for today (would normally query AttendanceEntity)
        stats.StudentAttendancePercentage = 94.5;

        // Critical: Financial data must only be percentages. No raw figures.
        var ledgerRows = await feeLedgers.ToListAsync();
        if (ledgerRows.Any())
        {
            decimal totalExpected = ledgerRows.Sum(f => f.TotalExpectedAmount);
            decimal totalPaid = ledgerRows.Sum(f => f.TotalPaidAmount);

            if (totalExpected > 0)
            {
                stats.FeesPaidPercentage = Math.Round((double)(totalPaid / totalExpected) * 100, 1);
                stats.FeesOutstandingPercentage = 100 - stats.FeesPaidPercentage;
            }
        }

        stats.PendingApprovalsCount =
            await leaves.CountAsync(l => l.Status == "PENDING" || l.Status == "Pending") +
            await performanceReports.CountAsync();

        return stats;
    }

    public async Task<List<ApprovalItemDto>> GetPendingApprovalsAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var schoolId = tenantProvider == null ? null : await tenantProvider.GetSchoolIdAsync();

        var approvals = new List<ApprovalItemDto>();

        // Pull pending teacher leave requests
        var leaveQuery = db.Leaves.AsNoTracking()
            .Where(l => l.Status == "PENDING" || l.Status == "Pending");
        if (schoolId.HasValue) leaveQuery = leaveQuery.Where(l => l.SchoolId == schoolId.Value);

        var pendingLeaves = await leaveQuery.Take(10).ToListAsync();

        foreach(var leave in pendingLeaves)
        {
            approvals.Add(new ApprovalItemDto
            {
                Id = Guid.NewGuid(), // Mocking ID if not available
                Type = "Leave Request",
                Description = $"Leave requested for {leave.Reasons ?? "Unknown"}",
                DateSubmitted = leave.StartDate,
                SubmittedBy = leave.EmploymentID.ToString()
            });
        }

        var performanceQuery =
            from report in db.ClassPerformanceReports.AsNoTracking()
            join workflow in db.ApprovalWorkflows.AsNoTracking() on report.WorkflowId equals workflow.WorkflowId
            where workflow.CurrentStatus == "Submitted"
            select report;
        if (schoolId.HasValue) performanceQuery = performanceQuery.Where(r => r.SchoolId == schoolId.Value);

        var pendingPerformanceReports = await performanceQuery
            .OrderByDescending(r => r.ReportDate)
            .Take(10)
            .ToListAsync();

        foreach (var report in pendingPerformanceReports)
        {
            approvals.Add(new ApprovalItemDto
            {
                Id = Guid.NewGuid(),
                Type = "Performance Report",
                Description = $"{report.ReportPeriod} report for {report.ClassId}: {Truncate(report.ReportText, 90)}",
                DateSubmitted = report.ReportDate,
                SubmittedBy = report.TeacherId.ToString()
            });
        }

        return approvals.OrderByDescending(a => a.DateSubmitted).ToList();
    }

    public async Task<bool> ApproveItemAsync(Guid itemId, string headmasterName)
    {
        // Implementation for approving items and adding to AuditLog
        // In a real scenario, we would lookup the specific item type and update its status.
        return true;
    }

    private static string Truncate(string? value, int max)
    {
        value = value?.Trim() ?? "";
        return value.Length <= max ? value : value[..max] + "...";
    }
}
