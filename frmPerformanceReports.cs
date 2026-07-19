using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmPerformanceReports : Form
    {
        private readonly PerformanceReportRepository _repository = new PerformanceReportRepository();
        private DataGridView _reportsGrid;
        private DataGridView _entriesGrid;
        private Button _refreshButton;
        private Button _recentButton;
        private Button _pendingButton;
        private Button _approveButton;
        private Button _rejectButton;
        private Label _statusLabel;
        private Label _detailLabel;
        private List<ClassPerformanceReport> _reports = new List<ClassPerformanceReport>();
        private bool _pendingOnly;

        public frmPerformanceReports()
        {
            BuildUi();
            SessionUi.AttachSignOut(this);
            NavigationSidebar.AddTo(this);
            if (!AuthService.RequireAccess("frmPerformanceReports", this)) return;
            Shown += async (s, e) => await LoadReportsAsync(false);
        }

        private void BuildUi()
        {
            Text = "Class Performance Reports";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 700);
            Size = new Size(1220, 760);
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.5F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(28),
                BackColor = UiTheme.Page
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);
            root.Controls.Add(BuildContent(), 0, 2);

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(_statusLabel, 0, 3);

            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                Text = "Class Performance Reports",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Text = "Synced teacher class performance summaries and student notes",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return panel;
        }

        private Control BuildToolbar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = UiTheme.Page,
                Padding = new Padding(0, 4, 0, 4)
            };

            _recentButton = MakeButton("Recent Reports", true);
            _pendingButton = MakeButton("Pending Approval", false);
            _refreshButton = MakeButton("Refresh", false);
            _approveButton = MakeButton("Approve", false);
            _rejectButton = MakeButton("Reject", false);
            _recentButton.Click += async (s, e) => await LoadReportsAsync(false);
            _pendingButton.Click += async (s, e) => await LoadReportsAsync(true);
            _refreshButton.Click += async (s, e) => await LoadReportsAsync(_pendingOnly);
            _approveButton.Click += async (s, e) => await SetSelectedApprovalStatusAsync("Approved");
            _rejectButton.Click += async (s, e) => await SetSelectedApprovalStatusAsync("Rejected");

            bar.Controls.Add(_recentButton);
            bar.Controls.Add(_pendingButton);
            bar.Controls.Add(_refreshButton);
            if (AuthService.CanWrite("Academics.Performance.Approve"))
            {
                bar.Controls.Add(_approveButton);
                bar.Controls.Add(_rejectButton);
            }
            return bar;
        }

        private Control BuildContent()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                BackColor = UiTheme.Page
            };

            _reportsGrid = MakeGrid();
            _reportsGrid.SelectionChanged += async (s, e) => await LoadSelectedEntriesAsync();
            split.Panel1.Controls.Add(_reportsGrid);

            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.White
            };
            bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _detailLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Select a report to view student entries.",
                BackColor = Color.White,
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            _entriesGrid = MakeGrid();

            bottom.Controls.Add(_detailLabel, 0, 0);
            bottom.Controls.Add(_entriesGrid, 0, 1);
            split.Panel2.Controls.Add(bottom);
            return split;
        }

        private DataGridView MakeGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private Button MakeButton(string text, bool primary)
        {
            var button = new Button
            {
                Text = text,
                Width = 150,
                Height = 34,
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = primary ? UiTheme.Navy : Color.White,
                ForeColor = primary ? Color.White : UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderColor = UiTheme.Border;
            return button;
        }

        private async Task LoadReportsAsync(bool pendingOnly)
        {
            _pendingOnly = pendingOnly;
            _refreshButton.Enabled = false;
            _recentButton.BackColor = pendingOnly ? Color.White : UiTheme.Navy;
            _recentButton.ForeColor = pendingOnly ? UiTheme.Text : Color.White;
            _pendingButton.BackColor = pendingOnly ? UiTheme.Navy : Color.White;
            _pendingButton.ForeColor = pendingOnly ? Color.White : UiTheme.Text;

            try
            {
                _statusLabel.Text = "Loading performance reports...";
                _reports = pendingOnly
                    ? await _repository.GetPendingApprovalAsync()
                    : await _repository.GetRecentAsync();

                var rows = new List<object>();
                foreach (var report in _reports)
                {
                    var workflow = await _repository.GetWorkflowAsync(report.WorkflowId);
                    rows.Add(new
                    {
                        report.ReportId,
                        report.WorkflowId,
                        Class = report.ClassId,
                        Teacher = report.TeacherId,
                        Period = report.ReportPeriod,
                        Year = report.AcademicYear,
                        report.Term,
                        Status = workflow == null ? "Unknown" : workflow.CurrentStatus,
                        Date = report.ReportDate.ToString("dd/MM/yyyy"),
                        Summary = Trim(report.ReportText, 70)
                    });
                }

                _reportsGrid.DataSource = rows;
                if (_reportsGrid.Columns.Contains("ReportId"))
                    _reportsGrid.Columns["ReportId"].Visible = false;
                if (_reportsGrid.Columns.Contains("WorkflowId"))
                    _reportsGrid.Columns["WorkflowId"].Visible = false;
                _entriesGrid.DataSource = null;
                _detailLabel.Text = rows.Count == 0
                    ? "No performance reports found."
                    : "Select a report to view student entries.";
                _statusLabel.Text = $"{rows.Count} performance report(s) loaded.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Could not load performance reports.";
                UIHelper.ShowError("Could not load performance reports: " + ex.Message, "Performance Reports");
            }
            finally
            {
                _refreshButton.Enabled = true;
            }
        }

        private async Task SetSelectedApprovalStatusAsync(string status)
        {
            if (!AuthService.RequireWriteAccess("Academics.Performance.Approve", status + " performance report")) return;

            var report = SelectedReport();
            if (report == null)
            {
                UIHelper.ShowWarning("Select a performance report first.", "Performance Reports");
                return;
            }

            var workflow = await _repository.GetWorkflowAsync(report.WorkflowId);
            if (workflow == null)
            {
                UIHelper.ShowError("The approval workflow for this report was not found.", "Performance Reports");
                return;
            }

            if (workflow.CurrentStatus == status)
            {
                _statusLabel.Text = "Report is already " + status.ToLowerInvariant() + ".";
                return;
            }

            if (!string.Equals(workflow.CurrentStatus, "Submitted", StringComparison.OrdinalIgnoreCase)
                && UIHelper.ShowConfirmation(
                    $"This report is currently {workflow.CurrentStatus}. Change it to {status}?",
                    "Confirm Status Change") != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _approveButton.Enabled = false;
                _rejectButton.Enabled = false;
                var saved = await _repository.SetApprovalStatusAsync(report.WorkflowId, status);
                if (!saved)
                {
                    UIHelper.ShowError("Could not update the approval status.", "Performance Reports");
                    return;
                }

                await LoadReportsAsync(_pendingOnly);
                _statusLabel.Text = "Performance report marked " + status.ToLowerInvariant() + " and queued for sync.";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not update approval status: " + ex.Message, "Performance Reports");
            }
            finally
            {
                _approveButton.Enabled = true;
                _rejectButton.Enabled = true;
            }
        }

        private ClassPerformanceReport SelectedReport()
        {
            if (_reportsGrid.CurrentRow == null || !_reportsGrid.Columns.Contains("ReportId")) return null;
            var value = _reportsGrid.CurrentRow.Cells["ReportId"].Value;
            if (value == null) return null;
            var reportId = Convert.ToInt32(value);
            return _reports.FirstOrDefault(r => r.ReportId == reportId);
        }

        private async Task LoadSelectedEntriesAsync()
        {
            var report = SelectedReport();
            if (report == null) return;

            try
            {
                var entries = await _repository.GetEntriesAsync(report.ReportId);
                _entriesGrid.DataSource = entries.Select(e => new
                {
                    Student = e.StudentId,
                    Trend = e.PerformanceTrend,
                    Notes = e.TeacherNotes
                }).ToList();
                _detailLabel.Text = $"{report.ClassId} | {report.ReportPeriod} | {report.AcademicYear} {report.Term} | {entries.Count} student entr{(entries.Count == 1 ? "y" : "ies")}";
            }
            catch (Exception ex)
            {
                _detailLabel.Text = "Could not load student entries.";
                LoggerHelper.LogError("frmPerformanceReports.LoadSelectedEntriesAsync failed", ex);
            }
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Replace(Environment.NewLine, " ").Trim();
            return value.Length <= max ? value : value.Substring(0, max - 3) + "...";
        }
    }
}
