# Offline-First Cloud Sync — Architecture (North Star)

**Date:** 2026-06-07
**Status:** Approved (architecture); sub-projects to be specced individually
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The product will be sold to many schools and hosted on the internet, but schools must keep
operating **without internet** — enrolling students/employees, taking fee payments, recording
attendance, transport, exams, etc. — and have that data **sync automatically to a central cloud
database when connectivity returns**. SMS triggered while offline must not be lost.

Today the app talks (via OleDb/MSOLEDBSQL) to a **local** `(localdb)\MSSQLLocalDB` database, with
~15 repositories using plain `SELECT/INSERT` and **auto-increment integer keys** (`MAX(id)+1`).
SMS is **fire-and-forget** (lost if offline). There is no cloud, no change tracking, no sync.

## Goals

- Schools work fully offline on their premises; data syncs to their cloud DB when online.
- No data loss or duplication across the offline→online boundary (idempotent, collision-free).
- SMS triggered offline is queued and delivered once connectivity returns (no duplicates).
- Minimal disruption/risk to the existing, working local app.

## Key decisions (rationale)

1. **Tenancy = one cloud database per school** (not shared multi-tenant).
   - Repos have no tenant filter; shared multi-tenant would require a `TenantId` retrofit in every
     query/insert/join (~15 repos) with a data-leak risk if any is missed. DB-per-school gives hard
     isolation, near-zero query churn, trivial per-customer backup/offboarding, and school-scoped
     conflicts. Azure SQL elastic pools keep hosting affordable. Revisit multi-tenant only at large
     scale.
2. **School topology = one local "primary" DB per school, shared over the LAN.**
   - The main PC/small server holds the school's local DB; other PCs connect to it over the LAN. A
     single-PC school is its own primary. Avoids messy PC-to-PC merge inside a school and gives one
     sync channel: **local-primary ↔ that school's cloud DB**. Full offline operation on the LAN.
3. **Keys = add a GUID "sync id" to synced rows** (existing int IDs stay for display/local use).
   - GUIDs make local↔cloud row mapping 1:1, collision-free, and sync idempotent (re-running sync
     never duplicates). Cheaper and safer than re-keying everything; the int `StudentID` display
     (KPS-prefix) is unaffected.
4. **Conflict resolution = last-write-wins by row `UpdatedAt`** (newest wins).
   - One local primary per school means conflicts only arise between offline-school edits and
     cloud-side edits (future web/parent portal). Newest-wins handles both directions simply; true
     simultaneous same-row edits are rare. Field-level merge is deferred (YAGNI).
5. **SMS = persistent outbox**, flushed when online, with de-dup.
   - Every trigger writes a queued row; a flusher sends when connectivity is up and marks it sent.
     Reliable offline behaviour and no double-sends.

## Target architecture

```
 ┌─────────────── School premises (offline-capable) ───────────────┐
 │  PC (front desk) ─┐                                              │
 │  PC (accounts)  ──┼──LAN──▶  Local PRIMARY DB (SQL Express/LocalDB)│
 │  PC (head)      ──┘            │  + SmsOutbox, ChangeLog/SyncState │
 └───────────────────────────────┼─────────────────────────────────┘
                                  │  Sync engine (when online):
                                  │   push pending → pull remote (LWW)
                                  ▼
                         School's CLOUD DB (Azure SQL)  ◀── future web/parent portal
                                  ▲
                         Cloud SMS worker (optional) flushes outbox
```

- **Local primary DB**: same schema as today + sync columns + `SmsOutbox`.
- **Sync engine**: background loop in the app (or a small service) on the primary node only.
- **Cloud DB**: per-school, same schema; source for any web portal.
- **SMS**: queued locally; sent by the app when online and/or a cloud worker (single owner to avoid
  double-send — see SP-0).

## Components / data model additions

- **Sync columns** on each synced table: `SyncId UNIQUEIDENTIFIER` (unique), `UpdatedAt DATETIME2`,
  `RowVersion ROWVERSION`, `SyncState` (or a separate `ChangeLog` outbox). Added via idempotent
  `ALTER` so existing data/app keep working.
- **`SmsOutbox`**: `Id, SyncId, Recipient, SenderId, Message, Status (Pending/Sent/Failed),
  Attempts, CreatedAt, SentAt, DedupKey`.
- **`SyncCursor`/config**: cloud connection string (encrypted), last-pulled watermark per table,
  this node's role (primary/secondary), device id.
- **Connectivity service**: cheap online check (ping cloud / open a connection with short timeout)
  + scheduler.
- **Sync engine**: per-entity push (rows where `SyncState=Pending` or `UpdatedAt > lastPushed`) and
  pull (rows where remote `UpdatedAt > watermark`), upsert by `SyncId`, LWW by `UpdatedAt`.

## Sub-projects (each gets its own spec → plan → build)

| ID | Sub-project | Why / depends on |
|----|-------------|------------------|
| **SP-0** | **SMS outbox** | Independent, high-value, low-risk. Persist+flush SMS; de-dup. Directly answers "consider the SMS triggers." Build first. |
| **SP-1** | Schema foundations | Add `SyncId`/`UpdatedAt`/`RowVersion`/`SyncState` (idempotent) to core tables; repos stamp them on write. No behaviour change. Everything else depends on this. |
| **SP-2** | Connectivity + cloud config | Encrypted cloud connection string, online detection, sync scheduler skeleton. |
| **SP-3** | Sync engine | Push/pull upsert-by-SyncId, LWW, per entity (students, employees, fees, payments, attendance, transport, exams…). |
| **SP-4** | Cloud provisioning | Create + migrate + seed a new school's cloud DB. |
| **SP-5** | LAN multi-PC | Secondary PCs point at the school primary (connection config + role). |

**Build order:** SP-0 → SP-1 → SP-2 → SP-3 → (SP-4, SP-5). SP-0 ships value immediately and is
independent of the cloud.

## Error handling / edge cases (architecture-level)

- Sync is **idempotent** (upsert by `SyncId`): a half-finished sync re-runs safely.
- Offline indefinitely: local app fully functional; outbox + change log grow and drain on reconnect.
- Clock skew affecting LWW: stamp `UpdatedAt` from a single source per write where possible; tolerate
  small skew (rare same-row cross-side edits).
- SMS double-send: single sender owner + `DedupKey` + status transitions guard it.
- Schema upgrades: idempotent migrations run on both local and each cloud DB.

## Security / cost notes

- Cloud connection string stored encrypted (reuse `SecretStorage`).
- DB-per-school = no cross-tenant leakage by construction.
- Transport-layer encryption to cloud SQL (`Encrypt=True` for the cloud connection).
- BulkSMS/Arkesel API keys remain server/secret-stored; never committed.

## Out of scope (for now)

- Shared multi-tenant DB; field-level merge; real-time push; a full web/parent portal (separate
  product); automatic cloud cost optimisation.

## Success criteria (whole programme)

- A school runs offline for days, then on reconnect its new students/employees/fees/attendance and
  queued SMS all appear in its cloud DB with no duplicates or losses, and cloud-side edits flow back.
