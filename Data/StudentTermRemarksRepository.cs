using System;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// OleDb implementation of StudentTermRemarks repository
    /// </summary>
    public class StudentTermRemarksRepository : IStudentTermRemarksRepository
    {
        private readonly string _connectionString;

        public StudentTermRemarksRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StudentTermRemarks> GetAsync(string studentId, string term, string year)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                        SELECT * FROM StudentTermRemarks
                        WHERE StudentID = @StudentID AND Term = @Term AND Year = @Year";

                    using (var cmd = new OleDbCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@StudentID", studentId);
                        cmd.Parameters.AddWithValue("@Term", term);
                        cmd.Parameters.AddWithValue("@Year", year);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return new StudentTermRemarks
                                {
                                    ID = (int)reader["ID"],
                                    StudentID = reader["StudentID"].ToString(),
                                    Term = reader["Term"].ToString(),
                                    Year = reader["Year"].ToString(),
                                    ClassTeacherRemarks = reader["ClassTeacherRemarks"].ToString(),
                                    HeadTeacherRemarks = reader["HeadTeacherRemarks"].ToString(),
                                    Attitude = reader["Attitude"].ToString(),
                                    Interest = reader["Interest"].ToString(),
                                    Conduct = reader["Conduct"].ToString(),
                                    CreatedDate = (DateTime)reader["CreatedDate"],
                                    ModifiedDate = reader["ModifiedDate"] != DBNull.Value ? (DateTime?)reader["ModifiedDate"] : null
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to retrieve student term remarks for student {studentId}, term {term}, year {year}", ex);
                throw new RepositoryException("Error retrieving student term remarks", ex);
            }
        }

        public async Task<bool> AddAsync(StudentTermRemarks remarks)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                        INSERT INTO StudentTermRemarks
                        (StudentID, Term, Year, ClassTeacherRemarks, HeadTeacherRemarks, Attitude, Interest, Conduct, CreatedDate)
                        VALUES (@StudentID, @Term, @Year, @ClassTeacherRemarks, @HeadTeacherRemarks, @Attitude, @Interest, @Conduct, @CreatedDate)";

                    using (var cmd = new OleDbCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@StudentID", remarks.StudentID);
                        cmd.Parameters.AddWithValue("@Term", remarks.Term);
                        cmd.Parameters.AddWithValue("@Year", remarks.Year);
                        cmd.Parameters.AddWithValue("@ClassTeacherRemarks", remarks.ClassTeacherRemarks ?? "");
                        cmd.Parameters.AddWithValue("@HeadTeacherRemarks", remarks.HeadTeacherRemarks ?? "");
                        cmd.Parameters.AddWithValue("@Attitude", remarks.Attitude ?? "");
                        cmd.Parameters.AddWithValue("@Interest", remarks.Interest ?? "");
                        cmd.Parameters.AddWithValue("@Conduct", remarks.Conduct ?? "");
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        return await cmd.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to add student term remarks for student {remarks?.StudentID ?? "unknown"}", ex);
                throw new RepositoryException("Error adding student term remarks", ex);
            }
        }

        public async Task<bool> UpdateAsync(StudentTermRemarks remarks)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                        UPDATE StudentTermRemarks
                        SET ClassTeacherRemarks = @ClassTeacherRemarks,
                            HeadTeacherRemarks = @HeadTeacherRemarks,
                            Attitude = @Attitude,
                            Interest = @Interest,
                            Conduct = @Conduct,
                            ModifiedDate = @ModifiedDate
                        WHERE StudentID = @StudentID AND Term = @Term AND Year = @Year";

                    using (var cmd = new OleDbCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ClassTeacherRemarks", remarks.ClassTeacherRemarks ?? "");
                        cmd.Parameters.AddWithValue("@HeadTeacherRemarks", remarks.HeadTeacherRemarks ?? "");
                        cmd.Parameters.AddWithValue("@Attitude", remarks.Attitude ?? "");
                        cmd.Parameters.AddWithValue("@Interest", remarks.Interest ?? "");
                        cmd.Parameters.AddWithValue("@Conduct", remarks.Conduct ?? "");
                        cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@StudentID", remarks.StudentID);
                        cmd.Parameters.AddWithValue("@Term", remarks.Term);
                        cmd.Parameters.AddWithValue("@Year", remarks.Year);

                        return await cmd.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to update student term remarks for student {remarks?.StudentID ?? "unknown"}", ex);
                throw new RepositoryException("Error updating student term remarks", ex);
            }
        }

        public async Task<bool> DeleteAsync(string studentId, string term, string year)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = "DELETE FROM StudentTermRemarks WHERE StudentID = @StudentID AND Term = @Term AND Year = @Year";

                    using (var cmd = new OleDbCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@StudentID", studentId);
                        cmd.Parameters.AddWithValue("@Term", term);
                        cmd.Parameters.AddWithValue("@Year", year);

                        return await cmd.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Failed to delete student term remarks for student {studentId}, term {term}, year {year}", ex);
                throw new RepositoryException("Error deleting student term remarks", ex);
            }
        }
    }

    public class RepositoryException : Exception
    {
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}
