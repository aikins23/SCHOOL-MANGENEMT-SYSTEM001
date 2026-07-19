using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class ExamRepository : IExamRepository
    {
        private readonly string _connectionString;
        private const string EXAMS_TABLE = "examss";

        private sealed class ResultAssessmentContext
        {
            public int? ExamTypeId { get; set; }
            public int? AssessmentNumber { get; set; }
            public string AssessmentLabel { get; set; }
        }

        public ExamRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<bool> ResultExistsAsync(string studentId, string subject, string term, string year)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var assessment = await GetAssessmentContextAsync(connection, term, year);
                    var hasExamType = await HasColumnAsync(connection, EXAMS_TABLE, "ExamTypeId");
                    var hasAssessmentNumber = await HasColumnAsync(connection, EXAMS_TABLE, "AssessmentNumber");
                    var query = $"SELECT COUNT(*) FROM {EXAMS_TABLE} WHERE std_id = ? AND subject = ? AND term = ? AND year = ?";
                    if (hasExamType)
                    {
                        query += assessment.ExamTypeId.HasValue ? " AND ExamTypeId = ?" : " AND ExamTypeId IS NULL";
                    }
                    if (hasAssessmentNumber)
                    {
                        query += assessment.AssessmentNumber.HasValue ? " AND AssessmentNumber = ?" : " AND AssessmentNumber IS NULL";
                    }
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        command.AddPositionalParameter(subject);
                        command.AddPositionalParameter(term);
                        command.AddPositionalParameter(year);
                        if (hasExamType && assessment.ExamTypeId.HasValue)
                        {
                            command.AddPositionalParameter(assessment.ExamTypeId.Value);
                        }
                        if (hasAssessmentNumber && assessment.AssessmentNumber.HasValue)
                        {
                            command.AddPositionalParameter(assessment.AssessmentNumber.Value);
                        }
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error checking if result exists for Student: {studentId}, Subject: {subject}", ex);
                return false;
            }
        }

        public async Task<bool> AddResultAsync(KingdomPrep.Shared.Models.ExamResult result)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    var assessment = await GetAssessmentContextAsync(connection, result.Term, result.Year);
                    var hasExamType = await HasColumnAsync(connection, EXAMS_TABLE, "ExamTypeId");
                    var hasAssessmentNumber = await HasColumnAsync(connection, EXAMS_TABLE, "AssessmentNumber");
                    var hasAssessmentLabel = await HasColumnAsync(connection, EXAMS_TABLE, "AssessmentLabel");
                    var query = $@"
                        INSERT INTO {EXAMS_TABLE}
                        (std_name, std_class, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark, std_id, subject, term, year{(hasExamType ? ", ExamTypeId" : "")}{(hasAssessmentNumber ? ", AssessmentNumber" : "")}{(hasAssessmentLabel ? ", AssessmentLabel" : "")}{(tenant ? ", SchoolId" : "")})
                        VALUES
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?{(hasExamType ? ", ?" : "")}{(hasAssessmentNumber ? ", ?" : "")}{(hasAssessmentLabel ? ", ?" : "")}{(tenant ? ", ?" : "")})";
                    using (var command = new SqlCommand(query, connection))
                    {
                        AddResultParameters(command, result);
                        if (hasExamType) command.AddPositionalParameter((object)assessment.ExamTypeId ?? DBNull.Value);
                        if (hasAssessmentNumber) command.AddPositionalParameter((object)assessment.AssessmentNumber ?? DBNull.Value);
                        if (hasAssessmentLabel) command.AddPositionalParameter((object)assessment.AssessmentLabel ?? DBNull.Value);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var rows = await command.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            await TryRecordResultSyncAsync(result, "Insert");
                            RefreshExamSummary(result);
                        }
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error adding exam result for {result?.StudentName}", ex);
                throw new DataException("Error adding exam result", ex);
            }
        }

        public async Task<bool> UpdateResultAsync(KingdomPrep.Shared.Models.ExamResult result)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var assessment = await GetAssessmentContextAsync(connection, result.Term, result.Year);
                    var hasExamType = await HasColumnAsync(connection, EXAMS_TABLE, "ExamTypeId");
                    var hasAssessmentNumber = await HasColumnAsync(connection, EXAMS_TABLE, "AssessmentNumber");
                    var hasAssessmentLabel = await HasColumnAsync(connection, EXAMS_TABLE, "AssessmentLabel");
                    var hasUpdatedAt = await HasColumnAsync(connection, EXAMS_TABLE, "UpdatedAt");
                    var query = $@"
                        UPDATE {EXAMS_TABLE}
                        SET std_name = ?, std_class = ?, cat1 = ?, cat2 = ?, cat3 = ?, tl_cat = ?,
                            exam_score = ?, gt = ?, grade = ?, remark = ?
                            {(hasExamType ? ", ExamTypeId = ?" : "")}
                            {(hasAssessmentNumber ? ", AssessmentNumber = ?" : "")}
                            {(hasAssessmentLabel ? ", AssessmentLabel = ?" : "")}
                            {(hasUpdatedAt ? ", UpdatedAt = SYSUTCDATETIME()" : "")}
                        WHERE std_id = ? AND subject = ? AND term = ? AND year = ?";
                    if (hasExamType)
                    {
                        query += assessment.ExamTypeId.HasValue ? " AND ExamTypeId = ?" : " AND ExamTypeId IS NULL";
                    }
                    if (hasAssessmentNumber)
                    {
                        query += assessment.AssessmentNumber.HasValue ? " AND AssessmentNumber = ?" : " AND AssessmentNumber IS NULL";
                    }
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddResultParameters(command, result, includeLookupValues: false);
                        if (hasExamType) command.AddPositionalParameter((object)assessment.ExamTypeId ?? DBNull.Value);
                        if (hasAssessmentNumber) command.AddPositionalParameter((object)assessment.AssessmentNumber ?? DBNull.Value);
                        if (hasAssessmentLabel) command.AddPositionalParameter((object)assessment.AssessmentLabel ?? DBNull.Value);
                        AddResultLookupParameters(command, result);
                        if (hasExamType && assessment.ExamTypeId.HasValue)
                        {
                            command.AddPositionalParameter(assessment.ExamTypeId.Value);
                        }
                        if (hasAssessmentNumber && assessment.AssessmentNumber.HasValue)
                        {
                            command.AddPositionalParameter(assessment.AssessmentNumber.Value);
                        }
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var rows = await command.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            await TryRecordResultSyncAsync(result, "Update");
                            RefreshExamSummary(result);
                        }
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error updating exam result for {result?.StudentName}", ex);
                throw new DataException("Error updating exam result", ex);
            }
        }

        public async Task<DataTable> GetAllResultsTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    // 1. Get all unique subjects recorded in the exams table
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    var subjects = new List<string>();
                    var subjectSql = "SELECT DISTINCT subject FROM examss WHERE subject IS NOT NULL";
                    if (tenant)
                    {
                        subjectSql += TenantContext.FilterClauseSql();
                    }

                    using (var subCmd = new SqlCommand(subjectSql, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(subCmd);
                        }

                        using (var subReader = await subCmd.ExecuteReaderAsync())
                        {
                            while (await subReader.ReadAsync())
                            {
                                subjects.Add(subReader["subject"].ToString());
                            }
                        }
                    }

                    if (subjects.Count == 0) return table;

                    // 2. Dynamically build the pivot query
                    var pivotColumns = new System.Text.StringBuilder();
                    var selectColumns = new System.Text.StringBuilder();

                    foreach (var s in subjects)
                    {
                        string safeName = s.Replace("'", "''");
                        // We use a simplified slug for column names to avoid special character issues in DataTable/SQL
                        string colId = s.Replace(" ", "_").Replace(".", "").Replace("&", "n").ToUpperInvariant();

                        pivotColumns.AppendLine($", MAX(CASE WHEN subject = '{safeName}' THEN Score END) AS {colId}");
                        pivotColumns.AppendLine($", MAX(CASE WHEN subject = '{safeName}' THEN grade END) AS {colId}_GRADE");
                        pivotColumns.AppendLine($", MAX(CASE WHEN subject = '{safeName}' THEN remark END) AS {colId}_REMARK");
                        pivotColumns.AppendLine($", MAX(CASE WHEN subject = '{safeName}' THEN RANK END) AS {colId}_POS");

                        selectColumns.AppendLine($", COALESCE({colId}, 0) AS [{s}]");
                        selectColumns.AppendLine($", COALESCE({colId}_GRADE, '') AS [{s} GRADE]");
                        selectColumns.AppendLine($", COALESCE({colId}_REMARK, '') AS [{s} REMARK]");
                        selectColumns.AppendLine($", COALESCE({colId}_POS, 0) AS [{s} POS]");
                    }

                    var query = $@"
