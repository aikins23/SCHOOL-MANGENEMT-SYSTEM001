using System;
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
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", subject);
                        command.Parameters.AddWithValue("?", term);
                        command.Parameters.AddWithValue("?", year);
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch { return false; }
        }

        public async Task<bool> AddResultAsync(Models.ExamResult result)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        INSERT INTO {EXAMS_TABLE} 
                        (std_name, std_class, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark, std_id, subject, term, year) 
                        VALUES 
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddResultParameters(command, result);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddResultParameters(command, result);
                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    // Original complex CTE query for ranked reports
                    var query = @"
WITH RankedExams AS (
    SELECT
        std_name AS NAME,
        std_class AS CLASS,
        term AS TERMS,
        year AS YEAR,
        subject,
        tl_cat,
        exam_score,
        gt,
        grade,
        remark,
        ROW_NUMBER() OVER (PARTITION BY std_class, term, year, subject ORDER BY gt DESC) AS RANK
    FROM examss
),
TotalScores AS (
    SELECT
        NAME,
        CLASS,
        TERMS,
        YEAR,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN tl_cat END) AS ENG_CAT,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN exam_score END) AS ENG_EXAM,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN gt END) AS ENG,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN grade END) AS ENG_GRADE,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN remark END) AS ENG_REMARK,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN RANK END) AS ENG_POS,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN tl_cat END) AS SCI_CAT,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN exam_score END) AS SCI_EXAM,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN gt END) AS SCI,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN grade END) AS SCI_GRADE,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN remark END) AS SCI_REMARK,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN RANK END) AS SCI_POS,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN tl_cat END) AS MATHS_CAT,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN exam_score END) AS MATHS_EXAM,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN gt END) AS MATHS,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN grade END) AS MATHS_GRADE,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN remark END) AS MATHS_REMARK,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN RANK END) AS MATHS_POS,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN tl_cat END) AS SOCIAL_CAT,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN exam_score END) AS SOCIAL_EXAM,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN gt END) AS SOCIAL,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN grade END) AS SOCIAL_GRADE,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN remark END) AS SOCIAL_REMARK,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN RANK END) AS SOCIAL_POS,
        MAX(CASE WHEN subject = 'COMPUTING' THEN tl_cat END) AS COMP_CAT,
        MAX(CASE WHEN subject = 'COMPUTING' THEN exam_score END) AS COMP_EXAM,
        MAX(CASE WHEN subject = 'COMPUTING' THEN gt END) AS COMP,
        MAX(CASE WHEN subject = 'COMPUTING' THEN grade END) AS COMP_GRADE,
        MAX(CASE WHEN subject = 'COMPUTING' THEN remark END) AS COMP_REMARK,
        MAX(CASE WHEN subject = 'COMPUTING' THEN RANK END) AS COMP_POS,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN tl_cat END) AS CAREER_CAT,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN exam_score END) AS CAREER_EXAM,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN gt END) AS CAREER,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN grade END) AS CAREER_GRADE,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN remark END) AS CAREER_REMARK,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN RANK END) AS CAREER_POS,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN tl_cat END) AS CRE_ART_CAT,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN exam_score END) AS CRE_ART_EXAM,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN gt END) AS CRE_ART,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN grade END) AS CRE_ART_GRADE,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN remark END) AS CRE_ART_REMARK,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN RANK END) AS CRE_ART_POS,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN tl_cat END) AS GHA_LANG_CAT,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN exam_score END) AS GHA_LANG_EXAM,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN gt END) AS GHA_LANG,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN grade END) AS GHA_LANG_GRADE,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN remark END) AS GHA_LANG_REMARK,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN RANK END) AS GHA_LANG_POS,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN tl_cat END) AS RME_CAT,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN exam_score END) AS RME_EXAM,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN gt END) AS RME,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN grade END) AS RME_GRADE,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN remark END) AS RME_REMARK,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN RANK END) AS RME_POS,
        SUM(gt) AS TOTAL_SCORE,
        SUM(tl_cat) AS TOTAL_CAT,
        SUM(exam_score) AS TOTAL_EXAM
    FROM RankedExams
    GROUP BY NAME, CLASS, TERMS, YEAR
)
SELECT
    UPPER(NAME) AS NAME,
    UPPER(CLASS) AS CLASS,
    UPPER(TERMS) AS TERMS,
    UPPER(YEAR) AS YEAR,
    COALESCE(ENG, 0) AS ENG,
    COALESCE(ENG_GRADE, '') AS ENG_GRADE,
    COALESCE(ENG_REMARK, '') AS ENG_REMARK,
    COALESCE(ENG_POS, 0) AS ENG_POS,
    COALESCE(SCI, 0) AS SCI,
    COALESCE(SCI_GRADE, '') AS SCI_GRADE,
    COALESCE(SCI_REMARK, '') AS SCI_REMARK,
    COALESCE(SCI_CAT, 0) AS SCI_CAT,
    COALESCE(SCI_EXAM, 0) AS SCI_EXAM,
    COALESCE(SCI_POS, 0) AS SCI_POS,
    COALESCE(MATHS_CAT, 0) AS MATHS_CAT,
    COALESCE(MATHS_EXAM, 0) AS MATHS_EXAM,
    COALESCE(MATHS, 0) AS MATHS,
    COALESCE(MATHS_GRADE, '') AS MATHS_GRADE,
    COALESCE(MATHS_REMARK, '') AS MATHS_REMARK,
    COALESCE(MATHS_POS, 0) AS MATHS_POS,
    COALESCE(SOCIAL_CAT, 0) AS SOCIAL_CAT,
    COALESCE(SOCIAL_EXAM, 0) AS SOCIAL_EXAM,
    COALESCE(SOCIAL, 0) AS SOCIAL,
    COALESCE(SOCIAL_GRADE, '') AS SOCIAL_GRADE,
    COALESCE(SOCIAL_REMARK, '') AS SOCIAL_REMARK,
    COALESCE(SOCIAL_POS, 0) AS SOCIAL_POS,
    COALESCE(COMP_CAT, 0) AS COMP_CAT,
    COALESCE(COMP_EXAM, 0) AS COMP_EXAM,
    COALESCE(COMP, 0) AS COMP,
    COALESCE(COMP_GRADE, '') AS COMP_GRADE,
    COALESCE(COMP_REMARK, '') AS COMP_REMARK,
    COALESCE(COMP_POS, 0) AS COMP_POS,
    COALESCE(CAREER_CAT, 0) AS CAREER_CAT,
    COALESCE(CAREER_EXAM, 0) AS CAREER_EXAM,
    COALESCE(CAREER, 0) AS CAREER,
    COALESCE(CAREER_GRADE, '') AS CAREER_GRADE,
    COALESCE(CAREER_REMARK, '') AS CAREER_REMARK,
    COALESCE(CAREER_POS, 0) AS CAREER_POS,
    COALESCE(CRE_ART_CAT, 0) AS CRE_ART_CAT,
    COALESCE(CRE_ART_EXAM, 0) AS CRE_ART_EXAM,
    COALESCE(CRE_ART, 0) AS CRE_ART,
    COALESCE(CRE_ART_GRADE, '') AS CRE_ART_GRADE,
    COALESCE(CRE_ART_REMARK, '') AS CRE_ART_REMARK,
    COALESCE(CRE_ART_POS, 0) AS CRE_ART_POS,
    COALESCE(GHA_LANG_CAT, 0) AS GHA_LANG_CAT,
    COALESCE(GHA_LANG_EXAM, 0) AS GHA_LANG_EXAM,
    COALESCE(GHA_LANG, 0) AS GHA_LANG,
    COALESCE(GHA_LANG_GRADE, '') AS GHA_LANG_GRADE,
    COALESCE(GHA_LANG_REMARK, '') AS GHA_LANG_REMARK,
    COALESCE(GHA_LANG_POS, 0) AS GHA_LANG_POS,
    COALESCE(RME_CAT, 0) AS RME_CAT,
    COALESCE(RME_EXAM, 0) AS RME_EXAM,
    COALESCE(RME, 0) AS RME,
    COALESCE(RME_GRADE, '') AS RME_GRADE,
    COALESCE(RME_REMARK, '') AS RME_REMARK,
    COALESCE(RME_POS, 0) AS RME_POS,
    TOTAL_SCORE,
    COALESCE(TOTAL_CAT, 0) AS TOTAL_CAT,
    COALESCE(TOTAL_EXAM, 0) AS TOTAL_EXAM,
    RANK() OVER (PARTITION BY CLASS, TERMS, YEAR ORDER BY TOTAL_SCORE DESC) AS TOTAL_RANK
FROM TotalScores";
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
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
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", term);
                        command.Parameters.AddWithValue("?", year);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch { }
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
