using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// OleDb implementation of Student Repository - Updated for OLE DB compatibility and correct schema
    /// </summary>
    public class StudentRepository : IStudentRepository
    {
        private readonly string _connectionString;
        private const string STUDENTS_TABLE = "Students";

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
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE StudentID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
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
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE ClassID = ? ORDER BY FirstName, LastName";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", classId);
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
                        (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, 
                         Residence, Allergies, EmergencyContact, GuardianName, GuardianEmail, Guardian_Location, 
                         admission_date, Std_pic) 
                        VALUES 
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddStudentParameters(command, student);

                        var result = await command.ExecuteNonQueryAsync();
                        
                        // Get the newly generated StudentID
                        if (result > 0)
                        {
                            using (var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection))
                            {
                                var id = await idCommand.ExecuteScalarAsync();
                                if (id != null && id != DBNull.Value)
                                {
                                    student.StudentID = id.ToString();
                                }
                            }
                            return true;
                        }
                        return false;
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
                        SET FirstName = ?, LastName = ?, DOB = ?, 
                            Gender = ?, ClassID = ?, Email = ?, HomeTown = ?, 
                            Residence = ?, Allergies = ?, EmergencyContact = ?, 
                            GuardianName = ?, GuardianEmail = ?, 
                            Guardian_Location = ?, admission_date = ?, 
                            Std_pic = ? 
                        WHERE StudentID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        command.Parameters.AddWithValue("?", student.StudentID);

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
                    var query = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
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
                    var query = $"SELECT COUNT(*) FROM {STUDENTS_TABLE} WHERE StudentID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
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
                    var query = $"SELECT ISNULL(MAX(StudentID), 0) + 1 FROM {STUDENTS_TABLE}";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "1";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error generating next student ID", ex);
            }
        }

        public async Task<DataTable> GetAsTableAsync(string filterId = null, string filterClass = null)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Note: Standard OLE DB for Access/SQL might not support CONCAT or complex formatting easily
                    // This query is optimized for the project's SQL Server context through OLE DB
                    var query = $@"
                        SELECT 
                            StudentID AS [ID],
                            'KPS' + RIGHT('000' + CAST(StudentID AS VARCHAR), 3) AS [UNIQUE ID],
                            FirstName AS [FIRST NAME],
                            LastName AS [LAST NAME],
                            DOB AS [DATE OF BIRTH],
                            Gender AS [GENDER],
                            Email AS [EMAIL],
                            ClassID AS [CLASS ID],
                            HomeTown AS [HOME TOWN],
                            Residence AS [RESIDENCE],
                            Allergies AS [ALLERGIES],
                            EmergencyContact AS [EMERGENCY CONTACT],
                            GuardianName AS [GUARDIAN NAME],
                            GuardianEmail AS [GUARDIAN EMAIL],
                            Guardian_Location AS [GUARDIAN LOCATION],
                            admission_date AS [ADMISSION DATE],
                            Std_pic AS [STUDENT PIC]
                        FROM {STUDENTS_TABLE}
                        WHERE 1=1";

                    if (!string.IsNullOrWhiteSpace(filterId)) query += " AND StudentID = ?";
                    if (!string.IsNullOrWhiteSpace(filterClass)) query += " AND ClassID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(filterId)) command.Parameters.AddWithValue("?", filterId);
                        if (!string.IsNullOrWhiteSpace(filterClass)) command.Parameters.AddWithValue("?", filterClass);

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving student table", ex);
            }
            return table;
        }

        public async Task<bool> UpdateStudentClassBatchAsync(IEnumerable<string> studentIds, string newClassId)
        {
            if (studentIds == null || !studentIds.Any()) return true;

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var query = $"UPDATE {STUDENTS_TABLE} SET ClassID = ? WHERE StudentID = ?";
                        using (var command = new OleDbCommand(query, connection, transaction))
                        {
                            foreach (var id in studentIds)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("?", newClassId);
                                command.Parameters.AddWithValue("?", id);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error in bulk class update", ex);
            }
        }

        public async Task<bool> RollOutAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var student = await GetByIdAsync(studentId);
                        if (student == null) return false;

                        var insertQuery = @"
                            INSERT INTO Rolled_Out_Students
                            (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allergies, EmergencyContact, GuardianName, GuardianEmail, Guardian_Location, admission_date, [date], Std_pic)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        
                        using (var insCmd = new OleDbCommand(insertQuery, connection, transaction))
                        {
                            insCmd.Parameters.AddWithValue("?", student.StudentID);
                            insCmd.Parameters.AddWithValue("?", student.FirstName ?? "");
                            insCmd.Parameters.AddWithValue("?", student.LastName ?? "");
                            insCmd.Parameters.AddWithValue("?", student.DateOfBirth);
                            insCmd.Parameters.AddWithValue("?", student.Gender ?? "");
                            insCmd.Parameters.AddWithValue("?", student.Email ?? "");
                            insCmd.Parameters.AddWithValue("?", student.ClassID ?? "");
                            insCmd.Parameters.AddWithValue("?", student.HomeTown ?? "");
                            insCmd.Parameters.AddWithValue("?", student.Residence ?? "");
                            insCmd.Parameters.AddWithValue("?", student.Allergies ?? "");
                            insCmd.Parameters.AddWithValue("?", student.EmergencyContact ?? "");
                            insCmd.Parameters.AddWithValue("?", student.GuardianName ?? "");
                            insCmd.Parameters.AddWithValue("?", student.GuardianEmail ?? "");
                            insCmd.Parameters.AddWithValue("?", student.GuardianLocation ?? "");
                            insCmd.Parameters.AddWithValue("?", student.AdmissionDate);
                            insCmd.Parameters.AddWithValue("?", DateTime.Today);
                            insCmd.Parameters.AddWithValue("?", student.ProfilePhoto ?? new byte[0]);
                            await insCmd.ExecuteNonQueryAsync();
                        }

                        var deleteQuery = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = ?";
                        using (var delCmd = new OleDbCommand(deleteQuery, connection, transaction))
                        {
                            delCmd.Parameters.AddWithValue("?", studentId);
                            await delCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException($"Error rolling out student {studentId}", ex);
            }
        }

        public async Task<DataTable> GetRolledOutAsTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT 
                            StudentID AS [ID],
                            'KPS' + RIGHT('000' + CAST(StudentID AS VARCHAR), 3) AS [UNIQUE ID],
                            FirstName AS [FIRST NAME],
                            LastName AS [LAST NAME],
                            DOB AS [DATE OF BIRTH],
                            Gender AS [GENDER],
                            Email AS [EMAIL],
                            ClassID AS [CLASS ID],
                            HomeTown AS [HOME TOWN],
                            Residence AS [RESIDENCE],
                            Allergies AS [ALLERGIES],
                            EmergencyContact AS [EMERGENCY CONTACT],
                            GuardianName AS [GUIDANCE NAME],
                            GuardianEmail AS [GUIDANCE EMAIL],
                            Guardian_Location AS [GUIDANCE LOCATION],
                            admission_date AS [ADMISSION DATE],
                            Std_pic AS [STUDENT PIC]
                        FROM Rolled_Out_Students
                        ORDER BY [date] DESC";
                    
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving rolled out students", ex);
            }
            return table;
        }

        private Models.Student MapReaderToStudent(IDataReader reader)
        {
            return new Models.Student
            {
                StudentID = reader["StudentID"].ToString(),
                FirstName = reader["FirstName"].ToString(),
                LastName = reader["LastName"].ToString(),
                DateOfBirth = (DateTime)reader["DOB"],
                Gender = reader["Gender"].ToString(),
                ClassID = reader["ClassID"].ToString(),
                Email = reader["Email"].ToString(),
                HomeTown = reader["HomeTown"].ToString(),
                Residence = reader["Residence"].ToString(),
                Allergies = reader["Allergies"].ToString(),
                ProfilePhoto = reader["Std_pic"] != DBNull.Value ? (byte[])reader["Std_pic"] : null,
                GuardianName = reader["GuardianName"].ToString(),
                GuardianEmail = reader["GuardianEmail"].ToString(),
                GuardianLocation = reader["Guardian_Location"].ToString(),
                EmergencyContact = reader["EmergencyContact"].ToString(),
                AdmissionDate = (DateTime)reader["admission_date"]
            };
        }

        private void AddStudentParameters(OleDbCommand command, Models.Student student)
        {
            // Parameters MUST be added in the same order as they appear in the query
            command.Parameters.AddWithValue("?", student.FirstName ?? "");
            command.Parameters.AddWithValue("?", student.LastName ?? "");
            command.Parameters.AddWithValue("?", student.DateOfBirth);
            command.Parameters.AddWithValue("?", student.Gender ?? "");
            command.Parameters.AddWithValue("?", student.Email ?? "");
            command.Parameters.AddWithValue("?", student.ClassID ?? "");
            command.Parameters.AddWithValue("?", student.HomeTown ?? "");
            command.Parameters.AddWithValue("?", student.Residence ?? "");
            command.Parameters.AddWithValue("?", student.Allergies ?? "");
            command.Parameters.AddWithValue("?", student.EmergencyContact ?? "");
            command.Parameters.AddWithValue("?", student.GuardianName ?? "");
            command.Parameters.AddWithValue("?", student.GuardianEmail ?? "");
            command.Parameters.AddWithValue("?", student.GuardianLocation ?? "");
            command.Parameters.AddWithValue("?", student.AdmissionDate);
            command.Parameters.AddWithValue("?", student.ProfilePhoto ?? new byte[0]);
        }
    }
}
