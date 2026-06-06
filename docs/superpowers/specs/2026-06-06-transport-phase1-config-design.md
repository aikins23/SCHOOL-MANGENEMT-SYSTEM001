# Transport — Phase 1: Bus & Route Configuration — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The school has no way to record its buses or transport routes. This is the configuration
foundation for school transport. (Net-new — no existing transport code.)

## Goal

Let Director/Administrator/Headmaster maintain a catalogue of **buses** and **routes** (each
route with a fee and a payment term), so a later phase can let students choose a route at
admission. Self-contained, mirroring the existing Library/settings module patterns.

## Scope

In scope (Phase 1):
- Buses: add/edit/delete/search.
- Routes: add/edit/delete, each with a **fee** and a **payment term** (Daily/Weekly/Monthly) and
  an optional assigned bus + stops/notes.
- A dedicated `frmTransport` settings screen + dashboard nav + RBAC.

Out of scope (later / explicitly):
- **Phase 2** — the "takes the bus?" radio at student admission, route selection, and recording
  the chosen route on the student (separate spec).
- Posting bus fees to the tuition/fee tables (bus fee is recorded on the student in Phase 2, not
  billed).
- Vehicle maintenance (insurance/service/fuel) and fuel-expense logs.

## Approach (chosen)

Two tables (`Buses`, `BusRoutes`) + one `TransportRepository` + one tabbed `frmTransport`
(Buses / Routes). Same structure as the Library module. Rejected: per-stop tables (stops are a
free-text field for now); embedding config inside `frmSchoolInfo` (would bloat that form — a
dedicated screen in the same settings group was chosen).

## Data model

```sql
IF OBJECT_ID(N'Buses', N'U') IS NULL
CREATE TABLE Buses (
    BusId         INT IDENTITY(1,1) PRIMARY KEY,
    Label         NVARCHAR(100) NOT NULL,   -- e.g. "Bus 1 / GT-1234-20"
    RegNumber     NVARCHAR(40),
    DriverName    NVARCHAR(120),
    DriverContact NVARCHAR(40),
    Capacity      INT NOT NULL DEFAULT (0),
    Notes         NVARCHAR(255),
    AddedDate     DATETIME
);

IF OBJECT_ID(N'BusRoutes', N'U') IS NULL
CREATE TABLE BusRoutes (
    RouteId     INT IDENTITY(1,1) PRIMARY KEY,
    RouteName   NVARCHAR(120) NOT NULL,
    Fee         MONEY NOT NULL DEFAULT (0),
    PaymentTerm NVARCHAR(20) NOT NULL DEFAULT ('Monthly'), -- 'Daily' | 'Weekly' | 'Monthly'
    BusId       INT NULL,                                  -- assigned bus (optional)
    Stops       NVARCHAR(255),
    Notes       NVARCHAR(255)
);
```

`MONEY` ↔ C# `decimal`; `AddedDate` truncated to whole seconds before binding (MSOLEDBSQL
caveat). Tables created idempotently (`IF OBJECT_ID … CREATE TABLE`). No DB-level FK (consistent
with the app); the bus↔route link is enforced in code (delete-guard).

## Components

### `Models/Bus.cs` (new)
`BusId (int), Label, RegNumber, DriverName, DriverContact, Capacity (int), Notes, AddedDate`.
`ToString() => Label` (for ComboBox display in the route dialog).

### `Models/BusRoute.cs` (new)
`RouteId (int), RouteName, Fee (decimal), PaymentTerm (string), BusId (int?), BusLabel (string,
joined for display), Stops, Notes`.

### `Data/ITransportRepository.cs` + `Data/TransportRepository.cs` (new)
- `Task EnsureTablesAsync()` — create both tables.
- Buses: `Task<List<Bus>> GetBusesAsync(string search)`, `Task<int> AddBusAsync(Bus)`,
  `Task UpdateBusAsync(Bus)`, `Task<bool> DeleteBusAsync(int busId)` (returns false if any route
  references the bus).
- Routes: `Task<List<BusRoute>> GetRoutesAsync()` (LEFT JOIN Buses for `BusLabel`, ordered by
  RouteName), `Task<int> AddRouteAsync(BusRoute)`, `Task UpdateRouteAsync(BusRoute)`,
  `Task<bool> DeleteRouteAsync(int routeId)`.
- OleDb, `AppConfig.ConnectionString`, mirrors `LibraryRepository`.

### `frmTransport.cs` (new — one form, `TabControl` with two tabs)
- **Buses tab:** search box + `DataGridView` (Label, Reg No., Driver, Contact, Capacity) +
  Add / Edit / Delete. Add/Edit open a small modal dialog for the bus fields.
  Delete blocked (with a message) if the bus is assigned to any route.
- **Routes tab:** `DataGridView` (Route, Fee, Term, Bus, Stops) + Add / Edit / Delete. The
  add/edit dialog has Route name, Fee (numeric), Payment term (combo Daily/Weekly/Monthly), Bus
  (combo of buses, optional/None), Stops, Notes.
- Constructor: `BuildUi(); if (!AuthService.RequireAccess("frmTransport", this)) return; Load += …`.
- Themed with `AppConfig.Colors`; verified offline via the render harness.

### RBAC + navigation
- `Services/AuthService.cs`: `["frmTransport"] = { Director, Administrator, Headmaster }`.
- `frmDashboard.cs`: a "Transport" nav entry in the Director/Admin/Headmaster settings group
  (next to School Information / Subjects / Library).

## Data flow

1. Admin opens **Transport** → Buses tab → add buses.
2. Routes tab → add routes, each with a fee, a payment term, and (optionally) an assigned bus.
3. Phase 2 (later) reads `GetRoutesAsync()` to populate the admission route picker.

## Error handling / edge cases

- Delete a bus referenced by a route → blocked with a message.
- Fee defaults to 0; payment term defaults to Monthly; required: route name, bus label.
- DB/load errors caught and shown via `UIHelper`, logged via `LoggerHelper`; module is isolated
  and never breaks the rest of the app (tables created on first use).

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection probe: `EnsureTablesAsync`, add a bus, add a route referencing it, `GetRoutesAsync`
  shows the joined bus label, delete-bus blocked while referenced.
- Render harness: `frmTransport` (both tabs render).
- Manual (user): add 2 buses; add routes with fees + terms + assigned bus; edit/delete; deleting
  an assigned bus is blocked.

## Success criteria

- Director/Administrator/Headmaster can maintain buses and routes (with fee + payment term) in a
  dedicated Transport settings screen.
- The route catalogue is ready for Phase 2's admission integration.
- No changes to fee/financial tables; self-contained module.
