using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for retrieving and aggregating all data needed for report card generation.
    /// Updated to handle DBNull safely and ensure correct data types for OLE DB parameters.
    /// </summary>
    public class ReportCardDataService
    {
        private readonly string _connectionString;
        private readonly IStudentTermRemarksRepository _remarksRepository;

        public ReportCardDataService(string connectionString, IStudentTermRemarksRepository remarksRepository)
        {
            _connectionString = connectionString;
            _remarksRepository = remarksRepository;
        }

        public async Task<ReportCardData> GetStudentReportCardDataAsync(string studentId, string term, string year)
        {
            try
            {
                // 1. Get student info
                var student = await GetStudentAsync(studentId);
                if (student == null)
                    throw new InvalidOperationException($"Student with ID {studentId} was not found in the 'Students' table.");

                // 2. Get all subject results
                var examResults = await GetExamResultsAsync(studentId, term, year);

                // 3. Calculate subject-level rankings
                var subjectRankings = await CalculateSubjectRankingsAsync(
                    student.ClassID, examResults, term, year);

                // 4. Calculate overall ranking
                var overallRanking = await CalculateOverallRankingAsync(
                    student.ClassID, studentId, term, year);

                var academicSessions = new AcademicSessionService();
                await TryEnsureAcademicSessionSchemaAsync();
                var reportTerm = await TryFindReportTermAsync(academicSessions, term, year);

                // 5. Get attendance summary for the selected term period when term dates exist.
                var attendanceSummary = await GetAttendanceSummaryAsync(studentId, term, year, reportTerm);

                // 6. Get teacher remarks
                var remarks = await _remarksRepository.GetAsync(studentId, term, year)
                    ?? new StudentTermRemarks { StudentID = studentId, Term = term, Year = year };

                var billing = await TryGetBillingBreakdownAsync(academicSessions, studentId, reportTerm);

                return new ReportCardData
                {
                    StudentID = student.StudentID,
                    StudentName = student.FullName,
                    ClassID = student.ClassID,
                    Gender = student.Gender,
                    ProfilePhoto = student.ProfilePhoto,
                    AdmissionDate = student.AdmissionDate,
                    Term = term,
                    Year = year,
                    TermStartDate = reportTerm?.StartDate,
                    TermClosingDate = reportTerm?.EndDate,
                    TermReopeningDate = reportTerm?.ReopeningDate,
                    PresentDays = attendanceSummary.PresentDays,
                    TotalSchoolDays = attendanceSummary.TotalDays,
                    SubjectResults = subjectRankings,
                    OverallPosition = overallRanking.Position,
                    TotalStudentsInClass = overallRanking.TotalStudents,
                    Remarks = remarks,
                    Billing = billing,
                    SchoolInfo = new SchoolInfo
                    {
                        Name = Common.SchoolProfile.Name,
                        Location = Common.SchoolProfile.Address,
                        PhoneNumbers = Common.SchoolProfile.Phones,
                        Logo = Common.SchoolProfile.Logo
                    }
                };
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Report card data aggregation failed for student {studentId}", ex);
                throw; // Rethrow to let manager handle it
            }
        }

        private async Task<Student> GetStudentAsync(string studentId)
        {
            using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM Students WHERE CAST(StudentID AS NVARCHAR(50)) IN (?, ?)";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, connection))
                {
                    var ids = BuildStudentIdCandidates(studentId);
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, ids[0]);
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, ids[1]);

                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return new Student
                            {
                                StudentID = reader["StudentID"].ToString(),
                                FirstName = reader["FirstName"]?.ToString() ?? "",
                                LastName = reader["LastName"]?.ToString() ?? "",
                                ClassID = reader["ClassID"]?.ToString() ?? "",
                                Gender = reader["Gender"]?.ToString() ?? "",
                                ProfilePhoto = reader["Std_pic"] as byte[],
                                AdmissionDate = reader["admission_date"] != DBNull.Value
                                    ? (DateTime)reader["admission_date"]
                                    : DateTime.Today
                            };
                        }
                    }
                }
            }
            return null;
        }

        private async Task<List<ExamResult>> GetExamResultsAsync(string studentId, string term, string year)
        {
            var results = new List<ExamResult>();

            using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM examss WHERE CAST(std_id AS NVARCHAR(50)) IN (?, ?) AND term = ? AND [year] = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "examss");
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, connection))
                {
                    var ids = BuildStudentIdCandidates(studentId);
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, ids[0]);
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, ids[1]);

                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, term ?? "");
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, year ?? "");
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ExamResult
                            {
                                StudentId = reader["std_id"].ToString(),
                                Subject = reader["subject"]?.ToString() ?? "",
                                Term = reader["term"]?.ToString() ?? "",
                                Year = reader["year"]?.ToString() ?? "",
                                Category1 = SafeDecimal(reader["cat1"]),
                                Category2 = SafeDecimal(reader["cat2"]),
                                Category3 = SafeDecimal(reader["cat3"]),
                                ExamScore = ClampScore(SafeDecimal(reader["exam_score"]), 0m, 50m),
                                TotalScore = ClampScore(SafeDecimal(reader["gt"]), 0m, 100m),
                                Grade = reader["grade"]?.ToString() ?? "",
                                Remark = reader["remark"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return results;
        }

        private async Task<List<SubjectResult>> CalculateSubjectRankingsAsync(
            string classId, List<ExamResult> studentResults, string term, string year)
        {
            var results = new List<SubjectResult>();

            foreach (var exam in studentResults)
            {
                var allClassResults = await GetSubjectClassResultsAsync(
                    exam.Subject, classId, term, year);

                var totalScore = RoundScore(ClampScore(exam.TotalScore, 0m, 100m));
                var position = allClassResults.Count(x => x.TotalScore > totalScore) + 1;

                results.Add(new SubjectResult
                {
                    Subject = exam.Subject,
                    ClassScore = RoundScore(ClampScore(exam.CategoryTotal, 0m, 50m)),
                    ExamScore = RoundScore(ClampScore(exam.ExamScore, 0m, 50m)),
                    TotalScore = totalScore,
                    Grade = Common.GradingScheme.CodeForScore(totalScore),
                    Remark = Common.GradingScheme.LabelForScore(totalScore),
                    PositionInClass = position
                });
            }

            return results;
        }

        private async Task<List<ExamResult>> GetSubjectClassResultsAsync(
            string subject, string classId, string term, string year)
        {
            var results = new List<ExamResult>();

            using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                var query = "SELECT gt FROM examss WHERE subject = ? AND std_class = ? AND term = ? AND [year] = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "examss");
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, connection))
                {
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, subject ?? "");
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, classId ?? "");
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, term ?? "");
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, year ?? "");
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ExamResult
                            {
                                TotalScore = RoundScore(ClampScore(SafeDecimal(reader["gt"]), 0m, 100m))
                            });
                        }
                    }
                }
            }

            return results;
        }

        private async Task<(int Position, int TotalStudents)> CalculateOverallRankingAsync(
            string classId, string studentId, string term, string year)
        {
            var allClassAggregates = new List<(string StudentId, decimal AggregateScore)>();

            using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();
                var query = @"SELECT std_id,
    AVG(CASE
        WHEN gt IS NULL THEN 0
        WHEN gt < 0 THEN 0
        WHEN gt > 100 THEN 100
        ELSE gt
    END) as AggregateScore
FROM examss WHERE std_class = ? AND term = ? AND [year] = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "examss");
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                query += " GROUP BY std_id";

                using (var cmd = new SqlCommand(query, connection))
                {
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, classId ?? "");
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, term ?? "");
                    kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, year ?? "");
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            allClassAggregates.Add((
                                reader["std_id"].ToString(),
                                RoundScore(ClampScore(SafeDecimal(reader["AggregateScore"]), 0m, 100m))
                            ));
                        }
                    }
                }
            }

            if (allClassAggregates.Count == 0) return (0, 0);

            var studentAggregate = allClassAggregates.FirstOrDefault(x => StudentIdMatches(x.StudentId, studentId));
            // If student has no scores at all for the term, they won't be in the aggregate list
            if (studentAggregate.StudentId == null) return (allClassAggregates.Count + 1, allClassAggregates.Count + 1);

            var position = allClassAggregates.Count(x => x.AggregateScore > studentAggregate.AggregateScore) + 1;
            return (position, allClassAggregates.Count);
        }

        private async Task<(int PresentDays, int TotalDays)> GetAttendanceSummaryAsync(
            string studentId, string term, string year, AcademicTerm reportTerm)
        {
            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    int yearNum = DateTime.Today.Year;
                    if (!string.IsNullOrEmpty(year) && int.TryParse(year.Split('/')[0], out var y)) yearNum = y;

                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Attendance");
                    var hasTermRange = reportTerm != null;
                    var presentQuery = "SELECT COUNT(*) FROM Attendance WHERE CAST(ReferenceID AS NVARCHAR(50)) IN (?, ?) AND ReferenceType = 'STUDENT' AND [Status] = 'PRESENT'";
                    presentQuery += hasTermRange ? " AND CAST([Date] AS date) BETWEEN ? AND ?" : " AND YEAR([Date]) = ?";
                    if (tenant)
                    {
                        presentQuery += TenantContext.FilterClauseSql();
                    }

                    int presentDays = 0;
                    using (var cmd = new SqlCommand(presentQuery, connection))
                    {
                        var ids = BuildStudentIdCandidates(studentId);
                        kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, ids[0]);
                        kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, ids[1]);

                        if (hasTermRange)
                        {
                            kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, reportTerm.StartDate.Date);
                            kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, reportTerm.EndDate.Date);
                        }
                        else
                        {
                            kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, yearNum);
                        }
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(cmd);
                        }

                        presentDays = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    var totalQuery = "SELECT COUNT(DISTINCT [Date]) FROM Attendance WHERE 1=1";
                    totalQuery += hasTermRange ? " AND CAST([Date] AS date) BETWEEN ? AND ?" : " AND YEAR([Date]) = ?";
                    if (tenant)
                    {
                        totalQuery += TenantContext.FilterClauseSql();
                    }

                    int totalDays = 0;
                    using (var cmd = new SqlCommand(totalQuery, connection))
                    {
                        if (hasTermRange)
                        {
                            kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, reportTerm.StartDate.Date);
                            kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, reportTerm.EndDate.Date);
                        }
                        else
                        {
                            kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.AddPositionalParameter(cmd, yearNum);
                        }
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(cmd);
                        }

                        totalDays = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    return (presentDays, totalDays == 0 ? 1 : totalDays);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Attendance summary failed for student {studentId}", ex);
                return (0, 1); // Return safe default
            }
        }

        private decimal SafeDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }

        private async Task TryEnsureAcademicSessionSchemaAsync()
        {
            try
            {
                await AcademicSessionSchema.EnsureAsync(_connectionString);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Academic session schema check failed during report card generation; continuing without optional session backfill.", ex);
            }
        }

        private async Task<AcademicTerm> TryFindReportTermAsync(AcademicSessionService academicSessions, string term, string year)
        {
            try
            {
                return await academicSessions.FindTermAsync(term, year)
                    ?? await academicSessions.FindTermAsync(term);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Could not load academic term metadata for report card; dates will be left blank.", ex);
                return null;
            }
        }

        private async Task<StudentBillingBreakdown> TryGetBillingBreakdownAsync(
            AcademicSessionService academicSessions, string studentId, AcademicTerm reportTerm)
        {
            try
            {
                return reportTerm == null
                    ? await academicSessions.GetStudentBillingBreakdownAsync(studentId)
                    : await academicSessions.GetStudentBillingBreakdownAsync(studentId, reportTerm.TermID);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Could not load billing breakdown for report card student {studentId}; continuing without billing sheet.", ex);
                return null;
            }
        }

        private static decimal ClampScore(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static decimal RoundScore(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string[] BuildStudentIdCandidates(string studentId)
        {
            var raw = (studentId ?? "").Trim();
            var numeric = StudentId.Parse(raw);
            var display = StudentId.Display(numeric.Length == 0 ? raw : numeric);
            if (string.Equals(numeric, display, StringComparison.OrdinalIgnoreCase))
            {
                display = raw;
            }
            if (string.IsNullOrWhiteSpace(display))
            {
                display = numeric;
            }
            return new[] { numeric, display };
        }

        private static bool StudentIdMatches(string left, string right)
        {
            var l = BuildStudentIdCandidates(left);
            var r = BuildStudentIdCandidates(right);
            return l.Any(x => r.Any(y => string.Equals(x, y, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
