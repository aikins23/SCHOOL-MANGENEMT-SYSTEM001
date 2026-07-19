using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public sealed class PerformanceReportRepository
    {
        private readonly string _connectionString;

        public PerformanceReportRepository() : this(AppConfig.ConnectionString) { }

        public PerformanceReportRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<List<ClassPerformanceReport>> GetRecentAsync(int take = 50)
        {
            await ReportSchema.EnsureReportTablesAsync();
            var schoolId = TenantContext.RequireSchoolId();
            var reports = new List<ClassPerformanceReport>();

            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT TOP (@Take)
    r.ReportId, r.ClassId, r.TeacherId, r.ReportDate, r.ReportPeriod, r.AcademicYear, r.Term,
    r.WeekNumber, r.MonthNumber, r.ReportText, r.AnalyticsData, r.WorkflowId,
    r.SchoolId, r.SyncId, r.UpdatedAt
FROM ClassPerformanceReports r
WHERE r.SchoolId = @SchoolId
ORDER BY r.ReportDate DESC, r.ReportId DESC;", connection))
                {
                    command.Parameters.AddWithValue("@Take", Math.Max(1, Math.Min(take, 200)));
                    command.Parameters.AddWithValue("@SchoolId", schoolId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            reports.Add(MapReport(reader));
                    }
                }
            }

            return reports;
        }

        public async Task<List<ClassPerformanceReport>> GetPendingApprovalAsync()
        {
            await ReportSchema.EnsureReportTablesAsync();
            var schoolId = TenantContext.RequireSchoolId();
            var reports = new List<ClassPerformanceReport>();

            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT
    r.ReportId, r.ClassId, r.TeacherId, r.ReportDate, r.ReportPeriod, r.AcademicYear, r.Term,
    r.WeekNumber, r.MonthNumber, r.ReportText, r.AnalyticsData, r.WorkflowId,
    r.SchoolId, r.SyncId, r.UpdatedAt
FROM ClassPerformanceReports r
INNER JOIN ApprovalWorkflows w ON w.WorkflowId = r.WorkflowId
WHERE r.SchoolId = @SchoolId
  AND w.CurrentStatus = 'Submitted'
ORDER BY r.ReportDate DESC, r.ReportId DESC;", connection))
                {
                    command.Parameters.AddWithValue("@SchoolId", schoolId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            reports.Add(MapReport(reader));
                    }
                }
            }

            return reports;
        }

        public async Task<List<StudentPerformanceEntry>> GetEntriesAsync(int reportId)
        {
            await ReportSchema.EnsureReportTablesAsync();
            var schoolId = TenantContext.RequireSchoolId();
            var entries = new List<StudentPerformanceEntry>();

            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT EntryId, ReportId, StudentId, PerformanceTrend,
       ExerciseMarksObtained, ExerciseMarksTotal, HomeworkMarksObtained, HomeworkMarksTotal,
       TeacherNotes, SchoolId, SyncId, UpdatedAt
FROM StudentPerformanceEntries
WHERE ReportId = @ReportId
  AND SchoolId = @SchoolId
ORDER BY StudentId;", connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);
                    command.Parameters.AddWithValue("@SchoolId", schoolId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            entries.Add(MapEntry(reader));
                    }
                }
            }

            return entries;
        }

        public async Task<ApprovalWorkflow> GetWorkflowAsync(int workflowId)
        {
            await ReportSchema.EnsureReportTablesAsync();
            var schoolId = TenantContext.RequireSchoolId();

            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT WorkflowId, EntityType, EntityId, CurrentStatus, SchoolId, SyncId, UpdatedAt
FROM ApprovalWorkflows
WHERE WorkflowId = @WorkflowId
  AND SchoolId = @SchoolId;", connection))
                {
                    command.Parameters.AddWithValue("@WorkflowId", workflowId);
                    command.Parameters.AddWithValue("@SchoolId", schoolId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        return await reader.ReadAsync() ? MapWorkflow(reader) : null;
                    }
                }
            }
        }

        public async Task<bool> SetApprovalStatusAsync(int workflowId, string status)
        {
            status = NormalizeStatus(status);
            await ReportSchema.EnsureReportTablesAsync();
            var schoolId = TenantContext.RequireSchoolId();

            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
UPDATE ApprovalWorkflows
SET CurrentStatus = @Status,
    SchoolId = COALESCE(SchoolId, @SchoolId),
    UpdatedAt = SYSUTCDATETIME()
WHERE WorkflowId = @WorkflowId
  AND (SchoolId = @SchoolId OR SchoolId IS NULL);", connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                    command.Parameters.AddWithValue("@WorkflowId", workflowId);
                    var saved = await command.ExecuteNonQueryAsync() > 0;
                    if (saved)
                    {
                        await new SyncChangeRecorder(_connectionString)
                            .RecordUpsertAsync("ApprovalWorkflows", "WorkflowId", workflowId, "Update");
                    }
                    return saved;
                }
            }
        }

        private static ClassPerformanceReport MapReport(SqlDataReader reader)
        {
            return new ClassPerformanceReport
            {
                ReportId = reader.GetInt32(0),
                ClassId = reader.GetString(1),
                TeacherId = reader.GetInt32(2),
                ReportDate = reader.GetDateTime(3),
                ReportPeriod = reader.GetString(4),
                AcademicYear = reader.GetString(5),
                Term = reader.GetString(6),
                WeekNumber = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                MonthNumber = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                ReportText = reader.IsDBNull(9) ? "" : reader.GetString(9),
                AnalyticsData = reader.IsDBNull(10) ? "" : reader.GetString(10),
                WorkflowId = reader.GetInt32(11),
                SchoolId = reader.IsDBNull(12) ? (Guid?)null : reader.GetGuid(12),
                SyncId = reader.IsDBNull(13) ? (Guid?)null : reader.GetGuid(13),
                UpdatedAt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14)
            };
        }

        private static StudentPerformanceEntry MapEntry(SqlDataReader reader)
        {
            return new StudentPerformanceEntry
            {
                EntryId = reader.GetInt32(0),
                ReportId = reader.GetInt32(1),
                StudentId = reader.GetString(2),
                PerformanceTrend = reader.GetString(3),
                ExerciseMarksObtained = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4),
                ExerciseMarksTotal = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                HomeworkMarksObtained = reader.IsDBNull(6) ? (decimal?)null : reader.GetDecimal(6),
                HomeworkMarksTotal = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7),
                TeacherNotes = reader.IsDBNull(8) ? "" : reader.GetString(8),
                SchoolId = reader.IsDBNull(9) ? (Guid?)null : reader.GetGuid(9),
                SyncId = reader.IsDBNull(10) ? (Guid?)null : reader.GetGuid(10),
                UpdatedAt = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11)
            };
        }

        private static ApprovalWorkflow MapWorkflow(SqlDataReader reader)
        {
            return new ApprovalWorkflow
            {
                WorkflowId = reader.GetInt32(0),
                EntityType = reader.GetString(1),
                EntityId = reader.GetInt32(2),
                CurrentStatus = reader.GetString(3),
                SchoolId = reader.IsDBNull(4) ? (Guid?)null : reader.GetGuid(4),
                SyncId = reader.IsDBNull(5) ? (Guid?)null : reader.GetGuid(5),
                UpdatedAt = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
            };
        }

        private static string NormalizeStatus(string status)
        {
            if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)) return "Approved";
            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)) return "Rejected";
            throw new ArgumentException("Approval status must be Approved or Rejected.", nameof(status));
        }
    }
}
