using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public static class ReportSchema
    {
        public static async Task EnsureReportTablesAsync()
        {
            using (var conn = new SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await conn.OpenAsync();

                // 1. Approval Workflows
                var cmd1 = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ApprovalWorkflows' and xtype='U')
                    BEGIN
                        CREATE TABLE ApprovalWorkflows (
                            WorkflowId INT IDENTITY(1,1) PRIMARY KEY,
                            EntityType NVARCHAR(50) NOT NULL,
                            EntityId INT NOT NULL,
                            CurrentStatus NVARCHAR(50) NOT NULL DEFAULT 'Draft',
                            SchoolId UNIQUEIDENTIFIER NULL,
                            SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                            UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                        );
                    END
                    IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NOT NULL AND COL_LENGTH('ApprovalWorkflows', 'SchoolId') IS NULL
                        ALTER TABLE ApprovalWorkflows ADD SchoolId UNIQUEIDENTIFIER NULL;
                    IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NOT NULL AND COL_LENGTH('ApprovalWorkflows', 'SyncId') IS NULL
                        ALTER TABLE ApprovalWorkflows ADD SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
                    IF OBJECT_ID(N'ApprovalWorkflows', N'U') IS NOT NULL AND COL_LENGTH('ApprovalWorkflows', 'UpdatedAt') IS NULL
                        ALTER TABLE ApprovalWorkflows ADD UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
                ", conn);
                await cmd1.ExecuteNonQueryAsync();

                // 2. Approval Steps
                var cmd2 = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ApprovalSteps' and xtype='U')
                    BEGIN
                        CREATE TABLE ApprovalSteps (
                            StepId INT IDENTITY(1,1) PRIMARY KEY,
                            WorkflowId INT NOT NULL FOREIGN KEY REFERENCES ApprovalWorkflows(WorkflowId) ON DELETE CASCADE,
                            StepOrder INT NOT NULL,
                            ApproverRole NVARCHAR(50) NOT NULL,
                            ApprovedByUserId INT NULL,
                            ApprovalDate DATETIME NULL,
                            Action NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                            Comments NVARCHAR(MAX) NULL
                        );
                    END
                ", conn);
                await cmd2.ExecuteNonQueryAsync();

                // 3. Class Performance Reports
                var cmd3 = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClassPerformanceReports' and xtype='U')
                    BEGIN
                        CREATE TABLE ClassPerformanceReports (
                            ReportId INT IDENTITY(1,1) PRIMARY KEY,
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
                            WorkflowId INT NOT NULL FOREIGN KEY REFERENCES ApprovalWorkflows(WorkflowId),
                            SchoolId UNIQUEIDENTIFIER NULL,
                            SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                            UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                        );
                    END
                    IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NOT NULL AND COL_LENGTH('ClassPerformanceReports', 'SchoolId') IS NULL
                        ALTER TABLE ClassPerformanceReports ADD SchoolId UNIQUEIDENTIFIER NULL;
                    IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NOT NULL AND COL_LENGTH('ClassPerformanceReports', 'SyncId') IS NULL
                        ALTER TABLE ClassPerformanceReports ADD SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
                    IF OBJECT_ID(N'ClassPerformanceReports', N'U') IS NOT NULL AND COL_LENGTH('ClassPerformanceReports', 'UpdatedAt') IS NULL
                        ALTER TABLE ClassPerformanceReports ADD UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
                ", conn);
                await cmd3.ExecuteNonQueryAsync();

                // 4. Student Performance Entries
                var cmd4 = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentPerformanceEntries' and xtype='U')
                    BEGIN
                        CREATE TABLE StudentPerformanceEntries (
                            EntryId INT IDENTITY(1,1) PRIMARY KEY,
                            ReportId INT NOT NULL FOREIGN KEY REFERENCES ClassPerformanceReports(ReportId) ON DELETE CASCADE,
                            StudentId NVARCHAR(50) NOT NULL,
                            PerformanceTrend NVARCHAR(50) NOT NULL DEFAULT 'Stable',
                            ExerciseMarksObtained DECIMAL(6,2) NULL,
                            ExerciseMarksTotal DECIMAL(6,2) NULL,
                            HomeworkMarksObtained DECIMAL(6,2) NULL,
                            HomeworkMarksTotal DECIMAL(6,2) NULL,
                            TeacherNotes NVARCHAR(MAX) NULL,
                            SchoolId UNIQUEIDENTIFIER NULL,
                            SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                            UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
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
                        ALTER TABLE StudentPerformanceEntries ADD SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
                    IF OBJECT_ID(N'StudentPerformanceEntries', N'U') IS NOT NULL AND COL_LENGTH('StudentPerformanceEntries', 'UpdatedAt') IS NULL
                        ALTER TABLE StudentPerformanceEntries ADD UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
                ", conn);
                await cmd4.ExecuteNonQueryAsync();

                // 5. Weekly Output Reports
                var cmd5 = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WeeklyOutputReports' and xtype='U')
                    BEGIN
                        CREATE TABLE WeeklyOutputReports (
                            OutputReportId INT IDENTITY(1,1) PRIMARY KEY,
                            ClassId NVARCHAR(50) NOT NULL,
                            SubjectId NVARCHAR(50) NOT NULL,
                            TeacherId INT NOT NULL,
                            AcademicYear NVARCHAR(20) NOT NULL,
                            Term NVARCHAR(20) NOT NULL,
                            WeekNumber INT NOT NULL,
                            WeekStartDate DATETIME NOT NULL,
                            WeekEndDate DATETIME NOT NULL,
                            AverageCompletionRate DECIMAL(5,2) NOT NULL DEFAULT 0,
                            StudentsOnTrack INT NOT NULL DEFAULT 0,
                            StudentsNeedingSupport INT NOT NULL DEFAULT 0,
                            Notes NVARCHAR(MAX) NULL,
                            WorkflowId INT NOT NULL FOREIGN KEY REFERENCES ApprovalWorkflows(WorkflowId)
                        );
                    END
                ", conn);
                await cmd5.ExecuteNonQueryAsync();

                // 6. Student Work Tracking
                var cmd6 = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentWorkTrackings' and xtype='U')
                    BEGIN
                        CREATE TABLE StudentWorkTrackings (
                            TrackingId INT IDENTITY(1,1) PRIMARY KEY,
                            OutputReportId INT NOT NULL FOREIGN KEY REFERENCES WeeklyOutputReports(OutputReportId) ON DELETE CASCADE,
                            StudentId NVARCHAR(50) NOT NULL,
                            ExercisesCompleted INT NOT NULL DEFAULT 0,
                            HomeworksCompleted INT NOT NULL DEFAULT 0,
                            CompletionPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
                            Status NVARCHAR(50) NOT NULL DEFAULT 'OnTrack'
                        );
                    END
                ", conn);
                await cmd6.ExecuteNonQueryAsync();
            }
        }
    }
}
