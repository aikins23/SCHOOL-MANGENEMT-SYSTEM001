# School Information Settings — Design

**Date:** 2026-06-05
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The school's identity (name, address, P.O. Box, Ghana Post GPS address, phones, email,
logo) and its fee schedule (one-time admission fee + per-class term fees) are currently
**hardcoded and scattered** across the codebase:

- School name/address/phones appear as string literals in receipts (`frmFessPayment`,
  `frmPendingApprovals`), the login screen, dashboards, and `Models/SchoolInfo.cs`
  defaults consumed by report cards.
- The admission fee is a constant: `Common/AdmissionFees.Amount = 100m`.
- Per-class term fees are a `switch` statement: `StudentService.GetFeeForClass(classId)`
  (CRECHE 2000, NURSERY 1 3450, NURSERY 2 3750, KINDERGARTEN 1 3654,
  KINDERGARTEN 2 / BASIC 1–9 2423, default 1200).

Changing any of these requires a code edit and rebuild. The school needs to manage this
information from inside the app.

## Goal

Provide a **single source of truth** for school identity and fees, stored in the database,
editable through a settings form by Director/Administrator, that drives the existing
hardcoded call sites with **no signature changes** at those sites.

## Scope

In scope:
- School **identity**: Name, Address, P.O. Box, **GP Address (Ghana Post GPS)**, Phone 1,
  Phone 2, Email, Logo (uploadable image stored in DB).
- **Fees**: one-time Admission Fee + per-class Term Fee.
- A settings form (`frmSchoolInfo`) restricted to Director + Administrator.
- Retiring the hardcodes by delegating them to the new data (admission fee, per-class fee,
  report-card school info).

Out of scope (noted as follow-ups, not built here):
- Rewriting the elaborate `frmFessPayment` styled receipt header to pull from the new
  source (kept minimal — that file carries unrelated WIP). Receipt/SMS header adoption is
  a later, low-risk follow-up.
- The SMS **school abbreviation** ("KPS") stays in `Properties.Settings`
  (`SmsSchoolAbbreviation`), where it is already wired to SMS sender IDs and editable in
  the SMS settings form. It is **not** duplicated into this table to avoid a dual source of
  truth. It may be surfaced read-only on the form.

## Approach (chosen)

**Two tables + a cached static accessor.**

- `SchoolInformation` — single row (Id = 1) holding identity + admission fee + logo bytes.
- `ClassFees` — one row per class (`ClassName`, `TermFee`), seeded from current
  `GetFeeForClass` values. One-row-per-class so classes can change without a schema change.
- `Common/SchoolProfile` — static, lazily-loaded cache exposing the values **synchronously**
  (the fee lookup and receipt drawing are synchronous and frequent). Fail-safe: any DB read
  error returns the current hardcoded default, so a missing/broken table never breaks the
  app. `Refresh()` reloads after a save.

Rejected alternatives:
- **One table, fees as columns/JSON** — brittle; adding/renaming a class needs a schema or
  parser change.
- **Properties.Settings only (no table)** — user explicitly wants a table; logo bytes and
  per-class rows do not fit `user.config`.

## Data model

```sql
-- Single-row identity + admission fee
IF OBJECT_ID(N'SchoolInformation', N'U') IS NULL
CREATE TABLE SchoolInformation (
    Id            INT            NOT NULL PRIMARY KEY,   -- always 1
    Name          NVARCHAR(200),
    Address       NVARCHAR(250),
    PoBox         NVARCHAR(100),
    GpsAddress    NVARCHAR(50),                          -- Ghana Post GPS, e.g. AO-1234-5678
    Phone1        NVARCHAR(50),
    Phone2        NVARCHAR(50),
    Email         NVARCHAR(150),
    Logo          VARBINARY(MAX),
    AdmissionFee  MONEY,
    UpdatedDate   DATETIME
);

-- One row per class
IF OBJECT_ID(N'ClassFees', N'U') IS NULL
CREATE TABLE ClassFees (
    ClassName NVARCHAR(50) NOT NULL PRIMARY KEY,
    TermFee   MONEY
);
```

**Seeding (only when empty):**
- `SchoolInformation` row (Id=1) from current hardcoded defaults:
  Name `KINGDOM PREPARATORY SCHOOL`, Address `AKIM ODA- ABENASE`, PoBox `P. O. BOX 7 AKIM ODA`,
  GpsAddress empty, Phone1 `0548050141`, Phone2 `0246087609`, Email
  `noreply@kingdomprep.edu.gh`, Logo `NULL`, AdmissionFee `100`.
- `ClassFees` from `AppConfig.ClassNames`, each seeded with the value the current
  `GetFeeForClass` returns for that class.

**Caveats honored:**
- `MONEY` for fee columns; `decimal` in C#.
- `UpdatedDate` truncated to whole seconds before binding (MSOLEDBSQL rejects sub-second
  precision on SQL `datetime` — same `TruncateSeconds` pattern as `DraftAdmissionRepository`).
- Logo upload validated against `AppConfig.MaxPhotoSizeBytes` and
  `AppConfig.AllowedImageExtensions`.

## Components

### `Models/SchoolInformation.cs` (new)
Full identity + fee model: `Name, Address, PoBox, GpsAddress, Phone1, Phone2, Email,
Logo (byte[]), AdmissionFee (decimal), UpdatedDate`.
Existing `Models/SchoolInfo.cs` (Name/Location/PhoneNumbers/Logo) is retained for report
cards and populated from the new data.

