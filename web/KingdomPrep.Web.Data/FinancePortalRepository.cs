using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KingdomPrep.Web.Data.Entities;

namespace KingdomPrep.Web.Data;

public sealed record FinancePaymentHistoryResult(
    List<PaymentRecordEntity> Items,
    int TotalCount,
    decimal TotalPaid,
    decimal TotalBalance);

public sealed record FinanceExpenseRegisterResult(
    List<ExpenseEntity> Items,
    int TotalCount,
    decimal TotalAmount);

public interface IFinancePortalRepository
{
    Task RecordExpenseAsync(ExpenseEntity expense, Guid? schoolId = null, string? actorUsername = null);
    Task<List<ExpenseEntity>> GetRecentExpensesAsync(int count = 20, Guid? schoolId = null);
    Task<FinanceExpenseRegisterResult> GetExpenseRegisterAsync(string? searchTerm = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 25, Guid? schoolId = null);
    Task<decimal> GetCurrentBalanceAsync(int studentId, Guid? schoolId = null);
    Task<List<PaymentRecordEntity>> GetStudentPaymentHistoryAsync(int studentId, int count = 20, Guid? schoolId = null);
    Task<FinancePaymentHistoryResult> GetPaymentHistoryAsync(string? searchTerm = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 25, Guid? schoolId = null);
    Task RecordFeeCollectionAsync(PaymentRecordEntity payment, decimal newBalance, Guid? schoolId = null, string? actorUsername = null);
}

public class FinancePortalRepository(IDbContextFactory<AppDbContext> factory, IAuditLogService? audit = null) : IFinancePortalRepository
{
    private const string EnsurePaymentReferenceSql = """
IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL AND COL_LENGTH('payment_record', 'PaymentReference') IS NULL
    ALTER TABLE payment_record ADD PaymentReference NVARCHAR(100) NULL;
IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL
   AND COL_LENGTH('payment_record', 'PaymentReference') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_payment_record_PaymentReference' AND object_id = OBJECT_ID(N'payment_record'))
   AND NOT EXISTS (
        SELECT 1 FROM payment_record
        WHERE PaymentReference IS NOT NULL AND LTRIM(RTRIM(PaymentReference)) <> ''
        GROUP BY PaymentReference
        HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UX_payment_record_PaymentReference ON payment_record(PaymentReference)
    WHERE PaymentReference IS NOT NULL AND PaymentReference <> '';
""";

