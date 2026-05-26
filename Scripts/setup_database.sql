-- ============================================================
-- FULL DATABASE SETUP — Kingdom Preparatory School
-- Run against (localdb)\KPS  (or any SQL Server instance)
-- ============================================================

-- 1. CREATE DATABASE
IF DB_ID('Neat_Academy') IS NULL
    CREATE DATABASE [Neat_Academy];
GO

USE [Neat_Academy];
GO
SET NOCOUNT ON;
GO

-- ============================================================
-- 2. CREATE TABLES (all idempotent)
-- ============================================================

IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employee (
        employmentID           int           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
        fullName               varchar(100)  NOT NULL,
        gender                 varchar(20)   NOT NULL,
        dOB                    date          NOT NULL,
        conatct                varchar(50)   NOT NULL,
        department             varchar(100)  NOT NULL,
        position               varchar(100)  NOT NULL,
        homeTown               varchar(100)  NOT NULL,
        residence              varchar(100)  NOT NULL,
        date_of_Emplyment      date          NOT NULL,
        employment_Mode        varchar(50)   NOT NULL,
        employment_Status      varchar(50)   NOT NULL,
        emergency_Contact_Person varchar(100) NOT NULL,
        emergency_contact      varchar(50)   NOT NULL,
        Employees_Reviews      varchar(50)   NOT NULL,
        salary                 money         NOT NULL,
        pic                    varbinary(max) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Students', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Students (
        StudentID              int           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Students PRIMARY KEY,
        FirstName              varchar(50)   NOT NULL,
        LastName               varchar(50)   NOT NULL,
        DOB                    date          NOT NULL,
        Gender                 varchar(10)   NOT NULL,
        Email                  varchar(150)  NULL,
        ClassID                varchar(20)   NOT NULL,
        HomeTown               varchar(70)   NOT NULL,
        Residence              varchar(70)   NOT NULL,
        Allegies               varchar(100)  NOT NULL CONSTRAINT DF_Students_Allegies DEFAULT ('None'),
        EmergencyConatct       varchar(50)   NOT NULL,
        GuidanceName           varchar(100)  NOT NULL,
        GuidianceEmail         varchar(170)  NOT NULL,
        Guidiance_Location     varchar(70)   NOT NULL,
        admission_date         date          NOT NULL,
        Std_pic                varbinary(max) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.fees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.fees (
        FeeID      int          IDENTITY(1,1) NOT NULL CONSTRAINT PK_fees PRIMARY KEY,
        StudentID  int          NOT NULL,
        ClassID    varchar(20)  NOT NULL,
        FeeName    varchar(100) NOT NULL,
        Amount     decimal(18,2) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.payment_record', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.payment_record (
        PaymentRecordID int          IDENTITY(1,1) NOT NULL CONSTRAINT PK_payment_record PRIMARY KEY,
        StudentID       int          NOT NULL,
        classID         varchar(20)  NOT NULL,
        FeeName         varchar(100) NULL,          -- kept for legacy compatibility, not used by app
        Balance         decimal(18,2) NOT NULL CONSTRAINT DF_payment_record_Balance DEFAULT (0),
        student_name    varchar(120) NOT NULL,
        Amount_paid     decimal(18,2) NOT NULL CONSTRAINT DF_payment_record_AmountPaid DEFAULT (0),
        [Date]          date         NOT NULL CONSTRAINT DF_payment_record_Date DEFAULT (CONVERT(date, GETDATE())),
        tm              time(0)      NOT NULL CONSTRAINT DF_payment_record_tm DEFAULT (CONVERT(time(0), GETDATE())),
        payment_mode    varchar(50)  NULL,
        Bursor_name     varchar(100) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.examss', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.examss (
        ExamID      int           IDENTITY(1,1) NOT NULL CONSTRAINT PK_examss PRIMARY KEY,
        std_id      int           NOT NULL,
        std_name    varchar(120)  NOT NULL,
        std_class   varchar(20)   NOT NULL,
        [subject]   varchar(80)   NOT NULL,
        term        varchar(50)   NOT NULL,
        [year]      varchar(20)   NOT NULL,
        cat1        decimal(9,2)  NOT NULL CONSTRAINT DF_examss_cat1 DEFAULT (0),
        cat2        decimal(9,2)  NOT NULL CONSTRAINT DF_examss_cat2 DEFAULT (0),
        cat3        decimal(9,2)  NOT NULL CONSTRAINT DF_examss_cat3 DEFAULT (0),
        tl_cat      decimal(9,2)  NOT NULL CONSTRAINT DF_examss_tlcat DEFAULT (0),
        exam_score  decimal(9,2)  NOT NULL CONSTRAINT DF_examss_exam DEFAULT (0),
        gt          decimal(9,2)  NOT NULL CONSTRAINT DF_examss_gt DEFAULT (0),
        grade       varchar(10)   NULL,
        remark      varchar(50)   NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.emp_leave', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.emp_leave (
        LeaveID      int         IDENTITY(1,1) NOT NULL CONSTRAINT PK_emp_leave PRIMARY KEY,
        employmentID int         NOT NULL,
        [name]       varchar(120) NOT NULL,
        department   varchar(100) NOT NULL,
        position     varchar(100) NOT NULL,
        Leave_op     varchar(50)  NOT NULL,
        Reasons      varchar(100) NOT NULL,
        Start_Date   date         NOT NULL,
        End_Date     date         NOT NULL,
        [status]     varchar(30)  NOT NULL CONSTRAINT DF_emp_leave_status DEFAULT ('PENDING')
    );
END;
GO

IF OBJECT_ID(N'dbo.Attendance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Attendance (
        AttendanceID  int         IDENTITY(1,1) NOT NULL CONSTRAINT PK_Attendance PRIMARY KEY,
        ReferenceID   int         NOT NULL,
        ReferenceType varchar(20) NOT NULL,
        FullName      varchar(120) NOT NULL,
        [Date]        date         NOT NULL,
        [Status]      varchar(20)  NOT NULL,
        Remarks       varchar(200) NULL,
        CreatedDate   datetime     NOT NULL CONSTRAINT DF_Attendance_CreatedDate DEFAULT (GETDATE())
    );
END;
GO

IF OBJECT_ID(N'dbo.StudentTermRemarks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StudentTermRemarks (
        ID                    int           IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentTermRemarks PRIMARY KEY,
        StudentID             varchar(50)   NOT NULL,
        Term                  varchar(50)   NOT NULL,
        [Year]                varchar(20)   NOT NULL,
        ClassTeacherRemarks   varchar(max)  NULL,
        HeadTeacherRemarks    varchar(max)  NULL,
        Attitude              varchar(500)  NULL,
        Interest              varchar(500)  NULL,
        Conduct               varchar(500)  NULL,
        CreatedDate           datetime      NOT NULL CONSTRAINT DF_StudentTermRemarks_CreatedDate DEFAULT (GETDATE()),
        ModifiedDate          datetime      NULL,
        CONSTRAINT UC_StudentTermRemarks UNIQUE(StudentID, Term, [Year])
    );
END;
GO

IF OBJECT_ID(N'dbo.Rolled_Out_Students', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rolled_Out_Students (
        StudentID          int          NOT NULL CONSTRAINT PK_Rolled_Out_Students PRIMARY KEY,
        FirstName          varchar(50)  NOT NULL,
        LastName           varchar(50)  NOT NULL,
        DOB                date         NOT NULL,
        Gender             varchar(10)  NOT NULL,
        Email              varchar(150) NULL,
        ClassID            varchar(20)  NOT NULL,
        HomeTown           varchar(70)  NOT NULL,
        Residence          varchar(70)  NOT NULL,
        Allegies           varchar(100) NOT NULL,
        EmergencyConatct   varchar(50)  NOT NULL,
        GuidanceName       varchar(100) NOT NULL,
        GuidianceEmail     varchar(170) NOT NULL,
        Guidiance_Location varchar(70)  NOT NULL,
        admission_date     date         NOT NULL,
        [date]             datetime     NULL,
        Std_pic            varbinary(max) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Rolled_Out_Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rolled_Out_Employees (
        employmentID             int          NOT NULL CONSTRAINT PK_Rolled_Out_Employees PRIMARY KEY,
        fullName                 varchar(100) NOT NULL,
        gender                   varchar(20)  NOT NULL,
        dOB                      date         NOT NULL,
        conatct                  varchar(50)  NOT NULL,
        department               varchar(100) NOT NULL,
        position                 varchar(100) NOT NULL,
        homeTown                 varchar(100) NOT NULL,
        residence                varchar(100) NOT NULL,
        date_of_Emplyment        date         NOT NULL,
        employment_Mode          varchar(50)  NOT NULL,
        employment_Status        varchar(50)  NOT NULL,
        emergency_Contact_Person varchar(100) NOT NULL,
        emergency_contact        varchar(50)  NOT NULL,
        Employees_Reviews        varchar(50)  NOT NULL,
        salary                   money        NOT NULL,
        pic                      varbinary(max) NULL,
        [DATE]                   datetime     NULL
    );
END;
GO

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Attendance_RefDate')
    CREATE INDEX IX_Attendance_RefDate ON dbo.Attendance(ReferenceID, ReferenceType, [Date]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Students_ClassID')
    CREATE INDEX IX_Students_ClassID ON dbo.Students(ClassID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Employee_Department')
    CREATE INDEX IX_Employee_Department ON dbo.Employee(department);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_fees_StudentID_ClassID')
    CREATE INDEX IX_fees_StudentID_ClassID ON dbo.fees(StudentID, ClassID) INCLUDE (Amount, FeeName);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_payment_record_Student_Date')
    CREATE INDEX IX_payment_record_Student_Date ON dbo.payment_record(StudentID, [Date] DESC, tm DESC) INCLUDE (student_name, classID, Balance);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_payment_record_Balance')
    CREATE INDEX IX_payment_record_Balance ON dbo.payment_record(Balance) INCLUDE (StudentID, student_name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_examss_Student_Subject_Term')
    CREATE UNIQUE INDEX IX_examss_Student_Subject_Term ON dbo.examss(std_id, [subject], term);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_examss_Class_Term_Year_Subject')
    CREATE INDEX IX_examss_Class_Term_Year_Subject ON dbo.examss(std_class, term, [year], [subject]) INCLUDE (std_name, gt, grade, remark);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_emp_leave_Employment_Status')
    CREATE INDEX IX_emp_leave_Employment_Status ON dbo.emp_leave(employmentID, [status]) INCLUDE (Start_Date, End_Date);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_StudentTermRemarks_StudentID')
    CREATE INDEX IX_StudentTermRemarks_StudentID ON dbo.StudentTermRemarks(StudentID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_StudentTermRemarks_TermYear')
    CREATE INDEX IX_StudentTermRemarks_TermYear ON dbo.StudentTermRemarks(Term, [Year]);
GO


-- ============================================================
-- 3. DEMO DATA — 8 staff + 12 students (safe re-run)
-- ============================================================

-- Cleanup demo rows
DELETE FROM StudentTermRemarks WHERE StudentID IN ('9001','9002','9003','9004','9005','9006','9007','9008','9009','9010','9011','9012');
DELETE FROM examss             WHERE std_id BETWEEN 9001 AND 9012;
DELETE FROM Attendance         WHERE (ReferenceType='STUDENT' AND ReferenceID BETWEEN 9001 AND 9012)
                                  OR (ReferenceType='STAFF'   AND ReferenceID BETWEEN 9001 AND 9008);
DELETE FROM payment_record     WHERE StudentID BETWEEN 9001 AND 9012;
DELETE FROM fees               WHERE StudentID BETWEEN 9001 AND 9012;
DELETE FROM emp_leave          WHERE employmentID BETWEEN 9001 AND 9008;
DELETE FROM Students           WHERE StudentID BETWEEN 9001 AND 9012;
DELETE FROM Employee           WHERE employmentID BETWEEN 9001 AND 9008;
GO

-- Employee
SET IDENTITY_INSERT Employee ON;
INSERT INTO Employee (employmentID,fullName,gender,dOB,conatct,department,position,homeTown,residence,date_of_Emplyment,employment_Mode,employment_Status,emergency_Contact_Person,emergency_contact,Employees_Reviews,salary,pic) VALUES
(9001,'ABENA MENSAH','FEMALE','1985-03-12','0244000001','ADMINISTRATION','HEAD','KUMASI','EAST LEGON','2018-09-01','FULL-TIME','ACTIVE','KOFI MENSAH','0244111111','A: EXCELLENT',3500,0x),
(9002,'KWAME ASARE','MALE','1990-07-22','0244000002','JHS (JUNIOR HIGH SCHOOL)','DEPUTY','TAKORADI','ADENTA','2019-09-01','FULL-TIME','ACTIVE','AKOSUA ASARE','0244222222','B: GOOD',2800,0x),
(9003,'AKOSUA OWUSU','FEMALE','1992-11-05','0244000003','UPPER PRIMARY','NON-POSITIONAL','CAPE COAST','MADINA','2020-09-01','FULL-TIME','ACTIVE','YAW OWUSU','0244333333','B: GOOD',2200,0x),
(9004,'YAW BOATENG','MALE','1988-01-18','0244000004','LOWER PRIMARY','NON-POSITIONAL','KOFORIDUA','TESHIE','2017-09-01','FULL-TIME','ACTIVE','AMA BOATENG','0244444444','A: EXCELLENT',2400,0x),
(9005,'AMA DARKO','FEMALE','1995-05-30','0244000005','KINDERGARTEN','NON-POSITIONAL','TAMALE','OSU','2021-09-01','FULL-TIME','ACTIVE','KOJO DARKO','0244555555','C: SATISFACTORY',2000,0x),
(9006,'KOFI NKRUMAH','MALE','1993-09-08','0244000006','NURSERY','SECRETARY','HO','ACHIMOTA','2022-09-01','FULL-TIME','ACTIVE','EFUA NKRUMAH','0244666666','B: GOOD',2100,0x),
(9007,'EFUA ADDO','FEMALE','1987-12-15','0244000007','ADMINISTRATION','NON-POSITIONAL','SUNYANI','LASHIBI','2016-09-01','PART-TIME','ACTIVE','NANA ADDO','0244777777','B: GOOD',1800,0x),
(9008,'KOJO ANTWI','MALE','1991-04-25','0244000008','SANITATION & CLEANING','NON-POSITIONAL','ACCRA','NUNGUA','2019-09-01','FULL-TIME','ACTIVE','AKUA ANTWI','0244888888','A: EXCELLENT',1500,0x);
SET IDENTITY_INSERT Employee OFF;
GO

-- Students
SET IDENTITY_INSERT Students ON;
INSERT INTO Students (StudentID,FirstName,LastName,DOB,Gender,Email,ClassID,HomeTown,Residence,Allegies,EmergencyConatct,GuidanceName,GuidianceEmail,Guidiance_Location,admission_date,Std_pic) VALUES
(9001,'KOFI','AGYEMAN','2019-04-12','MALE','kofi.agyeman@school.demo','BASIC 1','KUMASI','EAST LEGON','None','0244900001','SAMUEL AGYEMAN','samuel.agyeman@example.com','EAST LEGON','2024-09-01',NULL),
(9002,'AMA','BOATENG','2019-08-25','FEMALE','ama.boateng@school.demo','BASIC 1','ACCRA','MADINA','Peanuts','0244900002','GIFTY BOATENG','gifty.boateng@example.com','MADINA','2024-09-01',NULL),
(9003,'YAW','OPOKU','2019-12-03','MALE','yaw.opoku@school.demo','BASIC 1','TEMA','TESHIE','None','0244900003','JOHN OPOKU','john.opoku@example.com','TESHIE','2024-09-01',NULL),
(9004,'ABENA','MENSAH','2017-06-15','FEMALE','abena.mensah@school.demo','BASIC 3','KUMASI','ADENTA','Dust','0244900004','MARY MENSAH','mary.mensah@example.com','ADENTA','2022-09-01',NULL),
(9005,'KWAME','OSEI','2017-09-20','MALE','kwame.osei@school.demo','BASIC 3','TAKORADI','LASHIBI','None','0244900005','PETER OSEI','peter.osei@example.com','LASHIBI','2022-09-01',NULL),
(9006,'EFUA','DARKO','2017-11-08','FEMALE','efua.darko@school.demo','BASIC 3','HO','NUNGUA','Lactose','0244900006','RUTH DARKO','ruth.darko@example.com','NUNGUA','2022-09-01',NULL),
(9007,'NANA','ASANTE','2015-02-19','MALE','nana.asante@school.demo','BASIC 5','KOFORIDUA','OSU','None','0244900007','DANIEL ASANTE','daniel.asante@example.com','OSU','2020-09-01',NULL),
(9008,'AKUA','ADJEI','2015-05-30','FEMALE','akua.adjei@school.demo','BASIC 5','CAPE COAST','SPINTEX','Eggs','0244900008','VIDA ADJEI','vida.adjei@example.com','SPINTEX','2020-09-01',NULL),
(9009,'KOJO','OWUSU','2015-07-14','MALE','kojo.owusu@school.demo','BASIC 5','SUNYANI','ACHIMOTA','None','0244900009','KWESI OWUSU','kwesi.owusu@example.com','ACHIMOTA','2020-09-01',NULL),
(9010,'ESI','AFRIYIE','2013-03-22','FEMALE','esi.afriyie@school.demo','BASIC 7','TAMALE','DOME','None','0244900010','ANITA AFRIYIE','anita.afriyie@example.com','DOME','2018-09-01',NULL),
(9011,'KWESI','BOAKYE','2013-10-05','MALE','kwesi.boakye@school.demo','BASIC 7','ACCRA','KASOA','Asthma','0244900011','EMMA BOAKYE','emma.boakye@example.com','KASOA','2018-09-01',NULL),
(9012,'NHYIRA','GYAMFI','2013-12-11','FEMALE','nhyira.gyamfi@school.demo','BASIC 7','KUMASI','SAKUMONO','None','0244900012','JOSEPH GYAMFI','joseph.gyamfi@example.com','SAKUMONO','2018-09-01',NULL);
SET IDENTITY_INSERT Students OFF;
GO

-- fees
INSERT INTO fees (StudentID,ClassID,FeeName,Amount) VALUES
(9001,'BASIC 1','Tuition Fee',2423.00),(9002,'BASIC 1','Tuition Fee',2423.00),(9003,'BASIC 1','Tuition Fee',2423.00),
(9004,'BASIC 3','Tuition Fee',2423.00),(9005,'BASIC 3','Tuition Fee',2423.00),(9006,'BASIC 3','Tuition Fee',2423.00),
(9007,'BASIC 5','Tuition Fee',2600.00),(9008,'BASIC 5','Tuition Fee',2600.00),(9009,'BASIC 5','Tuition Fee',2600.00),
(9010,'BASIC 7','Tuition Fee',3000.00),(9011,'BASIC 7','Tuition Fee',3000.00),(9012,'BASIC 7','Tuition Fee',3000.00);
GO

-- payment_record (no FeeName — column is nullable/legacy)
INSERT INTO payment_record (StudentID,classID,Balance,student_name,Amount_paid,[Date],tm,payment_mode,Bursor_name) VALUES
(9001,'BASIC 1',2423.00,'KOFI AGYEMAN',0.00,'2026-01-15','08:30:00','ENROLLMENT','SYSTEM'),
(9001,'BASIC 1',1223.00,'KOFI AGYEMAN',1200.00,'2026-02-10','09:15:00','CASH','BURSAR JANE'),
(9001,'BASIC 1',0.00,'KOFI AGYEMAN',1223.00,'2026-03-08','10:20:00','MOMO','BURSAR JANE'),
(9002,'BASIC 1',2423.00,'AMA BOATENG',0.00,'2026-01-15','08:31:00','ENROLLMENT','SYSTEM'),
(9002,'BASIC 1',923.00,'AMA BOATENG',1500.00,'2026-02-12','11:00:00','CASH','BURSAR JANE'),
(9003,'BASIC 1',2423.00,'YAW OPOKU',0.00,'2026-01-15','08:32:00','ENROLLMENT','SYSTEM'),
(9003,'BASIC 1',1923.00,'YAW OPOKU',500.00,'2026-03-05','14:45:00','CHEQUE','BURSAR JANE'),
(9004,'BASIC 3',2423.00,'ABENA MENSAH',0.00,'2026-01-15','08:33:00','ENROLLMENT','SYSTEM'),
(9004,'BASIC 3',0.00,'ABENA MENSAH',2423.00,'2026-02-20','09:00:00','BANK','BURSAR JANE'),
(9005,'BASIC 3',2423.00,'KWAME OSEI',0.00,'2026-01-15','08:34:00','ENROLLMENT','SYSTEM'),
(9006,'BASIC 3',2423.00,'EFUA DARKO',0.00,'2026-01-15','08:35:00','ENROLLMENT','SYSTEM'),
(9006,'BASIC 3',1623.00,'EFUA DARKO',800.00,'2026-03-12','10:30:00','MOMO','BURSAR JANE'),
(9007,'BASIC 5',2600.00,'NANA ASANTE',0.00,'2026-01-16','08:36:00','ENROLLMENT','SYSTEM'),
(9007,'BASIC 5',0.00,'NANA ASANTE',2600.00,'2026-01-20','09:30:00','BANK','BURSAR JANE'),
(9008,'BASIC 5',2600.00,'AKUA ADJEI',0.00,'2026-01-16','08:37:00','ENROLLMENT','SYSTEM'),
(9008,'BASIC 5',1600.00,'AKUA ADJEI',1000.00,'2026-02-18','11:15:00','CASH','BURSAR JANE'),
(9009,'BASIC 5',2600.00,'KOJO OWUSU',0.00,'2026-01-16','08:38:00','ENROLLMENT','SYSTEM'),
(9010,'BASIC 7',3000.00,'ESI AFRIYIE',0.00,'2026-01-17','08:39:00','ENROLLMENT','SYSTEM'),
(9010,'BASIC 7',1000.00,'ESI AFRIYIE',2000.00,'2026-02-05','10:00:00','BANK','BURSAR JANE'),
(9010,'BASIC 7',0.00,'ESI AFRIYIE',1000.00,'2026-04-15','13:20:00','CHEQUE','BURSAR JANE'),
(9011,'BASIC 7',3000.00,'KWESI BOAKYE',0.00,'2026-01-17','08:40:00','ENROLLMENT','SYSTEM'),
(9011,'BASIC 7',1500.00,'KWESI BOAKYE',1500.00,'2026-03-22','11:45:00','MOMO','BURSAR JANE'),
(9012,'BASIC 7',3000.00,'NHYIRA GYAMFI',0.00,'2026-01-17','08:41:00','ENROLLMENT','SYSTEM');
GO

-- emp_leave
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status]) VALUES
(9001,'ABENA MENSAH','ADMINISTRATION','HEAD','With Pay','Vacation','2026-05-10','2026-05-12','APPROVED'),
(9002,'KWAME ASARE','JHS (JUNIOR HIGH SCHOOL)','DEPUTY','With Pay','Sick','2026-05-15','2026-05-19','APPROVED'),
(9003,'AKOSUA OWUSU','UPPER PRIMARY','NON-POSITIONAL','With Pay','Funeral','2026-05-01','2026-05-07','APPROVED'),
(9004,'YAW BOATENG','LOWER PRIMARY','NON-POSITIONAL','With Pay','Vacation','2026-03-10','2026-03-13','APPROVED'),
(9005,'AMA DARKO','KINDERGARTEN','NON-POSITIONAL','With Pay','Sick','2026-05-28','2026-05-29','PENDING'),
(9006,'KOFI NKRUMAH','NURSERY','SECRETARY','With Pay','Vacation','2026-06-01','2026-06-05','REJECTED'),
(9007,'EFUA ADDO','ADMINISTRATION','NON-POSITIONAL','With Pay','Sick','2026-05-04','2026-05-05','APPROVED'),
(9007,'EFUA ADDO','ADMINISTRATION','NON-POSITIONAL','With Pay','Paternity','2026-05-20','2026-05-20','APPROVED');
GO

-- Attendance (today = 2026-05-26)
DECLARE @today date = '2026-05-26', @now datetime = GETDATE();
INSERT INTO Attendance (ReferenceID,ReferenceType,FullName,[Date],[Status],Remarks,CreatedDate) VALUES
(9001,'STUDENT','KOFI AGYEMAN',@today,'PRESENT','',@now),
(9002,'STUDENT','AMA BOATENG',@today,'PRESENT','',@now),
(9003,'STUDENT','YAW OPOKU',@today,'ABSENT','Reported sick',@now),
(9004,'STUDENT','ABENA MENSAH',@today,'PRESENT','',@now),
(9005,'STUDENT','KWAME OSEI',@today,'PRESENT','',@now),
(9006,'STUDENT','EFUA DARKO',@today,'LATE','Arrived 9:15am',@now),
(9007,'STUDENT','NANA ASANTE',@today,'PRESENT','',@now),
(9008,'STUDENT','AKUA ADJEI',@today,'PRESENT','',@now),
(9009,'STUDENT','KOJO OWUSU',@today,'ABSENT','No call from parent',@now),
(9010,'STUDENT','ESI AFRIYIE',@today,'PRESENT','',@now),
(9011,'STUDENT','KWESI BOAKYE',@today,'PRESENT','',@now),
(9012,'STUDENT','NHYIRA GYAMFI',@today,'PRESENT','',@now),
(9001,'STAFF','ABENA MENSAH',@today,'PRESENT','',@now),
(9002,'STAFF','KWAME ASARE',@today,'PRESENT','',@now),
(9003,'STAFF','AKOSUA OWUSU',@today,'ON LEAVE','Approved leave',@now),
(9004,'STAFF','YAW BOATENG',@today,'PRESENT','',@now),
(9005,'STAFF','AMA DARKO',@today,'LATE','Traffic',@now),
(9006,'STAFF','KOFI NKRUMAH',@today,'PRESENT','',@now),
(9007,'STAFF','EFUA ADDO',@today,'PRESENT','',@now),
(9008,'STAFF','KOJO ANTWI',@today,'PRESENT','',@now);
GO

-- examss — 3 subjects each for the 12 demo students
INSERT INTO examss (std_id,std_name,std_class,subject,term,[year],cat1,cat2,cat3,tl_cat,exam_score,gt,grade,remark) VALUES
(9001,'KOFI AGYEMAN','BASIC 1','English','TERM 3','2026',9,8,9,26,60,86,'A','Excellent'),
(9001,'KOFI AGYEMAN','BASIC 1','Mathematics','TERM 3','2026',8,9,8,25,55,80,'A','Very Good'),
(9001,'KOFI AGYEMAN','BASIC 1','Science','TERM 3','2026',9,9,9,27,58,85,'A','Excellent'),
(9002,'AMA BOATENG','BASIC 1','English','TERM 3','2026',7,8,7,22,50,72,'B','Good'),
(9002,'AMA BOATENG','BASIC 1','Mathematics','TERM 3','2026',6,7,8,21,45,66,'C','Satisfactory'),
(9002,'AMA BOATENG','BASIC 1','Science','TERM 3','2026',8,7,8,23,52,75,'B','Good'),
(9003,'YAW OPOKU','BASIC 1','English','TERM 3','2026',6,6,7,19,40,59,'D','Needs Work'),
(9003,'YAW OPOKU','BASIC 1','Mathematics','TERM 3','2026',7,6,7,20,42,62,'C','Satisfactory'),
(9003,'YAW OPOKU','BASIC 1','Science','TERM 3','2026',6,7,6,19,41,60,'C','Satisfactory'),
(9004,'ABENA MENSAH','BASIC 3','English','TERM 3','2026',9,9,9,27,63,90,'A','Outstanding'),
(9004,'ABENA MENSAH','BASIC 3','Mathematics','TERM 3','2026',8,9,9,26,60,86,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Science','TERM 3','2026',9,8,9,26,59,85,'A','Excellent'),
(9005,'KWAME OSEI','BASIC 3','English','TERM 3','2026',7,7,7,21,48,69,'C','Satisfactory'),
(9005,'KWAME OSEI','BASIC 3','Mathematics','TERM 3','2026',8,8,7,23,53,76,'B','Good'),
(9005,'KWAME OSEI','BASIC 3','Science','TERM 3','2026',7,8,7,22,49,71,'B','Good'),
(9006,'EFUA DARKO','BASIC 3','English','TERM 3','2026',8,7,8,23,54,77,'B','Good'),
(9006,'EFUA DARKO','BASIC 3','Mathematics','TERM 3','2026',7,8,8,23,52,75,'B','Good'),
(9006,'EFUA DARKO','BASIC 3','Science','TERM 3','2026',8,8,8,24,55,79,'B','Good'),
(9007,'NANA ASANTE','BASIC 5','English','TERM 3','2026',9,9,8,26,62,88,'A','Excellent'),
(9007,'NANA ASANTE','BASIC 5','Mathematics','TERM 3','2026',9,9,9,27,65,92,'A','Outstanding'),
(9007,'NANA ASANTE','BASIC 5','Science','TERM 3','2026',8,9,9,26,60,86,'A','Excellent'),
(9008,'AKUA ADJEI','BASIC 5','English','TERM 3','2026',7,7,8,22,50,72,'B','Good'),
(9008,'AKUA ADJEI','BASIC 5','Mathematics','TERM 3','2026',8,7,7,22,51,73,'B','Good'),
(9008,'AKUA ADJEI','BASIC 5','Science','TERM 3','2026',7,7,7,21,48,69,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','English','TERM 3','2026',6,7,6,19,42,61,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Mathematics','TERM 3','2026',7,6,6,19,40,59,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','Science','TERM 3','2026',6,6,7,19,41,60,'C','Satisfactory'),
(9010,'ESI AFRIYIE','BASIC 7','English','TERM 3','2026',8,9,9,26,60,86,'A','Excellent'),
(9010,'ESI AFRIYIE','BASIC 7','Mathematics','TERM 3','2026',9,9,8,26,61,87,'A','Excellent'),
(9010,'ESI AFRIYIE','BASIC 7','Science','TERM 3','2026',9,9,9,27,62,89,'A','Excellent'),
(9011,'KWESI BOAKYE','BASIC 7','English','TERM 3','2026',7,8,7,22,50,72,'B','Good'),
(9011,'KWESI BOAKYE','BASIC 7','Mathematics','TERM 3','2026',8,8,8,24,56,80,'A','Very Good'),
(9011,'KWESI BOAKYE','BASIC 7','Science','TERM 3','2026',7,7,8,22,49,71,'B','Good'),
(9012,'NHYIRA GYAMFI','BASIC 7','English','TERM 3','2026',9,8,9,26,58,84,'A','Very Good'),
(9012,'NHYIRA GYAMFI','BASIC 7','Mathematics','TERM 3','2026',8,9,8,25,57,82,'A','Very Good'),
(9012,'NHYIRA GYAMFI','BASIC 7','Science','TERM 3','2026',9,9,8,26,58,84,'A','Very Good');
GO

-- StudentTermRemarks for demo students
INSERT INTO StudentTermRemarks (StudentID,Term,[Year],ClassTeacherRemarks,HeadTeacherRemarks,Attitude,Interest,Conduct,CreatedDate) VALUES
('9001','TERM 3','2026','Excellent performance across all subjects. Keep it up.','A model student. Commendable.','Excellent','High','Excellent','2026-05-20'),
('9002','TERM 3','2026','Good effort. Needs to focus more on mathematics.','Consistent effort shown.','Good','Good','Good','2026-05-20'),
('9003','TERM 3','2026','Has potential but needs to apply himself more.','Encouraged to seek extra help.','Average','Average','Good','2026-05-20'),
('9004','TERM 3','2026','Outstanding term. Leading the class.','Exemplary academic record.','Excellent','High','Excellent','2026-05-20'),
('9005','TERM 3','2026','Steady improvement noticed this term.','Keep the momentum going.','Good','Good','Good','2026-05-20'),
('9006','TERM 3','2026','Reliable and hardworking student.','Continue the good work.','Good','High','Excellent','2026-05-20'),
('9007','TERM 3','2026','Exceptional in mathematics. A future leader.','Top of the class. Brilliant.','Excellent','High','Excellent','2026-05-20'),
('9008','TERM 3','2026','Good performance. Encouraged to read more.','Solid term.','Good','Good','Good','2026-05-20'),
('9009','TERM 3','2026','Needs more attention in maths and English.','Recommended for tutoring.','Average','Average','Good','2026-05-20'),
('9010','TERM 3','2026','Brilliant work throughout the term.','One of the best in her class.','Excellent','High','Excellent','2026-05-20'),
('9011','TERM 3','2026','Strong in mathematics. Keep improving in others.','Promising student.','Good','Good','Good','2026-05-20'),
('9012','TERM 3','2026','Consistent and disciplined.','Very promising. Well-rounded.','Excellent','High','Excellent','2026-05-20');
GO


-- ============================================================
-- 4. FULL-RECORD STUDENT — BASIC 6, all 10 subjects, Term 3 2026
--    Student ID: 1001  (well below demo range 9001-9012)
-- ============================================================

-- Cleanup first (safe re-run)
DELETE FROM StudentTermRemarks WHERE StudentID = '1001';
DELETE FROM examss             WHERE std_id = 1001;
DELETE FROM payment_record     WHERE StudentID = 1001;
DELETE FROM fees               WHERE StudentID = 1001;
DELETE FROM Students           WHERE StudentID = 1001;
GO

SET IDENTITY_INSERT Students ON;
INSERT INTO Students
    (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID,
     HomeTown, Residence, Allegies, EmergencyConatct,
     GuidanceName, GuidianceEmail, Guidiance_Location,
     admission_date, Std_pic)
VALUES
(1001,
 'EMMANUEL', 'BUABENG',
 '2014-03-15', 'MALE',
 'emmanuel.buabeng@school.demo',
 'BASIC 6',
 'ACCRA', 'EAST LEGON',
 'None',
 '0200123456',
 'DANIEL BUABENG',
 'daniel.buabeng@example.com',
 'EAST LEGON',
 '2021-09-01',
 NULL);
SET IDENTITY_INSERT Students OFF;
GO

-- Fee & initial payment
INSERT INTO fees (StudentID, ClassID, FeeName, Amount)
VALUES (1001, 'BASIC 6', 'Tuition Fee', 2800.00);

INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(1001, 'BASIC 6', 2800.00, 'EMMANUEL BUABENG', 0.00,    '2026-01-15', '08:00:00', 'ENROLLMENT', 'SYSTEM'),
(1001, 'BASIC 6', 1300.00, 'EMMANUEL BUABENG', 1500.00, '2026-02-05', '09:10:00', 'CASH',       'BURSAR JANE'),
(1001, 'BASIC 6',    0.00, 'EMMANUEL BUABENG', 1300.00, '2026-04-10', '10:05:00', 'MOMO',       'BURSAR JANE');
GO

-- All 10 subjects — Term 3, 2026
-- Scores: cat1+cat2+cat3 = tl_cat (out of 30); exam_score out of 70; gt = tl_cat + exam_score (out of 100)
INSERT INTO examss
    (std_id, std_name, std_class, subject, term, [year],
     cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark)
VALUES
(1001,'EMMANUEL BUABENG','BASIC 6','English Language',       'TERM 3','2026', 9,  9,  8, 26, 62, 88, 'A', 'Excellent'),
(1001,'EMMANUEL BUABENG','BASIC 6','Mathematics',            'TERM 3','2026', 9,  8,  9, 26, 64, 90, 'A', 'Outstanding'),
(1001,'EMMANUEL BUABENG','BASIC 6','Integrated Science',     'TERM 3','2026', 8,  9,  9, 26, 59, 85, 'A', 'Excellent'),
(1001,'EMMANUEL BUABENG','BASIC 6','Social Studies',         'TERM 3','2026', 8,  8,  8, 24, 58, 82, 'A', 'Very Good'),
(1001,'EMMANUEL BUABENG','BASIC 6','Creative Arts',          'TERM 3','2026', 9,  9,  9, 27, 60, 87, 'A', 'Excellent'),
(1001,'EMMANUEL BUABENG','BASIC 6','Religious & Moral Edu.', 'TERM 3','2026', 8,  9,  8, 25, 57, 82, 'A', 'Very Good'),
(1001,'EMMANUEL BUABENG','BASIC 6','Ghanaian Language',      'TERM 3','2026', 7,  8,  8, 23, 55, 78, 'B', 'Good'),
(1001,'EMMANUEL BUABENG','BASIC 6','French',                 'TERM 3','2026', 8,  7,  8, 23, 54, 77, 'B', 'Good'),
(1001,'EMMANUEL BUABENG','BASIC 6','ICT',                    'TERM 3','2026', 9,  9,  8, 26, 61, 87, 'A', 'Excellent'),
(1001,'EMMANUEL BUABENG','BASIC 6','Physical Education',     'TERM 3','2026', 9,  9,  9, 27, 63, 90, 'A', 'Outstanding');
GO

-- Term remarks for the full-record student
INSERT INTO StudentTermRemarks
    (StudentID, Term, [Year], ClassTeacherRemarks, HeadTeacherRemarks,
     Attitude, Interest, Conduct, CreatedDate)
VALUES
('1001', 'TERM 3', '2026',
 'Emmanuel has demonstrated outstanding academic ability across all ten subjects this term. '
 'His dedication and consistent hard work place him at the top of the class. '
 'He is a role model to his peers and should continue to strive for excellence.',
 'A truly exceptional student. Emmanuel''s performance this term is commendable. '
 'We are proud of his achievements and look forward to his continued growth.',
 'Excellent', 'High', 'Excellent',
 '2026-05-22');
GO


-- ============================================================
-- 5. SUMMARY
-- ============================================================
PRINT '=== Setup complete ===';
SELECT 'Employee'           AS [Table], COUNT(*) AS [Rows] FROM Employee
UNION ALL SELECT 'Students',            COUNT(*) FROM Students
UNION ALL SELECT 'fees',                COUNT(*) FROM fees
UNION ALL SELECT 'payment_record',      COUNT(*) FROM payment_record
UNION ALL SELECT 'emp_leave',           COUNT(*) FROM emp_leave
UNION ALL SELECT 'Attendance',          COUNT(*) FROM Attendance
UNION ALL SELECT 'examss',              COUNT(*) FROM examss
UNION ALL SELECT 'StudentTermRemarks',  COUNT(*) FROM StudentTermRemarks;
GO

PRINT '';
PRINT '=== Full-record student (ID 1001) exam results ===';
SELECT subject AS [Subject], tl_cat AS [CA /30], exam_score AS [Exam /70], gt AS [Total /100], grade AS [Grade], remark AS [Remark]
FROM examss
WHERE std_id = 1001
ORDER BY subject;
GO
