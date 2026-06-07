# Transport Payment Tracking — Design

**Date:** 2026-06-07
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

Transport today is config + assignment only: `BusRoutes` has a `Fee` and a `PaymentTerm`
(Daily/Weekly/Monthly), and `StudentTransport(StudentID, RouteId)` records who rides which route.
There is **no record of any transport payment** — no amounts paid, balances, history, or arrears —
and the tuition fees ledger (`FeeRepository`/`PaymentRecord`) is deliberately tuition-only. So the
school cannot answer "has this bus student paid for this period, and what do they owe."

## Goal

Track transport payments in a **dedicated ledger** (separate from tuition), with a cashier screen
to record payments, a per-student history, an arrears ("who owes") view for the current period, and
an automatic overdue **SMS reminder** to guardians driven by actual bus usage (attendance).

## Scope

In scope: a new transport-payment ledger + repository, a dedicated `frmTransportPayments` screen
(record + history + arrears), an overdue-reminder pass piggybacking the existing dashboard reminder
timer, a configurable nothing-new-schema setting, RBAC + nav.

Out of scope: posting transport to the tuition ledger; auto-invoicing/auto-charging; receipt
printing (record-only by decision); auto-reminders for **Daily** routes; staff transport.

## Decisions (from brainstorming)

- **Period = the route's own term.** Monthly route → a 1-month period; Weekly route → a 2-week
  (fortnight) period; Daily route → a single day. "Current period" is computed from today.
- **Dedicated screen** (not folded into Fees Payment).
- **Arrears view** included (who owes for the current period).
- **No receipt on payment** — record only.
- **Overdue SMS reminder, attendance-triggered:** for a Monthly/Weekly route, when the current
  period's balance > 0 **and** the student has **≥ 2 PRESENT attendance days** in that period, send
  one SMS to the guardian, **once per student per period**. Daily routes are recordable but excluded
  from auto-reminders.

## Period model

`TransportPeriod` (a small static helper) maps `(PaymentTerm, DateTime today)` → `(Key, Start, End)`:

| Term | Key format | Start | End |
|------|-----------|-------|-----|
| Monthly | `YYYY-MM` (e.g. `2026-06`) | 1st of month | last day of month |
| Weekly | `YYYY-Wnn` fortnight (e.g. `2026-FN12`) | start of the 2-week block | +13 days |
| Daily | `YYYY-MM-DD` | that day | that day |

