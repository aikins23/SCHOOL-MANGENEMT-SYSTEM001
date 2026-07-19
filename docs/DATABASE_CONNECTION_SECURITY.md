# Database Connection Security

Last updated: 2026-07-19

## Implemented

- Removed the live SQL login and password from desktop tracked configuration.
- Replaced the desktop default with a password-free encrypted Windows-authentication fallback.
- Added `DatabaseConnectionSettings` as the single desktop connection resolver.
- Added a current-user DPAPI protected connection file.
- Added a secure setup script that verifies the database before saving the protected value.
- Enabled SQL encryption in local desktop, web, recovery, and database-promotion defaults.
- Added a production fail-closed rule requiring certificate validation.
- Removed password output and transcript logging from the SQL login bootstrap helper.
- Added a secure full-test prompt so the test password is not placed in shell history.
- Added automated tests for environment priority, protected-file secrecy, and production transport enforcement.
- Migrated and verified the current workstation connection in DPAPI without printing the SQL password.
- Removed the retired credential marker from generated workspace configuration files.
- Rotated the live `nyansapo_app` credential and proved the retired credential is rejected.
- Bound the `localhost` server-authentication certificate to `MSSQLSERVER` and enabled forced encryption.
- Trusted the certificate on this workstation and independently verified a certificate-validated DPAPI connection with `TrustServerCertificate=False`.
- Confirmed the protected connection still reaches `Neat_Academy` and reads 178 student rows.
- Rewrote the distributable Git history to remove retired secrets and added automated working-tree/history secret scanning.

## Resolution and Trust Model

The desktop trusts deployment-provided configuration in this order:

1. `NYANSAPO_CONNECTION_STRING`, for process-scoped deployment or CI injection.
2. `%LOCALAPPDATA%\Nyansapo ERP\database.connection`, encrypted with DPAPI for the current Windows user.
3. The password-free local fallback in `App.config`.

The protected file cannot be moved to another Windows account or machine and decrypted there. Each installed workstation must be configured under the Windows user that runs Nyansapo.

## Still Required Before Commercial Launch

1. Accept and push only the rewritten clean repository, then invalidate and remove retained pre-rewrite clones and packages.
2. Before any client connects from another machine, replace the workstation-local `localhost` certificate with a CA-issued certificate for the deployed SQL DNS name, or securely distribute the private CA trust anchor to every authorized client.
3. Add certificate expiry monitoring and rotate the current certificate before 4 February 2031.

The current certificate is trusted locally and is valid only for the DNS name `localhost`. It provides certificate validation for applications running on this SQL Server workstation. It is not, by itself, a complete trust solution for remote desktop or web servers.

## Maintenance Commands

Run the controlled administrator operation through Windows UAC:

```powershell
.\Scripts\Invoke-NyansapoSqlSecurityMaintenanceAdmin.ps1
```

Run the separate least-privilege verification under the Windows user that owns the DPAPI connection:

```powershell
.\Scripts\Test-NyansapoProductionSqlSecurity.ps1
```

Neither script writes a password or full connection string to its evidence files.
