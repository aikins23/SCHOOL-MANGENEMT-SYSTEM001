# Nyansapo ERP — Web Platform: Phase 0 → Deployment

**Document type:** Master roadmap / documentation plan
**Owner:** Buabeng Emmanuel Aikins
**Last updated:** 2026-06-20
**Status:** Living document — supersedes scattered phase notes; links out to the per-feature specs/plans under `docs/superpowers/`.

> Historical web roadmap. Its decisions and status snapshot describe the
> platform as of 2026-06-20 and are not the current release gate. Use
> `OUTSTANDING_FEATURES_TRACKER.md` and
> `CURRENT_IMPLEMENTATION_STATUS_JULY_2026.md` for current status. Current code,
> tested deployment configuration, and the security runbooks take precedence
> where this document differs.

This document records the web-platform plan that guided the `web/` solution. The existing WinForms desktop app is out of scope except where the two systems coexist (shared database, connection-string cutover).

---

## 1. Decisions register (authoritative)

These override anything older in `docs/superpowers/specs`. When code and an older spec disagree, this table wins.

| # | Decision | Status | Supersedes |
|---|----------|--------|------------|
| D1 | **Stack:** ASP.NET Core Blazor Server (Interactive Server), `net10.0`, EF Core + SQL Server provider. | Locked | spec said .NET 8 LTS; only .NET 10 (also LTS) installed |
| D2 | **Password compatibility:** reuse desktop PBKDF2/HMAC-SHA1 (`P2$` format, 100k iters, 8-byte salt, 16-byte hash); legacy plaintext fallback. | Locked & built | — |
| D3 | **Accountant on web = YES, but READ-ONLY.** Accountant may view Finance/Admissions dashboards; may **not** perform writes (Collect Fee, Record Expense, Approve Admission). | **Locked 2026-06-20** | reverses Phase-0 "Accountant denied" |
| D4 | **Tenancy = DB-per-school, hosted in an Azure SQL elastic pool.** Hard isolation per the sync north-star; `SchoolId` claim/column is **informational only**, never the sole security boundary. | **Locked 2026-06-20** | reconciles code's `SchoolId` filter with the sync architecture |
| D5 | **Offline-first:** schools run on a local primary DB; data syncs to the school's cloud DB (LWW by `UpdatedAt`, idempotent upsert by `SyncId`). SMS via persistent outbox. | Architecture approved; build in progress | — |
| D6 | **Login mechanism:** static-SSR `EditForm` POST → `HttpContext.SignInAsync` (cookie auth). | Built | plan's minimal-API handler |
| D7 | **Brute-force lockout:** per-username, 5 attempts → 15-min lock (in-memory; needs shared store if multi-instance). | Built 2026-06-20 | closes the spec's missing security item |

### Consequences of D3 (Accountant read-only) — action items
- [x] Gate the **New Admission / Collect Fee / Record Expense** quick-action buttons in `Home.razor` so they do **not** render for `Accountant` (2026-06-20).
- [x] **Admissions:** Accountant can view but Approve/Reject buttons are hidden **and** the `ApproveAdmission`/`DeleteDraft` handlers refuse writes server-side (`_canWrite` guard) (2026-06-20).
- [x] **Students/Edit** already restricted to `Administrator,Headmaster,Director` — Accountant excluded.
- [x] Added reusable `WriteAccess` authorization policy (authenticated, not Accountant/Parent) in `Program.cs` for future write-only pages.
- [x] Kept Accountant's **read** access to Finance/Admissions dashboards.

### Consequences of D4 (DB-per-school) — action items
- [ ] Treat `SchoolId` as display/audit/sync metadata, not a query filter that gates tenant isolation — **see the handling rules in §3.1**.
- [ ] One connection string per deployed school instance (App Service setting / Key Vault), pointing at that school's DB in the elastic pool.
- [ ] Provisioning automation creates a new DB in the pool per school (Phase 4 / SP-4).

---

