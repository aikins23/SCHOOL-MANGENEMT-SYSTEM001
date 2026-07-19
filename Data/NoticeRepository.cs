using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class NoticeRepository : INoticeRepository
    {
        private readonly string _connectionString;

        public NoticeRepository(string connectionString)
        {
            _connectionString = SqlCommandExtensions.StripProvider(connectionString);
        }

        public async Task<IEnumerable<Notice>> GetAllAsync()
        {
            await EnsureTableAsync();
            var schoolId = TenantContext.RequireSchoolId();
            var notices = new List<Notice>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT NoticeID, Title, Message, Target, TargetClass, Channel, SentBy, SentDate, RecipientCount, Status
FROM Notices
WHERE SchoolId = @SchoolId OR SchoolId IS NULL
ORDER BY SentDate DESC, NoticeID DESC;", connection))
                {
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notices.Add(MapNotice(reader));
                        }
                    }
                }
            }

            return notices;
        }

        public async Task<bool> AddAsync(Notice notice)
        {
            if (notice == null) throw new ArgumentNullException(nameof(notice));
            await EnsureTableAsync();

            var schoolId = TenantContext.RequireSchoolId();
            var now = DateTime.UtcNow;
            notice.Title = (notice.Title ?? "").Trim();
            notice.Message = (notice.Message ?? "").Trim();
            notice.Target = NormalizeTarget(notice.Target);
            notice.TargetClass = (notice.TargetClass ?? "").Trim();
            notice.Channel = NormalizeChannel(notice.Channel);
            notice.SentBy = string.IsNullOrWhiteSpace(notice.SentBy) ? AuthService.CurrentUser?.Username ?? "System" : notice.SentBy.Trim();
            notice.SentDate = notice.SentDate == default ? DateTime.Now : notice.SentDate;
            notice.RecipientCount = notice.RecipientCount > 0 ? notice.RecipientCount : await CountRecipientsAsync(notice.Target, notice.TargetClass, schoolId);
            notice.Status = string.IsNullOrWhiteSpace(notice.Status) ? "Saved" : notice.Status.Trim();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var hasBodyColumn = await HasColumnAsync(connection, "Notices", "Body");
                var bodyColumn = hasBodyColumn ? ", Body" : "";
                var bodyValue = hasBodyColumn ? ", @Message" : "";
                using (var command = new SqlCommand(@"
DECLARE @InsertedNotices TABLE (NoticeID INT);

INSERT INTO Notices
    (Title, [Message]" + bodyColumn + @", Target, TargetClass, Channel, SentBy, SentDate, RecipientCount, [Status], SchoolId, SyncId, UpdatedAt)
OUTPUT INSERTED.NoticeID INTO @InsertedNotices
VALUES
    (@Title, @Message" + bodyValue + @", @Target, @TargetClass, @Channel, @SentBy, @SentDate, @RecipientCount, @Status, @SchoolId, NEWID(), @UpdatedAt);

SELECT TOP 1 NoticeID FROM @InsertedNotices;", connection))
                {
                    command.Parameters.AddWithValue("@Title", notice.Title);
                    command.Parameters.AddWithValue("@Message", notice.Message);
                    command.Parameters.AddWithValue("@Target", notice.Target);
                    command.Parameters.AddWithValue("@TargetClass", string.IsNullOrWhiteSpace(notice.TargetClass) ? (object)DBNull.Value : notice.TargetClass);
                    command.Parameters.AddWithValue("@Channel", notice.Channel);
                    command.Parameters.AddWithValue("@SentBy", notice.SentBy);
                    command.Parameters.AddWithValue("@SentDate", notice.SentDate);
                    command.Parameters.AddWithValue("@RecipientCount", notice.RecipientCount);
                    command.Parameters.AddWithValue("@Status", notice.Status);
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                    command.Parameters.AddWithValue("@UpdatedAt", now);

                    notice.NoticeID = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }

            await TryRecordSyncUpsertAsync(notice.NoticeID, "Insert");
            return notice.NoticeID > 0;
        }

        public async Task<bool> DeleteAsync(int noticeId)
        {
            if (noticeId <= 0) return false;
            await EnsureTableAsync();
            var schoolId = TenantContext.RequireSchoolId();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
DELETE FROM Notices
WHERE NoticeID = @NoticeID
  AND (SchoolId = @SchoolId OR SchoolId IS NULL);", connection))
                {
                    command.Parameters.AddWithValue("@NoticeID", noticeId);
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                    var deleted = await command.ExecuteNonQueryAsync() > 0;
                    if (deleted) await TryRecordSyncDeleteAsync(noticeId);
                    return deleted;
                }
            }
        }

        public async Task<bool> UpdateDeliveryStatusAsync(int noticeId, int recipientCount, string status)
        {
            if (noticeId <= 0) return false;
            await EnsureTableAsync();
            var schoolId = TenantContext.RequireSchoolId();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
UPDATE Notices
SET RecipientCount = @RecipientCount,
    [Status] = @Status,
    UpdatedAt = SYSUTCDATETIME()
WHERE NoticeID = @NoticeID
  AND (SchoolId = @SchoolId OR SchoolId IS NULL);", connection))
                {
                    command.Parameters.AddWithValue("@RecipientCount", Math.Max(0, recipientCount));
                    command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) ? "Saved" : status.Trim());
                    command.Parameters.AddWithValue("@NoticeID", noticeId);
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                    var updated = await command.ExecuteNonQueryAsync() > 0;
                    if (updated) await TryRecordSyncUpsertAsync(noticeId, "Update");
                    return updated;
                }
            }
        }

        public async Task<DataTable> GetAsTableAsync()
        {
            await EnsureTableAsync();
            var schoolId = TenantContext.RequireSchoolId();
            var table = new DataTable();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT NoticeID, Title, Target, TargetClass, Channel, SentBy AS [Sent By],
       SentDate AS [Sent Date], RecipientCount AS Recipients, [Status]
FROM Notices
WHERE SchoolId = @SchoolId OR SchoolId IS NULL
ORDER BY SentDate DESC, NoticeID DESC;", connection))
                {
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        await Task.Run(() => adapter.Fill(table));
                    }
                }
            }

            return table;
        }

        public async Task<DataTable> GetRecipientTableAsync(string target, string targetClass)
        {
            await EnsureTableAsync();
            var schoolId = TenantContext.RequireSchoolId();
            target = NormalizeTarget(target);
            targetClass = (targetClass ?? "").Trim();

            var table = new DataTable();
            table.Columns.Add("RecipientType", typeof(string));
            table.Columns.Add("DisplayName", typeof(string));
            table.Columns.Add("Phone", typeof(string));
            table.Columns.Add("Email", typeof(string));

            if (string.Equals(target, "All", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, "Parents", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, "Specific Class", StringComparison.OrdinalIgnoreCase))
            {
                await AppendStudentRecipientsAsync(table, schoolId,
                    string.Equals(target, "Specific Class", StringComparison.OrdinalIgnoreCase) ? targetClass : "");
            }

            if (string.Equals(target, "All", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, "Employees", StringComparison.OrdinalIgnoreCase))
            {
                await AppendEmployeeRecipientsAsync(table, schoolId);
            }

            return table;
        }

        private async Task AppendStudentRecipientsAsync(DataTable table, Guid schoolId, string className)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var hasSchoolId = await HasColumnAsync(connection, "Students", "SchoolId");
                var hasCorrectEmergencyColumn = await HasColumnAsync(connection, "Students", "EmergencyContact");
                var hasLegacyEmergencyColumn = await HasColumnAsync(connection, "Students", "EmergencyConatct");
                var hasCorrectEmailColumn = await HasColumnAsync(connection, "Students", "GuidanceEmail");
                var hasLegacyEmailColumn = await HasColumnAsync(connection, "Students", "GuidianceEmail");
                var emergencyColumn = hasCorrectEmergencyColumn
                    ? "EmergencyContact"
                    : hasLegacyEmergencyColumn ? "EmergencyConatct" : null;
                var emailColumn = hasCorrectEmailColumn
                    ? "GuidanceEmail"
                    : hasLegacyEmailColumn ? "GuidianceEmail" : null;
                var phoneSelect = emergencyColumn == null ? "CAST(NULL AS NVARCHAR(100))" : emergencyColumn;
                var emailSelect = emailColumn == null ? "CAST(NULL AS NVARCHAR(200))" : emailColumn;
                var recipientClause = BuildRecipientClause(emergencyColumn, emailColumn);
                var schoolClause = hasSchoolId ? "(SchoolId = @SchoolId OR SchoolId IS NULL)" : "1=1";
                var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(FirstName, '') + ' ' + ISNULL(LastName, ''))) AS DisplayName,
    {phoneSelect} AS Phone,
    {emailSelect} AS Email
