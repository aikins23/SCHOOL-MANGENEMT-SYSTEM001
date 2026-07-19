using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

using System.Management;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    internal static class Program
    {
        private static Services.BackgroundSyncService _backgroundSyncService;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            EnsureLocalDbRunning();

            // Add sync columns (SyncId/UpdatedAt/RowVersion) to syncable tables — idempotent, no-op
            // once present. Best-effort: never blocks startup.


            // Tenant isolation foundation: every school-owned table gets SchoolId so desktop and
            // future web sync can safely separate one school's data from another's.


            // Dynamic role/permission tables — seeds system roles and the permission catalog
            // so directors can configure access without code changes.


            // Ensure Performance and Weekly Output reporting tables exist.


            // ReportSchema may create new syncable tables, so run the sync column pass again
            // before background sync starts.


            // Add query indexes for dashboard/search-heavy screens. Idempotent and best-effort:
            // first run may spend a moment creating indexes, later runs are effectively no-op.


            // Create dashboard summary tables up front. Refresh is best-effort after writes; reads
            // fall back to live queries if summaries are empty.


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Idle += (s, e) => ApplyBrandingToOpenForms();
            Application.ApplicationExit += (s, e) =>
            {
                if (_backgroundSyncService != null)
                    _backgroundSyncService.Dispose();
            };

            // Use ApplicationContext so the app lifetime is NOT tied to the splash
            // screen. The splash fades in, animates, then calls LaunchLogin() which
            // shows frmlogin and closes the splash. The app keeps running until
            // Application.Exit() is called — which frmDashboard already does in all
            // its exit paths (Exit button, top-right X, gunaPictureBox1_Click).
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                System.IO.File.WriteAllText("crash.log", e.ExceptionObject.ToString());
            };
            Application.ThreadException += (s, e) => {
                System.IO.File.WriteAllText("crash.log", e.Exception.ToString());
            };

            BeginStartupServices();

            try
            {
                Services.LoggerHelper.LogWarning("Reached Application.Run");
                Application.Run(new SplashContext());
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash.log", ex.ToString());
                throw;
            }
        }

        private static void BeginStartupServices()
        {
            Task.Run(async () =>
            {
                await RunStartupDatabaseMaintenanceAsync().ConfigureAwait(false);
                StartBackgroundSyncSafe();
            });
        }

        private static async Task RunStartupDatabaseMaintenanceAsync()
        {
            await RunStartupStepAsync("SyncSchema init", () => Data.SyncSchema.EnsureSyncColumnsAsync());
            await RunStartupStepAsync("TenantSchema init", () => Data.TenantSchema.EnsureTenantColumnsAsync());
            await RunStartupStepAsync("PermissionSchema init", () => Data.PermissionSchema.EnsurePermissionTablesAsync());
            await RunStartupStepAsync("ReportSchema init", () => Data.ReportSchema.EnsureReportTablesAsync());
            await RunStartupStepAsync("SyncSchema post-report init", () => Data.SyncSchema.EnsureSyncColumnsAsync());
            await RunStartupStepAsync("Performance index init", () => Data.SyncSchema.EnsurePerformanceIndexesAsync());
            await RunStartupStepAsync("Dashboard summary init", () =>
                new Data.DashboardSummaryRepository(AppConfig.ConnectionString).EnsureTablesAsync());
        }

        private static async Task RunStartupStepAsync(string name, Func<Task> step)
        {
            try
            {
                await step().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning(name + ": " + ex.Message);
            }
        }

        private static void StartBackgroundSyncSafe()
        {
            try
            {
                _backgroundSyncService = new Services.BackgroundSyncService();
                _backgroundSyncService.Start();
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Background sync start: " + ex.Message);
            }
        }

        private static void ApplyBrandingToOpenForms()
        {
            Icon appIcon = Branding.AppIcon;
            if (appIcon == null) return;

            foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
            {
                if (form == null || form.IsDisposed)
                {
                    continue;
                }

                try
                {
                    if (form.Icon == null || form.Icon.Handle != appIcon.Handle)
                    {
                        form.Icon = appIcon;
                    }

                    if (form.ShowInTaskbar)
                    {
                        form.ShowIcon = true;
                    }
                }
                catch
                {
                    // Some transient/closing forms can reject chrome updates.
                }
            }
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
                if (!UsesLocalDb(AppConfig.ConnectionString))
                    return;

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

        internal static bool UsesLocalDb(string connectionString)
        {
            return !string.IsNullOrWhiteSpace(connectionString)
                && connectionString.IndexOf("(localdb)", StringComparison.OrdinalIgnoreCase) >= 0;
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

                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1", conn))
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
