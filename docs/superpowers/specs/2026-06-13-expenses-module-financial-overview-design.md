# Expenses Module + Financial Overview — Design

**Date:** 2026-06-13
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The database has an `Expenses` table (12 legacy rows) and the dashboard already charts it
(`GetMonthlyIncomeVsExpensesAsync`, `GetExpenseByCategoryAsync`), but there is **no screen to record
or manage expenses** — the data could only be entered through the retired legacy system. The school
also has no at-a-glance **financial position** (income vs expenses vs net funds) anywhere in the app.

## Goal

1. A management screen to record/list/edit/delete expenses, feeding the existing dashboard charts.
2. A **Total Income / Total Expenses / Total Fund** summary for a **selectable date window** — shown
   on the Expenses form (with the window selector) and as three tiles on the main dashboard
   (current term) — visible only to finance/leadership roles.

## Decisions (from brainstorming)

- **Category** = a **configurable list** (`ExpenseCategories`) shown as an editable combo on the
  form: pick an existing category or type a new one, which is saved to the list on submit. This is
  the `Purpose` column the dashboard groups by.
- **Access** = **Accountant, Administrator, Director** (Headmaster excluded).
- **Totals period** = **current term by default**, with a selectable window (This Term / This Year /
  All Time / Custom from–to). The selector lives on the **Expenses form** (the Director's "selective
  window") and the **dashboard** shows the same three tiles fixed to the current term.

## Reused facts

- `Expenses` columns: `ID int, Expenses_name varchar(50), Purpose varchar(50), Description
  varchar(100), Date_Time date, Amount varchar(15), Payee varchar(30), payer varchar(30)`.
- `Amount` is **varchar** and the dashboard reads it via
  `TRY_CAST(REPLACE(ISNULL(Amount,'0'), ',', '') AS DECIMAL(18,2))` — so the form must **store a
  comma-free numeric string** to keep charts working.
- Income source: `payment_record.Amount_paid` summed over `payment_record.[Date]`.
- Term calendar already exists: `AppConfig.Leave.CurrentTerm` → `(TermName, Start, End)` and
  `AppConfig.Leave.GetTerm(date)` (Sep–Dec / Jan–Apr / May–Aug).

## Components

### `Common/ExpenseAmount.cs` (pure, unit-tested)
- `static decimal Parse(string raw)` — strips commas/whitespace/currency, `decimal.TryParse`, → 0 on
  failure. Mirrors the SQL `REPLACE`/`TRY_CAST` so UI and DB agree.
- `static string Store(decimal amount)` — invariant, no commas, two decimals (e.g. `"1500.00"`).

### `Common/FinancePeriod.cs` (pure, unit-tested)
- `enum FinanceWindow { Term, Year, AllTime, Custom }`.
- `static (DateTime From, DateTime To, string Label) Resolve(FinanceWindow w, DateTime today,
  DateTime? customFrom, DateTime? customTo)`:
  - Term → `AppConfig.Leave.GetTerm(today)` Start/End, label = term name.
  - Year → Jan 1 – Dec 31 of `today.Year`, label = the year.
  - AllTime → `DateTime.MinValue`–`DateTime.MaxValue`, label "All time".
  - Custom → the two dates (From ≤ To enforced), label "dd MMM yyyy – dd MMM yyyy".

### `Models/Expense.cs`
- `Id, Name, Category, Description, Date, Amount(decimal), Payee, Payer`.

### `Data/ExpenseRepository.cs`
- `EnsureTablesAsync()` — idempotent create of `ExpenseCategories (Name NVARCHAR(60) PRIMARY KEY)`
  and seed defaults (`Salaries, Utilities, Supplies, Maintenance, Transport, Rent, Repairs,
  Miscellaneous`) when empty. (The `Expenses` table already exists; not created here.)
- `GetByRangeAsync(DateTime from, DateTime to, string category=null)` → `List<Expense>`
  (Amount via `ExpenseAmount.Parse`).
- `AddAsync(Expense)` / `UpdateAsync(Expense)` / `DeleteAsync(int id)` — write `Amount` via
  `ExpenseAmount.Store`; OleDb positional params.
- `GetCategoriesAsync()` → `List<string>`; `AddCategoryAsync(string)` (ignore duplicate/blank).
- `GetTotalExpensesBetweenAsync(from, to)` → decimal (SQL `SUM(TRY_CAST(REPLACE(...)))`).

### `Data/DashboardRepository.cs` (extend `IDashboardRepository`)
- `GetTotalIncomeBetweenAsync(from, to)` → Σ `payment_record.Amount_paid` where `[Date]` in range.
- `GetTotalExpensesBetweenAsync(from, to)` → as above (or delegate to the same SQL).
- `Services/DashboardService.cs`: `GetFinanceSummaryAsync(from, to)` →
  `(decimal Income, decimal Expenses, decimal Fund)` with `Fund = Income - Expenses`.

### `frmExpenses.cs` (RBAC: Accountant/Administrator/Director; Finance nav)
- **Period selector** (combo: This Term / This Year / All Time / Custom + two date pickers shown for
  Custom). Changing it re-resolves the window and refreshes the summary + list.
- **Summary strip**: Total Income / Total Expenses / Total Fund for the window (Fund green/red).
- **Entry row**: Name, Category (editable combo from `GetCategoriesAsync`; new value persisted on
  save), Amount, Date picker, Payee, **Payer auto-filled** from `AuthService.CurrentUser.DisplayName`,
  Description. Buttons Record / Update / Clear / Delete; selecting a list row loads it for edit.
- **List**: DataGridView (Date, Name, Category, Amount, Payee, Payer) for the window + a row total.
- Shared Sign Out chip; all DB calls off the UI thread (`Task.Run`); `EnsureTablesAsync` on load.

### `frmDashboard.cs`
- Three tiles **Total Income / Total Expenses / Total Fund** for the **current term**
  (`FinancePeriod.Resolve(Term, today, …)`), built only when the role is Accountant/Administrator/
  Director; placed in the dashboard's metric area, refreshed by `LoadDashboardStatisticsAsync`.
  Term name shown as the tile caption suffix. Fund tile coloured by sign.

## Data flow

Record expense → `Expenses` (comma-free Amount) + new categories → both the existing charts and the
finance totals recompute. Selecting a window on the Expenses form re-queries income (`payment_record`)
and expenses (`Expenses`) for that range → updates the summary + list. Dashboard load computes the
current-term summary for the gated tiles.

## Error handling / edge cases

- Invalid/blank Amount → validation message; not saved. Legacy rows with commas parse via
  `ExpenseAmount.Parse`.
- Blank category → rejected; typed new category trimmed, de-duplicated case-insensitively.
- Custom window From > To → swapped/blocked with a message.
- AllTime uses wide date bounds; SQL `SUM` of no rows → 0 (not null) via `ISNULL`.
- Payee/Payer 30-char DB limit → trimmed to fit.
- Role outside the three → no tiles on dashboard; `frmExpenses` blocked by `RequireAccess`.
- All repo work try/catch + logged; a totals failure shows 0 rather than crashing the dashboard.

## Testing / verification

- `dotnet build` 0 errors; suite green.
- **Unit tests:** `ExpenseAmount.Parse/Store` (commas, blanks, decimals, round-trip);
  `FinancePeriod.Resolve` (term/year/all-time/custom boundaries + label).
- **Offline render:** `frmExpenses` constructs with summary strip + selector + grid; `frmDashboard`
  for an Accountant shows the three finance tiles, for a Teacher shows none.
- **Manual (user):** record an expense → appears in list, dashboard expense chart + tiles update;
  switch the window to This Year/All Time → totals change; Director sees tiles, Teacher does not.

## Success criteria

- Expenses can be recorded/edited/deleted with configurable categories, feeding the existing charts.
- Total Income / Expenses / Fund show for a selectable window on the Expenses form and as
  current-term tiles on the dashboard, only for Accountant/Administrator/Director.
