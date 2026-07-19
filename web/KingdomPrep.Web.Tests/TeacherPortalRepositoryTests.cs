using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class TeacherPortalRepositoryTests
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
    public async Task SaveAssignmentAsync_CreatesOnlyForAssignedClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new TeacherPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await repo.SaveAssignmentAsync(new AssignmentEntity
        {
            TeacherID = "10",
            ClassID = "BASIC 1",
            Subject = "English",
            Title = "Essay",
            DueDate = new DateTime(2026, 6, 30)
        }, schoolId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveAssignmentAsync(new AssignmentEntity
        {
            TeacherID = "10",
            ClassID = "BASIC 2",
            Subject = "Math",
            Title = "Wrong class",
            DueDate = new DateTime(2026, 6, 30)
        }, schoolId));

        using var verify = new AppDbContext(options);
        var assignment = Assert.Single(verify.Assignments);
        Assert.Equal("BASIC 1", assignment.ClassID);
    }

    [Fact]
    public async Task SaveAssignmentAsync_RejectsUpdatingAnotherTeachersAssignment()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new TeacherPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.AddRange(
                new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId },
                new ClassAssignmentEntity { ClassName = "BASIC 2", ClassTeacherID = 20, SchoolId = schoolId });
            db.Assignments.Add(new AssignmentEntity
            {
                AssignmentID = 1,
                TeacherID = "20",
                ClassID = "BASIC 2",
                Subject = "Science",
                Title = "Original",
                DueDate = new DateTime(2026, 6, 30)
            });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveAssignmentAsync(new AssignmentEntity
        {
            AssignmentID = 1,
            TeacherID = "10",
            ClassID = "BASIC 1",
            Subject = "English",
            Title = "Hijack",
            DueDate = new DateTime(2026, 7, 1)
        }, schoolId));

        using var verify = new AppDbContext(options);
        var assignment = Assert.Single(verify.Assignments);
        Assert.Equal("Original", assignment.Title);
        Assert.Equal("20", assignment.TeacherID);
    }

    [Fact]
    public async Task DeleteAssignmentAsync_DeletesOnlySignedInTeachersAssignment()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new TeacherPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.AddRange(
                new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId },
                new ClassAssignmentEntity { ClassName = "BASIC 2", ClassTeacherID = 20, SchoolId = schoolId });
            db.Assignments.AddRange(
                new AssignmentEntity { AssignmentID = 1, TeacherID = "10", ClassID = "BASIC 1", Subject = "English", Title = "Mine", DueDate = new DateTime(2026, 6, 30) },
                new AssignmentEntity { AssignmentID = 2, TeacherID = "20", ClassID = "BASIC 2", Subject = "Science", Title = "Other", DueDate = new DateTime(2026, 6, 30) });
            await db.SaveChangesAsync();
        }

        await repo.DeleteAssignmentAsync(2, 10, schoolId);
        await repo.DeleteAssignmentAsync(1, 10, schoolId);

        using var verify = new AppDbContext(options);
        var remaining = Assert.Single(verify.Assignments);
        Assert.Equal("Other", remaining.Title);
    }

    [Fact]
    public async Task SaveStudentRemarksAsync_CreatesAndUpdatesForAssignedClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new TeacherPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            db.Students.Add(new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await repo.SaveStudentRemarksAsync(10, "BASIC 1", new StudentTermRemarksEntity
        {
            StudentID = "101",
            Term = "First Term",
            Year = "2025/2026",
            Conduct = "Good",
            ClassTeacherRemarks = "Keep improving."
        }, schoolId);

        await repo.SaveStudentRemarksAsync(10, "BASIC 1", new StudentTermRemarksEntity
        {
            StudentID = "101",
            Term = "First Term",
            Year = "2025/2026",
            Conduct = "Excellent",
            ClassTeacherRemarks = "Excellent progress."
        }, schoolId);

        var remarks = await repo.GetClassRemarksAsync(10, "BASIC 1", "First Term", "2025/2026", schoolId);

        var row = Assert.Single(remarks);
        Assert.Equal("Excellent", row.Conduct);
        Assert.Equal("Excellent progress.", row.ClassTeacherRemarks);
    }

    [Fact]
    public async Task SaveStudentRemarksAsync_RejectsUnassignedClassAndWrongStudentClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new TeacherPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ClassAssignments.Add(new ClassAssignmentEntity { ClassName = "BASIC 1", ClassTeacherID = 10, SchoolId = schoolId });
            db.Students.AddRange(
                new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = schoolId },
                new StudentEntity { StudentID = 202, FirstName = "Kofi", LastName = "Boateng", ClassID = "BASIC 2", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveStudentRemarksAsync(10, "BASIC 2", new StudentTermRemarksEntity
        {
            StudentID = "202",
            Term = "First Term",
            Year = "2025/2026"
        }, schoolId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveStudentRemarksAsync(10, "BASIC 1", new StudentTermRemarksEntity
        {
            StudentID = "202",
            Term = "First Term",
            Year = "2025/2026"
        }, schoolId));
    }
}
