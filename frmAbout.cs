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
        }

        private void BuildModernAboutView()
        {
            SuspendLayout();
            Controls.Clear();
            BackColor = Color.FromArgb(246, 248, 251);
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 420);

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 1,
                BackColor = Color.White,
                Padding = new Padding(32)
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            card.Controls.Add(new Label { Dock = DockStyle.Fill, Text = string.IsNullOrWhiteSpace(AssemblyProduct) ? "Neat Academy" : AssemblyProduct, ForeColor = Color.FromArgb(25, 36, 49), Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            card.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Version " + AssemblyVersion, ForeColor = Color.FromArgb(93, 108, 123), Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            card.Controls.Add(new Label { Dock = DockStyle.Fill, Text = AssemblyCompany, ForeColor = Color.FromArgb(93, 108, 123), Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            card.Controls.Add(new Label { Dock = DockStyle.Fill, Text = AssemblyCopyright, ForeColor = Color.FromArgb(93, 108, 123), Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
            card.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(248, 250, 252), Text = string.IsNullOrWhiteSpace(AssemblyDescription) ? "School management system for admissions, employees, fees, exams, and leave workflows." : AssemblyDescription, Font = new Font("Segoe UI", 10F) }, 0, 4);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.White };
            var close = new Button { Width = 110, Height = 36, Text = "Close", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(31, 99, 198), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
            close.FlatAppearance.BorderColor = Color.FromArgb(31, 99, 198);
            close.Click += okButton_Click;
            actions.Controls.Add(close);
            card.Controls.Add(actions, 0, 6);

            Controls.Add(card);
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
