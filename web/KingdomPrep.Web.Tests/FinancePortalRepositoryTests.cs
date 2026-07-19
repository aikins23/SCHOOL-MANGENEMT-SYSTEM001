using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class FinancePortalRepositoryTests
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
    public async Task GetCurrentBalanceAsync_UsesLatestLedgerForCurrentSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.AddRange(
                new StudentFeeLedgerEntity { StudentID = "101", TermID = 1, TotalExpectedAmount = 900m, TotalPaidAmount = 300m, SchoolId = schoolId },
                new StudentFeeLedgerEntity { StudentID = "101", TermID = 2, TotalExpectedAmount = 1200m, TotalPaidAmount = 450m, SchoolId = schoolId },
                new StudentFeeLedgerEntity { StudentID = "101", TermID = 3, TotalExpectedAmount = 500m, TotalPaidAmount = 100m, SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var balance = await repo.GetCurrentBalanceAsync(101, schoolId);

        Assert.Equal(750m, balance);
    }

    [Fact]
    public async Task RecordFeeCollectionAsync_AddsPaymentAndUpdatesLedger()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.Add(new StudentFeeLedgerEntity
            {
                StudentID = "101",
                TermID = 1,
                TotalExpectedAmount = 1000m,
                TotalPaidAmount = 250m,
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        await repo.RecordFeeCollectionAsync(new PaymentRecordEntity
        {
            StudentID = 101,
            StudentName = "Ama Mensah",
            ClassID = "BASIC 1",
            AmountPaid = 150m,
            PaymentDate = new DateTime(2026, 6, 29)
        }, newBalance: 600m, schoolId);

        using var verify = new AppDbContext(options);
        var payment = Assert.Single(verify.PaymentRecords);
        Assert.Equal(600m, payment.Balance);
        Assert.Equal(schoolId, payment.SchoolId);

        var ledger = Assert.Single(verify.StudentFeeLedgers.Where(l => l.StudentID == "101" && l.SchoolId == schoolId));
        Assert.Equal(400m, ledger.TotalPaidAmount);
        Assert.Equal(1000m, ledger.TotalExpectedAmount);
    }

    [Fact]
    public async Task RecordFeeCollectionAsync_WritesAuditLog()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var factory = new TestDbContextFactory(options);
        var audit = new AuditLogService(factory);
        var repo = new FinancePortalRepository(factory, audit);

        await repo.RecordFeeCollectionAsync(new PaymentRecordEntity
        {
            StudentID = 101,
            StudentName = "Ama Mensah",
            ClassID = "BASIC 1",
            AmountPaid = 150m,
            PaymentDate = new DateTime(2026, 6, 29)
        }, newBalance: 600m, schoolId, "cashier");

        using var verify = new AppDbContext(options);
        var log = Assert.Single(verify.AuditLogs);
        Assert.Equal("FeePaymentRecorded", log.Action);
        Assert.Equal("cashier", log.ActorUsername);
        Assert.Contains("Ama Mensah", log.Summary);
    }

    [Fact]
    public async Task RecordFeeCollectionAsync_IgnoresDuplicatePaymentReference()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.Add(new StudentFeeLedgerEntity
            {
                StudentID = "101",
                TermID = 1,
                TotalExpectedAmount = 1000m,
                TotalPaidAmount = 250m,
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        var payment = new PaymentRecordEntity
        {
            StudentID = 101,
            StudentName = "Ama Mensah",
            ClassID = "BASIC 1",
            AmountPaid = 150m,
            PaymentDate = new DateTime(2026, 6, 29),
            PaymentReference = "FEE-101-12345"
        };

        await repo.RecordFeeCollectionAsync(payment, newBalance: 600m, schoolId);
        await repo.RecordFeeCollectionAsync(new PaymentRecordEntity
        {
            StudentID = 101,
            StudentName = "Ama Mensah",
            ClassID = "BASIC 1",
            AmountPaid = 150m,
            PaymentDate = new DateTime(2026, 6, 29),
            PaymentReference = "FEE-101-12345"
        }, newBalance: 450m, schoolId);

        using var verify = new AppDbContext(options);
        Assert.Single(verify.PaymentRecords);
        var ledger = Assert.Single(verify.StudentFeeLedgers.Where(l => l.StudentID == "101" && l.SchoolId == schoolId));
        Assert.Equal(400m, ledger.TotalPaidAmount);
    }

    [Fact]
    public async Task RecordFeeCollectionAsync_RejectsInvalidPaymentAmount()
    {
        var options = NewInMemoryOptions();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.RecordFeeCollectionAsync(new PaymentRecordEntity
        {
            StudentID = 101,
            AmountPaid = 0
        }, newBalance: 100m));
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_FiltersPagesAndTotalsBySchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.PaymentRecords.AddRange(
                new PaymentRecordEntity { StudentID = 101, StudentName = "Ama Mensah", ClassID = "BASIC 1", AmountPaid = 200m, Balance = 800m, PaymentDate = new DateTime(2026, 6, 1), SchoolId = schoolId },
                new PaymentRecordEntity { StudentID = 102, StudentName = "Kofi Boateng", ClassID = "BASIC 2", AmountPaid = 150m, Balance = 650m, PaymentDate = new DateTime(2026, 6, 2), SchoolId = schoolId },
                new PaymentRecordEntity { StudentID = 103, StudentName = "Ama Other", ClassID = "BASIC 1", AmountPaid = 999m, Balance = 1m, PaymentDate = new DateTime(2026, 6, 3), SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var result = await repo.GetPaymentHistoryAsync("BASIC", new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), page: 1, pageSize: 1, schoolId);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(350m, result.TotalPaid);
        Assert.Equal(1450m, result.TotalBalance);
        Assert.Equal(102, result.Items[0].StudentID);
    }

    [Fact]
    public async Task RecordExpenseAsync_ValidatesAmountAndStampsSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        await repo.RecordExpenseAsync(new ExpenseEntity
        {
            ExpenseName = "Books",
            Purpose = "Stationery",
            Amount = "125.50",
            Date = new DateTime(2026, 6, 29)
        }, schoolId);

        using var verify = new AppDbContext(options);
        var expense = Assert.Single(verify.Expenses);
        Assert.Equal(schoolId, expense.SchoolId);
        Assert.Equal("125.50", expense.Amount);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.RecordExpenseAsync(new ExpenseEntity
        {
            ExpenseName = "Invalid",
            Purpose = "Test",
            Amount = "0"
        }, schoolId));
    }

    [Fact]
    public async Task RecordExpenseAsync_WritesAuditLog()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var factory = new TestDbContextFactory(options);
        var audit = new AuditLogService(factory);
        var repo = new FinancePortalRepository(factory, audit);

        await repo.RecordExpenseAsync(new ExpenseEntity
        {
            ExpenseName = "Books",
            Purpose = "Stationery",
            Amount = "125.50",
            Date = new DateTime(2026, 6, 29),
            Payer = "bursar"
        }, schoolId, "bursar");

        using var verify = new AppDbContext(options);
        var log = Assert.Single(verify.AuditLogs);
        Assert.Equal("ExpenseRecorded", log.Action);
        Assert.Equal("Expense", log.EntityType);
        Assert.Equal("bursar", log.ActorUsername);
    }

    [Fact]
    public async Task GetExpenseRegisterAsync_FiltersPagesAndTotalsBySchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new FinancePortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Expenses.AddRange(
                new ExpenseEntity { ExpenseName = "Books", Purpose = "Stationery", Amount = "125.50", Date = new DateTime(2026, 6, 1), Payee = "Supplier", SchoolId = schoolId },
                new ExpenseEntity { ExpenseName = "Pens", Purpose = "Stationery", Amount = "74.50", Date = new DateTime(2026, 6, 2), Payee = "Supplier", SchoolId = schoolId },
                new ExpenseEntity { ExpenseName = "Books", Purpose = "Stationery", Amount = "999", Date = new DateTime(2026, 6, 3), Payee = "Other", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var result = await repo.GetExpenseRegisterAsync("Stationery", new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), page: 1, pageSize: 1, schoolId);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(200m, result.TotalAmount);
        Assert.Equal("Pens", result.Items[0].ExpenseName);
    }
}
