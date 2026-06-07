using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Drops a consistent, self-contained "Sign Out" chip into the top-right of any top-level
    /// form, wired to the existing FormManager.SignOut() flow. Auth-gated and idempotent, and
    /// self-healing: it re-asserts itself when the form is shown, so a constructor/Load rebuild
    /// of the control tree (some forms clear and rebuild Controls) can't leave it missing.
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

                Ensure(form);                          // add now (covers forms shown immediately)
                form.Shown += (s, e) => Ensure(form);  // and re-assert after all UI is built
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning($"SessionUi.AttachSignOut({form.Name}): {ex.Message}");
            }
        }

        private static void Ensure(Form form)
        {
            try
            {
                if (form == null || form.IsDisposed) return;

                // Already present → just keep it on top (a later docked panel may have covered it).
                var existing = form.Controls.Find(SignOutName, true);
                if (existing.Length > 0)
                {
                    if (!existing[0].IsDisposed) existing[0].BringToFront();
                    return;
                }

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
                LoggerHelper.LogWarning($"SessionUi.Ensure({form?.Name}): {ex.Message}");
            }
        }
    }
}
