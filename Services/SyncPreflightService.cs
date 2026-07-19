using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using Microsoft.Data.SqlClient;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public sealed class SyncPreflightService
    {
        private readonly string _connectionString;

        public SyncPreflightService() : this(AppConfig.ConnectionString)
        {
        }

        public SyncPreflightService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<SyncPreflightResult> RunAsync()
        {
            var result = new SyncPreflightResult();
            result.SyncConfigured = AppConfig.Sync.IsConfigured;
            result.EndpointConfigured = !string.IsNullOrWhiteSpace(AppConfig.Sync.EndpointBaseUrl);
            result.ApiKeyConfigured = !string.IsNullOrWhiteSpace(AppConfig.Sync.ApiKey);
            result.SyncTableCount = SyncSchema.SyncTables.Length;

            if (!result.EndpointConfigured) result.Warnings.Add("Web endpoint is not configured.");
            if (!result.ApiKeyConfigured) result.Warnings.Add("Sync API key is not configured.");

            try
            {
                result.SchoolId = TenantContext.RequireSchoolId();
            }
            catch (Exception ex)
            {
                result.Errors.Add("School identity is missing: " + ex.Message);
                return result;
            }

            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    await SyncSchema.EnsureSyncInfrastructureAsync(connection, result.SchoolId);

                    result.DeviceReady = await EnsureDeviceReadyAsync(result.SchoolId);
                    result.PendingUploadCount = await new OfflineSyncService().CountPendingUploadsAsync();
                    await InspectSyncTablesAsync(connection, result);
                    await InspectOutboxAsync(connection, result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add("Local sync database check failed: " + ex.Message);
            }

            return result;
        }

        private async Task<bool> EnsureDeviceReadyAsync(Guid schoolId)
        {
            try
            {
                var device = await new SyncOutboxRepository(_connectionString).EnsureDeviceAsync(schoolId);
                return device != null && device.DeviceId != Guid.Empty;
            }
            catch (Exception ex)
            {
                throw new DataException("Could not create or load the desktop sync device. " + ex.Message, ex);
            }
        }

        private static async Task InspectSyncTablesAsync(SqlConnection connection, SyncPreflightResult result)
        {
            foreach (var table in SyncSchema.SyncTables)
            {
                if (!await TableExistsAsync(connection, table))
                {
                    result.MissingTables.Add(table);
                    continue;
                }

                var missingColumns = new List<string>();
                if (!await ColumnExistsAsync(connection, table, "SyncId")) missingColumns.Add("SyncId");
                if (!await ColumnExistsAsync(connection, table, "UpdatedAt")) missingColumns.Add("UpdatedAt");
                if (!await ColumnExistsAsync(connection, table, "RowVersion")) missingColumns.Add("RowVersion");

                if (missingColumns.Count > 0)
                    result.TablesMissingSyncColumns.Add(table + " (" + string.Join(", ", missingColumns) + ")");
                else
                    result.ReadyTableCount++;
            }

            if (result.TablesMissingSyncColumns.Count > 0)
                result.Errors.Add("Some sync tables are missing required sync columns.");

            var importantMissing = result.MissingTables
                .Where(t => !string.Equals(t, "Notices", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (importantMissing.Count > 0)
                result.Warnings.Add("Some syncable tables do not exist yet: " + string.Join(", ", importantMissing.Take(6)) + (importantMissing.Count > 6 ? "..." : ""));
        }

        private static async Task InspectOutboxAsync(SqlConnection connection, SyncPreflightResult result)
        {
            if (!await TableExistsAsync(connection, "SyncOutbox"))
            {
                result.Errors.Add("SyncOutbox table is missing.");
                return;
            }

            using (var cmd = new SqlCommand(@"
SELECT COUNT(*)
FROM SyncOutbox
WHERE Status = 'Pending'
  AND (RecordSyncId IS NULL OR Payload IS NULL OR LTRIM(RTRIM(Payload)) = '')", connection))
            {
                var invalid = await cmd.ExecuteScalarAsync();
                result.InvalidPendingUploadCount = invalid == null || invalid == DBNull.Value ? 0 : Convert.ToInt32(invalid);
                if (result.InvalidPendingUploadCount > 0)
                    result.Errors.Add(result.InvalidPendingUploadCount + " pending sync change(s) are missing payload or SyncId.");
            }
        }

        private static async Task<bool> TableExistsAsync(SqlConnection connection, string table)
        {
            using (var cmd = new SqlCommand("SELECT OBJECT_ID(@TableName, N'U')", connection))
            {
                cmd.Parameters.AddWithValue("@TableName", table);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string table, string column)
        {
            using (var cmd = new SqlCommand("SELECT COL_LENGTH(@TableName, @ColumnName)", connection))
            {
                cmd.Parameters.AddWithValue("@TableName", table);
                cmd.Parameters.AddWithValue("@ColumnName", column);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }
    }

    public sealed class SyncPreflightResult
    {
        public bool SyncConfigured { get; set; }
        public bool EndpointConfigured { get; set; }
        public bool ApiKeyConfigured { get; set; }
        public bool DeviceReady { get; set; }
        public Guid SchoolId { get; set; }
        public int SyncTableCount { get; set; }
        public int ReadyTableCount { get; set; }
        public int PendingUploadCount { get; set; }
        public int InvalidPendingUploadCount { get; set; }
        public List<string> MissingTables { get; } = new List<string>();
        public List<string> TablesMissingSyncColumns { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();

        public bool IsReady => Errors.Count == 0 && EndpointConfigured && ApiKeyConfigured && DeviceReady;

        public string Summary
        {
            get
            {
                if (Errors.Count > 0) return "Blocked: " + Errors[0];
                if (!EndpointConfigured || !ApiKeyConfigured) return "Setup needed: endpoint or API key is missing.";
                if (Warnings.Count > 0) return "Ready with warnings: " + Warnings[0];
                return "Ready: local sync infrastructure is healthy.";
            }
        }

        public string ToReportText()
        {
            var lines = new List<string>
            {
                Summary,
                "SchoolId: " + (SchoolId == Guid.Empty ? "missing" : SchoolId.ToString("D")),
                "Device: " + (DeviceReady ? "ready" : "not ready"),
                "Endpoint: " + (EndpointConfigured ? "configured" : "missing"),
                "API key: " + (ApiKeyConfigured ? "configured" : "missing"),
                "Sync tables ready: " + ReadyTableCount + " of " + SyncTableCount,
                "Pending uploads: " + PendingUploadCount
            };

            if (InvalidPendingUploadCount > 0)
                lines.Add("Invalid pending uploads: " + InvalidPendingUploadCount);
            if (MissingTables.Count > 0)
                lines.Add("Missing tables: " + string.Join(", ", MissingTables.Take(10)) + (MissingTables.Count > 10 ? "..." : ""));
            if (TablesMissingSyncColumns.Count > 0)
                lines.Add("Tables missing sync columns: " + string.Join(", ", TablesMissingSyncColumns.Take(10)) + (TablesMissingSyncColumns.Count > 10 ? "..." : ""));
            if (Warnings.Count > 0)
                lines.Add("Warnings: " + string.Join(" | ", Warnings));
            if (Errors.Count > 0)
                lines.Add("Errors: " + string.Join(" | ", Errors));

            return string.Join(Environment.NewLine, lines);
        }
    }
}
