using KingdomPrep.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KingdomPrep.Web.Api;

public sealed record SyncHealthSummary(
    bool SyncConfigured,
    int AllowedTableCount,
    int DeviceCount,
    int ActiveDeviceCount,
    int InactiveDeviceCount,
    int NonActiveLicenseCount,
    int InboxTotal,
    int InboxReceived,
    int InboxApplied,
    int InboxConflict,
    DateTime? LastDeviceSeenAtUtc,
    DateTime? LastInboxReceivedAtUtc,
    DateTime? LastInboxAppliedAtUtc,
    string HealthStatus,
    List<SyncDeviceHealthRow> Devices,
    List<SyncInboxStatusRow> RecentInbox,
    List<SyncCheckpointRow> Checkpoints);

public sealed record SyncDeviceHealthRow(
    Guid SchoolId,
    Guid DeviceId,
    string DeviceName,
    string ApiKeyLast4,
    bool IsActive,
    string LicenseStatus,
    DateTime? ExpiresAtUtc,
    DateTime RegisteredAtUtc,
    DateTime? LastSeenAtUtc);

public sealed record SyncInboxStatusRow(
    long InboxId,
    Guid DeviceId,
    string TableName,
    string Operation,
    string Status,
    string? ConflictReason,
    DateTime ReceivedAtUtc,
    DateTime? AppliedAtUtc);

public sealed record SyncCheckpointRow(
    string TableName,
    DateTime? LastPulledAtUtc,
    DateTime UpdatedAtUtc);

