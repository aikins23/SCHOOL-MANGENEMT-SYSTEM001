param(
    [string]$Server = "localhost",
    [string]$ActiveDatabase = "Neat_Academy",
    [string]$CandidateDatabase = "Neat_Academy_LatestCandidate",
    [string]$BackupDirectory = "C:\ProgramData\NyansapoRecovery",
    [string]$LogPath = "",
    [switch]$Execute
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $LogPath) {
    $LogPath = Join-Path (Split-Path -Parent $scriptRoot) "logs\PromoteLatestNeatAcademyCandidate.log"
}

$logDirectory = Split-Path -Parent $LogPath
if ($logDirectory -and -not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

if (-not (Test-Path -LiteralPath $BackupDirectory)) {
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
}

Start-Transcript -Path $LogPath -Force | Out-Null

try {
function New-ConnectionString {
    # Promotion requires an elevated Windows account with SQL sysadmin rights.
    # SQL passwords are deliberately unsupported to keep credentials out of
    # process arguments, transcripts, and recovery logs.
    return "Data Source=$Server;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;Connect Timeout=8"
}

function Invoke-Sql([string]$connectionString, [string]$script) {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.add_InfoMessage({
        param($sender, $eventArgs)
        if ($eventArgs.Message) { Write-Host $eventArgs.Message }
    })

    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 600
        $command.CommandText = $script
        $reader = $command.ExecuteReader()
        $resultNumber = 0
        do {
            if ($reader.FieldCount -le 0) { continue }
            $resultNumber++
            $rows = New-Object System.Collections.Generic.List[object]
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                    $value = $reader.GetValue($i)
                    if ($value -is [DBNull]) { $value = $null }
                    $row[$reader.GetName($i)] = $value
                }
                $rows.Add([pscustomobject]$row)
            }
            if ($rows.Count -gt 0) {
                Write-Host ""
                Write-Host "Result set $resultNumber"
                $rows | Format-Table -AutoSize | Out-String -Width 4096 | Write-Host
            }
        } while ($reader.NextResult())
        $reader.Close()
    }
    finally {
        $connection.Close()
    }
}

function Escape-SqlLiteral([string]$value) {
    return $value.Replace("'", "''")
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$archiveDatabase = "${ActiveDatabase}_BeforeCandidate_$timestamp"
$backupPath = Join-Path $BackupDirectory "${ActiveDatabase}_BeforeCandidate_$timestamp.bak"

$escapedActive = Escape-SqlLiteral $ActiveDatabase
$escapedCandidate = Escape-SqlLiteral $CandidateDatabase
$escapedArchive = Escape-SqlLiteral $archiveDatabase
$escapedBackupPath = Escape-SqlLiteral $backupPath

$mode = if ($Execute) { "EXECUTE" } else { "DRY RUN" }
Write-Host "Promotion mode: $mode"
Write-Host "Active database: $ActiveDatabase"
Write-Host "Candidate database: $CandidateDatabase"
Write-Host "Archive database: $archiveDatabase"
Write-Host "Safety backup: $backupPath"

$verifySql = @"
SET NOCOUNT ON;

DECLARE @ActiveDatabase sysname = N'$escapedActive';
DECLARE @CandidateDatabase sysname = N'$escapedCandidate';
DECLARE @sql nvarchar(max);

IF DB_ID(@ActiveDatabase) IS NULL
    THROW 51000, 'Active database was not found.', 1;

IF DB_ID(@CandidateDatabase) IS NULL
    THROW 51001, 'Candidate database was not found. Attach and compare it first.', 1;

SELECT name AS DatabaseName, state_desc AS StateDesc, SUSER_SNAME(owner_sid) AS OwnerName
FROM sys.databases
WHERE name IN (@ActiveDatabase, @CandidateDatabase)
ORDER BY name;

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
"@

$promoteSql = @"
SET NOCOUNT ON;

DECLARE @ActiveDatabase sysname = N'$escapedActive';
DECLARE @CandidateDatabase sysname = N'$escapedCandidate';
DECLARE @ArchiveDatabase sysname = N'$escapedArchive';
DECLARE @BackupPath nvarchar(4000) = N'$escapedBackupPath';
DECLARE @sql nvarchar(max);

IF DB_ID(@ActiveDatabase) IS NULL
    THROW 51000, 'Active database was not found.', 1;

IF DB_ID(@CandidateDatabase) IS NULL
    THROW 51001, 'Candidate database was not found. Attach and compare it first.', 1;

IF DB_ID(@ArchiveDatabase) IS NOT NULL
    THROW 51002, 'Archive database name already exists. Retry with a new timestamp.', 1;

PRINT 'Creating safety backup before promotion...';
SET @sql = N'BACKUP DATABASE ' + QUOTENAME(@ActiveDatabase) +
    N' TO DISK = N''' + REPLACE(@BackupPath, '''', '''''') + N''' WITH INIT, FORMAT, NAME = N''Nyansapo pre-promotion backup'', STATS = 10;';
EXEC sys.sp_executesql @sql;

PRINT 'Renaming active database to archive name...';
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ActiveDatabase) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
EXEC sys.sp_executesql @sql;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ActiveDatabase) + N' MODIFY NAME = ' + QUOTENAME(@ArchiveDatabase) + N';';
EXEC sys.sp_executesql @sql;

PRINT 'Promoting candidate database to active name...';
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@CandidateDatabase) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
EXEC sys.sp_executesql @sql;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@CandidateDatabase) + N' MODIFY NAME = ' + QUOTENAME(@ActiveDatabase) + N';';
EXEC sys.sp_executesql @sql;

PRINT 'Returning databases to MULTI_USER...';
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ArchiveDatabase) + N' SET MULTI_USER;';
EXEC sys.sp_executesql @sql;
SET @sql = N'ALTER DATABASE ' + QUOTENAME(@ActiveDatabase) + N' SET MULTI_USER;';
EXEC sys.sp_executesql @sql;

PRINT 'Repairing Nyansapo SQL users on promoted database...';
SET @sql = N'
USE ' + QUOTENAME(@ActiveDatabase) + N';
IF SUSER_ID(N''nyansapo_app'') IS NOT NULL
BEGIN
    IF USER_ID(N''nyansapo_app'') IS NULL CREATE USER [nyansapo_app] FOR LOGIN [nyansapo_app];
    ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_app];
    ALTER ROLE [db_datawriter] ADD MEMBER [nyansapo_app];
    ALTER ROLE [db_ddladmin] ADD MEMBER [nyansapo_app];
END;
IF SUSER_ID(N''nyansapo_test'') IS NOT NULL
BEGIN
    IF USER_ID(N''nyansapo_test'') IS NULL CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];
    ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_test];
END;';
EXEC sys.sp_executesql @sql;

SELECT name AS DatabaseName, state_desc AS StateDesc
FROM sys.databases
WHERE name IN (@ActiveDatabase, @ArchiveDatabase)
ORDER BY name;

PRINT 'Promotion complete. Restart the desktop app before testing login and workflows.';
"@

$connectionString = New-ConnectionString
Invoke-Sql -connectionString $connectionString -script $verifySql

if (-not $Execute) {
    Write-Host ""
    Write-Host "Dry run only. No database was renamed or replaced."
    Write-Host "Rerun with -Execute only after confirming the row-count comparison and backup path."
    exit 0
}

Invoke-Sql -connectionString $connectionString -script $promoteSql
}
finally {
    Stop-Transcript | Out-Null
}
