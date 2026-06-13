using System;
using System.Data;
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
    public class frmExpenses : Form
    {
        private readonly ExpenseRepository _repo = new ExpenseRepository(AppConfig.ConnectionString);
        private readonly DashboardService _dash =
            new DashboardService(new DashboardRepository(AppConfig.ConnectionString));

        private ComboBox _window, _category;
        private DateTimePicker _customFrom, _customTo, _date;
        private TextBox _name, _amount, _payee, _description;
        private Label _lblIncome, _lblExpenses, _lblFund, _total;
        private DataGridView _grid;
        private Button _btnRecord, _btnDelete;
        private int _editingId;

        public frmExpenses()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmExpenses", this)) return;
            Load += async (s, e) =>
            {
                try { await _repo.EnsureTablesAsync(); await LoadCategoriesAsync(); await RefreshAsync(); }
                catch (Exception ex) { LoggerHelper.LogError("Expenses init", ex); }
            };
        }

        private (DateTime From, DateTime To, string Label) CurrentWindow()
        {
            var w = (FinanceWindow)(_window.SelectedIndex < 0 ? 0 : _window.SelectedIndex);
            return FinancePeriod.Resolve(w, DateTime.Today, _customFrom.Value, _customTo.Value);
        }

        private void BuildUi()
        {
            Text = "Expenses";
            Size = new Size(980, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label { Dock = DockStyle.Top, Height = 44, Text = "  Expenses",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 96, WrapContents = true, Padding = new Padding(12, 6, 12, 6) };
            top.Controls.Add(new Label { Text = "Window:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            _window = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 10, 0) };
            _window.Items.AddRange(new object[] { "This Term", "This Year", "All Time", "Custom" });
            _window.SelectedIndex = 0;
            _window.SelectedIndexChanged += async (s, e) => { UpdateCustomVisibility(); await RefreshAsync(); };
            _customFrom = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Visible = false, Margin = new Padding(0, 4, 6, 0) };
            _customTo = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Visible = false, Margin = new Padding(0, 4, 10, 0) };
            _customFrom.ValueChanged += async (s, e) => await RefreshAsync();
            _customTo.ValueChanged += async (s, e) => await RefreshAsync();
            top.Controls.Add(_window); top.Controls.Add(_customFrom); top.Controls.Add(_customTo);
            _lblIncome = SummaryCard("Total Income", AppConfig.Colors.SuccessColor);
            _lblExpenses = SummaryCard("Total Expenses", AppConfig.Colors.DangerColor);
            _lblFund = SummaryCard("Total Fund", AppConfig.Colors.PrimaryColor);
            top.Controls.Add(_lblIncome.Parent); top.Controls.Add(_lblExpenses.Parent); top.Controls.Add(_lblFund.Parent);

            var entry = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 76, WrapContents = true, Padding = new Padding(12, 4, 12, 4), BackColor = Color.White };
            _name = Field(entry, "Name", 160);
            _category = new ComboBox { Width = 150, Margin = new Padding(0, 22, 8, 0) };
            entry.Controls.Add(Captioned("Category", _category));
            _amount = Field(entry, "Amount", 100);
            _date = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 22, 8, 0) };
            entry.Controls.Add(Captioned("Date", _date));
            _payee = Field(entry, "Payee", 130);
            _description = Field(entry, "Description", 180);
            _btnRecord = new Button { Text = "Record", Width = 100, Height = 30, Margin = new Padding(0, 20, 6, 0),
                BackColor = AppConfig.Colors.PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnRecord.Click += async (s, e) => await SaveAsync();
            var clear = new Button { Text = "Clear", Width = 70, Height = 30, Margin = new Padding(0, 20, 6, 0), FlatStyle = FlatStyle.Flat };
            clear.Click += (s, e) => ClearEntry();
            _btnDelete = new Button { Text = "Delete", Width = 80, Height = 30, Margin = new Padding(0, 20, 0, 0), Enabled = false,
                BackColor = Color.FromArgb(190, 18, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnDelete.Click += async (s, e) => await DeleteAsync();
            entry.Controls.Add(_btnRecord); entry.Controls.Add(clear); entry.Controls.Add(_btnDelete);

            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            _grid.SelectionChanged += (s, e) => LoadSelectedRow();

            _total = new Label { Dock = DockStyle.Bottom, Height = 26, TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), Padding = new Padding(0, 0, 16, 0) };

            Controls.Add(_grid);
            Controls.Add(_total);
            Controls.Add(entry);
            Controls.Add(top);
            Controls.Add(title);
        }

        private void UpdateCustomVisibility()
        {
            bool custom = _window.SelectedIndex == 3;
            _customFrom.Visible = custom; _customTo.Visible = custom;
        }

        private static Label SummaryCard(string caption, Color accent)
        {
            var panel = new Panel { Width = 180, Height = 76, BackColor = Color.White, Margin = new Padding(6, 2, 0, 0), BorderStyle = BorderStyle.FixedSingle };
            panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top, Height = 22, ForeColor = AppConfig.Colors.MutedTextColor, Font = new Font("Segoe UI", 8.5F), Padding = new Padding(8, 4, 0, 0) });
            var val = new Label { Text = "GHS 0.00", Dock = DockStyle.Fill, ForeColor = accent, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
            panel.Controls.Add(val);
            return val;
        }

        private static TextBox Field(FlowLayoutPanel host, string caption, int width)
        {
            var t = new TextBox { Width = width };
            host.Controls.Add(Captioned(caption, t));
            return t;
        }

        private static Control Captioned(string caption, Control c)
        {
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            p.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = AppConfig.Colors.MutedTextColor, Font = new Font("Segoe UI", 8F) });
            p.Controls.Add(c);
            return p;
        }

        private async Task LoadCategoriesAsync()
        {
            var cats = await _repo.GetCategoriesAsync();
            _category.Items.Clear();
            _category.Items.AddRange(cats.Cast<object>().ToArray());
        }

        private async Task RefreshAsync()
        {
            try
            {
                var w = CurrentWindow();
                var fin = await _dash.GetFinanceSummaryAsync(w.From, w.To);
                _lblIncome.Text = "GHS " + fin.Income.ToString("N2");
                _lblExpenses.Text = "GHS " + fin.Expenses.ToString("N2");
                _lblFund.Text = "GHS " + fin.Fund.ToString("N2");
                _lblFund.ForeColor = fin.Fund < 0 ? AppConfig.Colors.DangerColor : AppConfig.Colors.SuccessColor;

                var list = await _repo.GetByRangeAsync(w.From, w.To, null);
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("Date"); dt.Columns.Add("Name"); dt.Columns.Add("Category");
                dt.Columns.Add("Amount", typeof(decimal)); dt.Columns.Add("Payee"); dt.Columns.Add("Payer"); dt.Columns.Add("Description");
                foreach (var e in list)
                    dt.Rows.Add(e.Id, e.Date.ToString("dd MMM yyyy"), e.Name, e.Category, e.Amount, e.Payee, e.Payer, e.Description);
                _grid.DataSource = dt;
                if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
                _total.Text = $"Window: {w.Label}   |   {list.Count} expense(s)   |   Total: GHS {list.Sum(x => x.Amount):N2}";
            }
            catch (Exception ex) { LoggerHelper.LogError("Expenses refresh", ex); }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { UIHelper.ShowWarning("Enter an expense name.", "Expenses"); return; }
            string category = (_category.Text ?? "").Trim();
            if (category.Length == 0) { UIHelper.ShowWarning("Enter or pick a category.", "Expenses"); return; }
            if (!decimal.TryParse(_amount.Text, out decimal amount) || amount < 0) { UIHelper.ShowWarning("Amount must be a number ≥ 0.", "Expenses"); return; }

            var e = new Expense
            {
                Id = _editingId, Name = _name.Text.Trim(), Category = category, Description = _description.Text.Trim(),
                Date = _date.Value.Date, Amount = amount, Payee = _payee.Text.Trim(),
                Payer = AuthService.CurrentUser?.DisplayName ?? ""
            };
            try
            {
                await _repo.AddCategoryAsync(category);
                if (_editingId > 0) await _repo.UpdateAsync(e); else await _repo.AddAsync(e);
                ClearEntry();
                await LoadCategoriesAsync();
                await RefreshAsync();
            }
            catch (Exception ex) { LoggerHelper.LogError("Expenses save", ex); UIHelper.ShowError("Save failed: " + ex.Message, "Expenses"); }
        }

        private void LoadSelectedRow()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.IsNewRow) return;
            var r = _grid.CurrentRow;
            _editingId = Convert.ToInt32(r.Cells["Id"].Value);
            _name.Text = r.Cells["Name"].Value?.ToString();
            _category.Text = r.Cells["Category"].Value?.ToString();
            _amount.Text = Convert.ToDecimal(r.Cells["Amount"].Value).ToString("0.##");
            _payee.Text = r.Cells["Payee"].Value?.ToString();
            _description.Text = r.Cells["Description"].Value?.ToString();
            DateTime dt; if (DateTime.TryParse(r.Cells["Date"].Value?.ToString(), out dt)) _date.Value = dt;
            _btnRecord.Text = "Update"; _btnDelete.Enabled = true;
        }

        private async Task DeleteAsync()
        {
            if (_editingId <= 0) return;
            if (UIHelper.ShowConfirmation("Delete this expense?", "Expenses") != DialogResult.Yes) return;
            try { await _repo.DeleteAsync(_editingId); ClearEntry(); await RefreshAsync(); }
            catch (Exception ex) { LoggerHelper.LogError("Expenses delete", ex); UIHelper.ShowError("Delete failed: " + ex.Message, "Expenses"); }
        }

        private void ClearEntry()
        {
            _editingId = 0;
            _name.Clear(); _category.Text = ""; _amount.Clear(); _payee.Clear(); _description.Clear();
            _date.Value = DateTime.Today;
            _btnRecord.Text = "Record"; _btnDelete.Enabled = false;
        }
    }
}
