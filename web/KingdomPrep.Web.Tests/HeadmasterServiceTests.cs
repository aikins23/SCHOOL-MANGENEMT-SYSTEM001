using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Tests;

public class HeadmasterServiceTests
{
    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppDbContext(options));
    }

    private sealed class TestTenantProvider(Guid? schoolId) : IHeadmasterTenantProvider
    {
        public Task<Guid?> GetSchoolIdAsync() => Task.FromResult(schoolId);
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task GetDashboardOverviewAsync_ReturnsAggregatedCountsForCurrentSchool()
    {
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var options = NewInMemoryOptions();

        await using (var db = new AppDbContext(options))
        {
            db.Students.AddRange(
                new StudentEntity { StudentID = 1, FirstName = "John", LastName = "Doe", ClassID = "BASIC 1", SchoolId = schoolId },
                new StudentEntity { StudentID = 2, FirstName = "Jane", LastName = "Doe", ClassID = "BASIC 2", SchoolId = schoolId },
                new StudentEntity { StudentID = 3, FirstName = "Other", LastName = "Student", ClassID = "BASIC 3", SchoolId = otherSchoolId });
            db.Employees.AddRange(
                new EmployeeEntity { EmployeeID = 1, FullName = "Teacher One", Position = "Teacher", SchoolId = schoolId },
                new EmployeeEntity { EmployeeID = 2, FullName = "Accountant", Position = "Accountant", SchoolId = schoolId },
                new EmployeeEntity { EmployeeID = 3, FullName = "Other Teacher", Position = "Teacher", SchoolId = otherSchoolId });
            db.Leaves.AddRange(
                new LeaveEntity { EmploymentID = 1, Name = "Teacher One", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 2), Status = "Pending", SchoolId = schoolId },
                new LeaveEntity { EmploymentID = 3, Name = "Other Teacher", StartDate = new DateTime(2026, 1, 3), EndDate = new DateTime(2026, 1, 4), Status = "Pending", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var service = new HeadmasterService(new TestDbContextFactory(options), new TestTenantProvider(schoolId));

        var stats = await service.GetDashboardOverviewAsync();

        Assert.Equal(2, stats.TotalStudents);
        Assert.Equal(1, stats.TotalTeachers);
        Assert.Equal(2, stats.TotalClasses);
        Assert.Equal(1, stats.PendingApprovalsCount);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_CalculatesFeePercentagesForCurrentSchoolOnly()
    {
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var options = NewInMemoryOptions();

        await using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.AddRange(
                new StudentFeeLedgerEntity { StudentID = "S1", TermID = 1, TotalExpectedAmount = 1000m, TotalPaidAmount = 250m, SchoolId = schoolId },
                new StudentFeeLedgerEntity { StudentID = "S2", TermID = 1, TotalExpectedAmount = 1000m, TotalPaidAmount = 750m, SchoolId = schoolId },
                new StudentFeeLedgerEntity { StudentID = "S3", TermID = 1, TotalExpectedAmount = 1000m, TotalPaidAmount = 1000m, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var service = new HeadmasterService(new TestDbContextFactory(options), new TestTenantProvider(schoolId));

        var stats = await service.GetDashboardOverviewAsync();

        Assert.Equal(50.0, stats.FeesPaidPercentage);
        Assert.Equal(50.0, stats.FeesOutstandingPercentage);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_ReturnsZeroFeePercentages_WhenNoLedgersExist()
    {
        var options = NewInMemoryOptions();
        var service = new HeadmasterService(new TestDbContextFactory(options), new TestTenantProvider(Guid.NewGuid()));

        var stats = await service.GetDashboardOverviewAsync();

        Assert.Equal(0, stats.FeesPaidPercentage);
        Assert.Equal(0, stats.FeesOutstandingPercentage);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ReturnsOnlyPendingLeavesForCurrentSchool()
    {
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var options = NewInMemoryOptions();

        await using (var db = new AppDbContext(options))
        {
            db.Leaves.AddRange(
                new LeaveEntity { EmploymentID = 1, Name = "Teacher One", Reasons = "Medical", StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 2), Status = "Pending", SchoolId = schoolId },
                new LeaveEntity { EmploymentID = 2, Name = "Teacher Two", Reasons = "Personal", StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 4), Status = "Approved", SchoolId = schoolId },
                new LeaveEntity { EmploymentID = 3, Name = "Other Teacher", Reasons = "Medical", StartDate = new DateTime(2026, 2, 5), EndDate = new DateTime(2026, 2, 6), Status = "Pending", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var service = new HeadmasterService(new TestDbContextFactory(options), new TestTenantProvider(schoolId));

        var approvals = await service.GetPendingApprovalsAsync();

        Assert.Single(approvals);
        Assert.Equal("Leave Request", approvals[0].Type);
        Assert.Contains("Medical", approvals[0].Description);
        Assert.Equal("1", approvals[0].SubmittedBy);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_IncludesSubmittedPerformanceReportsForCurrentSchool()
    {
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var options = NewInMemoryOptions();

        await using (var db = new AppDbContext(options))
        {
            db.ApprovalWorkflows.AddRange(
                new ApprovalWorkflowEntity { WorkflowId = 1, EntityType = "ClassPerformanceReport", EntityId = 1, CurrentStatus = "Submitted" },
                new ApprovalWorkflowEntity { WorkflowId = 2, EntityType = "ClassPerformanceReport", EntityId = 2, CurrentStatus = "Approved" },
                new ApprovalWorkflowEntity { WorkflowId = 3, EntityType = "ClassPerformanceReport", EntityId = 3, CurrentStatus = "Submitted" });
            db.ClassPerformanceReports.AddRange(
                new ClassPerformanceReportEntity { ReportId = 1, ClassId = "BASIC 1", TeacherId = 10, ReportPeriod = "Monthly", AcademicYear = "2025/2026", Term = "First Term", ReportText = "Class is improving.", ReportDate = new DateTime(2026, 6, 30), WorkflowId = 1, SchoolId = schoolId },
                new ClassPerformanceReportEntity { ReportId = 2, ClassId = "BASIC 2", TeacherId = 20, ReportPeriod = "Weekly", AcademicYear = "2025/2026", Term = "First Term", ReportText = "Already approved.", ReportDate = new DateTime(2026, 6, 29), WorkflowId = 2, SchoolId = schoolId },
                new ClassPerformanceReportEntity { ReportId = 3, ClassId = "BASIC 3", TeacherId = 30, ReportPeriod = "Termly", AcademicYear = "2025/2026", Term = "First Term", ReportText = "Other school.", ReportDate = new DateTime(2026, 6, 28), WorkflowId = 3, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var service = new HeadmasterService(new TestDbContextFactory(options), new TestTenantProvider(schoolId));

        var stats = await service.GetDashboardOverviewAsync();
        var approvals = await service.GetPendingApprovalsAsync();

        Assert.Equal(1, stats.PendingApprovalsCount);
        var item = Assert.Single(approvals);
        Assert.Equal("Performance Report", item.Type);
        Assert.Contains("BASIC 1", item.Description);
        Assert.Equal("10", item.SubmittedBy);
    }
}
