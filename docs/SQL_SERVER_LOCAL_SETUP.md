# SQL Server Local Setup

Nyansapo uses Microsoft SQL Server as the database engine and `Microsoft.Data.SqlClient` as the .NET connection driver.

## Security Rules

- Never place a real SQL password in `App.config`, `appsettings.json`, a command argument, a transcript, source control, or documentation.
- Use separate `nyansapo_app` and `nyansapo_test` SQL logins.
- SQL encryption is always enabled.
- `TrustServerCertificate=True` is allowed only during initial local certificate setup.
- Production must use `Encrypt=True;TrustServerCertificate=False` with a certificate whose name matches the SQL Server host.

## Configure Local SQL Server

Open an elevated PowerShell window and run:

```powershell
.\Enable-NyansapoSqlServer.ps1
```

The helper securely prompts for the two SQL login passwords. It does not accept plaintext password strings by default, write a transcript, print a connection string, or place the passwords on the `sqlcmd` command line.

It performs these local administrator actions:

1. Enables mixed SQL Server authentication.
2. Enables TCP/IP and Named Pipes.
3. Configures TCP port 1433.
4. Restarts `MSSQLSERVER`.
5. Creates or rotates `nyansapo_app` and `nyansapo_test` using `Scripts/CreateNyansapoSqlLogins.sql`.

## Protect the Desktop Connection

After the SQL certificate has been trusted and bound, run:

```powershell
.\Scripts\Set-NyansapoDesktopConnection.ps1 `
  -Server localhost `
  -Database Neat_Academy `
  -Username nyansapo_app
```

The script prompts for the password, verifies the connection, and stores it with Windows DPAPI at:

```text
%LOCALAPPDATA%\Nyansapo ERP\database.connection
```

Only the same Windows user on the same machine can decrypt that protected value. To remove it:

```powershell
.\Scripts\Set-NyansapoDesktopConnection.ps1 `
  -Server localhost `
  -Database Neat_Academy `
  -Username nyansapo_app `
  -Remove
```

Production startup rejects unencrypted or certificate-bypassing connections when `NYANSAPO_ENVIRONMENT=Production`.

## Rotate the App Login and Enforce Local Certificate Validation

Use the reviewed administrator wrapper to bind the certificate, enable forced encryption, rotate the app login, reject the retired password, and update the DPAPI connection atomically:

```powershell
.\Scripts\Invoke-NyansapoSqlSecurityMaintenanceAdmin.ps1
```

Then perform a separate read-only verification:

```powershell
.\Scripts\Test-NyansapoProductionSqlSecurity.ps1
```

The current `localhost` certificate is trusted on this workstation only. Remote clients require a certificate for the deployed DNS name and a trust chain installed on each authorized client.

## Connection Resolution Order

The desktop resolves its connection in this order:

1. Process environment variable `NYANSAPO_CONNECTION_STRING`.
2. Current-user DPAPI file configured above.
3. Password-free Windows-authentication fallback in `App.config`.

The environment variable is intended for controlled deployment injection and CI. Do not save a password-bearing value in a profile script or commit it to a file.

## Full Desktop Integration Tests

Run the full suite without placing the test password in shell history:

```powershell
.\Run-DesktopTests.ps1 -Full -TrustServerCertificate
```

The runner securely prompts for the `nyansapo_test` password, creates an encrypted process-only master connection, passes it only to the child test process, and clears it after the run.

When SQL Server has a trusted certificate, omit `-TrustServerCertificate`.

## Production Certificate Gate

Before production deployment:

1. Install a server-authentication certificate on SQL Server.
2. Set the SQL Server service certificate and restart the service.
3. Connect using the certificate host name, not `localhost` or a raw IP address.
4. Configure `Encrypt=True;TrustServerCertificate=False`.
5. Set `NYANSAPO_ENVIRONMENT=Production`.
6. Verify desktop and web startup, login, database health, and certificate expiry monitoring.
