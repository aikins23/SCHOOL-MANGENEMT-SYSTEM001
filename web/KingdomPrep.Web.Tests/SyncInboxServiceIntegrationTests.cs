using System.Text.Json;
using KingdomPrep.Web.Api;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KingdomPrep.Web.Tests;

[Trait("Category", "SqlServerIntegration")]
public class SyncInboxServiceIntegrationTests
{
    [Fact]
    public async Task AcceptUploadAndPull_RoundTripsAcademicYearChange()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var service = new SyncInboxService(db, NullLogger<SyncInboxService>.Instance);
        var schoolId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var recordSyncId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var upload = new SyncUploadRequest
        {
            SchoolId = schoolId,
            DeviceId = deviceId,
            PreparedAtUtc = now,
            Changes =
            [
                new SyncChangeDto
                {
                    OutboxId = 1001,
                    SchoolId = schoolId,
                    DeviceId = deviceId,
                    TableName = "AcademicYears",
                    RecordSyncId = recordSyncId,
                    PrimaryKeyName = "AcademicYearID",
                    PrimaryKeyValue = "1",
                    Operation = "Insert",
                    Payload = JsonSerializer.Serialize(new
                    {
                        SyncId = recordSyncId,
                        YearName = "2026 Academic Year",
                        StartDate = new DateTime(2026, 1, 1),
                        EndDate = new DateTime(2026, 12, 31),
                        IsActive = true
                    }),
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ]
        };

        var uploadResult = await service.AcceptUploadAsync(upload, CancellationToken.None);

        Assert.Contains(1001, uploadResult.AcceptedOutboxIds);
        Assert.Empty(uploadResult.RejectedChanges);
        Assert.Empty(uploadResult.Conflicts);

        await using (var connection = new SqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
SELECT COUNT(*)
FROM AcademicYears
WHERE SchoolId = @SchoolId
  AND SyncId = @SyncId
  AND YearName = @YearName
""", connection);
            command.Parameters.AddWithValue("@SchoolId", schoolId);
            command.Parameters.AddWithValue("@SyncId", recordSyncId);
            command.Parameters.AddWithValue("@YearName", "2026 Academic Year");
            var stored = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(1, stored);
        }

        var pull = await service.PullAsync(new SyncPullRequest
        {
            SchoolId = schoolId,
            DeviceId = deviceId,
            TableName = "AcademicYears",
            BatchSize = 10
        }, CancellationToken.None);

        Assert.Equal(schoolId, pull.SchoolId);
        Assert.Equal("AcademicYears", pull.TableName);
        Assert.Single(pull.Changes);
        Assert.Equal(recordSyncId, pull.Changes[0].RecordSyncId);
        Assert.Equal("Update", pull.Changes[0].Operation);
        Assert.Contains("2026 Academic Year", pull.Changes[0].Payload);
        Assert.False(string.IsNullOrWhiteSpace(pull.ServerCursor));
    }

    [Fact]
    public async Task AcceptUpload_RejectsUnknownTable()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var service = new SyncInboxService(db, NullLogger<SyncInboxService>.Instance);
        var schoolId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var upload = new SyncUploadRequest
        {
            SchoolId = schoolId,
            DeviceId = deviceId,
            PreparedAtUtc = DateTime.UtcNow,
            Changes =
            [
                new SyncChangeDto
                {
                    OutboxId = 1002,
                    SchoolId = schoolId,
                    DeviceId = deviceId,
                    TableName = "Students; DROP TABLE Users",
                    RecordSyncId = Guid.NewGuid(),
                    PrimaryKeyName = "StudentID",
                    PrimaryKeyValue = "1",
                    Operation = "Insert",
                    Payload = "{}"
                }
            ]
        };

        var result = await service.AcceptUploadAsync(upload, CancellationToken.None);

        Assert.Empty(result.AcceptedOutboxIds);
        var rejected = Assert.Single(result.RejectedChanges);
        Assert.Equal(1002, rejected.OutboxId);
        Assert.Equal("Table is not allowed for sync.", rejected.Reason);
    }

    [Fact]
    public async Task AcceptUpload_PreservesExamTypeIdentity()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var service = new SyncInboxService(db, NullLogger<SyncInboxService>.Instance);
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var registry = new SyncDeviceRegistryService(db, config, NullLogger<SyncDeviceRegistryService>.Instance);
        var initializer = new WebSchemaInitializer(
            new TestDbContextFactory(options),
            registry,
            service,
            NullLogger<WebSchemaInitializer>.Instance);
        await initializer.EnsureAsync(CancellationToken.None);

        var schoolId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var recordSyncId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var upload = new SyncUploadRequest
        {
            SchoolId = schoolId,
            DeviceId = deviceId,
            PreparedAtUtc = now,
            Changes =
            [
                new SyncChangeDto
                {
                    OutboxId = 1003,
                    SchoolId = schoolId,
                    DeviceId = deviceId,
                    TableName = "ExamTypes",
                    RecordSyncId = recordSyncId,
                    PrimaryKeyName = "ExamTypeId",
                    PrimaryKeyValue = "77",
                    Operation = "Insert",
                    Payload = JsonSerializer.Serialize(new
                    {
                        ExamTypeId = 77,
                        Name = "Basic 9 Mock",
                        Code = "MOCK-B9",
                        Description = "Candidate mock examination",
                        WeightPercentage = 40m,
                        IsGradedExam = true,
                        IncludeInReportCard = false,
                        DisplayOrder = 77,
                        SchoolId = schoolId,
                        SyncId = recordSyncId,
                        IsSystemType = false,
                        IsActive = true,
                        CreatedDate = now,
                        CreatedBy = "desktop",
                        UpdatedAt = now
                    }),
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ]
        };

        var uploadResult = await service.AcceptUploadAsync(upload, CancellationToken.None);

        Assert.Contains(1003, uploadResult.AcceptedOutboxIds);
        Assert.Empty(uploadResult.RejectedChanges);
        Assert.Empty(uploadResult.Conflicts);

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
SELECT COUNT(*)
FROM ExamTypes
WHERE SchoolId = @SchoolId
  AND SyncId = @SyncId
  AND ExamTypeId = 77
  AND Code = @Code
""", connection);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        command.Parameters.AddWithValue("@SyncId", recordSyncId);
        command.Parameters.AddWithValue("@Code", "MOCK-B9");

        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task PullAsync_ReturnsDeleteForServerTombstone()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var service = new SyncInboxService(db, NullLogger<SyncInboxService>.Instance);
        await service.EnsureSchemaAsync(CancellationToken.None);

        var schoolId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var recordSyncId = Guid.NewGuid();

        await using (var connection = new SqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using (var insert = new SqlCommand("""
INSERT INTO AcademicYears (YearName, StartDate, EndDate, IsActive, SchoolId, SyncId, UpdatedAt)
VALUES (@YearName, @StartDate, @EndDate, 1, @SchoolId, @SyncId, SYSUTCDATETIME());
""", connection))
            {
                insert.Parameters.AddWithValue("@YearName", "Deleted Academic Year");
                insert.Parameters.AddWithValue("@StartDate", new DateTime(2026, 1, 1));
                insert.Parameters.AddWithValue("@EndDate", new DateTime(2026, 12, 31));
                insert.Parameters.AddWithValue("@SchoolId", schoolId);
                insert.Parameters.AddWithValue("@SyncId", recordSyncId);
                await insert.ExecuteNonQueryAsync();
            }

            await using (var delete = new SqlCommand("""
DELETE FROM AcademicYears
WHERE SchoolId = @SchoolId AND SyncId = @SyncId;
""", connection))
            {
                delete.Parameters.AddWithValue("@SchoolId", schoolId);
                delete.Parameters.AddWithValue("@SyncId", recordSyncId);
                await delete.ExecuteNonQueryAsync();
            }
        }

        var pull = await service.PullAsync(new SyncPullRequest
        {
            SchoolId = schoolId,
            DeviceId = deviceId,
            TableName = "AcademicYears",
            BatchSize = 10
        }, CancellationToken.None);

        var change = Assert.Single(pull.Changes);
        Assert.Equal(recordSyncId, change.RecordSyncId);
        Assert.Equal("Delete", change.Operation);
        Assert.Equal("", change.Payload);
        Assert.False(string.IsNullOrWhiteSpace(pull.ServerCursor));
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppDbContext(options));
    }

    [Fact]
    public async Task RetryInboxAsync_AppliesStoredConflictPayload()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var service = new SyncInboxService(db, NullLogger<SyncInboxService>.Instance);
        await service.EnsureSchemaAsync(CancellationToken.None);

        var schoolId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var recordSyncId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            SyncId = recordSyncId,
            YearName = "Retried Academic Year",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            IsActive = true
        });

        var inboxId = await InsertInboxRowAsync(database.ConnectionString, schoolId, deviceId, 2001, "AcademicYears", recordSyncId, "Conflict", payload);

        var result = await service.RetryInboxAsync(inboxId, schoolId, "admin", CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Equal("Sync inbox row retried and applied.", result.Message);

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
SELECT COUNT(*)
FROM AcademicYears
WHERE SchoolId = @SchoolId
  AND SyncId = @SyncId
  AND YearName = @YearName
""", connection);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        command.Parameters.AddWithValue("@SyncId", recordSyncId);
        command.Parameters.AddWithValue("@YearName", "Retried Academic Year");

        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task MarkReviewedAsync_MovesConflictOutOfActiveConflictStatus()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var service = new SyncInboxService(db, NullLogger<SyncInboxService>.Instance);
        await service.EnsureSchemaAsync(CancellationToken.None);

        var schoolId = Guid.NewGuid();
        var inboxId = await InsertInboxRowAsync(
            database.ConnectionString,
            schoolId,
            Guid.NewGuid(),
            2002,
            "AcademicYears",
            Guid.NewGuid(),
            "Conflict",
            "{}");

        var result = await service.MarkReviewedAsync(inboxId, schoolId, "admin", CancellationToken.None);

        Assert.True(result.Ok);

        var conflicts = await service.GetConflictReviewRowsAsync(schoolId, "Conflict", cancellationToken: CancellationToken.None);
        var reviewed = await service.GetConflictReviewRowsAsync(schoolId, "Reviewed", cancellationToken: CancellationToken.None);

        Assert.Empty(conflicts);
        Assert.Single(reviewed);
        Assert.Equal(inboxId, reviewed[0].InboxId);
    }

    private static async Task<long> InsertInboxRowAsync(
        string connectionString,
        Guid schoolId,
        Guid deviceId,
        long outboxId,
        string tableName,
        Guid recordSyncId,
        string status,
        string payload)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
INSERT INTO SyncInbox
    (SchoolId, DeviceId, DesktopOutboxId, TableName, RecordSyncId, PrimaryKeyName, PrimaryKeyValue, Operation, Payload, Status, ConflictReason, ReceivedAt)
OUTPUT INSERTED.InboxId
VALUES
    (@SchoolId, @DeviceId, @DesktopOutboxId, @TableName, @RecordSyncId, @PrimaryKeyName, @PrimaryKeyValue, @Operation, @Payload, @Status, @ConflictReason, SYSUTCDATETIME());
""", connection);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        command.Parameters.AddWithValue("@DeviceId", deviceId);
        command.Parameters.AddWithValue("@DesktopOutboxId", outboxId);
        command.Parameters.AddWithValue("@TableName", tableName);
        command.Parameters.AddWithValue("@RecordSyncId", recordSyncId);
        command.Parameters.AddWithValue("@PrimaryKeyName", "AcademicYearID");
        command.Parameters.AddWithValue("@PrimaryKeyValue", outboxId.ToString());
        command.Parameters.AddWithValue("@Operation", "Insert");
        command.Parameters.AddWithValue("@Payload", payload);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@ConflictReason", "Test conflict");

        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