## 2. Current status snapshot (2026-06-20)

**Build:** green on `net10.0`. **Tests:** 45/45 passing. **Known vulnerabilities:** none (ImageSharp on 3.1.11).

### Solution layout (`web/KingdomPrep.Web.slnx`)
| Project | Role |
|---------|------|
| `KingdomPrep.Web` | Blazor Server host: pages, layout/nav, auth wiring, DI, **sync API** endpoints. |
| `KingdomPrep.Web.Core` | Auth logic (`RoleParser`, `PasswordHasher`, `AuthService`, `ILoginThrottle`), DTOs, report-card generator. No EF/UI deps. |
| `KingdomPrep.Web.Data` | EF Core `AppDbContext`, entities mapped to legacy schema, repositories. |
| `KingdomPrep.DesignSystem` | Reusable Razor component library (`Kp*` components, design tokens). |
| `KingdomPrep.Web.Tests` | xUnit (auth, throttle, sync policy, repositories). |

Dependency direction: `Web → Core → Data`; `Web → DesignSystem`.

### What is actually built
- **Auth & shell:** cookie login with success modal, role-gated nav, lockout, anti-forgery, HTTPS redirect/HSTS, denied page, not-found/error pages.
- **Staff dashboard (`/`):** role-aware KPI tiles + recent payments + class enrollment + academic/leave insight — reading **real DB values** (see §2.1).
- **Students:** `Index` (search/filter), `Edit` (write), `ReportCards`.
- **Admissions:** `Index` with approve/convert-to-student flow (write; Accountant view-only per D3).
- **Finance:** `Dashboard`, `CollectFee` (write), `RecordExpense` (write).
- **Teacher Portal (built):** `Dashboard`, `Attendance`, `Grading`, `Assignments`, `Defaulters`, `Leave`, `ViewResults` under `/teacher/*`.
- **Sync API:** inbound sync endpoints + `SyncInboxService`, gated by `SyncTablePolicy` (~40 whitelisted tables).
- **Design system:** buttons, cards, inputs, modal, table, toast, badge, dropdown, spinner, theme provider.

### What is stubbed / not yet built
- **Parent portal:** placeholder card only (Phase 1 — needs parent→student linkage).
- **`ModuleComingSoon`** (`/module/{name}`): generic placeholder still used for any not-yet-built link.
- **Outbound sync, connectivity service, cloud provisioning, LAN multi-PC:** SP-2/SP-4/SP-5 not built.

### 2.1 Dashboards by role
Each role lands on a dashboard tailored to its remit. KPI/insight widgets read live DB values via `IDashboardStats`; the staff dashboard composes `AdminMetricCards`, `RecentPaymentsTable`, and `EnrollmentInsights`.

| Role | Landing | KPI tiles | Tables / insights | Quick actions |
|------|---------|-----------|-------------------|---------------|
| **Administrator** | `/` | Students, Employees, Revenue, Balance, **Income, Expenses, Fund, Top Expense** | Recent Payments, Class Enrollment, Academic & Leave Insight | New Admission, Collect Fee, Record Expense, Add Employee, Mark Attendance |
| **Director** | `/` | Students, Employees, Revenue, Balance, **Income, Expenses, Fund, Top Expense** | Recent Payments, Class Enrollment, Academic & Leave Insight | Add Employee |
| **Headmaster** | `/` | Students, Employees, Revenue, Balance | Recent Payments, Class Enrollment, Academic & Leave Insight | Add Employee, Mark Attendance |
| **Accountant** | `/` | Students, Employees, Revenue, Balance, **Income, Expenses, Fund, Top Expense** | Recent Payments, Class Enrollment, Academic & Leave Insight | **none — read-only (D3)** |
| **Teacher** | `/teacher/dashboard` | Assigned Classes, Total Students | My Classes table → Attendance, Grading, Assignments, Defaulters, Leave, Results | within portal pages |
| **Parent** | `/` | — | Placeholder card (Phase 1) | none |

