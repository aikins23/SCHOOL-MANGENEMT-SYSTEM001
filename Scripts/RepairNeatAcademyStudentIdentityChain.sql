USE [Neat_Academy];
GO

/*
    Phase 1 live-data repair helper.

    Default mode is review-only. Set @Execute = 1 only after reviewing the
    result sets and confirming the rows are genuine orphan leftovers.
*/
DECLARE @Execute bit = 0;
DECLARE @RunId uniqueidentifier = NEWID();
DECLARE @StartedAt datetime2(0) = SYSUTCDATETIME();

IF OBJECT_ID(N'dbo.DataRepairArchive', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataRepairArchive
    (
        ArchiveID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        ArchivedAt datetime2(0) NOT NULL,
        TableName sysname NOT NULL,
        PrimaryKeyValue nvarchar(100) NOT NULL,
        Reason nvarchar(300) NOT NULL,
        RowJson nvarchar(max) NOT NULL
    );
END;

PRINT 'Nyansapo live-data identity-chain repair';
PRINT 'RunId: ' + CONVERT(varchar(36), @RunId);
PRINT 'Mode: ' + CASE WHEN @Execute = 1 THEN 'EXECUTE' ELSE 'REVIEW ONLY' END;

SELECT
    'fees' AS TableName,
    f.FeeID AS PrimaryKeyValue,
    f.StudentID,
    f.ClassID,
    f.FeeName,
    f.Amount,
    'Fee row references a missing Students.StudentID.' AS Issue
FROM dbo.fees f
LEFT JOIN dbo.Students s ON s.StudentID = f.StudentID
WHERE f.StudentID IS NOT NULL
  AND s.StudentID IS NULL
ORDER BY f.StudentID, f.FeeID;

SELECT
    'examss' AS TableName,
    e.sn AS PrimaryKeyValue,
    e.std_id AS StudentID,
    e.std_name,
    e.std_class,
    e.subject,
    e.term,
    e.[year],
    'Exam row references a missing or zero Students.StudentID.' AS Issue
FROM dbo.examss e
LEFT JOIN dbo.Students s ON s.StudentID = e.std_id
WHERE e.std_id IS NULL
   OR e.std_id = 0
   OR s.StudentID IS NULL
ORDER BY e.sn;

SELECT
    'Users' AS TableName,
    LOWER(u.Username) AS DuplicateKey,
    COUNT(*) AS DuplicateCount,
    'Case-insensitive duplicate usernames should be resolved manually per school scope.' AS Issue
FROM dbo.Users u
GROUP BY LOWER(u.Username)
HAVING COUNT(*) > 1
ORDER BY LOWER(u.Username);

IF @Execute = 1
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.DataRepairArchive (RunId, ArchivedAt, TableName, PrimaryKeyValue, Reason, RowJson)
        SELECT
            @RunId,
            @StartedAt,
            N'fees',
            CONVERT(nvarchar(100), f.FeeID),
            N'Fee row references a missing Students.StudentID.',
            (SELECT f.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        FROM dbo.fees f
        LEFT JOIN dbo.Students s ON s.StudentID = f.StudentID
        WHERE f.StudentID IS NOT NULL
          AND s.StudentID IS NULL;

        DELETE f
        FROM dbo.fees f
        LEFT JOIN dbo.Students s ON s.StudentID = f.StudentID
        WHERE f.StudentID IS NOT NULL
          AND s.StudentID IS NULL;

        INSERT INTO dbo.DataRepairArchive (RunId, ArchivedAt, TableName, PrimaryKeyValue, Reason, RowJson)
        SELECT
            @RunId,
            @StartedAt,
            N'examss',
            CONVERT(nvarchar(100), e.sn),
            N'Exam row references a missing or zero Students.StudentID.',
            (SELECT e.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        FROM dbo.examss e
        LEFT JOIN dbo.Students s ON s.StudentID = e.std_id
        WHERE e.std_id IS NULL
           OR e.std_id = 0
           OR s.StudentID IS NULL;

        DELETE e
        FROM dbo.examss e
        LEFT JOIN dbo.Students s ON s.StudentID = e.std_id
        WHERE e.std_id IS NULL
           OR e.std_id = 0
           OR s.StudentID IS NULL;

        COMMIT TRANSACTION;
        PRINT 'Repair completed and affected rows were archived in dbo.DataRepairArchive.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
ELSE
BEGIN
    PRINT 'Review-only mode. No rows were changed.';
END;

SELECT
    'fees orphan rows remaining' AS CheckName,
    COUNT(*) AS IssueCount
FROM dbo.fees f
LEFT JOIN dbo.Students s ON s.StudentID = f.StudentID
WHERE f.StudentID IS NOT NULL
  AND s.StudentID IS NULL
UNION ALL
SELECT
    'examss orphan/zero student rows remaining' AS CheckName,
    COUNT(*) AS IssueCount
FROM dbo.examss e
LEFT JOIN dbo.Students s ON s.StudentID = e.std_id
WHERE e.std_id IS NULL
   OR e.std_id = 0
   OR s.StudentID IS NULL;
GO
