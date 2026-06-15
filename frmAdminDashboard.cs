using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAdminDashboard : Form
    {
        private readonly IUserRepository _userRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly SmsOutboxRepository _smsRepo;

        private TabControl _tabs;
        private Guna2DataGridView _userGrid, _studentArchiveGrid, _graduateGrid, _staffArchiveGrid, _smsLogGrid;
        private Label _statusLabel;
        private Guna2Button _restoreButton;
        private Guna2Button _deleteButton;

        public frmAdminDashboard()
        {
            InitializeComponent();
            this.Icon = Branding.AppIcon;
            
            _userRepo = new Data.UserRepository(AppConfig.ConnectionString);
            _studentRepo = new StudentRepository(AppConfig.ConnectionString);
            _employeeRepo = new EmployeeRepository(AppConfig.ConnectionString);
            _smsRepo = new SmsOutboxRepository(AppConfig.ConnectionString);

            BuildUi();
            Load += async (s, e) => await RefreshAllAsync();
        }

        private void BuildUi()
        {
            Text = "System Administration & Archives";
            Size = new Size(1200, 800);
            MinimumSize = new Size(1040, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.5F);
            Padding = new Padding(18, 18, 18, 30);

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            Controls.Add(shell);

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page, Padding = new Padding(0, 0, 0, 14) };
            titlePanel.Controls.Add(new Label
            {
                Text = "Administration Hub",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                Location = new Point(0, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            titlePanel.Controls.Add(new Label
            {
                Text = "Manage accounts, archive records, graduates, and communication logs from one controlled workspace.",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(2, 48),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            shell.Controls.Add(titlePanel, 0, 0);

            var workspace = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.White,
                BorderColor = UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = 6,
                Padding = new Padding(0),
                ShadowDecoration = { Enabled = true, Color = Color.FromArgb(226, 232, 240), Depth = 6 }
            };
            shell.Controls.Add(workspace, 0, 1);

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(184, 42),
                SizeMode = TabSizeMode.Fixed,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Padding = new Point(14, 6)
            };
            _tabs.DrawItem += DrawAdminTab;
            _tabs.SelectedIndexChanged += (s, e) => UpdateActionStates();

            _tabs.TabPages.Add(CreateTabPage("User Accounts", out _userGrid, "Manage staff access and passwords."));
            _tabs.TabPages.Add(CreateTabPage("Graduated Students", out _graduateGrid, "View students who have completed their studies."));
            _tabs.TabPages.Add(CreateTabPage("Student Archive", out _studentArchiveGrid, "View and restore students who left or rolled out."));
            _tabs.TabPages.Add(CreateTabPage("Staff Archive", out _staffArchiveGrid, "View and restore former employees."));
            _tabs.TabPages.Add(CreateTabPage("Notification Logs", out _smsLogGrid, "Track SMS and Email communication history."));
            workspace.Controls.Add(_tabs);

            shell.Controls.Add(CreateActionBar(), 0, 2);
            UpdateActionStates();
        }

        private TabPage CreateTabPage(string title, out Guna2DataGridView grid, string description)
        {
            var page = new TabPage(title) { BackColor = Color.White, Padding = new Padding(24, 18, 24, 22) };

            var pageShell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            pageShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            pageShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            headerPanel.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Navy,
                Location = new Point(0, 3),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            headerPanel.Controls.Add(new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Muted,
                Location = new Point(1, 34),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            grid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            StyleAdminGrid(grid);

            pageShell.Controls.Add(headerPanel, 0, 0);
            pageShell.Controls.Add(grid, 0, 1);
            page.Controls.Add(pageShell);
            return page;
        }

        private Control CreateActionBar()
        {
            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 18)
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 650));

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ready.",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 0, 0, 0),
                ForeColor = UiTheme.Muted,
                BackColor = UiTheme.Page
            };
            bar.Controls.Add(_statusLabel, 0, 0);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0)
            };

            var btnRefresh = CreateAdminButton("Refresh Data", UiTheme.Navy, Color.White);
            btnRefresh.Click += async (s, e) => await RefreshAllAsync();
            
            _restoreButton = CreateAdminButton("Restore Selected", UiTheme.Success, Color.White);
            _restoreButton.Click += async (s, e) => await HandleRestoreAsync();

            _deleteButton = CreateAdminButton("Delete User", Color.FromArgb(190, 18, 60), Color.White);
            _deleteButton.Click += async (s, e) => await HandleDeleteAsync();

            buttons.Controls.Add(_deleteButton);
            buttons.Controls.Add(_restoreButton);
            buttons.Controls.Add(btnRefresh);
            bar.Controls.Add(buttons, 1, 0);
            return bar;
        }

        private Guna2Button CreateAdminButton(string text, Color fill, Color fore)
        {
            return new Guna2Button
            {
                Text = text,
                Size = new Size(188, 44),
                Margin = new Padding(10, 0, 0, 0),
                Height = 44,
                BorderRadius = 4,
                FillColor = fill,
                ForeColor = fore,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                HoverState = { FillColor = fill == UiTheme.Navy ? UiTheme.NavyHover : ControlPaint.Dark(fill, 0.06F) }
            };
        }

        private void StyleAdminGrid(Guna2DataGridView grid)
        {
            UiTheme.StyleDataGrid(grid, true);
            grid.ColumnHeadersHeight = 44;
            grid.RowTemplate.Height = 40;
            grid.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 251);
        }

        private void DrawAdminTab(object sender, DrawItemEventArgs e)
        {
            var selected = e.Index == _tabs.SelectedIndex;
            var bounds = e.Bounds;
            using (var back = new SolidBrush(selected ? UiTheme.Navy : Color.White))
            using (var fore = new SolidBrush(selected ? Color.White : UiTheme.Navy))
            using (var border = new Pen(UiTheme.Border))
            {
                e.Graphics.FillRectangle(back, bounds);
                e.Graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                var text = _tabs.TabPages[e.Index].Text;
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    _tabs.Font,
                    bounds,
                    selected ? Color.White : UiTheme.Navy,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void UpdateActionStates()
        {
            if (_tabs == null || _restoreButton == null || _deleteButton == null) return;

            var tab = _tabs.SelectedTab?.Text ?? string.Empty;
            var canRestore = tab == "Student Archive" || tab == "Staff Archive";
            var canDelete = tab == "User Accounts";

            _restoreButton.Enabled = canRestore;
            _restoreButton.FillColor = canRestore ? UiTheme.Success : Color.FromArgb(203, 213, 225);
            _restoreButton.ForeColor = canRestore ? Color.White : UiTheme.Muted;

            _deleteButton.Enabled = canDelete;
            _deleteButton.FillColor = canDelete ? Color.FromArgb(190, 18, 60) : Color.FromArgb(203, 213, 225);
            _deleteButton.ForeColor = canDelete ? Color.White : UiTheme.Muted;
        }

        private async Task RefreshAllAsync()
        {
            _statusLabel.Text = "Refreshing data...";
            try
            {
                _userGrid.DataSource = await _userRepo.GetAllUsersAsTableAsync();
                _graduateGrid.DataSource = await _studentRepo.GetGraduatedAsTableAsync();
                _studentArchiveGrid.DataSource = await _studentRepo.GetRolledOutAsTableAsync();
                _staffArchiveGrid.DataSource = await _employeeRepo.GetRolledOutAsTableAsync();
                _smsLogGrid.DataSource = await _smsRepo.GetLogTableAsync();
                FitGridColumns(_userGrid);
                FitGridColumns(_graduateGrid);
                FitGridColumns(_studentArchiveGrid);
                FitGridColumns(_staffArchiveGrid);
                FitGridColumns(_smsLogGrid);
                _statusLabel.Text = "Ready.";
            }
            catch (Exception ex) { _statusLabel.Text = "Error: " + ex.Message; }
        }

        private void FitGridColumns(DataGridView grid)
        {
            if (grid == null || grid.Columns.Count == 0) return;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.MinimumWidth = 110;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private async Task HandleRestoreAsync()
        {
            var tab = _tabs.SelectedTab.Text;
            if (tab == "Student Archive") await RestoreStudent();
            else if (tab == "Staff Archive") await RestoreEmployee();
            else UIHelper.ShowInfo("Restoration not applicable for this tab.");
        }

        private async Task RestoreStudent()
        {
            if (_studentArchiveGrid.CurrentRow == null) return;
            string id = _studentArchiveGrid.CurrentRow.Cells["ID"].Value.ToString();
            if (ConfirmationHelper.ConfirmBulkOperation("restore student", 1))
            {
                if (await _studentRepo.RestoreAsync(id))
                {
                    UIHelper.ShowSuccess("Student restored to active list.");
                    await RefreshAllAsync();
                }
            }
        }

        private async Task RestoreEmployee()
        {
            if (_staffArchiveGrid.CurrentRow == null) return;
            string id = _staffArchiveGrid.CurrentRow.Cells["ID"].Value.ToString();
            if (ConfirmationHelper.ConfirmBulkOperation("restore employee", 1))
            {
                if (await _employeeRepo.RestoreAsync(id))
                {
                    UIHelper.ShowSuccess("Employee restored to active staff list.");
                    await RefreshAllAsync();
                }
            }
        }

        private async Task HandleDeleteAsync()
        {
            if (_tabs.SelectedTab.Text == "User Accounts")
            {
                if (_userGrid.CurrentRow == null) return;
                string user = _userGrid.CurrentRow.Cells["Username"].Value.ToString();
                if (user.Equals(AuthService.CurrentUser.Username, StringComparison.OrdinalIgnoreCase))
                {
                    UIHelper.ShowError("You cannot delete your own account.");
                    return;
                }
                if (ConfirmationHelper.ConfirmDelete("User Account", user))
                {
                    if (await _userRepo.DeleteUserAsync(user))
                    {
                        UIHelper.ShowSuccess("User account deleted.");
                        await RefreshAllAsync();
                    }
                }
            }
            else UIHelper.ShowInfo("Permanent deletion only allowed for User Accounts in this dashboard.");
        }
    }
}
