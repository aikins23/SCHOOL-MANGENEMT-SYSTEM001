using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// SQL Server implementation of StudentTermRemarks repository.
    /// Uses positional parameters (?) for SQL Server compatibility.
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT * FROM StudentTermRemarks
                        WHERE CAST(StudentID AS NVARCHAR(50)) IN (?, ?) AND Term = ? AND [Year] = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "StudentTermRemarks");
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        var ids = BuildStudentIdCandidates(studentId);
                        cmd.AddPositionalParameter(ids[0]);
                        cmd.AddPositionalParameter(ids[1]);
                        cmd.AddPositionalParameter(term);
                        cmd.AddPositionalParameter(year);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return new StudentTermRemarks
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    StudentID = reader["StudentID"].ToString(),
                                    Term = reader["Term"].ToString(),
                                    Year = reader["Year"].ToString(),
                                    ClassTeacherRemarks = reader["ClassTeacherRemarks"]?.ToString() ?? "",
                                    HeadTeacherRemarks = reader["HeadTeacherRemarks"]?.ToString() ?? "",
                                    Attitude = reader["Attitude"]?.ToString() ?? "",
                                    Interest = reader["Interest"]?.ToString() ?? "",
                                    Conduct = reader["Conduct"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime)reader["CreatedDate"] : DateTime.Now,
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    const string query = @"
                        INSERT INTO StudentTermRemarks
                        (StudentID, Term, [Year], ClassTeacherRemarks, HeadTeacherRemarks, Attitude, Interest, Conduct, CreatedDate)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.AddPositionalParameter(remarks.StudentID);
                        cmd.AddPositionalParameter(remarks.Term);
                        cmd.AddPositionalParameter(remarks.Year);
                        cmd.AddPositionalParameter(remarks.ClassTeacherRemarks ?? "");
                        cmd.AddPositionalParameter(remarks.HeadTeacherRemarks ?? "");
                        cmd.AddPositionalParameter(remarks.Attitude ?? "");
                        cmd.AddPositionalParameter(remarks.Interest ?? "");
                        cmd.AddPositionalParameter(remarks.Conduct ?? "");
                        cmd.AddPositionalParameter(DateTime.Now);

                        var saved = await cmd.ExecuteNonQueryAsync() > 0;
                        if (saved)
                        {
                            var id = await GetLastIdentityAsync(connection);
                            await TryRecordSyncUpsertAsync("ID", id, "Insert");
                        }
                        return saved;
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    var query = @"
                        UPDATE StudentTermRemarks
                        SET ClassTeacherRemarks = ?,
                            HeadTeacherRemarks = ?,
                            Attitude = ?,
                            Interest = ?,
                            Conduct = ?,
                            ModifiedDate = ?
                        WHERE CAST(StudentID AS NVARCHAR(50)) IN (?, ?) AND Term = ? AND [Year] = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "StudentTermRemarks");
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        // Set columns
                        cmd.AddPositionalParameter(remarks.ClassTeacherRemarks ?? "");
                        cmd.AddPositionalParameter(remarks.HeadTeacherRemarks ?? "");
                        cmd.AddPositionalParameter(remarks.Attitude ?? "");
                        cmd.AddPositionalParameter(remarks.Interest ?? "");
                        cmd.AddPositionalParameter(remarks.Conduct ?? "");
                        cmd.AddPositionalParameter(DateTime.Now);

                        // Where clause
                        var ids = BuildStudentIdCandidates(remarks.StudentID);
                        cmd.AddPositionalParameter(ids[0]);
                        cmd.AddPositionalParameter(ids[1]);
                        cmd.AddPositionalParameter(remarks.Term);
                        cmd.AddPositionalParameter(remarks.Year);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);

                        var saved = await cmd.ExecuteNonQueryAsync() > 0;
                        if (saved) await TryRecordSyncByCompositeAsync(remarks.StudentID, remarks.Term, remarks.Year, "Update");
                        return saved;
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    var syncId = await FindSyncIdAsync(connection, studentId, term, year);
                    if (syncId != Guid.Empty) await TryRecordDeleteBySyncIdAsync(syncId);

                    var query = "DELETE FROM StudentTermRemarks WHERE CAST(StudentID AS NVARCHAR(50)) IN (?, ?) AND Term = ? AND [Year] = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "StudentTermRemarks");
                    if (tenant) query += TenantContext.FilterClauseSql();

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        var ids = BuildStudentIdCandidates(studentId);
                        cmd.AddPositionalParameter(ids[0]);
                        cmd.AddPositionalParameter(ids[1]);
                        cmd.AddPositionalParameter(term);
                        cmd.AddPositionalParameter(year);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);

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

        private async Task TryRecordSyncUpsertAsync(string primaryKeyName, object primaryKeyValue, string operation)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync("StudentTermRemarks", primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Student term remarks sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncByCompositeAsync(string studentId, string term, string year, string operation)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var syncId = await FindSyncIdAsync(connection, studentId, term, year);
                    if (syncId != Guid.Empty)
                        await new SyncChangeRecorder(_connectionString).RecordUpsertBySyncIdAsync("StudentTermRemarks", syncId, operation);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Student term remarks sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordDeleteBySyncIdAsync(Guid syncId)
        {
            try
            {
                if (syncId == Guid.Empty) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync("StudentTermRemarks", "SyncId", syncId);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Student term remarks delete sync capture skipped: " + ex.Message);
            }
        }

        private async Task<Guid> FindSyncIdAsync(SqlConnection connection, string studentId, string term, string year)
        {
            var schoolId = TenantContext.RequireSchoolId();
            await SyncSchema.EnsureSyncInfrastructureAsync(connection, schoolId);
            var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "StudentTermRemarks");
            var query = "SELECT TOP 1 SyncId FROM StudentTermRemarks WHERE CAST(StudentID AS NVARCHAR(50)) IN (?, ?) AND Term = ? AND [Year] = ?";
            if (tenant) query += TenantContext.FilterClauseSql();
            query += " ORDER BY ID DESC";
            using (var cmd = new SqlCommand(query, connection))
            {
                var ids = BuildStudentIdCandidates(studentId);
                cmd.AddPositionalParameter(ids[0]);
                cmd.AddPositionalParameter(ids[1]);
                cmd.AddPositionalParameter(term);
                cmd.AddPositionalParameter(year);
                if (tenant) TenantContext.AddSchoolParameter(cmd);
                var value = await cmd.ExecuteScalarAsync();
                if (value is Guid id) return id;
                return Guid.TryParse(Convert.ToString(value), out var parsed) ? parsed : Guid.Empty;
            }
        }

        private static async Task<object> GetLastIdentityAsync(SqlConnection connection)
        {
            using (var cmd = new SqlCommand("SELECT @@IDENTITY", connection))
            {
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : value;
            }
        }

        private static string[] BuildStudentIdCandidates(string studentId)
        {
            var raw = (studentId ?? "").Trim();
            var numeric = StudentId.Parse(raw);
            var display = StudentId.Display(string.IsNullOrWhiteSpace(numeric) ? raw : numeric);
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
    }

    public class RepositoryException : Exception
    {
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}
