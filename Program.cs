using System;
using System.Diagnostics;
using System.Data.OleDb;
using System.Management;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            EnsureLocalDbRunning();

            // Add sync columns (SyncId/UpdatedAt/RowVersion) to syncable tables — idempotent, no-op
            // once present. Best-effort: never blocks startup.
            try { Data.SyncSchema.EnsureSyncColumnsAsync().GetAwaiter().GetResult(); }
            catch (Exception ex) { Services.LoggerHelper.LogWarning("SyncSchema init: " + ex.Message); }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Use ApplicationContext so the app lifetime is NOT tied to the splash
            // screen. The splash fades in, animates, then calls LaunchLogin() which
            // shows frmlogin and closes the splash. The app keeps running until
            // Application.Exit() is called — which frmDashboard already does in all
            // its exit paths (Exit button, top-right X, gunaPictureBox1_Click).
            Application.Run(new SplashContext());
        }

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
    }

    /// <summary>
    /// Application context that launches the splash screen as the entry point.
    /// Because no MainForm is set, the message loop runs until Application.Exit()
    /// is called explicitly — the splash closing does NOT terminate the app.
    /// </summary>
    internal sealed class SplashContext : ApplicationContext
    {
        public SplashContext()
        {
            var splash = new load();
            splash.Show();
        }
    }
}
