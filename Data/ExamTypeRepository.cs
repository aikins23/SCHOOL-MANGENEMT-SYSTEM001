using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class ExamTypeRepository : IExamTypeRepository
    {
        private readonly string _connectionString;

        public ExamTypeRepository() : this(AppConfig.ConnectionString) { }
        public ExamTypeRepository(string connectionString) { _connectionString = SqlCommandExtensions.StripProvider(connectionString); }

        public async Task EnsureTableAsync()
        {
            var schoolId = await EnsureLocalSchoolIdAsync();
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExamTypes' AND xtype='U')
BEGIN
    CREATE TABLE ExamTypes (
        ExamTypeId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(20) NOT NULL,
        Description NVARCHAR(500) NULL,
        WeightPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
        IsGradedExam BIT NOT NULL DEFAULT 1,
        IncludeInReportCard BIT NOT NULL DEFAULT 1,
        DisplayOrder INT NOT NULL DEFAULT 0,
        SchoolId UNIQUEIDENTIFIER NULL,
        IsSystemType BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100) NULL
    );
END

IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'ExamTypes')
          AND c.name = N'SchoolId'
          AND t.name <> N'uniqueidentifier')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_ExamTypes_Code_School' AND parent_object_id = OBJECT_ID(N'ExamTypes'))
        ALTER TABLE ExamTypes DROP CONSTRAINT UQ_ExamTypes_Code_School;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExamTypes_SchoolId' AND object_id = OBJECT_ID(N'ExamTypes'))
        DROP INDEX IX_ExamTypes_SchoolId ON ExamTypes;
    EXEC sp_rename 'ExamTypes.SchoolId', 'LegacySchoolId', 'COLUMN';
    ALTER TABLE ExamTypes ADD SchoolId UNIQUEIDENTIFIER NULL;
END

IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'SchoolId') IS NULL
    ALTER TABLE ExamTypes ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'IsSystemType') IS NULL
    ALTER TABLE ExamTypes ADD IsSystemType BIT NOT NULL CONSTRAINT DF_ExamTypes_IsSystemType DEFAULT 0;
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'IsActive') IS NULL
    ALTER TABLE ExamTypes ADD IsActive BIT NOT NULL CONSTRAINT DF_ExamTypes_IsActive DEFAULT 1;
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'SyncId') IS NULL
    ALTER TABLE ExamTypes ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ExamTypes_SyncId DEFAULT NEWID();
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'UpdatedAt') IS NULL
    ALTER TABLE ExamTypes ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ExamTypes_UpdatedAt DEFAULT SYSUTCDATETIME();

IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL
BEGIN
    ;WITH Candidates AS
    (
        SELECT ExamTypeId,
               ROW_NUMBER() OVER
               (
                   PARTITION BY CASE WHEN SchoolId IS NULL THEN @SchoolId ELSE SchoolId END, Code
                   ORDER BY
                       CASE WHEN SchoolId = @SchoolId THEN 0 WHEN SchoolId IS NULL THEN 1 ELSE 2 END,
                       IsSystemType DESC,
                       IsActive DESC,
                       ExamTypeId
               ) AS rn
        FROM ExamTypes
        WHERE Code IS NOT NULL
          AND (SchoolId = @SchoolId OR SchoolId IS NULL)
    )
    UPDATE et
       SET Code = LEFT(COALESCE(NULLIF(et.Code, N''), N'EXAM'), 8) + N'-D' + CONVERT(NVARCHAR(10), et.ExamTypeId),
           IsActive = 0,
           UpdatedAt = SYSUTCDATETIME()
    FROM ExamTypes et
    INNER JOIN Candidates c ON c.ExamTypeId = et.ExamTypeId
    WHERE c.rn > 1;

    UPDATE ExamTypes SET SchoolId = @SchoolId WHERE SchoolId IS NULL;

    ;WITH Duplicates AS
    (
        SELECT ExamTypeId,
               ROW_NUMBER() OVER
               (
                   PARTITION BY SchoolId, Code
                   ORDER BY IsSystemType DESC, IsActive DESC, ExamTypeId
               ) AS rn
        FROM ExamTypes
        WHERE Code IS NOT NULL AND SchoolId IS NOT NULL
    )
    UPDATE et
       SET Code = LEFT(COALESCE(NULLIF(et.Code, N''), N'EXAM'), 8) + N'-D' + CONVERT(NVARCHAR(10), et.ExamTypeId),
           IsActive = 0,
           UpdatedAt = SYSUTCDATETIME()
    FROM ExamTypes et
    INNER JOIN Duplicates d ON d.ExamTypeId = et.ExamTypeId
    WHERE d.rn > 1;
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExamTypes_SchoolId' AND object_id = OBJECT_ID(N'ExamTypes'))
    CREATE INDEX IX_ExamTypes_SchoolId ON ExamTypes(SchoolId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExamTypes_School_Code' AND object_id = OBJECT_ID(N'ExamTypes'))
   AND NOT EXISTS (
        SELECT 1 FROM ExamTypes
        WHERE Code IS NOT NULL AND SchoolId IS NOT NULL
        GROUP BY SchoolId, Code
        HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UX_ExamTypes_School_Code ON ExamTypes(SchoolId, Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExamTypes_SyncId' AND object_id = OBJECT_ID(N'ExamTypes'))
    CREATE UNIQUE INDEX UX_ExamTypes_SyncId ON ExamTypes(SyncId);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name='ExamTypeId' AND Object_ID = Object_ID('examss'))
BEGIN
    ALTER TABLE examss ADD ExamTypeId INT NULL
END

IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name='ExamTypeId' AND Object_ID = Object_ID('ExamSetups'))
BEGIN
    ALTER TABLE ExamSetups ADD ExamTypeId INT NULL
END

IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'AssessmentNumber') IS NULL
    ALTER TABLE ExamSetups ADD AssessmentNumber INT NULL
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'AssessmentLabel') IS NULL
    ALTER TABLE ExamSetups ADD AssessmentLabel NVARCHAR(100) NULL
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'IsPublishedToPortal') IS NULL
    ALTER TABLE ExamSetups ADD IsPublishedToPortal BIT NOT NULL CONSTRAINT DF_ExamSetups_IsPublished DEFAULT 0
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'PublishedAt') IS NULL
    ALTER TABLE ExamSetups ADD PublishedAt DATETIME2 NULL
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'PublishedBy') IS NULL
    ALTER TABLE ExamSetups ADD PublishedBy NVARCHAR(100) NULL
IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss', 'AssessmentNumber') IS NULL
    ALTER TABLE examss ADD AssessmentNumber INT NULL
IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss', 'AssessmentLabel') IS NULL
    ALTER TABLE examss ADD AssessmentLabel NVARCHAR(100) NULL
IF OBJECT_ID(N'examss', N'U') IS NOT NULL
   AND COL_LENGTH('examss', 'year') IS NOT NULL
   AND COL_LENGTH('examss', 'year') < 20
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Result_Lookup' AND object_id = OBJECT_ID(N'examss'))
        DROP INDEX IX_examss_Result_Lookup ON examss
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Class_Term_Year_Subject' AND object_id = OBJECT_ID(N'examss'))
        DROP INDEX IX_examss_Class_Term_Year_Subject ON examss
    ALTER TABLE examss ALTER COLUMN [year] NVARCHAR(20) NULL
END
IF OBJECT_ID(N'examss', N'U') IS NOT NULL
   AND COL_LENGTH('examss', 'std_class') IS NOT NULL
   AND COL_LENGTH('examss', 'term') IS NOT NULL
   AND COL_LENGTH('examss', 'year') IS NOT NULL
   AND COL_LENGTH('examss', 'subject') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Class_Term_Year_Subject' AND object_id = OBJECT_ID(N'examss'))
    CREATE INDEX IX_examss_Class_Term_Year_Subject ON examss(std_class, term, [year], subject)";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task SeedSystemTypesAsync()
        {
            var schoolId = await EnsureLocalSchoolIdAsync();
            object[,] defaults = new object[,]
            {
                { "End of Term", "EOT", "Final terminal examination", 70m, true, true, 1 },
                { "Mid-Term Exam", "MID", "Half-term examination", 20m, true, true, 2 },
                { "Class Test", "CT", "Continuous assessment class tests", 10m, true, true, 3 },
                { "Mock Examination", "MOCK", "Mock exam (e.g. BECE mock for Basic 9)", 0m, true, false, 4 },
                { "Quiz", "QUIZ", "Short class quizzes", 0m, false, false, 5 },
                { "Assignment", "ASSIGN", "Homework / take-home assignments", 0m, false, false, 6 }
            };

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                for (int i = 0; i < defaults.GetLength(0); i++)
                {
                    const string sql = @"
DECLARE @existingId INT;

SELECT TOP 1 @existingId = ExamTypeId
FROM ExamTypes
WHERE (SchoolId = @schoolId AND (Code = @code OR Name = @name))
   OR (SchoolId IS NULL AND (Code = @code OR Name = @name))
   OR Code = @code
ORDER BY
    CASE
        WHEN SchoolId = @schoolId THEN 0
        WHEN SchoolId IS NULL THEN 1
        ELSE 2
    END,
    ExamTypeId;

IF @existingId IS NULL
BEGIN
    INSERT INTO ExamTypes (Name, Code, Description, WeightPercentage, IsGradedExam,
                           IncludeInReportCard, DisplayOrder, SchoolId, IsSystemType, CreatedBy)
    VALUES (@name, @code, @desc, @weight, @graded, @rc, @ord, @schoolId, 1, 'SYSTEM')
END
ELSE
BEGIN
    UPDATE ExamTypes
       SET Name = @name,
           Code = @code,
           Description = @desc,
           WeightPercentage = @weight,
           IsGradedExam = @graded,
           IncludeInReportCard = @rc,
           DisplayOrder = @ord,
           IsSystemType = 1,
           IsActive = 1,
           SchoolId = CASE WHEN SchoolId IS NULL THEN @schoolId ELSE SchoolId END,
           UpdatedAt = SYSUTCDATETIME()
     WHERE ExamTypeId = @existingId
END";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", defaults[i, 0]);
                        cmd.Parameters.AddWithValue("@code", defaults[i, 1]);
                        cmd.Parameters.AddWithValue("@desc", defaults[i, 2]);
                        cmd.Parameters.AddWithValue("@weight", defaults[i, 3]);
                        cmd.Parameters.AddWithValue("@graded", defaults[i, 4]);
                        cmd.Parameters.AddWithValue("@rc", defaults[i, 5]);
                        cmd.Parameters.AddWithValue("@ord", defaults[i, 6]);
                        cmd.Parameters.AddWithValue("@schoolId", schoolId);
                        try
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                        {
                            Services.LoggerHelper.LogWarning("Default exam type already exists; seed skipped for " + defaults[i, 1] + ": " + ex.Message);
                        }
                    }
                }
            }
        }

        public async Task<List<ExamType>> GetAllAsync() => await QueryAsync(
            @"SELECT ExamTypeId, Name, Code, Description, WeightPercentage, IsGradedExam,
                     IncludeInReportCard, DisplayOrder, SchoolId, IsSystemType, IsActive,
                     CreatedDate, CreatedBy
              FROM ExamTypes ORDER BY DisplayOrder, Name");

        public async Task<List<ExamType>> GetActiveAsync() => await QueryAsync(
            @"SELECT ExamTypeId, Name, Code, Description, WeightPercentage, IsGradedExam,
                     IncludeInReportCard, DisplayOrder, SchoolId, IsSystemType, IsActive,
                     CreatedDate, CreatedBy
              FROM ExamTypes WHERE IsActive = 1 ORDER BY DisplayOrder, Name");

        public async Task<List<ExamType>> GetReportCardTypesAsync() => await QueryAsync(
            @"SELECT ExamTypeId, Name, Code, Description, WeightPercentage, IsGradedExam,
                     IncludeInReportCard, DisplayOrder, SchoolId, IsSystemType, IsActive,
                     CreatedDate, CreatedBy
              FROM ExamTypes WHERE IsActive = 1 AND IncludeInReportCard = 1
              ORDER BY DisplayOrder, Name");

        private async Task<List<ExamType>> QueryAsync(string sql)
        {
            var list = new List<ExamType>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(Map(r));
            }
            return list;
        }

        public async Task<ExamType> GetByIdAsync(int id) => await QueryOneAsync(
            @"SELECT ExamTypeId, Name, Code, Description, WeightPercentage, IsGradedExam,
                     IncludeInReportCard, DisplayOrder, SchoolId, IsSystemType, IsActive,
                     CreatedDate, CreatedBy
              FROM ExamTypes WHERE ExamTypeId = @id",
            ("@id", id));

        public async Task<ExamType> GetByCodeAsync(string code) => await QueryOneAsync(
            @"SELECT TOP 1 ExamTypeId, Name, Code, Description, WeightPercentage, IsGradedExam,
                     IncludeInReportCard, DisplayOrder, SchoolId, IsSystemType, IsActive,
                     CreatedDate, CreatedBy
              FROM ExamTypes WHERE Code = @code AND IsActive = 1",
            ("@code", code));

        private async Task<ExamType> QueryOneAsync(string sql, params (string, object)[] args)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    foreach (var (n, v) in args) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
                    using (var r = await cmd.ExecuteReaderAsync())
                        if (await r.ReadAsync()) return Map(r);
                }
            }
            return null;
        }

        public async Task<int> CreateAsync(ExamType t)
        {
            var schoolId = t.SchoolId ?? await EnsureLocalSchoolIdAsync();
            var existingId = await FindExistingExamTypeIdAsync(t.Code, t.Name, schoolId);
            if (existingId.HasValue) return existingId.Value;

            const string sql = @"INSERT INTO ExamTypes (Name, Code, Description, WeightPercentage,
                                  IsGradedExam, IncludeInReportCard, DisplayOrder, SchoolId, CreatedBy)
                                 VALUES (@n, @c, @d, @w, @g, @rc, @o, @s, @by);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@n", t.Name);
                    cmd.Parameters.AddWithValue("@c", t.Code);
                    cmd.Parameters.AddWithValue("@d", (object)t.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@w", t.WeightPercentage);
                    cmd.Parameters.AddWithValue("@g", t.IsGradedExam);
                    cmd.Parameters.AddWithValue("@rc", t.IncludeInReportCard);
                    cmd.Parameters.AddWithValue("@o", t.DisplayOrder);
                    cmd.Parameters.AddWithValue("@s", schoolId);
                    cmd.Parameters.AddWithValue("@by", (object)t.CreatedBy ?? DBNull.Value);
                    try
                    {
                        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        await TryRecordExamTypeSyncAsync(id, "Insert");
                        return id;
                    }
                    catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                    {
                        existingId = await FindExistingExamTypeIdAsync(t.Code, t.Name, schoolId);
                        if (existingId.HasValue) return existingId.Value;
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateAsync(ExamType t)
        {
            const string sql = @"UPDATE ExamTypes SET Name=@n, Description=@d, WeightPercentage=@w,
                                       IsGradedExam=@g, IncludeInReportCard=@rc, DisplayOrder=@o,
                                       IsActive=@active
                                 WHERE ExamTypeId=@id";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@n", t.Name);
                    cmd.Parameters.AddWithValue("@d", (object)t.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@w", t.WeightPercentage);
                    cmd.Parameters.AddWithValue("@g", t.IsGradedExam);
                    cmd.Parameters.AddWithValue("@rc", t.IncludeInReportCard);
                    cmd.Parameters.AddWithValue("@o", t.DisplayOrder);
                    cmd.Parameters.AddWithValue("@active", t.IsActive);
                    cmd.Parameters.AddWithValue("@id", t.ExamTypeId);
                    var saved = (await cmd.ExecuteNonQueryAsync()) > 0;
                    if (saved) await TryRecordExamTypeSyncAsync(t.ExamTypeId, "Update");
                    return saved;
                }
            }
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            const string sql = "UPDATE ExamTypes SET IsActive = 0 WHERE ExamTypeId = @id AND IsSystemType = 0";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    var saved = (await cmd.ExecuteNonQueryAsync()) > 0;
                    if (saved) await TryRecordExamTypeSyncAsync(id, "Update");
                    return saved;
                }
            }
        }

        public async Task<decimal> GetTotalWeightAsync()
        {
            const string sql = @"SELECT ISNULL(SUM(WeightPercentage), 0) FROM ExamTypes
                                 WHERE IsActive = 1 AND IncludeInReportCard = 1";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                    return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
            }
        }

        private static ExamType Map(SqlDataReader r)
        {
            return new ExamType
            {
                ExamTypeId = r.GetInt32(0),
                Name = r.GetString(1),
                Code = r.GetString(2),
                Description = r.IsDBNull(3) ? null : r.GetString(3),
                WeightPercentage = r.GetDecimal(4),
                IsGradedExam = r.GetBoolean(5),
                IncludeInReportCard = r.GetBoolean(6),
                DisplayOrder = r.GetInt32(7),
                SchoolId = r.IsDBNull(8) ? (Guid?)null : r.GetGuid(8),
                IsSystemType = r.GetBoolean(9),
                IsActive = r.GetBoolean(10),
                CreatedDate = r.GetDateTime(11),
                CreatedBy = r.IsDBNull(12) ? null : r.GetString(12)
            };
        }

        private async Task<Guid> EnsureLocalSchoolIdAsync()
        {
            var repo = new SchoolInfoRepository(_connectionString);
            await repo.EnsureTablesAsync();
            var info = await repo.GetAsync();
            if (info.SchoolId == Guid.Empty)
            {
                info.SchoolId = Guid.NewGuid();
                await repo.SaveAsync(info);
            }
            return info.SchoolId;
        }

        private async Task<int?> FindExistingExamTypeIdAsync(string code, string name, Guid schoolId)
        {
            const string sql = @"
SELECT TOP 1 ExamTypeId
FROM ExamTypes
WHERE IsActive = 1
  AND (SchoolId = @schoolId OR SchoolId IS NULL)
  AND (
        (NULLIF(@code, N'') IS NOT NULL AND Code = @code)
        OR (NULLIF(@name, N'') IS NOT NULL AND Name = @name)
      )
ORDER BY
    CASE WHEN SchoolId = @schoolId THEN 0 ELSE 1 END,
    IsSystemType DESC,
    DisplayOrder,
    ExamTypeId";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@schoolId", schoolId);
                    cmd.Parameters.AddWithValue("@code", (object)code ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@name", (object)name ?? DBNull.Value);
                    var value = await cmd.ExecuteScalarAsync();
                    return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
                }
            }
        }

        private async Task TryRecordExamTypeSyncAsync(int examTypeId, string operation)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString)
                    .RecordUpsertAsync("ExamTypes", "ExamTypeId", examTypeId, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Exam type sync capture skipped: " + ex.Message);
            }
        }
    }
}
