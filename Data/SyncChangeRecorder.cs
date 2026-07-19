using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using Microsoft.Data.SqlClient;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class SyncChangeRecorder
    {
        private static readonly Regex Identifier = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private readonly string _connectionString;
        private readonly SyncOutboxRepository _outbox;

        public SyncChangeRecorder(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _outbox = new SyncOutboxRepository(connectionString);
        }

        public async Task RecordUpsertAsync(string tableName, string primaryKeyName, object primaryKeyValue, string operation)
        {
            if (string.Equals(operation, "Delete", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Use RecordDeleteAsync for delete operations.", nameof(operation));

            var schoolId = TenantContext.RequireSchoolId();
            var device = await _outbox.EnsureDeviceAsync(schoolId);
            var row = await LoadRowPayloadAsync(tableName, primaryKeyName, primaryKeyValue, schoolId, touchUpdatedAt: true);
            if (row.SyncId == Guid.Empty || string.IsNullOrWhiteSpace(row.Payload)) return;

            await _outbox.EnqueueChangeAsync(
                schoolId,
                device.DeviceId,
                tableName,
                row.SyncId,
                primaryKeyName,
                Convert.ToString(primaryKeyValue),
                NormalizeOperation(operation),
                row.Payload);
        }

        public async Task RecordDeleteAsync(string tableName, string primaryKeyName, object primaryKeyValue)
        {
            var schoolId = TenantContext.RequireSchoolId();
            var device = await _outbox.EnsureDeviceAsync(schoolId);
            var row = await LoadRowPayloadAsync(tableName, primaryKeyName, primaryKeyValue, schoolId, touchUpdatedAt: false);
            if (row.SyncId == Guid.Empty) return;

            await _outbox.EnqueueChangeAsync(
                schoolId,
                device.DeviceId,
                tableName,
                row.SyncId,
                primaryKeyName,
                Convert.ToString(primaryKeyValue),
                "Delete",
                row.Payload);
        }

        public async Task RecordUpsertsAsync(string tableName, string primaryKeyName, params object[] primaryKeyValues)
        {
            if (primaryKeyValues == null) return;
            foreach (var value in primaryKeyValues.Where(v => v != null))
            {
                await RecordUpsertAsync(tableName, primaryKeyName, value, "Update");
            }
        }

        public async Task RecordUpsertBySyncIdAsync(string tableName, Guid syncId, string operation)
        {
            if (syncId == Guid.Empty) return;

            var schoolId = TenantContext.RequireSchoolId();
            var device = await _outbox.EnsureDeviceAsync(schoolId);
            var row = await LoadRowPayloadBySyncIdAsync(tableName, syncId, schoolId, touchUpdatedAt: true);
            if (row.SyncId == Guid.Empty || string.IsNullOrWhiteSpace(row.Payload)) return;

            await _outbox.EnqueueChangeAsync(
                schoolId,
                device.DeviceId,
                tableName,
                row.SyncId,
                "SyncId",
                syncId.ToString("D"),
                NormalizeOperation(operation),
                row.Payload);
        }

        private async Task<RowPayload> LoadRowPayloadAsync(string tableName, string primaryKeyName, object primaryKeyValue, Guid schoolId, bool touchUpdatedAt)
        {
            EnsureSafeTable(tableName);
            EnsureSafeIdentifier(primaryKeyName, nameof(primaryKeyName));

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);

                if (!await ColumnExistsAsync(c, tableName, primaryKeyName)) return new RowPayload();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, tableName);
                var tableSql = Quote(tableName);
                var pkSql = Quote(primaryKeyName);
                var tenantClause = tenant ? " AND SchoolId = @SchoolId" : "";

                if (touchUpdatedAt && await ColumnExistsAsync(c, tableName, "UpdatedAt"))
                {
                    using (var touch = new SqlCommand("UPDATE " + tableSql + " SET UpdatedAt = SYSUTCDATETIME() WHERE " + pkSql + " = @PrimaryKeyValue" + tenantClause, c))
                    {
                        touch.Parameters.AddWithValue("@PrimaryKeyValue", primaryKeyValue ?? DBNull.Value);
                        if (tenant) touch.Parameters.AddWithValue("@SchoolId", schoolId);
                        await touch.ExecuteNonQueryAsync();
                    }
                }

                using (var cmd = new SqlCommand(@"
DECLARE @SyncId UNIQUEIDENTIFIER;
SELECT TOP 1 @SyncId = SyncId
FROM " + tableSql + @"
WHERE " + pkSql + @" = @PrimaryKeyValue" + tenantClause + @";

SELECT @SyncId AS SyncId,
       (
           SELECT TOP 1 *
           FROM " + tableSql + @"
           WHERE " + pkSql + @" = @PrimaryKeyValue" + tenantClause + @"
           FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
       ) AS Payload;", c))
                {
                    cmd.Parameters.AddWithValue("@PrimaryKeyValue", primaryKeyValue ?? DBNull.Value);
                    if (tenant) cmd.Parameters.AddWithValue("@SchoolId", schoolId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync()) return new RowPayload();
                        return new RowPayload
                        {
                            SyncId = reader["SyncId"] == DBNull.Value ? Guid.Empty : (Guid)reader["SyncId"],
                            Payload = reader["Payload"] == DBNull.Value ? "" : reader["Payload"].ToString()
                        };
                    }
                }
            }
        }

        private async Task<RowPayload> LoadRowPayloadBySyncIdAsync(string tableName, Guid syncId, Guid schoolId, bool touchUpdatedAt)
        {
            EnsureSafeTable(tableName);

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);

                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, tableName);
                var tableSql = Quote(tableName);
                var tenantClause = tenant ? " AND SchoolId = @SchoolId" : "";

                if (touchUpdatedAt && await ColumnExistsAsync(c, tableName, "UpdatedAt"))
                {
                    using (var touch = new SqlCommand("UPDATE " + tableSql + " SET UpdatedAt = SYSUTCDATETIME() WHERE SyncId = @SyncId" + tenantClause, c))
                    {
                        touch.Parameters.AddWithValue("@SyncId", syncId);
                        if (tenant) touch.Parameters.AddWithValue("@SchoolId", schoolId);
                        await touch.ExecuteNonQueryAsync();
                    }
                }

                using (var cmd = new SqlCommand(@"
SELECT @SyncId AS SyncId,
       (
           SELECT TOP 1 *
           FROM " + tableSql + @"
           WHERE SyncId = @SyncId" + tenantClause + @"
           FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
       ) AS Payload;", c))
                {
                    cmd.Parameters.AddWithValue("@SyncId", syncId);
                    if (tenant) cmd.Parameters.AddWithValue("@SchoolId", schoolId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync()) return new RowPayload();
                        return new RowPayload
                        {
                            SyncId = reader["SyncId"] == DBNull.Value ? Guid.Empty : (Guid)reader["SyncId"],
                            Payload = reader["Payload"] == DBNull.Value ? "" : reader["Payload"].ToString()
                        };
                    }
                }
            }
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName)
        {
            using (var cmd = new SqlCommand("SELECT COL_LENGTH(@TableName, @ColumnName)", connection))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static void EnsureSafeTable(string tableName)
        {
            EnsureSafeIdentifier(tableName, nameof(tableName));
            if (!SyncSchema.SyncTables.Any(t => string.Equals(t, tableName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Table is not configured for sync: " + tableName);
        }

        private static void EnsureSafeIdentifier(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !Identifier.IsMatch(value))
                throw new ArgumentException("Invalid SQL identifier.", parameterName);
        }

        private static string Quote(string identifier)
        {
            EnsureSafeIdentifier(identifier, nameof(identifier));
            return "[" + identifier + "]";
        }

        private static string NormalizeOperation(string operation)
        {
            return string.Equals(operation, "Insert", StringComparison.OrdinalIgnoreCase) ? "Insert" : "Update";
        }

        private struct RowPayload
        {
            public Guid SyncId;
            public string Payload;
        }
    }
}
