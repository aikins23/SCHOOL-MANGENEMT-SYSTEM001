using KingdomPrep.Web.Data.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public sealed record PaymentGatewaySettingsView(
    string PaystackPublicKey,
    bool HasPaystackSecretKey,
    bool HasMomoSubscriptionKey,
    bool HasMomoApiUser,
    bool HasMomoApiKey,
    string MomoTargetEnvironment,
    string MomoBaseUrl,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy)
{
    public bool PaystackConfigured => PaystackPublicKey.StartsWith("pk_", StringComparison.Ordinal) && HasPaystackSecretKey;
    public bool MomoConfigured => HasMomoSubscriptionKey && HasMomoApiUser && HasMomoApiKey;
}

public sealed record PaymentGatewaySettingsUpdate(
    string PaystackPublicKey,
    string PaystackSecretKey,
    string MomoSubscriptionKey,
    string MomoApiUser,
    string MomoApiKey,
    string MomoTargetEnvironment,
    string MomoBaseUrl);

public sealed record PaymentGatewayRuntimeSettings(
    string PaystackPublicKey,
    string PaystackSecretKey,
    string MomoSubscriptionKey,
    string MomoApiUser,
    string MomoApiKey,
    string MomoTargetEnvironment,
    string MomoBaseUrl)
{
    public bool PaystackConfigured => PaystackPublicKey.StartsWith("pk_", StringComparison.Ordinal) && PaystackSecretKey.StartsWith("sk_", StringComparison.Ordinal);
    public bool MomoConfigured => !string.IsNullOrWhiteSpace(MomoSubscriptionKey) && !string.IsNullOrWhiteSpace(MomoApiUser) && !string.IsNullOrWhiteSpace(MomoApiKey);
}

public sealed record PaystackWebhookSecretCandidate(Guid? SchoolId, string SecretKey);

public interface IPaymentGatewaySettingsService
{
    Task<PaymentGatewaySettingsView> GetForEditAsync(Guid? schoolId, CancellationToken cancellationToken = default);
    Task<PaymentGatewayRuntimeSettings> GetRuntimeSettingsAsync(Guid? schoolId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaystackWebhookSecretCandidate>> GetPaystackWebhookSecretCandidatesAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Guid? schoolId, PaymentGatewaySettingsUpdate update, string? actorUsername, CancellationToken cancellationToken = default);
}

