using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using System.Linq;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// SQL Server implementation of Student Repository - updated for the legacy schema.
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

        public async Task<KingdomPrep.Shared.Models.Student> GetByIdAsync(string studentId)
        {
            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE StudentID = @p0";
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter( studentId);
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

        public async Task<IEnumerable<KingdomPrep.Shared.Models.Student>> GetAllAsync()
        {
            var students = new List<KingdomPrep.Shared.Models.Student>();

            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE 1=1";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    query += " ORDER BY StudentID";

                    using (var command = new SqlCommand(query, connection))
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

        public async Task<IEnumerable<KingdomPrep.Shared.Models.Student>> GetByClassAsync(string classId)
        {
            var students = new List<KingdomPrep.Shared.Models.Student>();

            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT * FROM {STUDENTS_TABLE} WHERE ClassID = @p0 ORDER BY FirstName, LastName";
                    if (tenant)
                    {
                        query = $"SELECT * FROM {STUDENTS_TABLE} WHERE ClassID = @p0{TenantContext.FilterClauseSql()} ORDER BY FirstName, LastName";
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter( classId);
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

        public async Task<bool> AddAsync(KingdomPrep.Shared.Models.Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);

                    var query = $@"
                        DECLARE @InsertedStudents TABLE (StudentID int);

                        INSERT INTO {STUDENTS_TABLE}
                        (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown,
                         Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location,
                         admission_date, Std_pic)
                        OUTPUT INSERTED.StudentID INTO @InsertedStudents
                        VALUES
                        (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14);

                        SELECT TOP 1 StudentID FROM @InsertedStudents;";
                    if (tenant)
                    {
                        query = $@"
                        DECLARE @InsertedStudents TABLE (StudentID int);

                        INSERT INTO {STUDENTS_TABLE}
                        (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown,
                         Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location,
                         admission_date, Std_pic, SchoolId)
                        OUTPUT INSERTED.StudentID INTO @InsertedStudents
                        VALUES
                        (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @SchoolId);

                        SELECT TOP 1 StudentID FROM @InsertedStudents;";
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        if (tenant) TenantContext.AddSchoolParameter(command);

                        var id = await command.ExecuteScalarAsync();
                        if (id != null && id != DBNull.Value)
                        {
                            student.StudentID = id.ToString();
                            await TryRecordSyncUpsertAsync(student.StudentID, "Insert");
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

        public async Task<bool> UpdateAsync(KingdomPrep.Shared.Models.Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);

                    var query = $@"
                        UPDATE {STUDENTS_TABLE}
                        SET FirstName = @p0, LastName = @p1, DOB = @p2,
                            Gender = @p3, Email = @p4, ClassID = @p5, HomeTown = @p6,
                            Residence = @p7, Allegies = @p8, EmergencyConatct = @p9,
                            GuidanceName = @p10, GuidianceEmail = @p11,
                            Guidiance_Location = @p12, admission_date = @p13,
                            Std_pic = @p14
                        WHERE StudentID = @p15";
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddStudentParameters(command, student);
                        command.AddPositionalParameter( student.StudentID);
                        if (tenant) TenantContext.AddSchoolParameter(command);

                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0) await TryRecordSyncUpsertAsync(student.StudentID, "Update");
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
                await TryRecordSyncDeleteAsync(studentId);
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = @p0";
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter( studentId);
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    var query = $"SELECT COUNT(*) FROM {STUDENTS_TABLE} WHERE StudentID = @p0";
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter( studentId);
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);

                    var query = $"SELECT ISNULL(MAX(StudentID), 0) + 1 FROM {STUDENTS_TABLE} WHERE 1=1";
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);

                        var nextId = await command.ExecuteScalarAsync();
                        if (nextId != null && nextId != DBNull.Value)
                        {
                            return nextId.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error generating next student ID", ex);
                throw new DataException("Error generating next student ID", ex);
            }

            throw new DataException("Error generating next student ID");
        }

        public async Task<string> GetNextStudentIdAsync()
        {
            return await GenerateNextStudentIdAsync();
        }

        public async Task<DataTable> GetAsTableAsync(string filterId = null, string filterClass = null)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    // Note: SQL Server OLE DB optimized query
                    var includePhotoColumn = !string.IsNullOrWhiteSpace(filterId);
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
                            admission_date AS [ADMISSION DATE]
                            {(includePhotoColumn ? ", Std_pic AS [STUDENT PIC]" : "")}
                        FROM {STUDENTS_TABLE}
                        WHERE 1=1";

                    if (tenant) query += TenantContext.FilterClauseSql();
                    if (!string.IsNullOrWhiteSpace(filterId)) query += " AND StudentID = @FilterId";
                    if (!string.IsNullOrWhiteSpace(filterClass)) query += " AND ClassID = @FilterClass";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        if (!string.IsNullOrWhiteSpace(filterId)) command.Parameters.AddWithValue("@FilterId", filterId);
                        if (!string.IsNullOrWhiteSpace(filterClass)) command.Parameters.AddWithValue("@FilterClass", filterClass);

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
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

        public async Task<(DataTable Items, int TotalCount)> GetPageAsTableAsync(int page, int pageSize, string filterId = null, string filterClass = null, string search = null)
        {
            var table = new DataTable();
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 500));

            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);

                    string where = " WHERE 1=1";
                    if (tenant) where += TenantContext.FilterClauseSql();
                    if (!string.IsNullOrWhiteSpace(filterId)) where += " AND StudentID = @FilterId";
                    if (!string.IsNullOrWhiteSpace(filterClass)) where += " AND ClassID = @FilterClass";
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        where += @" AND (
                            CONVERT(NVARCHAR(50), StudentID) LIKE @Search
                            OR FirstName LIKE @Search
                            OR LastName LIKE @Search
                            OR (FirstName + ' ' + LastName) LIKE @Search)";
                    }

                    int total;
                    using (var count = new SqlCommand($"SELECT COUNT(*) FROM {STUDENTS_TABLE}{where}", connection))
                    {
                        AddStudentPageParameters(count, tenant, filterId, filterClass, search, includePaging: false, page: page, pageSize: pageSize);
                        total = Convert.ToInt32(await count.ExecuteScalarAsync());
                    }

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
                            admission_date AS [ADMISSION DATE]
                        FROM {STUDENTS_TABLE}
                        {where}
                        ORDER BY ClassID, FirstName, LastName, StudentID
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddStudentPageParameters(command, tenant, filterId, filterClass, search, includePaging: true, page: page, pageSize: pageSize);
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }

                    return (table, total);
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving paged student table", ex);
            }
        }

        private static void AddStudentPageParameters(SqlCommand command, bool tenant, string filterId, string filterClass, string search, bool includePaging, int page, int pageSize)
        {
            if (tenant) TenantContext.AddSchoolParameter(command);
            if (!string.IsNullOrWhiteSpace(filterId)) command.Parameters.AddWithValue("@FilterId", filterId);
            if (!string.IsNullOrWhiteSpace(filterClass)) command.Parameters.AddWithValue("@FilterClass", filterClass);
            if (!string.IsNullOrWhiteSpace(search)) command.Parameters.AddWithValue("@Search", "%" + search.Trim() + "%");
            if (includePaging)
            {
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);
            }
        }

        public async Task<bool> UpdateStudentClassBatchAsync(IEnumerable<string> studentIds, string newClassId)
        {
            if (studentIds == null || !studentIds.Any()) return true;

            try
            {
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, STUDENTS_TABLE);
                    using (var transaction = connection.BeginTransaction())
                    {
                        var query = $"UPDATE {STUDENTS_TABLE} SET ClassID = @p0 WHERE StudentID = @p1";
                        if (tenant) query += TenantContext.FilterClauseSql();
                        using (var command = new SqlCommand(query, connection, transaction))
                        {
                            foreach (var id in studentIds)
                            {
                                command.Parameters.Clear();
                                command.AddPositionalParameter( newClassId);
                                command.AddPositionalParameter( id);
                                if (tenant) TenantContext.AddSchoolParameter(command);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                        await TryRecordSyncUpsertsAsync(studentIds);
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
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
                            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16)";
                        if (archiveTenant)
                        {
                            insertQuery = @"
                            INSERT INTO Rolled_Out_Students
                            (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, [date], Std_pic, SchoolId)
                            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @SchoolId)";
                        }

                        using (var insCmd = new SqlCommand(insertQuery, connection, transaction))
                        {
                            insCmd.AddPositionalParameter( student.StudentID);
                            insCmd.AddPositionalParameter( student.FirstName ?? "");
                            insCmd.AddPositionalParameter( student.LastName ?? "");
                            insCmd.AddPositionalParameter( student.DateOfBirth);
                            insCmd.AddPositionalParameter( student.Gender ?? "");
                            insCmd.AddPositionalParameter( student.Email ?? "");
                            insCmd.AddPositionalParameter( student.ClassID ?? "");
                            insCmd.AddPositionalParameter( student.HomeTown ?? "");
                            insCmd.AddPositionalParameter( student.Residence ?? "");
                            insCmd.AddPositionalParameter( student.Allergies ?? "");
                            insCmd.AddPositionalParameter( student.EmergencyContact ?? "");
                            insCmd.AddPositionalParameter( student.GuardianName ?? "");
                            insCmd.AddPositionalParameter( student.GuardianEmail ?? "");
                            insCmd.AddPositionalParameter( student.GuardianLocation ?? "");
                            insCmd.AddPositionalParameter( student.AdmissionDate);
                            insCmd.AddPositionalParameter( DateTime.Today);
                            insCmd.AddPositionalParameter( student.ProfilePhoto ?? new byte[0]);
                            if (archiveTenant) TenantContext.AddSchoolParameter(insCmd);
                            await insCmd.ExecuteNonQueryAsync();
                        }

                        await TryRecordSyncDeleteAsync(studentId);

                        var deleteQuery = $"DELETE FROM {STUDENTS_TABLE} WHERE StudentID = @p0";
                        if (studentTenant) deleteQuery += TenantContext.FilterClauseSql();
                        using (var delCmd = new SqlCommand(deleteQuery, connection, transaction))
                        {
                            delCmd.AddPositionalParameter( studentId);
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
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
                                            SELECT * FROM Rolled_Out_Students WHERE StudentID = @p0";
                            if (archiveTenant) moveSql += TenantContext.FilterClauseSql();
                            using (var moveCmd = new SqlCommand(moveSql, connection, transaction))
                            {
                                moveCmd.AddPositionalParameter( studentId);
                                if (archiveTenant) TenantContext.AddSchoolParameter(moveCmd);
                                int rows = await moveCmd.ExecuteNonQueryAsync();
                                if (rows == 0) return false;
                            }

                            // 2. Delete from archive
                            var delSql = "DELETE FROM Rolled_Out_Students WHERE StudentID = @p0";
                            if (archiveTenant) delSql += TenantContext.FilterClauseSql();
                            using (var delCmd = new SqlCommand(delSql, connection, transaction))
                            {
                                delCmd.AddPositionalParameter( studentId);
                                if (archiveTenant) TenantContext.AddSchoolParameter(delCmd);
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            if (studentTenant)
                            {
                                using (var scopeCmd = new SqlCommand($"UPDATE {STUDENTS_TABLE} SET SchoolId = @SchoolId WHERE StudentID = @p0", connection, transaction))
                                {
                                    TenantContext.AddSchoolParameter(scopeCmd);
                                    scopeCmd.AddPositionalParameter( studentId);
                                    await scopeCmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            await TryRecordSyncUpsertAsync(studentId, "Insert");
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
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
                        query = query.Replace("ORDER BY", TenantContext.FilterClauseSql() + " ORDER BY");
                    }
                    using (var command = new SqlCommand(query, connection))
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        await Task.Run(() => adapter.Fill(table));
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
                using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
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
                        query = query.Replace("ORDER BY", TenantContext.FilterClauseSql() + " ORDER BY");
                    }
                    using (var command = new SqlCommand(query, connection))
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        await Task.Run(() => adapter.Fill(table));
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving graduated students table", ex);
            }
            return table;
        }

        private KingdomPrep.Shared.Models.Student MapReaderToStudent(IDataReader reader)
        {
            return new KingdomPrep.Shared.Models.Student
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

        private void AddStudentParameters(SqlCommand command, KingdomPrep.Shared.Models.Student student)
        {
            // Parameters MUST be added in the same order as they appear in the query
            command.AddPositionalParameter( student.FirstName ?? "");
            command.AddPositionalParameter( student.LastName ?? "");
            command.AddPositionalParameter( student.DateOfBirth);
            command.AddPositionalParameter( student.Gender ?? "");
            command.AddPositionalParameter( student.Email ?? "");
            command.AddPositionalParameter( student.ClassID ?? "");
            command.AddPositionalParameter( student.HomeTown ?? "");
            command.AddPositionalParameter( student.Residence ?? "");
            command.AddPositionalParameter( student.Allergies ?? "");
            command.AddPositionalParameter( student.EmergencyContact ?? "");
            command.AddPositionalParameter( student.GuardianName ?? "");
            command.AddPositionalParameter( student.GuardianEmail ?? "");
            command.AddPositionalParameter( student.GuardianLocation ?? "");
            command.AddPositionalParameter( student.AdmissionDate);
            command.AddPositionalParameter( student.ProfilePhoto ?? new byte[0]);
        }

        private async Task TryRecordSyncUpsertAsync(string studentId, string operation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId)) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(STUDENTS_TABLE, "StudentID", studentId, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Student sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncUpsertsAsync(IEnumerable<string> studentIds)
        {
            try
            {
                if (studentIds == null) return;
                var ids = studentIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Cast<object>().ToArray();
                if (ids.Length == 0) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertsAsync(STUDENTS_TABLE, "StudentID", ids);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Student batch sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(string studentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId)) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(STUDENTS_TABLE, "StudentID", studentId);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Student delete sync capture skipped: " + ex.Message);
            }
        }
    }
}
