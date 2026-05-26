-- ============================================================
-- Full demo seed — matches the ACTUAL Neat_Academy schema
-- (verified via INFORMATION_SCHEMA on 2026-05-26)
--
-- Demo ID ranges (chosen high to avoid collisions with real data):
--   Employee.employmentID   9001-9008   (8 staff)
--   Students.StudentID      9001-9012   (12 students)
--
-- FK insert order:
--   Employee → Students → fees → payment_record →
--   emp_leave → Attendance → examss → StudentTermRemarks
--
-- NOTE: Classes table is not present in this DB (created lazily
-- by the app's ClassRepository) so we don't touch it. ClassID is
-- a free-form varchar in fees / Students / payment_record.
-- ============================================================

USE [Neat_Academy];
GO
SET NOCOUNT ON;
GO

-- =============================================================
-- 0. CLEANUP (reverse FK order, demo rows only — safe re-run)
-- =============================================================
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

-- =============================================================
-- 1. EMPLOYEE (8 demo staff, IDs 9001–9008)
-- =============================================================
SET IDENTITY_INSERT Employee ON;
GO

INSERT INTO Employee
    (employmentID, fullName, gender, dOB, conatct, department, position,
     homeTown, residence, date_of_Emplyment, employment_Mode,
     employment_Status, emergency_Contact_Person, emergency_contact,
     employees_Reviews, salary, pic)
VALUES
(9001,'ABENA MENSAH',  'FEMALE','1985-03-12','0244000001','ADMINISTRATION',          'HEAD',           'KUMASI',   'EAST LEGON','2018-09-01','FULL-TIME','ACTIVE','KOFI MENSAH',  '0244111111','A: EXCELLENT',   3500,0x),
(9002,'KWAME ASARE',   'MALE',  '1990-07-22','0244000002','JHS (JUNIOR HIGH SCHOOL)','DEPUTY',         'TAKORADI', 'ADENTA',    '2019-09-01','FULL-TIME','ACTIVE','AKOSUA ASARE', '0244222222','B: GOOD',        2800,0x),
(9003,'AKOSUA OWUSU',  'FEMALE','1992-11-05','0244000003','UPPER PRIMARY',           'NON-POSITIONAL', 'CAPE COAST','MADINA',   '2020-09-01','FULL-TIME','ACTIVE','YAW OWUSU',    '0244333333','B: GOOD',        2200,0x),
(9004,'YAW BOATENG',   'MALE',  '1988-01-18','0244000004','LOWER PRIMARY',           'NON-POSITIONAL', 'KOFORIDUA','TESHIE',    '2017-09-01','FULL-TIME','ACTIVE','AMA BOATENG',  '0244444444','A: EXCELLENT',   2400,0x),
(9005,'AMA DARKO',     'FEMALE','1995-05-30','0244000005','KINDERGARTEN',            'NON-POSITIONAL', 'TAMALE',   'OSU',       '2021-09-01','FULL-TIME','ACTIVE','KOJO DARKO',   '0244555555','C: SATISFACTORY',2000,0x),
(9006,'KOFI NKRUMAH',  'MALE',  '1993-09-08','0244000006','NURSERY',                 'SECRETARY',      'HO',       'ACHIMOTA',  '2022-09-01','FULL-TIME','ACTIVE','EFUA NKRUMAH', '0244666666','B: GOOD',        2100,0x),
(9007,'EFUA ADDO',     'FEMALE','1987-12-15','0244000007','ADMINISTRATION',          'NON-POSITIONAL', 'SUNYANI',  'LASHIBI',   '2016-09-01','PART-TIME','ACTIVE','NANA ADDO',    '0244777777','B: GOOD',        1800,0x),
(9008,'KOJO ANTWI',    'MALE',  '1991-04-25','0244000008','SANITATION & CLEANING',   'NON-POSITIONAL', 'ACCRA',    'NUNGUA',    '2019-09-01','FULL-TIME','ACTIVE','AKUA ANTWI',   '0244888888','A: EXCELLENT',   1500,0x);
GO

SET IDENTITY_INSERT Employee OFF;
GO

-- =============================================================
-- 2. STUDENTS (12 demo students, IDs 9001–9012)
-- =============================================================
SET IDENTITY_INSERT Students ON;
GO

INSERT INTO Students
    (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID,
     HomeTown, Residence, Allegies, EmergencyConatct,
     GuidanceName, GuidianceEmail, Guidiance_Location,
     admission_date, Std_pic)
VALUES
(9001,'KOFI',   'AGYEMAN',  '2019-04-12','MALE',  'kofi.agyeman@school.demo',  'BASIC 1','KUMASI','EAST LEGON','None',   '0244900001','SAMUEL AGYEMAN','samuel.agyeman@example.com','EAST LEGON','2024-09-01',NULL),
(9002,'AMA',    'BOATENG',  '2019-08-25','FEMALE','ama.boateng@school.demo',   'BASIC 1','ACCRA', 'MADINA',    'Peanuts','0244900002','GIFTY BOATENG', 'gifty.boateng@example.com', 'MADINA',    '2024-09-01',NULL),
(9003,'YAW',    'OPOKU',    '2019-12-03','MALE',  'yaw.opoku@school.demo',     'BASIC 1','TEMA',  'TESHIE',    'None',   '0244900003','JOHN OPOKU',    'john.opoku@example.com',    'TESHIE',    '2024-09-01',NULL),
(9004,'ABENA',  'MENSAH',   '2017-06-15','FEMALE','abena.mensah@school.demo',  'BASIC 3','KUMASI','ADENTA',    'Dust',   '0244900004','MARY MENSAH',   'mary.mensah@example.com',   'ADENTA',    '2022-09-01',NULL),
(9005,'KWAME',  'OSEI',     '2017-09-20','MALE',  'kwame.osei@school.demo',    'BASIC 3','TAKORADI','LASHIBI', 'None',   '0244900005','PETER OSEI',    'peter.osei@example.com',    'LASHIBI',   '2022-09-01',NULL),
(9006,'EFUA',   'DARKO',    '2017-11-08','FEMALE','efua.darko@school.demo',    'BASIC 3','HO',    'NUNGUA',    'Lactose','0244900006','RUTH DARKO',    'ruth.darko@example.com',    'NUNGUA',    '2022-09-01',NULL),
(9007,'NANA',   'ASANTE',   '2015-02-19','MALE',  'nana.asante@school.demo',   'BASIC 5','KOFORIDUA','OSU',    'None',   '0244900007','DANIEL ASANTE', 'daniel.asante@example.com', 'OSU',       '2020-09-01',NULL),
(9008,'AKUA',   'ADJEI',    '2015-05-30','FEMALE','akua.adjei@school.demo',    'BASIC 5','CAPE COAST','SPINTEX','Eggs',  '0244900008','VIDA ADJEI',    'vida.adjei@example.com',    'SPINTEX',   '2020-09-01',NULL),
(9009,'KOJO',   'OWUSU',    '2015-07-14','MALE',  'kojo.owusu@school.demo',    'BASIC 5','SUNYANI','ACHIMOTA',  'None',  '0244900009','KWESI OWUSU',   'kwesi.owusu@example.com',   'ACHIMOTA',  '2020-09-01',NULL),
(9010,'ESI',    'AFRIYIE',  '2013-03-22','FEMALE','esi.afriyie@school.demo',   'BASIC 7','TAMALE','DOME',      'None',   '0244900010','ANITA AFRIYIE', 'anita.afriyie@example.com', 'DOME',      '2018-09-01',NULL),
(9011,'KWESI',  'BOAKYE',   '2013-10-05','MALE',  'kwesi.boakye@school.demo',  'BASIC 7','ACCRA', 'KASOA',     'Asthma', '0244900011','EMMA BOAKYE',   'emma.boakye@example.com',   'KASOA',     '2018-09-01',NULL),
(9012,'NHYIRA', 'GYAMFI',   '2013-12-11','FEMALE','nhyira.gyamfi@school.demo', 'BASIC 7','KUMASI','SAKUMONO',  'None',   '0244900012','JOSEPH GYAMFI', 'joseph.gyamfi@example.com', 'SAKUMONO',  '2018-09-01',NULL);
GO

SET IDENTITY_INSERT Students OFF;
GO

-- =============================================================
-- 3. FEES (one initial fee record per student)
-- =============================================================
INSERT INTO fees (StudentID, ClassID, FeeName, Amount) VALUES
(9001,'BASIC 1','Tuition Fee',2423.00),
(9002,'BASIC 1','Tuition Fee',2423.00),
(9003,'BASIC 1','Tuition Fee',2423.00),
(9004,'BASIC 3','Tuition Fee',2423.00),
(9005,'BASIC 3','Tuition Fee',2423.00),
(9006,'BASIC 3','Tuition Fee',2423.00),
(9007,'BASIC 5','Tuition Fee',2600.00),
(9008,'BASIC 5','Tuition Fee',2600.00),
(9009,'BASIC 5','Tuition Fee',2600.00),
(9010,'BASIC 7','Tuition Fee',3000.00),
(9011,'BASIC 7','Tuition Fee',3000.00),
(9012,'BASIC 7','Tuition Fee',3000.00);
GO

-- =============================================================
-- 4. PAYMENT_RECORD (mix of fully paid / partial / untouched)
--    NOTE: schema has no FeeName column — omitted from INSERT.
-- =============================================================
-- 9001 KOFI AGYEMAN — fully paid (2 payments)
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9001,'BASIC 1','KOFI AGYEMAN',2423.00,   0.00,'2026-01-15','08:30:00','ENROLLMENT','SYSTEM'),
(9001,'BASIC 1','KOFI AGYEMAN',1223.00,1200.00,'2026-02-10','09:15:00','CASH',      'BURSAR JANE'),
(9001,'BASIC 1','KOFI AGYEMAN',   0.00,1223.00,'2026-03-08','10:20:00','MOMO',      'BURSAR JANE');

