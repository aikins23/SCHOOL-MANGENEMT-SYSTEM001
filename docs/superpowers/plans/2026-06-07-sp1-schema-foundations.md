# SP-1 · Sync Schema Foundations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use checkbox (`- [ ]`).

**Goal:** Add `SyncId` (GUID, unique) + `RowVersion` + `UpdatedAt` to every syncable table via a central idempotent startup migration, with no behaviour change.

**Architecture:** `Data/SyncSchema.cs` owns the trusted table list and runs guarded `ALTER`s (via `EXEC` so DDL is deferred). `Program.Main` calls it once after `EnsureLocalDbRunning()`. Repos need no changes (RowVersion engine-maintained; SyncId/UpdatedAt default on insert).

**Tech Stack:** C#/.NET 4.7.2, OleDb/MSOLEDBSQL.

**Spec:** `docs/superpowers/specs/2026-06-07-sp1-schema-foundations-design.md`

---

## Conventions
- Gates: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; `dotnet run --project Tests/Kingdom.Tests` → all pass.
- Table names are a trusted constant list (no user input) — safe to interpolate into DDL.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: `SyncSchema` migration
- [ ] **Create `Data/SyncSchema.cs`**
```csharp
using System;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Idempotently adds the sync columns (SyncId, UpdatedAt, RowVersion) to every syncable table.
    /// Safe to run on every startup; tables that don't exist are skipped. No behaviour change — the
    /// columns default on insert and RowVersion is engine-maintained, so repositories need no edits.
    /// </summary>
    public static class SyncSchema
    {
        public static readonly string[] SyncTables =
        {
            "Students", "Employee", "fees", "payment_record", "examss", "Attendance", "emp_leave",
            "DraftAdmissions", "Buses", "BusRoutes", "StudentTransport", "TransportPayment",
            "Books", "BookLoans", "Rolled_Out_Students", "Users",
            "SchoolInformation", "ClassFees", "GradingScheme", "ClassSubjects", "SmsOutbox"
        };

        public static Task EnsureSyncColumnsAsync() => EnsureSyncColumnsAsync(AppConfig.ConnectionString);

        public static async Task EnsureSyncColumnsAsync(string connectionString)
        {
            try
            {
                using (var c = new OleDbConnection(connectionString))
                {
                    await c.OpenAsync();
                    foreach (var t in SyncTables)
                    {
                        try { await EnsureForTableAsync(c, t); }
                        catch (Exception ex) { Services.LoggerHelper.LogWarning($"SyncSchema[{t}]: {ex.Message}"); }
                    }
                }
            }
            catch (Exception ex) { Services.LoggerHelper.LogWarning("SyncSchema: " + ex.Message); }
        }

        private static async Task EnsureForTableAsync(OleDbConnection c, string table)
        {
            string sql = $@"
IF OBJECT_ID(N'{table}', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('{table}','SyncId') IS NULL
        EXEC('ALTER TABLE [{table}] ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_{table}_SyncId] DEFAULT NEWID()');
    IF COL_LENGTH('{table}','UpdatedAt') IS NULL
        EXEC('ALTER TABLE [{table}] ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT [DF_{table}_UpdatedAt] DEFAULT SYSUTCDATETIME()');
    IF COL_LENGTH('{table}','RowVersion') IS NULL
        EXEC('ALTER TABLE [{table}] ADD [RowVersion] ROWVERSION');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_{table}_SyncId' AND object_id = OBJECT_ID(N'{table}'))
        EXEC('CREATE UNIQUE INDEX [UX_{table}_SyncId] ON [{table}](SyncId)');
END";
            using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Test helper: true if the table exists and has the named column.</summary>
        public static async Task<bool> TableHasColumnAsync(string connectionString, string table, string column)
        {
            using (var c = new OleDbConnection(connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand($"SELECT COL_LENGTH('{table}', ?)", c))
                {
                    cmd.Parameters.AddWithValue("?", column);
                    var o = await cmd.ExecuteScalarAsync();
                    return o != null && o != DBNull.Value;
                }
            }
        }
    }
}
```
- [ ] **csproj**: add `<Compile Include="Data\SyncSchema.cs" />` next to the other `Data\*.cs` entries.
- [ ] Build → 0 errors. Commit.

### Task 2: Run at startup
- [ ] In `Program.cs` `Main`, immediately after `EnsureLocalDbRunning();` add:
```csharp
            try { Data.SyncSchema.EnsureSyncColumnsAsync().GetAwaiter().GetResult(); }
            catch (Exception ex) { Services.LoggerHelper.LogWarning("SyncSchema init: " + ex.Message); }
```
- [ ] Build → 0 errors. Commit.

### Task 3: Idempotency integration test
- [ ] In `Tests/Kingdom.Tests/Program.cs` register `new TestCase("SyncSchema adds sync columns idempotently", SyncSchema_AddsColumnsIdempotentlyAsync)` and add:
```csharp
        private static async Task SyncSchema_AddsColumnsIdempotentlyAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await SyncSchema.EnsureSyncColumnsAsync(database.ConnectionString);
                await SyncSchema.EnsureSyncColumnsAsync(database.ConnectionString); // second run must be a no-op

                AssertEx.True(await SyncSchema.TableHasColumnAsync(database.ConnectionString, "Employee", "SyncId"));
                AssertEx.True(await SyncSchema.TableHasColumnAsync(database.ConnectionString, "Employee", "UpdatedAt"));
                AssertEx.True(await SyncSchema.TableHasColumnAsync(database.ConnectionString, "Employee", "RowVersion"));
            }
        }
```
(`CreateIntegrationDatabaseOrSkipAsync` + `LocalDbTestDatabase` already exist and seed `Employee`; other tables in the list are absent in the test DB and are skipped by the `OBJECT_ID` guard.)
- [ ] `dotnet run --project Tests/Kingdom.Tests` → all pass (or this test SKIPs if LocalDB is unavailable). Commit.

### Task 4: Verify
- [ ] Build 0 errors; suite green.
- [ ] **User smoke test:** launch the app; it starts normally; via SSMS the core tables (`Students`, `Employee`, `fees`, `payment_record`, `examss`, `Attendance`, `emp_leave`, …) now have `SyncId` (all rows populated), `UpdatedAt`, `RowVersion`; app behaves exactly as before.

## Self-review notes
- Spec coverage: central idempotent migration (T1), startup hook (T2), idempotency test (T3). ✓
- No behaviour change: columns default on insert; RowVersion engine-maintained; mappers read named columns so `SELECT *` is unaffected.
- DDL uses `EXEC(...)` inside `IF` guards so conditional `ALTER`/`CREATE INDEX` parse safely; table names are trusted constants.
