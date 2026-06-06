# Universal "KPS+ID" Student ID Display — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The student ID is shown inconsistently. `StudentRepository`'s two list queries emit a
hardcoded `'KPS' + CAST(StudentID AS VARCHAR) AS [STUDENT ID]`, so the **View Students**
grid shows `KPS9016` — but everywhere else (dashboard **Recent Payments**, **student
details**, **Payment History**, **Outstanding Fees**, receipts, report cards, SMS/email)
shows the raw number `9016`. The school wants the `KPS9016` form **everywhere**, and
consistent.

## Goal

Display the student ID as `{Abbrev}{number}` (e.g. `KPS9016`) **everywhere** it appears, where
`{Abbrev}` is the configurable school abbreviation. Accept either form (`KPS9016` or `9016`)
on input/search. Do this as a **display concern only** — the database keeps storing the
numeric ID.

## Key constraint (why display-only)

`StudentID` is an `int IDENTITY(1,1)` primary key in `Students`, referenced as `int` in
`payment_record`, `fees`, `examss` (`std_id`), `Rolled_Out_Students`, and indexes. Changing
the stored key to a string would require converting those columns and every int-parsing call
site — a large, risky migration. **Out of scope.** We format at the view layer and keep all
data numeric.

## Approach (chosen)

**Central helper + view-layer formatting.** A `Common/StudentId` helper provides the prefix,
the parse, and a one-line grid hook. Grids are formatted *display-only* (underlying cell value
stays numeric, so every existing `row["ID"]` / `Cells["ID"].Value` readback keeps working and
numeric sort/filter is preserved). Text/labels/receipts/report-cards/SMS wrap with `Display`.
Inputs/search run through `Parse`. The two hardcoded SQL `'KPS'` aliases are removed so the
prefix is single-sourced and configurable.

Rejected:
- **Rewrite `[ID]` values to `"KPS9016"` strings** in queries/DataTables — breaks every numeric
  readback and kills numeric sort/filter.
- **Separate SQL display column** (`'KPS'+id AS [Student ID]`) — not configurable from SQL and
  forces column-layout rework in every grid.

## Components

### `Common/StudentId.cs` (new)
- `static string Abbrev` → `AppConfig.Sms.SchoolAbbreviation` (currently "KPS", configurable).
- `static string Display(object id)` → `Abbrev + id`. Returns "" for null/blank. Idempotent:
  if the value already starts with `Abbrev` (case-insensitive), it is returned unchanged (no
  double-prefix).
- `static string Parse(string input)` → trims, strips a leading `Abbrev` (case-insensitive) and
  surrounding spaces, returns the numeric string used for DB queries. Non-numeric remainder is
  returned as-is (caller validates).
- `static void AttachGridFormatting(DataGridView grid, params string[] idColumns)` → wires a
  `CellFormatting` handler that, for the named columns, sets `e.Value = Display(e.Value)` and
  `e.FormattingApplied = true`. **Display-only** — the bound `DataTable` value is untouched.

### Apply across the app (view layer)
- **Grids — `AttachGridFormatting`:**
  - Dashboard **Recent Payments** (`recentPaymentsGrid`, column `ID`).
  - **View Students** (`frmStdView`, column `STUDENT ID` — see SQL change below).
  - **Payment History** (`frmPaymentHistory`, column `STUDENT ID`).
  - **Outstanding Fees** (`frmOutstandingFees`, column `ID`).
  - **Attendance** (`frmAttendance`, the `ID` column) and any other grid showing the student ID.
- **Text / labels / inputs that double as display — `Display`:**
  - **Student details** (`frmStdDetails`: `txtStdID.Text = StudentId.Display(row["ID"])`).
  - Fee receipt **STUDENT ID** tile (`frmFessPayment`), and the on-form student-ID display.
  - **Report card** header student-ID line.
  - **SMS / email** bodies that include the student ID (registration, payment, admission).
- **Inputs / search — `Parse`:**
  - `frmFessPayment` lookup box (accept `KPS9016` or `9016`).
  - Student search, `EXAMS` student lookup, and any "enter student ID" field used for a DB query.
  - `frmOutstandingFees` CSV export writes the **Display** value for consistency.

### `Data/StudentRepository.cs` (change)
Replace the two `'KPS' + CAST(StudentID AS VARCHAR) AS [STUDENT ID]` aliases (lines ~280 and
~422) with `StudentID AS [STUDENT ID]` (numeric). `frmStdView` then formats that column via
`AttachGridFormatting(grid, "STUDENT ID")`. This removes the hardcoded prefix so the abbreviation
is single-sourced and configurable, and avoids double-prefixing.

## Data flow

1. A grid binds its `DataTable` (numeric IDs) → `AttachGridFormatting` prefixes the displayed
   text only → user sees `KPS9016`; `row["ID"]` readback still returns `9016`.
2. A label/receipt/report-card/SMS calls `StudentId.Display(id)` at render time.
3. A lookup/search field passes its text through `StudentId.Parse(...)` before querying, so
   `KPS9016` and `9016` both resolve to `9016`.

## Error handling / edge cases

- Null/blank id → "" (no bare "KPS").
- Already-prefixed value → not double-prefixed (idempotent `Display`).
- `Parse` is case-insensitive ("kps9016", "KPS9016") and tolerant of stray spaces.
- Numeric sort/filter unaffected (grid values stay numeric).
- If the abbreviation setting is blank, it falls back to "KPS" (existing `SchoolAbbreviation`
  behavior).

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Unit-style probe (PowerShell reflection) for `StudentId`: `Display(9016)=="KPS9016"`,
  `Display("KPS9016")=="KPS9016"`, `Display(null)==""`, `Parse("KPS9016")=="9016"`,
  `Parse(" kps9016 ")=="9016"`, `Parse("9016")=="9016"`.
- Manual (user, running app): every listed surface shows `KPS9016`; the fee lookup accepts both
  `KPS9016` and `9016`; selecting a grid row and acting on it still works (readback numeric);
  CSV export shows `KPS9016`.

## Success criteria

- The student ID renders as `{configurable-abbrev}{number}` on **all** listed surfaces,
  consistently.
- Lookups/searches accept the prefixed or bare numeric form.
- No database migration; all stored IDs and `row["ID"]` readbacks remain numeric.
- The prefix comes from one configurable source; the hardcoded SQL `'KPS'` is gone.
