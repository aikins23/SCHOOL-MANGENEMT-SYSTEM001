using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmDashboard
    {
        /// <summary>
        /// Draws a small red count bubble on the right edge of a nav button when there
        /// are pending admission approvals. Display-only; driven by _pendingApprovalsCount.
        /// </summary>
        private void AttachApprovalBadge(Button btn)
        {
            btn.Paint += (s, e) =>
            {
                if (_pendingApprovalsCount <= 0) return;
                string txt = _pendingApprovalsCount > 99 ? "99+" : _pendingApprovalsCount.ToString();
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var f = new Font("Segoe UI", 8F, FontStyle.Bold))
                {
                    int w = Math.Max(20, (int)e.Graphics.MeasureString(txt, f).Width + 10);
                    int h = 18;
                    var rect = new Rectangle(btn.Width - w - 12, (btn.Height - h) / 2, w, h);
                    using (var path = RoundedRectPath(rect, h / 2))
                    using (var br = new SolidBrush(AccentRed))
                        e.Graphics.FillPath(br, path);
                    using (var tb = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        e.Graphics.DrawString(txt, f, tb, rect, sf);
                }
            };
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRectPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Loads the pending-approval count (Accountant only), repaints the nav bubble, and
        /// shows a one-time sign-in alert when there are admissions awaiting approval.
        /// </summary>
        private async System.Threading.Tasks.Task RefreshApprovalNotificationAsync()
        {
            if (AuthService.CurrentUser.Role != AuthService.UserRole.Accountant || _approvalsNavBtn == null)
                return;

            int count;
            try
            {
                var svc = new Services.DraftAdmissionService(
                    new DraftAdmissionRepository(AppConfig.ConnectionString),
                    _studentService,
                    _feeRepository);
                count = await svc.GetPendingCountAsync();
            }
            catch
            {
                count = 0;
            }

            _pendingApprovalsCount = count;
            if (!_approvalsNavBtn.IsDisposed) _approvalsNavBtn.Invalidate();

            if (count > 0 && !_approvalAlertShown)
            {
                _approvalAlertShown = true;
                MessageBox.Show(
                    $"{count} admission{(count == 1 ? "" : "s")} awaiting your approval.",
                    "Admission Approvals", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyRolePermissions()
        {
            // Access is decided when the nav groups are built (BuildModernDashboard); inaccessible
            // items are simply not created. Here we just reflect who is signed in.
            var role = AuthService.CurrentUser.Role;
            statusLabel.Text = $"Signed in as {AuthService.CurrentUser.Username} ({role})";
        }

        private void FrmDashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            // When dashboard close button is clicked, close all child forms.
            // The dashboard is the app's main form — letting it finish closing
            // ends the message loop naturally. Calling Application.Exit() here
            // re-enters the thread-context teardown and throws NRE.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                FormManager.CloseAllForms();
            }
        }

        /// <summary>
        /// Shuts the application down robustly. Application.Exit() intermittently throws
        /// (IndexOutOfRangeException / NullReferenceException) from WinForms' thread-context
        /// teardown — a framework race where the GC finalizer thread mutates the static
        /// context table while ExitCommon copies it into a fixed-size array. When that
        /// happens, hard-exit the process so the user gets a clean shutdown instead of an
        /// unhandled-exception dialog. The graceful path is unchanged when no race occurs.
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                Application.Exit();
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Application.Exit teardown race; forcing process exit: " + ex.Message);
                Environment.Exit(0);
            }
        }

        private void BuildModernDashboard()
        {
            SuspendLayout();

            Controls.Clear();
            Text = Common.AppConfig.ProductName;
            var _brandIcon = Common.Branding.AppIconOnBlue; if (_brandIcon != null) Icon = _brandIcon;
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1220, 740);
            Size = new Size(1520, 900);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBackColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildSidebar(), 0, 0);
            root.Controls.Add(BuildContent(), 1, 0);
            Controls.Add(root);

            ResumeLayout(true);
        }

        private Control BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = SidebarBackColor,
                Padding   = Padding.Empty
            };

            // ── Brand block (Brand Name, Logo, and Contacts) ──────────────────
            var brand = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 172,
                BackColor = SidebarBackColor,
                Padding   = new Padding(18, 16, 18, 12)
            };

            var productLogo = Common.Branding.IconBgImage ?? Common.Branding.AppIconOnBlue?.ToBitmap();
            if (productLogo != null)
            {
                brand.Controls.Add(new PictureBox
                {
                    Size     = new Size(54, 54),
                    Location = new Point(18, 16),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Image    = productLogo
                });
            }
            else
            {
                var badge = new Panel { Size = new Size(48, 48), Location = new Point(20, 18), BackColor = Color.Transparent };
                badge.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var br = new SolidBrush(AccentGold))
                        g.FillEllipse(br, 0, 0, 47, 47);
                    using (var f  = new Font("Georgia", 16F, FontStyle.Bold))
                    using (var tb = new SolidBrush(Color.FromArgb(8, 14, 52)))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("K", f, tb, new RectangleF(0, 0, 48, 48), sf);
                    }
                };
                brand.Controls.Add(badge);
            }

            brand.Controls.Add(new Label
            {
                Text = "NYANSAPO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13.5F, FontStyle.Bold),
                Bounds = new Rectangle(82, 18, 140, 25),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });
            brand.Controls.Add(new Label
            {
                Text = "SCHOOL ERP",
                ForeColor = AccentGold,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Bounds = new Rectangle(82, 43, 140, 20),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });

            // Contact info separator
            var contactDivider = new Panel { Size = new Size(204, 1), Location = new Point(18, 78), BackColor = Color.FromArgb(36, 48, 88) };
            brand.Controls.Add(contactDivider);

            brand.Controls.Add(new Label
            {
                Text       = "darktechhub2@gmail.com",
                ForeColor  = Color.FromArgb(180, 195, 220),
                Font       = new Font("Segoe UI", 8.25F, FontStyle.Regular),
                Bounds     = new Rectangle(18, 88, 204, 20),
                AutoEllipsis = true,
                TextAlign  = ContentAlignment.MiddleLeft,
                BackColor  = Color.Transparent
            });
            brand.Controls.Add(new Label
            {
                Text       = "+233 54 836 9261 / +233 20 493 9571",
                ForeColor  = Color.FromArgb(140, 160, 190),
                Font       = new Font("Segoe UI", 7.85F, FontStyle.Regular),
                Bounds     = new Rectangle(18, 112, 204, 20),
                AutoEllipsis = true,
                TextAlign  = ContentAlignment.MiddleLeft,
                BackColor  = Color.Transparent
            });
            brand.Controls.Add(new Label
            {
                Text       = "Accra, Ghana",
                ForeColor  = Color.FromArgb(120, 145, 175),
                Font       = new Font("Segoe UI", 8F, FontStyle.Regular),
                Bounds     = new Rectangle(18, 136, 204, 20),
                AutoEllipsis = true,
                TextAlign  = ContentAlignment.MiddleLeft,
                BackColor  = Color.Transparent
            });

            // ── Gold brand divider ────────────────────────────────────────────
            var topDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(36, 48, 88) };

            // ── Nav section (scrollable) ──────────────────────────────────────
            var navScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = SidebarBackColor
            };

            var nav = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Padding       = new Padding(18, 14, 18, 0),
                BackColor     = SidebarBackColor
            };

            navScroll.SizeChanged += (s, e) =>
            {
                ResizeDashboardSidebarNav(navScroll, nav);
                UiTheme.HideNativeScrollbarsFor(navScroll);
            };

            nav.Controls.Add(CreateNavButton("Dashboard", null, true));

            bool known   = AuthService.CurrentUser.IsAuthenticated;

            void Add(System.Collections.Generic.List<Button> g, bool ok, string text, Action act)
            { if (ok) g.Add(CreateNavButton(text, act)); }
            void AddForm(System.Collections.Generic.List<Button> g, string formKey, string text, Action act)
            { Add(g, UiPermissionService.CanShowForm(formKey), text, act); }
            void AddAction(System.Collections.Generic.List<Button> g, string actionKey, string text, Action act)
            { Add(g, UiPermissionService.CanShowAction(actionKey), text, act); }
            void AddGroup(string title, System.Collections.Generic.List<Button> items)
            { if (items.Count > 0) nav.Controls.Add(CreateNavGroup(title, items)); }

            var students = new System.Collections.Generic.List<Button>();
            AddAction(students, "Students.Register", "Add Student", OpenAddStudentDialog);
            AddForm(students, "frmStdView", "View Students", () => OpenForm(new frmStdView()));
            AddGroup("Students", students);

            var staff = new System.Collections.Generic.List<Button>();
            AddAction(staff, "Staff.Register", "Add Employee", () => OpenForm(new frmEmployee()));
            AddForm(staff, "frmEmpView", "View Employees", () => OpenForm(new frmEmpView()));
            AddGroup("Staff", staff);

            var leave = new System.Collections.Generic.List<Button>();
            AddAction(leave, "Leave.Submit", "Apply for Leave", () => OpenForm(new frmEmpLeave()));
            AddForm(leave, "frmLeaveDetails", "Leave Requests", () => OpenForm(new frmLeaveDetails()));
            AddGroup("Leave", leave);

            var academics = new System.Collections.Generic.List<Button>();
            AddAction(academics, "Academics.ExamResults.Manage", "Exams", () => OpenForm(new EXAMS()));
            AddForm(academics, "EXAMSVIEW", "Exam Reports", () => OpenForm(new EXAMSVIEW()));
            AddForm(academics, "frmClassManager", "Class Manager", () => OpenForm(new frmClassManager()));
            AddForm(academics, "frmAcademicCalendar", "Academic Calendar", () => OpenForm(new frmAcademicCalendar()));
            AddForm(academics, "frmTimetable", "Timetable Generator", () => OpenForm(new frmTimetable()));
            AddForm(academics, "frmSubjects", "Subjects", () => new frmSubjects().ShowDialog());
            AddGroup("Academics", academics);

            var finance = new System.Collections.Generic.List<Button>();
            AddForm(finance, "frmFessPayment", "Fees Payment", () => OpenForm(new frmFessPayment()));
            AddForm(finance, "frmAdditionalFees", "Additional Fees", () => OpenForm(new frmAdditionalFees()));
            AddForm(finance, "frmPaymentHistory", "Payment History", () => OpenForm(new frmPaymentHistory()));
            AddForm(finance, "frmTransportPayments", "Transport Payments", () => OpenForm(new frmTransportPayments()));
            AddForm(finance, "frmScholarships", "Scholarships & Discounts", () => OpenForm(new frmScholarships()));
            AddForm(finance, "frmExpenses", "Expenses", () => OpenForm(new frmExpenses()));
            if (UiPermissionService.CanShowForm("frmPendingApprovals"))
            {
                _approvalsNavBtn = CreateNavButton("Admission Approvals", () => OpenForm(new frmPendingApprovals()));
                AttachApprovalBadge(_approvalsNavBtn);
                finance.Add(_approvalsNavBtn);
            }
            AddGroup("Finance", finance);

            var ops = new System.Collections.Generic.List<Button>();
            AddForm(ops, "frmLibrary", "Library", () => new frmLibrary().ShowDialog());
            AddForm(ops, "frmTransport", "Transport", () => new frmTransport().ShowDialog());
            AddGroup("Operations", ops);

            var comms = new System.Collections.Generic.List<Button>();
            AddForm(comms, "frmNotice", "Notice Board", () => OpenForm(new frmNotice()));
            AddAction(comms, "Admin.Notice.Send", "Send Notice", () => OpenForm(new frmSendNotice()));
            AddGroup("Communications", comms);

            var admin = new System.Collections.Generic.List<Button>();
            AddForm(admin, "frmEmailSettings", "Settings", () => new frmEmailSettings().ShowDialog());
            AddForm(admin, "frmClassManager", "Class Management", () => OpenForm(new frmClassManager()));
            AddForm(admin, "frmAdminDashboard", "Archive & Admin Hub", () => OpenForm(new frmAdminDashboard()));
            AddForm(admin, "frmSchoolInfo", "School Information", () => new frmSchoolInfo().ShowDialog());
            AddForm(admin, "frmGradingScheme", "Grading Scheme", () => new frmGradingScheme().ShowDialog());
            AddAction(admin, "Admin.Backup.Manage", "Database Backup", RunBackup);
            AddForm(admin, "frmDatabaseCoverageAudit", "Database Coverage", () => OpenForm(new frmDatabaseCoverageAudit()));
            AddAction(admin, "Admin.Users.Manage", "System Logs", ViewLogs);
            AddGroup("Administration", admin);

            if (known) nav.Controls.Add(CreateNavButton("Analytics", OpenAnalyticsDashboard));

            navScroll.Controls.Add(nav);
            ResizeDashboardSidebarNav(navScroll, nav);

            // ── User info footer ──────────────────────────────────────────────
            var exitBtn = CreateNavButton("Exit", ExitApplication);
            exitBtn.Dock      = DockStyle.Bottom;
            exitBtn.ForeColor = Color.FromArgb(239, 80, 80);
            exitBtn.Margin    = Padding.Empty;
            exitBtn.Padding   = new Padding(16, 0, 0, 0);

            // Sign Out: log out and return to the login screen.
            var signOutBtn = CreateNavButton("Sign Out", () => FormManager.SignOut());
            signOutBtn.Dock    = DockStyle.Bottom;
            signOutBtn.Margin  = Padding.Empty;
            signOutBtn.Padding = new Padding(16, 0, 0, 0);

            var bottomDivider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(36, 48, 88) };

            var userFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 66,
                BackColor = Color.FromArgb(8, 14, 52),
                Padding   = new Padding(16, 10, 16, 10)
            };

            // Blue circular avatar with username initial
            var avatar = new Panel { Size = new Size(38, 38), Location = new Point(16, 12), BackColor = Color.Transparent };
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(AccentBlue))
                    g.FillEllipse(br, 0, 0, 37, 37);
                string init = AuthService.CurrentUser?.Username?.Length > 0
                    ? AuthService.CurrentUser.Username[0].ToString().ToUpper() : "U";
                using (var f  = new Font("Segoe UI Semibold", 14F, FontStyle.Bold))
                using (var tb = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(init, f, tb, new RectangleF(0, 0, 38, 38), sf);
                }
            };

            userFooter.Controls.Add(avatar);
            userFooter.Controls.Add(new Label
            {
                Text      = AuthService.CurrentUser?.Username ?? "User",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Bounds    = new Rectangle(62, 12, 172, 18),
                TextAlign = ContentAlignment.MiddleLeft
            });
            userFooter.Controls.Add(new Label
            {
                Text      = AuthService.CurrentUser?.Role.ToString() ?? "",
                ForeColor = Color.FromArgb(120, 145, 175),
                Font      = new Font("Segoe UI", 8F),
                Bounds    = new Rectangle(62, 32, 172, 16),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Dock.Fill must be added FIRST so it is laid out LAST and takes only the space
            // left after the top brand and the bottom footer — otherwise navScroll overlaps the
            // footer and overflow nav buttons render behind it / off-screen (no scroll).
            sidebar.Controls.Add(navScroll);
            sidebar.Controls.Add(exitBtn);
            sidebar.Controls.Add(signOutBtn);
            sidebar.Controls.Add(bottomDivider);
            sidebar.Controls.Add(userFooter);
            sidebar.Controls.Add(topDivider);
            sidebar.Controls.Add(brand);

            UiTheme.HideNativeScrollbarsFor(navScroll);

            return sidebar;
        }

        private static void ResizeDashboardSidebarNav(Panel navScroll, FlowLayoutPanel nav)
        {
            if (navScroll == null || nav == null || nav.IsDisposed) return;

            int viewportWidth = Math.Max(220, navScroll.ClientSize.Width);
            int reservedScrollWidth = SystemInformation.VerticalScrollBarWidth + 6;
            int navWidth = Math.Max(200, viewportWidth - reservedScrollWidth);
            int buttonWidth = Math.Max(170, navWidth - nav.Padding.Left - nav.Padding.Right);

            nav.SuspendLayout();
            try
            {
                nav.Left = 0;
                nav.Width = navWidth;
                ResizeDashboardSidebarNavChildren(nav.Controls, buttonWidth);
                navScroll.AutoScrollMinSize = new Size(0, Math.Max(nav.Height, nav.PreferredSize.Height));
                navScroll.HorizontalScroll.Enabled = false;
                navScroll.HorizontalScroll.Visible = false;
                if (navScroll.HorizontalScroll.Value != 0)
                {
                    navScroll.HorizontalScroll.Value = 0;
                }
            }
            finally
            {
                nav.ResumeLayout(true);
            }
        }

        private static void ResizeDashboardSidebarNavChildren(Control.ControlCollection controls, int buttonWidth)
        {
            foreach (Control control in controls)
            {
                if (control is Button)
                {
                    control.Width = buttonWidth;
                }
                else if (control is FlowLayoutPanel panel)
                {
                    int panelWidth = Math.Max(170, buttonWidth + panel.Padding.Left + panel.Padding.Right);
                    panel.Width = panelWidth;
                    ResizeDashboardSidebarNavChildren(panel.Controls, Math.Max(150, buttonWidth - panel.Padding.Left));
                }
            }
        }

        private static string FormatSidebarSchoolName(string schoolName)
        {
            var text = string.IsNullOrWhiteSpace(schoolName)
                ? Common.AppConfig.ProductName
                : schoolName.Trim();

            text = text.ToUpperInvariant();
            if (text.Length <= 18)
            {
                return text;
            }

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1)
            {
                return text;
            }
            return string.Join(Environment.NewLine, words);
        }

        private Control BuildHeaderPanel()
        {
            var header = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = PageBackColor,
                Margin      = Padding.Empty,
                Padding     = new Padding(0, 0, 0, 10)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            string schoolDisplayName = string.IsNullOrWhiteSpace(Common.SchoolProfile.DisplayName)
                ? "KINGDOM PREPARATORY SCHOOL"
                : Common.SchoolProfile.DisplayName.ToUpperInvariant();

            string schoolContact = BuildSchoolHeaderContact();
            var titleBlock = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            titleBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            titleBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            titleBlock.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var schoolLogo = Common.Branding.GetLogo(false);
            var logoBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 14, 8),
                Image = schoolLogo
            };

            var schoolTextBlock = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var nameLabel = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 34,
                Text      = schoolDisplayName,
                ForeColor = PrimaryColor,
                Font      = new Font("Segoe UI Semibold", 19F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var subLabel = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 21,
                Text      = "Operations & Academic Management Dashboard",
                ForeColor = MutedTextColor,
                Font      = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var contactLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = schoolContact,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            schoolTextBlock.Controls.Add(contactLabel);
            schoolTextBlock.Controls.Add(subLabel);
            schoolTextBlock.Controls.Add(nameLabel);
            titleBlock.Controls.Add(logoBox, 0, 0);
            titleBlock.Controls.Add(schoolTextBlock, 1, 0);

            var statusPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            statusLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Initializing...",
                ForeColor = MutedTextColor,
                Font      = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            };
            statusPanel.Controls.Add(statusLabel);

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(statusPanel, 1, 0);

            return header;
        }

        private static string BuildSchoolHeaderContact()
        {
            string phones = (Common.SchoolProfile.Phones ?? "").Trim();
            string location = (Common.SchoolProfile.Address ?? "").Trim();
            string gps = (Common.SchoolProfile.GpsAddress ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(gps))
            {
                location = string.IsNullOrWhiteSpace(location) ? gps : location + " | " + gps;
            }

            if (!string.IsNullOrWhiteSpace(phones) && !string.IsNullOrWhiteSpace(location))
            {
                return "Tel: " + phones + "   Loc: " + location;
            }

            if (!string.IsNullOrWhiteSpace(phones))
            {
                return "Tel: " + phones;
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                return "Loc: " + location;
            }

            return "";
        }

        private Control BuildContent()
        {
            // Outer scroll host — kicks in when the window is smaller than
            // the dashboard's preferred working area.
            var scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                AutoScroll = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            var financeRole = AuthService.CurrentUser.Role;
            bool showFinanceMetrics = financeRole == AuthService.UserRole.Accountant ||
                                      financeRole == AuthService.UserRole.Administrator ||
                                      financeRole == AuthService.UserRole.Director;
            int metricRowHeight = showFinanceMetrics ? 360 : 200;
            int quickActionsHeight = GetQuickActionsHeight();
            int contentHeight = 1050 + (metricRowHeight - 200) + (quickActionsHeight - 170);

            var content = new TableLayoutPanel
            {
                BackColor   = PageBackColor,
                Padding     = new Padding(26, 24, 26, 22),
                ColumnCount = 1,
                RowCount    = 4,
                AutoScroll  = false,
                Size        = new Size(1300, contentHeight)
            };

            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));  // Header with school identity
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, metricRowHeight)); // Metrics
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Main grids
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, quickActionsHeight)); // Quick actions

            scrollHost.SizeChanged += (s, e) =>
            {
                content.Width  = Math.Max(scrollHost.ClientSize.Width, 1000);
                content.Height = Math.Max(scrollHost.ClientSize.Height, contentHeight);
            };

            // ── Top header bar (School Name and Identity) ─────────────────────
            var header = BuildHeaderPanel();

            var metricGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                BackColor = PageBackColor,
                Padding = new Padding(0, 6, 0, 12),
                RowCount = 1
            };
            for (int i = 0; i < 4; i++)
            {
                metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }
            metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            studentCountLabel = new Label();
            employeeCountLabel = new Label();
            feesCollectedLabel = new Label();
            feesBalanceLabel = new Label();

            metricGrid.Controls.Add(CreateMetricCard("Students",        studentCountLabel,  "Active student records",  AccentBlue,  "STUDENTS", DashboardIconType.Students),  0, 0);
            metricGrid.Controls.Add(CreateMetricCard("Employees",       employeeCountLabel, "Current staff records",   AccentGreen, "EMPLOYEES", DashboardIconType.Employees), 1, 0);
            metricGrid.Controls.Add(CreateMetricCard("Fees Collected",  feesCollectedLabel, "Total recorded payments", AccentGold,  "REVENUE", DashboardIconType.Fees),   2, 0);
            metricGrid.Controls.Add(CreateMetricCard("Outstanding Fees",feesBalanceLabel,   "Positive fee balances",   AccentRed,   "BALANCE", DashboardIconType.Balance),   3, 0);

            // Currency strings are long ("GHS 15,746.00") — shrink the font and enable
            // ellipsis so they fit the card width without being clipped to "GH".
            feesCollectedLabel.Font = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold);
            feesCollectedLabel.AutoEllipsis = true;
            feesBalanceLabel.Font = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold);
            feesBalanceLabel.AutoEllipsis = true;

            // Finance overview tiles (Income, Expenses, Fund, Top Expense) — only for finance/leadership.
            if (showFinanceMetrics)
            {
                metricGrid.RowCount = 2;
                metricGrid.RowStyles.Clear();
                metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

                _incomeLabel = new Label(); _expensesLabel = new Label(); _fundLabel = new Label();
                _topExpenseLabel = new Label();

                metricGrid.Controls.Add(CreateMetricCard("Total Income",   _incomeLabel,   "This term", AccentGreen, "INCOME",   DashboardIconType.Fees),    0, 1);
                metricGrid.Controls.Add(CreateMetricCard("Total Expenses", _expensesLabel, "This term", AccentRed,   "EXPENSES", DashboardIconType.Balance), 1, 1);
                metricGrid.Controls.Add(CreateMetricCard("Total Fund",     _fundLabel,     "This term", AccentGold,  "FUND",     DashboardIconType.Fees),    2, 1);
                metricGrid.Controls.Add(CreateMetricCard("Top Expense",    _topExpenseLabel, "Highest category", AccentRed, "HIGHEST",  DashboardIconType.Balance), 3, 1);

                foreach (var l in new[] { _incomeLabel, _expensesLabel, _fundLabel, _topExpenseLabel })
                { l.Font = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold); l.AutoEllipsis = true; }
            }

            var analyticsGrid = BuildAnalyticsGrid();
            var actionPanel = BuildQuickActionsPanel();

            content.Controls.Add(header, 0, 0);
            content.Controls.Add(metricGrid, 0, 1);
            content.Controls.Add(analyticsGrid, 0, 2);
            content.Controls.Add(actionPanel, 0, 3);

            scrollHost.Controls.Add(content);
            return scrollHost;
        }

        // ── Dashboard Icons ──────────────────────────────────────────────────
        private enum DashboardIconType { Students, Employees, Fees, Balance, Score, Class, Leave }

        private Panel CreateMetricIcon(DashboardIconType type, Color accent)
        {
            var p = new Panel { Size = new Size(48, 48), BackColor = Color.Transparent };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float w = p.Width;
                float h = p.Height;
                float pad = 4;

                // Use uniform scaling to prevent distortion (keep aspect ratio)
                float designSize = 48f;
                float actualSize = Math.Min(w, h) - pad;
                float scale = actualSize / designSize;

                // Center the icon within the panel
                float xOffset = (w - actualSize) / 2;
                float yOffset = (h - actualSize) / 2;
                g.TranslateTransform(xOffset, yOffset);

                // Subtle soft background circle
                using (var bg = new SolidBrush(Color.FromArgb(40, accent)))
                {
                    g.FillEllipse(bg, 0, 0, actualSize, actualSize);
                }

                using (var pen = new Pen(accent, 2F))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap   = System.Drawing.Drawing2D.LineCap.Round;
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                    if (type == DashboardIconType.Students)
                    {
                        g.DrawEllipse(pen, 18 * scale, 10 * scale, 12 * scale, 12 * scale);
                        g.DrawArc(pen, 10 * scale, 24 * scale, 28 * scale, 14 * scale, 180, 180);
                        g.DrawArc(pen, 6 * scale, 20 * scale, 12 * scale, 10 * scale, 180, 180);
                        g.DrawArc(pen, 30 * scale, 20 * scale, 12 * scale, 10 * scale, 180, 180);
                    }
                    else if (type == DashboardIconType.Employees)
                    {
                        g.DrawEllipse(pen, 18 * scale, 14 * scale, 12 * scale, 12 * scale);
                        g.DrawArc(pen, 10 * scale, 28 * scale, 28 * scale, 12 * scale, 180, 180);
                        g.DrawLine(pen, 24 * scale, 30 * scale, 21 * scale, 38 * scale);
                        g.DrawLine(pen, 24 * scale, 30 * scale, 27 * scale, 38 * scale);
                        g.DrawLine(pen, 21 * scale, 38 * scale, 27 * scale, 38 * scale);
                    }
                    else if (type == DashboardIconType.Fees)
                    {
                        g.DrawRectangle(pen, 8 * scale, 14 * scale, 32 * scale, 20 * scale);
                        g.DrawEllipse(pen, 20 * scale, 20 * scale, 8 * scale, 8 * scale);
                        g.DrawLine(pen, 12 * scale, 18 * scale, 16 * scale, 18 * scale);
                        g.DrawLine(pen, 32 * scale, 18 * scale, 36 * scale, 18 * scale);
                    }
                    else if (type == DashboardIconType.Balance)
                    {
                        g.DrawArc(pen, 10 * scale, 14 * scale, 28 * scale, 20 * scale, 0, 360);
                        g.DrawLine(pen, 10 * scale, 24 * scale, 38 * scale, 24 * scale);
                        using (var br = new SolidBrush(accent))
                            g.FillEllipse(br, 22 * scale, 18 * scale, 4 * scale, 4 * scale);
                    }
                    else if (type == DashboardIconType.Score)
                    {
                        PointF[] shield = {
                            new PointF(24 * scale, 10 * scale), new PointF(38 * scale, 16 * scale),
                            new PointF(34 * scale, 34 * scale), new PointF(24 * scale, 40 * scale),
                            new PointF(14 * scale, 34 * scale), new PointF(10 * scale, 16 * scale)
                        };
                        g.DrawPolygon(pen, shield);
                        g.DrawLine(pen, 24 * scale, 18 * scale, 24 * scale, 32 * scale);
                    }
                    else if (type == DashboardIconType.Class)
                    {
                        g.DrawRectangle(pen, 10 * scale, 12 * scale, 28 * scale, 24 * scale);
                        g.DrawLine(pen, 14 * scale, 12 * scale, 14 * scale, 36 * scale);
                        g.DrawLine(pen, 18 * scale, 20 * scale, 32 * scale, 20 * scale);
                        g.DrawLine(pen, 18 * scale, 28 * scale, 28 * scale, 28 * scale);
                    }
                    else if (type == DashboardIconType.Leave)
                    {
                        g.DrawRectangle(pen, 10 * scale, 14 * scale, 28 * scale, 24 * scale);
                        g.DrawLine(pen, 10 * scale, 20 * scale, 38 * scale, 20 * scale);
                        g.DrawLine(pen, 16 * scale, 10 * scale, 16 * scale, 16 * scale);
                        g.DrawLine(pen, 32 * scale, 10 * scale, 32 * scale, 16 * scale);
                    }
                }
                g.ResetTransform();
            };
            return p;
        }

        private Control BuildAnalyticsGrid()
        {
            bool accountantDashboard = AuthService.CurrentUser.Role == AuthService.UserRole.Accountant;

            var analyticsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(0, 0, 0, 12)
            };
            analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

            recentPaymentsGrid = CreateAnalyticsGrid();
            Common.StudentId.AttachGridFormatting(recentPaymentsGrid, "ID");
            classSummaryGrid = CreateAnalyticsGrid();
            leaveSummaryGrid = CreateAnalyticsGrid();
            recentPaymentsGrid.ScrollBars = ScrollBars.Vertical;
            classSummaryGrid.ScrollBars = ScrollBars.Vertical;
            leaveSummaryGrid.ScrollBars = ScrollBars.Vertical;

            // Dashboard grids stretch columns to avoid heavy horizontal native scrollbars.
            recentPaymentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            classSummaryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            leaveSummaryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var paymentsPanel = CreateSectionPanel("Recent Payments");
            paymentsPanel.Margin = new Padding(0, 0, 14, 0);
            paymentsPanel.Controls.Add(recentPaymentsGrid);

            var rightStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = PageBackColor
            };
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, accountantDashboard ? 40 : 46));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, accountantDashboard ? 60 : 54));

            var classPanel = CreateSectionPanel(accountantDashboard ? "Class Fee Position" : "Class Enrollment");
            classPanel.Margin = new Padding(0, 0, 0, 12);
            classPanel.Controls.Add(classSummaryGrid);

            var insightPanel = CreateSectionPanel(accountantDashboard ? "Finance Insight" : "Academic And Leave Insight");
            averageExamLabel = new Label();
            topClassLabel = new Label();
            pendingLeaveLabel = new Label();

            var insightBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };
            insightBody.RowStyles.Add(new RowStyle(SizeType.Absolute, accountantDashboard ? 98 : 84));
            insightBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var academicSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };
            academicSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            academicSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            academicSummary.Controls.Add(CreateInsightTile(accountantDashboard ? "Collection Rate" : "Average Score", averageExamLabel, accountantDashboard ? DashboardIconType.Fees : DashboardIconType.Score), 0, 0);
            academicSummary.Controls.Add(CreateInsightTile(accountantDashboard ? "Highest Debt" : "Top Class", topClassLabel, accountantDashboard ? DashboardIconType.Balance : DashboardIconType.Class), 1, 0);

            var leaveSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            leaveSummary.RowStyles.Add(new RowStyle(SizeType.Absolute, accountantDashboard ? 72 : 66));
            leaveSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leaveSummary.Controls.Add(CreateInsightTile(accountantDashboard ? "Unpaid Fees" : "Pending Leave", pendingLeaveLabel, accountantDashboard ? DashboardIconType.Balance : DashboardIconType.Leave), 0, 0);
            if (accountantDashboard)
            {
                financeInsightList = CreateFinanceInsightList();
                leaveSummary.Controls.Add(financeInsightList, 0, 1);
            }
            else
            {
                leaveSummary.Controls.Add(leaveSummaryGrid, 0, 1);
            }

            insightBody.Controls.Add(academicSummary, 0, 0);
            insightBody.Controls.Add(leaveSummary, 0, 1);
            insightPanel.Controls.Add(insightBody);

            rightStack.Controls.Add(classPanel, 0, 0);
            rightStack.Controls.Add(insightPanel, 0, 1);
            analyticsGrid.Controls.Add(paymentsPanel, 0, 0);
            analyticsGrid.Controls.Add(rightStack, 1, 0);

            return analyticsGrid;
        }

        private Control BuildQuickActionsPanel()
        {
            var actionPanel = CreateSectionPanel("Quick Actions");
            actionPanel.Margin = new Padding(0);

            var actionHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true
            };

            var actions = new TableLayoutPanel
            {
                BackColor = Color.White,
                Dock = DockStyle.Top,
                Padding = new Padding(22, 18, 22, 18),
                Margin = Padding.Empty,
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var actionTiles = new System.Collections.Generic.List<Control>();

            void ArrangeActionTiles()
            {
                int width = actionHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                if (width <= 0) return;

                int innerWidth = Math.Max(260, width - actions.Padding.Horizontal);
                int columns = innerWidth >= 1040 ? 3 : innerWidth >= 680 ? 2 : 1;
                int rows = Math.Max(1, (int)Math.Ceiling(actionTiles.Count / (double)columns));

                actions.SuspendLayout();
                try
                {
                    actions.Controls.Clear();
                    actions.ColumnStyles.Clear();
                    actions.RowStyles.Clear();
                    actions.ColumnCount = columns;
                    actions.RowCount = rows;
                    actions.Width = Math.Max(320, width);

                    for (int c = 0; c < columns; c++)
                    {
                        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                    }

                    for (int r = 0; r < rows; r++)
                    {
                        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                    }

                    for (int i = 0; i < actionTiles.Count; i++)
                    {
                        var tile = actionTiles[i];
                        tile.Dock = DockStyle.Fill;
                        tile.Margin = new Padding(10, 8, 10, 8);
                        actions.Controls.Add(tile, i % columns, i / columns);
                    }
                }
                finally
                {
                    actions.ResumeLayout(true);
                }
            }

            void AddTile(string text, Action action)
            {
                var tile = CreateActionButton(text, action);
                tile.MinimumSize = new Size(0, 84);
                actionTiles.Add(tile);
            }

            void AddAction(string permission, string text, Action action)
            {
                if (UiPermissionService.CanShowAction(permission))
                    AddTile(text, action);
            }

            void AddForm(string formKey, string text, Action action)
            {
                if (UiPermissionService.CanShowForm(formKey))
                    AddTile(text, action);
            }

            if (AuthService.CurrentUser.Role == AuthService.UserRole.Accountant)
            {
                AddAction("Finance.FeePayment.Record", "Record Fees", () => OpenForm(new frmFessPayment()));
                AddForm("frmAdditionalFees", "Additional Fees", () => OpenForm(new frmAdditionalFees()));
                AddForm("frmOutstandingFees", "Outstanding Fees", () => OpenForm(new frmOutstandingFees()));
                AddForm("frmPaymentHistory", "Payment History", () => OpenForm(new frmPaymentHistory()));
                AddForm("frmExpenses", "Record Expenses", () => OpenForm(new frmExpenses()));
                AddForm("frmPendingApprovals", "Admission Approvals", () => OpenForm(new frmPendingApprovals()));
                AddForm("frmDashboardCharts", "Finance Analytics", OpenAnalyticsDashboard);
            }
            else
            {
                AddAction("Students.Register", "Register Student", OpenAddStudentDialog);
                AddAction("Academics.Attendance.Record", "Record Attendance", () => OpenForm(new frmAttendance()));
                AddAction("Finance.FeePayment.Record", "Record Fees", () => OpenForm(new frmFessPayment()));

                AddAction("Academics.ExamResults.Manage", "Submit Exam Scores", () => OpenForm(new EXAMS()));
                AddForm("EXAMSVIEW", "Generate Report Cards", () => OpenForm(new EXAMSVIEW()));
                AddForm("frmClassManager", "Class Manager", () => OpenForm(new frmClassManager()));
                AddForm("frmDashboardCharts", "Analytics Dashboard", OpenAnalyticsDashboard);
                AddForm("frmSyncStatus", "Sync Status", () => OpenForm(new frmSyncStatus()));
                AddForm("frmAcademicSessionManager", "Academic Sessions", () => OpenForm(new frmAcademicSessionManager()));
            }

            if (actionTiles.Count == 0)
            {
                actions.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Height = 64,
                    Text = "No quick actions are available for this role.",
                    ForeColor = MutedTextColor,
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }

            actionHost.Controls.Add(actions);
            actionHost.Resize += (sender, args) => ArrangeActionTiles();
            actions.HandleCreated += (sender, args) => ArrangeActionTiles();
            ArrangeActionTiles();
            UiTheme.AttachModernScrollbar(actionHost);

            actionPanel.Controls.Add(actionHost);

            return actionPanel;
        }

        private int GetQuickActionsHeight()
        {
            int count = GetVisibleQuickActionCount();
            int rows = Math.Max(1, (int)Math.Ceiling(count / 3.0));
            return Math.Max(240, 92 + (rows * 112));
        }

        private int GetVisibleQuickActionCount()
        {
            int count = 0;

            void CountAction(string permission)
            {
                if (UiPermissionService.CanShowAction(permission)) count++;
            }

            void CountForm(string formKey)
            {
                if (UiPermissionService.CanShowForm(formKey)) count++;
            }

            if (AuthService.CurrentUser.Role == AuthService.UserRole.Accountant)
            {
                CountAction("Finance.FeePayment.Record");
                CountForm("frmAdditionalFees");
                CountForm("frmOutstandingFees");
                CountForm("frmPaymentHistory");
                CountForm("frmExpenses");
                CountForm("frmPendingApprovals");
                CountForm("frmDashboardCharts");
            }
            else
            {
                CountAction("Students.Register");
                CountAction("Academics.Attendance.Record");
                CountAction("Finance.FeePayment.Record");
                CountAction("Academics.ExamResults.Manage");
                CountForm("EXAMSVIEW");
                CountForm("frmClassManager");
                CountForm("frmDashboardCharts");
                CountForm("frmSyncStatus");
                CountForm("frmAcademicSessionManager");
            }

            return count;
        }

        private Button CreateNavButton(string text, Action action, bool selected = false)
        {
            var button = new Button
            {
                Width  = 246,
                Height = 42,
                Margin = new Padding(0, 0, 0, 5),
                Text   = text,
                TextAlign  = ContentAlignment.MiddleLeft,
                FlatStyle  = FlatStyle.Flat,
                BackColor  = selected ? Color.FromArgb(22, 34, 78) : SidebarBackColor,
                ForeColor  = selected ? Color.White : Color.FromArgb(174, 190, 215),
                Font    = new Font("Segoe UI", 9.5F, selected ? FontStyle.Bold : FontStyle.Regular),
                Padding = new Padding(selected ? 20 : 16, 0, 0, 0),
                Cursor  = Cursors.Hand
            };
            button.FlatAppearance.BorderSize          = 0;
            button.FlatAppearance.MouseOverBackColor  = Color.FromArgb(31, 43, 100);
            button.FlatAppearance.MouseDownBackColor  = Color.FromArgb(10, 18, 56);

            // Gold left-bar accent on selected item
            if (selected)
            {
                button.Paint += (s, e) =>
                {
                    using (var br = new SolidBrush(AccentGold))
                        e.Graphics.FillRectangle(br, 0, 9, 4, button.Height - 18);
                };
            }

            if (action != null)
                button.Click += (sender, args) => action();

            return button;
        }

        /// <summary>
        /// A collapsible sidebar group: a header row with a chevron that toggles the
        /// visibility of its child nav buttons. Children are pre-filtered by role by the caller.
        /// </summary>
        private Control CreateNavGroup(string header, System.Collections.Generic.List<Button> children)
        {
            var container = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty, BackColor = SidebarBackColor
            };

            var headerBtn = CreateNavButton("▸  " + header, null);   // collapsed chevron
            headerBtn.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            var childPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 4), Padding = new Padding(8, 0, 0, 0),
                BackColor = SidebarBackColor, Visible = false
            };
            foreach (var c in children) childPanel.Controls.Add(c);

            headerBtn.Click += (s, e) =>
            {
                childPanel.Visible = !childPanel.Visible;
                headerBtn.Text = (childPanel.Visible ? "▾  " : "▸  ") + header;
            };

            container.Controls.Add(headerBtn);
            container.Controls.Add(childPanel);
            return container;
        }

        private Control CreateMetricCard(string title, Label valueLabel, string caption,
                                         Color accent, string tag, DashboardIconType iconType)
        {
            var card = CreateModernPanel(8);
            card.Dock    = DockStyle.Fill;
            card.Margin  = new Padding(0, 0, 16, 14);
            card.Padding = new Padding(20, 16, 20, 14);
            card.MinimumSize = new Size(0, 150);

            // Coloured top accent strip (4 px, painted on the panel itself)
            card.Paint += (s, e) =>
            {
                using (var br = new SolidBrush(accent))
                    e.Graphics.FillRectangle(br, 0, 0, card.Width, 3);
            };

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Tag chip (e.g. "STUDENTS") in accent colour
            var tagLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = tag,
                ForeColor = accent,
                Font      = new Font("Segoe UI", 7.75F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var titleLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = title,
                ForeColor = MutedTextColor,
                Font      = new Font("Segoe UI", 9.25F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Margin = Padding.Empty;
            valueLabel.Padding = new Padding(0, 0, 8, 0);
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 18.5F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.AutoEllipsis = true;

            var captionLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = caption,
                ForeColor = MutedTextColor,
                Font      = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var icon = CreateMetricIcon(iconType, accent);
            icon.Dock = DockStyle.Fill;
            icon.Margin = new Padding(0, 2, 0, 0);

            body.Controls.Add(tagLabel, 0, 0);
            body.Controls.Add(titleLabel, 0, 1);
            body.Controls.Add(valueLabel, 0, 2);
            body.Controls.Add(captionLabel, 0, 3);
            body.Controls.Add(icon, 1, 1);
            body.SetRowSpan(icon, 2);

            card.Controls.Add(body);
            return card;
        }

        private Panel CreateCompactInsight(string title, Label valueLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 6)
            };

            valueLabel.Dock = DockStyle.Bottom;
            valueLabel.Height = 32;
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;

            panel.Controls.Add(valueLabel);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = title,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            });

            return panel;
        }

        private Control CreateInsightTile(string title, Label valueLabel, DashboardIconType iconType)
        {
            var tile = CreateModernPanel(7, UiTheme.SurfaceAlt, UiTheme.SurfaceAlt);
            tile.Dock = DockStyle.Fill;
            tile.Margin = new Padding(0, 0, 8, 0);
            tile.Padding = new Padding(12, 6, 12, 6);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 13.25F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.AutoEllipsis = true;

            var icon = CreateMetricIcon(iconType, MutedTextColor);
            icon.Size = new Size(32, 32);
            icon.Dock = DockStyle.Fill;

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(valueLabel, 0, 1);
            layout.Controls.Add(icon, 1, 0);
            layout.SetRowSpan(icon, 2);

            tile.Controls.Add(layout);
            return tile;
        }

        private Control CreateSectionPanel(string title)
        {
            var section = CreateModernPanel(8);
            section.Dock    = DockStyle.Fill;
            section.Padding = new Padding(0, 45, 0, 0);

            // Navy header bar with white title text
            var titleLabel = new Label
            {
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height    = 42,
                Width     = section.ClientSize.Width,
                Location  = new Point(0, 0),
                Padding   = new Padding(18, 0, 0, 0),
                Text      = title,
                BackColor = SidebarBackColor,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Gold accent line below the header
            var accent = new Panel
            {
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height    = 3,
                Width     = section.ClientSize.Width,
                Location  = new Point(0, 42),
                BackColor = AccentGold
            };

            // titleLabel and accent both have Anchor = Left|Right so WinForms
            // automatically stretches them when section resizes — no extra handlers needed.
            // (A Layout handler that sets child widths causes infinite recursion because
            // changing a child's size fires Layout on the parent again.)

            section.Controls.Add(accent);
            section.Controls.Add(titleLabel);
            titleLabel.BringToFront();
            accent.BringToFront();
            return section;
        }

        private Guna.UI2.WinForms.Guna2Panel CreateModernPanel(int radius, Color? fill = null, Color? border = null)
        {
            var panel = new Guna.UI2.WinForms.Guna2Panel
            {
                BackColor = Color.Transparent,
                FillColor = fill ?? Color.White,
                BorderColor = border ?? UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = radius
            };
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 3;
            panel.ShadowDecoration.Color = Color.FromArgb(22, 25, 25, 112);
            return panel;
        }

        private DataGridView CreateAnalyticsGrid()
        {
            var grid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 38,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowTemplate = { Height = 34 },
                GridColor = BorderColor,
                ScrollBars = ScrollBars.Vertical
            };
            UiTheme.StyleDataGrid(grid, true);
            grid.ThemeStyle.AlternatingRowsStyle.BackColor = UiTheme.SurfaceAlt;
            grid.ThemeStyle.BackColor = Color.White;
            grid.ThemeStyle.GridColor = BorderColor;
            grid.ThemeStyle.HeaderStyle.BackColor = PrimaryColor;
            grid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            grid.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ThemeStyle.HeaderStyle.Height = 38;
            grid.ThemeStyle.RowsStyle.BackColor = Color.White;
            grid.ThemeStyle.RowsStyle.ForeColor = TextColor;
            grid.ThemeStyle.RowsStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.ThemeStyle.RowsStyle.SelectionForeColor = TextColor;
            grid.ThemeStyle.RowsStyle.Height = 31;

            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PrimaryColor;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.DefaultCellStyle.Padding = new Padding(8, 2, 8, 2);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;

            return grid;
        }

        private ListView CreateFinanceInsightList()
        {
            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Regular),
                HideSelection = true,
                MultiSelect = false
            };

            list.Columns.Add("Payment Mode", 160);
            list.Columns.Add("Total", 120, HorizontalAlignment.Right);

            list.Resize += (sender, args) =>
            {
                int width = Math.Max(220, list.ClientSize.Width);
                list.Columns[0].Width = Math.Max(120, width - 130);
                list.Columns[1].Width = 120;
            };

            return list;
        }

        // Cycle accent colours across quick-action buttons
        private static readonly Color[] _actionAccents =
        {
            Color.FromArgb(59,  130, 246),   // blue  – Register Student
            Color.FromArgb(16,  185, 129),   // green – Record Attendance
            Color.FromArgb(212, 175,  55),   // gold  – Record Fees
            Color.FromArgb(139,  92, 246),   // violet– Submit Exam Scores
            Color.FromArgb(239, 100,  68),   // orange– Generate Report Cards
            Color.FromArgb(20,  184, 166),   // teal  – Analytics Dashboard
        };
        private int _actionAccentIdx = 0;

        private Control CreateActionButton(string text, Action action)
        {
            Color accent = _actionAccents[_actionAccentIdx % _actionAccents.Length];
            _actionAccentIdx++;

            var tile = new Guna.UI2.WinForms.Guna2Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 6, 8, 6),
                FillColor = accent,
                BorderColor = ControlPaint.Light(accent, 0.25F),
                BorderRadius = 7,
                BorderThickness = 1,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            tile.ShadowDecoration.Enabled = true;
            tile.ShadowDecoration.Depth = 2;
            tile.ShadowDecoration.Color = Color.FromArgb(18, 25, 25, 112);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(14, 10, 14, 10),
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var iconHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(6) };
            var icon = CreateMetricIcon(ActionIconFor(text), Color.White);
            icon.Dock = DockStyle.Fill;
            iconHost.Controls.Add(icon);

            var separator = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(135, Color.White),
                Margin = new Padding(0, 12, 0, 12)
            };

            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Padding = new Padding(16, 0, 0, 0),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            layout.Controls.Add(iconHost, 0, 0);
            layout.Controls.Add(separator, 1, 0);
            layout.Controls.Add(label, 2, 0);
            tile.Controls.Add(layout);

            void WireClick(Control control)
            {
                control.Cursor = Cursors.Hand;
                control.Click += (sender, args) => action();
                foreach (Control child in control.Controls)
                {
                    WireClick(child);
                }
            }

            void SetFill(Control control, Color color)
            {
                if (control is Guna.UI2.WinForms.Guna2Panel panel)
                {
                    panel.FillColor = color;
                }
            }

            void WireHover(Control control)
            {
                control.MouseEnter += (sender, args) => SetFill(tile, ControlPaint.Light(accent, 0.08F));
                control.MouseLeave += (sender, args) => SetFill(tile, accent);
                foreach (Control child in control.Controls)
                {
                    WireHover(child);
                }
            }

            WireClick(tile);
            WireHover(tile);
            return tile;
        }

        private static DashboardIconType ActionIconFor(string text)
        {
            if (text.IndexOf("Student", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Students;
            if (text.IndexOf("Attendance", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Leave;
            if (text.IndexOf("Fee", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Fees;
            if (text.IndexOf("Exam", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Score;
            if (text.IndexOf("Report", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Score;
            if (text.IndexOf("Analytics", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Class;
            if (text.IndexOf("Class", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Class;
            if (text.IndexOf("Sync", StringComparison.OrdinalIgnoreCase) >= 0) return DashboardIconType.Balance;
            return DashboardIconType.Class;
        }

    }
}
