# LocalDB Self-Heal on Startup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `Program.EnsureLocalDbRunning()` self-heal a stuck/cold `(localdb)\MSSQLLocalDB` instance on startup so login no longer fails with "Login timeout expired".

**Architecture:** Start the instance → probe the real connection → if the probe fails, force-stop + kill the stuck MSSQLLocalDB `sqlservr.exe` + start again + warm-up connect with a long timeout. All steps are best-effort and logged; nothing is fatal, and no data-destructive operation is performed.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms. One file: `Program.cs`. Uses `System.Data.OleDb` (already in System.Data) and `System.Management` (already referenced in the csproj, line 124).

**Spec:** `docs/superpowers/specs/2026-06-06-localdb-self-heal-design.md`

---

## Conventions

- **No unit-test framework.** Gate: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Single file: `Program.cs`. Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- Connection string (from `App.config`): `Provider=MSOLEDBSQL;Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI;Initial Catalog=Neat_Academy;Encrypt=False` — has **no** `Connect Timeout`, so we append one for the probe/warm-up.
- `Services.LoggerHelper.LogWarning(string)` is the existing logger used here today.

---

### Task 1: Harden `EnsureLocalDbRunning` with probe + recovery + warm-up

**Files:** Modify `Program.cs`.

- [ ] **Step 1: Add the required usings**

At the top of `Program.cs`, after `using System.Diagnostics;` (line 2), add:

```csharp
using System.Data.OleDb;
using System.Management;
```

(`using System;`, `using System.Diagnostics;`, `using System.Windows.Forms;`, and
`using ...Common;` already present.)

- [ ] **Step 2: Replace the `EnsureLocalDbRunning` method body**

Replace the entire current method (lines ~29-58, the XML-doc comment through the method's closing
brace) with the version below. It keeps the original `sqllocaldb start` behaviour, then adds the
probe → recover → warm-up logic and two private helpers.