WITH RankedExams AS (
    SELECT
        std_id,
        std_name AS NAME,
        std_class AS CLASS,
        term AS TERMS,
        year AS YEAR,
        subject,
        CAST(ROUND(CASE
            WHEN gt IS NULL THEN 0
            WHEN gt < 0 THEN 0
            WHEN gt > 100 THEN 100
            ELSE gt
        END, 2) AS decimal(10,2)) AS Score,
        grade,
        remark,
        ROW_NUMBER() OVER (PARTITION BY std_class, term, year, subject ORDER BY
            CASE
                WHEN gt IS NULL THEN 0
                WHEN gt < 0 THEN 0
                WHEN gt > 100 THEN 100
                ELSE gt
            END DESC) AS RANK
    FROM examss
    WHERE 1=1
    {(tenant ? "AND SchoolId = @SchoolId" : "")}
),
TotalScores AS (
    SELECT
        std_id,
        NAME,
        CLASS,
        TERMS,
        YEAR
        {pivotColumns}
        , CAST(ROUND(AVG(Score), 2) AS decimal(10,2)) AS TOTAL_SCORE
    FROM RankedExams
    GROUP BY std_id, NAME, CLASS, TERMS, YEAR
)
SELECT
    std_id AS StudentID,
    UPPER(NAME) AS NAME,
    UPPER(CLASS) AS CLASS,
    UPPER(TERMS) AS TERMS,
    UPPER(YEAR) AS YEAR
    {selectColumns}
    , TOTAL_SCORE,
    RANK() OVER (PARTITION BY CLASS, TERMS, YEAR ORDER BY TOTAL_SCORE DESC) AS TOTAL_RANK
