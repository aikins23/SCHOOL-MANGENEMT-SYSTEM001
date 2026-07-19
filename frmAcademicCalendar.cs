using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAcademicCalendar : Form
    {
        private readonly AcademicCalendarRepository _calendarRepo;
        private Guna2DataGridView _termGrid, _eventGrid;
        private Guna2DateTimePicker _dateStart, _dateEnd, _dateEvent;
        private Guna2TextBox _txtTermName, _txtEventName;
        private Guna2ComboBox _cmbEventType;

        public frmAcademicCalendar()
        {
            InitializeComponent();
            this.Icon = Branding.AppIcon;
            _calendarRepo = new AcademicCalendarRepository(AppConfig.ConnectionString);
            if (!AuthService.RequireAccess("frmAcademicCalendar", this)) return;

            BuildUi();
            Load += async (s, e) => await RefreshDataAsync();
        }

        private void BuildUi()
        {
            Text = "Academic Calendar & Events";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Page;

            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(230, 20, 20, 20) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // --- Left Panel: Terms ---
            var leftPanel = CreateSection("ACADEMIC TERMS", out _termGrid);
            var termForm = new Panel { Dock = DockStyle.Top, Height = 220, Padding = new Padding(10) };

            _txtTermName = new Guna2TextBox { PlaceholderText = "Term Name (e.g. Term 1 2024)", Width = 250, Location = new Point(120, 10) };
            _dateStart = new Guna2DateTimePicker { Width = 250, Format = DateTimePickerFormat.Short, Location = new Point(120, 60) };
            _dateEnd = new Guna2DateTimePicker { Width = 250, Format = DateTimePickerFormat.Short, Location = new Point(120, 110) };

            var btnSaveTerm = new Guna2Button { Text = "Save Term", FillColor = UiTheme.Navy, Width = 150, Location = new Point(120, 160) };
            btnSaveTerm.Click += async (s, e) => await SaveTermAsync();
            btnSaveTerm.Visible = AuthService.CanWrite("Academics.Calendar.Manage");

            termForm.Controls.Add(new Label { Text = "Term Name:", Location = new Point(10, 18), AutoSize = true });
            termForm.Controls.Add(_txtTermName);
            termForm.Controls.Add(new Label { Text = "Start Date:", Location = new Point(10, 68), AutoSize = true });
            termForm.Controls.Add(_dateStart);
            termForm.Controls.Add(new Label { Text = "End Date:", Location = new Point(10, 118), AutoSize = true });
            termForm.Controls.Add(_dateEnd);
            termForm.Controls.Add(btnSaveTerm);

            leftPanel.Controls.Add(termForm);
            mainLayout.Controls.Add(leftPanel, 0, 0);

            // --- Right Panel: Events ---
            var rightPanel = CreateSection("HOLIDAYS & EVENTS", out _eventGrid);
            var eventForm = new Panel { Dock = DockStyle.Top, Height = 220, Padding = new Padding(10) };

            _txtEventName = new Guna2TextBox { PlaceholderText = "Event Description", Width = 250, Location = new Point(120, 10) };
            _dateEvent = new Guna2DateTimePicker { Width = 250, Format = DateTimePickerFormat.Short, Location = new Point(120, 60) };
            _cmbEventType = new Guna2ComboBox { Width = 250, Location = new Point(120, 110) };
            _cmbEventType.Items.AddRange(new[] { "Holiday", "Event", "Exam" });
            _cmbEventType.SelectedIndex = 0;

            var btnSaveEvent = new Guna2Button { Text = "Add Event", FillColor = UiTheme.Gold, Width = 150, Location = new Point(120, 160) };
            btnSaveEvent.Visible = AuthService.CanWrite("Academics.Calendar.Manage");

            eventForm.Controls.Add(new Label { Text = "Event Name:", Location = new Point(10, 18), AutoSize = true });
            eventForm.Controls.Add(_txtEventName);
            eventForm.Controls.Add(new Label { Text = "Date:", Location = new Point(10, 68), AutoSize = true });
            eventForm.Controls.Add(_dateEvent);
            eventForm.Controls.Add(new Label { Text = "Type:", Location = new Point(10, 118), AutoSize = true });
            eventForm.Controls.Add(_cmbEventType);
            eventForm.Controls.Add(btnSaveEvent);

            rightPanel.Controls.Add(eventForm);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            Controls.Add(mainLayout);
            NavigationSidebar.AddTo(this);
        }

        private Panel CreateSection(string title, out Guna2DataGridView grid)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lbl = new Label { Text = title, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = UiTheme.Navy, Dock = DockStyle.Top, Height = 30 };
            grid = new Guna2DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, ReadOnly = true, RowHeadersVisible = false };
            UiTheme.StyleDataGrid(grid);
            pnl.Controls.Add(grid);
            pnl.Controls.Add(lbl);
            return pnl;
        }

        private async System.Threading.Tasks.Task RefreshDataAsync()
        {
            var terms = await _calendarRepo.GetAllTermsAsync();
            _termGrid.DataSource = terms.Select(t => new { t.TermName, Start = t.StartDate.ToShortDateString(), End = t.EndDate.ToShortDateString(), Active = t.IsActive }).ToList();

            var events = await _calendarRepo.GetEventsAsync(DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(6));
            _eventGrid.DataSource = events.Select(e => new { e.EventDate, e.EventName, e.EventType }).ToList();
        }

        private async System.Threading.Tasks.Task SaveTermAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Calendar.Manage", "Save Academic Term"))
                return;

            if (string.IsNullOrWhiteSpace(_txtTermName.Text)) return;

            var term = new AcademicTerm
            {
                TermName = _txtTermName.Text.Trim(),
                StartDate = _dateStart.Value,
                EndDate = _dateEnd.Value,
                IsActive = true
            };

            if (await _calendarRepo.SaveTermAsync(term))
            {
                UIHelper.ShowSuccess("Term saved successfully.");
                await RefreshDataAsync();
            }
        }
    }
}
