using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle);
            BuildModernAboutView();

            // Wire events commented-out in designer
            okButton.Click += okButton_Click;
        }

        private void BuildModernAboutView()
        {
            SuspendLayout();
            Controls.Clear();
            UiTheme.Apply(this);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = UiTheme.Navy };

            var titleLbl = new Label { Text = string.IsNullOrWhiteSpace(AssemblyProduct) ? "Nyansapo School ERP" : AssemblyProduct, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold), AutoSize = true, Left = 30, Top = 30 };
            var versionLbl = new Label { Text = "Version " + AssemblyVersion, ForeColor = UiTheme.Gold, Font = new Font("Segoe UI", 10F), AutoSize = true, Left = 34, Top = 74 };
            topPanel.Controls.Add(titleLbl);
            topPanel.Controls.Add(versionLbl);

            var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page, Padding = new Padding(30) };

            var descBox = new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = UiTheme.Page, ForeColor = UiTheme.Text, Text = string.IsNullOrWhiteSpace(AssemblyDescription) ? "Comprehensive School Management System for admissions, employees, fees, exams, and workflows." : AssemblyDescription, Font = new Font("Segoe UI", 11F), Dock = DockStyle.Top, Height = 60 };

            var companyLbl = new Label { Text = "Developed by: " + (string.IsNullOrWhiteSpace(AssemblyCompany) ? "Dartek Integration" : AssemblyCompany), ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10F), AutoSize = true, Top = 100, Left = 30 };
            var copyLbl = new Label { Text = string.IsNullOrWhiteSpace(AssemblyCopyright) ? "© Nyansapo ERP. All rights reserved." : AssemblyCopyright, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 9F), AutoSize = true, Top = 130, Left = 30 };

            mainPanel.Controls.Add(copyLbl);
            mainPanel.Controls.Add(companyLbl);
            mainPanel.Controls.Add(descBox);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = UiTheme.Surface };
            var closeBtn = new Button { Text = "Close", Width = 120, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Navy, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Top = 20;
            closeBtn.Left = ClientSize.Width - closeBtn.Width - 30;
            closeBtn.Click += okButton_Click;
            bottomPanel.Controls.Add(closeBtn);

            Controls.Add(mainPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            ResumeLayout(true);
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
