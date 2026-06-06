# LocalDB Self-Heal on Startup — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

`(localdb)\MSSQLLocalDB` is flaky on this machine. Two failure modes were observed live and
repeatedly blocked login with *"Login timeout expired / SQL Server process failed to start"*:
1. A **stuck `sqlservr.exe`** for MSSQLLocalDB holds the instance so `sqllocaldb start` fails.
2. A **slow cold start** (~18 s incl. `Neat_Academy` recovery) exceeds the default ~15 s login
   timeout, so the first connection from the app times out even when the engine is coming up.

`Program.EnsureLocalDbRunning()` currently just runs `sqllocaldb start` with a 10 s wait and logs
a warning on failure — it neither clears a stuck process nor waits for the DB to actually answer.

## Goal

Make the app **self-heal** the LocalDB instance on startup (behind the splash screen) so the
login screen only appears once the database is reachable — eliminating the recurring timeout,
without ever making startup worse.

## Scope

In scope: harden `Program.EnsureLocalDbRunning()` only (`Program.cs`).

Out of scope: changing the connection string/settings, the instance name, any UI, or adding a
manual "repair DB" button. No data is touched (no delete/recreate of the instance — recovery is
limited to stop/kill-stuck-process/start, which never drops data).

## Approach (chosen)

Start → probe the real connection → if it fails, recover (force-stop + kill the stuck MSSQLLocalDB
`sqlservr.exe` + start) → warm-up connect with a long timeout so recovery completes before the UI.
All non-fatal. Rejected: deleting/recreating the instance (data risk — explicitly avoided);
only bumping the connection timeout (doesn't fix the stuck-process case); a manual repair button
(worse UX than auto-heal).

## Behaviour

`EnsureLocalDbRunning()`:
1. `sqllocaldb start MSSQLLocalDB` (best-effort, as today; 10 s wait).
2. `if (TryDbConnect(8))` → done.
3. Else **recover**:
   - `sqllocaldb stop MSSQLLocalDB -k`
   - `KillStuckLocalDb()` — kill any `sqlservr.exe` whose `CommandLine` contains `MSSQLLocalDB`
     (via `System.Management` WMI; the reference already exists).
   - `sqllocaldb start MSSQLLocalDB`
   - `TryDbConnect(45)` — warm-up; waits through the cold start so the DB answers before login.
4. Every step is wrapped in try/catch and logged via `LoggerHelper`; failures are swallowed so
   startup proceeds and the login attempt surfaces the real error exactly as today.

### Helpers (private, in `Program`)
- `static void RunLocalDb(string args)` — runs `sqllocaldb <args>` hidden, `WaitForExit(10000)`.
- `static void KillStuckLocalDb()` — WMI query
  `SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='sqlservr.exe'`; for each whose
  `CommandLine` contains `MSSQLLocalDB`, `Process.Kill()`. Swallows errors.
- `static bool TryDbConnect(int timeoutSeconds)` — opens an `OleDbConnection` built from
  `AppConfig.ConnectionString` with `Connect Timeout=<n>` appended (if not already present),
  runs `SELECT 1`, returns success. Swallows errors → false.

## Data flow

`Program.Main` → `EnsureLocalDbRunning()` (start → probe → recover+warm-up) → splash → login.
The recovery/warm-up runs before any UI, so the user only sees the splash a little longer on a
cold/stuck start, then a working login.

## Error handling / edge cases

- Instance already healthy → step 2 probe succeeds immediately; no recovery, no delay.
- `sqllocaldb` not on PATH / WMI denied → caught and logged; startup continues (same as today).
- Killing `sqlservr.exe`: filtered to MSSQLLocalDB command lines so the stable `(localdb)\KPS`
  fallback instance is not touched.
- Still unreachable after recovery → login shows the real connection error (no regression).

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Manual (user): with MSSQLLocalDB stopped/stuck, launch the app → it starts, recovers, and the
  login connects without "Login timeout". With a healthy instance, startup is unchanged (fast).
- (Diagnostic, optional) the same recovery sequence we ran by hand — `stop -k`, kill stuck
  `sqlservr`, `start`, connect — now happens automatically.

## Success criteria

- After idle/stuck LocalDB, launching the app recovers it automatically and login succeeds.
- A healthy instance sees no added delay.
- No data-destructive operations; no other files changed.
