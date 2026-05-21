using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI.WinForms;
using Guna.UI2.WinForms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public static class NavigationSidebar
    {
        public static void AddTo(Form form)
        {
            // Only add if not already present
            if (form.Controls.Find("pnlGlobalSidebar", true).Length > 0) return;

            var sidebar = new Guna2Panel
            {
                Name = "pnlGlobalSidebar",
                Dock = DockStyle.Left,
                Width = 220,
                FillColor = UiTheme.Navy,
                Padding = new Padding(0, 20, 0, 0)
            };

            // Branding
            var lblBrand = new Label
            {
                Text = "KINGDOM PREP",
                Dock = DockStyle.Top,
                Height = 60,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            sidebar.Controls.Add(lblBrand);

            // Nav Buttons
            AddNavButton(sidebar, "Dashboard", "frmDashboard");
            AddNavButton(sidebar, "Students", "frmStdView");
            AddNavButton(sidebar, "Staff", "frmEmpView");
            AddNavButton(sidebar, "Attendance", "frmAttendance");
            AddNavButton(sidebar, "Exams", "EXAMSVIEW");
            AddNavButton(sidebar, "Fees", "frmFess");
            
            // Back/Home at bottom
            var btnHome = CreateButton("Main Menu", () => FormManager.ShowForm<frmDashboard>(form));
            btnHome.Dock = DockStyle.Bottom;
            btnHome.Height = 50;
            btnHome.BaseColor = Color.Transparent;
            btnHome.OnHoverBaseColor = UiTheme.NavyHover;
            sidebar.Controls.Add(btnHome);

            form.Controls.Add(sidebar);
            sidebar.BringToFront();
        }

        private static void AddNavButton(Panel parent, string text, string formName)
        {
            var btn = CreateButton(text, () => NavigateTo(parent.FindForm(), formName));
            btn.Dock = DockStyle.Top;
            btn.Height = 50;
            btn.TextAlign = HorizontalAlignment.Left;
            parent.Controls.Add(btn);
            btn.BringToFront();
        }

        private static GunaButton CreateButton(string text, Action action)
        {
            var btn = new GunaButton
            {
                Text = "  " + text,
                BaseColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10),
                BorderSize = 0,
                Cursor = Cursors.Hand,
                AnimationHoverSpeed = 0.07F,
                AnimationSpeed = 0.03F,
                OnHoverBaseColor = UiTheme.NavyHover,
                OnHoverForeColor = Color.White
            };
            btn.Click += (s, e) => action();
            return btn;
        }

        private static void NavigateTo(Form current, string formName)
        {
            if (current.Name == formName) return;

            switch (formName)
            {
                case "frmDashboard": FormManager.ShowForm<frmDashboard>(current); break;
                case "frmStdView": FormManager.ShowForm<frmStdView>(current); break;
                case "frmEmpView": FormManager.ShowForm<frmEmpView>(current); break;
                case "frmAttendance": FormManager.ShowForm<frmAttendance>(current); break;
                case "EXAMSVIEW": FormManager.ShowForm<EXAMSVIEW>(current); break;
                case "frmFess": FormManager.ShowForm<frmFess>(current); break;
            }
        }
    }
}
