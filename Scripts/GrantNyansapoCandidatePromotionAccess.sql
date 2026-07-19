/*
Run this from SSMS as a SQL Server sysadmin before promoting
Neat_Academy_LatestCandidate to Neat_Academy.

Why this exists:
- The app/test SQL logins can access Neat_Academy.
- The attached candidate database may not contain users mapped to those logins.
- Promotion must not continue until the app login can open the candidate.
*/

USE [master];
GO

IF SUSER_ID(N'nyansapo_app') IS NULL
    THROW 51010, 'Login nyansapo_app does not exist. Run CreateNyansapoSqlLogins.sql first.', 1;

IF SUSER_ID(N'nyansapo_test') IS NULL
    THROW 51011, 'Login nyansapo_test does not exist. Run CreateNyansapoSqlLogins.sql first.', 1;

IF DB_ID(N'Neat_Academy_LatestCandidate') IS NULL
    THROW 51012, 'Database Neat_Academy_LatestCandidate is not attached.', 1;
GO

USE [Neat_Academy_LatestCandidate];
GO

IF USER_ID(N'nyansapo_app') IS NULL
    CREATE USER [nyansapo_app] FOR LOGIN [nyansapo_app];

ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_app];
ALTER ROLE [db_datawriter] ADD MEMBER [nyansapo_app];
ALTER ROLE [db_ddladmin] ADD MEMBER [nyansapo_app];
GO

IF USER_ID(N'nyansapo_test') IS NULL
    CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];

ALTER ROLE [db_datareader] ADD MEMBER [nyansapo_test];
GO

USE [Neat_Academy];
GO

IF USER_ID(N'nyansapo_test') IS NULL
    CREATE USER [nyansapo_test] FOR LOGIN [nyansapo_test];

ALTER ROLE [db_backupoperator] ADD MEMBER [nyansapo_test];
GO

USE [master];
GO

GRANT ALTER ANY DATABASE TO [nyansapo_test];
GO

PRINT 'Nyansapo candidate promotion access is ready.';
