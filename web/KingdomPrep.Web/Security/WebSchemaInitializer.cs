using KingdomPrep.Web.Api;
using KingdomPrep.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Security;

public sealed class WebSchemaInitializer(
    IDbContextFactory<AppDbContext> dbFactory,
    SyncDeviceRegistryService syncDeviceRegistry,
    SyncInboxService syncInbox,
    ILogger<WebSchemaInitializer> logger)
{
    internal const string WebSupportTablesSql = """
IF OBJECT_ID(N'ExamTypes', N'U') IS NULL
BEGIN
    CREATE TABLE ExamTypes (
        ExamTypeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(20) NOT NULL,
        Description NVARCHAR(500) NULL,
        WeightPercentage DECIMAL(5,2) NOT NULL CONSTRAINT DF_ExamTypes_Weight DEFAULT 0,
        IsGradedExam BIT NOT NULL CONSTRAINT DF_ExamTypes_IsGraded DEFAULT 1,
        IncludeInReportCard BIT NOT NULL CONSTRAINT DF_ExamTypes_ReportCard DEFAULT 1,
        DisplayOrder INT NOT NULL CONSTRAINT DF_ExamTypes_DisplayOrder DEFAULT 0,
        SchoolId UNIQUEIDENTIFIER NULL,
        IsSystemType BIT NOT NULL CONSTRAINT DF_ExamTypes_IsSystem DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_ExamTypes_IsActive DEFAULT 1,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_ExamTypes_CreatedDate DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100) NULL,
        CONSTRAINT UQ_ExamTypes_Code_School UNIQUE (Code, SchoolId)
    );
END
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'IsActive') IS NULL
    ALTER TABLE ExamTypes ADD IsActive BIT NOT NULL CONSTRAINT DF_ExamTypes_IsActive_Upgrade DEFAULT 1;
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'IsSystemType') IS NULL
    ALTER TABLE ExamTypes ADD IsSystemType BIT NOT NULL CONSTRAINT DF_ExamTypes_IsSystem_Upgrade DEFAULT 0;
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
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'SyncId') IS NULL
    ALTER TABLE ExamTypes ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ExamTypes_SyncId DEFAULT NEWID();
IF OBJECT_ID(N'ExamTypes', N'U') IS NOT NULL AND COL_LENGTH('ExamTypes', 'UpdatedAt') IS NULL
    ALTER TABLE ExamTypes ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ExamTypes_UpdatedAt DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExamTypes_Active_Order' AND object_id = OBJECT_ID(N'ExamTypes'))
    CREATE INDEX IX_ExamTypes_Active_Order ON ExamTypes(IsActive, DisplayOrder, Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExamTypes_School_Code' AND object_id = OBJECT_ID(N'ExamTypes'))
   AND NOT EXISTS (
        SELECT 1 FROM ExamTypes
        WHERE Code IS NOT NULL AND SchoolId IS NOT NULL
        GROUP BY SchoolId, Code
        HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UX_ExamTypes_School_Code ON ExamTypes(SchoolId, Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExamTypes_SyncId' AND object_id = OBJECT_ID(N'ExamTypes'))
    CREATE UNIQUE INDEX UX_ExamTypes_SyncId ON ExamTypes(SyncId);

IF OBJECT_ID(N'ExamSetups', N'U') IS NULL
BEGIN
    CREATE TABLE ExamSetups (
        SetupID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Term NVARCHAR(100) NULL,
        [Year] NVARCHAR(100) NULL,
        StartDate DATETIME2 NULL,
        EndDate DATETIME2 NULL,
        ExamTypeId INT NULL,
        AssessmentNumber INT NULL,
        AssessmentLabel NVARCHAR(100) NULL,
        ClassWeight INT NOT NULL CONSTRAINT DF_ExamSetups_ClassWeight DEFAULT 50,
        ExamWeight INT NOT NULL CONSTRAINT DF_ExamSetups_ExamWeight DEFAULT 50,
        SchoolId UNIQUEIDENTIFIER NULL,
        IsPublishedToPortal BIT NOT NULL CONSTRAINT DF_ExamSetups_IsPublished DEFAULT 0,
        PublishedAt DATETIME2 NULL,
        PublishedBy NVARCHAR(100) NULL
    );
END
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'SchoolId') IS NULL
    ALTER TABLE ExamSetups ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'ExamTypeId') IS NULL
    ALTER TABLE ExamSetups ADD ExamTypeId INT NULL;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'AssessmentNumber') IS NULL
    ALTER TABLE ExamSetups ADD AssessmentNumber INT NULL;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'AssessmentLabel') IS NULL
    ALTER TABLE ExamSetups ADD AssessmentLabel NVARCHAR(100) NULL;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'IsPublishedToPortal') IS NULL
    ALTER TABLE ExamSetups ADD IsPublishedToPortal BIT NOT NULL CONSTRAINT DF_ExamSetups_IsPublished_Upgrade DEFAULT 0;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'PublishedAt') IS NULL
    ALTER TABLE ExamSetups ADD PublishedAt DATETIME2 NULL;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'PublishedBy') IS NULL
    ALTER TABLE ExamSetups ADD PublishedBy NVARCHAR(100) NULL;
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'SyncId') IS NULL
    ALTER TABLE ExamSetups ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ExamSetups_SyncId DEFAULT NEWID();
IF OBJECT_ID(N'ExamSetups', N'U') IS NOT NULL AND COL_LENGTH('ExamSetups', 'UpdatedAt') IS NULL
    ALTER TABLE ExamSetups ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ExamSetups_UpdatedAt DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExamSetups_School_TermYear' AND object_id = OBJECT_ID(N'ExamSetups'))
    CREATE INDEX IX_ExamSetups_School_TermYear ON ExamSetups(SchoolId, Term, [Year]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExamSetups_Published' AND object_id = OBJECT_ID(N'ExamSetups'))
    CREATE INDEX IX_ExamSetups_Published ON ExamSetups(SchoolId, Term, [Year], IsPublishedToPortal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExamSetups_SyncId' AND object_id = OBJECT_ID(N'ExamSetups'))
    CREATE UNIQUE INDEX UX_ExamSetups_SyncId ON ExamSetups(SyncId);

IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss', 'ExamTypeId') IS NULL
    ALTER TABLE examss ADD ExamTypeId INT NULL;
IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss', 'AssessmentNumber') IS NULL
    ALTER TABLE examss ADD AssessmentNumber INT NULL;
IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss', 'AssessmentLabel') IS NULL
    ALTER TABLE examss ADD AssessmentLabel NVARCHAR(100) NULL;
IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss', 'UpdatedAt') IS NULL
    ALTER TABLE examss ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_examss_UpdatedAt_Upgrade DEFAULT SYSUTCDATETIME();
IF OBJECT_ID(N'examss', N'U') IS NOT NULL
   AND COL_LENGTH('examss', 'year') IS NOT NULL
   AND COL_LENGTH('examss', 'year') < 20
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Result_Lookup' AND object_id = OBJECT_ID(N'examss'))
        DROP INDEX IX_examss_Result_Lookup ON examss;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Class_Term_Year_Subject' AND object_id = OBJECT_ID(N'examss'))
        DROP INDEX IX_examss_Class_Term_Year_Subject ON examss;
    ALTER TABLE examss ALTER COLUMN [year] NVARCHAR(20) NULL;
END
IF OBJECT_ID(N'examss', N'U') IS NOT NULL
   AND COL_LENGTH('examss', 'std_class') IS NOT NULL
   AND COL_LENGTH('examss', 'term') IS NOT NULL
   AND COL_LENGTH('examss', 'year') IS NOT NULL
   AND COL_LENGTH('examss', 'subject') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Class_Term_Year_Subject' AND object_id = OBJECT_ID(N'examss'))
    CREATE INDEX IX_examss_Class_Term_Year_Subject ON examss(std_class, term, [year], subject);

IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NULL
BEGIN
    CREATE TABLE ApprovalWorkflows (
        WorkflowId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId INT NOT NULL,
        CurrentStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_ApprovalWorkflows_Status DEFAULT 'Draft',
        SchoolId UNIQUEIDENTIFIER NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ApprovalWorkflows_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ApprovalWorkflows_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NOT NULL AND COL_LENGTH('ApprovalWorkflows', 'SchoolId') IS NULL
    ALTER TABLE ApprovalWorkflows ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NOT NULL AND COL_LENGTH('ApprovalWorkflows', 'SyncId') IS NULL
    ALTER TABLE ApprovalWorkflows ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ApprovalWorkflows_SyncId_Upgrade DEFAULT NEWID();
IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NOT NULL AND COL_LENGTH('ApprovalWorkflows', 'UpdatedAt') IS NULL
    ALTER TABLE ApprovalWorkflows ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ApprovalWorkflows_UpdatedAt_Upgrade DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ApprovalWorkflows_SyncId' AND object_id = OBJECT_ID(N'ApprovalWorkflows'))
    CREATE UNIQUE INDEX UX_ApprovalWorkflows_SyncId ON ApprovalWorkflows(SyncId);

IF OBJECT_ID(N'ApprovalSteps', N'U') IS NULL
BEGIN
    CREATE TABLE ApprovalSteps (
        StepId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WorkflowId INT NOT NULL,
        StepOrder INT NOT NULL,
        ApproverRole NVARCHAR(50) NOT NULL,
        ApprovedByUserId INT NULL,
        ApprovalDate DATETIME NULL,
        [Action] NVARCHAR(50) NOT NULL CONSTRAINT DF_ApprovalSteps_Action DEFAULT 'Pending',
        Comments NVARCHAR(MAX) NULL
    );
END

IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NULL
BEGIN
    CREATE TABLE ClassPerformanceReports (
        ReportId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClassId NVARCHAR(50) NOT NULL,
        TeacherId INT NOT NULL,
        ReportDate DATETIME NOT NULL,
        ReportPeriod NVARCHAR(50) NOT NULL,
        AcademicYear NVARCHAR(20) NOT NULL,
        Term NVARCHAR(20) NOT NULL,
        WeekNumber INT NULL,
        MonthNumber INT NULL,
        ReportText NVARCHAR(MAX) NULL,
        AnalyticsData NVARCHAR(MAX) NULL,
        WorkflowId INT NOT NULL,
        SchoolId UNIQUEIDENTIFIER NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ClassPerformanceReports_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ClassPerformanceReports_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NOT NULL AND COL_LENGTH('ClassPerformanceReports', 'SchoolId') IS NULL
    ALTER TABLE ClassPerformanceReports ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NOT NULL AND COL_LENGTH('ClassPerformanceReports', 'SyncId') IS NULL
    ALTER TABLE ClassPerformanceReports ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ClassPerformanceReports_SyncId_Upgrade DEFAULT NEWID();
IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NOT NULL AND COL_LENGTH('ClassPerformanceReports', 'UpdatedAt') IS NULL
    ALTER TABLE ClassPerformanceReports ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ClassPerformanceReports_UpdatedAt_Upgrade DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassPerformanceReports_School_Status' AND object_id = OBJECT_ID(N'ClassPerformanceReports'))
    CREATE INDEX IX_ClassPerformanceReports_School_Status ON ClassPerformanceReports(SchoolId, ReportDate DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ClassPerformanceReports_SyncId' AND object_id = OBJECT_ID(N'ClassPerformanceReports'))
    CREATE UNIQUE INDEX UX_ClassPerformanceReports_SyncId ON ClassPerformanceReports(SyncId);

IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NULL
BEGIN
    CREATE TABLE StudentPerformanceEntries (
        EntryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReportId INT NOT NULL,
        StudentId NVARCHAR(50) NOT NULL,
        PerformanceTrend NVARCHAR(50) NOT NULL CONSTRAINT DF_StudentPerformanceEntries_Trend DEFAULT 'Stable',
        ExerciseMarksObtained DECIMAL(6,2) NULL,
        ExerciseMarksTotal DECIMAL(6,2) NULL,
        HomeworkMarksObtained DECIMAL(6,2) NULL,
        HomeworkMarksTotal DECIMAL(6,2) NULL,
        TeacherNotes NVARCHAR(MAX) NULL,
        SchoolId UNIQUEIDENTIFIER NULL,
        SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_StudentPerformanceEntries_SyncId DEFAULT NEWID(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_StudentPerformanceEntries_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'SchoolId') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD SchoolId UNIQUEIDENTIFIER NULL;
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'ExerciseMarksObtained') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD ExerciseMarksObtained DECIMAL(6,2) NULL;
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'ExerciseMarksTotal') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD ExerciseMarksTotal DECIMAL(6,2) NULL;
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'HomeworkMarksObtained') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD HomeworkMarksObtained DECIMAL(6,2) NULL;
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'HomeworkMarksTotal') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD HomeworkMarksTotal DECIMAL(6,2) NULL;
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'SyncId') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_StudentPerformanceEntries_SyncId_Upgrade DEFAULT NEWID();
IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'UpdatedAt') IS NULL
    ALTER TABLE StudentPerformanceEntries ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_StudentPerformanceEntries_UpdatedAt_Upgrade DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentPerformanceEntries_Report_Student' AND object_id = OBJECT_ID(N'StudentPerformanceEntries'))
    CREATE INDEX IX_StudentPerformanceEntries_Report_Student ON StudentPerformanceEntries(ReportId, StudentId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentPerformanceEntries_School_Report' AND object_id = OBJECT_ID(N'StudentPerformanceEntries'))
    CREATE INDEX IX_StudentPerformanceEntries_School_Report ON StudentPerformanceEntries(SchoolId, ReportId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_StudentPerformanceEntries_SyncId' AND object_id = OBJECT_ID(N'StudentPerformanceEntries'))
    CREATE UNIQUE INDEX UX_StudentPerformanceEntries_SyncId ON StudentPerformanceEntries(SyncId);

IF OBJECT_ID(N'AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE AuditLogs (
        AuditLogID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt DATETIME2 NOT NULL,
        ActorUsername NVARCHAR(150) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NULL,
        Summary NVARCHAR(1000) NULL,
        SchoolId UNIQUEIDENTIFIER NULL
    );
END
IF OBJECT_ID(N'AuditLogs', N'U') IS NOT NULL AND COL_LENGTH('AuditLogs', 'SchoolId') IS NULL
    ALTER TABLE AuditLogs ADD SchoolId UNIQUEIDENTIFIER NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_School_CreatedAt' AND object_id = OBJECT_ID(N'AuditLogs'))
    CREATE INDEX IX_AuditLogs_School_CreatedAt ON AuditLogs(SchoolId, CreatedAt DESC);

IF OBJECT_ID(N'SchoolInformation', N'U') IS NOT NULL AND COL_LENGTH('SchoolInformation', 'Latitude') IS NULL
    ALTER TABLE SchoolInformation ADD Latitude FLOAT NOT NULL CONSTRAINT DF_SI_Lat DEFAULT (5.6037);
IF OBJECT_ID(N'SchoolInformation', N'U') IS NOT NULL AND COL_LENGTH('SchoolInformation', 'Longitude') IS NULL
    ALTER TABLE SchoolInformation ADD Longitude FLOAT NOT NULL CONSTRAINT DF_SI_Lng DEFAULT (-0.1870);
IF OBJECT_ID(N'SchoolInformation', N'U') IS NOT NULL AND COL_LENGTH('SchoolInformation', 'GeofenceRadiusMeters') IS NULL
    ALTER TABLE SchoolInformation ADD GeofenceRadiusMeters FLOAT NOT NULL CONSTRAINT DF_SI_Radius DEFAULT (150.0);

IF OBJECT_ID(N'StaffAttendance', N'U') IS NULL
BEGIN
    CREATE TABLE StaffAttendance (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeId INT NOT NULL,
        EmployeeName NVARCHAR(200) NOT NULL,
        Department NVARCHAR(100) NOT NULL,
        Date DATETIME NOT NULL,
        ClockInTime DATETIME NOT NULL,
        ClockInLatitude FLOAT NOT NULL,
        ClockInLongitude FLOAT NOT NULL,
        ClockInDistanceMeters FLOAT NOT NULL,
        ClockOutTime DATETIME NULL,
        ClockOutLatitude FLOAT NULL,
        ClockOutLongitude FLOAT NULL,
        ClockOutDistanceMeters FLOAT NULL,
        Status NVARCHAR(50) NOT NULL,
        VerificationStatus NVARCHAR(100) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        SchoolId UNIQUEIDENTIFIER NULL
    );
END
""";

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Ensuring web-owned database support schema.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(WebSupportTablesSql, cancellationToken);

        await syncDeviceRegistry.EnsureSchemaAsync(cancellationToken);
        await syncInbox.EnsureSchemaAsync(cancellationToken);
    }
}
