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
    /// Matches database typos: Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE StudentID = ?";
                    if (tenant) query += TenantContext.FilterClause();

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE 1=1";
                    if (tenant) query += TenantContext.FilterClause();
                    query += " ORDER BY StudentID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE ClassID = ? ORDER BY FirstName, LastName";
                    if (tenant)
                    {
                        query = $"SELECT * FROM {STUDENTS_TABLE} WHERE ClassID = ?{TenantContext.FilterClause()} ORDER BY FirstName, LastName";
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", classId);
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);

                    var query = $@"
                        INSERT INTO {STUDENTS_TABLE} 
                        (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, 
                         Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, 
                         admission_date, Std_pic) 
                        VALUES 
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    if (tenant)
                    {
                        query = $@"
                        INSERT INTO {STUDENTS_TABLE} 
                        (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, 
                         Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, 
                         admission_date, Std_pic, SchoolId) 
                        VALUES 
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        if (tenant) TenantContext.AddSchoolParameter(command);

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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);

                    var query = $@"
                        UPDATE {STUDENTS_TABLE} 
                        SET FirstName = ?, LastName = ?, DOB = ?, 
                            Gender = ?, ClassID = ?, Email = ?, HomeTown = ?, 
                            Residence = ?, Allegies = ?, EmergencyConatct = ?, 
                            GuidanceName = ?, GuidianceEmail = ?, 
                            Guidiance_Location = ?, admission_date = ?, 
                            Std_pic = ? 
                        WHERE StudentID = ?";
                    if (tenant) query += TenantContext.FilterClause();

                    using (var command = new OleDbCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        command.Parameters.AddWithValue("?", student.StudentID);
                        if (tenant) TenantContext.AddSchoolParameter(command);

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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = ?";
                    if (tenant) query += TenantContext.FilterClause();

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT COUNT(*) FROM {STUDENTS_TABLE} WHERE StudentID = ?";
                    if (tenant) query += TenantContext.FilterClause();

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT ISNULL(MAX(StudentID), 0) + 1 FROM {STUDENTS_TABLE}";
                    if (tenant) query += " WHERE SchoolId = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    // Note: SQL Server OLE DB optimized query
                    var query = $@"
                        SELECT 
                            StudentID AS [ID],
                            StudentID AS [STUDENT ID],
                            FirstName AS [FIRST NAME],
                            LastName AS [LAST NAME],
                            DOB AS [DATE OF BIRTH],
                            Gender AS [GENDER],
                            Email AS [EMAIL],
                            ClassID AS [CLASS ID],
                            HomeTown AS [HOME TOWN],
                            Residence AS [RESIDENCE],
                            Allegies AS [ALLERGIES],
                            EmergencyConatct AS [EMERGENCY CONTACT],
                            GuidanceName AS [GUARDIAN NAME],
                            GuidianceEmail AS [GUARDIAN EMAIL],
                            Guidiance_Location AS [GUARDIAN LOCATION],
                            admission_date AS [ADMISSION DATE],
                            Std_pic AS [STUDENT PIC]
                        FROM {STUDENTS_TABLE}
                        WHERE 1=1";

                    if (tenant) query += TenantContext.FilterClause();
                    if (!string.IsNullOrWhiteSpace(filterId)) query += " AND StudentID = ?";
                    if (!string.IsNullOrWhiteSpace(filterClass)) query += " AND ClassID = ?";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    using (var transaction = connection.BeginTransaction())
                    {
                        var query = $"UPDATE {STUDENTS_TABLE} SET ClassID = ? WHERE StudentID = ?";
                        if (tenant) query += TenantContext.FilterClause();
                        using (var command = new OleDbCommand(query, connection, transaction))
                        {
                            foreach (var id in studentIds)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("?", newClassId);
                                command.Parameters.AddWithValue("?", id);
                                if (tenant) TenantContext.AddSchoolParameter(command);
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
                    bool studentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    bool archiveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Students");
                    using (var transaction = connection.BeginTransaction())
                    {
                        var student = await GetByIdAsync(studentId);
                        if (student == null) return false;

                        var insertQuery = @"
                            INSERT INTO Rolled_Out_Students
                            (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, [date], Std_pic)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        if (archiveTenant)
                        {
                            insertQuery = @"
                            INSERT INTO Rolled_Out_Students
                            (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, [date], Std_pic, SchoolId)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        }
                        
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
                            if (archiveTenant) TenantContext.AddSchoolParameter(insCmd);
                            await insCmd.ExecuteNonQueryAsync();
                        }

                        var deleteQuery = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = ?";
                        if (studentTenant) deleteQuery += TenantContext.FilterClause();
                        using (var delCmd = new OleDbCommand(deleteQuery, connection, transaction))
                        {
                            delCmd.Parameters.AddWithValue("?", studentId);
                            if (studentTenant) TenantContext.AddSchoolParameter(delCmd);
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

        public async Task<bool> RestoreAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool studentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    bool archiveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Students");
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Move back to main table
                            var moveSql = @"INSERT INTO Students 
                                            SELECT * FROM Rolled_Out_Students WHERE StudentID = ?";
                            if (archiveTenant) moveSql += TenantContext.FilterClause();
                            using (var moveCmd = new OleDbCommand(moveSql, connection, transaction))
                            {
                                moveCmd.Parameters.AddWithValue("?", studentId);
                                if (archiveTenant) TenantContext.AddSchoolParameter(moveCmd);
                                int rows = await moveCmd.ExecuteNonQueryAsync();
                                if (rows == 0) return false;
                            }

                            // 2. Delete from archive
                            var delSql = "DELETE FROM Rolled_Out_Students WHERE StudentID = ?";
                            if (archiveTenant) delSql += TenantContext.FilterClause();
                            using (var delCmd = new OleDbCommand(delSql, connection, transaction))
                            {
                                delCmd.Parameters.AddWithValue("?", studentId);
                                if (archiveTenant) TenantContext.AddSchoolParameter(delCmd);
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            if (studentTenant)
                            {
                                using (var scopeCmd = new OleDbCommand($"UPDATE {STUDENTS_TABLE} SET SchoolId = ? WHERE StudentID = ?", connection, transaction))
                                {
                                    TenantContext.AddSchoolParameter(scopeCmd);
                                    scopeCmd.Parameters.AddWithValue("?", studentId);
                                    await scopeCmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error restoring student {studentId}", ex);
                return false;
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
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Students");
                    var query = @"SELECT StudentID AS ID, FirstName + ' ' + LastName AS [FULL NAME], 
                                         Gender AS GENDER, ClassID AS [CLASS], admission_date AS [ADMISSION DATE] 
                                  FROM Rolled_Out_Students
                                  WHERE 1=1
                                  ORDER BY [date] DESC";
                    if (tenant)
                    {
                        query = query.Replace("ORDER BY", TenantContext.FilterClause() + " ORDER BY");
                    }
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving rolled out students table", ex);
            }
            return table;
        }

        public async Task<DataTable> GetGraduatedAsTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = @"SELECT StudentID AS ID, FirstName + ' ' + LastName AS [FULL NAME], 
                                         Gender AS GENDER, admission_date AS [ADMISSION DATE] 
                                  FROM Students 
                                  WHERE UPPER(ClassID) = 'GRADUATED'
                                  ORDER BY StudentID";
                    if (tenant)
                    {
                        query = query.Replace("ORDER BY", TenantContext.FilterClause() + " ORDER BY");
                    }
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving graduated students table", ex);
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
                Allergies = reader["Allegies"].ToString(),
                ProfilePhoto = reader["Std_pic"] != DBNull.Value ? (byte[])reader["Std_pic"] : null,
                GuardianName = reader["GuidanceName"].ToString(),
                GuardianEmail = reader["GuidianceEmail"].ToString(),
                GuardianLocation = reader["Guidiance_Location"].ToString(),
                EmergencyContact = reader["EmergencyConatct"].ToString(),
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