-- 9002 AMA BOATENG — partial
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9002,'BASIC 1','AMA BOATENG', 2423.00,   0.00,'2026-01-15','08:31:00','ENROLLMENT','SYSTEM'),
(9002,'BASIC 1','AMA BOATENG',  923.00,1500.00,'2026-02-12','11:00:00','CASH',      'BURSAR JANE');

-- 9003 YAW OPOKU — single partial
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9003,'BASIC 1','YAW OPOKU',   2423.00,   0.00,'2026-01-15','08:32:00','ENROLLMENT','SYSTEM'),
(9003,'BASIC 1','YAW OPOKU',   1923.00, 500.00,'2026-03-05','14:45:00','CHEQUE',    'BURSAR JANE');

-- 9004 ABENA MENSAH — fully paid
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9004,'BASIC 3','ABENA MENSAH',2423.00,   0.00,'2026-01-15','08:33:00','ENROLLMENT','SYSTEM'),
(9004,'BASIC 3','ABENA MENSAH',   0.00,2423.00,'2026-02-20','09:00:00','BANK',      'BURSAR JANE');

-- 9005 KWAME OSEI — untouched
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9005,'BASIC 3','KWAME OSEI',  2423.00,   0.00,'2026-01-15','08:34:00','ENROLLMENT','SYSTEM');

