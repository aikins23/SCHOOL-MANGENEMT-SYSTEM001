using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class SyncOutboxRepository
    {
        private readonly string _connectionString;
        private const string DevicesTable = "SyncDevices";
        private const string OutboxTable = "SyncOutbox";

        public SyncOutboxRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<SyncDeviceInfo> EnsureDeviceAsync(Guid schoolId)
        {
            if (schoolId == Guid.Empty) throw new ArgumentException("SchoolId is required.", nameof(schoolId));

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);

                var machineName = Environment.MachineName ?? "";
                using (var cmd = new SqlCommand($@"
                    SELECT TOP 1 *
                    FROM {DevicesTable}
                    WHERE SchoolId = @p0 AND MachineName = @p1 AND IsActive = 1
                    ORDER BY CreatedAt", c))
                {
                    cmd.AddPositionalParameter( schoolId);
                    cmd.AddPositionalParameter( machineName);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var device = MapDevice(reader);
                            await TouchDeviceAsync(c, device.DeviceId);
                            return device;
                        }
                    }
                }

                var newDevice = new SyncDeviceInfo
                {
                    DeviceId = Guid.NewGuid(),
                    SchoolId = schoolId,
                    DeviceName = machineName,
                    MachineName = machineName,
                    CreatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                    IsActive = true
                };

                using (var cmd = new SqlCommand($@"
                    INSERT INTO {DevicesTable}
                        (DeviceId, SchoolId, DeviceName, MachineName, CreatedAt, LastSeenAt, IsActive)
                    VALUES (@p0, @p1, @p2, @p3, SYSUTCDATETIME(), SYSUTCDATETIME(), 1)", c))
                {
                    cmd.AddPositionalParameter( newDevice.DeviceId);
                    cmd.AddPositionalParameter( newDevice.SchoolId);
                    cmd.AddPositionalParameter( newDevice.DeviceName ?? "");
                    cmd.AddPositionalParameter( newDevice.MachineName ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }

                return newDevice;
            }
        }

        public async Task<List<SyncOutboxEntry>> GetPendingAsync(int batchSize = 100)
        {
            var list = new List<SyncOutboxEntry>();
            if (batchSize < 1) batchSize = 1;

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var sql = $@"
                    SELECT TOP {batchSize} *
                    FROM {OutboxTable}
                    WHERE Status = 'Pending'
                    ORDER BY OutboxId";
                using (var cmd = new SqlCommand(sql, c))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) list.Add(MapOutbox(reader));
                }
            }

            return list;
        }

        public async Task EnqueueChangeAsync(
            Guid schoolId,
            Guid deviceId,
            string tableName,
            Guid? recordSyncId,
            string primaryKeyName,
            string primaryKeyValue,
            string operation,
            string payload)
        {
            if (schoolId == Guid.Empty) throw new ArgumentException("SchoolId is required.", nameof(schoolId));
            if (deviceId == Guid.Empty) throw new ArgumentException("DeviceId is required.", nameof(deviceId));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("TableName is required.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(primaryKeyName)) throw new ArgumentException("PrimaryKeyName is required.", nameof(primaryKeyName));
            if (string.IsNullOrWhiteSpace(primaryKeyValue)) throw new ArgumentException("PrimaryKeyValue is required.", nameof(primaryKeyValue));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation is required.", nameof(operation));

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);

                using (var cmd = new SqlCommand($@"
                    INSERT INTO {OutboxTable}
                        (SchoolId, DeviceId, TableName, RecordSyncId, PrimaryKeyName, PrimaryKeyValue, Operation, Payload, Status, CreatedAt, UpdatedAt)
                    VALUES
                        (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, 'Pending', SYSUTCDATETIME(), SYSUTCDATETIME())", c))
                {
                    cmd.AddPositionalParameter(schoolId);
                    cmd.AddPositionalParameter(deviceId);
                    cmd.AddPositionalParameter(Trim(tableName, 128));
                    cmd.AddPositionalParameter((object)recordSyncId ?? DBNull.Value);
                    cmd.AddPositionalParameter(Trim(primaryKeyName, 128));
                    cmd.AddPositionalParameter(Trim(primaryKeyValue, 120));
                    cmd.AddPositionalParameter(Trim(operation, 12));
                    cmd.AddPositionalParameter(string.IsNullOrWhiteSpace(payload) ? (object)DBNull.Value : payload);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task MarkSentAsync(IEnumerable<long> outboxIds)
        {
            if (outboxIds == null) return;

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                foreach (var id in outboxIds)
                {
                    using (var cmd = new SqlCommand($@"
                        UPDATE {OutboxTable}
                        SET Status = 'Sent', SentAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), LastError = NULL
                        WHERE OutboxId = @p0", c))
                    {
                        cmd.AddPositionalParameter( id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task MarkFailedAsync(IEnumerable<long> outboxIds, string error)
        {
            if (outboxIds == null) return;

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                foreach (var id in outboxIds)
                {
                    using (var cmd = new SqlCommand($@"
                        UPDATE {OutboxTable}
                        SET Attempts = Attempts + 1,
                            LastError = @p0,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE OutboxId = @p1 AND Status = 'Pending'", c))
                    {
                        cmd.AddPositionalParameter( Trim(error, 500));
                        cmd.AddPositionalParameter( id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<int> CountPendingAsync()
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand($"SELECT COUNT(*) FROM {OutboxTable} WHERE Status = 'Pending'", c))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
        }

        public async Task<SyncOutboxSummary> GetSummaryAsync()
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand($@"
SELECT
    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
    SUM(CASE WHEN Status = 'Pending' AND (Attempts > 0 OR LastError IS NOT NULL) THEN 1 ELSE 0 END) AS ProblemCount,
    SUM(CASE WHEN Status = 'Sent' THEN 1 ELSE 0 END) AS SentCount,
    MAX(CASE WHEN Status = 'Sent' THEN SentAt ELSE NULL END) AS LastSentAt,
    MAX(UpdatedAt) AS LastUpdatedAt
FROM {OutboxTable};

SELECT TOP 1 LastError
FROM {OutboxTable}
WHERE Status = 'Pending' AND LastError IS NOT NULL
ORDER BY UpdatedAt DESC, OutboxId DESC;", c))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var summary = new SyncOutboxSummary();
                    if (await reader.ReadAsync())
                    {
                        summary.PendingCount = AsInt(reader["PendingCount"]);
                        summary.ProblemCount = AsInt(reader["ProblemCount"]);
                        summary.SentCount = AsInt(reader["SentCount"]);
                        summary.LastSentAt = reader["LastSentAt"] == DBNull.Value ? (DateTime?)null : AsDate(reader["LastSentAt"]);
                        summary.LastUpdatedAt = reader["LastUpdatedAt"] == DBNull.Value ? (DateTime?)null : AsDate(reader["LastUpdatedAt"]);
                    }

                    if (await reader.NextResultAsync() && await reader.ReadAsync())
                        summary.LatestError = AsString(reader["LastError"]);

                    return summary;
                }
            }
        }

        public async Task<List<SyncOutboxIssueRow>> GetIssueRowsAsync(int take = 200)
        {
            if (take < 1) take = 1;
            if (take > 500) take = 500;

            var rows = new List<SyncOutboxIssueRow>();
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand($@"
SELECT TOP {take}
    OutboxId,
    TableName,
    PrimaryKeyName,
    PrimaryKeyValue,
    Operation,
    Attempts,
    LastError,
    CreatedAt,
    UpdatedAt
FROM {OutboxTable}
WHERE Status = 'Pending'
  AND (Attempts > 0 OR LastError IS NOT NULL)
ORDER BY UpdatedAt DESC, OutboxId DESC", c))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rows.Add(new SyncOutboxIssueRow
                        {
                            OutboxId = Convert.ToInt64(reader["OutboxId"]),
                            TableName = AsString(reader["TableName"]),
                            PrimaryKeyName = AsString(reader["PrimaryKeyName"]),
                            PrimaryKeyValue = AsString(reader["PrimaryKeyValue"]),
                            Operation = AsString(reader["Operation"]),
                            Attempts = AsInt(reader["Attempts"]),
                            LastError = AsString(reader["LastError"]),
                            CreatedAt = AsDate(reader["CreatedAt"]),
                            UpdatedAt = AsDate(reader["UpdatedAt"])
                        });
                    }
                }
            }

            return rows;
        }

        private static Task TouchDeviceAsync(SqlConnection c, Guid deviceId)
        {
            using (var cmd = new SqlCommand($@"
                UPDATE {DevicesTable}
                SET LastSeenAt = SYSUTCDATETIME()
                WHERE DeviceId = @p0", c))
            {
                cmd.AddPositionalParameter( deviceId);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        private static SyncDeviceInfo MapDevice(IDataRecord r) => new SyncDeviceInfo
        {
            DeviceId = AsGuid(r["DeviceId"]),
            SchoolId = AsGuid(r["SchoolId"]),
            DeviceName = AsString(r["DeviceName"]),
            MachineName = AsString(r["MachineName"]),
            CreatedAt = AsDate(r["CreatedAt"]),
            LastSeenAt = AsDate(r["LastSeenAt"]),
            IsActive = AsBool(r["IsActive"])
        };

        private static SyncOutboxEntry MapOutbox(IDataRecord r) => new SyncOutboxEntry
        {
            OutboxId = Convert.ToInt64(r["OutboxId"]),
            SchoolId = AsGuid(r["SchoolId"]),
            DeviceId = AsGuid(r["DeviceId"]),
            TableName = AsString(r["TableName"]),
            RecordSyncId = r["RecordSyncId"] == DBNull.Value ? (Guid?)null : AsGuid(r["RecordSyncId"]),
            PrimaryKeyName = AsString(r["PrimaryKeyName"]),
            PrimaryKeyValue = AsString(r["PrimaryKeyValue"]),
            Operation = AsString(r["Operation"]),
            Payload = AsString(r["Payload"]),
            Status = AsString(r["Status"]),
            Attempts = r["Attempts"] == DBNull.Value ? 0 : Convert.ToInt32(r["Attempts"]),
            LastError = AsString(r["LastError"]),
            CreatedAt = AsDate(r["CreatedAt"]),
            UpdatedAt = AsDate(r["UpdatedAt"]),
            SentAt = r["SentAt"] == DBNull.Value ? (DateTime?)null : AsDate(r["SentAt"])
        };

        private static Guid AsGuid(object value)
        {
            if (value == null || value == DBNull.Value) return Guid.Empty;
            if (value is Guid id) return id;
            Guid parsed;
            return Guid.TryParse(value.ToString(), out parsed) ? parsed : Guid.Empty;
        }

        private static DateTime AsDate(object value) =>
            value == null || value == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(value);

        private static bool AsBool(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool b) return b;
            int i;
            return int.TryParse(value.ToString(), out i) ? i != 0 : string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string AsString(object value) => value == null || value == DBNull.Value ? "" : value.ToString();

        private static int AsInt(object value) => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }

    public class SyncOutboxSummary
    {
        public int PendingCount { get; set; }
        public int ProblemCount { get; set; }
        public int SentCount { get; set; }
        public DateTime? LastSentAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string LatestError { get; set; } = "";
    }

    public class SyncOutboxIssueRow
    {
        public long OutboxId { get; set; }
        public string TableName { get; set; } = "";
        public string PrimaryKeyName { get; set; } = "";
        public string PrimaryKeyValue { get; set; } = "";
        public string Operation { get; set; } = "";
        public int Attempts { get; set; }
        public string LastError { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
