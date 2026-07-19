using KingdomPrep.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace KingdomPrep.Web.Api;

public sealed class SyncDeviceRegistryService(
    AppDbContext db,
    IConfiguration config,
    ILogger<SyncDeviceRegistryService> logger,
    IAuditLogService? audit = null)
{
    public const int MinimumSyncApiKeyLength = 32;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
IF OBJECT_ID(N'SyncRegisteredDevices', N'U') IS NULL
BEGIN
    CREATE TABLE SyncRegisteredDevices (
        RegistrationId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        DeviceId UNIQUEIDENTIFIER NOT NULL,
        DeviceName NVARCHAR(120) NULL,
        ApiKeyHash VARBINARY(32) NOT NULL,
        ApiKeyLast4 NVARCHAR(12) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SyncRegisteredDevices_IsActive DEFAULT (1),
        LicenseStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_SyncRegisteredDevices_LicenseStatus DEFAULT ('Active'),
        ExpiresAt DATETIME2 NULL,
        RegisteredAt DATETIME2 NOT NULL CONSTRAINT DF_SyncRegisteredDevices_RegisteredAt DEFAULT SYSUTCDATETIME(),
        LastSeenAt DATETIME2 NULL
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SyncRegisteredDevices_School_Device' AND object_id = OBJECT_ID(N'SyncRegisteredDevices'))
    CREATE UNIQUE INDEX UX_SyncRegisteredDevices_School_Device ON SyncRegisteredDevices(SchoolId, DeviceId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncRegisteredDevices_School_Active' AND object_id = OBJECT_ID(N'SyncRegisteredDevices'))
    CREATE INDEX IX_SyncRegisteredDevices_School_Active ON SyncRegisteredDevices(SchoolId, IsActive, LicenseStatus);
""";

        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task<bool> HasRegisteredDevicesAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM SyncRegisteredDevices";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<SyncDeviceAuthorizationResult> AuthorizeAsync(
        Guid schoolId,
        Guid deviceId,
        string? suppliedApiKey,
        CancellationToken cancellationToken)
    {
        if (schoolId == Guid.Empty)
            return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status400BadRequest, "SchoolId is required.");
        if (deviceId == Guid.Empty)
            return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status400BadRequest, "DeviceId is required.");
        if (string.IsNullOrWhiteSpace(suppliedApiKey))
            return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status401Unauthorized, "Sync API key is required.");

        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
SELECT TOP 1 ApiKeyHash, IsActive, LicenseStatus, ExpiresAt
FROM SyncRegisteredDevices
WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId
""";
            command.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
            command.Parameters.Add(new SqlParameter("@DeviceId", deviceId));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var storedHash = reader["ApiKeyHash"] as byte[] ?? [];
                var isActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
                var licenseStatus = reader["LicenseStatus"]?.ToString() ?? "";
                var expiresAt = reader["ExpiresAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ExpiresAt"]);

                if (!FixedTimeEquals(storedHash, ComputeApiKeyHash(suppliedApiKey)))
                    return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status401Unauthorized, "Sync API key does not match this registered device.");
                if (!isActive)
                    return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status403Forbidden, "This sync device is inactive.");
                if (!string.Equals(licenseStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status403Forbidden, "This school's sync license is not active.");
                if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
                    return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status403Forbidden, "This school's sync license has expired.");

                await reader.DisposeAsync();
                await TouchDeviceAsync(connection, schoolId, deviceId, cancellationToken);
                return SyncDeviceAuthorizationResult.Success("RegisteredDevice");
            }
        }

        if (AllowSharedKeyFallback() && FixedTimeEquals(config["Sync:ApiKey"], suppliedApiKey))
        {
            logger.LogWarning("Sync shared-key fallback used for unregistered device {DeviceId} / school {SchoolId}", deviceId, schoolId);
            return SyncDeviceAuthorizationResult.Success("SharedKeyFallback");
        }

        return SyncDeviceAuthorizationResult.Fail(StatusCodes.Status401Unauthorized, "This desktop device is not registered for the supplied school.");
    }

    public async Task<SyncDeviceRegistrationResponse> RegisterAsync(
        SyncDeviceRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SchoolId == Guid.Empty)
            return SyncDeviceRegistrationResponse.Failed("SchoolId is required.");
        if (request.DeviceId == Guid.Empty)
            return SyncDeviceRegistrationResponse.Failed("DeviceId is required.");
        var keyValidationMessage = ValidateSyncApiKey(
            request.SyncApiKey,
            config["Sync:ApiKey"],
            config["Sync:ProvisioningKey"]);
        if (keyValidationMessage != null)
            return SyncDeviceRegistrationResponse.Failed(keyValidationMessage);

        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
IF EXISTS (SELECT 1 FROM SyncRegisteredDevices WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId)
BEGIN
    UPDATE SyncRegisteredDevices
    SET DeviceName = @DeviceName,
        ApiKeyHash = @ApiKeyHash,
        ApiKeyLast4 = @ApiKeyLast4,
        IsActive = @IsActive,
        LicenseStatus = @LicenseStatus,
        ExpiresAt = @ExpiresAt
    WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId;
