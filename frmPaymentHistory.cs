using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Standalone, searchable payment-history browser. Split out of frmFessPayment so the
    /// payment wizard isn't cramped. Read-only; reuses FeeRepository.GetPaymentHistoryTableAsync.
    /// Access: Accountant / Director / Administrator / Headmaster.
    /// </summary>
    public class frmPaymentHistory : Form
    {
        private readonly FeeRepository _feeRepository = new FeeRepository(AppConfig.ConnectionString);
        private DataGridView _grid;
        private TextBox _searchBox;
        private Label _countLbl;
        private Button _refreshBtn;

        public frmPaymentHistory()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmPaymentHistory", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "Payment History";
            Size = new Size(1020, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.Surface;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 46, Text = "  Payment History",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Navy, TextAlign = ContentAlignment.MiddleLeft
            };

            var bar = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(12, 7, 12, 7), BackColor = UiTheme.Surface };
            var searchLbl = new Label { Dock = DockStyle.Left, Width = 56, Text = "Search:", TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10F), ForeColor = UiTheme.Text };
            _searchBox = new TextBox { Dock = DockStyle.Left, Width = 360, Font = new Font("Segoe UI", 10F) };
            _searchBox.TextChanged += (s, e) => Filter();
            _refreshBtn = new Button { Dock = DockStyle.Right, Width = 100, Text = "Refresh", FlatStyle = FlatStyle.Flat };
            _refreshBtn.Click += async (s, e) => await LoadAsync();
            _countLbl = new Label
            {
                Dock = DockStyle.Right, Width = 116, Text = "0 records", ForeColor = Color.White,
                BackColor = UiTheme.Navy, TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
            };
            bar.Controls.Add(_searchBox);
            bar.Controls.Add(searchLbl);
            bar.Controls.Add(_countLbl);
            bar.Controls.Add(_refreshBtn);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = UiTheme.Surface, BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            UiTheme.StyleDataGrid(_grid, true);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Navy;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            _grid.ColumnHeadersHeight = 32;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.DefaultCellStyle.BackColor = UiTheme.Surface;
            _grid.DefaultCellStyle.ForeColor = UiTheme.Text;
            _grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            _grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
            _grid.GridColor = UiTheme.Border;
            _grid.CellFormatting += Grid_CellFormatting;
            Common.StudentId.AttachGridFormatting(_grid, "STUDENT ID");

            Controls.Add(_grid);
            Controls.Add(bar);
            Controls.Add(title);
        }

        private async Task LoadAsync()
        {
            try
            {
                DataTable table = await _feeRepository.GetPaymentHistoryTableAsync();
                _grid.DataSource = table;
                ApplyColumnLayout();
                _countLbl.Text = table.Rows.Count + " records";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("History load error: " + ex.Message, "Payment History");
            }
        }

        private void Filter()
        {
            if (!(_grid.DataSource is DataTable table)) return;
            string filter = _searchBox.Text.Trim().Replace("'", "''");
            table.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                ? ""
                : string.Format(
                    "[STUDENT ID] LIKE '%{0}%' OR [STUDENT NAME] LIKE '%{0}%' OR [CLASS ID] LIKE '%{0}%' OR [BURSAR NAME] LIKE '%{0}%'",
                    filter);
            _countLbl.Text = table.DefaultView.Count + " records";
        }

        private void ApplyColumnLayout()
        {
            if (_grid.Columns.Count == 0) return;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SetColumn("STUDENT ID", 76, 70);
            SetColumn("CLASS ID", 82, 75);
            SetColumn("STUDENT NAME", 170, 150);
            SetColumn("AMOUNT PAID", 104, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn("BALANCE", 96, 90, DataGridViewContentAlignment.MiddleRight);
            SetColumn("PAYMENT DATE", 104, 95, DataGridViewContentAlignment.MiddleCenter);
            SetColumn("PAYMENT TIME", 96, 90, DataGridViewContentAlignment.MiddleCenter);
            SetColumn("PAYMENT MODE", 118, 105);
            SetColumn("BURSAR NAME", 136, 120);
        }

        private void SetColumn(string name, int fillWeight, int minWidth,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            if (!_grid.Columns.Contains(name)) return;
            var c = _grid.Columns[name];
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            c.FillWeight = fillWeight;
            c.MinimumWidth = minWidth;
            c.DefaultCellStyle.Alignment = alignment;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null || e.Value == DBNull.Value || e.ColumnIndex < 0) return;
            string columnName = _grid.Columns[e.ColumnIndex].Name;

            if (columnName == "AMOUNT PAID" || columnName == "BALANCE")
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal amount))
                {
                    e.Value = amount.ToString("N2");
                    e.FormattingApplied = true;
                }
                return;
            }
            if (columnName == "PAYMENT DATE")
            {
                if (DateTime.TryParse(e.Value.ToString(), out DateTime dateValue))
                {
                    e.Value = dateValue.ToString("dd/MM/yyyy");
                    e.FormattingApplied = true;
                }
                return;
            }
            if (columnName == "PAYMENT TIME")
            {
                if (e.Value is TimeSpan timeValue)
                {
                    e.Value = timeValue.ToString(@"hh\:mm");
                    e.FormattingApplied = true;
                }
                else if (DateTime.TryParse(e.Value.ToString(), out DateTime dateTimeValue))
                {
                    e.Value = dateTimeValue.ToString("HH:mm");
                    e.FormattingApplied = true;
                }
                else if (TimeSpan.TryParse(e.Value.ToString(), out TimeSpan parsedTime))
                {
                    e.Value = parsedTime.ToString(@"hh\:mm");
                    e.FormattingApplied = true;
                }
            }
        }
    }
}
