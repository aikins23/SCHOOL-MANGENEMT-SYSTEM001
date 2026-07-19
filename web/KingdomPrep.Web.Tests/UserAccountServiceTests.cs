using KingdomPrep.Web.Core.Auth;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class UserAccountServiceTests
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
    public async Task ChangePasswordAsync_VerifiesCurrentPasswordAndStoresHash()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var service = new UserAccountService(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Users.Add(new UserEntity { Username = "admin", Password = PasswordHasher.Hash("OldPass123"), UserType = "Administrator", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        await service.ChangePasswordAsync("admin", "OldPass123", "NewPass123", schoolId);

        using var verify = new AppDbContext(options);
        var user = await verify.Users.SingleAsync(u => u.Username == "admin");
        Assert.NotEqual("NewPass123", user.Password);
        Assert.True(PasswordHasher.Verify("NewPass123", user.Password));
        Assert.False(PasswordHasher.Verify("OldPass123", user.Password));
    }

    [Fact]
    public async Task ChangePasswordAsync_MigratesLegacyPlainTextPassword()
    {
        var options = NewInMemoryOptions();
        var service = new UserAccountService(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Users.Add(new UserEntity { Username = "legacy", Password = "plainpw", UserType = "Teacher" });
            await db.SaveChangesAsync();
        }

        await service.ChangePasswordAsync("legacy", "plainpw", "Modern123");

        using var verify = new AppDbContext(options);
        var user = await verify.Users.SingleAsync(u => u.Username == "legacy");
        Assert.StartsWith("P3$", user.Password);
        Assert.True(PasswordHasher.Verify("Modern123", user.Password));
    }

    [Fact]
    public async Task ChangePasswordAsync_RejectsWrongCurrentPassword()
    {
        var options = NewInMemoryOptions();
        var service = new UserAccountService(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Users.Add(new UserEntity { Username = "admin", Password = PasswordHasher.Hash("OldPass123"), UserType = "Administrator" });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync("admin", "wrong", "NewPass123"));
    }

    [Fact]
    public async Task ChangePasswordAsync_RespectsSchoolScope()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var service = new UserAccountService(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Users.Add(new UserEntity { Username = "admin", Password = PasswordHasher.Hash("OldPass123"), UserType = "Administrator", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync("admin", "OldPass123", "NewPass123", schoolId));
    }

    [Fact]
    public async Task GetUsersAsync_FiltersBySchoolSearchAndRole()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var service = new UserAccountService(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Users.AddRange(
                new UserEntity { Username = "teacher1", Password = PasswordHasher.Hash("Password123"), UserType = "Teacher", EmploymentID = 10, SchoolId = schoolId },
                new UserEntity { Username = "accountant1", Password = "legacy", UserType = "Accountant", EmploymentID = 20, SchoolId = schoolId },
                new UserEntity { Username = "teacher2", Password = PasswordHasher.Hash("Password123"), UserType = "Teacher", EmploymentID = 30, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var users = await service.GetUsersAsync("teacher", "Teacher", schoolId);

        var user = Assert.Single(users);
        Assert.Equal("teacher1", user.Username);
        Assert.Equal("Teacher", user.Role);
        Assert.Equal("Current hash", user.PasswordFormat);
    }

    [Fact]
    public async Task GetUsersAsync_ReportsLegacyPasswordStateWithoutPasswordValue()
    {
        var options = NewInMemoryOptions();
        var service = new UserAccountService(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Users.Add(new UserEntity { Username = "legacy", Password = "plainpw", UserType = "Parent" });
            await db.SaveChangesAsync();
        }

        var users = await service.GetUsersAsync("legacy");

        var user = Assert.Single(users);
        Assert.Equal("Legacy plain text", user.PasswordFormat);
    }
}
