using System;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Use ApplicationContext so the app lifetime is NOT tied to the splash
            // screen. The splash fades in, animates, then calls LaunchLogin() which
            // shows frmlogin and closes the splash. The app keeps running until
            // Application.Exit() is called — which frmDashboard already does in all
            // its exit paths (Exit button, top-right X, gunaPictureBox1_Click).
            Application.Run(new SplashContext());
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