-- 9006 EFUA DARKO — partial
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9006,'BASIC 3','EFUA DARKO',  2423.00,   0.00,'2026-01-15','08:35:00','ENROLLMENT','SYSTEM'),
(9006,'BASIC 3','EFUA DARKO',  1623.00, 800.00,'2026-03-12','10:30:00','MOMO',      'BURSAR JANE');

-- 9007 NANA ASANTE — fully paid
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9007,'BASIC 5','NANA ASANTE', 2600.00,   0.00,'2026-01-16','08:36:00','ENROLLMENT','SYSTEM'),
(9007,'BASIC 5','NANA ASANTE',    0.00,2600.00,'2026-01-20','09:30:00','BANK',      'BURSAR JANE');

-- 9008 AKUA ADJEI — partial
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9008,'BASIC 5','AKUA ADJEI',  2600.00,   0.00,'2026-01-16','08:37:00','ENROLLMENT','SYSTEM'),
(9008,'BASIC 5','AKUA ADJEI',  1600.00,1000.00,'2026-02-18','11:15:00','CASH',      'BURSAR JANE');

-- 9009 KOJO OWUSU — untouched
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9009,'BASIC 5','KOJO OWUSU',  2600.00,   0.00,'2026-01-16','08:38:00','ENROLLMENT','SYSTEM');

