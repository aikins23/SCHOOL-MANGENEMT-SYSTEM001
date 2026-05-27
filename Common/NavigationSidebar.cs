using System;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Shared navigation sidebar injected into child forms.
    /// Design deliberately matches frmDashboard's sidebar (navy / gold theme).
    /// Uses no AutoSize and no SizeChanged handlers to avoid WinForms layout loops.
    /// </summary>
    public static class NavigationSidebar
    {
        private const int SidebarWidth  = 225;
        private const int NavItemHeight = 42;
        private const int NavItemGap    = 3;

        private static readonly Color NavyBack  = Color.FromArgb(11, 31, 73);
        private static readonly Color NavyDark  = Color.FromArgb(5,  18, 48);
        private static readonly Color NavyHover = Color.FromArgb(22, 44, 90);
        private static readonly Color NavySel   = Color.FromArgb(22, 34, 78);
        private static readonly Color GoldAccent= Color.FromArgb(197, 158, 57);

        // ─────────────────────────────────────────────────────────────────────
        public static void AddTo(Form form)
        {
            if (form.Controls.Find("pnlGlobalSidebar", true).Length > 0) return;

            var sidebar = new Panel
            {
                Name      = "pnlGlobalSidebar",
                Dock      = DockStyle.Left,
                Width     = SidebarWidth,
                BackColor = NavyBack
            };

            // ── Brand header ──────────────────────────────────────────────────
            var brand = BuildBrand();

            var topDivider = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = Color.FromArgb(36, 48, 88)
            };

            // ── Footer: Main Menu button ──────────────────────────────────────
            var bottomDivider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(36, 48, 88)
            };

            var btnHome = MakeButton("← Main Menu",
                () => FormManager.ShowForm<frmDashboard>(form));
            btnHome.Dock      = DockStyle.Bottom;
            btnHome.Height    = 46;
            btnHome.ForeColor = Color.FromArgb(239, 80, 80);
            btnHome.BackColor = NavyDark;
            btnHome.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 18, 18);

            // ── Scrollable nav panel ──────────────────────────────────────────
            // navScroll fills whatever height remains between brand and footer.
            // nav is a plain Panel stacking buttons at fixed Y offsets so that
            // WinForms never needs to run AutoSize and cannot enter a layout loop.
            var navScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = NavyBack
            };

            string active = form.GetType().Name;

            // Build nav items
            string[] labels = {
                "Dashboard", "Students", "Promotion", "Staff",
                "Attendance", "Submit Exams", "Report Cards", "Fees", "Outstanding"
            };
            string[] targets = {
                "frmDashboard", "frmStdView", "frmStudentPromotion", "frmEmpView",
                "frmAttendance", "EXAMS", "EXAMSVIEW", "frmFessPayment", "frmOutstandingFees"
            };

            // Determine inner height from item count so nav is exactly tall enough
            int itemCount  = labels.Length;
            int navPadTop  = 10;
            int navHeight  = navPadTop + itemCount * (NavItemHeight + NavItemGap);

            var nav = new Panel
            {
                Width     = SidebarWidth,
                Height    = navHeight,
                BackColor = NavyBack
            };

            int y = navPadTop;
            for (int i = 0; i < labels.Length; i++)
            {
                bool isActive = active == targets[i];
                var btn = MakeButton(labels[i], MakeAction(form, targets[i]));
                btn.Bounds    = new Rectangle(14, y, SidebarWidth - 28, NavItemHeight);
                btn.BackColor = isActive ? NavySel : NavyBack;
                btn.ForeColor = isActive ? Color.White : Color.FromArgb(165, 182, 205);
                btn.Font      = new Font("Segoe UI", 9.5F,
                                    isActive ? FontStyle.Bold : FontStyle.Regular);
                btn.Padding   = new Padding(isActive ? 18 : 14, 0, 0, 0);

                if (isActive)
                {
                    // Gold left-bar accent drawn at paint time
                    btn.Paint += (s, e) =>
                    {
                        using (var br = new SolidBrush(GoldAccent))
                            e.Graphics.FillRectangle(br, 0, 8, 3, NavItemHeight - 16);
                    };
                }

                nav.Controls.Add(btn);
                y += NavItemHeight + NavItemGap;
            }

            navScroll.Controls.Add(nav);

            // Add to sidebar in correct DockStyle order:
            // Bottom items first, then Fill, then Top items (brand last = topmost)
            sidebar.Controls.Add(btnHome);
            sidebar.Controls.Add(bottomDivider);
            sidebar.Controls.Add(navScroll);   // DockStyle.Fill — takes remaining space
            sidebar.Controls.Add(topDivider);  // DockStyle.Top — sits below brand
            sidebar.Controls.Add(brand);       // DockStyle.Top — topmost

            form.Controls.Add(sidebar);
            // No BringToFront call — DockStyle.Left doesn't overlap DockStyle.Fill content
        }

        // ─────────────────────────────────────────────────────────────────────
        private static Panel BuildBrand()
        {
            var brand = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = NavyBack,
                Padding   = new Padding(18, 14, 14, 10)
            };

            // Gold "K" badge
            var badge = new Panel
            {
                Size      = new Size(40, 40),
                Location  = new Point(18, 20),
                BackColor = Color.Transparent
            };
            badge.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(GoldAccent))
                    g.FillEllipse(br, 0, 0, 39, 39);
                using (var f  = new Font("Georgia", 15F, FontStyle.Bold))
                using (var tb = new SolidBrush(Color.FromArgb(8, 14, 52)))
                {
                    var sf = new StringFormat
                    {
                        Alignment     = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("K", f, tb, new RectangleF(0, 0, 40, 40), sf);
                }
            };

            brand.Controls.Add(badge);
            brand.Controls.Add(new Label
            {
                Text      = "KPS Admin",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Bounds    = new Rectangle(66, 18, 148, 22),
                BackColor = Color.Transparent
            });
            brand.Controls.Add(new Label
            {
                Text      = "School Management",
                ForeColor = Color.FromArgb(130, 150, 180),
                Font      = new Font("Segoe UI", 8F),
                Bounds    = new Rectangle(66, 40, 148, 18),
                BackColor = Color.Transparent
            });

            return brand;
        }

        private static Button MakeButton(string text, Action action)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(165, 182, 205),
                BackColor = NavyBack,
                Font      = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = NavyHover;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 22, 58);
            if (action != null)
                btn.Click += (s, e) => action();
            return btn;
        }

        private static Action MakeAction(Form form, string target)
        {
            return () =>
            {
                // Guard: already on this form
                if (form.GetType().Name == target) return;

                switch (target)
                {
                    case "frmDashboard":
                        FormManager.ShowForm<frmDashboard>(form);       break;
                    case "frmStdView":
                        FormManager.ShowForm<frmStdView>(form);         break;
                    case "frmStudentPromotion":
                        FormManager.ShowForm<frmStudentPromotion>(form);break;
                    case "frmEmpView":
                        FormManager.ShowForm<frmEmpView>(form);         break;
                    case "frmAttendance":
                        FormManager.ShowForm<frmAttendance>(form);      break;
                    case "EXAMS":
                        FormManager.ShowForm<EXAMS>(form);              break;
                    case "EXAMSVIEW":
                        FormManager.ShowForm<EXAMSVIEW>(form);          break;
                    case "frmFessPayment":
                        FormManager.ShowForm<frmFessPayment>(form);     break;
                    case "frmOutstandingFees":
                        FormManager.ShowForm<frmOutstandingFees>(form); break;
                }
            };
        }
    }
}
