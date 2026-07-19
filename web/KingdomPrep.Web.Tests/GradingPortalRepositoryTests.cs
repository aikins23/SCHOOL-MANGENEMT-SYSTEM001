using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class GradingPortalRepositoryTests
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
    public async Task GetSubjectsAsync_ReturnsOnlyConfiguredClassSubjectsForSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassSubjects.AddRange(
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "English", SortOrder = 2, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SortOrder = 1, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 2", Subject = "Science", SortOrder = 1, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Other School Subject", SortOrder = 1, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var subjects = await repo.GetSubjectsAsync("BASIC 1", schoolId);

        Assert.Equal(new[] { "Mathematics", "English" }, subjects);
    }

    [Fact]
    public async Task GetSubjectsAsync_DoesNotFallbackWhenSpecificClassHasNoSubjects()
    {
        var options = NewInMemoryOptions();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        var subjects = await repo.GetSubjectsAsync("BASIC 9", Guid.NewGuid());

        Assert.Empty(subjects);
    }

    [Fact]
    public async Task GetClassGradesAsync_LoadsWholeClassForTerm()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Exams.AddRange(
                new ExamResultEntity { StudentId = 101, StudentName = "Ama", ClassId = "BASIC 1", Subject = "Mathematics", Term = "First Term", Year = "2025/2026", TotalScore = 80, SchoolId = schoolId },
                new ExamResultEntity { StudentId = 101, StudentName = "Ama", ClassId = "BASIC 1", Subject = "English", Term = "First Term", Year = "2025/2026", TotalScore = 75, SchoolId = schoolId },
                new ExamResultEntity { StudentId = 102, StudentName = "Kofi", ClassId = "BASIC 1", Subject = "Mathematics", Term = "First Term", Year = "2025/2026", TotalScore = 70, SchoolId = schoolId },
                new ExamResultEntity { StudentId = 101, StudentName = "Ama", ClassId = "BASIC 1", Subject = "Science", Term = "Second Term", Year = "2025/2026", TotalScore = 90, SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var grades = await repo.GetClassGradesAsync("BASIC 1", "First Term", "2025/2026", schoolId);

        Assert.Equal(3, grades.Count);
        Assert.DoesNotContain(grades, g => g.Term == "Second Term");
    }

    [Fact]
    public async Task SaveGradesAsync_InsertsAndUpdatesByStudentSubjectTermYearSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Exams.Add(new ExamResultEntity
            {
                StudentId = 101,
                StudentName = "Ama",
                ClassId = "BASIC 1",
                Subject = "Mathematics",
                Term = "First Term",
                Year = "2025/2026",
                TotalScore = 70,
                Grade = "3",
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        await repo.SaveGradesAsync(new[]
        {
            new ExamResultEntity
            {
                StudentId = 101,
                StudentName = "Ama",
                ClassId = "BASIC 1",
                Subject = "Mathematics",
                Term = "First Term",
                Year = "2025/2026",
                TotalScore = 88,
                Grade = "1",
                SchoolId = schoolId
            },
            new ExamResultEntity
            {
                StudentId = 102,
                StudentName = "Kofi",
                ClassId = "BASIC 1",
                Subject = "Mathematics",
                Term = "First Term",
                Year = "2025/2026",
                TotalScore = 74,
                Grade = "3",
                SchoolId = schoolId
            }
        });

        using var verify = new AppDbContext(options);
        Assert.Equal(2, verify.Exams.Count(e => e.SchoolId == schoolId));
        Assert.Equal(88, verify.Exams.Single(e => e.StudentId == 101).TotalScore);
        Assert.Equal("1", verify.Exams.Single(e => e.StudentId == 101).Grade);
    }

    [Fact]
    public async Task IsGradingOpenAsync_RequiresCurrentDateInsideSetupWindow()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ExamSetups.Add(new ExamSetupEntity
            {
                SetupID = 1,
                Term = "First Term",
                Year = "2025/2026",
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 6, 30),
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        Assert.True(await repo.IsGradingOpenAsync("First Term", "2025/2026", schoolId, new DateTime(2026, 6, 15)));
        Assert.False(await repo.IsGradingOpenAsync("First Term", "2025/2026", schoolId, new DateTime(2026, 7, 1)));
        Assert.False(await repo.IsGradingOpenAsync("First Term", "2025/2026", Guid.NewGuid(), new DateTime(2026, 6, 15)));
    }

    [Fact]
    public async Task GetOpenExamSetupAsync_ReturnsOnlyCurrentlyOpenSetup()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ExamSetups.AddRange(
                new ExamSetupEntity { SetupID = 1, Term = "Past", Year = "2025/2026", StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 5, 31), SchoolId = schoolId },
                new ExamSetupEntity { SetupID = 2, Term = "Open", Year = "2025/2026", StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2026, 6, 30), SchoolId = schoolId },
                new ExamSetupEntity { SetupID = 3, Term = "Future", Year = "2025/2026", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 7, 31), SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var setup = await repo.GetOpenExamSetupAsync(schoolId, new DateTime(2026, 6, 15));

        Assert.NotNull(setup);
        Assert.Equal("Open", setup.Term);
    }

    [Fact]
    public async Task GetLatestExamSetupAsync_ReturnsMostRecentSetupForSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new GradingPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ExamSetups.AddRange(
                new ExamSetupEntity { SetupID = 1, Term = "First Term", Year = "2025/2026", EndDate = new DateTime(2026, 3, 31), SchoolId = schoolId },
                new ExamSetupEntity { SetupID = 2, Term = "Second Term", Year = "2025/2026", EndDate = new DateTime(2026, 6, 30), SchoolId = schoolId },
                new ExamSetupEntity { SetupID = 3, Term = "Other School", Year = "2025/2026", EndDate = new DateTime(2026, 12, 31), SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var setup = await repo.GetLatestExamSetupAsync(schoolId);

        Assert.NotNull(setup);
        Assert.Equal("Second Term", setup.Term);
    }
}
