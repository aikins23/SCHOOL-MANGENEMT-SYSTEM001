/*
Run from SSMS or sqlcmd as a SQL Server sysadmin.

This promotes the attached Neat_Academy_LatestCandidate database to the live
Neat_Academy name. It first backs up and archives the current live database.

Expected before running:
- Neat_Academy exists and is the current small/reset database.
- Neat_Academy_LatestCandidate exists and contains the recovered/latest data.
- SQL logins nyansapo_app and nyansapo_test already exist.
*/

SET NOCOUNT ON;

DECLARE @ActiveDatabase sysname = N'Neat_Academy';
DECLARE @CandidateDatabase sysname = N'Neat_Academy_LatestCandidate';
DECLARE @Timestamp nvarchar(32) = REPLACE(REPLACE(REPLACE(CONVERT(nvarchar(19), GETDATE(), 120), '-', ''), ':', ''), ' ', '_');
DECLARE @ArchiveDatabase sysname = @ActiveDatabase + N'_BeforeCandidate_' + @Timestamp;
DECLARE @BackupPath nvarchar(4000) = N'C:\ProgramData\NyansapoRecovery\' + @ArchiveDatabase + N'.bak';
DECLARE @sql nvarchar(max);

IF IS_SRVROLEMEMBER(N'sysadmin') <> 1
    THROW 51000, 'Run this script as a SQL Server sysadmin.', 1;

IF DB_ID(@ActiveDatabase) IS NULL
    THROW 51001, 'Active database Neat_Academy was not found.', 1;

IF DB_ID(@CandidateDatabase) IS NULL
    THROW 51002, 'Candidate database Neat_Academy_LatestCandidate was not found.', 1;

IF SUSER_ID(N'nyansapo_app') IS NULL
    THROW 51003, 'Login nyansapo_app was not found. Run CreateNyansapoSqlLogins.sql first.', 1;

IF SUSER_ID(N'nyansapo_test') IS NULL
    THROW 51004, 'Login nyansapo_test was not found. Run CreateNyansapoSqlLogins.sql first.', 1;

IF DB_ID(@ArchiveDatabase) IS NOT NULL
    THROW 51005, 'Archive database name already exists. Run again to generate a new timestamp.', 1;

PRINT 'Pre-promotion row-count comparison:';
IF OBJECT_ID('tempdb..#Counts') IS NOT NULL DROP TABLE #Counts;
CREATE TABLE #Counts(DatabaseName sysname NOT NULL, TableName sysname NOT NULL, [RowCount] bigint NOT NULL);

DECLARE @db sysname;
DECLARE dbs CURSOR LOCAL FAST_FORWARD FOR
SELECT name FROM sys.databases WHERE name IN (@ActiveDatabase, @CandidateDatabase);

OPEN dbs;
FETCH NEXT FROM dbs INTO @db;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'
        INSERT INTO #Counts(DatabaseName, TableName, [RowCount])
        SELECT N''' + REPLACE(@db, '''', '''''') + N''', t.name, SUM(p.rows)
        FROM ' + QUOTENAME(@db) + N'.sys.tables t
        INNER JOIN ' + QUOTENAME(@db) + N'.sys.partitions p
            ON p.object_id = t.object_id AND p.index_id IN (0, 1)
        WHERE t.name IN (N''Students'', N''Users'', N''fees'', N''payment_record'', N''TimetableEntries'', N''examss'', N''DraftAdmissions'', N''SmsOutbox'', N''Notices'')
        GROUP BY t.name;';
    EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM dbs INTO @db;
END
CLOSE dbs;
DEALLOCATE dbs;

SELECT
    COALESCE(a.TableName, c.TableName) AS TableName,
    ISNULL(a.[RowCount], 0) AS ActiveRows,
    ISNULL(c.[RowCount], 0) AS CandidateRows,
    ISNULL(c.[RowCount], 0) - ISNULL(a.[RowCount], 0) AS CandidateMinusActive
FROM (SELECT TableName, [RowCount] FROM #Counts WHERE DatabaseName = @ActiveDatabase) a
FULL OUTER JOIN (SELECT TableName, [RowCount] FROM #Counts WHERE DatabaseName = @CandidateDatabase) c
    ON c.TableName = a.TableName
ORDER BY TableName;

PRINT 'Creating safety backup: ' + @BackupPath;
SET @sql = N'BACKUP DATABASE ' + QUOTENAME(@ActiveDatabase) +
    N' TO DISK = N''' + REPLACE(@BackupPath, '''', '''''') + N''' WITH INIT, FORMAT, NAME = N''Nyansapo pre-promotion backup'', STATS = 10;';
EXEC sys.sp_executesql @sql;

PRINT 'Archiving current live database as ' + @ArchiveDatabase;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ActiveDatabase) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
EXEC sys.sp_executesql @sql;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ActiveDatabase) + N' MODIFY NAME = ' + QUOTENAME(@ArchiveDatabase) + N';';
EXEC sys.sp_executesql @sql;

PRINT 'Promoting candidate database to ' + @ActiveDatabase;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@CandidateDatabase) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
EXEC sys.sp_executesql @sql;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@CandidateDatabase) + N' MODIFY NAME = ' + QUOTENAME(@ActiveDatabase) + N';';
EXEC sys.sp_executesql @sql;

PRINT 'Returning databases to multi-user mode.';
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ArchiveDatabase) + N' SET MULTI_USER;';
EXEC sys.sp_executesql @sql;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ActiveDatabase) + N' SET MULTI_USER;';
EXEC sys.sp_executesql @sql;

PRINT 'Mapping Nyansapo SQL logins into promoted database.';
SET @sql = N'
USE ' + QUOTENAME(@ActiveDatabase) + N';
IF USER_ID(N''nyansapo_app'') IS NULL CREATE USER [nyansapo_app] FOR LOGIN [nyansapo_app];
ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_app];
ALTER ROLE [db_datawriter] ADD MEMBER [nyansapo_app];
ALTER ROLE [db_ddladmin] ADD MEMBER [nyansapo_app];
IF USER_ID(N''nyansapo_test'') IS NULL CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];
ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_test];';
EXEC sys.sp_executesql @sql;

PRINT 'Post-promotion live student population:';
SET @sql = N'SELECT COUNT(*) AS StudentPopulation FROM ' + QUOTENAME(@ActiveDatabase) + N'.dbo.Students;';
EXEC sys.sp_executesql @sql;

SELECT name AS DatabaseName, state_desc AS StateDesc
FROM sys.databases
WHERE name IN (@ActiveDatabase, @ArchiveDatabase)
ORDER BY name;

PRINT 'Promotion complete.';
