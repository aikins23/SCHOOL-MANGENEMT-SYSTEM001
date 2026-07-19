# KingdomPrep.Web - Nyansapo ERP Companion Platform

Blazor Server web platform for Kingdom Preparatory School. This companion portal
provides role-based access for parents, teachers, accountants, administrators,
headmasters, and directors against the central `Neat_Academy` database.

## Current Status

- Named authorization policies protect real write surfaces instead of scattered role strings.
- Parent fee payment requires Paystack public and secret keys, plus server-side transaction verification.
- Desktop sync endpoints use per-device API keys and school/device scoping.
- The desktop app still owns the main legacy school schema.

## Projects

- **KingdomPrep.Web** - Main Blazor Server application, UI, auth, sync API, deployment schema initializer.
- **KingdomPrep.DesignSystem** - Shared UI components and Nyansapo ERP branding.
- **KingdomPrep.Web.Core** - Business logic, authorization policy constants, authentication, reporting.
- **KingdomPrep.Web.Data** - EF Core `AppDbContext` and repositories over the existing school schema.
- **KingdomPrep.Web.Tests** - Unit and repository tests.

## Prerequisites

- .NET 10 SDK.
- SQL Server database initialized with the desktop-owned `Neat_Academy` schema.
- Production configuration for:
  - `ConnectionStrings:Default`
  - `Paystack:PublicKey`
  - `Paystack:SecretKey`
  - `Sync:ProvisioningKey`
  - `Sync:AllowSharedApiKeyFallback=false`
  - `Security:AllowLegacyPlainTextPasswords=false`
  - `Security:SyncUploadMaxBytes`
  - `ForwardedHeaders:KnownProxies` when behind a trusted reverse proxy
  - restricted `AllowedHosts`

In non-development environments the app fails fast when production-critical
configuration is missing or unsafe. Production must not use LocalDB, localhost,
blank Paystack keys, blank sync provisioning keys, wildcard `AllowedHosts`, or
shared sync-key fallback. It also blocks legacy plaintext password fallback and
invalid sync upload size limits in production.

## Running the Portal

```bash
cd web
dotnet build
cd KingdomPrep.Web
dotnet run
```

Log in using standard school credentials. Permissions are enforced by named
policies from `KingdomPrep.Web.Core.Auth.WebPermission`.

## Deployment Schema

The web app does not run EF migrations. At startup it only ensures web-owned
support tables exist:

- `ExamSetups`
- `AuditLogs`
- `SyncRegisteredDevices`
- `SyncInbox`
- sync support tables used by desktop/web synchronization

Main tables such as `Students`, `Users`, `payment_record`, `emp_leave`,
`examss`, and other desktop-owned tables must already exist before deployment.

## Synchronization

The web portal serves as the cloud anchor. The desktop application uses the sync
API to push offline changes to the web database and pull web changes back down.

Device registration requires `Sync:ProvisioningKey`. Each registered device
must use a unique sync API key with at least 32 characters.

Desktop installations must keep their local database for offline operation.
The web database is the central synchronization target, not a replacement for
the desktop runtime database.

## Developer Notes

- Do not add EF migrations for desktop-owned tables.
- Web-owned support schema is initialized by `WebSchemaInitializer`.
- Write pages should use named policies, not hard-coded role strings.
