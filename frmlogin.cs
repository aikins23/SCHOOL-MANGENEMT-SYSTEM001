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

        private static readonly Color PageBackColor = Color.FromArgb(248, 246, 239);
        private static readonly Color SurfaceColor = Color.FromArgb(255, 253, 247);
        private static readonly Color PrimaryColor = Color.FromArgb(11, 31, 73);
        private static readonly Color SidebarColor = Color.FromArgb(5, 18, 48);
        private static readonly Color GoldColor = Color.FromArgb(197, 158, 57);
        private static readonly Color GoldSoft = Color.FromArgb(235, 219, 167);
        private static readonly Color TextColor = Color.FromArgb(28, 36, 52);
        private static readonly Color MutedTextColor = Color.FromArgb(105, 113, 130);
        private static readonly Color BorderColor = Color.FromArgb(215, 207, 185);

        public frmlogin()
        {
            InitializeComponent();
            BuildModernLoginView();

            // Event handlers are commented-out in the designer — wire them here
            BTN_Login.Click          += (s, e) => LoginUser();
            Check.CheckedChanged     += Check_CheckedChanged;
            lab_Register.Click       += lab_Register_Click;
        }

        private void BuildModernLoginView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Kingdom Preparatory School - Login";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(940, 580);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor,
                Padding = new Padding(0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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
                Padding = new Padding(38, 36, 38, 32)
            };

            pictureBox1.Dock = DockStyle.Top;
            pictureBox1.Height = 150;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.Transparent;
            
            try
            {
                string logoPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "school_logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    pictureBox1.Image = Image.FromFile(logoPath);
                }
                else
                {
                    // Fallback to searching for the file in the project structure if not in bin
                    string projectLogoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "school_logo.png");
                    if (System.IO.File.Exists(projectLogoPath))
                    {
                        pictureBox1.Image = Image.FromFile(projectLogoPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load login logo: " + ex.Message);
            }

            panel.Controls.Add(pictureBox1);

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Text = "Secure staff access",
                ForeColor = Color.FromArgb(154, 168, 205),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.BottomLeft
            });

            var rule = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = GoldColor,
                Margin = new Padding(0, 8, 0, 16)
            };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 96,
                Text = "Kingdom Preparatory School",
                ForeColor = Color.White,
                Font = new Font("Georgia", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 66,
                Text = "School Management System",
                ForeColor = GoldSoft,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.TopLeft
            };

            var copy = new Label
            {
                Dock = DockStyle.Top,
                Height = 86,
                Text = "Sign in to manage admissions, staff records, fees, attendance, exams, and reports.",
                ForeColor = Color.FromArgb(211, 218, 235),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.TopLeft
            };

            panel.Controls.Add(copy);
            panel.Controls.Add(subtitle);
            panel.Controls.Add(rule);
            panel.Controls.Add(title);

            return panel;
        }

        private Control BuildLoginPanel()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                Padding = new Padding(72, 64, 72, 64)
            };

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 8,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Padding = new Padding(40, 36, 40, 30)
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Welcome Back",
                ForeColor = TextColor,
                Font = new Font("Georgia", 23F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Enter your account credentials to continue.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);

            StyleLoginTextBox(TXTUser, "Username");
            StyleLoginTextBox(TXTPass, "Password");
            TXTPass.PasswordChar = '*';
            card.Controls.Add(CreateField("Username", TXTUser), 0, 2);
            card.Controls.Add(CreateField("Password", TXTPass), 0, 3);

            Check.Text = "Show password";
            Check.Dock = DockStyle.Fill;
            Check.ForeColor = MutedTextColor;
            Check.Font = new Font("Segoe UI", 9.5F);
            Check.FlatStyle = FlatStyle.Flat;
            card.Controls.Add(Check, 0, 4);

            BTN_Login.Text = "Sign in";
            BTN_Login.Dock = DockStyle.Fill;
            BTN_Login.BaseColor = PrimaryColor;
            BTN_Login.ForeColor = Color.White;
            BTN_Login.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            BTN_Login.OnHoverBaseColor = Color.FromArgb(19, 45, 95);
            BTN_Login.OnHoverForeColor = Color.White;
            BTN_Login.Radius = 6;
            card.Controls.Add(BTN_Login, 0, 5);

            lab_Register.Text = "Create a new account";
            lab_Register.Dock = DockStyle.Fill;
            lab_Register.ForeColor = GoldColor;
            lab_Register.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lab_Register.TextAlign = ContentAlignment.MiddleCenter;
            lab_Register.Cursor = Cursors.Hand;
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
            input.Height = 40;
            return panel;
        }

        private void StyleLoginTextBox(Guna.UI2.WinForms.Guna2TextBox textBox, string placeholder)
        {
            textBox.PlaceholderText = placeholder;
            textBox.Font = new Font("Segoe UI", 10F);
            textBox.ForeColor = TextColor;
            textBox.FillColor = Color.White;
            textBox.BorderColor = BorderColor;
            textBox.BorderRadius = 6;
            textBox.BorderThickness = 1;
            textBox.FocusedState.BorderColor = GoldColor;
            textBox.HoverState.BorderColor = GoldColor;
        }
        private void frmlogin_Load(object sender, EventArgs e)
        {

        }

        private void BTN_Login_Click(object sender, EventArgs e) => LoginUser();

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

        /// <summary>
        /// Ensures the (localdb)\KPS LocalDB instance is running before we attempt any
        /// database connection.  LocalDB stops automatically after ~5 minutes of idle;
        /// this call is instant when the instance is already running.
        /// </summary>
        private static void EnsureLocalDbRunning()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = "start KPS",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.WaitForExit(8000); // 8 s max; start usually takes <1 s
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: if sqllocaldb.exe is not on PATH the connection attempt
                // will surface the real error message to the user.
                LoggerHelper.LogWarning($"EnsureLocalDbRunning: {ex.Message}");
            }
        }

        private async void LoginUser()
        {
            try
            {
                if (!FormValidationHelper.ValidateRequired(TXTUser, "Username")) return;
                if (!FormValidationHelper.ValidateRequired(TXTPass, "Password")) return;

                string username = TXTUser.Text.Trim();
                string password = TXTPass.Text;

                if (statusLabel != null) statusLabel.Text = "Connecting to database...";
                EnsureLocalDbRunning();

                if (statusLabel != null) statusLabel.Text = "Authenticating...";

                var (success, message) = await AuthService.LoginAsync(username, password);

                if (success)
                {
                    if (statusLabel != null) statusLabel.Text = "Login successful.";

                    // Attempt to log, but continue even if logging fails
                    try
                    {
                        LoggerHelper.LogInfo($"User logged in: {username}");
                    }
                    catch (Exception logEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log info: {logEx.Message}");
                    }

                    UIHelper.ShowSuccess("Welcome! Loading dashboard...", "Login Success");

                    // Show dashboard then hide login.
                    // We Hide (not Close) because Application.Run(frmlogin) keeps
                    // the message loop alive only while this form exists. Closing it
                    // would exit the app before the dashboard appears.
                    // frmDashboard already calls Application.Exit() in all its
                    // exit paths, which will terminate the process cleanly.
                    var dashboard = new frmDashboard();
                    dashboard.Show();
                    this.Hide();
                }
                else
                {
                    if (statusLabel != null) statusLabel.Text = message;

                    // Attempt to log, but continue even if logging fails
                    try
                    {
                        LoggerHelper.LogWarning($"Login failed for user {username}");
                    }
                    catch (Exception logEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log warning: {logEx.Message}");
                    }

                    UIHelper.ShowWarning(message, "Login Failed");
                    ClearLoginForm();
                }
            }
            catch (Exception ex)
            {
                // Attempt to log the error, but don't let logging failure prevent error display
                try
                {
                    LoggerHelper.LogError("LoginUser failed", ex);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log error: {logEx.Message}\nOriginal error: {ex.Message}");
                }

                if (statusLabel != null) statusLabel.Text = "Login error.";
                UIHelper.ShowError("Login failed: " + ex.Message, "Login");
            }
        }

        private void ClearLoginForm()
        {
            TXTUser.Text = "";
            TXTPass.Text = "";
            TXTUser.Focus();
        }

        private void BTN_Login_Click_1(object sender, EventArgs e) => LoginUser();
    }
}
