using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(AppConfig.ConnectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
-- 1. Ensure Employee table has 'email' column
IF OBJECT_ID(N'Employee', N'U') IS NOT NULL AND COL_LENGTH('Employee', 'email') IS NULL
BEGIN
    ALTER TABLE [Employee] ADD [email] VARCHAR(100) NULL;
END

-- 1b. Standardize Students.ClassID for performance indexing
IF OBJECT_ID(N'Students', N'U') IS NOT NULL
BEGIN
    -- Only alter if it's currently a 'MAX' or large type that prevents indexing
    IF EXISTS (SELECT 1 FROM sys.columns c 
               JOIN sys.types t ON c.user_type_id = t.user_type_id
               WHERE c.object_id = OBJECT_ID(N'Students') 
               AND c.name = 'ClassID' 
               AND (t.name = 'text' OR (t.name = 'nvarchar' AND c.max_length = -1)))
    BEGIN
        ALTER TABLE [Students] ALTER COLUMN [ClassID] NVARCHAR(50);
    END
END

-- 2. Ensure Classes table exists
IF OBJECT_ID(N'Classes', N'U') IS NULL
BEGIN
    CREATE TABLE Classes (
        ClassName varchar(50) NOT NULL PRIMARY KEY,
        TuitionFee money NOT NULL DEFAULT 0,
        PromotionLevel int NOT NULL DEFAULT 1
    );
    INSERT INTO Classes (ClassName, TuitionFee, PromotionLevel) VALUES ('CRECHE', 2000, 1);
    INSERT INTO Classes (ClassName, TuitionFee, PromotionLevel) VALUES ('NURSERY 1', 3450, 2);
    INSERT INTO Classes (ClassName, TuitionFee, PromotionLevel) VALUES ('BASIC 1', 2423, 5);
END

-- 3. Ensure ClassAssignments table exists
IF OBJECT_ID(N'ClassAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE ClassAssignments (
        ClassName       VARCHAR(50)  NOT NULL PRIMARY KEY,
        ClassTeacherID  INT          NULL,
        AssignedDate    DATE         NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        Notes           VARCHAR(200) NULL
    );
END

-- 4. Ensure Attendance table exists
IF OBJECT_ID(N'Attendance', N'U') IS NULL
BEGIN
    CREATE TABLE Attendance (
        AttendanceID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReferenceID varchar(50) NOT NULL,
        ReferenceType varchar(20) NOT NULL,
        FullName varchar(120) NOT NULL,
        [Date] date NOT NULL,
        [Status] varchar(20) NOT NULL,
        Remarks varchar(200) NULL,
        [CreatedDate] datetime NOT NULL DEFAULT GETDATE()
    );
END

-- 5. Ensure Expenses table exists
IF OBJECT_ID(N'Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE Expenses (
        ID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Expenses_name varchar(100) NOT NULL,
        Purpose varchar(100) NOT NULL,
        Description varchar(200) NULL,
        Date_Time datetime NOT NULL DEFAULT GETDATE(),
        Amount varchar(50) NOT NULL,
        Payee varchar(100) NULL,
        payer varchar(100) NULL
    );
END

-- 6. Ensure ExpenseCategories table exists
IF OBJECT_ID(N'ExpenseCategories', N'U') IS NULL
BEGIN
    CREATE TABLE ExpenseCategories (Name NVARCHAR(60) NOT NULL PRIMARY KEY);
    INSERT INTO ExpenseCategories (Name) VALUES ('Salaries'), ('Utilities'), ('Supplies'), ('Maintenance'), ('Transport'), ('Rent'), ('Repairs'), ('Miscellaneous');
END

-- 7. Ensure Archive tables exist
IF OBJECT_ID(N'Rolled_Out_Students', N'U') IS NULL
BEGIN
    CREATE TABLE Rolled_Out_Students (
        StudentID varchar(50) NOT NULL PRIMARY KEY,
        FirstName varchar(50), LastName varchar(50), DOB date, Gender varchar(20), 
        Email varchar(100), ClassID varchar(50), HomeTown varchar(100), Residence varchar(100), 
        Allegies varchar(200), EmergencyConatct varchar(50), GuidanceName varchar(100), 
        GuidianceEmail varchar(100), Guidiance_Location varchar(100), admission_date date, 
        [date] date NOT NULL DEFAULT GETDATE(), Std_pic varbinary(max)
    );
END

IF OBJECT_ID(N'Rolled_Out_Employees', N'U') IS NULL
BEGIN
    CREATE TABLE Rolled_Out_Employees (
        employmentID int NOT NULL PRIMARY KEY,
        fullName varchar(120), gender varchar(20), DOB date, 
        homeTown varchar(100), residence varchar(100), 
        position varchar(100), department varchar(100), 
        mobile varchar(50), email varchar(100), 
        [date] date NOT NULL DEFAULT GETDATE()
    );
END

-- 8. Academic Calendar tables
IF OBJECT_ID(N'AcademicCalendar', N'U') IS NULL
BEGIN
    CREATE TABLE AcademicCalendar (
        TermID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TermName varchar(50) NOT NULL,
        StartDate date NOT NULL,
        EndDate date NOT NULL,
        IsActive bit NOT NULL DEFAULT 0
    );
END

IF OBJECT_ID(N'SchoolHolidays', N'U') IS NULL
BEGIN
    CREATE TABLE SchoolHolidays (
        EventID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EventName varchar(100) NOT NULL,
        EventDate date NOT NULL,
        EventType varchar(20) NOT NULL DEFAULT 'Holiday' -- 'Holiday', 'Event', 'Exam'
    );
END

-- 9. Timetable Infrastructure
IF OBJECT_ID(N'TimePeriods', N'U') IS NULL
BEGIN
    CREATE TABLE TimePeriods (
        PeriodID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PeriodName varchar(50) NOT NULL,
        StartTime time NOT NULL,
        EndTime time NOT NULL,
        IsBreak bit NOT NULL DEFAULT 0,
        SortOrder int NOT NULL DEFAULT 1
    );
    -- Seed default periods
    INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder) VALUES ('Period 1', '08:00:00', '08:40:00', 0, 1);
    INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder) VALUES ('Period 2', '08:40:00', '09:20:00', 0, 2);
    INSERT INTO TimePeriods (PeriodName, StartTime, EndTime, IsBreak, SortOrder) VALUES ('Break', '09:20:00', '09:50:00', 1, 3);
