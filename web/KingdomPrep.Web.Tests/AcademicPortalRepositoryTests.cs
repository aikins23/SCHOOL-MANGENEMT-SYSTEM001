using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class AcademicPortalRepositoryTests
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
    public async Task GetClassSubjectsAsync_FiltersByClassSearchAndSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassSubjects.AddRange(
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SortOrder = 2, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "English Language", SortOrder = 1, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 2", Subject = "Mathematics", SortOrder = 1, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SortOrder = 1, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var subjects = await repo.GetClassSubjectsAsync("BASIC 1", "math", schoolId);

        Assert.Single(subjects);
        Assert.Equal("Mathematics", subjects[0].Subject);
        Assert.Equal(schoolId, subjects[0].SchoolId);
    }

    [Fact]
    public async Task SaveClassSubjectAsync_CreatesUpdatesAndBlocksDuplicates()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        var saved = await repo.SaveClassSubjectAsync(new ClassSubjectEntity
        {
            ClassName = " BASIC 1 ",
            Subject = " Mathematics ",
            SortOrder = 1
        }, schoolId);

        Assert.NotEqual(0, saved.Id);
        Assert.NotNull(saved.SyncId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveClassSubjectAsync(new ClassSubjectEntity
        {
            ClassName = "BASIC 1",
            Subject = "Mathematics"
        }, schoolId));

        saved.Subject = "English Language";
        saved.SortOrder = 2;
        await repo.SaveClassSubjectAsync(saved, schoolId);

        using var verify = new AppDbContext(options);
        var row = Assert.Single(verify.ClassSubjects.Where(s => s.SchoolId == schoolId));
        Assert.Equal("English Language", row.Subject);
        Assert.Equal(2, row.SortOrder);
    }

    [Fact]
    public async Task GetClassSubjectSummaryAsync_GroupsByClassForCurrentSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassSubjects.AddRange(
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "English", SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 2", Subject = "Science", SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Other", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var summary = await repo.GetClassSubjectSummaryAsync(schoolId);

        Assert.Equal(2, summary.Count);
        Assert.Equal(2, summary.Single(s => s.ClassName == "BASIC 1").SubjectCount);
        Assert.Equal(1, summary.Single(s => s.ClassName == "BASIC 2").SubjectCount);
    }

    [Fact]
    public async Task DeleteClassSubjectAsync_RemovesOnlyCurrentSchoolRow()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassSubjects.AddRange(
                new ClassSubjectEntity { Id = 1, ClassName = "BASIC 1", Subject = "Mathematics", SchoolId = schoolId },
                new ClassSubjectEntity { Id = 2, ClassName = "BASIC 1", Subject = "Mathematics", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        await repo.DeleteClassSubjectAsync(1, schoolId);

        using var verify = new AppDbContext(options);
        Assert.DoesNotContain(verify.ClassSubjects, s => s.Id == 1);
        Assert.Contains(verify.ClassSubjects, s => s.Id == 2);
    }

    [Fact]
    public async Task GetResultsOverviewAsync_CalculatesCompletionMissingAndAverage()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassSubjects.AddRange(
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SortOrder = 1, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "English", SortOrder = 2, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 2", Subject = "Science", SortOrder = 1, SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SortOrder = 1, SchoolId = otherSchoolId });
            db.Students.AddRange(
                new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = schoolId },
                new StudentEntity { StudentID = 102, FirstName = "Kofi", LastName = "Boateng", ClassID = "BASIC 1", SchoolId = schoolId },
                new StudentEntity { StudentID = 201, FirstName = "Esi", LastName = "Owusu", ClassID = "BASIC 2", SchoolId = schoolId },
                new StudentEntity { StudentID = 301, FirstName = "Other", LastName = "School", ClassID = "BASIC 1", SchoolId = otherSchoolId });
            db.Exams.AddRange(
                new ExamResultEntity { StudentId = 101, ClassId = "BASIC 1", Subject = "Mathematics", Term = "First Term", Year = "2025/2026", TotalScore = 80, SchoolId = schoolId },
                new ExamResultEntity { StudentId = 102, ClassId = "BASIC 1", Subject = "Mathematics", Term = "First Term", Year = "2025/2026", TotalScore = 70, SchoolId = schoolId },
                new ExamResultEntity { StudentId = 101, ClassId = "BASIC 1", Subject = "English", Term = "First Term", Year = "2025/2026", TotalScore = 60, SchoolId = schoolId },
                new ExamResultEntity { StudentId = 301, ClassId = "BASIC 1", Subject = "Mathematics", Term = "First Term", Year = "2025/2026", TotalScore = 100, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var overview = await repo.GetResultsOverviewAsync("First Term", "2025/2026", schoolId: schoolId);

        Assert.Equal(3, overview.Count);

        var math = overview.Single(o => o.ClassName == "BASIC 1" && o.Subject == "Mathematics");
        Assert.Equal(2, math.StudentCount);
        Assert.Equal(2, math.RecordedCount);
        Assert.Equal(0, math.MissingCount);
        Assert.Equal(100m, math.CompletionRate);
        Assert.Equal(75m, math.AverageScore);

        var english = overview.Single(o => o.ClassName == "BASIC 1" && o.Subject == "English");
        Assert.Equal(2, english.StudentCount);
        Assert.Equal(1, english.RecordedCount);
        Assert.Equal(1, english.MissingCount);
        Assert.Equal(50m, english.CompletionRate);
        Assert.Equal(60m, english.AverageScore);
    }

    [Fact]
    public async Task GetResultsOverviewAsync_FiltersByClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassSubjects.AddRange(
                new ClassSubjectEntity { ClassName = "BASIC 1", Subject = "Mathematics", SchoolId = schoolId },
                new ClassSubjectEntity { ClassName = "BASIC 2", Subject = "Science", SchoolId = schoolId });
            db.Students.AddRange(
                new StudentEntity { StudentID = 101, FirstName = "Ama", ClassID = "BASIC 1", SchoolId = schoolId },
                new StudentEntity { StudentID = 201, FirstName = "Esi", ClassID = "BASIC 2", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var overview = await repo.GetResultsOverviewAsync("First Term", "2025/2026", classFilter: "BASIC 2", schoolId: schoolId);

        Assert.Single(overview);
        Assert.Equal("BASIC 2", overview[0].ClassName);
        Assert.Equal("Science", overview[0].Subject);
    }

    [Fact]
    public async Task GetExamTypesAsync_SeedsDefaultSystemTypes()
    {
        var options = NewInMemoryOptions();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        var types = await repo.GetExamTypesAsync();

        Assert.Contains(types, t => t.Code == "EOT" && t.IsSystemType && t.IsActive);
        Assert.Contains(types, t => t.Code == "MID" && t.IsSystemType && t.IsActive);
        Assert.Contains(types, t => t.Code == "MOCK" && t.IsSystemType && t.IsActive);
    }

    [Fact]
    public async Task GetExamTypesAsync_SeedsDefaultsForCurrentSchoolOnly()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        var types = await repo.GetExamTypesAsync(schoolId: schoolId);
        await repo.GetExamTypesAsync(schoolId: otherSchoolId);

        Assert.Contains(types, t => t.Code == "EOT" && t.SchoolId == schoolId);
        Assert.DoesNotContain(types, t => t.SchoolId == otherSchoolId);

        using var verify = new AppDbContext(options);
        Assert.Equal(6, verify.ExamTypes.Count(t => t.SchoolId == schoolId));
        Assert.Equal(6, verify.ExamTypes.Count(t => t.SchoolId == otherSchoolId));
    }

    [Fact]
    public async Task SaveExamTypeAsync_CreatesUpdatesAndBlocksDuplicateCodes()
    {
        var options = NewInMemoryOptions();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        var saved = await repo.SaveExamTypeAsync(new ExamTypeEntity
        {
            Name = " Candidate Mock ",
            Code = " mock-b9 ",
            Description = " Basic 9 candidate mock ",
            WeightPercentage = 40,
            IncludeInReportCard = false,
            IsGradedExam = true,
            DisplayOrder = 75
        });

        Assert.NotEqual(0, saved.ExamTypeId);
        Assert.Equal("MOCK-B9", saved.Code);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveExamTypeAsync(new ExamTypeEntity
        {
            Name = "Duplicate Mock",
            Code = "MOCK-B9"
        }));

        saved.Name = "Basic 9 Mock";
        saved.WeightPercentage = 50;
        await repo.SaveExamTypeAsync(saved);

        using var verify = new AppDbContext(options);
        var row = Assert.Single(verify.ExamTypes.Where(t => t.Code == "MOCK-B9"));
        Assert.Equal("Basic 9 Mock", row.Name);
        Assert.Equal(50, row.WeightPercentage);
    }

    [Fact]
    public async Task DeactivateExamTypeAsync_BlocksSystemTypeAndDeactivatesCustomType()
    {
        var options = NewInMemoryOptions();
        var repo = new AcademicPortalRepository(new TestDbContextFactory(options));

        var defaults = await repo.GetExamTypesAsync();
        var systemType = defaults.Single(t => t.Code == "EOT");

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeactivateExamTypeAsync(systemType.ExamTypeId));

        var custom = await repo.SaveExamTypeAsync(new ExamTypeEntity
        {
            Name = "Weekly Test",
            Code = "WEEKLY",
            WeightPercentage = 10,
            DisplayOrder = 90
        });

        await repo.DeactivateExamTypeAsync(custom.ExamTypeId);

        using var verify = new AppDbContext(options);
        Assert.False(verify.ExamTypes.Single(t => t.ExamTypeId == custom.ExamTypeId).IsActive);
    }
}