-- 9010 ESI AFRIYIE — fully paid (3 payments)
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9010,'BASIC 7','ESI AFRIYIE', 3000.00,   0.00,'2026-01-17','08:39:00','ENROLLMENT','SYSTEM'),
(9010,'BASIC 7','ESI AFRIYIE', 1000.00,2000.00,'2026-02-05','10:00:00','BANK',      'BURSAR JANE'),
(9010,'BASIC 7','ESI AFRIYIE',    0.00,1000.00,'2026-04-15','13:20:00','CHEQUE',    'BURSAR JANE');

-- 9011 KWESI BOAKYE — partial
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9011,'BASIC 7','KWESI BOAKYE',3000.00,   0.00,'2026-01-17','08:40:00','ENROLLMENT','SYSTEM'),
(9011,'BASIC 7','KWESI BOAKYE',1500.00,1500.00,'2026-03-22','11:45:00','MOMO',      'BURSAR JANE');

-- 9012 NHYIRA GYAMFI — untouched
INSERT INTO payment_record (StudentID, classID, student_name, Balance, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES
(9012,'BASIC 7','NHYIRA GYAMFI',3000.00,  0.00,'2026-01-17','08:41:00','ENROLLMENT','SYSTEM');
GO

-- =============================================================
-- 5. EMP_LEAVE (scenarios drive Leave Balance report values)
-- =============================================================
-- 9001 Abena: 3 days APPROVED this term → balance = 4
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9001,'ABENA MENSAH','ADMINISTRATION','HEAD','With Pay','Vacation','2026-05-10','2026-05-12','APPROVED');

-- 9002 Kwame: 5 days APPROVED this term → balance = 2
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9002,'KWAME ASARE','JHS (JUNIOR HIGH SCHOOL)','DEPUTY','With Pay','Sick','2026-05-15','2026-05-19','APPROVED');

-- 9003 Akosua: full entitlement used → balance = 0
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9003,'AKOSUA OWUSU','UPPER PRIMARY','NON-POSITIONAL','With Pay','Funeral','2026-05-01','2026-05-07','APPROVED');

-- 9004 Yaw: 4 days approved in PREVIOUS term → balance still 7
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9004,'YAW BOATENG','LOWER PRIMARY','NON-POSITIONAL','With Pay','Vacation','2026-03-10','2026-03-13','APPROVED');

-- 9005 Ama: PENDING → doesn't count → balance = 7
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9005,'AMA DARKO','KINDERGARTEN','NON-POSITIONAL','With Pay','Sick','2026-05-28','2026-05-29','PENDING');

-- 9006 Kofi: REJECTED → doesn't count → balance = 7
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9006,'KOFI NKRUMAH','NURSERY','SECRETARY','With Pay','Vacation','2026-06-01','2026-06-05','REJECTED');

-- 9007 Efua: 2+1 days across two requests → balance = 4
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9007,'EFUA ADDO','ADMINISTRATION','NON-POSITIONAL','With Pay','Sick','2026-05-04','2026-05-05','APPROVED');
INSERT INTO emp_leave (employmentID,[name],department,position,Leave_op,Reasons,Start_Date,End_Date,[status])
VALUES (9007,'EFUA ADDO','ADMINISTRATION','NON-POSITIONAL','With Pay','Paternity','2026-05-20','2026-05-20','APPROVED');

-- 9008 Kojo: no leaves → balance = 7
GO

