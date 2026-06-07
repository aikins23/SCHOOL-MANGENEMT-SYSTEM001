# SP-1 · Sync Schema Foundations — Design

**Date:** 2026-06-07
**Status:** Approved (pending spec review)
**Parent:** `2026-06-07-offline-first-cloud-sync-architecture.md`
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

Cloud sync (SP-3) needs, on every syncable row: a **globally-unique identity** that survives the
offline→online merge (auto-increment ints collide across DBs), a cheap **"changed since last sync"**
signal, and a comparable **timestamp** for last-write-wins conflict resolution. None of these exist
today. Tables are a mix of app-created (idempotent `CREATE TABLE` in repos) and **pre-existing**
ones in `Neat_Academy` (`Students`, `Employee`, `PaymentRecord`, exam/leave tables) — so the change
must not assume the app created the table.

## Goal

Add three **sync columns** to every syncable table, idempotently and centrally, with **no behaviour
change** to the running app. This is the bedrock SP-2/SP-3 build on. Ship it as a no-op-by-itself
foundation that's safe to deploy immediately.

## Columns (added to each syncable table)

| Column | Type | Purpose |
|--------|------|---------|
| `SyncId` | `UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()`, unique index | Cross-DB row identity; sync upserts by this. Backfills existing rows with distinct GUIDs (NEWID() is evaluated per row on add). |
| `RowVersion` | `ROWVERSION` | Engine-maintained, monotonic per row. Cheap local "changed since watermark" detection — **no triggers, no repo edits**. |
| `UpdatedAt` | `DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()` | Comparable UTC timestamp for last-write-wins at conflict time. Defaults on insert; existing rows backfill to the migration time. |

Rationale: `RowVersion` does the heavy lifting for "what changed" automatically, so SP-1 needs
**almost no repository edits**. `UpdatedAt` is only consulted at conflict resolution (rare), so a
default-on-insert plus an optional update trigger is sufficient — we do **not** have to edit ~40
`UPDATE` statements across 15 repos.

## Approach (chosen)

A **central migration** — `Data/SyncSchema.cs` — owns the table list and runs idempotent `ALTER`s:

```sql
IF COL_LENGTH('<table>', 'SyncId')     IS NULL ALTER TABLE [<table>] ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_<table>_SyncId DEFAULT NEWID();
IF COL_LENGTH('<table>', 'UpdatedAt')  IS NULL ALTER TABLE [<table>] ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_<table>_UpdatedAt DEFAULT SYSUTCDATETIME();
IF COL_LENGTH('<table>', 'RowVersion') IS NULL ALTER TABLE [<table>] ADD RowVersion ROWVERSION;
-- unique index on SyncId (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_<table>_SyncId') CREATE UNIQUE INDEX UX_<table>_SyncId ON [<table>](SyncId);
```

- Each table guarded by `IF OBJECT_ID(N'<table>', N'U') IS NOT NULL` so a missing table is skipped
  (not every install has every optional table).
- `EnsureSyncColumnsAsync()` runs **once at startup**, after `Program.EnsureLocalDbRunning()` and
  before the UI (behind the splash), best-effort + logged. Idempotent → safe on every launch.

Rejected: per-repo `ALTER`s (the pre-existing tables have no repo `EnsureTables`); a `ChangeLog`
outbox (heavier than needed — `RowVersion` suffices for SP-3); editing every `UPDATE` to stamp
`UpdatedAt` (large, error-prone — deferred; default-on-insert covers new rows, and an optional
per-table AFTER UPDATE trigger can be added in a later step if conflict precision proves necessary).

## Syncable table set (v1)

Operational: `Students`, `Employee`, `PaymentRecord`, `Attendance`, `DraftAdmissions`,
`Buses`, `BusRoutes`, `StudentTransport`, `TransportPayment`, `Books`, `BookLoans`, plus the exam
results and leave-request tables (exact names confirmed in the plan), and `Users` (accounts).
Config/reference: `SchoolInformation`, `ClassFees`, `GradingScheme`, `ClassSubjects`.
Already done: `SmsOutbox` (already carries `SyncId`; add `UpdatedAt`/`RowVersion` for uniformity).

(Exact pre-existing table/PK names — `Students`, `Employee`, `PaymentRecord`, exam, leave — are
verified against the DB while writing the plan; the migration only touches tables that exist.)

## Components

- **`Data/SyncSchema.cs`** — `static readonly string[] SyncTables`; `Task EnsureSyncColumnsAsync()`
  iterating the list with the idempotent SQL above; `Task<bool> HasSyncColumnsAsync(string table)`
  helper for tests. Best-effort, logged.
- **`Program.cs`** — call `Data.SyncSchema.EnsureSyncColumnsAsync().GetAwaiter().GetResult()` (or
  await in an async context) right after `EnsureLocalDbRunning()`, wrapped/logged so a failure never
  blocks startup.
- **Repositories** — no change required for SP-1 (RowVersion auto, SyncId/UpdatedAt default on
  insert). If a repo does `SELECT *` and maps by column, the new columns are simply ignored by the
  existing mappers (no breakage).

## Data flow

App start → LocalDB up → `SyncSchema.EnsureSyncColumnsAsync()` adds/【skips】 columns idempotently →
UI loads. New rows get a `SyncId`, `UpdatedAt`, and `RowVersion` automatically; existing rows are
backfilled once. Nothing else changes until SP-3 reads these for sync.

## Error handling / edge cases

- Table absent → skipped (guarded by `OBJECT_ID`).
- Column already present → skipped (`COL_LENGTH` guard). Safe on every launch.
- `SELECT *` mappers seeing new columns → harmless (mappers read named columns).
- An `INSERT (col list)` without the new columns → fine (defaults fill them).
- Adding `NOT NULL DEFAULT NEWID()` to a large existing table → one-time backfill cost at first
  launch only; acceptable for school-sized data.
- Migration failure (permissions, etc.) → logged, startup continues; sync (SP-3) simply not enabled
  yet.

## Testing / verification

- `dotnet build` 0 errors; existing suite stays green (no behaviour change).
- **Integration test (LocalDbTestDatabase):** run `EnsureSyncColumnsAsync` twice on a seeded test DB;
  assert it's idempotent and that `SyncId`/`UpdatedAt`/`RowVersion` exist on a sample table and that
  every row has a non-empty `SyncId`.
- **Manual (user):** launch app; confirm it starts normally and (via SSMS/query) the core tables now
  have the three columns with backfilled `SyncId`s, and the app behaves exactly as before.

## Success criteria

- Every existing syncable table has `SyncId` (unique, all rows populated), `UpdatedAt`, and
  `RowVersion`, added idempotently at startup, with zero change to app behaviour and a green test
  suite — ready for SP-2/SP-3 to build the sync engine on top.
