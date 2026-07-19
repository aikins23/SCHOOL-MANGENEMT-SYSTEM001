using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Guided first-run setup shown before login when a fresh installation has no users.
    /// It can also be previewed with --first-run-preview without clearing the real database.
    /// </summary>
    public class frmFirstRunSetup : Form
    {
        private readonly bool _previewMode;
        private TextBox _schoolName;
        private TextBox _address;
        private TextBox _poBox;
        private TextBox _gps;
        private TextBox _phone1;
        private TextBox _phone2;
        private TextBox _email;
        private TextBox _portalUrl;
        private TextBox _username;
        private TextBox _password;
        private TextBox _confirmPassword;
        private Label _schoolIdLabel;
        private Label _status;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _stepLabel;
        private Panel _contentHost;
        private Control _schoolStepView;
        private Control _adminStepView;
        private Control _reviewStepView;
        private Button _backButton;
        private Button _nextButton;
        private PictureBox _logo;
        private byte[] _logoBytes;
        private Guid _schoolId = Guid.NewGuid();
        private int _currentStep = 1;

        public frmFirstRunSetup(bool previewMode = false)
        {
            _previewMode = previewMode;
            BuildView();
            Load += async (s, e) => await LoadDefaultsAsync();
        }

        public static bool ForcePreviewRequested()
        {
            return Environment.GetCommandLineArgs()
                       .Any(a => string.Equals(a, "--first-run-preview", StringComparison.OrdinalIgnoreCase))
                   || string.Equals(Environment.GetEnvironmentVariable("NYANSAPO_FIRST_RUN_PREVIEW"), "1", StringComparison.OrdinalIgnoreCase);
        }

        private static Image FirstRunBrandLogo(bool onBlue)
        {
            return onBlue
                ? (Branding.AppLogoOnBlue ?? Branding.AppLogo ?? Branding.IconBgImage)
                : (Branding.AppLogo ?? Branding.IconBgImage ?? Branding.AppLogoOnBlue);
        }

        public static async Task<bool> ShouldShowAsync()
        {
            try
            {
                await AuthService.EnsureDatabaseSetupAsync();
                var repo = new SchoolInfoRepository(AppConfig.ConnectionString);
                var health = await repo.GetIdentityHealthAsync();
                return health.UserCount == 0 && !health.SetupAuditFound;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("First-run check failed: " + ex.Message);
                return false;
            }
        }

        private void BuildView()
        {
            Text = _previewMode ? "First-Time Setup Preview" : "First-Time School Setup";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 780);
            ClientSize = new Size(1320, 840);
            BackColor = Color.FromArgb(249, 250, 251); // Softer modern gray background
            Font = new Font("Segoe UI", 10F);
            Icon = Branding.AppIcon;
            DoubleBuffered = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(BuildBrandPanel(), 0, 0);
            root.Controls.Add(BuildFormPanel(), 1, 0);
        }

        private Control BuildBrandPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(38, 46, 34, 38), BackColor = Color.FromArgb(15, 23, 42) };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(panel.ClientRectangle, Color.FromArgb(15, 23, 42), Color.FromArgb(2, 6, 23), LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(brush, panel.ClientRectangle);
                using (var gold = new SolidBrush(Color.FromArgb(212, 175, 55)))
                    e.Graphics.FillRectangle(gold, panel.Width - 4, 0, 4, panel.Height);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 162));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            panel.Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "NYANSAPO ERP",
                ForeColor = Color.FromArgb(212, 175, 55),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var divider = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 10, 0, 10) };
            divider.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, 212, 175, 55)))
                    e.Graphics.DrawLine(pen, 0, divider.Height / 2, divider.Width / 2, divider.Height / 2);
            };
            layout.Controls.Add(divider, 0, 1);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Set Up Your\nSchool Workspace",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "A guided first-run setup for school identity, branding, and administrator access.",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.TopLeft
            }, 0, 3);

            layout.Controls.Add(new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = FirstRunBrandLogo(onBlue: true),
                BackColor = Color.Transparent,
                Padding = new Padding(18, 18, 18, 18)
            }, 0, 4);

            var infoPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 20, 0) };
            infoPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                using (var path = GetRoundedPath(new Rectangle(0, 0, infoPanel.Width - 1, infoPanel.Height - 1), 8))
                    e.Graphics.FillPath(brush, path);
            };
            infoPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "The School ID created here stays permanent and protects each school's records.",
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                Padding = new Padding(20, 16, 20, 16),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            });
            layout.Controls.Add(infoPanel, 0, 6);

            return panel;
        }

        private Control BuildFormPanel()
        {
            var shell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(70, 48, 70, 44), BackColor = Color.FromArgb(249, 250, 251) };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            shell.Controls.Add(layout);

            layout.Controls.Add(Header(), 0, 0);

            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            layout.Controls.Add(_contentHost, 0, 1);

            _status = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(_status, 0, 2);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };
            _nextButton = Button("Continue", UiTheme.Navy, Color.White);
            _nextButton.Width = 200;
            _nextButton.Click += async (s, e) => await MoveNextAsync();
            actions.Controls.Add(_nextButton);

            if (_previewMode)
            {
                var skip = Button("Close Preview", Color.White, Color.FromArgb(71, 85, 105), true);
                skip.Width = 150;
                skip.Click += (s, e) => Close();
                actions.Controls.Add(skip);
            }

            _backButton = Button("Back", Color.White, Color.FromArgb(71, 85, 105), true);
            _backButton.Width = 120;
            _backButton.Click += (s, e) =>
            {
                if (_currentStep > 1) _currentStep--;
                ShowStep();
            };
            actions.Controls.Add(_backButton);

            layout.Controls.Add(actions, 0, 3);
            ShowStep();
            return shell;
        }

        private Control Header()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 10)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

            var titleStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            titleStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI Semibold", 23F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.TopLeft
            };

            _stepLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Margin = new Padding(0, 18, 0, 0),
                BackColor = Color.FromArgb(255, 247, 220),
                ForeColor = Color.FromArgb(146, 64, 14),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _titleLabel = title;
            _subtitleLabel = subtitle;

            titleStack.Controls.Add(title, 0, 0);
            titleStack.Controls.Add(subtitle, 0, 1);
            panel.Controls.Add(titleStack, 0, 0);
            panel.Controls.Add(_stepLabel, 1, 0);
            return panel;
        }

        private void ShowStep()
        {
            _contentHost.Controls.Clear();
            _status.Text = "";
            _backButton.Visible = _currentStep > 1;

            if (_currentStep == 1)
            {
                _titleLabel.Text = _previewMode ? "School Profile Preview" : "School Profile Setup";
                _subtitleLabel.Text = "Identity, contact details, and branding for reports, SMS, and the parent portal.";
                _stepLabel.Text = "STEP 1 OF 3";
                _nextButton.Text = "Continue";
                if (_schoolStepView == null) _schoolStepView = SchoolSection();
                _contentHost.Controls.Add(_schoolStepView);
                return;
            }

            if (_currentStep == 2)
            {
                _titleLabel.Text = _previewMode ? "Administrator Preview" : "First Administrator";
                _subtitleLabel.Text = "Create the first account that will manage this school.";
                _stepLabel.Text = "STEP 2 OF 3";
                _nextButton.Text = "Review Setup";
                if (_adminStepView == null) _adminStepView = AdminSection();
                _contentHost.Controls.Add(_adminStepView);
                return;
            }

            _titleLabel.Text = _previewMode ? "Review Preview" : "Review Setup";
            _subtitleLabel.Text = "Confirm the setup details before saving anything.";
            _stepLabel.Text = "STEP 3 OF 3";
            _nextButton.Text = _previewMode ? "Finish Preview" : "Complete Setup";
            _reviewStepView = ReviewSection();
            _contentHost.Controls.Add(_reviewStepView);
        }

        private async Task MoveNextAsync()
        {
            if (_currentStep == 1)
            {
                if (!ValidateSchoolStep()) return;
                _currentStep = 2;
                ShowStep();
                return;
            }

            if (_currentStep == 2)
            {
                if (!ValidateAdminStep()) return;
                _currentStep = 3;
                ShowStep();
                return;
            }

            await CompleteSetupAsync();
        }

        private Control SchoolSection()
        {
            var card = Card("School Identity", "These details appear across official documents, receipts, SMS messages and the parent portal.");
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 18, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));

            var fieldStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            fieldStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            fieldStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));

            var fields = Grid(2, 4);
            _schoolName = TextInput();
            _address = TextInput();
            _poBox = TextInput();
            _gps = TextInput();
            _phone1 = TextInput();
            _phone2 = TextInput();
            _email = TextInput();
            _portalUrl = TextInput();
            _schoolIdLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Consolas", 10.5F)
            };

            AddField(fields, "School name", _schoolName, 0, 0);
            AddField(fields, "Primary phone", _phone1, 1, 0);
            AddField(fields, "Location / address", _address, 0, 1);
            AddField(fields, "School email", _email, 1, 1);
            AddField(fields, "Postal address", _poBox, 0, 2);
            AddField(fields, "Ghana GPS address", _gps, 1, 2);
            AddField(fields, "Parent portal URL", _portalUrl, 0, 3);
            AddField(fields, "Secondary phone", _phone2, 1, 3);
            AddField(fieldStack, "Permanent school identity", _schoolIdLabel, 0, 1);
            fieldStack.Controls.Add(fields, 0, 0);

            var logoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(30, 0, 0, 0)
            };
            logoPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            logoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logoPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            logoPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            logoPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "School logo",
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var logoContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.Transparent };
            logoContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, logoContainer.Width - 1, logoContainer.Height - 1);
                using (var path = GetRoundedPath(rect, 12))
                using (var brush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (var pen = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            _logo = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = FirstRunBrandLogo(onBlue: false)
            };
            logoContainer.Controls.Add(_logo);
            logoPanel.Controls.Add(logoContainer, 0, 1);

            var logoButton = Button("Upload Logo", Color.White, Color.FromArgb(71, 85, 105), true);
            logoButton.Dock = DockStyle.Fill;
            logoButton.Height = 46;
            logoButton.Margin = new Padding(0, 12, 0, 0);
            logoButton.Click += (s, e) => UploadLogo();
            logoPanel.Controls.Add(logoButton, 0, 2);

            logoPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "PNG, JPG or BMP",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 3);

            body.Controls.Add(fieldStack, 0, 0);
            body.Controls.Add(logoPanel, 1, 0);
            card.Controls.Add(body, 0, 2);
            return card;
        }

        private Control AdminSection()
        {
            var card = Card("First Administrator", "This account opens the dashboard and can create staff accounts later.");
            var grid = Grid(3, 1);
            _username = TextInput();
            _password = TextInput();
            _confirmPassword = TextInput();
            _username.Text = "admin";
            _password.UseSystemPasswordChar = true;
            _confirmPassword.UseSystemPasswordChar = true;
            AddField(grid, "Admin username", _username, 0, 0);
            AddField(grid, "Password", _password, 1, 0);
            AddField(grid, "Confirm password", _confirmPassword, 2, 0);
            card.Controls.Add(grid, 0, 2);
            return card;
        }

        private Control ReviewSection()
        {
            var card = Card("Review and Confirm", "Check these details carefully. Saving will create the school profile and first administrator account.");
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 18, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

            var summary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                BackColor = Color.Transparent
            };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 8; i++)
                summary.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));

            AddReviewRow(summary, 0, "School name", _schoolName.Text);
            AddReviewRow(summary, 1, "Location / address", _address.Text);
            AddReviewRow(summary, 2, "Primary phone", _phone1.Text);
            AddReviewRow(summary, 3, "School email", _email.Text);
            AddReviewRow(summary, 4, "Postal address", _poBox.Text);
            AddReviewRow(summary, 5, "Parent portal URL", _portalUrl.Text);
            AddReviewRow(summary, 6, "Permanent School ID", _schoolId.ToString("D"));
            AddReviewRow(summary, 7, "First administrator", _username.Text);

            var logoReview = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(28, 0, 0, 0)
            };
            logoReview.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            logoReview.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logoReview.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            logoReview.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Logo preview",
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var logoBox = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18), BackColor = Color.Transparent };
            logoBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, logoBox.Width - 1, logoBox.Height - 1);
                using (var path = GetRoundedPath(rect, 12))
                using (var brush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (var pen = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            logoBox.Controls.Add(new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = _logo?.Image ?? FirstRunBrandLogo(onBlue: false)
            });
            logoReview.Controls.Add(logoBox, 0, 1);
            logoReview.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Password is set but hidden.",
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.8F),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 2);

            body.Controls.Add(summary, 0, 0);
            body.Controls.Add(logoReview, 1, 0);
            card.Controls.Add(body, 0, 2);
            return card;
        }

        private static void AddReviewRow(TableLayoutPanel grid, int row, string label, string value)
        {
            var caption = new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var text = new Label
            {
                Dock = DockStyle.Fill,
                Text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim(),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            grid.Controls.Add(caption, 0, row);
            grid.Controls.Add(text, 1, row);
        }

        private TableLayoutPanel Card(string title, string subtitle)
        {
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(42, 34, 42, 34),
                Margin = new Padding(0, 0, 0, 20)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var path = GetRoundedPath(rect, 14))
                {
                    using (var shadow = new SolidBrush(Color.FromArgb(8, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadow, GetRoundedPath(new Rectangle(2, 4, card.Width - 1, card.Height - 1), 14));
                        e.Graphics.FillPath(shadow, GetRoundedPath(new Rectangle(1, 2, card.Width - 1, card.Height - 1), 14));
                    }

                    using (var brush = new SolidBrush(Color.White))
                        e.Graphics.FillPath(brush, path);

                    using (var pen = new Pen(Color.FromArgb(220, 226, 235)))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = subtitle,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.8F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            return card;
        }

        private static TableLayoutPanel Grid(int columns, int rows)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columns,
                RowCount = rows,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < columns; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            for (int i = 0; i < rows; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            return grid;
        }

        private static TextBox TextInput()
        {
            return new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.2F),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(0),
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 24
            };
        }

        private static void AddField(TableLayoutPanel grid, string label, Control input, int col, int row, int span = 1)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 28, 14),
                BackColor = Color.Transparent
            };

            var caption = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = label,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 8.8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var inputShell = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.Transparent
            };
            inputShell.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, inputShell.Width - 1, inputShell.Height - 1);
                var focused = input.Focused || inputShell.ContainsFocus;
                using (var path = GetRoundedPath(rect, 8))
                using (var brush = new SolidBrush(focused ? Color.White : Color.FromArgb(248, 250, 252)))
                using (var pen = new Pen(focused ? UiTheme.Gold : Color.FromArgb(203, 213, 225), focused ? 2 : 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0);
            input.BackColor = Color.FromArgb(248, 250, 252);
            input.Enter += (s, e) =>
            {
                input.BackColor = Color.White;
                inputShell.Invalidate();
            };
            input.Leave += (s, e) =>
            {
                input.BackColor = Color.FromArgb(248, 250, 252);
                inputShell.Invalidate();
            };
            inputShell.Controls.Add(input);

            panel.Controls.Add(inputShell);
            panel.Controls.Add(caption);

            grid.Controls.Add(panel, col, row);
            if (span > 1) grid.SetColumnSpan(panel, span);
        }

        private static Button Button(string text, Color back, Color fore, bool outlined = false)
        {
            var button = new Button
            {
                Text = text,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Margin = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = outlined ? 1 : 0;
            if (outlined) button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

            button.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);

                using (var bgBrush = new SolidBrush(button.Parent?.BackColor ?? Color.Transparent))
                    e.Graphics.FillRectangle(bgBrush, button.ClientRectangle);

                bool isHovered = button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
                Color drawBack = back;
                if (isHovered)
                    drawBack = outlined ? Color.FromArgb(248, 250, 252) : ControlPaint.Light(back, 0.08f);

                using (var path = GetRoundedPath(rect, 8))
                {
                    using (var brush = new SolidBrush(drawBack))
                        e.Graphics.FillPath(brush, path);
                    if (outlined)
                    {
                        using (var pen = new Pen(Color.FromArgb(203, 213, 225)))
                            e.Graphics.DrawPath(pen, path);
                    }
                }

                TextRenderer.DrawText(e.Graphics, button.Text, button.Font, rect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            button.MouseEnter += (s, e) => button.Invalidate();
            button.MouseLeave += (s, e) => button.Invalidate();
            button.MouseDown += (s, e) => button.Invalidate();
            button.MouseUp += (s, e) => button.Invalidate();

            return button;
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private async Task LoadDefaultsAsync()
        {
            try
            {
                var repo = new SchoolInfoRepository(AppConfig.ConnectionString);
                await repo.EnsureTablesAsync();

                if (_previewMode)
                {
                    LoadFreshFirstRunDefaults();
                    return;
                }

                var info = await repo.GetAsync();
                _schoolId = info.SchoolId == Guid.Empty ? Guid.NewGuid() : info.SchoolId;
                _schoolName.Text = info.Name == "NYANSAPO SCHOOL ERP" ? "" : info.Name;
                _address.Text = info.Address == "ACCRA - GHANA" ? "" : info.Address;
                _poBox.Text = info.PoBox == "P. O. BOX 123 ACCRA" ? "" : info.PoBox;
                _gps.Text = info.GpsAddress ?? "";
                _phone1.Text = info.Phone1 == "0548050141" ? "" : info.Phone1;
                _phone2.Text = info.Phone2 == "0246087609" ? "" : info.Phone2;
                _email.Text = info.Email == "noreply@nyansapoerp.edu.gh" ? "" : info.Email;
                _portalUrl.Text = info.PortalUrl ?? "";
                _logoBytes = null;
                if (_logo != null) _logo.Image = FirstRunBrandLogo(onBlue: false);
                _schoolIdLabel.Text = _schoolId.ToString("D");
            }
            catch (Exception ex)
            {
                _status.Text = "Setup defaults could not be loaded: " + ex.Message;
            }
        }

        private void LoadFreshFirstRunDefaults()
        {
            _schoolId = Guid.NewGuid();
            _schoolName.Text = "";
            _address.Text = "";
            _poBox.Text = "";
            _gps.Text = "";
            _phone1.Text = "";
            _phone2.Text = "";
            _email.Text = "";
            _portalUrl.Text = "";
            _logoBytes = null;
            if (_logo != null) _logo.Image = FirstRunBrandLogo(onBlue: false);
            _schoolIdLabel.Text = _schoolId.ToString("D");
            _status.Text = "Preview mode: no data will be saved.";
        }

        private void UploadLogo()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var bytes = File.ReadAllBytes(dialog.FileName);
                _logoBytes = bytes;
                using (var stream = new MemoryStream(bytes))
                    _logo.Image = Image.FromStream(stream);
            }
        }

        private async Task CompleteSetupAsync()
        {
            if (!ValidateForm()) return;
            _nextButton.Enabled = false;

            if (_previewMode)
            {
                UIHelper.ShowInfo("Preview complete. No school data or administrator account was changed.", "First-Time Setup Preview");
                Close();
                return;
            }

            _status.Text = "Saving school profile and administrator account...";

            try
            {
                var repo = new SchoolInfoRepository(AppConfig.ConnectionString);
                await repo.EnsureTablesAsync();
                await repo.SaveAsync(new SchoolInformation
                {
                    SchoolId = _schoolId,
                    Name = _schoolName.Text.Trim(),
                    Address = _address.Text.Trim(),
                    PoBox = _poBox.Text.Trim(),
                    GpsAddress = _gps.Text.Trim(),
                    Phone1 = _phone1.Text.Trim(),
                    Phone2 = _phone2.Text.Trim(),
                    Email = _email.Text.Trim(),
                    PortalUrl = _portalUrl.Text.Trim(),
                    Logo = _logoBytes,
                    AdmissionFee = 100m
                });
                SchoolProfile.Refresh();

                var result = await AuthService.RegisterAsync(
                    _username.Text.Trim(),
                    _password.Text,
                    _confirmPassword.Text,
                    "Administrator");

                if (!result.Success)
                {
                    _status.Text = result.Message;
                    UIHelper.ShowWarning(result.Message, "First-Time Setup");
                    _nextButton.Enabled = true;
                    return;
                }

                await repo.RecordFirstRunCompletedAsync(_username.Text.Trim());
                UIHelper.ShowInfo("Setup complete. Sign in with the administrator account you created.", "Welcome");
                Close();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("First-time setup failed", ex);
                _status.Text = "Setup failed.";
                UIHelper.ShowError("Setup failed: " + ex.Message, "First-Time Setup");
                _nextButton.Enabled = true;
            }
        }

        private bool ValidateForm()
        {
            return ValidateSchoolStep() && ValidateAdminStep();
        }

        private bool ValidateSchoolStep()
        {
            if (string.IsNullOrWhiteSpace(_schoolName.Text))
                return Warn(_schoolName, "Enter the school name.");
            if (string.IsNullOrWhiteSpace(_address.Text))
                return Warn(_address, "Enter the school location/address.");
            if (string.IsNullOrWhiteSpace(_phone1.Text))
                return Warn(_phone1, "Enter a primary phone number.");
            return true;
        }

        private bool ValidateAdminStep()
        {
            string validation = AuthService.ValidateRegistration(
                _username.Text.Trim(),
                _password.Text,
                _confirmPassword.Text,
                "Administrator");

            if (!string.IsNullOrEmpty(validation))
            {
                return Warn(AdminValidationTarget(validation), validation);
            }

            return true;
        }

        private Control AdminValidationTarget(string validation)
        {
            if (validation.IndexOf("username", StringComparison.OrdinalIgnoreCase) >= 0)
                return _username;
            if (validation.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                validation.IndexOf("match", StringComparison.OrdinalIgnoreCase) >= 0)
                return _confirmPassword;
            return _password;
        }

        private bool Warn(Control control, string message)
        {
            _status.Text = message;
            UIHelper.ShowWarning(message, "First-Time Setup");
            control.Focus();
            return false;
        }
    }
}
