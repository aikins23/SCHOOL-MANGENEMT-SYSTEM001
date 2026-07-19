using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class ScholarshipRepository
    {
        private readonly string _connectionString;
        private const string CategoriesTable = "ScholarshipCategories";
        private const string AssignmentsTable = "StudentScholarships";
        private const string StudentsTable = "Students";

        public ScholarshipRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<List<ScholarshipCategory>> GetCategoriesAsync()
        {
            var list = new List<ScholarshipCategory>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, CategoriesTable);
                var query = "SELECT * FROM ScholarshipCategories WHERE 1=1";
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                query += " ORDER BY Name";
                using (var cmd = new SqlCommand(query, conn))
                {
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ScholarshipCategory
                            {
                                CategoryID = Convert.ToInt32(reader["CategoryID"]),
                                Name = reader["Name"].ToString(),
                                DiscountType = reader["DiscountType"].ToString(),
                                DiscountValue = Convert.ToDecimal(reader["DiscountValue"]),
                                IsActive = (bool)reader["IsActive"]
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<bool> SaveCategoryAsync(ScholarshipCategory category)
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, CategoriesTable);
                string query = category.CategoryID == 0
                    ? (tenant
                        ? "INSERT INTO ScholarshipCategories ([Name], DiscountType, DiscountValue, IsActive, SchoolId) VALUES (?, ?, ?, ?, ?)"
                        : "INSERT INTO ScholarshipCategories ([Name], DiscountType, DiscountValue, IsActive) VALUES (?, ?, ?, ?)")
                    : "UPDATE ScholarshipCategories SET [Name]=?, DiscountType=?, DiscountValue=?, IsActive=? WHERE CategoryID=?";
                if (category.CategoryID != 0 && tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(category.Name);
                    cmd.AddPositionalParameter(category.DiscountType);
                    cmd.AddPositionalParameter(category.DiscountValue);
                    cmd.AddPositionalParameter(category.IsActive);
                    if (category.CategoryID == 0 && tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    if (category.CategoryID != 0) cmd.AddPositionalParameter(category.CategoryID);
                    if (category.CategoryID != 0 && tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    var saved = await cmd.ExecuteNonQueryAsync() > 0;
                    if (saved)
                    {
                        var id = category.CategoryID == 0 ? await GetLastIdentityAsync(conn) : (object)category.CategoryID;
                        await TryRecordSyncUpsertAsync(CategoriesTable, "CategoryID", id, category.CategoryID == 0 ? "Insert" : "Update");
                    }
                    return saved;
                }
            }
        }

        public async Task<List<StudentScholarship>> GetStudentScholarshipsAsync(string studentId = null)
        {
            var list = new List<StudentScholarship>();
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var assignmentTenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                var studentTenant = await TenantContext.HasSchoolIdColumnAsync(conn, StudentsTable);
                var categoryTenant = await TenantContext.HasSchoolIdColumnAsync(conn, CategoriesTable);
                var query = @"SELECT ss.*, s.FirstName + ' ' + s.LastName as StudentName, sc.Name as CategoryName, sc.DiscountType, sc.DiscountValue
                             FROM StudentScholarships ss
                             INNER JOIN Students s ON ss.StudentID = s.StudentID
                             INNER JOIN ScholarshipCategories sc ON ss.CategoryID = sc.CategoryID
                             WHERE 1=1";

                if (assignmentTenant) query += TenantContext.FilterClauseSql("ss");
                if (studentTenant) query += TenantContext.FilterClauseSql("s");
                if (categoryTenant) query += TenantContext.FilterClauseSql("sc");
                if (!string.IsNullOrEmpty(studentId)) query += " AND ss.StudentID = ?";

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (assignmentTenant) TenantContext.AddSchoolParameter(cmd);
                    if (studentTenant) TenantContext.AddSchoolParameter(cmd);
                    if (categoryTenant) TenantContext.AddSchoolParameter(cmd);
                    if (!string.IsNullOrEmpty(studentId)) cmd.AddPositionalParameter(studentId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            list.Add(new StudentScholarship
                            {
                                AssignmentID = Convert.ToInt32(reader["AssignmentID"]),
                                StudentID = reader["StudentID"].ToString(),
                                CategoryID = Convert.ToInt32(reader["CategoryID"]),
                                AssignedDate = (DateTime)reader["AssignedDate"],
                                ApprovalStatus = reader["ApprovalStatus"]?.ToString() ?? "Pending",
                                StudentName = reader["StudentName"].ToString(),
                                CategoryName = reader["CategoryName"].ToString(),
                                DiscountType = reader["DiscountType"].ToString(),
                                DiscountValue = Convert.ToDecimal(reader["DiscountValue"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<bool> AssignScholarshipAsync(string studentId, int categoryId)
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                // Check if already assigned to avoid duplicates
                var checkQuery = "SELECT COUNT(*) FROM StudentScholarships WHERE StudentID = ? AND CategoryID = ?";
                if (tenant)
                {
                    checkQuery += TenantContext.FilterClauseSql();
                }

                using (var checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.AddPositionalParameter(studentId);
                    checkCmd.AddPositionalParameter(categoryId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(checkCmd);
                    }

                    if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0) return true;
                }

                var query = tenant
                    ? "INSERT INTO StudentScholarships (StudentID, CategoryID, ApprovalStatus, SchoolId) VALUES (?, ?, 'Pending', ?)"
                    : "INSERT INTO StudentScholarships (StudentID, CategoryID, ApprovalStatus) VALUES (?, ?, 'Pending')";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(studentId);
                    cmd.AddPositionalParameter(categoryId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    var saved = await cmd.ExecuteNonQueryAsync() > 0;
                    if (saved)
                    {
                        var id = await GetLastIdentityAsync(conn);
                        await TryRecordSyncUpsertAsync(AssignmentsTable, "AssignmentID", id, "Insert");
                    }
                    return saved;
                }
            }
        }

        public async Task<bool> UpdateScholarshipStatusAsync(int assignmentId, string status)
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var query = "UPDATE StudentScholarships SET ApprovalStatus = ? WHERE AssignmentID = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.AddPositionalParameter(status);
                    cmd.AddPositionalParameter(assignmentId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    var saved = await cmd.ExecuteNonQueryAsync() > 0;
                    if (saved) await TryRecordSyncUpsertAsync(AssignmentsTable, "AssignmentID", assignmentId, "Update");
                    return saved;
                }
            }
        }

        public async Task<bool> RemoveScholarshipAsync(int assignmentId)
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await conn.OpenAsync();
                var query = "DELETE FROM StudentScholarships WHERE AssignmentID = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                if (tenant)
                {
                    query += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(query, conn))
                {
                    await TryRecordSyncDeleteAsync(AssignmentsTable, "AssignmentID", assignmentId);
                    cmd.AddPositionalParameter(assignmentId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
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

        private async Task TryRecordSyncUpsertAsync(string tableName, string primaryKeyName, object primaryKeyValue, string operation)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(tableName, primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Scholarship sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(string tableName, string primaryKeyName, object primaryKeyValue)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(tableName, primaryKeyName, primaryKeyValue);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Scholarship delete sync capture skipped: " + ex.Message);
            }
        }
    }
}