    public async Task RecordExpenseAsync(ExpenseEntity expense, Guid? schoolId = null, string? actorUsername = null)
    {
        if (expense == null) throw new ArgumentNullException(nameof(expense));
        if (!decimal.TryParse(expense.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            throw new ArgumentException("Expense amount must be greater than zero.", nameof(expense));
        }

        if (schoolId.HasValue) expense.SchoolId = schoolId.Value;
        if (expense.Date == default) expense.Date = DateTime.Now;

        using var db = await factory.CreateDbContextAsync();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? expense.Payer ?? "system",
                "ExpenseRecorded",
                "Expense",
                expense.ID.ToString(CultureInfo.InvariantCulture),
                $"Recorded expense {expense.ExpenseName} for GHS {amount:N2}.",
                expense.SchoolId ?? schoolId));
        }
    }

    public async Task<List<ExpenseEntity>> GetRecentExpensesAsync(int count = 20, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);
        return await query
            .OrderByDescending(e => e.Date)
            .Take(count)
            .ToListAsync();
    }

    public async Task<FinanceExpenseRegisterResult> GetExpenseRegisterAsync(string? searchTerm = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 25, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);
        if (from.HasValue) query = query.Where(e => e.Date >= from.Value.Date);
        if (to.HasValue) query = query.Where(e => e.Date < to.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e =>
                e.ExpenseName.Contains(term) ||
                e.Purpose.Contains(term) ||
                (e.Payee != null && e.Payee.Contains(term)) ||
                (e.Payer != null && e.Payer.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var amounts = await query.Select(e => e.Amount).ToListAsync();
        var totalAmount = amounts.Sum(ParseExpenseAmount);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new FinanceExpenseRegisterResult(items, totalCount, totalAmount);
    }

    public async Task<decimal> GetCurrentBalanceAsync(int studentId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var studentIdText = studentId.ToString(CultureInfo.InvariantCulture);

        var ledgerQuery = db.StudentFeeLedgers.AsNoTracking().Where(l => l.StudentID == studentIdText);
        if (schoolId.HasValue) ledgerQuery = ledgerQuery.Where(l => l.SchoolId == schoolId.Value);

        var ledger = await ledgerQuery.OrderByDescending(l => l.TermID).FirstOrDefaultAsync();
        if (ledger != null)
        {
            return Math.Max(0, ledger.TotalExpectedAmount - ledger.TotalPaidAmount);
        }

        var paymentQuery = db.PaymentRecords.AsNoTracking().Where(p => p.StudentID == studentId);
        if (schoolId.HasValue) paymentQuery = paymentQuery.Where(p => p.SchoolId == schoolId.Value);

        var latestPayment = await paymentQuery
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ID)
            .FirstOrDefaultAsync();

        return latestPayment == null ? 0 : Math.Max(0, latestPayment.Balance);
    }

    public async Task<List<PaymentRecordEntity>> GetStudentPaymentHistoryAsync(int studentId, int count = 20, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.PaymentRecords.AsNoTracking().Where(p => p.StudentID == studentId);
        if (schoolId.HasValue) query = query.Where(p => p.SchoolId == schoolId.Value);

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ID)
            .Take(Math.Clamp(count, 1, 100))
            .ToListAsync();
    }

    public async Task<FinancePaymentHistoryResult> GetPaymentHistoryAsync(string? searchTerm = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 25, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.PaymentRecords.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(p => p.SchoolId == schoolId.Value);
        if (from.HasValue) query = query.Where(p => p.PaymentDate.HasValue && p.PaymentDate.Value >= from.Value.Date);
        if (to.HasValue) query = query.Where(p => p.PaymentDate.HasValue && p.PaymentDate.Value < to.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            if (int.TryParse(term, NumberStyles.Integer, CultureInfo.InvariantCulture, out var studentId))
            {
                query = query.Where(p => p.StudentID == studentId || (p.StudentName != null && p.StudentName.Contains(term)) || (p.ClassID != null && p.ClassID.Contains(term)));
            }
            else
            {
                query = query.Where(p => (p.StudentName != null && p.StudentName.Contains(term)) || (p.ClassID != null && p.ClassID.Contains(term)));
            }
        }

        var totalCount = await query.CountAsync();
        var totalPaid = await query.SumAsync(p => p.AmountPaid);
        var totalBalance = await query.SumAsync(p => p.Balance);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new FinancePaymentHistoryResult(items, totalCount, totalPaid, totalBalance);
    }

    public async Task RecordFeeCollectionAsync(PaymentRecordEntity payment, decimal newBalance, Guid? schoolId = null, string? actorUsername = null)
    {
        if (payment == null) throw new ArgumentNullException(nameof(payment));
        if (payment.StudentID <= 0) throw new ArgumentException("Student ID is required.", nameof(payment));
        if (payment.AmountPaid <= 0) throw new ArgumentException("Payment amount must be greater than zero.", nameof(payment));

        using var db = await factory.CreateDbContextAsync();
        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlRawAsync(EnsurePaymentReferenceSql);
        }

        if (schoolId.HasValue)
        {
            payment.SchoolId = schoolId.Value;
        }

        payment.PaymentReference = string.IsNullOrWhiteSpace(payment.PaymentReference) ? null : payment.PaymentReference.Trim();
        if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
        {
            var existing = db.PaymentRecords.AsNoTracking()
                .Where(p => p.PaymentReference == payment.PaymentReference);
            if (payment.SchoolId.HasValue) existing = existing.Where(p => p.SchoolId == payment.SchoolId.Value);
            if (await existing.AnyAsync()) return;
        }

        if (payment.PaymentDate == null) payment.PaymentDate = DateTime.Now;
        payment.Balance = Math.Max(0, newBalance);

        db.PaymentRecords.Add(payment);

        var studentId = payment.StudentID.ToString(CultureInfo.InvariantCulture);
        var ledgerQuery = db.StudentFeeLedgers.Where(l => l.StudentID == studentId);
        if (payment.SchoolId.HasValue) ledgerQuery = ledgerQuery.Where(l => l.SchoolId == payment.SchoolId.Value);
        var ledger = await ledgerQuery.OrderByDescending(l => l.TermID).FirstOrDefaultAsync();
        if (ledger != null)
        {
            ledger.TotalPaidAmount += payment.AmountPaid;
            var expectedAfterPayment = ledger.TotalPaidAmount + payment.Balance;
            if (ledger.TotalExpectedAmount < expectedAfterPayment)
            {
                ledger.TotalExpectedAmount = expectedAfterPayment;
            }

            db.StudentFeeLedgers.Update(ledger);
        }
        else
        {
            db.StudentFeeLedgers.Add(new StudentFeeLedgerEntity
            {
                StudentID = studentId,
                TotalExpectedAmount = payment.AmountPaid + payment.Balance,
                TotalPaidAmount = payment.AmountPaid,
                SchoolId = payment.SchoolId
            });
        }

        await db.SaveChangesAsync();

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                "FeePaymentRecorded",
                "PaymentRecord",
                payment.ID.ToString(CultureInfo.InvariantCulture),
                $"Recorded GHS {payment.AmountPaid:N2} payment for {payment.StudentName ?? payment.StudentID.ToString(CultureInfo.InvariantCulture)}.",
                payment.SchoolId ?? schoolId));
        }
    }

    private static decimal ParseExpenseAmount(string? amount)
    {
        return decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }
}
