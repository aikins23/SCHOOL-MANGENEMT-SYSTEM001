using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class PerformanceReportRepositoryTests
{
    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppDbContext(options));
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task SubmitTeacherReportAsync_CreatesSubmittedReportForAssignedClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new PerformanceReportRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            db.Students.Add(new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var result = await repo.SubmitTeacherReportAsync(10, new ClassPerformanceReportEntity
        {
            ClassId = "BASIC 1",
            ReportPeriod = "Weekly",
            AcademicYear = "2025/2026",
            Term = "First Term",
            WeekNumber = 4,
            ReportText = "Class is improving steadily."
        }, new[]
        {
            new StudentPerformanceEntryEntity
            {
                StudentId = "101",
                PerformanceTrend = "Improving",
                ExerciseMarksObtained = 8.5m,
                ExerciseMarksTotal = 10m,
                HomeworkMarksObtained = 18m,
                HomeworkMarksTotal = 20m,
                TeacherNotes = "Better completion this week."
            }
        }, schoolId);

        Assert.NotEqual(0, result.Report.ReportId);
        Assert.Equal("Submitted", result.Status);
        Assert.Single(result.Entries);

        using var verify = new AppDbContext(options);
        var report = Assert.Single(verify.ClassPerformanceReports);
        var entry = Assert.Single(verify.StudentPerformanceEntries);
        var workflow = Assert.Single(verify.ApprovalWorkflows.Where(w => w.CurrentStatus == "Submitted"));
        Assert.Equal(schoolId, report.SchoolId);
        Assert.Equal(schoolId, entry.SchoolId);
        Assert.Equal(8.5m, entry.ExerciseMarksObtained);
        Assert.Equal(10m, entry.ExerciseMarksTotal);
        Assert.Equal(18m, entry.HomeworkMarksObtained);
        Assert.Equal(20m, entry.HomeworkMarksTotal);
        Assert.Equal(schoolId, workflow.SchoolId);
        Assert.NotEqual(Guid.Empty, report.SyncId);
        Assert.NotEqual(Guid.Empty, entry.SyncId);
        Assert.NotEqual(Guid.Empty, workflow.SyncId);
    }

    [Fact]
    public async Task SubmitTeacherReportAsync_RejectsUnassignedClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new PerformanceReportRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            db.Students.Add(new StudentEntity { StudentID = 202, FirstName = "Kofi", LastName = "Boateng", ClassID = "BASIC 2", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SubmitTeacherReportAsync(10, new ClassPerformanceReportEntity
        {
            ClassId = "BASIC 2",
            ReportPeriod = "Monthly",
            AcademicYear = "2025/2026",
            Term = "First Term",
            ReportText = "Wrong class."
        }, new[]
        {
            new StudentPerformanceEntryEntity { StudentId = "202", PerformanceTrend = "Stable", TeacherNotes = "Note" }
        }, schoolId));
    }

    [Fact]
    public async Task ApprovalAndParentVisibility_OnlyShowsApprovedReportsForOwnWard()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new PerformanceReportRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            db.Students.AddRange(
                new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", ParentUsername = "parent1", SchoolId = schoolId },
                new StudentEntity { StudentID = 102, FirstName = "Esi", LastName = "Owusu", ClassID = "BASIC 1", ParentUsername = "parent2", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var submitted = await repo.SubmitTeacherReportAsync(10, new ClassPerformanceReportEntity
        {
            ClassId = "BASIC 1",
            ReportPeriod = "Termly",
            AcademicYear = "2025/2026",
            Term = "First Term",
            ReportText = "Termly class performance summary."
        }, new[]
        {
            new StudentPerformanceEntryEntity { StudentId = "101", PerformanceTrend = "Improving", TeacherNotes = "Excellent reading progress." },
            new StudentPerformanceEntryEntity { StudentId = "102", PerformanceTrend = "Stable", TeacherNotes = "Maintaining steady performance." }
        }, schoolId);

        var pending = await repo.GetPendingApprovalReportsAsync(schoolId);
        Assert.Single(pending);

        Assert.Empty(await repo.GetApprovedReportsForStudentAsync("parent1", 101, schoolId));

        await repo.SetApprovalStatusAsync(submitted.Report.ReportId, "Approved", schoolId);

        var ownWardReports = await repo.GetApprovedReportsForStudentAsync("parent1", 101, schoolId);
        var otherWardReports = await repo.GetApprovedReportsForStudentAsync("parent1", 102, schoolId);

        var report = Assert.Single(ownWardReports);
        Assert.Equal("Approved", report.Status);
        Assert.Equal("Excellent reading progress.", Assert.Single(report.Entries).TeacherNotes);
        Assert.Empty(otherWardReports);
    }

    [Fact]
    public async Task SubmitTeacherReportAsync_IncludesEntryWhenOnlyMarksAreProvided()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new PerformanceReportRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            db.Students.Add(new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var result = await repo.SubmitTeacherReportAsync(10, new ClassPerformanceReportEntity
        {
            ClassId = "BASIC 1",
            ReportPeriod = "Weekly",
            AcademicYear = "2025/2026",
            Term = "First Term",
            ReportText = "Class homework scores were reviewed."
        }, new[]
        {
            new StudentPerformanceEntryEntity { StudentId = "101", PerformanceTrend = "Stable", HomeworkMarksObtained = 15m, HomeworkMarksTotal = 20m }
        }, schoolId);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(15m, entry.HomeworkMarksObtained);
        Assert.Equal(20m, entry.HomeworkMarksTotal);
    }
}