Weekly uses **2-week (fortnight) blocks** anchored to a fixed epoch Monday so the block for any date
is deterministic. (Term comes from the student's assigned route.)

## Data model (idempotent creation, like the other repos)

```sql
IF OBJECT_ID(N'TransportPayment', N'U') IS NULL
CREATE TABLE TransportPayment (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StudentID INT NOT NULL,
    RouteId INT NOT NULL,
    Period NVARCHAR(20) NOT NULL,        -- period Key (see table above)
    PeriodStart DATETIME NOT NULL,
    PeriodEnd DATETIME NOT NULL,
    AmountPaid MONEY NOT NULL DEFAULT (0),
    PaymentDate DATETIME NOT NULL,
    Cashier NVARCHAR(120),
    Notes NVARCHAR(255)
);

IF OBJECT_ID(N'TransportReminderLog', N'U') IS NULL
CREATE TABLE TransportReminderLog (
    StudentID INT NOT NULL,
    Period NVARCHAR(20) NOT NULL,
    SentDate DATETIME NOT NULL,
    CONSTRAINT PK_TransportReminderLog PRIMARY KEY (StudentID, Period)
);
```

- Partial payments allowed: multiple `TransportPayment` rows per `(StudentID, Period)`.
- **Balance for a period** = route `Fee` − Σ`AmountPaid` for that `(StudentID, Period)` (floored at 0).

These tables are created by `TransportRepository.EnsureTablesAsync()` (extended), reusing the
existing transport repo/connection.

## Components

### `Common/TransportPeriod.cs` (new, pure → unit-testable)
- `static (string Key, DateTime Start, DateTime End) Current(string paymentTerm, DateTime today)`.
- `static bool SupportsReminders(string paymentTerm)` → true for Monthly/Weekly, false for Daily.

### `Data/TransportRepository.cs` (extend `ITransportRepository`)
- `EnsureTablesAsync()` also creates the two new tables.
- `Task<bool> AddTransportPaymentAsync(int studentId, int routeId, (Key,Start,End) period, decimal amountPaid, DateTime date, string cashier, string notes)`.
- `Task<decimal> GetPaidForPeriodAsync(int studentId, string periodKey)` → Σ AmountPaid.
- `Task<DataTable> GetStudentTransportHistoryAsync(int studentId)` → all payments, newest first.
- `Task<DataTable> GetArrearsTableAsync(DateTime asOf)` → every student with a route, joined to
  route Fee/Term, with current-period Paid, Balance, and PRESENT-day count in the period.
- `Task<List<TransportArrear>> GetReminderCandidatesAsync(DateTime asOf)` → Monthly/Weekly bus
  students with current-period Balance > 0 AND PRESENT days ≥ 2 AND no `TransportReminderLog` row
  for `(StudentID, currentPeriodKey)`. Includes guardian phone + amounts for the message.
- `Task LogReminderSentAsync(int studentId, string periodKey)`.
- Present-day count query: `SELECT COUNT(*) FROM <AttendanceTable> WHERE ReferenceID=? AND
  ReferenceType='STUDENT' AND Status='PRESENT' AND [Date] BETWEEN ? AND ?`.

### `frmTransportPayments` (new, code-built, RBAC: Accountant/Administrator/Headmaster)
- **Record tab:** look up a bus student by KPS-prefixed ID (`StudentId.Parse`) → show name, route,
  term, current period, route Fee, paid-this-period, **balance**. Cashier auto-filled from the
  signed-in user's full name (same as Fees). "Record Payment" writes a `TransportPayment` row and
  refreshes. Below: per-student **history** grid (`StudentId.Display` formatting). No receipt.
- **Arrears tab:** grid from `GetArrearsTableAsync` — `Student, Route, Term, Period, Fee, Paid,
  Balance, Present days`, with an "unpaid only" filter. Read-only.
- Gets the shared Sign Out chip via `SessionUi.AttachSignOut(this)` (consistent with other forms).

### Overdue reminder (piggyback the existing dashboard reminder pass)
- The dashboard already runs `CheckFeeRemindersAsync()` on load + a weekly timer. Add a sibling
  `CheckTransportRemindersAsync()` called from the same places (Accountant/Admin/Headmaster only).
- It calls `GetReminderCandidatesAsync(DateTime.Today)`, sends one SMS per candidate via the
  existing `SmsSender`/SMS settings (message: school name, student, route, period, amount owed),
  then `LogReminderSentAsync` so it is never resent for that period. Non-fatal/logged.
- SMS sender ID: reuse the existing Fee reminder sender (`SmsSenderIds.FeeReminder`).

### Setting
- Reminder behaviour is fixed by the agreed rule (≥2 present days; Monthly/Weekly). No new "days"
  setting is needed — the period term itself defines the cadence. (If a manual override is wanted
  later it can be added; YAGNI for now.)

## Access + navigation
- RBAC `frmTransportPayments` = Director? No — mirror Fees Payment: **Accountant / Administrator /
  Headmaster**. Add `AuthService._formAccess` entry + nav button under the **Finance** group in
  `frmDashboard` (and `frmTeacherDashboard` not applicable).

## Data flow

Admission assigns a route (existing) → cashier opens **Transport Payments**, looks up the student,
sees current-period balance, records a payment → ledger updated. Arrears tab shows who still owes.
When the dashboard is open, the reminder pass SMSes guardians of unpaid students who have ridden
(≥2 present days) this period, once per period.

## Error handling / edge cases

- Student with no route → screen says "not assigned to a bus route"; not in arrears/candidates.
- Overpayment / balance floored at 0; route `Fee` = 0 → balance 0, never a candidate.
- Daily routes → recordable, excluded from reminders (`SupportsReminders` false).
- No guardian phone / SMS disabled → candidate skipped (logged), not retried into a loop.
- Attendance table absent/empty → present-day count 0 → no reminder (safe).
- All reminder/DB work wrapped in try/catch and logged; never blocks the dashboard.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- **Unit tests (Kingdom.Tests):** `TransportPeriod.Current` for Monthly/Weekly/Daily boundaries;
  `SupportsReminders`; balance calc (Fee − Σpaid, floored). Add a fake-repo test for the
  reminder-candidate rule (balance>0 AND present≥2 AND not already logged) if feasible with the
  existing fake pattern.
- **Offline render:** `frmTransportPayments` constructs with the Sign Out chip; Record + Arrears
  tabs present.
- **Manual (user):** assign a student to a monthly route; record a partial payment → balance drops;
  arrears tab shows the remainder; mark 2 present days + leave unpaid → reminder SMS logged once.

## Success criteria

- Cashiers can record transport payments and see balances + per-student history on a dedicated
  screen; an arrears list shows who owes for the current period.
- Guardians of unpaid bus students who have ridden ≥2 days in the period get exactly one SMS per
  period; daily routes are excluded.
- No change to the tuition ledger, RBAC of other screens, or existing transport config.
