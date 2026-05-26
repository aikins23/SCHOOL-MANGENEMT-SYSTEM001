using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

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

                // 5. Get attendance summary
                var attendanceSummary = await GetAttendanceSummaryAsync(studentId, term, year);

                // 6. Get teacher remarks
                var remarks = await _remarksRepository.GetAsync(studentId, term, year)
                    ?? new StudentTermRemarks { StudentID = studentId, Term = term, Year = year };

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
                    PresentDays = attendanceSummary.PresentDays,
                    TotalSchoolDays = attendanceSummary.TotalDays,
                    SubjectResults = subjectRankings,
                    OverallPosition = overallRanking.Position,
                    TotalStudentsInClass = overallRanking.TotalStudents,
                    Remarks = remarks,
                    SchoolInfo = new SchoolInfo()
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
            using (var connection = new OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT * FROM Students WHERE StudentID = ?";

                using (var cmd = new OleDbCommand(query, connection))
                {
                    // Ensure studentId is passed as the correct type (int) if the database expects it
                    if (int.TryParse(studentId, out int idInt))
                        cmd.Parameters.AddWithValue("?", idInt);
                    else
                        cmd.Parameters.AddWithValue("?", studentId);

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

            using (var connection = new OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT * FROM examss WHERE std_id = ? AND term = ? AND [year] = ?";

                using (var cmd = new OleDbCommand(query, connection))
                {
                    if (int.TryParse(studentId, out int idInt))
                        cmd.Parameters.AddWithValue("?", idInt);
                    else
                        cmd.Parameters.AddWithValue("?", studentId);

                    cmd.Parameters.AddWithValue("?", term ?? "");
                    cmd.Parameters.AddWithValue("?", year ?? "");

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
                                ExamScore = SafeDecimal(reader["exam_score"]),
                                TotalScore = SafeDecimal(reader["gt"]),
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

                var position = allClassResults.Count(x => x.TotalScore > exam.TotalScore) + 1;

                results.Add(new SubjectResult
                {
                    Subject = exam.Subject,
                    ClassScore = exam.Category1 + exam.Category2 + exam.Category3,
                    ExamScore = exam.ExamScore,
                    TotalScore = exam.TotalScore,
                    Grade = exam.Grade,
                    Remark = exam.Remark,
                    PositionInClass = position
                });
            }

            return results;
        }

        private async Task<List<ExamResult>> GetSubjectClassResultsAsync(
            string subject, string classId, string term, string year)
        {
            var results = new List<ExamResult>();

            using (var connection = new OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT gt FROM examss WHERE subject = ? AND std_class = ? AND term = ? AND [year] = ?";

                using (var cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("?", subject ?? "");
                    cmd.Parameters.AddWithValue("?", classId ?? "");
                    cmd.Parameters.AddWithValue("?", term ?? "");
                    cmd.Parameters.AddWithValue("?", year ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ExamResult
                            {
                                TotalScore = SafeDecimal(reader["gt"])
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

            using (var connection = new OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT std_id, SUM(gt) as AggregateScore FROM examss WHERE std_class = ? AND term = ? AND [year] = ? GROUP BY std_id";

                using (var cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("?", classId ?? "");
                    cmd.Parameters.AddWithValue("?", term ?? "");
                    cmd.Parameters.AddWithValue("?", year ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            allClassAggregates.Add((
                                reader["std_id"].ToString(),
                                SafeDecimal(reader["AggregateScore"])
                            ));
                        }
                    }
                }
            }

            if (allClassAggregates.Count == 0) return (0, 0);

            var studentAggregate = allClassAggregates.FirstOrDefault(x => x.StudentId == studentId);
            // If student has no scores at all for the term, they won't be in the aggregate list
            if (studentAggregate.StudentId == null) return (allClassAggregates.Count + 1, allClassAggregates.Count + 1);

            var position = allClassAggregates.Count(x => x.AggregateScore > studentAggregate.AggregateScore) + 1;
            return (position, allClassAggregates.Count);
        }

        private async Task<(int PresentDays, int TotalDays)> GetAttendanceSummaryAsync(
            string studentId, string term, string year)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    int yearNum = DateTime.Today.Year;
                    if (!string.IsNullOrEmpty(year) && int.TryParse(year.Split('/')[0], out var y)) yearNum = y;

                    const string presentQuery = "SELECT COUNT(*) FROM Attendance WHERE ReferenceID = ? AND ReferenceType = 'STUDENT' AND [Status] = 'PRESENT' AND YEAR([Date]) = ?";
                    int presentDays = 0;
                    using (var cmd = new OleDbCommand(presentQuery, connection))
                    {
                        if (int.TryParse(studentId, out int idInt))
                            cmd.Parameters.AddWithValue("?", idInt);
                        else
                            cmd.Parameters.AddWithValue("?", studentId);
                        
                        cmd.Parameters.AddWithValue("?", yearNum);
                        presentDays = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    const string totalQuery = "SELECT COUNT(DISTINCT [Date]) FROM Attendance WHERE YEAR([Date]) = ?";
                    int totalDays = 0;
                    using (var cmd = new OleDbCommand(totalQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("?", yearNum);
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
    }
}
