using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for retrieving and aggregating all data needed for report card generation
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

        /// <summary>
        /// Retrieves complete report card data for a single student
        /// </summary>
        public async Task<ReportCardData> GetStudentReportCardDataAsync(string studentId, string term, string year)
        {
            try
            {
                // 1. Get student info
                var student = await GetStudentAsync(studentId);
                if (student == null)
                    throw new InvalidOperationException($"Student {studentId} not found");

                // 2. Get all subject results for this student in this term/year
                var examResults = await GetExamResultsAsync(studentId, term, year);

                // 3. Calculate subject-level rankings (rank per subject)
                var subjectRankings = await CalculateSubjectRankingsAsync(
                    student.ClassID, examResults, term, year);

                // 4. Calculate overall ranking (rank by sum of all subjects)
                var overallRanking = await CalculateOverallRankingAsync(
                    student.ClassID, studentId, term, year);

                // 5. Get attendance summary
                var attendanceSummary = await GetAttendanceSummaryAsync(studentId, term, year);

                // 6. Get teacher remarks
                var remarks = await _remarksRepository.GetAsync(studentId, term, year)
                    ?? new StudentTermRemarks { StudentID = studentId, Term = term, Year = year };

                // 7. Aggregate into ReportCardData DTO
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
                throw new ServiceException($"Error retrieving report card data for student {studentId}", ex);
            }
        }

        private async Task<Student> GetStudentAsync(string studentId)
        {
            using (var connection = new System.Data.OleDb.OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT * FROM Student WHERE StudentID = @StudentID";

                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StudentID", studentId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return new Student
                            {
                                StudentID = reader["StudentID"].ToString(),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                ClassID = reader["ClassID"].ToString(),
                                Gender = reader["Gender"].ToString(),
                                ProfilePhoto = reader["ProfilePhoto"] as byte[],
                                AdmissionDate = DateTime.Parse(reader["AdmissionDate"].ToString())
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

            using (var connection = new System.Data.OleDb.OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = @"
                    SELECT * FROM ExamResult
                    WHERE StudentId = @StudentId AND Term = @Term AND Year = @Year";

                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@Term", term);
                    cmd.Parameters.AddWithValue("@Year", year);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ExamResult
                            {
                                StudentId = reader["StudentId"].ToString(),
                                Subject = reader["Subject"].ToString(),
                                Term = reader["Term"].ToString(),
                                Year = reader["Year"].ToString(),
                                Category1 = decimal.Parse(reader["Category1"].ToString() ?? "0"),
                                Category2 = decimal.Parse(reader["Category2"].ToString() ?? "0"),
                                Category3 = decimal.Parse(reader["Category3"].ToString() ?? "0"),
                                ExamScore = decimal.Parse(reader["ExamScore"].ToString() ?? "0"),
                                TotalScore = decimal.Parse(reader["TotalScore"].ToString() ?? "0"),
                                Grade = reader["Grade"].ToString(),
                                Remark = reader["Remark"].ToString()
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
                // Get all students' scores for this subject/class/term/year
                var allClassResults = await GetSubjectClassResultsAsync(
                    exam.Subject, classId, term, year);

                // Rank: count how many students scored higher (higher total = better rank)
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

            using (var connection = new System.Data.OleDb.OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = @"
                    SELECT er.* FROM ExamResult er
                    JOIN Student s ON er.StudentId = s.StudentID
                    WHERE er.Subject = @Subject AND s.ClassID = @ClassID
                    AND er.Term = @Term AND er.Year = @Year";

                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Subject", subject);
                    cmd.Parameters.AddWithValue("@ClassID", classId);
                    cmd.Parameters.AddWithValue("@Term", term);
                    cmd.Parameters.AddWithValue("@Year", year);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ExamResult
                            {
                                StudentId = reader["StudentId"].ToString(),
                                TotalScore = decimal.Parse(reader["TotalScore"].ToString() ?? "0")
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

            using (var connection = new System.Data.OleDb.OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string query = @"
                    SELECT er.StudentId, SUM(er.TotalScore) as AggregateScore
                    FROM ExamResult er
                    JOIN Student s ON er.StudentId = s.StudentID
                    WHERE s.ClassID = @ClassID AND er.Term = @Term AND er.Year = @Year
                    GROUP BY er.StudentId";

                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ClassID", classId);
                    cmd.Parameters.AddWithValue("@Term", term);
                    cmd.Parameters.AddWithValue("@Year", year);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            allClassAggregates.Add((
                                reader["StudentId"].ToString(),
                                decimal.Parse(reader["AggregateScore"].ToString() ?? "0")
                            ));
                        }
                    }
                }
            }

            var studentAggregate = allClassAggregates.FirstOrDefault(x => x.StudentId == studentId);
            var position = allClassAggregates.Count(x => x.AggregateScore > studentAggregate.AggregateScore) + 1;

            return (position, allClassAggregates.Count);
        }

        private async Task<(int PresentDays, int TotalDays)> GetAttendanceSummaryAsync(
            string studentId, string term, string year)
        {
            using (var connection = new System.Data.OleDb.OleDbConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Count present days
                const string presentQuery = @"
                    SELECT COUNT(*) FROM Attendance
                    WHERE StudentID = @StudentID AND Status = 'Present'
                    AND YEAR(AttendanceDate) = @Year AND Term = @Term";

                int presentDays = 0;
                using (var cmd = new System.Data.OleDb.OleDbCommand(presentQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@StudentID", studentId);
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Term", term);
                    presentDays = (int)await cmd.ExecuteScalarAsync();
                }

                // Count total school days
                const string totalQuery = @"
                    SELECT COUNT(DISTINCT AttendanceDate) FROM Attendance
                    WHERE YEAR(AttendanceDate) = @Year AND Term = @Term";

                int totalDays = 0;
                using (var cmd = new System.Data.OleDb.OleDbCommand(totalQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Term", term);
                    totalDays = (int)await cmd.ExecuteScalarAsync();
                }

                return (presentDays, totalDays == 0 ? 1 : totalDays);
            }
        }
    }

    public class ServiceException : Exception
    {
        public ServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