> Financial tiles (Income/Expenses/Fund/Top Expense) render only for **Administrator, Director, Accountant** (`AdminMetricCards`). Headmaster sees operational tiles but not the income/expense set. Write quick-actions are gated by D3 (Accountant excluded).

### 2.2 Analytics & insights ("the analyst")
There is **no "Analyst" role** — analytics are dashboard widgets fed by `IDashboardStats`. Current analytics are **KPI tiles + summary tables** (server-rendered reads); **graphical charts are not yet on the web** (the desktop's `frmDashboardCharts` is a future port — see Phase 1/Open items).

| Metric / insight | Source (`IDashboardStats`) | Surfaced on |
|------------------|----------------------------|-------------|
| Student count | `GetStudentCountAsync` | Staff dashboard |
| Employee count | `GetEmployeeCountAsync` | Staff dashboard |
| Revenue (fees collected) | `GetTotalRevenueAsync` | Staff + Finance dashboard |
| Outstanding balance | `GetTotalBalanceAsync` | Staff + Finance dashboard |
| Total income | `GetTotalIncomeAsync` | Finance roles |
| Total expenses | `GetTotalExpensesAsync` | Finance roles |
| Fund (income − expenses) | derived | Finance roles |
| Top expense | `GetTopExpenseAsync` | Finance roles |
| Recent payments (latest 8) | `GetRecentPaymentsAsync` | Staff + Finance dashboard |
| Class enrollment (count + teacher) | `GetClassEnrollmentsAsync` | Staff dashboard (Enrollment Insights) |
| Average score / Top class | `GetAverageScoreAsync` | Staff dashboard (Academic Insight) |
| Pending leave count | `GetPendingLeaveCountAsync` | Staff dashboard (Leave Insight) |

Dashboard queries run **in parallel** (`Task.WhenAll`) to keep first paint fast.

---

## 3. Architecture overview

```
 Desktop (WinForms, .NET FmWk)  ─┐
                                 ├─▶  School LOCAL PRIMARY DB (SQL Express/LocalDB) ──┐
 LAN PCs ────────────────────────┘     + SmsOutbox, SyncId/UpdatedAt/RowVersion       │ sync engine
                                                                                      │ (push/pull, LWW)
                                                                                      ▼
 Web (Blazor Server, App Service) ───────────────────────────────▶  School CLOUD DB (Azure SQL, in elastic pool)
                                                                          ▲
                                                                   Cloud SMS worker (optional, single owner)
```

- **One cloud DB per school** (D4), grouped in an elastic pool for cost.
- **Web reads/writes the cloud DB directly** for online roles; the **desktop + LAN** operate on the local primary and sync up.
- **Conflict resolution:** last-write-wins by `UpdatedAt`; idempotent upsert by `SyncId`.

Reference specs: [Offline-first cloud sync architecture](superpowers/specs/2026-06-07-offline-first-cloud-sync-architecture.md), [Web Phase 0 design](superpowers/specs/2026-06-03-progress-webapp-phase0-foundation-design.md).

### 3.1 `SchoolId` handling (tenancy field) — rules

`SchoolId` is a `UNIQUEIDENTIFIER` (`Guid?`) column attached to tenant-owned tables (`Users`, `Students`, `Employee`, `Attendance`, `examss`, `StudentTermRemarks`, …). It tags which school a row belongs to. **Per D4, `SchoolId` is NOT the security boundary** — isolation comes from **one database per school**. `SchoolId` is defense-in-depth + sync routing + audit, never the sole gate. Handle it by these rules.

**How it flows today**
- **Source of truth:** `Users.SchoolId`. On login, `AuthService` reads it and `Login.razor` writes a `"SchoolId"` claim onto the principal. The web reads it from the **claim**, never from user input.
- **Read path:** portal repositories filter with the pattern
  `WHERE row.SchoolId == claimSchoolId || row.SchoolId == null`.
  The `|| == null` clause means **unstamped (legacy) rows are visible to everyone**.
- **Sync path:** `/api/sync/upload` cross-checks the `X-School-Id` header against the payload `SchoolId` and rejects mismatches; the shared API key is the (current) caller gate.

**Mandatory rules**

1. **DB-per-school is the boundary; `SchoolId` is secondary.** Never rely on a `SchoolId` filter alone for isolation. A connection string points at exactly one school's DB; that is what keeps tenants apart.
2. **Always derive `SchoolId` server-side from the authenticated claim.** Never accept it from a form, query string, or client field for a *read/write decision*. (Sync is the one inbound case, and it must be validated — see rule 6.)
3. **Stamp on write.** Every insert/update to a tenant table must set `SchoolId` from the authenticated claim (web) or the connected DB's school identity (sync/desktop, via SP-1). Do not leave new rows `NULL`.
4. **Treat `|| SchoolId == null` as a migration crutch, not a feature.** It is only safe because each DB holds one school. **It must never be copied into a shared-DB query** — there, `NULL` rows would leak across tenants. Plan to remove it after backfill (rule 5).
5. **Backfill, then tighten.** As part of SP-1, backfill existing `NULL` `SchoolId` rows to the owning school, verify zero `NULL`s remain, then drop the `|| == null` clause and make `SchoolId` `NOT NULL` on tenant tables.
6. **Validate `SchoolId` on inbound sync.** `X-School-Id` must equal the payload `SchoolId` **and** (target state, §7.B) the `SchoolId` bound to the caller's per-school key. Reject any mismatch; never trust a self-asserted `SchoolId`.
7. **Never expose `SchoolId` in the UI or URLs.** It is infrastructure metadata, not a user-facing value. No `?schoolId=` routes, no editable fields.
8. **Audit, don't authorize, with it.** Use `SchoolId` in logs/audit records (who/what/which school) and for sync routing — not as the access-control check (that is role + DB-per-school).

**Dev vs prod**
- **Dev:** a single `Neat_Academy` LocalDB may hold one school; the `|| == null` filter is why dev "just works" with unstamped data.
- **Prod:** one DB per school in the elastic pool. `SchoolId` is then largely redundant for isolation (the DB already is the boundary) but is retained for **sync correctness, audit, and as belt-and-braces** against misconfiguration.

> **Red flag:** if you ever find yourself adding a `SchoolId` filter *to make isolation work*, stop — that means two schools are sharing a database, which violates D4. Fix the deployment, not the query.

---

## 4. Phased roadmap

### Phase 0 — Foundation ✅ (complete)
**Goal:** deployable Blazor skeleton: desktop-compatible login + role-gated shell + read-only data path.
Delivered per [the Phase-0 plan](superpowers/plans/2026-06-03-progress-webapp-phase0-foundation.md): solution scaffold, `UserRole`/`RoleParser` (with desktop aliases), `PasswordHasher` (known-vector tested), EF read mapping, `AuthService`, cookie auth + login, role landing, parent placeholder.
**Deviations now ratified:** D3 (Accountant), D6 (login mechanism), plus the added `DesignSystem` project.
**Exit criteria:** ✅ build clean, ✅ auth tests pass, ✅ dashboard reads a real value.

### Phase 1 — Core web modules (IN PROGRESS)
**Goal:** the online roles can do real work against the cloud DB.
| Workstream | State | Remaining |
|-----------|-------|-----------|
| Dashboard | Built | tidy role gating per D3 |
| Students (Index/Edit) | Built | Edit excludes Accountant ✓ |
| Admissions | Built | Accountant view-only enforced ✓ (D3) |
| Finance dashboard | Built | Accountant read-only ✓ (D3) |
| Teacher portal | Built | `/teacher/*` (Dashboard, Attendance, Grading, Assignments, Defaulters, Leave, Results) live ✓; `/teacher/dashboard` redirect resolved ✓; confirm per-teacher write scoping |
| Parent portal | Placeholder | needs parent→student linkage (own spec) |
| Report cards (web) | Built | `/students/report-cards` page + Core generator; confirm print/export path |

**Exit criteria:** every nav target either works or is an honest "coming soon"; D3/D4 action items closed; authorization policies (not just hidden UI) enforced; integration tests for each write path.

### Phase 2 — Offline-first sync (SP-0 → SP-5)
Maps the north-star sub-projects. Build order: **SP-0 → SP-1 → SP-2 → SP-3 → (SP-4, SP-5)**.
| ID | Sub-project | State | Spec/plan |
|----|-------------|-------|-----------|
| SP-0 | SMS outbox (persist + flush + de-dup) | Plan exists | [plan](superpowers/plans/2026-06-07-sp0-sms-outbox.md) |
| SP-1 | Schema foundations (`SyncId`/`UpdatedAt`/`RowVersion`/`SyncState`) | Plan exists | [plan](superpowers/plans/2026-06-07-sp1-schema-foundations.md) |
| SP-2 | Connectivity + encrypted cloud config + scheduler | Not started | needs spec |
| SP-3 | Sync engine (push/pull, upsert-by-SyncId, LWW) | Inbound endpoints partly built (web side) | needs full spec for desktop side |
| SP-4 | Cloud provisioning (create/migrate/seed a school DB) | Not started | folds into Phase 4 |
| SP-5 | LAN multi-PC (secondary points at primary) | Not started | needs spec |

**Exit criteria:** a school runs offline for days, reconnects, and all new rows + queued SMS appear in its cloud DB with no dupes/losses; cloud-side edits flow back.

### Phase 3 — Security & pre-production hardening
- [x] Login lockout (D7).
- [x] ImageSharp patched.
- [x] **D3 enforced:** `WriteAccess` policy + Admissions server-side write guards + Home quick-action gating (2026-06-20). Remaining: apply `[Authorize(Policy="WriteAccess")]` to future write pages as they are built.
- [ ] Secrets out of source: user-secrets (dev), Key Vault / App Service settings (prod). Audit `appsettings*.json` and connection strings.
- [ ] Anti-forgery confirmed on every state-changing POST (login ✓; verify Students/Admissions/Finance writes and the sync endpoints).
- [x] **Sync endpoint authentication** — `X-Sync-Key` shared secret (fixed-time compare), table whitelist, `X-School-Id` cross-check. [ ] **Harden:** move to per-school keys with server-side `SchoolId` binding (§7.B).
- [ ] Cookie review: `Secure` + `HttpOnly` + `SameSite=Strict` ✓; confirm 8-hour expiry policy is desired.
- [ ] HSTS/HTTPS ✓ in non-dev; confirm behind App Service TLS.
- [ ] Dependency scan in CI (fail build on high-severity advisories).
- [ ] PII minimization: DTOs to the UI, not raw entities, on sensitive views.
- [ ] **Multi-instance note:** if scaling out App Service, replace the in-memory `LoginThrottle` and Blazor circuit affinity needs sticky sessions or a backplane.

**Exit criteria:** a security review (`/security-review`) passes with no high findings; pen-test of the login/sync surface.

### Phase 4 — Deployment & cutover (see §5 runbook)
Provision Azure SQL elastic pool + per-school DB, migrate data, configure App Service, smoke-test, repoint desktop at cutover.

### Phase 5 — Operations & onboarding
- Per-school **onboarding**: create DB in pool, run idempotent migrations, seed, issue connection string + admin credential.
- **Offboarding**: export BACPAC, drop DB from pool.
- Backups (Azure SQL PITR), monitoring/alerts, cost review of the pool, runbook for sync failures and SMS outbox backlog.

---

## 5. Deployment runbook (Phase 4, detailed)

> Target: publish `KingdomPrep.Web` to **Azure App Service** against a per-school **Azure SQL** DB in an **elastic pool**. Provider specifics can shift; this is the documented happy path.

### 5.1 Prerequisites
- Azure subscription; resource group (e.g. `rg-nyansapo-prod`).
- .NET 10 SDK locally; `sqlpackage` (BACPAC) and `az` CLI.
- Source connection: the school's `(localdb)\MSSQLLocalDB / Neat_Academy` (or SQL Express primary).

### 5.2 Provision Azure SQL (per school, pooled)
1. Create a logical SQL **server** (e.g. `sql-nyansapo-prod`) with an admin login (store in Key Vault).
2. Create an **elastic pool** (start small, e.g. Standard 50–100 eDTU) to host all school DBs.
3. For each school, create a DB **in the pool** (e.g. `school-<slug>`).
4. **Firewall:** allow App Service outbound (or use a private endpoint / VNet integration); deny public where possible.

### 5.3 Migrate schema + data (LocalDB → cloud DB)
1. Export the school's local DB to BACPAC:
   `sqlpackage /a:Export /scs:"Server=(localdb)\MSSQLLocalDB;Database=Neat_Academy;Trusted_Connection=True;Encrypt=False" /tf:school.bacpac`
2. Import into the school's cloud DB:
   `sqlpackage /a:Import /tsn:sql-nyansapo-prod.database.windows.net /tdn:school-<slug> /tu:<admin> /tp:<secret> /sf:school.bacpac`
3. Run **idempotent sync migrations** (SP-1: add `SyncId`/`UpdatedAt`/`RowVersion`/`SyncState`, `SmsOutbox`) against the cloud DB. **No EF migrations** create/alter legacy tables — schema is matched, not owned.
4. Spot-check row counts (Students/Employee/fees/payment_record) match source.

### 5.4 Configure the web app
1. App Service (Linux/Windows) on .NET 10; deploy via `dotnet publish` + zip-deploy or CI.
2. Set `ConnectionStrings__Default` as an **App Service setting** (or Key Vault reference) → the school's cloud DB, `Encrypt=True`.
3. Set environment to `Production` (enables HSTS, exception handler).
4. Secrets (BulkSMS/Arkesel keys, cloud admin) via Key Vault; never in source.
5. Confirm TLS/HTTPS-only and the cookie `SecurePolicy=Always` works behind App Service.

### 5.5 Smoke test (per role)
1. `/` → redirected to `/login`.
2. Staff login (desktop credentials) → dashboard shows real student count + tiles.
3. **Accountant** login → can **view** Finance/Admissions, **cannot** write (D3).
4. Parent login → placeholder.
5. Wrong password ×5 → lockout message (D7).
6. Sign out → `/login`.
7. Sync endpoint → authenticated push of a test row upserts idempotently (re-push = no dupe).

### 5.6 Cutover & coexistence
- At go-live, **repoint the desktop app's connection string** to the same school cloud DB (or keep desktop on the local primary with sync, per D5 — choose per customer).
- Single source of truth: desktop and web share the school's cloud DB (or local↔cloud via sync).

### 5.7 Rollback
- Keep the pre-cutover BACPAC. If go-live fails: repoint desktop back to LocalDB, take web offline, restore from BACPAC / Azure SQL PITR.

---

## 6. Testing strategy

| Layer | What | Where |
|-------|------|-------|
| Unit | RoleParser, PasswordHasher (known vector + legacy plaintext), AuthService, **LoginThrottle**, SyncTablePolicy | `KingdomPrep.Web.Tests` ✅ |
| Repository | Student/Admission repos over EF InMemory | ✅ (fixed fake-factory disposal bug 2026-06-20) |
| Integration | each write path (Students/Admissions/Finance) + authz (Accountant denied writes per D3) | **to add (Phase 1/3)** |
| Sync | idempotency (re-push no dupe), LWW direction, table-policy rejection | **to add (Phase 2)** |
| Manual/E2E | role login matrix, lockout, sign-out, dashboard real values | scripted checklist (§5.5) |
| Security | `/security-review`, dependency scan in CI | **to add (Phase 3)** |

**Gate:** `dotnet test web/KingdomPrep.Web.slnx` green + no high-severity advisories before any deploy.

---

## 7. Security features (implemented & to review)

This is the security catalogue for an internet-facing deployment — what is already in place, and what to look at before go-live. The *work* lives in Phase 3 (§4); this is the inventory and review list.

### A. Implemented controls
| Control | Where | Notes |
|---------|-------|-------|
| Password hashing | `Core/Auth/PasswordHasher.cs` | PBKDF2/HMAC-SHA1, 100k iters, constant-time compare; desktop-compatible. Legacy plaintext only as fallback. |
| Cookie auth hardening | `Program.cs` | `HttpOnly`, `SecurePolicy=Always`, `SameSite=Strict`, 8-hour expiry. |
| HTTPS + HSTS | `Program.cs` | `UseHttpsRedirection`; `UseHsts()` in non-dev. |
| Anti-forgery | `Program.cs` (`UseAntiforgery`) | Blazor SSR `EditForm` login is tokenized. Logout/sync explicitly opt out (POST + `SameSite=Strict` / API-key gated). |
| Brute-force lockout | `Core/Auth/LoginThrottle.cs` (D7) | 5 failed attempts → 15-min per-username lock. |
| RBAC + write policy | `Program.cs` `WriteAccess`, page `[Authorize]` | Role claims; **Accountant read-only** enforced in UI **and** server-side (D3). |
| No account enumeration | `Components/Pages/Login.razor` | Generic "invalid username or password" for both cases. |
| SQL injection resistance | EF Core repositories | Parameterized LINQ; no string-concatenated SQL. |
| Sync API auth | `Api/SyncEndpoints.cs` | `X-Sync-Key` shared secret, **fixed-time compare**, `503` if unset; `SyncTablePolicy` table whitelist; `X-School-Id` cross-checked against payload. |
| Dependency hygiene | `Core.csproj` | ImageSharp patched to 3.1.11 (no known advisories). |
| Secrets not in source | `appsettings.json` | Dev uses `Trusted_Connection` (no password); `Sync:ApiKey` blank in source. |
| Safe error pages | `Program.cs` | `/Error` (scoped) + status-code re-execution in non-dev — no stack traces to users. |

### B. To review / harden before production
| Area | Risk | Recommendation |
|------|------|----------------|
| **Per-school sync keys** | One shared `Sync:ApiKey` for all schools; `X-School-Id` is self-asserted, so a leaked key lets a caller claim another school's `SchoolId`. | Issue a **distinct key per school**; bind the key→`SchoolId` server-side and reject mismatches. |
| **Prod secrets** | Connection string, `Sync:ApiKey`, SMS (BulkSMS/Arkesel) keys must not be committed. | Key Vault / App Service settings; user-secrets in dev. Audit before each deploy. |
| **`AllowedHosts: "*"`** | Host-header attacks / cache poisoning. | Restrict to the app's real hostname(s) in prod config. |
| **Security response headers** | Missing `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`/CSP `frame-ancestors`, `Content-Security-Policy`. | Add a headers middleware; tune CSP for Blazor (it needs specific script/style allowances). |
| **Global rate limiting** | Only login is throttled; sync `/upload` + other POSTs are not. | `AddRateLimiter` with per-endpoint policies (esp. `/api/sync/*`). |
| **Multi-instance lockout** | In-memory `LoginThrottle` is bypassable across instances; Blazor circuits need affinity. | Shared store (Redis/DB) for throttle + sticky sessions or a backplane before scaling out. |
| **Session fixation** | Confirm a fresh auth cookie is issued on login (post-auth rotation). | Verify `SignInAsync` rotation; regenerate on privilege change. |
| **Audit logging** | No record of failed logins, lockouts, admission approvals, or sync uploads (who/when/what). | Structured audit log for security-relevant events; ship to a sink. |
| **Account lifecycle** | "Create a new account" link is a dead `#`; no password reset/rotation policy. | Remove or implement; define reset + rotation + disabled-account handling. |
| **Parent data isolation** | Phase 1 parent views must scope strictly to that parent's child. | Enforce parent→student scoping server-side, not in UI. |
| **TLS to cloud SQL** | Dev string uses `TrustServerCertificate=True`. | Prod connection: `Encrypt=True`, no cert bypass. |
| **Sync payload limits** | Large/abusive upload bodies = DoS. | Request body-size limits + payload validation on `/api/sync/upload`. |
| **CI security gates** | No automated dependency/secret scan. | Fail the build on high-severity advisories and committed secrets. |

### C. Pre-production security checklist (go/no-go)
- [ ] All prod secrets in Key Vault / App Service settings; nothing sensitive in source or `appsettings*.json`.
- [ ] Per-school sync key issued; `X-School-Id` binding enforced.
- [ ] `AllowedHosts` restricted; `Encrypt=True` on the cloud connection.
- [ ] Security headers middleware live (CSP tuned for Blazor).
- [ ] Rate limiting on `/api/sync/*` and other POST surfaces.
- [ ] Lockout backed by a shared store **iff** running multi-instance.
- [ ] Audit logging for login failures, lockouts, approvals, sync uploads.
- [ ] `/security-review` run with no high-severity findings; dependency + secret scan green in CI.
- [ ] Manual role matrix (incl. Accountant write-denied) passes (§5.5).

---

## 8. Risk register

| Risk | Impact | Mitigation |
|------|--------|------------|
| Accountant write reachable despite hidden UI | Data integrity / segregation-of-duties breach | Enforce with authorization **policy**, not UI hiding (D3 action items) |
| Shared sync key + self-asserted `X-School-Id` | Cross-school data injection if key leaks | Per-school keys bound to `SchoolId` server-side; keep `SyncTablePolicy` whitelist (§7.B) |
| In-memory lockout under multi-instance | Lockout bypass on scale-out | Shared store (Redis/DB) before horizontal scaling (D7 note) |
| `SchoolId` mistaken as the isolation boundary | Cross-tenant leak if pool DBs ever merged | D4: isolation is DB-per-school; `SchoolId` is informational only |
| Teacher per-class write scoping | A teacher acts on a class they don't own | Enforce server-side that grading/attendance writes are limited to the teacher's assigned classes |
| Clock skew affecting LWW | Wrong row wins | Stamp `UpdatedAt` from one source per write; tolerate small skew |
| Secrets in source/config | Credential leak | Key Vault / user-secrets; CI secret scan |

---

## 9. Open items (tracked)

1. ~~**D3 enforcement** — authorization policy + Home.razor gating for Accountant read-only.~~ ✅ Done 2026-06-20.
2. ~~**Teacher portal** — build the pages.~~ ✅ Built (`/teacher/*`). Remaining: enforce **per-teacher class write scoping** (see Risk register).
3. **Sync endpoint authn** — has a shared `X-Sync-Key` (good); upgrade to **per-school keys** with server-side `SchoolId` binding (see §7.B).
4. **SP-2/SP-3 desktop side** — connectivity service + outbound sync engine still need spec → plan.
5. **Parent linkage** — own spec before any parent data view.
6. **CI** — add build+test+dependency-scan pipeline; wire the deploy steps in §5.

---

## 10. Document map

- This file: master roadmap (Phase 0 → deployment).
- `docs/superpowers/specs/2026-06-03-progress-webapp-phase0-foundation-design.md` — Phase 0 design.
- `docs/superpowers/plans/2026-06-03-progress-webapp-phase0-foundation.md` — Phase 0 plan.
- `docs/superpowers/specs/2026-06-07-offline-first-cloud-sync-architecture.md` — sync north-star.
- `docs/superpowers/plans/2026-06-07-sp0-sms-outbox.md`, `…sp1-schema-foundations.md` — sync sub-projects.
- `web/README.md` — run/deploy quickstart (keep in sync with §5).
