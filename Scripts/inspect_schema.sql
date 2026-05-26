-- ============================================================
-- Diagnostic: dump real column definitions for every table the
-- seed script touches. Paste the full output back to Claude.
-- ============================================================
USE [Neat_Academy];
GO

PRINT '=== Tables that currently exist ===';
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

PRINT '';
PRINT '=== Column definitions (name, type, max length, nullable, identity) ===';
SELECT
    t.name                          AS TableName,
    c.column_id                     AS Ord,
    c.name                          AS ColumnName,
    ty.name                         AS DataType,
    c.max_length                    AS MaxLength,
    c.is_nullable                   AS IsNullable,
    c.is_identity                   AS IsIdentity,
    CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPK
FROM sys.tables t
JOIN sys.columns c       ON c.object_id = t.object_id
JOIN sys.types ty        ON ty.user_type_id = c.user_type_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id
    FROM sys.index_columns ic
    JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.is_primary_key = 1
) pk ON pk.object_id = t.object_id AND pk.column_id = c.column_id
WHERE t.name IN ('Classes','Employee','Students','fees','payment_record','emp_leave','Attendance','examss','StudentTermRemarks','Rolled_Out_Students')
ORDER BY t.name, c.column_id;
GO

PRINT '';
PRINT '=== Sample existing rows from each table (1 row each) ===';

IF OBJECT_ID('Employee') IS NOT NULL  SELECT TOP 1 * FROM Employee;
IF OBJECT_ID('Students') IS NOT NULL  SELECT TOP 1 * FROM Students;
IF OBJECT_ID('Classes')  IS NOT NULL  SELECT TOP 1 * FROM Classes;
IF OBJECT_ID('fees')     IS NOT NULL  SELECT TOP 1 * FROM fees;
IF OBJECT_ID('payment_record') IS NOT NULL  SELECT TOP 1 * FROM payment_record;
IF OBJECT_ID('emp_leave') IS NOT NULL SELECT TOP 1 * FROM emp_leave;
IF OBJECT_ID('Attendance') IS NOT NULL SELECT TOP 1 * FROM Attendance;
IF OBJECT_ID('examss')   IS NOT NULL  SELECT TOP 1 * FROM examss;
IF OBJECT_ID('StudentTermRemarks') IS NOT NULL SELECT TOP 1 * FROM StudentTermRemarks;
GO
