using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

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
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, CategoriesTable);
                var query = "SELECT * FROM ScholarshipCategories WHERE 1=1";
                if (tenant)
                {
                    query += TenantContext.FilterClause();
                }

                query += " ORDER BY Name";
                using (var cmd = new OleDbCommand(query, conn))
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
            using (var conn = new OleDbConnection(_connectionString))
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
                    query += TenantContext.FilterClause();
                }

                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", category.Name);
                    cmd.Parameters.AddWithValue("?", category.DiscountType);
                    cmd.Parameters.AddWithValue("?", category.DiscountValue);
                    cmd.Parameters.AddWithValue("?", category.IsActive);
                    if (category.CategoryID == 0 && tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    if (category.CategoryID != 0) cmd.Parameters.AddWithValue("?", category.CategoryID);
                    if (category.CategoryID != 0 && tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<List<StudentScholarship>> GetStudentScholarshipsAsync(string studentId = null)
        {
            var list = new List<StudentScholarship>();
            using (var conn = new OleDbConnection(_connectionString))
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
                
                if (assignmentTenant) query += TenantContext.FilterClause("ss");
                if (studentTenant) query += TenantContext.FilterClause("s");
                if (categoryTenant) query += TenantContext.FilterClause("sc");
                if (!string.IsNullOrEmpty(studentId)) query += " AND ss.StudentID = ?";

                using (var cmd = new OleDbCommand(query, conn))
                {
                    if (assignmentTenant) TenantContext.AddSchoolParameter(cmd);
                    if (studentTenant) TenantContext.AddSchoolParameter(cmd);
                    if (categoryTenant) TenantContext.AddSchoolParameter(cmd);
                    if (!string.IsNullOrEmpty(studentId)) cmd.Parameters.AddWithValue("?", studentId);
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
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                // Check if already assigned to avoid duplicates
                var checkQuery = "SELECT COUNT(*) FROM StudentScholarships WHERE StudentID = ? AND CategoryID = ?";
                if (tenant)
                {
                    checkQuery += TenantContext.FilterClause();
                }

                using (var checkCmd = new OleDbCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("?", studentId);
                    checkCmd.Parameters.AddWithValue("?", categoryId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(checkCmd);
                    }

                    if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0) return true;
                }

                var query = tenant
                    ? "INSERT INTO StudentScholarships (StudentID, CategoryID, ApprovalStatus, SchoolId) VALUES (?, ?, 'Pending', ?)"
                    : "INSERT INTO StudentScholarships (StudentID, CategoryID, ApprovalStatus) VALUES (?, ?, 'Pending')";
                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", categoryId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> UpdateScholarshipStatusAsync(int assignmentId, string status)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var query = "UPDATE StudentScholarships SET ApprovalStatus = ? WHERE AssignmentID = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                if (tenant)
                {
                    query += TenantContext.FilterClause();
                }

                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", status);
                    cmd.Parameters.AddWithValue("?", assignmentId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> RemoveScholarshipAsync(int assignmentId)
        {
            using (var conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                var query = "DELETE FROM StudentScholarships WHERE AssignmentID = ?";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(conn, AssignmentsTable);
                if (tenant)
                {
                    query += TenantContext.FilterClause();
                }

                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", assignmentId);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }
    }
}
