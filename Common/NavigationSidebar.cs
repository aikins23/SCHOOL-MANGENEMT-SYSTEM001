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
                Width = 232,
                FillColor = UiTheme.Navy,
                Padding = new Padding(14, 20, 14, 14)
            };

            // Branding
            var lblBrand = new Label
            {
                Text = "KINGDOM PREP",
                Dock = DockStyle.Top,
                Height = 48,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            sidebar.Controls.Add(lblBrand);

            var lblCaption = new Label
            {
                Text = "Academic operations",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Color.FromArgb(190, 202, 220),
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.TopLeft
            };
            sidebar.Controls.Add(lblCaption);

            // Nav Buttons
            AddNavButton(sidebar, "Dashboard", "frmDashboard");
            AddNavButton(sidebar, "Students", "frmStdView");
            AddNavButton(sidebar, "Promotion", "frmStudentPromotion");
            AddNavButton(sidebar, "Staff", "frmEmpView");
            AddNavButton(sidebar, "Attendance", "frmAttendance");
            AddNavButton(sidebar, "Submit Exams", "EXAMS");
            AddNavButton(sidebar, "Report Cards", "EXAMSVIEW");
            AddNavButton(sidebar, "Fees", "frmFess");
            AddNavButton(sidebar, "Outstanding", "frmOutstandingFees");
            
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
            btn.Height = 44;
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
                OnHoverForeColor = Color.White,
                Radius = 6,
                Margin = new Padding(0, 0, 0, 6)
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
                case "frmStudentPromotion": FormManager.ShowForm<frmStudentPromotion>(current); break;
                case "frmEmpView": FormManager.ShowForm<frmEmpView>(current); break;
                case "frmAttendance": FormManager.ShowForm<frmAttendance>(current); break;
                case "EXAMS": FormManager.ShowForm<EXAMS>(current); break;
                case "EXAMSVIEW": FormManager.ShowForm<EXAMSVIEW>(current); break;
                case "frmFess": FormManager.ShowForm<frmFess>(current); break;
                case "frmOutstandingFees": FormManager.ShowForm<frmOutstandingFees>(current); break;
            }
        }
    }
}
