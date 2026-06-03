# Progress Web Platform — Phase 0: Foundation — Design

**Date:** 2026-06-03
**Status:** Approved (design)
**Scope:** Phase 0 of a multi-phase web platform. This spec covers ONLY the foundation. Later phases (academic progress, attendance, students, HR/leave, dashboards) each get their own spec → plan → build cycle.

## Context

The existing product is a **WinForms desktop app** (.NET Framework 4.7.2) over a **SQL Server LocalDB** database `Neat_Academy`. The goal is a **web platform** giving every role except Accountant — Director, Administrator, Headmaster, Teacher — plus **Parents** access to manage/view student academic progress online. "Full web management" was chosen, which is a multi-month platform, so it is phased.

**Phase 0 delivers a deployable skeleton:** a hosted database + a Blazor Server app + role-based login (reusing existing credentials) + a role-gated app shell. No module data and no writes yet — those are Phase 1+.

## Decisions

- **Stack:** ASP.NET Core **Blazor Server**, **.NET 8 (LTS)**. Full-stack C#; reuse `Models` POCOs, the `UserRole` enum, and the PBKDF2 password logic.
- **Data access:** **EF Core** (parameterized, modern), read-only in Phase 0, entities mapped to the existing legacy schema/column names.
- **Prod database:** **Azure SQL Database** (managed, internet-reachable, cheap tier). **Dev:** the app points at the existing `(localdb)\MSSQLLocalDB / Neat_Academy` to read real data immediately. Connection string is configuration-driven.
- **Coexistence:** in production the desktop app **repoints its connection string to the same hosted Azure SQL DB**, so desktop and web share one source of truth.
- **Accountant** can authenticate but is **denied** the web app. **Parents** authenticate; their landing is a placeholder until Phase 1 (parent→student linkage is Phase 1).

## Architecture

Three projects in a new solution `KingdomPrep.Web.sln` (separate from the desktop solution; lives in a `web/` folder in the repo):

- **`KingdomPrep.Web`** (Blazor Server) — UI: login, role-based layout/nav, landing dashboards, auth wiring, DI, config.
- **`KingdomPrep.Web.Core`** — `UserRole` enum (copy), DTOs (`AuthUser`), `PasswordHasher` (reproduces the desktop PBKDF2 verify), `IAuthService`/`AuthService`, role-policy constants. No EF/UI dependencies.
- **`KingdomPrep.Web.Data`** — EF Core `AppDbContext`, entity classes (`UserEntity`, `StudentEntity`, `EmployeeEntity`) mapped to existing tables/columns, and repository(ies) the Core services consume.

Dependency direction: `Web → Core → Data`. Core defines interfaces; Data implements data access; Web wires DI and renders.

### Password compatibility (critical)
The desktop `AuthService` (`AuthService.cs:434`) stores `P2${iterations}${base64Salt}${base64Hash}` produced by `new Rfc2898DeriveBytes(password, salt, iterations)` — i.e. **PBKDF2 / HMAC-SHA1**, iterations = **100000**, salt = **8 bytes**, hash = **16 bytes**. The web `PasswordHasher.Verify(password, stored)` MUST reproduce these exactly and pass `HashAlgorithmName.SHA1` explicitly on modern .NET (the algorithm-less constructor is obsolete there), parse the `P2$…` format, recompute, and compare in **constant time** — otherwise existing accounts fail to log in. The plan includes a test that verifies a known desktop-created hash string.

## Data

- **EF entities** map to the existing schema, including legacy names (e.g. `Students.GuidianceEmail`, `Students.EmergencyConatct`) via `[Column]`/Fluent config. Phase 0 maps only `Users`, `Students`, `Employee` (and only the columns needed for auth + a count for the dashboard).
- **No EF migrations against the legacy DB** — the DbContext is configured to match the existing schema (`HasNoKey`/explicit keys/column maps); we do not let EF create or alter tables.
- **DB migration to prod** (deploy step, detailed in plan): create the Azure SQL DB, copy schema+data from LocalDB (SqlPackage/BACPAC or generate-scripts), repoint both apps' connection strings.

## Auth & access

- **Cookie authentication** (ASP.NET Core). A login page collects username + password; `AuthService` loads the user from `Users`, verifies via `PasswordHasher`, and on success signs in a `ClaimsPrincipal` with a `role` claim.
- **Authorization policies** mirror the desktop RBAC. A global rule denies `Accountant` from the web app (redirect to an "use the desktop app" notice). Unauthenticated users are redirected to login.
- **Login throttling / lockout** (track failed attempts; temporary lock) to resist brute force on an internet-facing login.

## App shell (Phase 0 deliverable)

- **Login page** (branding, username/password, error states).
- **Role-based landing:** staff → a staff dashboard placeholder (cards: Students, Academic, Attendance, HR — disabled "coming soon"); parent → a parent dashboard placeholder. Header shows school name/logo and "signed in as {name} ({role})" + logout.
- **Layout:** top bar + role-filtered side nav (nav items for future modules shown disabled). Responsive enough for desktop + tablet.

## Security (internet-facing)

HTTPS enforced (HSTS); secure, HttpOnly, SameSite cookies; anti-forgery on the login post; password lockout; secrets via user-secrets (dev) and environment/Key Vault (prod), never in source; EF parameterization (no raw SQL concatenation); minimal data exposure (DTOs, not entities, to the UI).

## Deployment

MVP target: publish `KingdomPrep.Web` to **Azure App Service** (or IIS on a VPS) pointing at the Azure SQL DB. The plan documents: create Azure SQL + firewall rules, migrate the DB, set the connection string as an App Service setting, publish, smoke-test. Provider specifics can shift, but Azure SQL + App Service is the documented happy path.

## Testing / verification

- **Unit:** `PasswordHasher.Verify` against a known desktop-generated `P2$…` hash (must pass); role→policy mapping; accountant-denied rule.
- **Integration/manual:** run locally against LocalDB; log in as each role; confirm role-based landing, accountant block, logout, and an invalid-login error. Confirm the dashboard reads at least one real value from the DB (e.g. student count) to prove the data path.

## Out of scope (Phase 0)

- Any module functionality (exams/grades, report cards, students CRUD, attendance, HR/leave, analytics).
- **All writes** — Phase 0 is read-only + auth.
- Parent→student linkage and parent data views (Phase 1).
- Mobile app.
- Migrating the desktop app off .NET Framework (it stays as-is; only its connection string changes at prod cutover).