FROM Students
WHERE {schoolClause}
  AND (@ClassID = '' OR ClassID = @ClassID)
  AND ({recipientClause});";

                using (var command = new SqlCommand(sql, connection))
                {
                    if (hasSchoolId) command.Parameters.AddWithValue("@SchoolId", schoolId);
                    command.Parameters.AddWithValue("@ClassID", className ?? "");
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            table.Rows.Add("Parent", reader["DisplayName"]?.ToString() ?? "", reader["Phone"]?.ToString() ?? "", reader["Email"]?.ToString() ?? "");
                        }
                    }
                }
            }
        }

        private async Task AppendEmployeeRecipientsAsync(DataTable table, Guid schoolId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var hasSchoolId = await HasColumnAsync(connection, "Employee", "SchoolId");
                var hasCorrectPhoneColumn = await HasColumnAsync(connection, "Employee", "contact");
                var hasLegacyPhoneColumn = await HasColumnAsync(connection, "Employee", "conatct");
                var hasEmailColumn = await HasColumnAsync(connection, "Employee", "email");
                var phoneColumn = hasCorrectPhoneColumn ? "contact" : hasLegacyPhoneColumn ? "conatct" : null;
                var emailColumn = hasEmailColumn ? "email" : null;
                var phoneSelect = phoneColumn == null ? "CAST(NULL AS NVARCHAR(100))" : phoneColumn;
                var emailSelect = emailColumn == null ? "CAST(NULL AS NVARCHAR(200))" : emailColumn;
                var recipientClause = BuildRecipientClause(phoneColumn, emailColumn);
                var schoolClause = hasSchoolId ? "(SchoolId = @SchoolId OR SchoolId IS NULL)" : "1=1";
                var sql = $@"
SELECT fullName AS DisplayName, {phoneSelect} AS Phone, {emailSelect} AS Email
FROM Employee
WHERE {schoolClause}
  AND ({recipientClause});";

                using (var command = new SqlCommand(sql, connection))
                {
                    if (hasSchoolId) command.Parameters.AddWithValue("@SchoolId", schoolId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            table.Rows.Add("Employee", reader["DisplayName"]?.ToString() ?? "", reader["Phone"]?.ToString() ?? "", reader["Email"]?.ToString() ?? "");
                        }
                    }
                }
            }
        }

        private async Task EnsureTableAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