public sealed class SyncHealthService(
    AppDbContext db,
    IConfiguration config,
    SyncDeviceRegistryService registry,
    SyncInboxService inbox)
{
    public async Task<SyncHealthSummary> GetSummaryAsync(Guid? schoolId, CancellationToken cancellationToken = default)
    {
        await registry.EnsureSchemaAsync(cancellationToken);
        await inbox.EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var deviceStats = await GetDeviceStatsAsync(connection, schoolId, cancellationToken);
        var inboxStats = await GetInboxStatsAsync(connection, schoolId, cancellationToken);
        var devices = await GetDevicesAsync(connection, schoolId, cancellationToken);
        var recentInbox = await GetRecentInboxAsync(connection, schoolId, cancellationToken);
        var checkpoints = await GetCheckpointsAsync(connection, schoolId, cancellationToken);
        var health = ComputeHealthStatus(deviceStats.Active, deviceStats.LastSeenAt, inboxStats.Conflict, DateTime.UtcNow);

        return new SyncHealthSummary(
            SyncConfigured: IsConfigured(),
            AllowedTableCount: SyncTablePolicy.AllowedTables.Length,
            DeviceCount: deviceStats.Total,
            ActiveDeviceCount: deviceStats.Active,
            InactiveDeviceCount: deviceStats.Inactive,
            NonActiveLicenseCount: deviceStats.NonActiveLicense,
            InboxTotal: inboxStats.Total,
            InboxReceived: inboxStats.Received,
            InboxApplied: inboxStats.Applied,
            InboxConflict: inboxStats.Conflict,
            LastDeviceSeenAtUtc: deviceStats.LastSeenAt,
            LastInboxReceivedAtUtc: inboxStats.LastReceivedAt,
            LastInboxAppliedAtUtc: inboxStats.LastAppliedAt,
            HealthStatus: health,
            Devices: devices,
            RecentInbox: recentInbox,
            Checkpoints: checkpoints);
    }

    public static string ComputeHealthStatus(int activeDeviceCount, DateTime? lastSeenAtUtc, int conflictCount, DateTime nowUtc)
    {
        if (activeDeviceCount == 0) return "No active devices";
        if (conflictCount > 0) return "Needs attention";
        if (!lastSeenAtUtc.HasValue) return "Waiting for first sync";
        return lastSeenAtUtc.Value < nowUtc.AddDays(-2) ? "Stale" : "Healthy";
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(config["Sync:ApiKey"]) ||
        !string.IsNullOrWhiteSpace(config["Sync:ProvisioningKey"]);

    private static async Task<(int Total, int Active, int Inactive, int NonActiveLicense, DateTime? LastSeenAt)> GetDeviceStatsAsync(
        System.Data.Common.DbConnection connection,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT
    COUNT(1) AS Total,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS Active,
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS Inactive,
    SUM(CASE WHEN LicenseStatus <> 'Active' THEN 1 ELSE 0 END) AS NonActiveLicense,
    MAX(LastSeenAt) AS LastSeenAt
FROM SyncRegisteredDevices
{WhereSchool(schoolId)}
""";
        AddSchoolParameter(command, schoolId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return (0, 0, 0, 0, null);
        return (
            AsInt(reader, "Total"),
            AsInt(reader, "Active"),
            AsInt(reader, "Inactive"),
            AsInt(reader, "NonActiveLicense"),
            AsDateTime(reader, "LastSeenAt"));
    }

    private static async Task<(int Total, int Received, int Applied, int Conflict, DateTime? LastReceivedAt, DateTime? LastAppliedAt)> GetInboxStatsAsync(
        System.Data.Common.DbConnection connection,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT
    COUNT(1) AS Total,
    SUM(CASE WHEN Status = 'Received' THEN 1 ELSE 0 END) AS Received,
    SUM(CASE WHEN Status = 'Applied' THEN 1 ELSE 0 END) AS Applied,
    SUM(CASE WHEN Status = 'Conflict' THEN 1 ELSE 0 END) AS Conflict,
    MAX(ReceivedAt) AS LastReceivedAt,
    MAX(AppliedAt) AS LastAppliedAt
FROM SyncInbox
{WhereSchool(schoolId)}
""";
        AddSchoolParameter(command, schoolId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return (0, 0, 0, 0, null, null);
        return (
            AsInt(reader, "Total"),
            AsInt(reader, "Received"),
            AsInt(reader, "Applied"),
            AsInt(reader, "Conflict"),
            AsDateTime(reader, "LastReceivedAt"),
            AsDateTime(reader, "LastAppliedAt"));
    }

    private static async Task<List<SyncDeviceHealthRow>> GetDevicesAsync(
        System.Data.Common.DbConnection connection,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT TOP 20 SchoolId, DeviceId, DeviceName, ApiKeyLast4, IsActive, LicenseStatus, ExpiresAt, RegisteredAt, LastSeenAt
FROM SyncRegisteredDevices
{WhereSchool(schoolId)}
ORDER BY CASE WHEN LastSeenAt IS NULL THEN 1 ELSE 0 END, LastSeenAt DESC, RegisteredAt DESC
""";
        AddSchoolParameter(command, schoolId);

        var rows = new List<SyncDeviceHealthRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SyncDeviceHealthRow(
                reader.GetGuid(reader.GetOrdinal("SchoolId")),
                reader.GetGuid(reader.GetOrdinal("DeviceId")),
                AsString(reader, "DeviceName"),
                AsString(reader, "ApiKeyLast4"),
                AsBool(reader, "IsActive"),
                AsString(reader, "LicenseStatus"),
                AsDateTime(reader, "ExpiresAt"),
                Convert.ToDateTime(reader["RegisteredAt"]),
                AsDateTime(reader, "LastSeenAt")));
        }

        return rows;
    }

    private static async Task<List<SyncInboxStatusRow>> GetRecentInboxAsync(
        System.Data.Common.DbConnection connection,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT TOP 30 InboxId, DeviceId, TableName, Operation, Status, ConflictReason, ReceivedAt, AppliedAt
FROM SyncInbox
{WhereSchool(schoolId)}
ORDER BY ReceivedAt DESC, InboxId DESC
""";
        AddSchoolParameter(command, schoolId);

        var rows = new List<SyncInboxStatusRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SyncInboxStatusRow(
                Convert.ToInt64(reader["InboxId"]),
                reader.GetGuid(reader.GetOrdinal("DeviceId")),
                AsString(reader, "TableName"),
                AsString(reader, "Operation"),
                AsString(reader, "Status"),
                AsNullableString(reader, "ConflictReason"),
                Convert.ToDateTime(reader["ReceivedAt"]),
                AsDateTime(reader, "AppliedAt")));
        }

        return rows;
    }

    private static async Task<List<SyncCheckpointRow>> GetCheckpointsAsync(
        System.Data.Common.DbConnection connection,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "SyncCheckpoints", cancellationToken)) return [];

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT TOP 50 TableName, LastPulledAt, UpdatedAt
FROM SyncCheckpoints
{WhereSchool(schoolId)}
ORDER BY UpdatedAt DESC, TableName ASC
""";
        AddSchoolParameter(command, schoolId);

        var rows = new List<SyncCheckpointRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SyncCheckpointRow(
                AsString(reader, "TableName"),
                AsDateTime(reader, "LastPulledAt"),
                Convert.ToDateTime(reader["UpdatedAt"])));
        }

        return rows;
    }

    private static async Task<bool> TableExistsAsync(System.Data.Common.DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN OBJECT_ID(@TableName, N'U') IS NULL THEN 0 ELSE 1 END";
        command.Parameters.Add(new SqlParameter("@TableName", tableName));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static string WhereSchool(Guid? schoolId) => schoolId.HasValue ? "WHERE SchoolId = @SchoolId" : "";

    private static void AddSchoolParameter(System.Data.Common.DbCommand command, Guid? schoolId)
    {
        if (schoolId.HasValue) command.Parameters.Add(new SqlParameter("@SchoolId", schoolId.Value));
    }

    private static int AsInt(IDataRecord reader, string name) =>
        reader[name] == DBNull.Value ? 0 : Convert.ToInt32(reader[name]);

    private static bool AsBool(IDataRecord reader, string name) =>
        reader[name] != DBNull.Value && Convert.ToBoolean(reader[name]);

    private static DateTime? AsDateTime(IDataRecord reader, string name) =>
        reader[name] == DBNull.Value ? null : Convert.ToDateTime(reader[name]);

    private static string AsString(IDataRecord reader, string name) =>
        reader[name]?.ToString() ?? "";

    private static string? AsNullableString(IDataRecord reader, string name) =>
        reader[name] == DBNull.Value ? null : reader[name]?.ToString();
}
