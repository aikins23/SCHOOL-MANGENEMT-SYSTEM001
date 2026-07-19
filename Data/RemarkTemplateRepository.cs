using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class RemarkTemplateRepository : IRemarkTemplateRepository
    {
        private readonly string _connectionString;

        public RemarkTemplateRepository() : this(AppConfig.ConnectionString) { }
        public RemarkTemplateRepository(string connectionString) { _connectionString = SqlCommandExtensions.StripProvider(connectionString); }

        public async Task EnsureTableAsync()
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RemarkTemplates' AND xtype='U')
BEGIN
    CREATE TABLE RemarkTemplates (
        RemarkTemplateId INT IDENTITY(1,1) PRIMARY KEY,
        Category NVARCHAR(50) NOT NULL,
        SubCategory NVARCHAR(50) NULL,
        RemarkText NVARCHAR(500) NOT NULL,
        SchoolId INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        DisplayOrder INT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100) NULL
    );
    CREATE INDEX IX_RemarkTemplates_Category ON RemarkTemplates(Category);
END";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                    await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task SeedDefaultsAsync()
        {
            string[,] defaults =
            {
                {"Conduct", "Excellent", "Shows exemplary conduct and is a role model to peers."},
                {"Conduct", "Good", "Generally well-behaved and respectful to teachers and peers."},
                {"Conduct", "Satisfactory", "Conduct is acceptable but inconsistent at times."},
                {"Conduct", "Needs Improvement", "Needs to improve on classroom behavior."},

                {"Effort", "Excellent", "Demonstrates outstanding effort in all subjects."},
                {"Effort", "Good", "Puts in good effort and is willing to learn."},
                {"Effort", "Satisfactory", "Effort is acceptable but could be more consistent."},
                {"Effort", "Needs Improvement", "Needs to put more effort into school work."},

                {"Attendance", "Excellent", "Excellent attendance and punctuality."},
                {"Attendance", "Good", "Good attendance with very few absences."},
                {"Attendance", "Satisfactory", "Attendance is acceptable."},
                {"Attendance", "Needs Improvement", "Attendance has been irregular and needs improvement."},

                {"Academic", "Excellent", "Outstanding academic performance across all subjects."},
                {"Academic", "Good", "Good grasp of subjects with consistent performance."},
                {"Academic", "Satisfactory", "Performance is satisfactory; revision is encouraged."},
                {"Academic", "Needs Improvement", "Needs to focus more on academic work and seek help where needed."},

                {"General", "Excellent", "A delight to teach. Keep up the excellent work!"},
                {"General", "Good", "Continue working hard and you will excel."},
                {"General", "Satisfactory", "You can do better with more dedication."},
                {"General", "Needs Improvement", "More support and effort needed. Parents are encouraged to assist."}
            };

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                for (int i = 0; i < defaults.GetLength(0); i++)
                {
                    const string sql = @"
IF NOT EXISTS (SELECT 1 FROM RemarkTemplates
               WHERE Category=@cat AND SubCategory=@sub AND RemarkText=@txt AND SchoolId IS NULL)
BEGIN
    INSERT INTO RemarkTemplates (Category, SubCategory, RemarkText, DisplayOrder, CreatedBy)
    VALUES (@cat, @sub, @txt, @ord, 'SYSTEM')
END";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat", defaults[i, 0]);
                        cmd.Parameters.AddWithValue("@sub", defaults[i, 1]);
                        cmd.Parameters.AddWithValue("@txt", defaults[i, 2]);
                        cmd.Parameters.AddWithValue("@ord", i);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<List<RemarkTemplate>> GetAllAsync()
        {
            var list = new List<RemarkTemplate>();
            const string sql = @"SELECT RemarkTemplateId, Category, SubCategory, RemarkText, SchoolId,
                                        IsActive, DisplayOrder, CreatedDate, CreatedBy
                                 FROM RemarkTemplates WHERE IsActive = 1
                                 ORDER BY Category, SubCategory, DisplayOrder";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(Map(r));
            }
            return list;
        }

        public async Task<List<RemarkTemplate>> GetByCategoryAsync(string category)
        {
            var list = new List<RemarkTemplate>();
            const string sql = @"SELECT RemarkTemplateId, Category, SubCategory, RemarkText, SchoolId,
                                        IsActive, DisplayOrder, CreatedDate, CreatedBy
                                 FROM RemarkTemplates WHERE IsActive = 1 AND Category = @c
                                 ORDER BY SubCategory, DisplayOrder";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@c", category);
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(Map(r));
                }
            }
            return list;
        }

        public async Task<List<RemarkTemplate>> GetByCategoryAndSubCategoryAsync(string category, string subCategory)
        {
            var list = new List<RemarkTemplate>();
            const string sql = @"SELECT RemarkTemplateId, Category, SubCategory, RemarkText, SchoolId,
                                        IsActive, DisplayOrder, CreatedDate, CreatedBy
                                 FROM RemarkTemplates WHERE IsActive = 1 AND Category = @c AND SubCategory = @s
                                 ORDER BY DisplayOrder";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@c", category);
                    cmd.Parameters.AddWithValue("@s", subCategory);
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(Map(r));
                }
            }
            return list;
        }

        public async Task<int> CreateAsync(RemarkTemplate t)
        {
            const string sql = @"INSERT INTO RemarkTemplates (Category, SubCategory, RemarkText, SchoolId, DisplayOrder, CreatedBy)
                                 VALUES (@cat, @sub, @txt, @sch, @ord, @by);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cat", t.Category);
                    cmd.Parameters.AddWithValue("@sub", (object)t.SubCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@txt", t.RemarkText);
                    cmd.Parameters.AddWithValue("@sch", (object)t.SchoolId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ord", t.DisplayOrder);
                    cmd.Parameters.AddWithValue("@by", (object)t.CreatedBy ?? DBNull.Value);
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
            }
        }

        public async Task<bool> UpdateAsync(RemarkTemplate t)
        {
            const string sql = @"UPDATE RemarkTemplates SET Category=@cat, SubCategory=@sub, RemarkText=@txt,
                                        DisplayOrder=@ord, IsActive=@active
                                 WHERE RemarkTemplateId=@id";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cat", t.Category);
                    cmd.Parameters.AddWithValue("@sub", (object)t.SubCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@txt", t.RemarkText);
                    cmd.Parameters.AddWithValue("@ord", t.DisplayOrder);
                    cmd.Parameters.AddWithValue("@active", t.IsActive);
                    cmd.Parameters.AddWithValue("@id", t.RemarkTemplateId);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE RemarkTemplates SET IsActive = 0 WHERE RemarkTemplateId = @id";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        private static RemarkTemplate Map(SqlDataReader r)
        {
            return new RemarkTemplate
            {
                RemarkTemplateId = r.GetInt32(0),
                Category = r.GetString(1),
                SubCategory = r.IsDBNull(2) ? null : r.GetString(2),
                RemarkText = r.GetString(3),
                SchoolId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                IsActive = r.GetBoolean(5),
                DisplayOrder = r.GetInt32(6),
                CreatedDate = r.GetDateTime(7),
                CreatedBy = r.IsDBNull(8) ? null : r.GetString(8)
            };
        }
    }
}