-- =============================================================
-- 6. ATTENDANCE (today's attendance, students + staff)
--    ReferenceID is INT; CreatedDate is NOT NULL.
-- =============================================================
DECLARE @today date = '2026-05-26';
DECLARE @now   datetime = GETDATE();

-- Students
INSERT INTO Attendance (ReferenceID, ReferenceType, FullName, [Date], [Status], Remarks, CreatedDate) VALUES
(9001,'STUDENT','KOFI AGYEMAN',  @today,'PRESENT','',                 @now),
(9002,'STUDENT','AMA BOATENG',   @today,'PRESENT','',                 @now),
(9003,'STUDENT','YAW OPOKU',     @today,'ABSENT', 'Reported sick',    @now),
(9004,'STUDENT','ABENA MENSAH',  @today,'PRESENT','',                 @now),
(9005,'STUDENT','KWAME OSEI',    @today,'PRESENT','',                 @now),
(9006,'STUDENT','EFUA DARKO',    @today,'LATE',   'Arrived 9:15am',   @now),
(9007,'STUDENT','NANA ASANTE',   @today,'PRESENT','',                 @now),
(9008,'STUDENT','AKUA ADJEI',    @today,'PRESENT','',                 @now),
(9009,'STUDENT','KOJO OWUSU',    @today,'ABSENT', 'No call from parent',@now),
(9010,'STUDENT','ESI AFRIYIE',   @today,'PRESENT','',                 @now),
(9011,'STUDENT','KWESI BOAKYE',  @today,'PRESENT','',                 @now),
(9012,'STUDENT','NHYIRA GYAMFI', @today,'PRESENT','',                 @now);

-- Staff
INSERT INTO Attendance (ReferenceID, ReferenceType, FullName, [Date], [Status], Remarks, CreatedDate) VALUES
(9001,'STAFF','ABENA MENSAH', @today,'PRESENT','',                @now),
(9002,'STAFF','KWAME ASARE',  @today,'PRESENT','',                @now),
(9003,'STAFF','AKOSUA OWUSU', @today,'ON LEAVE','Approved leave', @now),
(9004,'STAFF','YAW BOATENG',  @today,'PRESENT','',                @now),
(9005,'STAFF','AMA DARKO',    @today,'LATE',   'Traffic',         @now),
(9006,'STAFF','KOFI NKRUMAH', @today,'PRESENT','',                @now),
(9007,'STAFF','EFUA ADDO',    @today,'PRESENT','',                @now),
(9008,'STAFF','KOJO ANTWI',   @today,'PRESENT','',                @now);
GO

-- =============================================================
-- 7. EXAMSS (Term 3, 2026 — 3 subjects per student)
--    remark column is varchar(15), so labels are kept short.
-- =============================================================
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9001,'KOFI AGYEMAN', 'BASIC 1','English',     'TERM 3','2026', 9, 8, 9, 26, 60, 86,'A','Excellent'),
(9001,'KOFI AGYEMAN', 'BASIC 1','Mathematics', 'TERM 3','2026', 8, 9, 8, 25, 55, 80,'A','Very Good'),
(9001,'KOFI AGYEMAN', 'BASIC 1','Science',     'TERM 3','2026', 9, 9, 9, 27, 58, 85,'A','Excellent'),

(9002,'AMA BOATENG',  'BASIC 1','English',     'TERM 3','2026', 7, 8, 7, 22, 50, 72,'B','Good'),
(9002,'AMA BOATENG',  'BASIC 1','Mathematics', 'TERM 3','2026', 6, 7, 8, 21, 45, 66,'C','Satisfactory'),
(9002,'AMA BOATENG',  'BASIC 1','Science',     'TERM 3','2026', 8, 7, 8, 23, 52, 75,'B','Good'),

(9003,'YAW OPOKU',    'BASIC 1','English',     'TERM 3','2026', 6, 6, 7, 19, 40, 59,'D','Needs Work'),
(9003,'YAW OPOKU',    'BASIC 1','Mathematics', 'TERM 3','2026', 7, 6, 7, 20, 42, 62,'C','Satisfactory'),
(9003,'YAW OPOKU',    'BASIC 1','Science',     'TERM 3','2026', 6, 7, 6, 19, 41, 60,'C','Satisfactory'),

