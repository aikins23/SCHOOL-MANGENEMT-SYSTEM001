using KingdomPrep.Web.Core.Auth;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class AuditLogServiceTests
{
    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppDbContext(options));
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions(bool ignoreTransactions = false)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());

        if (ignoreTransactions)
        {
            builder.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }

        return builder.Options;
    }

    [Fact]
    public async Task GetLogsAsync_FiltersBySchoolSearchAndAction()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var service = new AuditLogService(new TestDbContextFactory(options));

        await service.WriteAsync(new AuditLogEntry("admin", "PasswordChanged", "User", "admin", "Changed password", schoolId));
        await service.WriteAsync(new AuditLogEntry("head", "LeaveAPPROVED", "Leave", "1", "Approved leave", schoolId));
        await service.WriteAsync(new AuditLogEntry("admin", "PasswordChanged", "User", "other", "Other school", otherSchoolId));

        var logs = await service.GetLogsAsync("password", "PasswordChanged", schoolId: schoolId);

        var log = Assert.Single(logs);
        Assert.Equal("admin", log.ActorUsername);
        Assert.Equal("PasswordChanged", log.Action);
        Assert.Equal(schoolId, log.SchoolId);
    }

    [Fact]
    public async Task ChangePasswordAsync_WritesAuditLog()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var factory = new TestDbContextFactory(options);
        var audit = new AuditLogService(factory);
        var service = new UserAccountService(factory, audit);

        using (var db = new AppDbContext(options))
        {
            db.Users.Add(new UserEntity { Username = "admin", Password = PasswordHasher.Hash("OldPass123"), UserType = "Administrator", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await service.ChangePasswordAsync("admin", "OldPass123", "NewPass123", schoolId, "admin");

        using var verify = new AppDbContext(options);
        var log = Assert.Single(verify.AuditLogs);
        Assert.Equal("PasswordChanged", log.Action);
        Assert.Equal("admin", log.ActorUsername);
        Assert.Equal("User", log.EntityType);
        Assert.Equal("admin", log.EntityId);
    }

    [Fact]
    public async Task ParentRequestStatusUpdate_WritesAuditLog()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var factory = new TestDbContextFactory(options);
        var audit = new AuditLogService(factory);
        var repo = new ParentPortalRepository(factory, audit);

        using (var db = new AppDbContext(options))
        {
            db.ParentRequests.Add(new ParentRequestEntity { RequestID = 1, ParentUsername = "parent1", StudentID = 101, RequestType = "TransferLetter", Details = "A", Status = "Pending", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await repo.UpdateRequestStatusAsync(1, "Approved", schoolId, "head");

        using var verify = new AppDbContext(options);
        var log = Assert.Single(verify.AuditLogs);
        Assert.Equal("ParentRequestApproved", log.Action);
        Assert.Equal("ParentRequest", log.EntityType);
        Assert.Equal("1", log.EntityId);
    }

    [Fact]
    public async Task AdmissionApproval_WritesAuditLog()
    {
        var options = NewInMemoryOptions(ignoreTransactions: true);
        var schoolId = Guid.NewGuid();
        var factory = new TestDbContextFactory(options);
        var audit = new AuditLogService(factory);
        var repo = new AdmissionRepository(factory, audit);

        using (var db = new AppDbContext(options))
        {
            db.DraftAdmissions.Add(new DraftAdmissionEntity { DraftID = 1, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await repo.ApproveAdmissionAsync(1, schoolId, "director");

        using var verify = new AppDbContext(options);
        var log = Assert.Single(verify.AuditLogs);
        Assert.Equal("AdmissionApproved", log.Action);
        Assert.Equal("DraftAdmission", log.EntityType);
        Assert.Equal("1", log.EntityId);
        Assert.Contains("Ama Mensah", log.Summary);
    }
}