END

IF OBJECT_ID(N'SubjectAllocations', N'U') IS NULL
BEGIN
    CREATE TABLE SubjectAllocations (
        AllocationID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClassID varchar(50) NOT NULL,
        SubjectName varchar(100) NOT NULL,
        TeacherID int NULL,
        PeriodsPerWeek int NOT NULL DEFAULT 1
    );
END

IF OBJECT_ID(N'TimetableEntries', N'U') IS NULL
BEGIN
    CREATE TABLE TimetableEntries (
        EntryID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClassID varchar(50) NOT NULL,
        PeriodID int NOT NULL,
        DayOfWeek int NOT NULL, -- 1=Mon, 2=Tue...
        SubjectName varchar(100) NOT NULL,
        TeacherID int NULL
    );
END

-- 10. Scholarship & Discount System
IF OBJECT_ID(N'ScholarshipCategories', N'U') IS NULL
BEGIN
    CREATE TABLE ScholarshipCategories (
        CategoryID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] varchar(100) NOT NULL,
        DiscountType varchar(20) NOT NULL, -- 'PERCENTAGE', 'FIXED'
        DiscountValue decimal(18,2) NOT NULL DEFAULT 0,
        IsActive bit NOT NULL DEFAULT 1
    );
END

IF OBJECT_ID(N'StudentScholarships', N'U') IS NULL
BEGIN
    CREATE TABLE StudentScholarships (
        AssignmentID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StudentID varchar(50) NOT NULL,
        CategoryID int NOT NULL,
        AssignedDate date NOT NULL DEFAULT GETDATE(),
        ApprovalStatus varchar(20) NOT NULL DEFAULT 'Pending'
    );
END
ELSE
BEGIN
    -- Add ApprovalStatus column if it doesn't exist (for seamless updates)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'StudentScholarships') AND name = 'ApprovalStatus')
    BEGIN
        ALTER TABLE StudentScholarships ADD ApprovalStatus varchar(20) NOT NULL DEFAULT 'Pending';
    END
END";
                    using (var cmd = new OleDbCommand(sql, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Database initialization failed", ex);
            }
        }
    }
}