```csharp
        /// <summary>
        /// Ensures (localdb)\MSSQLLocalDB is running AND actually answering before the UI loads.
        /// Fast path: start the instance, probe a real connection (~8 s ceiling) and return.
        /// Self-heal path (only if the probe fails): force-stop the instance, kill any stuck
        /// MSSQLLocalDB sqlservr.exe, start again, then warm-up connect with a long timeout so the
        /// cold start completes before login. Every step is best-effort and logged; nothing here is
        /// fatal and no data is touched (no delete/recreate of the instance).
        /// </summary>
        internal static void EnsureLocalDbRunning()
        {
            try
            {
                RunLocalDb("start MSSQLLocalDB");

                // Fast path: already healthy → nothing more to do.
                if (TryDbConnect(8))
                    return;

                Services.LoggerHelper.LogWarning(
                    "EnsureLocalDbRunning: MSSQLLocalDB did not answer; attempting self-heal.");

                // Recover: force the instance down, clear a stuck engine process, bring it back.
                RunLocalDb("stop MSSQLLocalDB -k");
                KillStuckLocalDb();
                RunLocalDb("start MSSQLLocalDB");

                // Warm-up: wait through the cold start (Neat_Academy recovery) so the DB answers
                // before the login screen appears.
                if (TryDbConnect(45))
                    Services.LoggerHelper.LogWarning("EnsureLocalDbRunning: MSSQLLocalDB recovered.");
                else
                    Services.LoggerHelper.LogWarning(
                        "EnsureLocalDbRunning: MSSQLLocalDB still unreachable after self-heal; "
                        + "login will surface the real error.");
            }
            catch (Exception ex)
            {
                // Non-fatal — if sqllocaldb is not on PATH (or anything else throws) the login
                // attempt will surface the real error message to the user.
                Services.LoggerHelper.LogWarning($"EnsureLocalDbRunning: {ex.Message}");
            }
        }

        /// <summary>Runs "sqllocaldb &lt;args&gt;" hidden with a 10 s ceiling. Best-effort.</summary>
        private static void RunLocalDb(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "sqllocaldb",
                    Arguments              = args,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using (var proc = Process.Start(psi))
                {
                    proc.WaitForExit(10000); // 10 s ceiling; normally exits in <2 s
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning($"RunLocalDb('{args}'): {ex.Message}");
            }
        }

        /// <summary>
        /// Kills any sqlservr.exe whose command line references MSSQLLocalDB (a stuck engine for
        /// this instance). Filtered by command line so the stable (localdb)\KPS fallback instance
        /// is left untouched. Best-effort.
        /// </summary>
        private static void KillStuckLocalDb()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'sqlservr.exe'"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        try
                        {
                            var cmd = mo["CommandLine"] as string;
                            if (string.IsNullOrEmpty(cmd) ||
                                cmd.IndexOf("MSSQLLocalDB", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;

                            int pid = Convert.ToInt32(mo["ProcessId"]);
                            using (var proc = Process.GetProcessById(pid))
                            {
                                proc.Kill();
                                proc.WaitForExit(5000);
                            }
                            Services.LoggerHelper.LogWarning(
                                $"KillStuckLocalDb: killed stuck sqlservr.exe (PID {pid}).");
                        }
                        catch (Exception exInner)
                        {
                            Services.LoggerHelper.LogWarning($"KillStuckLocalDb(inner): {exInner.Message}");
                        }
                        finally { mo.Dispose(); }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning($"KillStuckLocalDb: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the app connection (with an explicit Connect Timeout) and runs SELECT 1.
        /// Returns true if the DB answered, false on any failure. Best-effort (never throws).
        /// </summary>
        private static bool TryDbConnect(int timeoutSeconds)
        {
            try
            {
                string cs = AppConfig.ConnectionString ?? string.Empty;
                if (cs.IndexOf("Connect Timeout", StringComparison.OrdinalIgnoreCase) < 0 &&
                    cs.IndexOf("Connection Timeout", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    if (!cs.EndsWith(";")) cs += ";";
                    cs += "Connect Timeout=" + timeoutSeconds;
                }

                using (var conn = new OleDbConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT 1", conn))
                    {
                        cmd.CommandTimeout = timeoutSeconds;
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning($"TryDbConnect({timeoutSeconds}s): {ex.Message}");
                return false;
            }
        }
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If `ManagementObjectSearcher` is unresolved, Step 1's `using System.Management;`
is missing. If `OleDbConnection` is unresolved, Step 1's `using System.Data.OleDb;` is missing.)

- [ ] **Step 4: Commit**

```bash
git add Program.cs docs/superpowers/plans/2026-06-06-localdb-self-heal.md
git commit -m "fix(localdb): self-heal stuck/cold MSSQLLocalDB on startup (probe + recover + warm-up)"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **Code review:** the fast path (`start` → `TryDbConnect(8)` → return) is unchanged in spirit
  from today; recovery only runs when the probe fails; `KillStuckLocalDb` filters on the
  `MSSQLLocalDB` command line (KPS untouched); all paths swallow exceptions and log.
- [ ] **User smoke test:** with MSSQLLocalDB stopped or stuck, launch the app → splash holds
  briefly, then login connects without "Login timeout expired". With a healthy instance, startup
  speed is unchanged (probe returns immediately).

## Self-review notes

- **Spec coverage:** start (Step 2 `RunLocalDb("start…")`) ✓; probe ~8 s (`TryDbConnect(8)`) ✓;
  recover = `stop -k` + kill stuck `sqlservr` + `start` (Step 2) ✓; warm-up ~45 s
  (`TryDbConnect(45)`) ✓; all non-fatal + logged ✓; helpers `KillStuckLocalDb`/`TryDbConnect`
  present (plus `RunLocalDb` to DRY the three `sqllocaldb` calls) ✓; no data-destructive op ✓;
  KPS fallback protected by command-line filter ✓.
- **Placeholder scan:** none — full method + three helpers given verbatim.
- **Type consistency:** `RunLocalDb(string)`, `KillStuckLocalDb()`, `TryDbConnect(int)→bool`,
  `AppConfig.ConnectionString` (string), `Services.LoggerHelper.LogWarning(string)` — all match
  existing signatures.
- **Note:** added a small `RunLocalDb` helper not named in the spec (the spec listed it under
  Helpers) to avoid repeating the `ProcessStartInfo` block three times — DRY, no behaviour change.
