using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// OleDb implementation of Student Repository
    /// </summary>
    public class StudentRepository : IStudentRepository
    {
        private readonly string _connectionString;
        private const string STUDENTS_TABLE = "students";

        public StudentRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<Models.Student> GetByIdAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE StudentID = @StudentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentID", studentId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return MapReaderToStudent(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error retrieving student with ID {studentId}", ex);
            }

            return null;
        }

        public async Task<IEnumerable<Models.Student>> GetAllAsync()
        {
            var students = new List<Models.Student>();

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {STUDENTS_TABLE} ORDER BY StudentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                students.Add(MapReaderToStudent(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving all students", ex);
            }

            return students;
        }

        public async Task<IEnumerable<Models.Student>> GetByClassAsync(string classId)
        {
            var students = new List<Models.Student>();

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE ClassID = @ClassID ORDER BY FirstName, LastName";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClassID", classId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                students.Add(MapReaderToStudent(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error retrieving students for class {classId}", ex);
            }

            return students;
        }

        public async Task<bool> AddAsync(Models.Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"
                        INSERT INTO {STUDENTS_TABLE} 
                        (StudentID, FirstName, LastName, DateOfBirth, Gender, ClassID, Email, HomeTown, 
                         Residence, Allergies, ProfilePhoto, GuardianName, GuardianEmail, GuardianLocation, 
                         EmergencyContact, AdmissionDate, CreatedDate) 
                        VALUES 
                        (@StudentID, @FirstName, @LastName, @DateOfBirth, @Gender, @ClassID, @Email, @HomeTown, 
                         @Residence, @Allergies, @ProfilePhoto, @GuardianName, @GuardianEmail, @GuardianLocation, 
                         @EmergencyContact, @AdmissionDate, @CreatedDate)";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error adding student", ex);
            }
        }

        public async Task<bool> UpdateAsync(Models.Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"
                        UPDATE {STUDENTS_TABLE} 
                        SET FirstName = @FirstName, LastName = @LastName, DateOfBirth = @DateOfBirth, 
                            Gender = @Gender, ClassID = @ClassID, Email = @Email, HomeTown = @HomeTown, 
                            Residence = @Residence, Allergies = @Allergies, ProfilePhoto = @ProfilePhoto, 
                            GuardianName = @GuardianName, GuardianEmail = @GuardianEmail, 
                            GuardianLocation = @GuardianLocation, EmergencyContact = @EmergencyContact, 
                            AdmissionDate = @AdmissionDate, ModifiedDate = @ModifiedDate 
                        WHERE StudentID = @StudentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        command.Parameters.AddWithValue("@StudentID", student.StudentID);
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error updating student", ex);
            }
        }

        public async Task<bool> DeleteAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = @StudentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentID", studentId);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error deleting student with ID {studentId}", ex);
            }
        }

        public async Task<bool> ExistsAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT COUNT(*) FROM {STUDENTS_TABLE} WHERE StudentID = @StudentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentID", studentId);
                        var count = (int)await command.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error checking if student exists", ex);
            }
        }

        public async Task<string> GenerateNextStudentIdAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT MAX(StudentID) FROM {STUDENTS_TABLE}";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            return "STD001";
                        }

                        var lastId = result.ToString();
                        if (int.TryParse(lastId.Substring(3), out int number))
                        {
                            return $"STD{(number + 1):D3}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error generating next student ID", ex);
            }

            return "STD001";
        }

        private Models.Student MapReaderToStudent(IDataReader reader)
        {
            return new Models.Student
            {
                StudentID = reader["StudentID"].ToString(),
                FirstName = reader["FirstName"].ToString(),
                LastName = reader["LastName"].ToString(),
                DateOfBirth = (DateTime)reader["DateOfBirth"],
                Gender = reader["Gender"].ToString(),
                ClassID = reader["ClassID"].ToString(),
                Email = reader["Email"].ToString(),
                HomeTown = reader["HomeTown"].ToString(),
                Residence = reader["Residence"].ToString(),
                Allergies = reader["Allergies"].ToString(),
                ProfilePhoto = reader["ProfilePhoto"] != DBNull.Value ? (byte[])reader["ProfilePhoto"] : null,
                GuardianName = reader["GuardianName"].ToString(),
                GuardianEmail = reader["GuardianEmail"].ToString(),
                GuardianLocation = reader["GuardianLocation"].ToString(),
                EmergencyContact = reader["EmergencyContact"].ToString(),
                AdmissionDate = (DateTime)reader["AdmissionDate"],
                CreatedDate = (DateTime)reader["CreatedDate"],
                ModifiedDate = reader["ModifiedDate"] != DBNull.Value ? (DateTime?)reader["ModifiedDate"] : null
            };
        }

        private void AddStudentParameters(OleDbCommand command, Models.Student student)
        {
            command.Parameters.AddWithValue("@FirstName", student.FirstName ?? "");
            command.Parameters.AddWithValue("@LastName", student.LastName ?? "");
            command.Parameters.AddWithValue("@DateOfBirth", student.DateOfBirth);
            command.Parameters.AddWithValue("@Gender", student.Gender ?? "");
            command.Parameters.AddWithValue("@ClassID", student.ClassID ?? "");
            command.Parameters.AddWithValue("@Email", student.Email ?? "");
            command.Parameters.AddWithValue("@HomeTown", student.HomeTown ?? "");
            command.Parameters.AddWithValue("@Residence", student.Residence ?? "");
            command.Parameters.AddWithValue("@Allergies", student.Allergies ?? "");
            command.Parameters.AddWithValue("@ProfilePhoto", student.ProfilePhoto ?? new byte[0]);
            command.Parameters.AddWithValue("@GuardianName", student.GuardianName ?? "");
            command.Parameters.AddWithValue("@GuardianEmail", student.GuardianEmail ?? "");
            command.Parameters.AddWithValue("@GuardianLocation", student.GuardianLocation ?? "");
            command.Parameters.AddWithValue("@EmergencyContact", student.EmergencyContact ?? "");
            command.Parameters.AddWithValue("@AdmissionDate", student.AdmissionDate);
        }
    }
}