(9004,'ABENA MENSAH', 'BASIC 3','English',     'TERM 3','2026', 9, 9, 9, 27, 63, 90,'A','Outstanding'),
(9004,'ABENA MENSAH', 'BASIC 3','Mathematics', 'TERM 3','2026', 8, 9, 9, 26, 60, 86,'A','Excellent'),
(9004,'ABENA MENSAH', 'BASIC 3','Science',     'TERM 3','2026', 9, 8, 9, 26, 59, 85,'A','Excellent'),

(9005,'KWAME OSEI',   'BASIC 3','English',     'TERM 3','2026', 7, 7, 7, 21, 48, 69,'C','Satisfactory'),
(9005,'KWAME OSEI',   'BASIC 3','Mathematics', 'TERM 3','2026', 8, 8, 7, 23, 53, 76,'B','Good'),
(9005,'KWAME OSEI',   'BASIC 3','Science',     'TERM 3','2026', 7, 8, 7, 22, 49, 71,'B','Good'),

(9006,'EFUA DARKO',   'BASIC 3','English',     'TERM 3','2026', 8, 7, 8, 23, 54, 77,'B','Good'),
(9006,'EFUA DARKO',   'BASIC 3','Mathematics', 'TERM 3','2026', 7, 8, 8, 23, 52, 75,'B','Good'),
(9006,'EFUA DARKO',   'BASIC 3','Science',     'TERM 3','2026', 8, 8, 8, 24, 55, 79,'B','Good'),

(9007,'NANA ASANTE',  'BASIC 5','English',     'TERM 3','2026', 9, 9, 8, 26, 62, 88,'A','Excellent'),
(9007,'NANA ASANTE',  'BASIC 5','Mathematics', 'TERM 3','2026', 9, 9, 9, 27, 65, 92,'A','Outstanding'),
(9007,'NANA ASANTE',  'BASIC 5','Science',     'TERM 3','2026', 8, 9, 9, 26, 60, 86,'A','Excellent'),

(9008,'AKUA ADJEI',   'BASIC 5','English',     'TERM 3','2026', 7, 7, 8, 22, 50, 72,'B','Good'),
(9008,'AKUA ADJEI',   'BASIC 5','Mathematics', 'TERM 3','2026', 8, 7, 7, 22, 51, 73,'B','Good'),
(9008,'AKUA ADJEI',   'BASIC 5','Science',     'TERM 3','2026', 7, 7, 7, 21, 48, 69,'C','Satisfactory'),

(9009,'KOJO OWUSU',   'BASIC 5','English',     'TERM 3','2026', 6, 7, 6, 19, 42, 61,'C','Satisfactory'),
(9009,'KOJO OWUSU',   'BASIC 5','Mathematics', 'TERM 3','2026', 7, 6, 6, 19, 40, 59,'D','Needs Work'),
(9009,'KOJO OWUSU',   'BASIC 5','Science',     'TERM 3','2026', 6, 6, 7, 19, 41, 60,'C','Satisfactory'),

(9010,'ESI AFRIYIE',  'BASIC 7','English',     'TERM 3','2026', 8, 9, 9, 26, 60, 86,'A','Excellent'),
(9010,'ESI AFRIYIE',  'BASIC 7','Mathematics', 'TERM 3','2026', 9, 9, 8, 26, 61, 87,'A','Excellent'),
(9010,'ESI AFRIYIE',  'BASIC 7','Science',     'TERM 3','2026', 9, 9, 9, 27, 62, 89,'A','Excellent'),

(9011,'KWESI BOAKYE', 'BASIC 7','English',     'TERM 3','2026', 7, 8, 7, 22, 50, 72,'B','Good'),
(9011,'KWESI BOAKYE', 'BASIC 7','Mathematics', 'TERM 3','2026', 8, 8, 8, 24, 56, 80,'A','Very Good'),
(9011,'KWESI BOAKYE', 'BASIC 7','Science',     'TERM 3','2026', 7, 7, 8, 22, 49, 71,'B','Good'),

(9012,'NHYIRA GYAMFI','BASIC 7','English',     'TERM 3','2026', 9, 8, 9, 26, 58, 84,'A','Very Good'),
(9012,'NHYIRA GYAMFI','BASIC 7','Mathematics', 'TERM 3','2026', 8, 9, 8, 25, 57, 82,'A','Very Good'),
(9012,'NHYIRA GYAMFI','BASIC 7','Science',     'TERM 3','2026', 9, 9, 8, 26, 58, 84,'A','Very Good');
GO

