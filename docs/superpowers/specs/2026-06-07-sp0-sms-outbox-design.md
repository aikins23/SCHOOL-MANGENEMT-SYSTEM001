# SP-0 · SMS Outbox — Design

**Date:** 2026-06-07
**Status:** Approved (pending spec review)
**Parent:** `2026-06-07-offline-first-cloud-sync-architecture.md`
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

SMS today is fire-and-forget: `SmsService.SendSmsAsync(recipient, message, senderId)` resolves a
provider and sends immediately. If the machine is **offline** (or the provider call fails), the
message is **lost** — no retry, no record of intent. Admission confirmations, fee/transport reminders,
payment confirmations, and leave notices all flow through this path.

## Goal

Make SMS **durable and offline-safe**: every triggered message is persisted to a local **outbox**
before sending; it is delivered when a provider send succeeds; a background **flusher** retries
pending messages on startup and on a timer. No message lost; none double-sent. This is independent of
the cloud (purely local) and ships value immediately.

## Scope

In scope: a local `SmsOutbox` table + repository, integrating it into `SmsService.SendSmsAsync`
(enqueue → try-send-now → mark), a flusher run on app start + the existing dashboard timer, de-dup,
and an attempts cap.

Out of scope: cloud sync of the outbox (SP-3 will sync it later — that's why the table carries a
`SyncId` now); a dedicated connectivity service (SP-2 — the flusher simply retries on schedule);
changing trigger-level de-dup (TransportReminderLog / weekly fee-reminder guards stay as-is).

## Data model — `SmsOutbox` (local, idempotent creation)

```sql
IF OBJECT_ID(N'SmsOutbox', N'U') IS NULL
CREATE TABLE SmsOutbox (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),   -- forward-compat for SP-3 sync
    Recipient NVARCHAR(40) NOT NULL,                    -- normalized 233xxxxxxxxx
    SenderId NVARCHAR(20) NOT NULL,
    Message NVARCHAR(800) NOT NULL,
    Status NVARCHAR(12) NOT NULL DEFAULT ('Pending'),   -- Pending | Sent | Failed
    Attempts INT NOT NULL DEFAULT (0),
    DedupKey NVARCHAR(64) NOT NULL,
    LastError NVARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL,
    SentAt DATETIME NULL
);
```

- **DedupKey** = lowercase hex SHA-256 of `normalizedRecipient | senderId | message` (stable, no time
  component). Prevents queuing a duplicate **Pending** row for the same content.

## Components

### `Common/SmsOutboxKey.cs` (pure, unit-testable)
- `static string Compute(string recipient, string senderId, string message)` → SHA-256 hex of the
  three joined by `|`. Deterministic; used for `DedupKey`.

### `Data/SmsOutboxRepository.cs`
- `Task EnsureTableAsync()` — idempotent create.
- `Task<int> EnqueueAsync(string recipient, string senderId, string message)` — computes DedupKey;
  **skips** (returns existing id) if a `Pending` row with the same DedupKey already exists; else
  inserts `Pending`, `CreatedAt=now`, returns new id.
- `Task<List<SmsOutboxItem>> GetPendingAsync(int maxAttempts, int batch)` — `Status='Pending' AND
  Attempts < maxAttempts` oldest first, top `batch`.
- `Task MarkSentAsync(int id)` — `Status='Sent', SentAt=now`.
- `Task MarkAttemptFailedAsync(int id, string error, int maxAttempts)` — `Attempts+=1, LastError`,
  and set `Status='Failed'` when `Attempts >= maxAttempts`, else stay `Pending`.

### `Models/SmsOutboxItem.cs`
- `Id, Recipient, SenderId, Message, Attempts` (what the flusher needs).

### `Services/SmsOutboxService.cs`
- `static Task<int> FlushPendingAsync(int maxAttempts = 5, int batch = 50)` — ensures table, gets
  pending, sends each via the provider (`SmsService.SendDirectAsync`), marks Sent / attempt-failed.
  Returns count sent. Best-effort, never throws. Re-entrancy guarded by a static flag so overlapping
  timer ticks don't double-send.

### `Services/SmsService.cs` (modified)
- Refactor the existing provider-send body into `internal static Task<(bool,string)> SendDirectAsync(
  string recipient, string message, string senderId)` (current logic: validate, normalize, resolve
  provider, send, log).
- `SendSmsAsync(recipient, message, senderId)` becomes durable:
  1. validate message; normalize recipient — if invalid, log SKIPPED and return false (do **not**
     enqueue rubbish);
  2. `EnqueueAsync` (Pending) — persist intent first;
  3. `SendDirectAsync` once now; on success `MarkSentAsync`, on failure leave Pending for the flusher;
  4. return the immediate attempt's result (callers' behaviour unchanged when online).
- `SendTestAsync` (settings Test button) calls `SendDirectAsync` directly — it must reflect real
  connectivity, not "queued".

### Flusher hooks
- `frmDashboard` load + the existing hourly reminder timer also call `SmsOutboxService.FlushPendingAsync()`
  (alongside the fee/transport reminder passes). When connectivity returns, the next tick drains the
  queue. (Startup flush covers the "was offline, now reopened online" case.)

## Data flow

Trigger → `SendSmsAsync` enqueues (Pending) → tries to send now → Sent or stays Pending. While
offline, rows accumulate Pending; on reconnect the dashboard flusher sends them and marks Sent, with
the attempts cap moving permanently-failing rows to Failed (visible for support).

## Error handling / edge cases

- Offline / provider error → row stays Pending; retried until `maxAttempts`, then `Failed`.
- LogOnly provider (SMS not configured) → `SendDirectAsync` "succeeds" (logs) → row marked Sent.
- Duplicate rapid identical trigger → second enqueue finds a Pending row with same DedupKey, skips.
- Invalid/blank phone → not enqueued (returns false), as today.
- Overlapping flushes → static re-entrancy guard.
- All outbox DB work wrapped in try/catch + logged; SMS must never break a business action.

## Testing / verification

- `dotnet build` 0 errors; `dotnet run` test suite green.
- **Unit test:** `SmsOutboxKey.Compute` is deterministic and differs when any input differs.
- **Manual (user):** with SMS enabled but internet off, trigger an admission/fee SMS → row is
  `Pending`; reconnect, reopen dashboard (or wait a tick) → row flips to `Sent` and the SMS arrives,
  exactly once.

## Success criteria

- No SMS lost when offline; queued messages send automatically on reconnect; no duplicates; the
  settings Test button still reflects live connectivity.
