using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Drops a consistent, self-contained "Sign Out" chip into the top-right of any top-level
    /// form, wired to the existing FormManager.SignOut() flow. Auth-gated and idempotent.
    /// </summary>
    public static class SessionUi
    {
        private const string SignOutName = "_sharedSignOut";

        public static void AttachSignOut(Form form)
        {
            if (form == null) return;
            try
            {
                // Only for a signed-in session — never on login/splash.
                if (!AuthService.CurrentUser.IsAuthenticated) return;
                // Idempotent: don't add a second chip.
                if (form.Controls.Find(SignOutName, true).Length > 0) return;

                var btn = new Button
                {
                    Name      = SignOutName,
                    Text      = "⎋  Sign Out",
                    Size      = new Size(104, 30),
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(190, 18, 60), // AccentRed
                    Cursor    = Cursors.Hand,
                    TabStop   = false,
                    Anchor    = AnchorStyles.Top | AnchorStyles.Right
                };
                btn.FlatAppearance.BorderColor        = Color.FromArgb(244, 199, 207);
                btn.FlatAppearance.BorderSize         = 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 235, 238);

                const int margin = 8;
                btn.Location = new Point(Math.Max(margin, form.ClientSize.Width - btn.Width - margin), margin);
                btn.Click += (s, e) => FormManager.SignOut();

                form.Controls.Add(btn);
                btn.BringToFront();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning($"SessionUi.AttachSignOut({form.Name}): {ex.Message}");
            }
        }
    }
}