IF OBJECT_ID(N'Notices', N'U') IS NULL
BEGIN
    CREATE TABLE Notices (
        NoticeID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        Target NVARCHAR(50) NOT NULL,
        TargetClass NVARCHAR(80) NULL,
        Channel NVARCHAR(50) NOT NULL,
        SentBy NVARCHAR(120) NOT NULL,
        SentDate DATETIME NOT NULL,
        RecipientCount INT NOT NULL CONSTRAINT DF_Notices_RecipientCount DEFAULT 0,
        [Status] NVARCHAR(255) NOT NULL CONSTRAINT DF_Notices_Status DEFAULT 'Saved',
        SchoolId UNIQUEIDENTIFIER NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Notices_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notices_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'SchoolId') IS NULL
    ALTER TABLE Notices ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'Title') IS NULL
    ALTER TABLE Notices ADD Title NVARCHAR(200) NOT NULL CONSTRAINT DF_Notices_Title_Upgrade DEFAULT '';
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'Message') IS NULL
    ALTER TABLE Notices ADD [Message] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Notices_Message_Upgrade DEFAULT '';
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'Target') IS NULL
    ALTER TABLE Notices ADD Target NVARCHAR(50) NOT NULL CONSTRAINT DF_Notices_Target_Upgrade DEFAULT 'All';
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'TargetClass') IS NULL
    ALTER TABLE Notices ADD TargetClass NVARCHAR(80) NULL;
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'Channel') IS NULL
    ALTER TABLE Notices ADD Channel NVARCHAR(50) NOT NULL CONSTRAINT DF_Notices_Channel_Upgrade DEFAULT 'Both';
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'SentBy') IS NULL
    ALTER TABLE Notices ADD SentBy NVARCHAR(120) NOT NULL CONSTRAINT DF_Notices_SentBy_Upgrade DEFAULT 'System';
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'SentDate') IS NULL
    ALTER TABLE Notices ADD SentDate DATETIME NOT NULL CONSTRAINT DF_Notices_SentDate_Upgrade DEFAULT GETDATE();
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'RecipientCount') IS NULL
    ALTER TABLE Notices ADD RecipientCount INT NOT NULL CONSTRAINT DF_Notices_RecipientCount_Upgrade DEFAULT 0;
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'Status') IS NULL
    ALTER TABLE Notices ADD [Status] NVARCHAR(255) NOT NULL CONSTRAINT DF_Notices_Status_Upgrade DEFAULT 'Saved';
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'Status') IS NOT NULL AND COL_LENGTH('Notices', 'Status') < 510
    ALTER TABLE Notices ALTER COLUMN [Status] NVARCHAR(255) NOT NULL;
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'SyncId') IS NULL
    ALTER TABLE Notices ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Notices_SyncId_Upgrade DEFAULT NEWID();