FROM TotalScores";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error generating dynamic results report table", ex);
                throw new DataException("Error generating results report table", ex);
            }
            return table;
        }

        public async Task<DataTable> GetStudentResultsAsync(string studentId, string term, string year)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {EXAMS_TABLE} WHERE std_id = ? AND term = ? AND year = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        command.AddPositionalParameter(term);
                        command.AddPositionalParameter(year);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving student results for ID: {studentId}, Term: {term}", ex);
            }
            return table;
        }

        public async Task<DataTable> GetClassSubjectResultsAsync(string classId, string subject, string term, string year)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {EXAMS_TABLE} WHERE std_class = ? AND subject = ? AND term = ? AND year = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(classId);
                        command.AddPositionalParameter(subject);
                        command.AddPositionalParameter(term);
                        command.AddPositionalParameter(year);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        using (var adapter = new SqlDataAdapter(command)) await Task.Run(() => adapter.Fill(table));
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving class subject results for {classId}/{subject}/{term}/{year}", ex);
            }
            return table;
        }

        private void AddResultParameters(SqlCommand command, KingdomPrep.Shared.Models.ExamResult result, bool includeLookupValues = true)
        {
            command.AddPositionalParameter(result.StudentName ?? "");
            command.AddPositionalParameter(result.ClassId ?? "");
            command.AddPositionalParameter(result.Category1);
            command.AddPositionalParameter(result.Category2);
            command.AddPositionalParameter(result.Category3);
            command.AddPositionalParameter(result.CategoryTotal);
            command.AddPositionalParameter(result.ExamScore);
            command.AddPositionalParameter(result.TotalScore);
            command.AddPositionalParameter(result.Grade ?? "");
            command.AddPositionalParameter(result.Remark ?? "");
            if (includeLookupValues)
            {
                AddResultLookupParameters(command, result);
            }
        }

        private void AddResultLookupParameters(SqlCommand command, KingdomPrep.Shared.Models.ExamResult result)
        {
            command.AddPositionalParameter(result.StudentId ?? "");
            command.AddPositionalParameter(result.Subject ?? "");
            command.AddPositionalParameter(result.Term ?? "");
            command.AddPositionalParameter(result.Year ?? "");
        }

        private async Task<bool> HasColumnAsync(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT CASE WHEN COL_LENGTH(@table, @column) IS NULL THEN 0 ELSE 1 END", connection))
            {
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@column", columnName);
                return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
            }
        }

        private async Task<ResultAssessmentContext> GetAssessmentContextAsync(SqlConnection connection, string term, string year)
        {
            var context = new ResultAssessmentContext();
            if (!await HasColumnAsync(connection, "ExamSetups", "ExamTypeId")) return context;

            var hasAssessmentNumber = await HasColumnAsync(connection, "ExamSetups", "AssessmentNumber");
            var hasAssessmentLabel = await HasColumnAsync(connection, "ExamSetups", "AssessmentLabel");
            var selectAssessmentNumber = hasAssessmentNumber ? "AssessmentNumber" : "CAST(NULL AS INT) AS AssessmentNumber";
            var selectAssessmentLabel = hasAssessmentLabel ? "AssessmentLabel" : "CAST(NULL AS NVARCHAR(100)) AS AssessmentLabel";

            using (var command = new SqlCommand($@"
                SELECT TOP 1 ExamTypeId, {selectAssessmentNumber}, {selectAssessmentLabel}
                FROM ExamSetups
                WHERE Term = ? AND [Year] = ?
                ORDER BY
                    CASE WHEN StartDate <= CONVERT(date, GETDATE()) AND EndDate >= CONVERT(date, GETDATE()) THEN 0 ELSE 1 END,
                    SetupID DESC", connection))
            {
                command.AddPositionalParameter(term ?? "");
                command.AddPositionalParameter(year ?? "");
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        context.ExamTypeId = reader["ExamTypeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ExamTypeId"]);
                        context.AssessmentNumber = reader["AssessmentNumber"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["AssessmentNumber"]);
                        context.AssessmentLabel = reader["AssessmentLabel"] == DBNull.Value ? null : reader["AssessmentLabel"].ToString();
                    }
                }
            }

            return context;
        }

        private void RefreshExamSummary(KingdomPrep.Shared.Models.ExamResult result)
        {
            int year;
            if (!int.TryParse(result?.Year, out year))
            {
                year = DateTime.Today.Year;
            }
            DashboardSummaryRepository.RefreshYearBestEffort(_connectionString, year);
        }

        private async Task TryRecordResultSyncAsync(KingdomPrep.Shared.Models.ExamResult result, string operation)
        {
            try
            {
                var syncId = await FindResultSyncIdAsync(result);
                if (syncId == Guid.Empty) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertBySyncIdAsync(EXAMS_TABLE, syncId, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Exam result sync capture skipped: " + ex.Message);
            }
        }

        private async Task<Guid> FindResultSyncIdAsync(KingdomPrep.Shared.Models.ExamResult result)
        {
            if (result == null) return Guid.Empty;
            var schoolId = TenantContext.RequireSchoolId();
            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(connection, schoolId);
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                var assessment = await GetAssessmentContextAsync(connection, result.Term, result.Year);
                var hasExamType = await HasColumnAsync(connection, EXAMS_TABLE, "ExamTypeId");
                var hasAssessmentNumber = await HasColumnAsync(connection, EXAMS_TABLE, "AssessmentNumber");
                var query = $"SELECT TOP 1 SyncId FROM {EXAMS_TABLE} WHERE std_id = ? AND subject = ? AND term = ? AND year = ?";
                if (hasExamType)
                {
                    query += assessment.ExamTypeId.HasValue ? " AND ExamTypeId = ?" : " AND ExamTypeId IS NULL";
                }
                if (hasAssessmentNumber)
                {
                    query += assessment.AssessmentNumber.HasValue ? " AND AssessmentNumber = ?" : " AND AssessmentNumber IS NULL";
                }
                if (tenant) query += TenantContext.FilterClauseSql();
                query += " ORDER BY UpdatedAt DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.AddPositionalParameter(result.StudentId ?? "");
                    command.AddPositionalParameter(result.Subject ?? "");
                    command.AddPositionalParameter(result.Term ?? "");
                    command.AddPositionalParameter(result.Year ?? "");
                    if (hasExamType && assessment.ExamTypeId.HasValue)
                    {
                        command.AddPositionalParameter(assessment.ExamTypeId.Value);
                    }
                    if (hasAssessmentNumber && assessment.AssessmentNumber.HasValue)
                    {
                        command.AddPositionalParameter(assessment.AssessmentNumber.Value);
                    }
                    if (tenant) TenantContext.AddSchoolParameter(command);
                    var value = await command.ExecuteScalarAsync();
                    if (value is Guid id) return id;
                    return Guid.TryParse(Convert.ToString(value), out var parsed) ? parsed : Guid.Empty;
                }
            }
        }
    }
}