END
ELSE
BEGIN
    INSERT INTO SyncRegisteredDevices
        (SchoolId, DeviceId, DeviceName, ApiKeyHash, ApiKeyLast4, IsActive, LicenseStatus, ExpiresAt, RegisteredAt)
    VALUES
        (@SchoolId, @DeviceId, @DeviceName, @ApiKeyHash, @ApiKeyLast4, @IsActive, @LicenseStatus, @ExpiresAt, SYSUTCDATETIME());
END
""";
        command.Parameters.Add(new SqlParameter("@SchoolId", request.SchoolId));
        command.Parameters.Add(new SqlParameter("@DeviceId", request.DeviceId));
        command.Parameters.Add(new SqlParameter("@DeviceName", (object?)request.DeviceName ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ApiKeyHash", ComputeApiKeyHash(request.SyncApiKey)));
        command.Parameters.Add(new SqlParameter("@ApiKeyLast4", Last4(request.SyncApiKey)));
        command.Parameters.Add(new SqlParameter("@IsActive", request.IsActive));
        command.Parameters.Add(new SqlParameter("@LicenseStatus", string.IsNullOrWhiteSpace(request.LicenseStatus) ? "Active" : request.LicenseStatus.Trim()));
        command.Parameters.Add(new SqlParameter("@ExpiresAt", (object?)request.ExpiresAtUtc ?? DBNull.Value));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new SyncDeviceRegistrationResponse
        {
            Ok = true,
            SchoolId = request.SchoolId,
            DeviceId = request.DeviceId,
            ApiKeyLast4 = Last4(request.SyncApiKey),
            Message = "Device registered for school sync."
        };
    }

    public async Task<SyncDeviceStatusUpdateResult> SetDeviceActiveAsync(
        Guid schoolId,
        Guid deviceId,
        bool isActive,
        string? actorUsername,
        CancellationToken cancellationToken)
    {
        if (schoolId == Guid.Empty)
            return SyncDeviceStatusUpdateResult.Failed("SchoolId is required.");
        if (deviceId == Guid.Empty)
            return SyncDeviceStatusUpdateResult.Failed("DeviceId is required.");

        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE SyncRegisteredDevices
SET IsActive = @IsActive
WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId
""";
        command.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
        command.Parameters.Add(new SqlParameter("@DeviceId", deviceId));
        command.Parameters.Add(new SqlParameter("@IsActive", isActive));

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
            return SyncDeviceStatusUpdateResult.Failed("Registered sync device was not found.");

        if (audit != null)
        {
            var action = isActive ? "SyncDeviceActivated" : "SyncDeviceDeactivated";
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                action,
                "SyncRegisteredDevice",
                deviceId.ToString(),
                isActive ? "Activated desktop sync device." : "Deactivated desktop sync device.",
                schoolId));
        }

        return new SyncDeviceStatusUpdateResult(
            true,
            isActive ? "Sync device activated." : "Sync device deactivated.");
    }

    private async Task TouchDeviceAsync(System.Data.Common.DbConnection connection, Guid schoolId, Guid deviceId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE SyncRegisteredDevices
SET LastSeenAt = SYSUTCDATETIME()
WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId
""";
        command.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
        command.Parameters.Add(new SqlParameter("@DeviceId", deviceId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private bool AllowSharedKeyFallback() =>
        bool.TryParse(config["Sync:AllowSharedApiKeyFallback"], out var enabled)
        && enabled
        && !string.IsNullOrWhiteSpace(config["Sync:ApiKey"]);

    public static string? ValidateSyncApiKey(string? syncApiKey, string? sharedSyncApiKey = null, string? provisioningKey = null)
    {
        var key = syncApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(key))
            return "A unique sync API key is required.";
        if (key.Length < MinimumSyncApiKeyLength)
            return $"Sync API key must be at least {MinimumSyncApiKeyLength} characters.";
        if (FixedTimeEquals(sharedSyncApiKey, key))
            return "Device sync API key cannot reuse the shared sync key.";
        if (FixedTimeEquals(provisioningKey, key))
            return "Device sync API key cannot reuse the provisioning key.";

        return null;
    }

    private static byte[] ComputeApiKeyHash(string apiKey) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.Trim()));

    private static bool FixedTimeEquals(string? expected, string? supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied)) return false;
        return FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied));
    }

    private static bool FixedTimeEquals(byte[] expected, byte[] supplied) =>
        expected.Length == supplied.Length && CryptographicOperations.FixedTimeEquals(expected, supplied);

    private static string Last4(string value)
    {
        value = (value ?? "").Trim();
        return value.Length <= 4 ? value : value.Substring(value.Length - 4);
    }
}

public sealed class SyncDeviceAuthorizationResult
{
    public bool Ok { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = "";
    public string Mode { get; init; } = "";

    public static SyncDeviceAuthorizationResult Success(string mode) =>
        new() { Ok = true, StatusCode = StatusCodes.Status200OK, Mode = mode };

    public static SyncDeviceAuthorizationResult Fail(int statusCode, string message) =>
        new() { Ok = false, StatusCode = statusCode, Message = message };
}

public sealed record SyncDeviceStatusUpdateResult(bool Ok, string Message)
{
    public static SyncDeviceStatusUpdateResult Failed(string message) => new(false, message);
}
