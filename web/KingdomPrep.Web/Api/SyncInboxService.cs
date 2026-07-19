using KingdomPrep.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace KingdomPrep.Web.Api;

public sealed class SyncInboxService(
    AppDbContext db,
    ILogger<SyncInboxService> logger,
    IAuditLogService? audit = null)
{
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
IF OBJECT_ID(N'SyncInbox', N'U') IS NULL
BEGIN
    CREATE TABLE SyncInbox (
        InboxId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        DeviceId UNIQUEIDENTIFIER NOT NULL,
        DesktopOutboxId BIGINT NOT NULL,
        TableName NVARCHAR(128) NOT NULL,
        RecordSyncId UNIQUEIDENTIFIER NULL,
        PrimaryKeyName NVARCHAR(128) NULL,
        PrimaryKeyValue NVARCHAR(120) NULL,
        Operation NVARCHAR(12) NOT NULL,
        Payload NVARCHAR(MAX) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SyncInbox_Status DEFAULT ('Received'),
        ConflictReason NVARCHAR(500) NULL,
        ReceivedAt DATETIME2 NOT NULL CONSTRAINT DF_SyncInbox_ReceivedAt DEFAULT SYSUTCDATETIME(),
        AppliedAt DATETIME2 NULL
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SyncInbox_DeviceOutbox' AND object_id = OBJECT_ID(N'SyncInbox'))
    CREATE UNIQUE INDEX UX_SyncInbox_DeviceOutbox ON SyncInbox(SchoolId, DeviceId, DesktopOutboxId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncInbox_Received' AND object_id = OBJECT_ID(N'SyncInbox'))
    CREATE INDEX IX_SyncInbox_Received ON SyncInbox(SchoolId, Status, InboxId);

IF OBJECT_ID(N'SyncTombstones', N'U') IS NULL
BEGIN
    CREATE TABLE SyncTombstones (
        TombstoneId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        TableName NVARCHAR(128) NOT NULL,
        RecordSyncId UNIQUEIDENTIFIER NOT NULL,
        DeletedAt DATETIME2 NOT NULL CONSTRAINT DF_SyncTombstones_DeletedAt DEFAULT SYSUTCDATETIME()
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncTombstones_Pull' AND object_id = OBJECT_ID(N'SyncTombstones'))
    CREATE INDEX IX_SyncTombstones_Pull ON SyncTombstones(SchoolId, TableName, DeletedAt, RecordSyncId);

IF OBJECT_ID(N'AcademicYears', N'U') IS NULL
BEGIN
    CREATE TABLE AcademicYears (
        AcademicYearID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        YearName NVARCHAR(30) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_WebAcademicYears_IsActive DEFAULT 0,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WebAcademicYears_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WebAcademicYears_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'AcademicTerms', N'U') IS NULL
BEGIN
    CREATE TABLE AcademicTerms (
        TermID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AcademicYearID INT NOT NULL,
        TermName NVARCHAR(60) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        ReopeningDate DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_WebAcademicTerms_IsActive DEFAULT 0,
        IsClosed BIT NOT NULL CONSTRAINT DF_WebAcademicTerms_IsClosed DEFAULT 0,
        ClosedAt DATETIME2 NULL,
        ClosureReportPath NVARCHAR(260) NULL,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WebAcademicTerms_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WebAcademicTerms_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'StudentEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE StudentEnrollments (
        EnrollmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StudentID NVARCHAR(50) NOT NULL,
        ClassID NVARCHAR(50) NOT NULL,
        AcademicYearID INT NOT NULL,
        EnrollmentDate DATETIME2 NOT NULL CONSTRAINT DF_WebStudentEnrollments_Date DEFAULT SYSUTCDATETIME(),
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_WebStudentEnrollments_Status DEFAULT 'Active',
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WebStudentEnrollments_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WebStudentEnrollments_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NULL
BEGIN
    CREATE TABLE StudentFeeLedger (
        LedgerID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StudentID NVARCHAR(50) NOT NULL,
        TermID INT NOT NULL,
        PreviousBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_WebStudentFeeLedger_Previous DEFAULT 0,
        CurrentTermCharge DECIMAL(18,2) NOT NULL CONSTRAINT DF_WebStudentFeeLedger_Current DEFAULT 0,
        TotalExpectedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_WebStudentFeeLedger_Expected DEFAULT 0,
        TotalPaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_WebStudentFeeLedger_Paid DEFAULT 0,
        CarriedForwardFromTermID INT NULL,
        Balance AS (TotalExpectedAmount - TotalPaidAmount),
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WebStudentFeeLedger_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WebStudentFeeLedger_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'TermReminderSchedule', N'U') IS NULL
BEGIN
    CREATE TABLE TermReminderSchedule (
        ReminderID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TermID INT NOT NULL,
        ReminderType NVARCHAR(20) NOT NULL,
        SendOnDate DATE NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT DF_WebTermReminderSchedule_Status DEFAULT 'Pending',
        SentAt DATETIME2 NULL,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WebTermReminderSchedule_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WebTermReminderSchedule_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NOT NULL AND COL_LENGTH('StudentFeeLedger', 'PreviousBalance') IS NULL
    ALTER TABLE StudentFeeLedger ADD PreviousBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_WebStudentFeeLedger_Previous_Upgrade DEFAULT 0;
IF OBJECT_ID(N'AcademicTerms', N'U') IS NOT NULL AND COL_LENGTH('AcademicTerms', 'ReopeningDate') IS NULL
    ALTER TABLE AcademicTerms ADD ReopeningDate DATE NULL;
IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NOT NULL AND COL_LENGTH('StudentFeeLedger', 'CurrentTermCharge') IS NULL
    ALTER TABLE StudentFeeLedger ADD CurrentTermCharge DECIMAL(18,2) NOT NULL CONSTRAINT DF_WebStudentFeeLedger_Current_Upgrade DEFAULT 0;
IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NOT NULL AND COL_LENGTH('StudentFeeLedger', 'CarriedForwardFromTermID') IS NULL
    ALTER TABLE StudentFeeLedger ADD CarriedForwardFromTermID INT NULL;
IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NOT NULL
    UPDATE StudentFeeLedger SET CurrentTermCharge = TotalExpectedAmount WHERE CurrentTermCharge = 0 AND PreviousBalance = 0 AND TotalExpectedAmount > 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AcademicTerms_School_Active' AND object_id = OBJECT_ID(N'AcademicTerms'))
    CREATE INDEX IX_AcademicTerms_School_Active ON AcademicTerms(SchoolId, IsActive, IsClosed);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentFeeLedger_School_Term' AND object_id = OBJECT_ID(N'StudentFeeLedger'))
    CREATE INDEX IX_StudentFeeLedger_School_Term ON StudentFeeLedger(SchoolId, TermID, StudentID);
""";
        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        await EnsureTombstoneTriggersAsync(cancellationToken);
    }

    private async Task EnsureTombstoneTriggersAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        foreach (var tableName in SyncTablePolicy.AllowedTables)
        {
            var columns = await GetColumnsAsync(connection, tableName, cancellationToken);
            if (columns.Count == 0) continue;
            if (!columns.Any(c => string.Equals(c.Name, "SchoolId", StringComparison.OrdinalIgnoreCase))) continue;
            if (!columns.Any(c => string.Equals(c.Name, "SyncId", StringComparison.OrdinalIgnoreCase))) continue;

            var triggerName = "TR_SyncTombstone_" + tableName + "_Delete";
            var tableLiteral = EscapeSqlLiteral(tableName);
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
CREATE OR ALTER TRIGGER {Quote(triggerName)}
ON {Quote(tableName)}
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SyncTombstones (SchoolId, TableName, RecordSyncId, DeletedAt)
    SELECT d.SchoolId, N'{tableLiteral}', d.SyncId, SYSUTCDATETIME()
    FROM deleted d
    WHERE d.SchoolId IS NOT NULL
      AND d.SyncId IS NOT NULL;
END
""";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<SyncUploadResponse> AcceptUploadAsync(SyncUploadRequest request, CancellationToken cancellationToken)
    {
        var response = new SyncUploadResponse
        {
            SchoolId = request.SchoolId,
            DeviceId = request.DeviceId,
            ReceivedAtUtc = DateTime.UtcNow
        };

        if (request.SchoolId == Guid.Empty)
        {
            response.RejectedChanges.Add(new SyncRejectedChange { OutboxId = 0, Reason = "SchoolId is required." });
            return response;
        }

        if (request.DeviceId == Guid.Empty)
        {
            response.RejectedChanges.Add(new SyncRejectedChange { OutboxId = 0, Reason = "DeviceId is required." });
            return response;
        }

        await EnsureSchemaAsync(cancellationToken);

        foreach (var change in request.Changes ?? [])
        {
            var reason = ValidateChange(request, change);
            if (!string.IsNullOrEmpty(reason))
            {
                response.RejectedChanges.Add(new SyncRejectedChange { OutboxId = change.OutboxId, Reason = reason });
                continue;
            }

            try
            {
                var inserted = await InsertIfNewAsync(change, cancellationToken);
                response.AcceptedOutboxIds.Add(change.OutboxId);

                if (string.IsNullOrWhiteSpace(change.Payload))
                {
                    response.Conflicts.Add(new SyncConflictDto
                    {
                        OutboxId = change.OutboxId,
                        TableName = change.TableName,
                        RecordSyncId = change.RecordSyncId,
                        Reason = "Accepted for retry, but no payload was supplied yet. Desktop must send row payload before server can apply the change."
                    });
                }
                else
                {
                    var apply = await ApplyChangeAsync(change, cancellationToken);
                    if (!apply.Ok)
                    {
                        await MarkInboxConflictAsync(change, apply.Message, cancellationToken);
                        response.Conflicts.Add(new SyncConflictDto
                        {
                            OutboxId = change.OutboxId,
                            TableName = change.TableName,
                            RecordSyncId = change.RecordSyncId,
                            Reason = apply.Message
                        });
                    }
                    else
                    {
                        await MarkInboxAppliedAsync(change, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to accept sync change {OutboxId} from device {DeviceId}", change.OutboxId, request.DeviceId);
                response.RejectedChanges.Add(new SyncRejectedChange { OutboxId = change.OutboxId, Reason = "Server could not store this change." });
            }
        }

        return response;
    }

    public async Task<List<SyncConflictReviewRow>> GetConflictReviewRowsAsync(
        Guid? schoolId,
        string status = "Conflict",
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT TOP {Math.Clamp(take, 1, 500)}
    InboxId,
    SchoolId,
    DeviceId,
    DesktopOutboxId,
    TableName,
    RecordSyncId,
    PrimaryKeyName,
    PrimaryKeyValue,
    Operation,
    Payload,
    Status,
    ConflictReason,
    ReceivedAt,
    AppliedAt
FROM SyncInbox
WHERE Status = @Status
{(schoolId.HasValue ? "AND SchoolId = @SchoolId" : "")}
ORDER BY ReceivedAt DESC, InboxId DESC
""";
        command.Parameters.Add(new SqlParameter("@Status", status));
        if (schoolId.HasValue) command.Parameters.Add(new SqlParameter("@SchoolId", schoolId.Value));

        var rows = new List<SyncConflictReviewRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SyncConflictReviewRow(
                Convert.ToInt64(reader["InboxId"]),
                reader.GetGuid(reader.GetOrdinal("SchoolId")),
                reader.GetGuid(reader.GetOrdinal("DeviceId")),
                Convert.ToInt64(reader["DesktopOutboxId"]),
                AsString(reader, "TableName"),
                AsNullableGuid(reader, "RecordSyncId"),
                AsString(reader, "PrimaryKeyName"),
                AsString(reader, "PrimaryKeyValue"),
                AsString(reader, "Operation"),
                AsNullableString(reader, "Payload"),
                AsString(reader, "Status"),
                AsNullableString(reader, "ConflictReason"),
                Convert.ToDateTime(reader["ReceivedAt"]),
                AsDateTime(reader, "AppliedAt")));
        }

        return rows;
    }

    public async Task<SyncInboxAdminActionResult> RetryInboxAsync(
        long inboxId,
        Guid? schoolId,
        string? actorUsername,
        CancellationToken cancellationToken = default)
    {
        if (inboxId <= 0) return SyncInboxAdminActionResult.Failed("InboxId is required.");

        var row = await GetInboxChangeAsync(inboxId, schoolId, cancellationToken);
        if (row == null) return SyncInboxAdminActionResult.Failed("Sync inbox row was not found.");

        var apply = await ApplyChangeAsync(row.Value.Change, cancellationToken);
        if (!apply.Ok)
        {
            await MarkInboxConflictAsync(row.Value.Change, apply.Message, cancellationToken);
            await WriteInboxAuditAsync(actorUsername, "SyncInboxRetryFailed", row.Value.Change, apply.Message);
            return SyncInboxAdminActionResult.Failed(apply.Message);
        }

        await MarkInboxAppliedAsync(row.Value.Change, cancellationToken);
        await WriteInboxAuditAsync(actorUsername, "SyncInboxRetried", row.Value.Change, "Retried and applied sync inbox row.");
        return new SyncInboxAdminActionResult(true, "Sync inbox row retried and applied.");
    }

    public async Task<SyncInboxAdminActionResult> MarkReviewedAsync(
        long inboxId,
        Guid? schoolId,
        string? actorUsername,
        CancellationToken cancellationToken = default)
    {
        if (inboxId <= 0) return SyncInboxAdminActionResult.Failed("InboxId is required.");

        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
UPDATE SyncInbox
SET Status = 'Reviewed'
WHERE InboxId = @InboxId
  AND Status = 'Conflict'
{(schoolId.HasValue ? "AND SchoolId = @SchoolId" : "")}
""";
        command.Parameters.Add(new SqlParameter("@InboxId", inboxId));
        if (schoolId.HasValue) command.Parameters.Add(new SqlParameter("@SchoolId", schoolId.Value));

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0) return SyncInboxAdminActionResult.Failed("Conflict row was not found or was already resolved.");

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                "SyncInboxReviewed",
                "SyncInbox",
                inboxId.ToString(),
                "Marked sync conflict as reviewed.",
                schoolId));
        }

        return new SyncInboxAdminActionResult(true, "Sync conflict marked as reviewed.");
    }

    private async Task<(bool Ok, string Message)> ApplyChangeAsync(SyncChangeDto change, CancellationToken cancellationToken)
    {
        if (!SyncTablePolicy.IsAllowed(change.TableName)) return (false, "Table is not allowed for sync.");

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var columns = await GetColumnsAsync(connection, change.TableName, cancellationToken);
        if (columns.Count == 0) return (false, "Target table was not found on the server.");
        if (!columns.Any(c => string.Equals(c.Name, "SchoolId", StringComparison.OrdinalIgnoreCase)))
            return (false, "Target table is not tenant-scoped.");
        if (!columns.Any(c => string.Equals(c.Name, "SyncId", StringComparison.OrdinalIgnoreCase)))
            return (false, "Target table is not sync-enabled.");

        if (string.Equals(change.Operation, "Delete", StringComparison.OrdinalIgnoreCase))
            return await ApplyDeleteAsync(connection, change, cancellationToken);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(change.Payload);
        }
        catch
        {
            return (false, "Payload is not valid JSON.");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return (false, "Payload must be a JSON object.");

            var syncId = change.RecordSyncId ?? GetGuid(document.RootElement, "SyncId");
            if (syncId == Guid.Empty) return (false, "Record SyncId is required.");

            bool exists = await RecordExistsAsync(connection, change.TableName, change.SchoolId, syncId, cancellationToken);
            if (!exists && await RecordExistsForAnotherSchoolAsync(connection, change.TableName, change.SchoolId, syncId, cancellationToken))
            {
                await RehomeRecordSchoolAsync(connection, change.TableName, change.SchoolId, syncId, cancellationToken);
                exists = true;
            }

            var preserveIdentity = ShouldPreserveIdentity(change.TableName);
            var writable = columns
                .Where(c => (!c.IsIdentity || preserveIdentity) && !c.IsComputed && !c.IsRowVersion)
                .Where(c => document.RootElement.TryGetProperty(c.Name, out _) ||
                            string.Equals(c.Name, "SchoolId", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(c.Name, "SyncId", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!writable.Any(c => string.Equals(c.Name, "SchoolId", StringComparison.OrdinalIgnoreCase)))
                return (false, "Payload cannot be applied without SchoolId.");
            if (!writable.Any(c => string.Equals(c.Name, "SyncId", StringComparison.OrdinalIgnoreCase)))
                return (false, "Payload cannot be applied without SyncId.");

            if (exists)
                return await ApplyUpdateAsync(connection, change, document.RootElement, writable, syncId, cancellationToken);

            return await ApplyInsertAsync(connection, change, document.RootElement, writable, syncId, cancellationToken);
        }
    }

    private static async Task<(bool Ok, string Message)> ApplyDeleteAsync(
        System.Data.Common.DbConnection connection,
        SyncChangeDto change,
        CancellationToken cancellationToken)
    {
        var syncId = change.RecordSyncId;
        if (syncId == null || syncId == Guid.Empty) return (false, "Delete requires RecordSyncId.");

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {Quote(change.TableName)} WHERE SchoolId = @SchoolId AND SyncId = @SyncId";
        command.Parameters.Add(new SqlParameter("@SchoolId", change.SchoolId));
        command.Parameters.Add(new SqlParameter("@SyncId", syncId.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "");
    }

    private static async Task<(bool Ok, string Message)> ApplyInsertAsync(
        System.Data.Common.DbConnection connection,
        SyncChangeDto change,
        JsonElement payload,
        List<ColumnInfo> writable,
        Guid syncId,
        CancellationToken cancellationToken)
    {
        var columns = writable
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        var columnNames = columns.Select(c => c.Name).ToList();
        var columnSql = string.Join(", ", columnNames.Select(Quote));
        var paramSql = string.Join(", ", columnNames.Select(c => "@" + SafeParameter(c)));

        await using var command = connection.CreateCommand();
        var identityInsert = ShouldPreserveIdentity(change.TableName) && columns.Any(c => c.IsIdentity);
        command.CommandText = identityInsert
            ? $"SET IDENTITY_INSERT {Quote(change.TableName)} ON; INSERT INTO {Quote(change.TableName)} ({columnSql}) VALUES ({paramSql}); SET IDENTITY_INSERT {Quote(change.TableName)} OFF;"
            : $"INSERT INTO {Quote(change.TableName)} ({columnSql}) VALUES ({paramSql})";
        AddColumnParameters(command, columns, payload, change.SchoolId, syncId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "");
    }

    private static async Task<(bool Ok, string Message)> ApplyUpdateAsync(
        System.Data.Common.DbConnection connection,
        SyncChangeDto change,
        JsonElement payload,
        List<ColumnInfo> writable,
        Guid syncId,
        CancellationToken cancellationToken)
    {
        var columns = writable
            .Where(c => !c.IsIdentity)
            .Where(c => !string.Equals(c.Name, "SchoolId", StringComparison.OrdinalIgnoreCase))
            .Where(c => !string.Equals(c.Name, "SyncId", StringComparison.OrdinalIgnoreCase))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (columns.Count == 0) return (true, "");

        var setSql = string.Join(", ", columns.Select(c => $"{Quote(c.Name)} = @{SafeParameter(c.Name)}"));
        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE {Quote(change.TableName)} SET {setSql} WHERE SchoolId = @SchoolId_Where AND SyncId = @SyncId_Where";
        AddColumnParameters(command, columns, payload, change.SchoolId, syncId);
        command.Parameters.Add(new SqlParameter("@SchoolId_Where", change.SchoolId));
        command.Parameters.Add(new SqlParameter("@SyncId_Where", syncId));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "");
    }

    private static async Task<bool> RecordExistsAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        Guid schoolId,
        Guid syncId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {Quote(tableName)} WHERE SchoolId = @SchoolId AND SyncId = @SyncId";
        command.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
        command.Parameters.Add(new SqlParameter("@SyncId", syncId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> RecordExistsForAnotherSchoolAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        Guid schoolId,
        Guid syncId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {Quote(tableName)} WHERE SyncId = @SyncId AND SchoolId <> @SchoolId";
        command.Parameters.Add(new SqlParameter("@SyncId", syncId));
        command.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
    }

    private static async Task RehomeRecordSchoolAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        Guid schoolId,
        Guid syncId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE {Quote(tableName)} SET SchoolId = @SchoolId WHERE SyncId = @SyncId AND SchoolId <> @SchoolId";
        command.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
        command.Parameters.Add(new SqlParameter("@SyncId", syncId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<ColumnInfo>> GetColumnsAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
    c.name,
    t.name AS TypeName,
    c.is_identity,
    c.is_computed
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(@TableName)
ORDER BY c.column_id
""";
        command.Parameters.Add(new SqlParameter("@TableName", tableName));

        var columns = new List<ColumnInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync())
        {
            var typeName = reader["TypeName"]?.ToString() ?? "";
            columns.Add(new ColumnInfo(
                reader["name"]?.ToString() ?? "",
                typeName,
                Convert.ToBoolean(reader["is_identity"]),
                Convert.ToBoolean(reader["is_computed"]),
                string.Equals(typeName, "timestamp", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(typeName, "rowversion", StringComparison.OrdinalIgnoreCase)));
        }

        return columns;
    }

    private static void AddColumnParameters(
        System.Data.Common.DbCommand command,
        IEnumerable<ColumnInfo> columns,
        JsonElement payload,
        Guid schoolId,
        Guid syncId)
    {
        foreach (var column in columns)
        {
            object value;
            if (string.Equals(column.Name, "SchoolId", StringComparison.OrdinalIgnoreCase))
                value = schoolId;
            else if (string.Equals(column.Name, "SyncId", StringComparison.OrdinalIgnoreCase))
                value = syncId;
            else if (payload.TryGetProperty(column.Name, out var property))
                value = ToParameterValue(property, column.TypeName);
            else
                value = DBNull.Value;

            command.Parameters.Add(new SqlParameter("@" + SafeParameter(column.Name), value ?? DBNull.Value));
        }
    }

    private static object ToParameterValue(JsonElement value, string typeName)
    {
        if (IsBinaryType(typeName))
        {
            if (value.ValueKind == JsonValueKind.Null) return DBNull.Value;
            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (string.IsNullOrEmpty(text)) return Array.Empty<byte>();
                try { return Convert.FromBase64String(text); }
                catch { return Array.Empty<byte>(); }
            }
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null => DBNull.Value,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out var i) => i,
            JsonValueKind.Number when value.TryGetInt64(out var l) => l,
            JsonValueKind.Number when value.TryGetDecimal(out var d) => d,
            JsonValueKind.String when value.TryGetGuid(out var g) => g,
            JsonValueKind.String when value.TryGetDateTime(out var dt) => dt,
            JsonValueKind.String => value.GetString() ?? "",
            _ => value.GetRawText()
        };
    }

    private static bool IsBinaryType(string typeName) =>
        string.Equals(typeName, "binary", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "varbinary", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "image", StringComparison.OrdinalIgnoreCase);

    private static Guid GetGuid(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var property) && property.TryGetGuid(out var value)
            ? value
            : Guid.Empty;
    }

    private async Task MarkInboxAppliedAsync(SyncChangeDto change, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("""
UPDATE SyncInbox
SET Status = 'Applied', AppliedAt = SYSUTCDATETIME(), ConflictReason = NULL
WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId AND DesktopOutboxId = @DesktopOutboxId
""",
            [
                new SqlParameter("@SchoolId", change.SchoolId),
                new SqlParameter("@DeviceId", change.DeviceId),
                new SqlParameter("@DesktopOutboxId", change.OutboxId)
            ],
            cancellationToken);
    }

    private async Task MarkInboxConflictAsync(SyncChangeDto change, string reason, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("""
UPDATE SyncInbox
SET Status = 'Conflict', ConflictReason = @Reason
WHERE SchoolId = @SchoolId AND DeviceId = @DeviceId AND DesktopOutboxId = @DesktopOutboxId
""",
            [
                new SqlParameter("@Reason", Trim(reason, 500)),
                new SqlParameter("@SchoolId", change.SchoolId),
                new SqlParameter("@DeviceId", change.DeviceId),
                new SqlParameter("@DesktopOutboxId", change.OutboxId)
            ],
            cancellationToken);
    }

    private async Task<(SyncChangeDto Change, string Status)?> GetInboxChangeAsync(
        long inboxId,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT TOP 1 SchoolId, DeviceId, DesktopOutboxId, TableName, RecordSyncId, PrimaryKeyName, PrimaryKeyValue, Operation, Payload, Status
FROM SyncInbox
WHERE InboxId = @InboxId
{(schoolId.HasValue ? "AND SchoolId = @SchoolId" : "")}
""";
        command.Parameters.Add(new SqlParameter("@InboxId", inboxId));
        if (schoolId.HasValue) command.Parameters.Add(new SqlParameter("@SchoolId", schoolId.Value));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var change = new SyncChangeDto
        {
            SchoolId = reader.GetGuid(reader.GetOrdinal("SchoolId")),
            DeviceId = reader.GetGuid(reader.GetOrdinal("DeviceId")),
            OutboxId = Convert.ToInt64(reader["DesktopOutboxId"]),
            TableName = AsString(reader, "TableName"),
            RecordSyncId = AsNullableGuid(reader, "RecordSyncId"),
            PrimaryKeyName = AsString(reader, "PrimaryKeyName"),
            PrimaryKeyValue = AsString(reader, "PrimaryKeyValue"),
            Operation = AsString(reader, "Operation"),
            Payload = AsNullableString(reader, "Payload") ?? ""
        };

        return (change, AsString(reader, "Status"));
    }

    private async Task WriteInboxAuditAsync(string? actorUsername, string action, SyncChangeDto change, string summary)
    {
        if (audit == null) return;

        await audit.WriteAsync(new AuditLogEntry(
            actorUsername ?? "system",
            action,
            "SyncInbox",
            change.OutboxId.ToString(),
            summary,
            change.SchoolId));
    }

    public async Task<SyncPullResponse> PullAsync(SyncPullRequest request, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        if (!SyncTablePolicy.IsAllowed(request.TableName))
            return new SyncPullResponse { SchoolId = request.SchoolId, TableName = request.TableName, ServerCursor = request.ServerCursor, Changes = [] };

        var changes = new List<SyncServerChangeDto>();
        var cursor = ParseCursor(request.ServerCursor);
        var newCursor = request.ServerCursor;

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var columns = await GetColumnsAsync(connection, request.TableName, cancellationToken);
        if (columns.Count == 0) return new SyncPullResponse { SchoolId = request.SchoolId, TableName = request.TableName, ServerCursor = newCursor, Changes = [] };

        bool hasUpdatedAt = columns.Any(c => string.Equals(c.Name, "UpdatedAt", StringComparison.OrdinalIgnoreCase));
        bool hasSyncId = columns.Any(c => string.Equals(c.Name, "SyncId", StringComparison.OrdinalIgnoreCase));
        bool hasSchoolId = columns.Any(c => string.Equals(c.Name, "SchoolId", StringComparison.OrdinalIgnoreCase));

        if (!hasSyncId) return new SyncPullResponse { SchoolId = request.SchoolId, TableName = request.TableName, ServerCursor = newCursor, Changes = [] };

        var pk = columns.FirstOrDefault(c => c.IsIdentity) ?? columns.FirstOrDefault();
        if (pk == null) return new SyncPullResponse { SchoolId = request.SchoolId, TableName = request.TableName, ServerCursor = newCursor, Changes = [] };

        var batchSize = Math.Clamp(request.BatchSize, 1, 500);
        var sql = $"SELECT TOP {batchSize} * FROM {Quote(request.TableName)} WHERE 1=1";
        if (hasSchoolId) sql += " AND SchoolId = @SchoolId";
        if (hasUpdatedAt && cursor.UpdatedAtUtc.HasValue)
        {
             sql += " AND (UpdatedAt > @CursorAt";
             if (cursor.SyncId.HasValue)
                 sql += " OR (UpdatedAt = @CursorAt AND CONVERT(NVARCHAR(36), SyncId) > @CursorSyncId)";
             sql += ")";
        }

        if (hasUpdatedAt) sql += " ORDER BY UpdatedAt ASC, SyncId ASC";
        else sql += " ORDER BY " + Quote(pk.Name);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@SchoolId", request.SchoolId));
        if (hasUpdatedAt && cursor.UpdatedAtUtc.HasValue)
        {
             command.Parameters.Add(new SqlParameter("@CursorAt", cursor.UpdatedAtUtc.Value));
             if (cursor.SyncId.HasValue)
                 command.Parameters.Add(new SqlParameter("@CursorSyncId", cursor.SyncId.Value.ToString("D")));
        }

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync())
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i);
                    if (val != DBNull.Value) dict[reader.GetName(i)] = val;
                }

                var payload = JsonSerializer.Serialize(dict);
                var syncIdGuid = reader.GetGuid(reader.GetOrdinal("SyncId"));

                changes.Add(new SyncServerChangeDto
                {
                    RecordSyncId = syncIdGuid,
                    Operation = "Update",
                    Payload = payload,
                    UpdatedAtUtc = hasUpdatedAt ? reader.GetDateTime(reader.GetOrdinal("UpdatedAt")) : DateTime.UtcNow
                });
            }
        }

        await using var tombstoneCommand = connection.CreateCommand();
        tombstoneCommand.CommandText = $"""
SELECT TOP {batchSize} RecordSyncId, DeletedAt
FROM SyncTombstones
WHERE SchoolId = @SchoolId
  AND TableName = @TableName
{(cursor.UpdatedAtUtc.HasValue ? "  AND (DeletedAt > @CursorAt OR (DeletedAt = @CursorAt AND CONVERT(NVARCHAR(36), RecordSyncId) > @CursorSyncId))" : "")}
ORDER BY DeletedAt ASC, RecordSyncId ASC
""";
        tombstoneCommand.Parameters.Add(new SqlParameter("@SchoolId", request.SchoolId));
        tombstoneCommand.Parameters.Add(new SqlParameter("@TableName", request.TableName));
        if (cursor.UpdatedAtUtc.HasValue)
        {
            tombstoneCommand.Parameters.Add(new SqlParameter("@CursorAt", cursor.UpdatedAtUtc.Value));
            tombstoneCommand.Parameters.Add(new SqlParameter("@CursorSyncId", cursor.SyncId?.ToString("D") ?? ""));
        }

        await using (var tombstoneReader = await tombstoneCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await tombstoneReader.ReadAsync(cancellationToken))
            {
                changes.Add(new SyncServerChangeDto
                {
                    RecordSyncId = tombstoneReader.GetGuid(tombstoneReader.GetOrdinal("RecordSyncId")),
                    Operation = "Delete",
                    Payload = "",
                    UpdatedAtUtc = Convert.ToDateTime(tombstoneReader["DeletedAt"])
                });
            }
        }

        changes = changes
            .OrderBy(c => c.UpdatedAtUtc)
            .ThenBy(c => c.RecordSyncId?.ToString("D") ?? "")
            .Take(batchSize)
            .ToList();

        if (changes.Count > 0)
        {
            var last = changes[^1];
            if (last.RecordSyncId.HasValue)
                newCursor = BuildCursor(last.UpdatedAtUtc, last.RecordSyncId.Value);
        }

        return new SyncPullResponse
        {
            SchoolId = request.SchoolId,
            TableName = request.TableName,
            ServerCursor = newCursor,
            Changes = changes
        };
    }

    private static (DateTime? UpdatedAtUtc, Guid? SyncId) ParseCursor(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return (null, null);

        var parts = cursor.Split('|', 2);
        if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var updatedAt))
            return (null, null);

        return parts.Length == 2 && Guid.TryParse(parts[1], out var syncId)
            ? (updatedAt, syncId)
            : (updatedAt, null);
    }

    private static string BuildCursor(DateTime updatedAtUtc, Guid syncId) =>
        updatedAtUtc.ToString("o") + "|" + syncId.ToString("D");

    private static string ValidateChange(SyncUploadRequest request, SyncChangeDto change)
    {
        if (change.OutboxId <= 0) return "OutboxId is required.";
        if (change.SchoolId != request.SchoolId) return "Change SchoolId does not match batch SchoolId.";
        if (change.DeviceId != request.DeviceId) return "Change DeviceId does not match batch DeviceId.";
        if (!SyncTablePolicy.IsAllowed(change.TableName)) return "Table is not allowed for sync.";
        if (!IsAllowedOperation(change.Operation)) return "Operation must be Insert, Update, or Delete.";
        if (string.IsNullOrWhiteSpace(change.PrimaryKeyName)) return "PrimaryKeyName is required.";
        if (string.IsNullOrWhiteSpace(change.PrimaryKeyValue)) return "PrimaryKeyValue is required.";
        return "";
    }

    private static bool IsAllowedOperation(string operation) =>
        string.Equals(operation, "Insert", StringComparison.OrdinalIgnoreCase)
        || string.Equals(operation, "Update", StringComparison.OrdinalIgnoreCase)
        || string.Equals(operation, "Delete", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldPreserveIdentity(string tableName) =>
        string.Equals(tableName, "ExamTypes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(tableName, "ApprovalWorkflows", StringComparison.OrdinalIgnoreCase)
        || string.Equals(tableName, "ClassPerformanceReports", StringComparison.OrdinalIgnoreCase)
        || string.Equals(tableName, "StudentPerformanceEntries", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> InsertIfNewAsync(SyncChangeDto change, CancellationToken cancellationToken)
    {
        const string sql = """
IF NOT EXISTS (
    SELECT 1
    FROM SyncInbox
    WHERE SchoolId = @SchoolId
      AND DeviceId = @DeviceId
      AND DesktopOutboxId = @DesktopOutboxId)
BEGIN
    INSERT INTO SyncInbox
        (SchoolId, DeviceId, DesktopOutboxId, TableName, RecordSyncId, PrimaryKeyName, PrimaryKeyValue, Operation, Payload, Status, ReceivedAt)
    VALUES
        (@SchoolId, @DeviceId, @DesktopOutboxId, @TableName, @RecordSyncId, @PrimaryKeyName, @PrimaryKeyValue, @Operation, @Payload, 'Received', SYSUTCDATETIME());
END
""";
        var before = await CountStoredAsync(change, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(sql,
            [
                new SqlParameter("@SchoolId", change.SchoolId),
                new SqlParameter("@DeviceId", change.DeviceId),
                new SqlParameter("@DesktopOutboxId", change.OutboxId),
                new SqlParameter("@TableName", Trim(change.TableName, 128)),
                new SqlParameter("@RecordSyncId", (object?)change.RecordSyncId ?? DBNull.Value),
                new SqlParameter("@PrimaryKeyName", Trim(change.PrimaryKeyName, 128)),
                new SqlParameter("@PrimaryKeyValue", Trim(change.PrimaryKeyValue, 120)),
                new SqlParameter("@Operation", Trim(change.Operation, 12)),
                new SqlParameter("@Payload", string.IsNullOrWhiteSpace(change.Payload) ? DBNull.Value : change.Payload)
            ],
            cancellationToken);
        var after = await CountStoredAsync(change, cancellationToken);
        return after > before;
    }

    private async Task<int> CountStoredAsync(SyncChangeDto change, CancellationToken cancellationToken)
    {
        const string sql = """
SELECT COUNT(*)
FROM SyncInbox
WHERE SchoolId = @SchoolId
  AND DeviceId = @DeviceId
  AND DesktopOutboxId = @DesktopOutboxId
""";
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@SchoolId", change.SchoolId));
        command.Parameters.Add(new SqlParameter("@DeviceId", change.DeviceId));
        command.Parameters.Add(new SqlParameter("@DesktopOutboxId", change.OutboxId));

        if (command.Connection!.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private static string Trim(string value, int max)
    {
        value = value?.Trim() ?? "";
        return value.Length <= max ? value : value[..max];
    }

    private static string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    private static string SafeParameter(string identifier)
    {
        var chars = identifier.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        return new string(chars);
    }

    private sealed record ColumnInfo(
        string Name,
        string TypeName,
        bool IsIdentity,
        bool IsComputed,
        bool IsRowVersion);

    private static DateTime? AsDateTime(IDataRecord reader, string name) =>
        reader[name] == DBNull.Value ? null : Convert.ToDateTime(reader[name]);

    private static Guid? AsNullableGuid(IDataRecord reader, string name) =>
        reader[name] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal(name));

    private static string AsString(IDataRecord reader, string name) =>
        reader[name]?.ToString() ?? "";

    private static string? AsNullableString(IDataRecord reader, string name) =>
        reader[name] == DBNull.Value ? null : reader[name]?.ToString();
}

public sealed record SyncConflictReviewRow(
    long InboxId,
    Guid SchoolId,
    Guid DeviceId,
    long DesktopOutboxId,
    string TableName,
    Guid? RecordSyncId,
    string PrimaryKeyName,
    string PrimaryKeyValue,
    string Operation,
    string? Payload,
    string Status,
    string? ConflictReason,
    DateTime ReceivedAtUtc,
    DateTime? AppliedAtUtc);

public sealed record SyncInboxAdminActionResult(bool Ok, string Message)
{
    public static SyncInboxAdminActionResult Failed(string message) => new(false, message);
}
