using System;
using System.Diagnostics;
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
            // Wake up the (localdb)\KPS instance BEFORE showing any UI.
            // LocalDB auto-stops after ~5 min of idle; starting it here gives the
            // engine time to become ready for connections before the login form appears.
            EnsureLocalDbRunning();

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
        /// Starts the (localdb)\KPS LocalDB instance if it is stopped.
        /// The call completes in &lt;100 ms when the instance is already running,
        /// and takes ~1-2 s on a cold start — well before the first DB call.
        /// </summary>
        internal static void EnsureLocalDbRunning()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "sqllocaldb",
                    Arguments              = "start KPS",
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
                // Non-fatal — if sqllocaldb is not on PATH the login attempt will
                // surface the real error message to the user.
                Common.LoggerHelper.LogWarning($"EnsureLocalDbRunning: {ex.Message}");
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
