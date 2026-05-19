USE [Neat_Academy];
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.payment_record', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.payment_record
    (
        PaymentRecordID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_payment_record PRIMARY KEY,
        StudentID int NOT NULL,
        classID varchar(20) NOT NULL,
        FeeName varchar(100) NULL,
        Balance decimal(18,2) NOT NULL CONSTRAINT DF_payment_record_Balance DEFAULT (0),
        student_name varchar(120) NOT NULL,
        Amount_paid decimal(18,2) NOT NULL CONSTRAINT DF_payment_record_AmountPaid DEFAULT (0),
        [Date] date NOT NULL CONSTRAINT DF_payment_record_Date DEFAULT (CONVERT(date, GETDATE())),
        tm time(0) NOT NULL CONSTRAINT DF_payment_record_tm DEFAULT (CONVERT(time(0), GETDATE())),
        payment_mode varchar(50) NULL,
        Bursor_name varchar(100) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.examss', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.examss
    (
        ExamID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_examss PRIMARY KEY,
        std_id int NOT NULL,
        std_name varchar(120) NOT NULL,
        std_class varchar(20) NOT NULL,
        [subject] varchar(80) NOT NULL,
        term varchar(50) NOT NULL,
        [year] varchar(20) NOT NULL,
        cat1 decimal(9,2) NOT NULL CONSTRAINT DF_examss_cat1 DEFAULT (0),
        cat2 decimal(9,2) NOT NULL CONSTRAINT DF_examss_cat2 DEFAULT (0),
        cat3 decimal(9,2) NOT NULL CONSTRAINT DF_examss_cat3 DEFAULT (0),
        tl_cat decimal(9,2) NOT NULL CONSTRAINT DF_examss_tlcat DEFAULT (0),
        exam_score decimal(9,2) NOT NULL CONSTRAINT DF_examss_exam DEFAULT (0),
        gt decimal(9,2) NOT NULL CONSTRAINT DF_examss_gt DEFAULT (0),
        grade varchar(10) NULL,
        remark varchar(50) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.emp_leave', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.emp_leave
    (
        LeaveID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_emp_leave PRIMARY KEY,
        employmentID int NOT NULL,
        [name] varchar(120) NOT NULL,
        department varchar(100) NOT NULL,
        position varchar(100) NOT NULL,
        Leave_op varchar(50) NOT NULL,
        Reasons varchar(100) NOT NULL,
        Start_Date date NOT NULL,
        End_Date date NOT NULL,
        [status] varchar(30) NOT NULL CONSTRAINT DF_emp_leave_status DEFAULT ('PENDING')
    );
END;
GO

IF OBJECT_ID(N'dbo.Rolled_Out_Students', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rolled_Out_Students
    (
        StudentID int NOT NULL CONSTRAINT PK_Rolled_Out_Students PRIMARY KEY,
        FirstName varchar(50) NOT NULL,
        LastName varchar(50) NOT NULL,
        DOB date NOT NULL,
        Gender varchar(10) NOT NULL,
        Email varchar(150) NULL,
        ClassID varchar(20) NOT NULL,
        HomeTown varchar(70) NOT NULL,
        Residence varchar(70) NOT NULL,
        Allegies varchar(100) NOT NULL,
        EmergencyConatct varchar(50) NOT NULL,
        GuidanceName varchar(100) NOT NULL,
        GuidianceEmail varchar(170) NOT NULL,
        Guidiance_Location varchar(70) NOT NULL,
        admission_date date NOT NULL,
        [date] datetime NULL,
        Std_pic varbinary(max) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Rolled_Out_Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rolled_Out_Employees
    (
        employmentID int NOT NULL CONSTRAINT PK_Rolled_Out_Employees PRIMARY KEY,
        fullName varchar(100) NOT NULL,
        gender varchar(20) NOT NULL,
        dOB date NOT NULL,
        conatct varchar(50) NOT NULL,
        department varchar(100) NOT NULL,
        position varchar(100) NOT NULL,
        homeTown varchar(100) NOT NULL,
        residence varchar(100) NOT NULL,
        date_of_Emplyment date NOT NULL,
        employment_Mode varchar(50) NOT NULL,
        employment_Status varchar(50) NOT NULL,
        emergency_Contact_Person varchar(100) NOT NULL,
        emergency_contact varchar(50) NOT NULL,
        Employees_Reviews varchar(50) NOT NULL,
        salary money NOT NULL,
        pic varbinary(max) NULL,
        [DATE] datetime NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Students_ClassID' AND object_id = OBJECT_ID(N'dbo.Students'))
    CREATE INDEX IX_Students_ClassID ON dbo.Students(ClassID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employee_Department' AND object_id = OBJECT_ID(N'dbo.Employee'))
    CREATE INDEX IX_Employee_Department ON dbo.Employee(department);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fees_StudentID_ClassID' AND object_id = OBJECT_ID(N'dbo.fees'))
    CREATE INDEX IX_fees_StudentID_ClassID ON dbo.fees(StudentID, ClassID) INCLUDE (Amount, FeeName);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payment_record_Student_Date' AND object_id = OBJECT_ID(N'dbo.payment_record'))
    CREATE INDEX IX_payment_record_Student_Date ON dbo.payment_record(StudentID, [Date] DESC, tm DESC) INCLUDE (student_name, classID, Balance);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payment_record_Balance' AND object_id = OBJECT_ID(N'dbo.payment_record'))
    CREATE INDEX IX_payment_record_Balance ON dbo.payment_record(Balance) INCLUDE (StudentID, student_name);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Student_Subject_Term' AND object_id = OBJECT_ID(N'dbo.examss'))
    CREATE UNIQUE INDEX IX_examss_Student_Subject_Term ON dbo.examss(std_id, [subject], term);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Class_Term_Year_Subject' AND object_id = OBJECT_ID(N'dbo.examss'))
    CREATE INDEX IX_examss_Class_Term_Year_Subject ON dbo.examss(std_class, term, [year], [subject]) INCLUDE (std_name, gt, grade, remark);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_emp_leave_Employment_Status' AND object_id = OBJECT_ID(N'dbo.emp_leave'))
    CREATE INDEX IX_emp_leave_Employment_Status ON dbo.emp_leave(employmentID, [status]) INCLUDE (Start_Date, End_Date);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Rolled_Out_Students_ClassID' AND object_id = OBJECT_ID(N'dbo.Rolled_Out_Students'))
    CREATE INDEX IX_Rolled_Out_Students_ClassID ON dbo.Rolled_Out_Students(ClassID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Rolled_Out_Employees_Department' AND object_id = OBJECT_ID(N'dbo.Rolled_Out_Employees'))
    CREATE INDEX IX_Rolled_Out_Employees_Department ON dbo.Rolled_Out_Employees(department);
GO
