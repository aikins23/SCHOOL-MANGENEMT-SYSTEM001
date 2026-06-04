# KingdomPrep.Web — Progress Web Platform (Phase 0: Foundation)

Blazor Server web platform for Kingdom Preparatory School. Phase 0 is the
foundation: role-based login over the existing `Neat_Academy` database, plus a
role-gated app shell. No module features and no writes yet (later phases).

## Projects
- **KingdomPrep.Web** — Blazor Server app (UI, auth wiring, DI).
- **KingdomPrep.Web.Core** — auth logic: `UserRole`, `RoleParser`, `PasswordHasher` (PBKDF2-SHA1, desktop-compatible), `AuthService`.
- **KingdomPrep.Web.Data** — EF Core `AppDbContext` + repositories mapped to the existing legacy schema (read-only).
- **KingdomPrep.Web.Tests** — xUnit tests (run with `dotnet test`).

## Prerequisites
- .NET 10 SDK (LTS).
- SQL Server LocalDB running with the `Neat_Academy` database (the desktop app's DB).

## Run (development)
```bash
cd web
dotnet test                       # all unit tests pass
cd KingdomPrep.Web
dotnet run                        # or: dotnet run --urls http://localhost:5099
```
Open the printed URL. You are redirected to `/login`. Sign in with the **same
username/password as the desktop app** (the web verifies the existing PBKDF2
hashes). Accountant accounts are blocked (redirected to `/denied`). Parents log
in to a placeholder (their child's progress arrives in Phase 1).

Dev connection string is in `KingdomPrep.Web/appsettings.json`
(`ConnectionStrings:Default` → `(localdb)\MSSQLLocalDB / Neat_Academy`).

## Production (Azure SQL + App Service) — outline
1. Create an **Azure SQL Database**; add a firewall rule for the App Service.
2. Migrate `Neat_Academy` to it (e.g. export a **BACPAC** from LocalDB via
   SqlPackage / SSMS, import into Azure SQL).
3. Deploy `KingdomPrep.Web` to **Azure App Service**; set
   `ConnectionStrings:Default` (and any secrets) as App Service configuration —
   never in source.
4. At cutover, **repoint the desktop app's connection string** to the same
   Azure SQL DB so desktop and web share one source of truth.
5. Ensure HTTPS only (the app already enforces HSTS + secure cookies).

## Notes
- The EF context is **read-only** for the existing schema. Do **not** run
  `dotnet ef migrations` against it — the desktop app owns the schema.
- Password compatibility is covered by a known-answer test in
  `KingdomPrep.Web.Tests/PasswordHasherTests.cs`.