-- =============================================================
-- 8. STUDENTTERMREMARKS (StudentID column is varchar(50))
-- =============================================================
INSERT INTO StudentTermRemarks (StudentID, Term, Year, ClassTeacherRemarks, HeadTeacherRemarks, Attitude, Interest, Conduct, CreatedDate) VALUES
('9001','TERM 3','2026','Excellent performance across all subjects. Keep it up.','A model student. Commendable.','Excellent','High','Excellent','2026-05-20'),
('9002','TERM 3','2026','Good effort. Needs to focus more on mathematics.',     'Consistent effort shown.',     'Good',     'Good','Good',     '2026-05-20'),
('9003','TERM 3','2026','Has potential but needs to apply himself more.',       'Encouraged to seek extra help.','Average',  'Average','Good',  '2026-05-20'),
('9004','TERM 3','2026','Outstanding term. Leading the class.',                 'Exemplary academic record.',   'Excellent','High','Excellent','2026-05-20'),
('9005','TERM 3','2026','Steady improvement noticed this term.',                'Keep the momentum going.',     'Good',     'Good','Good',     '2026-05-20'),
('9006','TERM 3','2026','Reliable and hardworking student.',                    'Continue the good work.',      'Good',     'High','Excellent','2026-05-20'),
('9007','TERM 3','2026','Exceptional in mathematics. A future leader.',         'Top of the class. Brilliant.', 'Excellent','High','Excellent','2026-05-20'),
('9008','TERM 3','2026','Good performance. Encouraged to read more.',           'Solid term.',                  'Good',     'Good','Good',     '2026-05-20'),
('9009','TERM 3','2026','Needs more attention in maths and English.',           'Recommended for tutoring.',    'Average',  'Average','Good',  '2026-05-20'),
('9010','TERM 3','2026','Brilliant work throughout the term.',                  'One of the best in her class.','Excellent','High','Excellent','2026-05-20'),
('9011','TERM 3','2026','Strong in mathematics. Keep improving in others.',     'Promising student.',           'Good',     'Good','Good',     '2026-05-20'),
('9012','TERM 3','2026','Consistent and disciplined.',                          'Very promising. Well-rounded.','Excellent','High','Excellent','2026-05-20');
GO

-- =============================================================
-- SUMMARY
-- =============================================================
PRINT '';
PRINT '======================================================';
PRINT '  Demo seed complete';
PRINT '======================================================';

SELECT 'Employee (demo)'         AS [Table], COUNT(*) AS [Rows] FROM Employee  WHERE employmentID BETWEEN 9001 AND 9008
UNION ALL SELECT 'Students (demo)',       COUNT(*) FROM Students  WHERE StudentID BETWEEN 9001 AND 9012
UNION ALL SELECT 'fees (demo)',           COUNT(*) FROM fees      WHERE StudentID BETWEEN 9001 AND 9012
UNION ALL SELECT 'payment_record (demo)', COUNT(*) FROM payment_record WHERE StudentID BETWEEN 9001 AND 9012
UNION ALL SELECT 'emp_leave (demo)',      COUNT(*) FROM emp_leave WHERE employmentID BETWEEN 9001 AND 9008
UNION ALL SELECT 'Attendance (demo)',     COUNT(*) FROM Attendance
                                          WHERE (ReferenceType='STUDENT' AND ReferenceID BETWEEN 9001 AND 9012)
                                             OR (ReferenceType='STAFF'   AND ReferenceID BETWEEN 9001 AND 9008)
UNION ALL SELECT 'examss (demo)',         COUNT(*) FROM examss    WHERE std_id BETWEEN 9001 AND 9012
UNION ALL SELECT 'StudentTermRemarks (demo)', COUNT(*) FROM StudentTermRemarks WHERE StudentID IN ('9001','9002','9003','9004','9005','9006','9007','9008','9009','9010','9011','9012');
GO
