using System;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Shared navigation sidebar injected into child forms so users can jump
    /// between sections without returning to the dashboard first.
    /// Layout: fixed brand header → scrollable nav list → fixed footer button.
    /// </summary>
    public static class NavigationSidebar
    {
        private const int SidebarWidth = 225;
        private static readonly Color NavyBack   = Color.FromArgb(11, 31, 73);
        private static readonly Color NavyDark   = Color.FromArgb(5,  18, 48);
        private static readonly Color NavyHover  = Color.FromArgb(22, 44, 90);
        private static readonly Color GoldAccent = Color.FromArgb(197, 158, 57);

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

            // ── Brand header (fixed top) ──────────────────────────────────────
            var brand = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = NavyBack,
                Padding   = new Padding(18, 14, 14, 10)
            };

            // Gold circular "K" badge
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
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });
            brand.Controls.Add(new Label
            {
                Text      = "School Management",
                ForeColor = Color.FromArgb(130, 150, 180),
                Font      = new Font("Segoe UI", 8F),
                Bounds    = new Rectangle(66, 40, 148, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });

            var topDivider = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = Color.FromArgb(36, 48, 88)
            };

            // ── Scrollable nav list (fills middle) ────────────────────────────
            var navScroll = new Panel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = NavyBack
            };

            var nav = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Padding       = new Padding(14, 10, 14, 0),
                BackColor     = NavyBack
            };
            navScroll.SizeChanged += (s, e) => nav.Width = navScroll.ClientSize.Width;

            string currentForm = form.GetType().Name;

            AddNavItem(nav, "Dashboard",      "frmDashboard",         currentForm, form);
            AddNavItem(nav, "Students",       "frmStdView",           currentForm, form);
            AddNavItem(nav, "Promotion",      "frmStudentPromotion",  currentForm, form);
            AddNavItem(nav, "Staff",          "frmEmpView",           currentForm, form);
            AddNavItem(nav, "Attendance",     "frmAttendance",        currentForm, form);
            AddNavItem(nav, "Submit Exams",   "EXAMS",                currentForm, form);
            AddNavItem(nav, "Report Cards",   "EXAMSVIEW",            currentForm, form);
            AddNavItem(nav, "Fees",           "frmFessPayment",       currentForm, form);
            AddNavItem(nav, "Outstanding",    "frmOutstandingFees",   currentForm, form);

            navScroll.Controls.Add(nav);

            // ── Footer divider + Main Menu button (fixed bottom) ──────────────
            var bottomDivider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(36, 48, 88)
            };

            var btnHome = CreateNavButton("← Main Menu", () => FormManager.ShowForm<frmDashboard>(form));
            btnHome.Dock      = DockStyle.Bottom;
            btnHome.Height    = 46;
            btnHome.ForeColor = Color.FromArgb(239, 80, 80);
            btnHome.BackColor = NavyDark;
            btnHome.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 20, 20);

            // Add in correct order: Bottom items first, then Fill, then Top
            sidebar.Controls.Add(btnHome);
            sidebar.Controls.Add(bottomDivider);
            sidebar.Controls.Add(navScroll);   // DockStyle.Fill
            sidebar.Controls.Add(topDivider);  // DockStyle.Top (below brand)
            sidebar.Controls.Add(brand);       // DockStyle.Top (topmost)

            form.Controls.Add(sidebar);
            // Don't call BringToFront — DockStyle.Left automatically sits
            // beside DockStyle.Fill content without overlapping.
        }

        private static void AddNavItem(FlowLayoutPanel nav, string text, string targetForm,
                                       string currentForm, Form form)
        {
            bool isActive = currentForm == targetForm;
            var btn = CreateNavButton(text, () => NavigateTo(form, targetForm));
            btn.Width     = 185;
            btn.Height    = 42;
            btn.Margin    = new Padding(0, 0, 0, 3);
            btn.BackColor = isActive ? Color.FromArgb(22, 44, 90) : NavyBack;
            btn.ForeColor = isActive ? Color.White : Color.FromArgb(165, 182, 205);
            btn.Font      = new Font("Segoe UI", 9.5F,
                                     isActive ? FontStyle.Bold : FontStyle.Regular);
            btn.Padding   = new Padding(isActive ? 18 : 14, 0, 0, 0);

            // Gold left-bar accent on active item
            if (isActive)
            {
                btn.Paint += (s, e) =>
                {
                    using (var br = new SolidBrush(GoldAccent))
                        e.Graphics.FillRectangle(br, 0, 8, 3, btn.Height - 16);
                };
            }

            nav.Controls.Add(btn);
        }

        private static Button CreateNavButton(string text, Action action)
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
            btn.Click += (s, e) => action();
            return btn;
        }

        private static void NavigateTo(Form current, string formName)
        {
            if (current.GetType().Name == formName) return;
            switch (formName)
            {
                case "frmDashboard":        FormManager.ShowForm<frmDashboard>(current);        break;
                case "frmStdView":          FormManager.ShowForm<frmStdView>(current);          break;
                case "frmStudentPromotion": FormManager.ShowForm<frmStudentPromotion>(current); break;
                case "frmEmpView":          FormManager.ShowForm<frmEmpView>(current);          break;
                case "frmAttendance":       FormManager.ShowForm<frmAttendance>(current);       break;
                case "EXAMS":               FormManager.ShowForm<EXAMS>(current);               break;
                case "EXAMSVIEW":           FormManager.ShowForm<EXAMSVIEW>(current);           break;
                case "frmFessPayment":      FormManager.ShowForm<frmFessPayment>(current);      break;
                case "frmOutstandingFees":  FormManager.ShowForm<frmOutstandingFees>(current);  break;
            }
        }
    }
}