IF OBJECT_ID(N'Notices', N'U') IS NOT NULL AND COL_LENGTH('Notices', 'UpdatedAt') IS NULL
    ALTER TABLE Notices ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notices_UpdatedAt_Upgrade DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notices_School_SentDate' AND object_id = OBJECT_ID(N'Notices'))
    CREATE INDEX IX_Notices_School_SentDate ON Notices(SchoolId, SentDate DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Notices_SyncId' AND object_id = OBJECT_ID(N'Notices'))
    CREATE UNIQUE INDEX UX_Notices_SyncId ON Notices(SyncId);", connection))
                {
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<int> CountRecipientsAsync(string target, string targetClass, Guid schoolId)
        {
            if (string.Equals(target, "Employees", StringComparison.OrdinalIgnoreCase))
                return await CountTableAsync("Employee", null, schoolId);

            if (string.Equals(target, "Parents", StringComparison.OrdinalIgnoreCase))
                return await CountParentsAsync(schoolId);

            if (string.Equals(target, "Specific Class", StringComparison.OrdinalIgnoreCase))
                return await CountTableAsync("Students", targetClass, schoolId);

            var students = await CountTableAsync("Students", null, schoolId);
            var employees = await CountTableAsync("Employee", null, schoolId);
            return students + employees;
        }

        private async Task<int> CountTableAsync(string tableName, string className, Guid schoolId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var hasSchoolId = await HasColumnAsync(connection, tableName, "SchoolId");
                var schoolClause = hasSchoolId ? "(SchoolId = @SchoolId OR SchoolId IS NULL)" : "1=1";
                var sql = tableName == "Students"
                    ? "SELECT COUNT(*) FROM Students WHERE " + schoolClause + (string.IsNullOrWhiteSpace(className) ? "" : " AND ClassID = @ClassID")
                    : "SELECT COUNT(*) FROM Employee WHERE " + schoolClause;

                using (var command = new SqlCommand(sql, connection))
                {
                    if (hasSchoolId) command.Parameters.AddWithValue("@SchoolId", schoolId);
                    if (!string.IsNullOrWhiteSpace(className)) command.Parameters.AddWithValue("@ClassID", className);
                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private async Task<int> CountParentsAsync(Guid schoolId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var hasSchoolId = await HasColumnAsync(connection, "Students", "SchoolId");
                var hasCorrectEmergencyColumn = await HasColumnAsync(connection, "Students", "EmergencyContact");
                var hasLegacyEmergencyColumn = await HasColumnAsync(connection, "Students", "EmergencyConatct");
                var hasCorrectEmailColumn = await HasColumnAsync(connection, "Students", "GuidanceEmail");
                var hasLegacyEmailColumn = await HasColumnAsync(connection, "Students", "GuidianceEmail");
                var emergencyColumn = hasCorrectEmergencyColumn
                    ? "EmergencyContact"
                    : hasLegacyEmergencyColumn ? "EmergencyConatct" : null;
                var emailColumn = hasCorrectEmailColumn
                    ? "GuidanceEmail"
                    : hasLegacyEmailColumn ? "GuidianceEmail" : null;
                var recipientClause = BuildRecipientClause(emergencyColumn, emailColumn);
                var schoolClause = hasSchoolId ? "(SchoolId = @SchoolId OR SchoolId IS NULL)" : "1=1";
                var sql = $@"
SELECT COUNT(*)
FROM Students
WHERE {schoolClause}
  AND ({recipientClause});";

                using (var command = new SqlCommand(sql, connection))
                {
                    if (hasSchoolId) command.Parameters.AddWithValue("@SchoolId", schoolId);
                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private static async Task<bool> HasColumnAsync(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT COL_LENGTH(@TableName, @ColumnName)", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static string BuildRecipientClause(params string[] columns)
        {
            var checks = new List<string>();
            foreach (var column in columns)
            {
                if (string.IsNullOrWhiteSpace(column)) continue;
                checks.Add($"({column} IS NOT NULL AND LTRIM(RTRIM({column})) <> '')");
            }

            return checks.Count == 0 ? "1=0" : string.Join(" OR ", checks);
        }

        private static Notice MapNotice(SqlDataReader reader)
        {
            return new Notice
            {
                NoticeID = reader.GetInt32(0),
                Title = reader.GetString(1),
                Message = reader.GetString(2),
                Target = reader.GetString(3),
                TargetClass = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Channel = reader.GetString(5),
                SentBy = reader.GetString(6),
                SentDate = reader.GetDateTime(7),
                RecipientCount = reader.GetInt32(8),
                Status = reader.GetString(9)
            };
        }

        private static string NormalizeTarget(string target)
        {
            target = (target ?? "").Trim();
            return string.IsNullOrWhiteSpace(target) ? "All" : target;
        }

        private static string NormalizeChannel(string channel)
        {
            channel = (channel ?? "").Trim();
            if (channel.StartsWith("Both", StringComparison.OrdinalIgnoreCase)) return "Both";
            if (channel.StartsWith("SMS", StringComparison.OrdinalIgnoreCase)) return "SMS";
            if (channel.StartsWith("Email", StringComparison.OrdinalIgnoreCase)) return "Email";
            return string.IsNullOrWhiteSpace(channel) ? "Both" : channel;
        }

        private async Task TryRecordSyncUpsertAsync(int noticeId, string operation)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync("Notices", "NoticeID", noticeId, operation);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Notice sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(int noticeId)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync("Notices", "NoticeID", noticeId);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Notice delete sync capture skipped: " + ex.Message);
            }
        }
    }
}
