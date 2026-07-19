using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class OnlinePaymentIntentRepositoryTests
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
    public async Task CreateAsync_SavesPrivatePaymentContext()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new OnlinePaymentIntentRepository(new TestDbContextFactory(options));

        await repo.CreateAsync(new OnlinePaymentIntentEntity
        {
            Reference = "FEE-101-12345",
            SchoolId = schoolId,
            StudentID = 101,
            ClassID = "BASIC 1",
            StudentName = "Ama Mensah",
            Amount = 150m,
            BalanceBeforePayment = 600m,
            Gateway = "Paystack",
            CreatedBy = "parent:ama"
        });

        var intent = await repo.GetByReferenceAsync("FEE-101-12345");

        Assert.NotNull(intent);
        Assert.Equal(schoolId, intent!.SchoolId);
        Assert.Equal(101, intent.StudentID);
        Assert.Equal(150m, intent.Amount);
        Assert.Equal("Pending", intent.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsExistingReferenceWithoutDuplicating()
    {
        var options = NewInMemoryOptions();
        var repo = new OnlinePaymentIntentRepository(new TestDbContextFactory(options));

        await repo.CreateAsync(new OnlinePaymentIntentEntity
        {
            Reference = "FEE-101-12345",
            StudentID = 101,
            Amount = 150m,
            Gateway = "Paystack"
        });

        await repo.CreateAsync(new OnlinePaymentIntentEntity
        {
            Reference = "FEE-101-12345",
            StudentID = 101,
            Amount = 150m,
            Gateway = "Paystack"
        });

        using var verify = new AppDbContext(options);
        Assert.Single(verify.OnlinePaymentIntents);
    }

    [Fact]
    public async Task MarkCompletedAsync_UpdatesStatusOnce()
    {
        var options = NewInMemoryOptions();
        var repo = new OnlinePaymentIntentRepository(new TestDbContextFactory(options));

        await repo.CreateAsync(new OnlinePaymentIntentEntity
        {
            Reference = "FEE-101-12345",
            StudentID = 101,
            Amount = 150m,
            Gateway = "Momo"
        });

        await repo.MarkCompletedAsync("FEE-101-12345");
        await repo.MarkCompletedAsync("FEE-101-12345");

        var intent = await repo.GetByReferenceAsync("FEE-101-12345");

        Assert.NotNull(intent);
        Assert.Equal("Completed", intent!.Status);
        Assert.NotNull(intent.CompletedAtUtc);
    }
}