public sealed class PaymentGatewaySettingsService(
    IDbContextFactory<AppDbContext> dbFactory,
    IConfiguration configuration) : IPaymentGatewaySettingsService
{
    private const string EnsureTableSql = """
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NULL
BEGIN
    CREATE TABLE SchoolPaymentSettings (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        PaystackPublicKey NVARCHAR(200) NULL,
        PaystackSecretKey NVARCHAR(500) NULL,
        MomoSubscriptionKey NVARCHAR(500) NULL,
        MomoApiUser NVARCHAR(200) NULL,
        MomoApiKey NVARCHAR(500) NULL,
        MomoTargetEnvironment NVARCHAR(50) NULL,
        MomoBaseUrl NVARCHAR(300) NULL,
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SchoolPaymentSettings_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedBy NVARCHAR(150) NULL
    );
END
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'PaystackPublicKey') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD PaystackPublicKey NVARCHAR(200) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'PaystackSecretKey') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD PaystackSecretKey NVARCHAR(500) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'MomoSubscriptionKey') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD MomoSubscriptionKey NVARCHAR(500) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'MomoApiUser') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD MomoApiUser NVARCHAR(200) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'MomoApiKey') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD MomoApiKey NVARCHAR(500) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'MomoTargetEnvironment') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD MomoTargetEnvironment NVARCHAR(50) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'MomoBaseUrl') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD MomoBaseUrl NVARCHAR(300) NULL;
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'UpdatedAtUtc') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SchoolPaymentSettings_UpdatedAtUtc_Upgrade DEFAULT SYSUTCDATETIME();
IF OBJECT_ID(N'SchoolPaymentSettings', N'U') IS NOT NULL AND COL_LENGTH('SchoolPaymentSettings', 'UpdatedBy') IS NULL
    ALTER TABLE SchoolPaymentSettings ADD UpdatedBy NVARCHAR(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SchoolPaymentSettings_SchoolId' AND object_id = OBJECT_ID(N'SchoolPaymentSettings'))
   AND NOT EXISTS (
        SELECT 1 FROM SchoolPaymentSettings
        GROUP BY SchoolId
        HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UX_SchoolPaymentSettings_SchoolId ON SchoolPaymentSettings(SchoolId);
""";

    public async Task<PaymentGatewaySettingsView> GetForEditAsync(Guid? schoolId, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityAsync(schoolId, cancellationToken);
        var fallback = GetFallbackSettings();

        return new PaymentGatewaySettingsView(
            entity?.PaystackPublicKey?.Trim() ?? fallback.PaystackPublicKey,
            !string.IsNullOrWhiteSpace(entity?.PaystackSecretKey) || !string.IsNullOrWhiteSpace(fallback.PaystackSecretKey),
            !string.IsNullOrWhiteSpace(entity?.MomoSubscriptionKey) || !string.IsNullOrWhiteSpace(fallback.MomoSubscriptionKey),
            !string.IsNullOrWhiteSpace(entity?.MomoApiUser) || !string.IsNullOrWhiteSpace(fallback.MomoApiUser),
            !string.IsNullOrWhiteSpace(entity?.MomoApiKey) || !string.IsNullOrWhiteSpace(fallback.MomoApiKey),
            entity?.MomoTargetEnvironment?.Trim() ?? fallback.MomoTargetEnvironment,
            entity?.MomoBaseUrl?.Trim() ?? fallback.MomoBaseUrl,
            entity?.UpdatedAtUtc,
            entity?.UpdatedBy);
    }

    public async Task<PaymentGatewayRuntimeSettings> GetRuntimeSettingsAsync(Guid? schoolId, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityAsync(schoolId, cancellationToken);
        var fallback = GetFallbackSettings();

        return new PaymentGatewayRuntimeSettings(
            FirstConfigured(entity?.PaystackPublicKey, fallback.PaystackPublicKey),
            FirstConfigured(entity?.PaystackSecretKey, fallback.PaystackSecretKey),
            FirstConfigured(entity?.MomoSubscriptionKey, fallback.MomoSubscriptionKey),
            FirstConfigured(entity?.MomoApiUser, fallback.MomoApiUser),
            FirstConfigured(entity?.MomoApiKey, fallback.MomoApiKey),
            FirstConfigured(entity?.MomoTargetEnvironment, fallback.MomoTargetEnvironment, "sandbox"),
            FirstConfigured(entity?.MomoBaseUrl, fallback.MomoBaseUrl, "https://sandbox.momodeveloper.mtn.com"));
    }

    public async Task<IReadOnlyList<PaystackWebhookSecretCandidate>> GetPaystackWebhookSecretCandidatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureSchemaAsync(db, cancellationToken);

        var candidates = await db.SchoolPaymentSettings
            .AsNoTracking()
            .Where(s => s.PaystackSecretKey != null && s.PaystackSecretKey != "")
            .Select(s => new PaystackWebhookSecretCandidate(s.SchoolId, s.PaystackSecretKey!.Trim()))
            .ToListAsync(cancellationToken);

        var fallbackSecret = configuration["Paystack:SecretKey"]?.Trim();
        if (!string.IsNullOrWhiteSpace(fallbackSecret))
        {
            candidates.Add(new PaystackWebhookSecretCandidate(null, fallbackSecret));
        }

        return candidates
            .Where(c => c.SecretKey.StartsWith("sk_", StringComparison.Ordinal))
            .GroupBy(c => c.SecretKey, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();
    }

    public async Task SaveAsync(Guid? schoolId, PaymentGatewaySettingsUpdate update, string? actorUsername, CancellationToken cancellationToken = default)
    {
        if (!schoolId.HasValue)
        {
            throw new InvalidOperationException("Your account is not assigned to a school, so private payment settings cannot be saved.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureSchemaAsync(db, cancellationToken);

        var entity = await db.SchoolPaymentSettings.FirstOrDefaultAsync(s => s.SchoolId == schoolId.Value, cancellationToken);
        if (entity == null)
        {
            entity = new SchoolPaymentSettingsEntity { SchoolId = schoolId.Value };
            db.SchoolPaymentSettings.Add(entity);
        }

        entity.PaystackPublicKey = EmptyToNull(update.PaystackPublicKey);
        if (!string.IsNullOrWhiteSpace(update.PaystackSecretKey)) entity.PaystackSecretKey = update.PaystackSecretKey.Trim();
        if (!string.IsNullOrWhiteSpace(update.MomoSubscriptionKey)) entity.MomoSubscriptionKey = update.MomoSubscriptionKey.Trim();
        if (!string.IsNullOrWhiteSpace(update.MomoApiUser)) entity.MomoApiUser = update.MomoApiUser.Trim();
        if (!string.IsNullOrWhiteSpace(update.MomoApiKey)) entity.MomoApiKey = update.MomoApiKey.Trim();
        entity.MomoTargetEnvironment = EmptyToNull(update.MomoTargetEnvironment) ?? "sandbox";
        entity.MomoBaseUrl = EmptyToNull(update.MomoBaseUrl) ?? "https://sandbox.momodeveloper.mtn.com";
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.UpdatedBy = EmptyToNull(actorUsername);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SchoolPaymentSettingsEntity?> GetEntityAsync(Guid? schoolId, CancellationToken cancellationToken)
    {
        if (!schoolId.HasValue)
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureSchemaAsync(db, cancellationToken);
        return await db.SchoolPaymentSettings.AsNoTracking().FirstOrDefaultAsync(s => s.SchoolId == schoolId.Value, cancellationToken);
    }

    private static async Task EnsureSchemaAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(EnsureTableSql, cancellationToken);
    }

    private PaymentGatewayRuntimeSettings GetFallbackSettings() => new(
        configuration["Paystack:PublicKey"]?.Trim() ?? "",
        configuration["Paystack:SecretKey"]?.Trim() ?? "",
        configuration["Momo:SubscriptionKey"]?.Trim() ?? "",
        configuration["Momo:ApiUser"]?.Trim() ?? "",
        configuration["Momo:ApiKey"]?.Trim() ?? "",
        configuration["Momo:TargetEnvironment"]?.Trim() ?? "sandbox",
        configuration["Momo:BaseUrl"]?.Trim() ?? "https://sandbox.momodeveloper.mtn.com");

    private static string FirstConfigured(params string?[] values)
        => values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
