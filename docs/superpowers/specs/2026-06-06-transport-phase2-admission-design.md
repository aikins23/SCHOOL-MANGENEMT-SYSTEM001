# Transport — Phase 2: Admission Bus Choice — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

Phase 1 built the bus/route catalogue. There is still no way for a student to be assigned a
transport route. The school wants to capture, at admission, whether a student will use the
school bus and on which route.

## Goal

At student admission, ask "Will the student use the school bus?"; if yes, let the admitting user
pick a configured route. Record the chosen route on the student (informational — not billed) and
let it be viewed/changed later in the student's details.

## Scope

In scope:
- A "takes the bus?" Yes/No choice + route picker on `frmAddStd` (new admissions).
- Carry the chosen route through the existing draft-admission flow and record it on the student
  when the bursar approves.
- View/change a student's route in `frmStdDetails`.

Out of scope (explicit):
- Posting the bus fee to the fee/financial tables (the fee is informational only, per decisions).
- Changing the bus choice during the fee-payment step (`frmFessPayment` just carries the draft).
- Capacity enforcement against a bus/route, SMS/receipt mention of the route.

## Approach (chosen)

A small `StudentTransport(StudentID, RouteId)` link table in the Transport module records a
student's route. The draft carries the chosen `BusRouteId`; on approval the link row is written.
This keeps transport self-contained and avoids ALTERing the legacy `Students` table or its
fragile insert/update. Rejected: `BusRouteId` columns on `Students` (touches the legacy table and
its limited update path for no benefit).

## Data model

```sql
IF OBJECT_ID(N'StudentTransport', N'U') IS NULL
CREATE TABLE StudentTransport (
    StudentID INT NOT NULL PRIMARY KEY,   -- one route per student
    RouteId   INT NOT NULL
);
```
Created in `TransportRepository.EnsureTablesAsync` (alongside Buses/BusRoutes).

`DraftAdmissions` gains a nullable column (idempotent, in `DraftAdmissionRepository.EnsureTableAsync`):
```sql
IF COL_LENGTH('DraftAdmissions','BusRouteId') IS NULL
    ALTER TABLE DraftAdmissions ADD BusRouteId INT NULL;
```

## Components

### `Models/DraftAdmission.cs` (modify)
Add `public int? BusRouteId { get; set; }`. (`ToStudent()` is unchanged — transport is stored in
the link table, not on `Student`.)

### `Data/DraftAdmissionRepository.cs` (modify)
- `EnsureTableAsync`: add the idempotent `ALTER` for `BusRouteId`.
- `AddAsync`: persist `BusRouteId` (NULL when not taking the bus).
- `Map`: read `BusRouteId` (DBNull-safe via a column-exists guard).

### `Data/TransportRepository.cs` (modify; + interface)
- `EnsureTablesAsync`: also create `StudentTransport`.
- `Task SetStudentRouteAsync(int studentId, int? routeId)` — upsert the link; when `routeId` is
  null, delete the row (student no longer on a route).
- `Task<BusRoute> GetStudentRouteAsync(int studentId)` — the student's route joined to
  Buses for the bus label, or null.

### `Services/DraftAdmissionService.cs` (modify)
In `ApproveAsync`, after `AddStudentAsync` returns the created student (with `StudentID`): if
`draft.BusRouteId.HasValue`, call `TransportRepository.SetStudentRouteAsync(int.Parse(student.StudentID), draft.BusRouteId)`.
(The service already takes the connection string context via its repos; it gains a
`TransportRepository` field built from `AppConfig.ConnectionString`.)

### `frmAddStd.cs` (modify)
- Add a **"School bus?"** group: two radio buttons **Yes / No** (default No), and a **Route**
  `ComboBox` (enabled only when Yes) populated from `TransportRepository.GetRoutesAsync()` showing
  "RouteName — GHS fee / term". Loaded on form load.
- In the new-admission branch (`isNew`), set `draft.BusRouteId = (yes && route selected) ? routeId : (int?)null`.
- Reset the radio/combo in the form's clear logic.

### `frmStdDetails.cs` (modify)
- Add a **"Bus route"** read-only label + a **Change…** button (or an inline combo + Save) that
  shows the student's current route (from `GetStudentRouteAsync`) and lets the user pick a
  different route or **None**, saved via `SetStudentRouteAsync`. Loaded when a student is shown.

## Data flow

1. Admission: `frmAddStd` → Yes + route → `draft.BusRouteId` → `frmFessPayment` (carries draft) →
   `CreateDraftAsync` stores it → bursar **Approve** → `ApproveAsync` promotes the student and
   writes `StudentTransport(studentId, routeId)`.
2. Later: `frmStdDetails` shows/edits the student's route via the link table.

## Error handling / edge cases

- "Yes" selected but no route chosen → warn and don't submit (or treat as No — chosen: **warn**).
- No routes configured yet → the combo is empty; selecting Yes warns "No routes configured —
  add them under Transport first."
- Student removed from a route → link row deleted (route = None).
- DB errors caught and shown via `UIHelper`; transport never blocks admission or student save —
  if the link write fails, the student is still created (the failure is logged, surfaced as a
  non-fatal warning).

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection probe: `EnsureTablesAsync` creates `StudentTransport`; `SetStudentRouteAsync` upsert
  + delete; `GetStudentRouteAsync` returns the joined route.
- Render harness: `frmAddStd` shows the bus radio + route combo; `frmStdDetails` shows the bus
  route control.
- Manual (user): configure a route under Transport; add a new student, choose Yes + that route,
  pay & approve → the student's details show that route; change it to None → cleared; a student
  admitted with No has no route.

## Success criteria

- New admissions can capture a bus route; it persists onto the approved student via the link
  table and is viewable/editable in student details.
- No changes to the fee/financial tables and no ALTER of the legacy `Students` table.
- Transport remains self-contained; missing tables are created on first use.
