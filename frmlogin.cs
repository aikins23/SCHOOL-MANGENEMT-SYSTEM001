using kingdom_Preparatory_School_Management_System;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmlogin : Form
    {
        private Label statusLabel;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color SidebarColor = Color.FromArgb(17, 35, 58);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        public frmlogin()
        {
            InitializeComponent();
            BuildModernLoginView();
        }

        private void BuildModernLoginView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Login";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(920, 560);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));

            root.Controls.Add(BuildBrandPanel(), 0, 0);
            root.Controls.Add(BuildLoginPanel(), 1, 0);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildBrandPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarColor,
                Padding = new Padding(34)
            };

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Text = "Neat Academy",
                ForeColor = Color.FromArgb(191, 219, 254),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            });

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 140,
                Text = "School Management System",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };
            panel.Controls.Add(title);

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 86,
                Text = "Secure access for admissions, employees, fees, exams, and leave workflows.",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.TopLeft
            });

            return panel;
        }

        private Control BuildLoginPanel()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                Padding = new Padding(62, 60, 62, 60)
            };

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 8,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Padding = new Padding(34)
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Sign in",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Use your registered account to continue.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);

            TXTUser.PlaceholderText = "";
            TXTPass.PlaceholderText = "";
            TXTPass.PasswordChar = '*';
            card.Controls.Add(CreateField("Username", TXTUser), 0, 2);
            card.Controls.Add(CreateField("Password", TXTPass), 0, 3);

            Check.Text = "Show password";
            Check.Dock = DockStyle.Fill;
            Check.ForeColor = MutedTextColor;
            Check.Font = new Font("Segoe UI", 9.5F);
            card.Controls.Add(Check, 0, 4);

            BTN_Login.Text = "Sign in";
            BTN_Login.Dock = DockStyle.Fill;
            BTN_Login.BaseColor = PrimaryColor;
            BTN_Login.ForeColor = Color.White;
            card.Controls.Add(BTN_Login, 0, 5);

            lab_Register.Text = "Create a new account";
            lab_Register.Dock = DockStyle.Fill;
            lab_Register.ForeColor = PrimaryColor;
            lab_Register.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lab_Register.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lab_Register, 0, 6);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft
            };
            card.Controls.Add(statusLabel, 0, 7);

            shell.Controls.Add(card);
            return shell;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = SurfaceColor
            };
            panel.Controls.Add(input);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = labelText,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            });
            input.Dock = DockStyle.Bottom;
            input.Height = 34;
            return panel;
        }
        private void frmlogin_Load(object sender, EventArgs e)
        {

        }

        private void BTN_Login_Click(object sender, EventArgs e)
        {
            LoginUser();
        }

        private void lab_Register_Click(object sender, EventArgs e)
        {
            new frmRegistration().Show();
            this.Hide();
        }

        private void Check_CheckedChanged(object sender, EventArgs e)
        {
            TXTPass.PasswordChar = Check.Checked ? '\0' : '*';

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void LoginUser()
        {
            string username = TXTUser.Text.Trim();
            string password = TXTPass.Text;

            if (statusLabel != null) statusLabel.Text = "Authenticating...";
            
            var (success, message) = await AuthService.LoginAsync(username, password);

            if (success)
            {
                if (statusLabel != null) statusLabel.Text = "Login successful.";
                UIHelper.ShowSuccess("Welcome! Loading dashboard...", "Login Success");

                // Open dashboard and close login
                new frmDashboard().Show();
                this.Close();
            }
            else
            {
                if (statusLabel != null) statusLabel.Text = message;
                UIHelper.ShowWarning(message, "Login Failed");
                ClearLoginForm();
            }
        }

        private void ClearLoginForm()
        {
            TXTUser.Text = "";
            TXTPass.Text = "";
            TXTUser.Focus();
        }

        private void TryUpgradePasswordHash(OleDbConnection connection, string username, string password, string storedPassword)
        {
            if (AuthService.IsHashedPassword(storedPassword))
            {
                return;
            }

            try
            {
                AuthService.EnsurePasswordColumns(connection);
                string passwordHash = AuthService.HashPassword(password);
                using (var command = new OleDbCommand("UPDATE Users SET [Password] = ?, Con_Password = ? WHERE Username = ?", connection))
                {
                    command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("?", OleDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("?", OleDbType.VarChar).Value = username;
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // Keep login working if an older database cannot be upgraded yet.
            }
        }

        private void BTN_Login_Click_1(object sender, EventArgs e)
        {
            LoginUser();
        }
    }
}
