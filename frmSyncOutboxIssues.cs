using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmSyncOutboxIssues : Form
    {
        private readonly OfflineSyncService _syncService = new OfflineSyncService();
        private DataGridView _grid;
        private Label _summaryLabel;
        private TextBox _errorBox;
        private Button _refreshButton;
        private Button _closeButton;

        public frmSyncOutboxIssues()
        {
            if (!AuthService.RequireAccess("frmSyncStatus", this)) return;
            InitializeLayout();
            Load += async (s, e) => await LoadIssuesAsync();
        }

        private void InitializeLayout()
        {
            Text = "Sync Problem Rows";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1060, 700);
            MinimumSize = new Size(900, 560);
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            Icon = Branding.AppIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                Padding = new Padding(26, 24, 26, 22),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildGrid(), 0, 1);
            root.Controls.Add(BuildErrorPanel(), 0, 2);
            root.Controls.Add(BuildActionBar(), 0, 3);
        }

        private Control BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Sync Problem Rows",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _summaryLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Loading local outbox rows with upload errors...",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.Add(_summaryLabel);
            panel.Controls.Add(title);
            return panel;
        }

        private Control BuildGrid()
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = UiTheme.Surface,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Navy;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            _grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            _grid.Columns.Add(CreateTextColumn("OutboxId", "ID", 70));
            _grid.Columns.Add(CreateTextColumn("TableName", "Table", 145));
            _grid.Columns.Add(CreateTextColumn("Operation", "Op", 70));
            _grid.Columns.Add(CreateTextColumn("RecordText", "Record", 210));
            _grid.Columns.Add(CreateTextColumn("Attempts", "Attempts", 80));
            _grid.Columns.Add(CreateTextColumn("UpdatedText", "Updated", 150));
            _grid.Columns.Add(CreateTextColumn("LastError", "Last Error", 320));
            _grid.SelectionChanged += (s, e) => ShowSelectedError();

            return _grid;
        }

        private Control BuildErrorPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(16, 12, 16, 12)
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiTheme.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = "Selected Row Error",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.Text
            };

            _errorBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.Text
            };

            panel.Controls.Add(_errorBox);
            panel.Controls.Add(label);
            return panel;
        }

        private Control BuildActionBar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = UiTheme.Page,
                Padding = new Padding(0, 14, 0, 0),
                WrapContents = false
            };

            _closeButton = CreateButton("Close", UiTheme.Navy, Color.White);
            _closeButton.Click += (s, e) => Close();

            _refreshButton = CreateButton("Refresh", Color.White, UiTheme.Text);
            _refreshButton.FlatAppearance.BorderColor = UiTheme.Border;
            _refreshButton.FlatAppearance.BorderSize = 1;
            _refreshButton.Click += async (s, e) => await LoadIssuesAsync();

            bar.Controls.Add(_closeButton);
            bar.Controls.Add(_refreshButton);
            return bar;
        }

        private async Task LoadIssuesAsync()
        {
            try
            {
                SetBusy(true);
                var rows = await _syncService.GetOutboxIssueRowsAsync(200);
                _grid.DataSource = rows.ConvertAll(SyncOutboxIssueView.FromRow);
                _summaryLabel.Text = rows.Count == 0
                    ? "No pending sync rows currently have upload errors."
                    : rows.Count.ToString("N0") + " pending sync row(s) need attention.";
                ShowSelectedError();
            }
            catch (Exception ex)
            {
                _summaryLabel.Text = "Could not load sync problem rows.";
                _errorBox.Text = ex.Message;
                UIHelper.ShowError(ex.Message, "Sync Problem Rows");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowSelectedError()
        {
            if (_grid.CurrentRow?.DataBoundItem is SyncOutboxIssueView row)
                _errorBox.Text = string.IsNullOrWhiteSpace(row.LastError) ? "No error text stored." : row.LastError;
            else
                _errorBox.Text = "";
        }

        private void SetBusy(bool busy)
        {
            _refreshButton.Enabled = !busy;
            _closeButton.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string property, string header, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                Width = width,
                AutoSizeMode = property == "LastError"
                    ? DataGridViewAutoSizeColumnMode.Fill
                    : DataGridViewAutoSizeColumnMode.None
            };
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var button = new Button
            {
                Width = 150,
                Height = 42,
                Margin = new Padding(10, 0, 0, 0),
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private sealed class SyncOutboxIssueView
        {
            public long OutboxId { get; set; }
            public string TableName { get; set; }
            public string Operation { get; set; }
            public string RecordText { get; set; }
            public int Attempts { get; set; }
            public string UpdatedText { get; set; }
            public string LastError { get; set; }

            public static SyncOutboxIssueView FromRow(SyncOutboxIssueRow row)
            {
                return new SyncOutboxIssueView
                {
                    OutboxId = row.OutboxId,
                    TableName = row.TableName,
                    Operation = row.Operation,
                    RecordText = row.PrimaryKeyName + ": " + row.PrimaryKeyValue,
                    Attempts = row.Attempts,
                    UpdatedText = row.UpdatedAt == DateTime.MinValue ? "-" : row.UpdatedAt.ToLocalTime().ToString("dd MMM yyyy h:mm tt"),
                    LastError = row.LastError
                };
            }
        }
    }
}
