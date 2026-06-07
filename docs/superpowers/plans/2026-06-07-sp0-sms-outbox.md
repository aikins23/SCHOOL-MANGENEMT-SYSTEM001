# SP-0 · SMS Outbox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use checkbox (`- [ ]`).

**Goal:** Make SMS durable/offline-safe — enqueue every message to a local `SmsOutbox`, send now if possible, retry pending on a flusher.

**Architecture:** `SmsService.SendSmsAsync` enqueues then calls the refactored `SendDirectAsync` (old send logic) and marks the row; `SmsOutboxService.FlushPendingAsync` retries pending rows, run on dashboard load + the hourly timer. De-dup by SHA-256 content hash; attempts cap → Failed.

**Tech Stack:** C#/.NET 4.7.2, OleDb/MSOLEDBSQL. Explicit-include csproj.

**Spec:** `docs/superpowers/specs/2026-06-07-sp0-sms-outbox-design.md`

---

## Conventions
- Gates: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; `dotnet run --project Tests/Kingdom.Tests` → all pass.
- OleDb positional `?` params. Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- Reuse: `AppConfig.ConnectionString`, `PhoneNumberGh.NormalizeGh`, `LoggerHelper`, `ISmsProvider`.

---

### Task 1: DedupKey helper + model + tests
- [ ] **Create `Common/SmsOutboxKey.cs`**
```csharp
using System.Security.Cryptography;
using System.Text;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>Stable content hash for SMS outbox de-duplication (no time component).</summary>
    public static class SmsOutboxKey
    {
        public static string Compute(string recipient, string senderId, string message)
        {
            string raw = (recipient ?? "") + "|" + (senderId ?? "") + "|" + (message ?? "");
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder(h.Length * 2);
                foreach (var b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
```
- [ ] **Create `Models/SmsOutboxItem.cs`**
```csharp
namespace kingdom_Preparatory_School_Management_System.Models
{
    public class SmsOutboxItem
    {
        public int Id { get; set; }
        public string Recipient { get; set; } = "";
        public string SenderId { get; set; } = "";
        public string Message { get; set; } = "";
        public int Attempts { get; set; }
    }
}
```
- [ ] **csproj**: add `<Compile Include="Common\SmsOutboxKey.cs" />` and `<Compile Include="Models\SmsOutboxItem.cs" />`.
- [ ] **Test** in `Tests/Kingdom.Tests/Program.cs`: register `new TestCase("SmsOutboxKey is deterministic and content-sensitive", SmsOutboxKey_IsDeterministic)` and add:
```csharp
        private static void SmsOutboxKey_IsDeterministic()
        {
            string a = SmsOutboxKey.Compute("233241234567", "KPSFEES.", "Hello");
            string b = SmsOutboxKey.Compute("233241234567", "KPSFEES.", "Hello");
            AssertEx.Equal(a, b);
            AssertEx.Equal(64, a.Length);
            AssertEx.NotEqual(a, SmsOutboxKey.Compute("233241234567", "KPSFEES.", "Hello world"));
            AssertEx.NotEqual(a, SmsOutboxKey.Compute("233240000000", "KPSFEES.", "Hello"));
        }
```
- [ ] Build + test green. Commit.

### Task 2: `SmsOutboxRepository`
- [ ] **Create `Data/SmsOutboxRepository.cs`** with `EnsureTableAsync` (idempotent create per spec DDL), `EnqueueAsync` (skip if Pending dup by DedupKey, else insert, return id), `GetPendingAsync(maxAttempts,batch)`, `MarkSentAsync(id)`, `MarkAttemptFailedAsync(id,error,maxAttempts)`. OleDb, positional params; `SELECT @@IDENTITY` for new id; `MarkAttemptFailedAsync` uses `UPDATE SmsOutbox SET Attempts=Attempts+1, LastError=?, Status=CASE WHEN Attempts+1 >= ? THEN 'Failed' ELSE 'Pending' END WHERE Id=?`. Truncate DateTime to whole seconds.
- [ ] **csproj**: add `<Compile Include="Data\SmsOutboxRepository.cs" />`.
- [ ] Build. Commit.

### Task 3: `SmsService` durable path + flusher
- [ ] In `Services/SmsService.cs`, rename the current body of `SendSmsAsync` to `internal static Task<(bool Success,string Message)> SendDirectAsync(string recipient,string message,string senderId)` (unchanged logic).
- [ ] New `SendSmsAsync`: validate message; normalize recipient (skip+return false if invalid); `EnqueueAsync(normalized,senderId,message)` (try/catch log); `SendDirectAsync(normalized,message,senderId)`; mark Sent on success else `MarkAttemptFailedAsync(id,msg,5)`; return the immediate result.
- [ ] `SendTestAsync` calls `SendDirectAsync` (bypass outbox).
- [ ] **Create `Services/SmsOutboxService.cs`**: `FlushPendingAsync(maxAttempts=5,batch=50)` with an `Interlocked` re-entrancy guard; ensures table, gets pending, sends each via `SmsService.SendDirectAsync`, marks Sent/attempt-failed; best-effort.
- [ ] **csproj**: add `<Compile Include="Services\SmsOutboxService.cs" />`.
- [ ] Build. Commit.

### Task 4: Dashboard flush hooks
- [ ] In `frmDashboard`: timer tick lambda also `await Services.SmsOutboxService.FlushPendingAsync();`; in `frmDashboard_Load` after the reminder calls add `await Services.SmsOutboxService.FlushPendingAsync();`.
- [ ] Build. Commit.

### Task 5: Verify
- [ ] `dotnet build` 0 errors; `dotnet run --project Tests/Kingdom.Tests` all pass.
- [ ] **User smoke test:** SMS enabled + internet off → trigger SMS → `SmsOutbox` row Pending; reconnect + reopen dashboard → row flips to Sent, message delivered once.

## Self-review notes
- Spec coverage: table+repo (T2), key+model+test (T1), durable SendSmsAsync + SendDirectAsync + Test bypass + flusher (T3), hooks (T4). ✓
- No placeholders: helper/model/test verbatim; repo/flusher fully specified by method contracts + the spec DDL/SQL.
- Forward-compat: table carries `SyncId DEFAULT NEWID()` for SP-3.
