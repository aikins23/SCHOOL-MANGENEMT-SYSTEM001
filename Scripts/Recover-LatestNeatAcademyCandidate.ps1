param(
    [string]$Server = "localhost",
    [string]$ActiveDatabase = "Neat_Academy",
    [string]$CandidateDatabase = "Neat_Academy_LatestCandidate",
    [string]$SourceMdf = "$env:USERPROFILE\Downloads\database\database\Neat_Academy.mdf",
    [string]$SourceLdf = "$env:USERPROFILE\Downloads\database\database\Neat_Academy_log.ldf",
    [string]$LogPath = "",
    [switch]$UseSqlCmd
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $LogPath) {
    $LogPath = Join-Path (Split-Path -Parent $scriptRoot) "logs\RecoverLatestNeatAcademyCandidate.log"
}

$logDirectory = Split-Path -Parent $LogPath
if ($logDirectory -and -not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

Start-Transcript -Path $LogPath -Force | Out-Null

try {

function Find-SqlCmd {
    $candidates = @(
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE",
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE",
        "C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SQLCMD.EXE",
        "sqlcmd.exe"
    )

    foreach ($candidate in $candidates) {
        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    throw "sqlcmd was not found. Install SQL Server command-line tools or run this from a Developer PowerShell with sqlcmd available."
}

function Escape-SqlLiteral([string]$value) {
    return $value.Replace("'", "''")
}

function New-ConnectionString {
    # Recovery requires an elevated Windows account with SQL sysadmin rights.
    # SQL passwords are deliberately unsupported to keep credentials out of
    # process arguments, transcripts, and recovery logs.
    return "Data Source=$Server;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;Connect Timeout=8"
}

function Invoke-SqlClientScript([string]$connectionString, [string]$script) {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.add_InfoMessage({
        param($sender, $eventArgs)
        if ($eventArgs.Message) {
            Write-Host $eventArgs.Message
        }
    })

    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
        $command.CommandText = $script

        $reader = $command.ExecuteReader()
        $resultNumber = 0
        do {
            if ($reader.FieldCount -le 0) {
                continue
            }

            $resultNumber++
            $rows = New-Object System.Collections.Generic.List[object]
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                    $value = $reader.GetValue($i)
                    if ($value -is [DBNull]) {
                        $value = $null
                    }

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

if (-not (Test-Path -LiteralPath $SourceMdf)) {
    throw "Source MDF was not found: $SourceMdf"
}

if (-not (Test-Path -LiteralPath $SourceLdf)) {
    throw "Source LDF was not found: $SourceLdf"
}

$escapedActive = Escape-SqlLiteral $ActiveDatabase
$escapedCandidate = Escape-SqlLiteral $CandidateDatabase
$escapedMdf = Escape-SqlLiteral (Resolve-Path -LiteralPath $SourceMdf).Path
$escapedLdf = Escape-SqlLiteral (Resolve-Path -LiteralPath $SourceLdf).Path

$sql = @"
SET NOCOUNT ON;

DECLARE @ActiveDatabase sysname = N'$escapedActive';
DECLARE @CandidateDatabase sysname = N'$escapedCandidate';
DECLARE @Mdf nvarchar(4000) = N'$escapedMdf';
DECLARE @Ldf nvarchar(4000) = N'$escapedLdf';
DECLARE @sql nvarchar(max);

PRINT 'Active database: ' + @ActiveDatabase;
PRINT 'Candidate database: ' + @CandidateDatabase;
PRINT 'Candidate MDF: ' + @Mdf;
PRINT 'Candidate LDF: ' + @Ldf;

IF DB_ID(@CandidateDatabase) IS NULL
BEGIN
    SET @sql = N'CREATE DATABASE ' + QUOTENAME(@CandidateDatabase) +
        N' ON (FILENAME = N''' + REPLACE(@Mdf, '''', '''''') + N'''), ' +
        N'(FILENAME = N''' + REPLACE(@Ldf, '''', '''''') + N''') FOR ATTACH;';
    EXEC sys.sp_executesql @sql;
    PRINT 'Attached candidate database successfully.';
END
ELSE
BEGIN
    PRINT 'Candidate database already exists. It was not re-attached.';
END

PRINT '';
PRINT 'Database files currently visible to SQL Server:';
SELECT
    DB_NAME(database_id) AS DatabaseName,
    type_desc AS FileType,
    physical_name AS PhysicalName
FROM sys.master_files
WHERE DB_NAME(database_id) IN (@ActiveDatabase, @CandidateDatabase)
ORDER BY DatabaseName, FileType;

PRINT '';
PRINT 'Row-count comparison for important school ERP tables:';

IF OBJECT_ID('tempdb..#Counts') IS NOT NULL
    DROP TABLE #Counts;

CREATE TABLE #Counts
(
    DatabaseName sysname NOT NULL,
    TableName sysname NOT NULL,
    [RowCount] bigint NOT NULL
);

DECLARE @db sysname;
DECLARE dbs CURSOR LOCAL FAST_FORWARD FOR
SELECT name
FROM sys.databases
WHERE name IN (@ActiveDatabase, @CandidateDatabase);

OPEN dbs;
FETCH NEXT FROM dbs INTO @db;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'
        INSERT INTO #Counts (DatabaseName, TableName, [RowCount])
        SELECT
            N''' + REPLACE(@db, '''', '''''') + N''',
            t.name,
            SUM(p.rows)
        FROM ' + QUOTENAME(@db) + N'.sys.tables AS t
        INNER JOIN ' + QUOTENAME(@db) + N'.sys.partitions AS p
            ON p.object_id = t.object_id
           AND p.index_id IN (0, 1)
        WHERE t.name IN
        (
            N''Students'', N''Student'', N''StudentRecords'', N''Admissions'', N''Fees'',
            N''Payments'', N''PaymentHistory'', N''Notice'', N''Notices'', N''ExamResults'',
            N''ReportCards'', N''StudentRemarks'', N''Timetable'', N''TimetableEntries'',
            N''ClassWorkloads'', N''Employees'', N''Users'', N''SchoolProfile''
        )
        GROUP BY t.name;';
    EXEC sys.sp_executesql @sql;

    FETCH NEXT FROM dbs INTO @db;
END

CLOSE dbs;
DEALLOCATE dbs;

SELECT
    COALESCE(active.TableName, candidate.TableName) AS TableName,
    ISNULL(active.[RowCount], 0) AS ActiveRows,
    ISNULL(candidate.[RowCount], 0) AS CandidateRows,
    ISNULL(candidate.[RowCount], 0) - ISNULL(active.[RowCount], 0) AS CandidateMinusActive
FROM (SELECT TableName, [RowCount] FROM #Counts WHERE DatabaseName = @ActiveDatabase) AS active
FULL OUTER JOIN (SELECT TableName, [RowCount] FROM #Counts WHERE DatabaseName = @CandidateDatabase) AS candidate
    ON candidate.TableName = active.TableName
ORDER BY TableName;

PRINT '';
PRINT 'No live database was replaced. If CandidateRows contain the missing latest data, promote only after taking a fresh backup of the active database.';
"@

Write-Host "Running candidate attach and comparison..."

if ($UseSqlCmd) {
    $sqlcmd = Find-SqlCmd
    $tempSql = Join-Path $env:TEMP ("RecoverLatestNeatAcademyCandidate_{0:yyyyMMdd_HHmmss}.sql" -f (Get-Date))
    Set-Content -Path $tempSql -Value $sql -Encoding UTF8

    Write-Host "Using sqlcmd: $sqlcmd"
    $args = @("-S", $Server, "-N", "o", "-C", "-b", "-i", $tempSql)
    $args += "-E"

    & $sqlcmd @args
    exit $LASTEXITCODE
}

$connectionString = New-ConnectionString
Write-Host "Using .NET SqlClient against $Server."
Invoke-SqlClientScript -connectionString $connectionString -script $sql
}
finally {
    Stop-Transcript | Out-Null
}
