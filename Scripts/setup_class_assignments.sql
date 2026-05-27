-- ============================================================
-- Class Teacher assignment table.
-- One row per class; ClassTeacherID points at Employee.employmentID.
-- Re-runnable: only creates table / inserts rows that don't exist.
-- ============================================================

USE [Neat_Academy];
GO
SET NOCOUNT ON;
GO

-- 1. Create the table if it doesn't exist
IF OBJECT_ID('ClassAssignments') IS NULL
BEGIN
    CREATE TABLE ClassAssignments (
        ClassName       VARCHAR(50)  NOT NULL PRIMARY KEY,
        ClassTeacherID  INT          NULL,
        AssignedDate    DATE         NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        Notes           VARCHAR(200) NULL
    );
    PRINT 'ClassAssignments table created.';
END
ELSE
    PRINT 'ClassAssignments table already exists — skipping create.';
GO

-- 2. Seed initial assignments using the department-mapping heuristic.
--    Picks the alphabetically-first employee in each matching department.
--    Only inserts rows for classes that don't already have an assignment,
--    so manual edits on existing rows are preserved on re-run.
DECLARE @assignments TABLE (ClassName VARCHAR(50), Department VARCHAR(50));
INSERT INTO @assignments VALUES
('CRECHE',         'NURSERY'),
('NURSERY 1',      'NURSERY'),
('NURSERY 2',      'NURSERY'),
('KINDERGARTEN 1', 'KINDERGARTEN'),
('KINDERGARTEN 2', 'KINDERGARTEN'),
('BASIC 1',        'LOWER PRIMARY'),
('BASIC 2',        'LOWER PRIMARY'),
('BASIC 3',        'LOWER PRIMARY'),
('BASIC 4',        'UPPER PRIMARY'),
('BASIC 5',        'UPPER PRIMARY'),
('BASIC 6',        'UPPER PRIMARY'),
('BASIC 7',        'JHS (JUNIOR HIGH SCHOOL)'),
('BASIC 8',        'JHS (JUNIOR HIGH SCHOOL)'),
('BASIC 9',        'JHS (JUNIOR HIGH SCHOOL)');

INSERT INTO ClassAssignments (ClassName, ClassTeacherID)
SELECT
    a.ClassName,
    (SELECT TOP 1 e.employmentID
     FROM Employee e
     WHERE e.department = a.Department
     ORDER BY e.fullName)
FROM @assignments a
WHERE NOT EXISTS (
    SELECT 1 FROM ClassAssignments c WHERE c.ClassName = a.ClassName
);
GO

-- 3. Verify
SELECT
    ca.ClassName,
    ca.ClassTeacherID,
    ISNULL(e.fullName, '<unassigned>') AS [Class Teacher],
    e.department,
    ca.AssignedDate
FROM ClassAssignments ca
LEFT JOIN Employee e ON e.employmentID = ca.ClassTeacherID
ORDER BY ca.ClassName;

PRINT '';
PRINT 'Setup complete. To reassign a class teacher manually, run:';
PRINT '  UPDATE ClassAssignments SET ClassTeacherID = <employeeID> WHERE ClassName = ''<CLASS>'';';
GO
