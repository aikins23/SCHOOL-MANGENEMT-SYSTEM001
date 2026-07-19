/*
Run this script from SSMS as a SQL Server sysadmin after enabling mixed
authentication mode and restarting SQL Server.

Before running:
1. Pass APP_PASSWORD and TEST_PASSWORD as sqlcmd variables.
2. Confirm Neat_Academy is the live local database name.

Use Enable-NyansapoSqlServer.ps1. It prompts securely and provides the sqlcmd
variables through the child-process environment instead of command arguments.
*/

USE [master];
GO

IF SUSER_ID(N'nyansapo_app') IS NULL
BEGIN
    CREATE LOGIN [nyansapo_app]
    WITH PASSWORD = N'$(APP_PASSWORD)',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
ELSE
BEGIN
    ALTER LOGIN [nyansapo_app]
    WITH PASSWORD = N'$(APP_PASSWORD)',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

ALTER LOGIN [nyansapo_app] ENABLE;
GO

IF SUSER_ID(N'nyansapo_test') IS NULL
BEGIN
    CREATE LOGIN [nyansapo_test]
    WITH PASSWORD = N'$(TEST_PASSWORD)',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
ELSE
BEGIN
    ALTER LOGIN [nyansapo_test]
    WITH PASSWORD = N'$(TEST_PASSWORD)',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

ALTER LOGIN [nyansapo_test] ENABLE;
GO

IF DB_ID(N'Neat_Academy') IS NOT NULL
BEGIN
    EXEC(N'
USE [Neat_Academy];

IF USER_ID(N''nyansapo_app'') IS NULL
    CREATE USER [nyansapo_app] FOR LOGIN [nyansapo_app];

IF IS_ROLEMEMBER(N''db_datareader'', N''nyansapo_app'') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_app];
IF IS_ROLEMEMBER(N''db_datawriter'', N''nyansapo_app'') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER [nyansapo_app];
IF IS_ROLEMEMBER(N''db_ddladmin'', N''nyansapo_app'') <> 1
    ALTER ROLE [db_ddladmin] ADD MEMBER [nyansapo_app];

IF USER_ID(N''nyansapo_test'') IS NULL
    CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];

IF IS_ROLEMEMBER(N''db_datareader'', N''nyansapo_test'') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_test];
');
END
GO

IF DB_ID(N'Neat_Academy_LatestCandidate') IS NOT NULL
BEGIN
    EXEC(N'
USE [Neat_Academy_LatestCandidate];

IF USER_ID(N''nyansapo_app'') IS NULL
    CREATE USER [nyansapo_app] FOR LOGIN [nyansapo_app];

IF IS_ROLEMEMBER(N''db_datareader'', N''nyansapo_app'') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_app];
IF IS_ROLEMEMBER(N''db_datawriter'', N''nyansapo_app'') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER [nyansapo_app];
IF IS_ROLEMEMBER(N''db_ddladmin'', N''nyansapo_app'') <> 1
    ALTER ROLE [db_ddladmin] ADD MEMBER [nyansapo_app];

IF USER_ID(N''nyansapo_test'') IS NULL
    CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];

IF IS_ROLEMEMBER(N''db_datareader'', N''nyansapo_test'') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_test];
');
END
GO

USE [master];
GO

IF USER_ID(N'nyansapo_test') IS NULL
    CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];
GO

IF IS_SRVROLEMEMBER(N'dbcreator', N'nyansapo_test') <> 1
    ALTER SERVER ROLE [dbcreator] ADD MEMBER [nyansapo_test];
GO
