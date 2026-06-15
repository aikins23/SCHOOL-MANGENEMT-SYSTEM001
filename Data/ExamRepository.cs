using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class ExamRepository : IExamRepository
    {
        private readonly string _connectionString;
        private const string EXAMS_TABLE = "examss";

        public ExamRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<bool> ResultExistsAsync(string studentId, string subject, string term, string year)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT COUNT(*) FROM {EXAMS_TABLE} WHERE std_id = ? AND subject = ? AND term = ? AND year = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", subject);
                        command.Parameters.AddWithValue("?", term);
                        command.Parameters.AddWithValue("?", year);
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

        public async Task<bool> AddResultAsync(Models.ExamResult result)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    var query = $@"
                        INSERT INTO {EXAMS_TABLE} 
                        (std_name, std_class, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark, std_id, subject, term, year{(tenant ? ", SchoolId" : "")}) 
                        VALUES 
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?{(tenant ? ", ?" : "")})";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddResultParameters(command, result);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var rows = await command.ExecuteNonQueryAsync();
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

        public async Task<bool> UpdateResultAsync(Models.ExamResult result)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        UPDATE {EXAMS_TABLE} 
                        SET std_name = ?, std_class = ?, cat1 = ?, cat2 = ?, cat3 = ?, tl_cat = ?, 
                            exam_score = ?, gt = ?, grade = ?, remark = ? 
                        WHERE std_id = ? AND subject = ? AND term = ? AND year = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddResultParameters(command, result);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var rows = await command.ExecuteNonQueryAsync();
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Get all unique subjects recorded in the exams table
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    var subjects = new List<string>();
                    var subjectSql = "SELECT DISTINCT subject FROM examss WHERE subject IS NOT NULL";
                    if (tenant)
                    {
                        subjectSql += TenantContext.FilterClause();
                    }

                    using (var subCmd = new OleDbCommand(subjectSql, connection))
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
                        
                        pivotColumns.AppendLine($", MAX(CASE WHEN subject = '{safeName}' THEN gt END) AS {colId}");
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
        gt,
        grade,
        remark,
        ROW_NUMBER() OVER (PARTITION BY std_class, term, year, subject ORDER BY gt DESC) AS RANK
    FROM examss
    WHERE 1=1
    {(tenant ? "AND SchoolId = ?" : "")}
),
TotalScores AS (
    SELECT
        std_id,
        NAME,
        CLASS,
        TERMS,
        YEAR
        {pivotColumns}
        , SUM(gt) AS TOTAL_SCORE
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

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {EXAMS_TABLE} WHERE std_id = ? AND term = ? AND year = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, EXAMS_TABLE);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", term);
                        command.Parameters.AddWithValue("?", year);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
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

        private void AddResultParameters(OleDbCommand command, Models.ExamResult result)
        {
            command.Parameters.AddWithValue("?", result.StudentName ?? "");
            command.Parameters.AddWithValue("?", result.ClassId ?? "");
            command.Parameters.AddWithValue("?", result.Category1);
            command.Parameters.AddWithValue("?", result.Category2);
            command.Parameters.AddWithValue("?", result.Category3);
            command.Parameters.AddWithValue("?", result.CategoryTotal);
            command.Parameters.AddWithValue("?", result.ExamScore);
            command.Parameters.AddWithValue("?", result.TotalScore);
            command.Parameters.AddWithValue("?", result.Grade ?? "");
            command.Parameters.AddWithValue("?", result.Remark ?? "");
            command.Parameters.AddWithValue("?", result.StudentId ?? "");
            command.Parameters.AddWithValue("?", result.Subject ?? "");
            command.Parameters.AddWithValue("?", result.Term ?? "");
            command.Parameters.AddWithValue("?", result.Year ?? "");
        }
    }
}
