using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmAcademicSessionManager : Form
    {
        private readonly AcademicSessionService _service = new AcademicSessionService();
        private DataGridView _yearsGrid;
        private DataGridView _termsGrid;
        private TextBox _yearNameText;
        private DateTimePicker _yearStartDate;
        private DateTimePicker _yearEndDate;
        private ComboBox _yearCombo;
        private TextBox _termNameText;
        private DateTimePicker _termStartDate;
        private DateTimePicker _termEndDate;
        private DateTimePicker _termReopeningDate;
        private Label _activeTermLabel;
        private Label _statusLabel;

        private List<AcademicYear> _years = new List<AcademicYear>();
        private List<AcademicTerm> _terms = new List<AcademicTerm>();

        public frmAcademicSessionManager()
        {
            if (!AuthService.RequireAccess("frmAcademicSessionManager", this)) return;
            InitializeLayout();
            Load += async (s, e) => await RefreshDataAsync();
        }

        private void InitializeLayout()
        {
            Text = "Academic Session Manager";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1260, 760);
            MinimumSize = new Size(1040, 650);
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.25F);
            Icon = Branding.AppIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                Padding = new Padding(30, 26, 30, 24),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildCreatePanels(), 0, 1);
            root.Controls.Add(BuildGrids(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);
        }

        private Control BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };
            _activeTermLabel = new Label
            {
                Dock = DockStyle.Right,
                Width = 390,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                Text = "Academic Session Manager",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold)
            };
            var subtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Create academic years, open terms, close completed sessions, and prepare reopening reminders.",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.75F)
            };

            panel.Controls.Add(_activeTermLabel);
            panel.Controls.Add(subtitle);
            panel.Controls.Add(title);
            return panel;
        }

        private Control BuildCreatePanels()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 2,
                RowCount = 1
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.Controls.Add(BuildYearPanel(), 0, 0);
            grid.Controls.Add(BuildTermPanel(), 1, 0);
            return grid;
        }

        private Control BuildYearPanel()
        {
            var panel = CreateCard("Create Academic Year");
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(20, 18, 20, 18), BackColor = UiTheme.Surface };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            _yearNameText = CreateTextBox($"{DateTime.Today.Year}/{DateTime.Today.Year + 1}");
            _yearStartDate = CreateDatePicker(new DateTime(DateTime.Today.Year, 9, 1));
            _yearEndDate = CreateDatePicker(new DateTime(DateTime.Today.Year + 1, 8, 31));
            var createButton = CreateButton("Create Year", UiTheme.Navy, Color.White);
            createButton.Click += async (s, e) => await CreateYearAsync();
            createButton.Visible = AuthService.CanWrite("Academics.Session.Manage");

            fields.Controls.Add(CreateLabel("Year name"), 0, 0);
            fields.Controls.Add(CreateLabel("Start date"), 1, 0);
            fields.Controls.Add(CreateLabel("End date"), 2, 0);
            fields.Controls.Add(new Label(), 3, 0);
            fields.Controls.Add(_yearNameText, 0, 1);
            fields.Controls.Add(_yearStartDate, 1, 1);
            fields.Controls.Add(_yearEndDate, 2, 1);
            fields.Controls.Add(createButton, 3, 1);
            panel.Controls.Add(fields);
            return panel;
        }

        private Control BuildTermPanel()
        {
            var panel = CreateCard("Create Term");
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, Padding = new Padding(20, 18, 20, 18), BackColor = UiTheme.Surface };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            _yearCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            _termNameText = CreateTextBox("First Term");
            _termStartDate = CreateDatePicker(DateTime.Today);
            _termEndDate = CreateDatePicker(DateTime.Today.AddMonths(3));
            _termReopeningDate = CreateDatePicker(DateTime.Today.AddMonths(4));
            _termEndDate.ValueChanged += (s, e) =>
            {
                if (_termReopeningDate.Value.Date <= _termEndDate.Value.Date)
                    _termReopeningDate.Value = _termEndDate.Value.Date.AddDays(14);
            };
            var createButton = CreateButton("Create Term", UiTheme.Success, Color.White);
            createButton.Click += async (s, e) => await CreateTermAsync();
            createButton.Visible = AuthService.CanWrite("Academics.Session.Manage");

            fields.Controls.Add(CreateLabel("Academic year"), 0, 0);
            fields.Controls.Add(CreateLabel("Term name"), 1, 0);
            fields.Controls.Add(CreateLabel("Start date"), 2, 0);
            fields.Controls.Add(CreateLabel("Closing date"), 3, 0);
            fields.Controls.Add(CreateLabel("Reopens"), 4, 0);
            fields.Controls.Add(new Label(), 5, 0);
            fields.Controls.Add(_yearCombo, 0, 1);
            fields.Controls.Add(_termNameText, 1, 1);
            fields.Controls.Add(_termStartDate, 2, 1);
            fields.Controls.Add(_termEndDate, 3, 1);
            fields.Controls.Add(_termReopeningDate, 4, 1);
            fields.Controls.Add(createButton, 5, 1);
            panel.Controls.Add(fields);
            return panel;
        }

        private Control BuildGrids()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));

            _yearsGrid = CreateGrid();
            _termsGrid = CreateGrid();
            _termsGrid.SelectionChanged += (s, e) => UpdateTermActionState();

            grid.Controls.Add(CreateGridSection("Academic Years", _yearsGrid, null), 0, 0);
            grid.Controls.Add(CreateGridSection("Terms and Closure", _termsGrid, BuildTermActions()), 1, 0);
            return grid;
        }

        private Control BuildTermActions()
        {
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 380,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = UiTheme.Surface
            };
            var close = CreateButton("Close Term", UiTheme.Danger, Color.White);
            close.Name = "btnCloseTerm";
            close.Click += async (s, e) => await CloseSelectedTermAsync();
            close.Visible = AuthService.CanWrite("Academics.Session.Manage");
            var active = CreateButton("Set Active", UiTheme.Success, Color.White);
            active.Name = "btnSetActive";
            active.Click += async (s, e) => await SetSelectedTermActiveAsync();
            active.Visible = AuthService.CanWrite("Academics.Session.Manage");
            actions.Controls.Add(close);
            actions.Controls.Add(active);
            return actions;
        }

        private Control BuildFooter()
        {
            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ready.",
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            return _statusLabel;
        }

        private Panel CreateCard(string title)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = new Padding(0, 0, 16, 0), Padding = new Padding(0, 42, 0, 0) };
            panel.Paint += (s, e) =>
            {
                using (var navy = new SolidBrush(UiTheme.Navy)) e.Graphics.FillRectangle(navy, 0, 0, panel.Width, 42);
                using (var gold = new SolidBrush(UiTheme.Gold)) e.Graphics.FillRectangle(gold, 0, 40, panel.Width, 2);
                using (var pen = new Pen(UiTheme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                using (var font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                    e.Graphics.DrawString(title, font, brush, new RectangleF(18, 11, panel.Width - 36, 22));
            };
            return panel;
        }

        private Control CreateGridSection(string title, DataGridView grid, Control actions)
        {
            var panel = CreateCard(title);
            var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(16, 16, 16, 16) };
            if (actions != null)
            {
                var top = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.Surface };
                top.Controls.Add(actions);
                body.Controls.Add(grid);
                body.Controls.Add(top);
            }
            else
            {
                body.Controls.Add(grid);
            }
            panel.Controls.Add(body);
            return panel;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            UiTheme.StyleDataGrid(grid, true);
            return grid;
        }

        private static Label CreateLabel(string text) =>
            new Label { Dock = DockStyle.Fill, Text = text, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

        private static TextBox CreateTextBox(string text) =>
            new TextBox { Dock = DockStyle.Fill, Text = text, Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle };

        private static DateTimePicker CreateDatePicker(DateTime value) =>
            new DateTimePicker { Dock = DockStyle.Fill, Value = value, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 10F) };

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var button = new Button
            {
                Width = 128,
                Height = 38,
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Margin = new Padding(8, 0, 0, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private async Task RefreshDataAsync()
        {
            SetStatus("Loading academic sessions...", true);
            await _service.InitializeAsync();
            _years = await _service.GetYearsAsync();
            _terms = await _service.GetTermsAsync(true);

            _yearsGrid.DataSource = _years.Select(y => new
            {
                y.AcademicYearID,
                Year = y.DisplayName,
                Start = y.StartDate.ToString("dd/MM/yyyy"),
                End = y.EndDate.ToString("dd/MM/yyyy"),
                Active = y.IsActive ? "Yes" : ""
            }).ToList();
            if (_yearsGrid.Columns["AcademicYearID"] != null) _yearsGrid.Columns["AcademicYearID"].Visible = false;

            _termsGrid.DataSource = _terms.Select(t => new
            {
                t.TermID,
                Year = t.AcademicYearName,
                Term = t.TermName,
                Start = t.StartDate.ToString("dd/MM/yyyy"),
                Closing = t.EndDate.ToString("dd/MM/yyyy"),
                Reopens = t.ReopeningDate.HasValue ? t.ReopeningDate.Value.ToString("dd/MM/yyyy") : "",
                Active = t.IsActive ? "Yes" : "",
                Closed = t.IsClosed ? "Yes" : "",
                Report = t.ClosureReportPath
            }).ToList();
            if (_termsGrid.Columns["TermID"] != null) _termsGrid.Columns["TermID"].Visible = false;

            _yearCombo.DataSource = _years.ToList();
            _yearCombo.DisplayMember = "DisplayName";
            _yearCombo.ValueMember = "AcademicYearID";

            var active = await _service.GetActiveTermAsync();
            _activeTermLabel.Text = active == null ? "No active term selected" : $"Active term: {active.DisplayName}";
            SetStatus("Ready.", true);
            UpdateTermActionState();
        }

        private async Task CreateYearAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Session.Manage", "Create Academic Year"))
                return;

            var result = await _service.CreateAcademicYearAsync(new AcademicYear
            {
                YearName = _yearNameText.Text.Trim(),
                StartDate = _yearStartDate.Value.Date,
                EndDate = _yearEndDate.Value.Date
            });
            SetStatus(result.Message, result.Success);
            if (result.Success) await RefreshDataAsync();
        }

        private async Task CreateTermAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Session.Manage", "Create Academic Term"))
                return;

            if (_yearCombo.SelectedValue == null)
            {
                SetStatus("Create or select an academic year first.", false);
                return;
            }

            var result = await _service.CreateTermAsync(new AcademicTerm
            {
                AcademicYearID = Convert.ToInt32(_yearCombo.SelectedValue),
                TermName = _termNameText.Text.Trim(),
                StartDate = _termStartDate.Value.Date,
                EndDate = _termEndDate.Value.Date,
                ReopeningDate = _termReopeningDate.Value.Date
            });
            SetStatus(result.Message, result.Success);
            if (result.Success) await RefreshDataAsync();
        }

        private async Task SetSelectedTermActiveAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Session.Manage", "Set Active Term"))
                return;

            var term = GetSelectedTerm();
            if (term == null) { SetStatus("Select a term first.", false); return; }
            var result = await _service.SetActiveTermAsync(term.TermID);
            SetStatus(result.Message, result.Success);
            if (result.Success) await RefreshDataAsync();
        }

        private async Task CloseSelectedTermAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Session.Manage", "Close Academic Term"))
                return;

            var term = GetSelectedTerm();
            if (term == null) { SetStatus("Select a term first.", false); return; }
            if (term.IsClosed) { SetStatus("This term is already closed.", false); return; }

            var confirm = MessageBox.Show(
                "Closing this term locks it as an audit period and generates a PDF summary report. Continue?",
                "Close Term",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            var result = await _service.CloseTermAsync(term.TermID);
            SetStatus(string.IsNullOrWhiteSpace(result.ReportPath) ? result.Message : $"{result.Message} Report: {result.ReportPath}", result.Success);
            if (result.Success) await RefreshDataAsync();
        }

        private AcademicTerm GetSelectedTerm()
        {
            if (_termsGrid.CurrentRow == null || _termsGrid.CurrentRow.Cells["TermID"] == null) return null;
            var raw = _termsGrid.CurrentRow.Cells["TermID"].Value;
            if (raw == null) return null;
            var id = Convert.ToInt32(raw);
            return _terms.FirstOrDefault(t => t.TermID == id);
        }

        private void UpdateTermActionState()
        {
            var term = GetSelectedTerm();
            foreach (Control control in _termsGrid.Parent.Controls)
            {
                // Buttons live in the sibling top panel; this method is intentionally light.
            }
        }

        private void SetStatus(string message, bool ok)
        {
            if (_statusLabel == null) return;
            _statusLabel.Text = message;
            _statusLabel.ForeColor = ok ? UiTheme.Muted : UiTheme.Danger;
        }
    }
}
