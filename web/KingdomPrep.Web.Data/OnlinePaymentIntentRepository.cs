using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IOnlinePaymentIntentRepository
{
    Task<OnlinePaymentIntentEntity> CreateAsync(OnlinePaymentIntentEntity intent, CancellationToken cancellationToken = default);
    Task<OnlinePaymentIntentEntity?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed class OnlinePaymentIntentRepository(IDbContextFactory<AppDbContext> factory) : IOnlinePaymentIntentRepository
{
    private const string EnsureTableSql = """
IF OBJECT_ID(N'OnlinePaymentIntents', N'U') IS NULL
BEGIN
    CREATE TABLE OnlinePaymentIntents (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Reference NVARCHAR(100) NOT NULL,
        SchoolId UNIQUEIDENTIFIER NULL,
        StudentID INT NOT NULL,
        ClassID NVARCHAR(100) NULL,
        StudentName NVARCHAR(250) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        BalanceBeforePayment DECIMAL(18,2) NOT NULL,
        Gateway NVARCHAR(30) NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_OnlinePaymentIntents_Status DEFAULT N'Pending',
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_OnlinePaymentIntents_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CompletedAtUtc DATETIME2 NULL,
        CreatedBy NVARCHAR(150) NULL
    );
END
IF OBJECT_ID(N'OnlinePaymentIntents', N'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'UX_OnlinePaymentIntents_Reference' AND object_id = OBJECT_ID(N'OnlinePaymentIntents'))
    CREATE UNIQUE INDEX UX_OnlinePaymentIntents_Reference ON OnlinePaymentIntents(Reference);
""";

    public async Task<OnlinePaymentIntentEntity> CreateAsync(OnlinePaymentIntentEntity intent, CancellationToken cancellationToken = default)
    {
        if (intent == null) throw new ArgumentNullException(nameof(intent));
        if (string.IsNullOrWhiteSpace(intent.Reference)) throw new ArgumentException("Payment reference is required.", nameof(intent));
        if (intent.StudentID <= 0) throw new ArgumentException("Student ID is required.", nameof(intent));
        if (intent.Amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.", nameof(intent));

        intent.Reference = intent.Reference.Trim();
        intent.Gateway = string.IsNullOrWhiteSpace(intent.Gateway) ? "Unknown" : intent.Gateway.Trim();
        intent.Status = string.IsNullOrWhiteSpace(intent.Status) ? "Pending" : intent.Status.Trim();
        intent.CreatedAtUtc = intent.CreatedAtUtc == default ? DateTime.UtcNow : intent.CreatedAtUtc;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await EnsureSchemaAsync(db, cancellationToken);

        var existing = await db.OnlinePaymentIntents.FirstOrDefaultAsync(i => i.Reference == intent.Reference, cancellationToken);
        if (existing != null) return existing;

        db.OnlinePaymentIntents.Add(intent);
        await db.SaveChangesAsync(cancellationToken);
        return intent;
    }

    public async Task<OnlinePaymentIntentEntity?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await EnsureSchemaAsync(db, cancellationToken);

        var cleanReference = reference.Trim();
        return await db.OnlinePaymentIntents.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Reference == cleanReference, cancellationToken);
    }

    public async Task MarkCompletedAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference)) return;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await EnsureSchemaAsync(db, cancellationToken);

        var cleanReference = reference.Trim();
        var intent = await db.OnlinePaymentIntents.FirstOrDefaultAsync(i => i.Reference == cleanReference, cancellationToken);
        if (intent == null || string.Equals(intent.Status, "Completed", StringComparison.OrdinalIgnoreCase)) return;

        intent.Status = "Completed";
        intent.CompletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSchemaAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlRawAsync(EnsureTableSql, cancellationToken);
        }
    }
}