### `Data/ISchoolInfoRepository.cs` + `Data/SchoolInfoRepository.cs` (new)
- `Task EnsureTablesAsync()` — create both tables + seed when empty.
- `Task<SchoolInformation> GetAsync()` — read the Id=1 row (returns defaults if no row).
- `Task<Dictionary<string,decimal>> GetClassFeesAsync()`.
- `Task SaveAsync(SchoolInformation info)` — upsert the Id=1 row.
- `Task SaveClassFeesAsync(IDictionary<string,decimal> fees)` — upsert each class row.
- OleDb, `AppConfig.ConnectionString`, mirrors `DraftAdmissionRepository` style.

### `Common/SchoolProfile.cs` (new — static cached accessor)
- Lazy load on first access (calls repository synchronously via `.GetAwaiter().GetResult()`
  once, then caches).
- Properties: `Name, Address, PoBox, GpsAddress, Phone1, Phone2, Phones (combined),
  Email, Logo (byte[]), AdmissionFee`.
- `decimal FeeForClass(string classId)` — cached dictionary lookup; falls back to the legacy
  switch defaults for unknown/missing classes.
- `void Refresh()` — clears the cache so the next access reloads (called after Save).
- **Every accessor is wrapped fail-safe**: on any exception, return the hardcoded default
  that the code uses today.

### Integration edits (retire hardcodes)
- `Common/AdmissionFees.cs`: `Amount` becomes `public static decimal Amount => SchoolProfile.AdmissionFee;`
  (all existing call sites unchanged).
- `Services/StudentService.GetFeeForClass(classId)`: body becomes
  `return SchoolProfile.FeeForClass(classId);` (keep the old switch as the fallback **inside
  `SchoolProfile`**, not duplicated).
- `Services/ReportCardDataService.cs`: replace `SchoolInfo = new SchoolInfo()` with a
  `SchoolInfo` populated from `SchoolProfile` (Name, Location = Address, PhoneNumbers =
  combined phones, Logo bytes). Report cards then print the configured identity + uploaded logo.
- Startup: call `SchoolInfoRepository.EnsureTablesAsync()` in the same path that ensures the
  other tables (alongside `DraftAdmissionRepository.EnsureTableAsync` / auth setup).

### `frmSchoolInfo.cs` (new — settings form)
- Constructor: `InitializeComponent(); if (!AuthService.RequireAccess("frmSchoolInfo", this)) return; ApplyTheme(); LoadAsync();`
  (mirrors `frmEmailSettings`).
- **Identity** group: textboxes — Name, Address, P.O. Box, **GP Address (Ghana Post GPS)**,
  Phone 1, Phone 2, Email; a logo `PictureBox` preview + "Upload Logo…" button
  (OpenFileDialog with image filter, size/extension validation; falls back to
  `Resources/school_logo.png` preview if none stored).
- **Fees** group: a numeric/textbox for Admission Fee; a `DataGridView` with columns
  `Class` (read-only) and `Term Fee` (editable), one row per class loaded from `ClassFees`.
- **Save**: validates (Name non-empty; Admission Fee and every Term Fee ≥ 0), writes both
  tables, calls `SchoolProfile.Refresh()`, shows a status message. **Cancel** closes.
- Themed with `AppConfig.Colors`; verified offline via the reflection render harness.

### RBAC + navigation
- `Services/AuthService.cs`: add `["frmSchoolInfo"] = new[] { Director, Administrator }` to
  `_formAccess`.
- `frmDashboard.cs`: add a "School Information" entry in the Settings area for
  Director/Administrator that opens `frmSchoolInfo`.

## Data flow

1. App start → `EnsureTablesAsync()` creates + seeds tables if absent.
2. Any read of fees/identity (`GetFeeForClass`, `AdmissionFees.Amount`, report card) →
   `SchoolProfile` (loads once from DB, then cached; fail-safe to defaults).
3. Director/Admin opens `frmSchoolInfo` → loads current values → edits → Save → DB upsert →
   `SchoolProfile.Refresh()` → subsequent reads reflect the new values.

## Error handling

- DB/table errors in `SchoolProfile` → return hardcoded defaults; the app keeps working.
- Save errors → caught, shown in the form status label, logged via `LoggerHelper`.
- Logo upload: reject oversize/invalid files with a clear message; storing no logo is valid
  (preview/report-card fall back to `Resources/school_logo.png`).

## Testing / verification

- Build clean: `dotnet build -clp:ErrorsOnly -nologo`.
- Offline render of `frmSchoolInfo` via the PowerShell reflection harness to confirm layout
  (Identity group, Fees grid, logo preview) without launching the app.
- Manual (user, running app): edit a class fee + admission fee, save, confirm a new
  admission/fee payment picks up the changed amounts; upload a logo, confirm it appears on a
  report card.
- Seeding idempotency: running `EnsureTablesAsync()` twice does not duplicate rows or
  overwrite user edits.

## Success criteria

- School identity and all fees are editable in-app by Director/Administrator and persisted in
  the database.
- `AdmissionFees.Amount`, `StudentService.GetFeeForClass`, and report-card school info read
  from the configured values with no call-site signature changes.
- GP (Ghana Post GPS) address is captured and available to report-card/receipt headers.
- A missing or unreadable table never crashes the app (fail-safe to current defaults).
