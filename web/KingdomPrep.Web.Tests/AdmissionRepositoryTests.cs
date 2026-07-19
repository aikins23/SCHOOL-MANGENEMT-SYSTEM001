using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class AdmissionRepositoryTests
{
    // Fresh context per call over a shared in-memory database name — the repository
    // disposes the context it gets, so a single shared instance would be unusable
    // after the first call.
    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(new AppDbContext(options));
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    [Fact]
    public async Task ApproveAdmissionAsync_ConvertsDraftToStudent()
    {
        // Arrange
        var options = NewInMemoryOptions();
        var repo = new AdmissionRepository(new TestDbContextFactory(options));

        var draft = new DraftAdmissionEntity
        {
            FirstName = "Test",
            LastName = "Student",
            ClassID = "BASIC 1",
            AdmissionDate = DateTime.Today
        };
        using (var db = new AppDbContext(options))
        {
            db.DraftAdmissions.Add(draft);
            await db.SaveChangesAsync();
        }

        // Act
        await repo.ApproveAdmissionAsync(draft.DraftID);

        // Assert
        var pending = await repo.GetPendingAdmissionsAsync();
        Assert.Empty(pending);

        using var verify = new AppDbContext(options);
        var student = await verify.Students.FirstOrDefaultAsync(s => s.FirstName == "Test" && s.LastName == "Student");
        Assert.NotNull(student);
        Assert.Equal("BASIC 1", student.ClassID);
    }

    [Fact]
    public async Task GetPendingAdmissionsAsync_FiltersBySchoolSearchAndClass()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AdmissionRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.DraftAdmissions.AddRange(
                new DraftAdmissionEntity { FirstName = "Ama", LastName = "Mensah", GuidanceName = "Grace", ClassID = "BASIC 1", SchoolId = schoolId, SubmittedDate = new DateTime(2026, 6, 1) },
                new DraftAdmissionEntity { FirstName = "Kofi", LastName = "Boateng", GuidanceName = "Kojo", ClassID = "BASIC 2", SchoolId = schoolId, SubmittedDate = new DateTime(2026, 6, 2) },
                new DraftAdmissionEntity { FirstName = "Ama", LastName = "Owusu", GuidanceName = "Other", ClassID = "BASIC 1", SchoolId = otherSchoolId, SubmittedDate = new DateTime(2026, 6, 3) });
            await db.SaveChangesAsync();
        }

        var drafts = await repo.GetPendingAdmissionsAsync(schoolId, "grace", "BASIC 1");

        var draft = Assert.Single(drafts);
        Assert.Equal("Ama", draft.FirstName);
        Assert.Equal(schoolId, draft.SchoolId);
    }

    [Fact]
    public async Task ApproveAdmissionAsync_RequiresValidDraft()
    {
        var options = NewInMemoryOptions();
        var repo = new AdmissionRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.DraftAdmissions.Add(new DraftAdmissionEntity { DraftID = 1, FirstName = "Missing", LastName = "", ClassID = "BASIC 1" });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApproveAdmissionAsync(1));
    }

    [Fact]
    public async Task ApproveAdmissionAsync_RejectsWrongSchoolDraft()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new AdmissionRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.DraftAdmissions.Add(new DraftAdmissionEntity { DraftID = 1, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApproveAdmissionAsync(1, schoolId));
    }

    [Fact]
    public async Task DeleteDraftAsync_RemovesDraft()
    {
        // Arrange
        var options = NewInMemoryOptions();
        var repo = new AdmissionRepository(new TestDbContextFactory(options));

        var draft = new DraftAdmissionEntity { FirstName = "To Delete" };
        using (var db = new AppDbContext(options))
        {
            db.DraftAdmissions.Add(draft);
            await db.SaveChangesAsync();
        }

        // Act
        await repo.DeleteDraftAsync(draft.DraftID);

        // Assert
        var pending = await repo.GetPendingAdmissionsAsync();
        Assert.Empty(pending);
    }
}
