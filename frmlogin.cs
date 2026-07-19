using kingdom_Preparatory_School_Management_System;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmlogin : Form
    {
        private Label statusLabel;
        private bool _recoveryPromptOpen;
        private bool _loginInProgress;
        private const int LoginTimeoutSeconds = 20;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color SidebarColor = UiTheme.NavyDark;
        private static readonly Color GoldColor = UiTheme.Gold;
        private static readonly Color GoldSoft = UiTheme.GoldSoft;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public frmlogin()
        {
            InitializeComponent();
            BuildModernLoginView();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;

            // Event handlers are commented-out in the designer — wire them here
            BTN_Login.Click          += async (s, e) => await LoginUserAsync();
            Check.CheckedChanged     += Check_CheckedChanged;
            lab_Register.Click       += lab_Register_Click;
            Load += async (s, e) => await OfferSystemRecoveryIfNeededAsync();
        }

        private void BuildModernLoginView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = $"{Common.AppConfig.ProductName} - Login";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(940, 580);
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;

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
                Padding = new Padding(38, 36, 38, 32)
            };

            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = panel.ClientRectangle;
                if (rect.Width == 0 || rect.Height == 0) return;

                using (var brush = new LinearGradientBrush(
                    rect,
                    PrimaryColor,
                    SidebarColor,
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, rect);
                }

                // Match the right-side border accents from the splash screen
                using (var accent = new SolidBrush(UiTheme.NavySoft))
                    g.FillRectangle(accent, rect.Width - 6, 0, 6, rect.Height);

                using (var gold = new SolidBrush(GoldColor))
                    g.FillRectangle(gold, rect.Width - 2, 0, 2, rect.Height);
            };
            panel.Resize += (s, e) => panel.Invalidate();

            pictureBox1.Dock = DockStyle.Top;
            pictureBox1.Height = 150;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = Branding.GetLogo(onBlue: true);

            panel.Controls.Add(pictureBox1);

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Text = "SECURE STAFF ACCESS",
                ForeColor = /*UiTheme.Muted*/Color.White,
                BackColor = Color.Transparent,
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
                Text = Common.AppConfig.ProductName,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Georgia", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 66,
                Text = "School Management System",
                ForeColor = GoldSoft,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.TopLeft
            };

            var copy = new Label
            {
                Dock = DockStyle.Top,
                Height = 86,
                Text = "Sign in to manage admissions, staff records, fees, attendance, exams, and reports.",
                ForeColor = Color.FromArgb(211, 218, 235),
                BackColor = Color.Transparent,
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

            // Add modern icons using in-memory generation
            textBox.IconLeftSize = new Size(20, 20);
            textBox.IconLeftOffset = new Point(10, 0);
            textBox.TextOffset = new Point(10, 0);

            if (placeholder.ToLower().Contains("user"))
                textBox.IconLeft = CreateModernIcon(IconType.User);
            else if (placeholder.ToLower().Contains("pass"))
                textBox.IconLeft = CreateModernIcon(IconType.Lock);
        }

        private enum IconType { User, Lock }

        private Image CreateModernIcon(IconType type)
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(MutedTextColor, 2F))
                {
                    if (type == IconType.User)
                    {
                        // Head
                        g.DrawEllipse(pen, 10, 6, 12, 12);
                        // Shoulders
                        g.DrawArc(pen, 4, 20, 24, 16, 180, 180);
                    }
                    else if (type == IconType.Lock)
                    {
                        // Shackle
                        g.DrawArc(pen, 9, 6, 14, 14, 180, 180);
                        // Body
                        g.DrawRectangle(pen, 8, 16, 16, 10);
                        // Keyhole
                        g.FillEllipse(new SolidBrush(MutedTextColor), 14, 19, 4, 4);
                    }
                }
            }
            return bmp;
        }
        private void frmlogin_Load(object sender, EventArgs e)
        {

        }

        private async void BTN_Login_Click(object sender, EventArgs e) => await LoginUserAsync();

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
        /// Re-wakes KPS in case it auto-stopped while the user was sitting on the
        /// login screen.  The actual implementation lives in Program.EnsureLocalDbRunning
        /// so startup and login both go through the same path.
        /// </summary>
        private static void EnsureLocalDbRunning() => Program.EnsureLocalDbRunning();

        private async Task<bool> OfferSystemRecoveryIfNeededAsync()
        {
            if (_recoveryPromptOpen) return false;

            try
            {
                var health = await Task.Run(async () =>
                {
                    EnsureLocalDbRunning();
                    return await new kingdom_Preparatory_School_Management_System.Data.SchoolInfoRepository(AppConfig.ConnectionString)
                        .GetIdentityHealthAsync()
                        .ConfigureAwait(false);
                });

                if (!health.SetupAuditFound || health.UserCount != 0)
                {
                    return false;
                }

                _recoveryPromptOpen = true;
                if (statusLabel != null)
                    statusLabel.Text = "System recovery is required before sign in.";

                using (var recovery = new frmSystemRecovery())
                {
                    var result = recovery.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        if (statusLabel != null)
                            statusLabel.Text = "Recovery complete. Sign in with the new administrator account.";
                        ClearLoginForm();
                    }
                    else if (statusLabel != null)
                    {
                        statusLabel.Text = "Recovery was cancelled. Sign in is unavailable until an administrator account exists.";
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("System recovery check skipped: " + ex.Message);
                return false;
            }
            finally
            {
                _recoveryPromptOpen = false;
            }
        }

        private async Task LoginUserAsync()
        {
            if (_loginInProgress) return;

            try
            {
                if (!FormValidationHelper.ValidateRequired(TXTUser, "Username")) return;
                if (!FormValidationHelper.ValidateRequired(TXTPass, "Password")) return;

                string username = TXTUser.Text.Trim();
                string password = TXTPass.Text;

                SetLoginBusy(true);
                if (statusLabel != null) statusLabel.Text = "Connecting to database...";

                if (statusLabel != null) statusLabel.Text = "Authenticating...";

                var loginTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
                var loginTask = Task.Run(async () =>
                {
                    EnsureLocalDbRunning();
                    return await AuthService.LoginAsync(username, password, loginTimeout.Token).ConfigureAwait(false);
                }, loginTimeout.Token);

                var completedTask = await Task.WhenAny(loginTask, Task.Delay(TimeSpan.FromSeconds(LoginTimeoutSeconds)));
                if (completedTask != loginTask)
                {
                    loginTimeout.Cancel();
                    _ = loginTask.ContinueWith(t => loginTimeout.Dispose());
                    if (statusLabel != null) statusLabel.Text = "Database did not respond.";
                    UIHelper.ShowWarning(
                        "The database did not respond within 20 seconds. Check SQL Server/LocalDB and try again.",
                        "Login Timeout");
                    return;
                }

                var (success, message) = await loginTask;
                loginTimeout.Dispose();

                if (success)
                {
                    // Parents never use the desktop app — they have a separate web portal.
                    if (AuthService.CurrentUser.Role == AuthService.UserRole.Parent)
                    {
                        AuthService.Logout();
                        if (statusLabel != null) statusLabel.Text = "Use the parent web portal.";
                        UIHelper.ShowWarning(
                            "Parent accounts cannot sign in here. Please use the parent web portal.",
                            "Wrong portal");
                        ClearLoginForm();
                        return;
                    }

                    if (statusLabel != null) statusLabel.Text = "Login successful.";
                    _ = DynamicPermissionService.RefreshCurrentUserPermissionsAsync();

                    // Attempt to log, but continue even if logging fails
                    try
                    {
                        LoggerHelper.LogInfo($"User logged in: {username}");
                    }
                    catch (Exception logEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log info: {logEx.Message}");
                    }

                    // Route to the role-appropriate dashboard.
                    // We Hide (not Close) because Application.Run(frmlogin) keeps
                    // the message loop alive only while this form exists. Closing
                    // it would exit the app before the dashboard appears.
                    Form dashboard;
                    if (AuthService.CurrentUser.Role == AuthService.UserRole.Teacher)
                    {
                        LoggerHelper.LogInfo($"Creating teacher dashboard for {username}");
                        dashboard = new frmTeacherDashboard();
                    }
                    else
                    {
                        LoggerHelper.LogInfo($"Creating main dashboard for {username}");
                        dashboard = new frmDashboard();
                    }
                    LoggerHelper.LogInfo($"Opening dashboard for {username} as {AuthService.CurrentUser.Role}");
                    dashboard.Show();
                    this.Hide();
                }
                else
                {
                    if (statusLabel != null) statusLabel.Text = message;
                    bool openedRecovery = false;
                    if (ShouldOfferRecovery(message))
                    {
                        if (statusLabel != null) statusLabel.Text = "Checking recovery status...";
                        openedRecovery = await OfferSystemRecoveryIfNeededWithTimeoutAsync();
                    }

                    // Attempt to log, but continue even if logging fails
                    try
                    {
                        LoggerHelper.LogWarning($"Login failed for user {username}");
                    }
                    catch (Exception logEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log warning: {logEx.Message}");
                    }

                    if (!openedRecovery)
                    {
                        UIHelper.ShowWarning(message, "Login Failed");
                    }
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
            finally
            {
                SetLoginBusy(false);
            }
        }

        private static bool ShouldOfferRecovery(string message)
        {
            return !string.IsNullOrWhiteSpace(message)
                && message.IndexOf("No user accounts were found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<bool> OfferSystemRecoveryIfNeededWithTimeoutAsync()
        {
            var recoveryTask = OfferSystemRecoveryIfNeededAsync();
            var completedTask = await Task.WhenAny(recoveryTask, Task.Delay(TimeSpan.FromSeconds(8)));
            if (completedTask == recoveryTask)
            {
                return await recoveryTask;
            }

            LoggerHelper.LogWarning("System recovery check timed out during login.");
            if (statusLabel != null) statusLabel.Text = "Recovery check timed out.";
            return false;
        }

        private void SetLoginBusy(bool busy)
        {
            _loginInProgress = busy;
            BTN_Login.Enabled = !busy;
            TXTUser.Enabled = !busy;
            TXTPass.Enabled = !busy;
            Check.Enabled = !busy;
            lab_Register.Enabled = !busy;
            UseWaitCursor = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            BTN_Login.Text = busy ? "Signing in..." : "Sign in";
        }

        private void ClearLoginForm()
        {
            TXTUser.Text = "";
            TXTPass.Text = "";
            TXTUser.Focus();
        }

        private void ShowLoginSuccessPrompt(string username, AuthService.UserRole role)
        {
            using (var dialog = new LoginSuccessDialog(username, role.ToString()))
            {
                dialog.ShowDialog(this);
            }
        }

        private async void BTN_Login_Click_1(object sender, EventArgs e) => await LoginUserAsync();

        private sealed class LoginSuccessDialog : Form
        {
            private static readonly Color DialogBackColor = UiTheme.Surface;
            private static readonly Color DialogBorderColor = UiTheme.Border;
            private static readonly Color DialogTextColor = UiTheme.Text;
            private static readonly Color DialogMutedColor = UiTheme.Muted;
            private static readonly Color DialogSuccessColor = UiTheme.Success;

            private readonly Timer _autoCloseTimer;
            private int _secondsRemaining = 3;
            private readonly Button _continueButton;

            public LoginSuccessDialog(string username, string roleName)
            {
                Text = "Login Successful";
                Width = 340;  // Reduced from 400
                Height = 250; // Reduced from 300
                BackColor = DialogBackColor;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterParent;
                ShowInTaskbar = false;
                Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                KeyPreview = true;
                Opacity = 0; // For fade-in animation

                var content = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = DialogBackColor,
                    ColumnCount = 1,
                    RowCount = 6,
                    Padding = new Padding(24, 18, 24, 18)
                };
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // Icon
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Title
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // Welcome message
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); // Role info
                content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Spacer
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Button
                Controls.Add(content);

                var iconContainer = new Panel
                {
                    Width = 48, // Reduced from 64
                    Height = 48,
                    Anchor = AnchorStyles.None,
                    BackColor = Color.Transparent
                };
                iconContainer.Paint += PaintSuccessIcon;
                content.Controls.Add(iconContainer, 0, 0);

                content.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Login Successful",
                    ForeColor = DialogTextColor,
                    Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold), // Reduced from 18
                    TextAlign = ContentAlignment.MiddleCenter
                }, 0, 1);

                content.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = $"Welcome back, {username}",
                    ForeColor = TextColor,
                    Font = new Font("Segoe UI", 10F), // Reduced from 10.5
                    TextAlign = ContentAlignment.MiddleCenter
                }, 0, 2);

                content.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = $"Authorized as {roleName}",
                    ForeColor = DialogMutedColor,
                    Font = new Font("Segoe UI", 8.5F), // Reduced from 9
                    TextAlign = ContentAlignment.MiddleCenter
                }, 0, 3);

                _continueButton = new Button
                {
                    Width = 160, // Reduced from 180
                    Height = 36,  // Reduced from 38
                    Anchor = AnchorStyles.None,
                    Text = $"Continue ({_secondsRemaining}s)",
                    BackColor = PrimaryColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.OK
                };
                _continueButton.FlatAppearance.BorderSize = 0;
                content.Controls.Add(_continueButton, 0, 5);

                // Animations & Timers
                var fadeInTimer = new Timer { Interval = 15 };
                fadeInTimer.Tick += (s, e) =>
                {
                    if (Opacity < 1) Opacity += 0.1;
                    else fadeInTimer.Stop();
                };

                _autoCloseTimer = new Timer { Interval = 1000 };
                _autoCloseTimer.Tick += (s, e) =>
                {
                    _secondsRemaining--;
                    if (_secondsRemaining <= 0)
                    {
                        _autoCloseTimer.Stop();
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        _continueButton.Text = $"Continue ({_secondsRemaining}s)";
                    }
                };

                Load += (s, e) =>
                {
                    fadeInTimer.Start();
                    _autoCloseTimer.Start();
                };

                AcceptButton = _continueButton;
                KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                    {
                        _autoCloseTimer.Stop();
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                };
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    const int CS_DROPSHADOW = 0x00020000;
                    var cp = base.CreateParams;
                    cp.ClassStyle |= CS_DROPSHADOW;
                    return cp;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                // Subtle border
                using (var pen = new Pen(DialogBorderColor, 1))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            private void PaintSuccessIcon(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int size = 48; // Reduced from 64

                using (var pen = new Pen(DialogSuccessColor, 2.5F))
                using (var bgBrush = new SolidBrush(UiTheme.SurfaceAlt))
                {
                    // Draw a subtle soft background circle
                    e.Graphics.FillEllipse(bgBrush, 2, 2, size - 4, size - 4);

                    // Draw a thin, modern outer circle
                    e.Graphics.DrawEllipse(pen, 2, 2, size - 4, size - 4);

                    // Draw a sleek, thin modern checkmark
                    pen.Width = 3F;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;

                    PointF[] points =
                    {
                        new PointF(size * 0.30f, size * 0.52f),
                        new PointF(size * 0.45f, size * 0.67f),
                        new PointF(size * 0.70f, size * 0.35f)
                    };
                    e.Graphics.DrawLines(pen, points);
                }
            }
        }
    }
}
