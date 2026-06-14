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

        // Changing the reporting window (term / year / all-time / custom) is a Director privilege;
        // everyone else sees the current term only.
        private readonly bool _isDirector = AuthService.CurrentUser.Role == AuthService.UserRole.Director;

        private ComboBox _window, _category;
        private Label _windowFixed;
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
            // Non-directors are locked to the current term.
            if (!_isDirector) return FinancePeriod.Resolve(FinanceWindow.Term, DateTime.Today);
            var w = (FinanceWindow)(_window.SelectedIndex < 0 ? 0 : _window.SelectedIndex);
            return FinancePeriod.Resolve(w, DateTime.Today, _customFrom.Value, _customTo.Value);
        }

        private void BuildUi()
        {
            Text = "Expenses";
            Size = new Size(1040, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.25F);

            // ── Header band ──────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = UiTheme.Surface };
            header.Controls.Add(new Label
            {
                Text = "Expenses", Location = new Point(22, 12), AutoSize = true,
                Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), ForeColor = UiTheme.Navy
            });
            header.Controls.Add(new Label
            {
                Text = "Track and manage school expenditure", Location = new Point(24, 44), AutoSize = true,
                Font = new Font("Segoe UI", 9F), ForeColor = UiTheme.Muted
            });
            header.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = UiTheme.Gold });

            // ── Summary band: window selector + 3 accent cards ───────────
            var band = new Panel { Dock = DockStyle.Top, Height = 138, BackColor = UiTheme.Page, Padding = new Padding(22, 14, 22, 6) };

            var winRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, WrapContents = false, AutoSize = false };
            winRow.Controls.Add(new Label { Text = "Window", AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(0, 7, 8, 0), Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) });
            if (_isDirector)
            {
                _window = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
                _window.Items.AddRange(new object[] { "This Term", "This Year", "All Time", "Custom" });
                _window.SelectedIndex = 0;
                _window.SelectedIndexChanged += async (s, e) => { UpdateCustomVisibility(); await RefreshAsync(); };
                _customFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Visible = false, Margin = new Padding(0, 3, 6, 0) };
                _customTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Visible = false, Margin = new Padding(0, 3, 0, 0) };
                _customFrom.ValueChanged += async (s, e) => await RefreshAsync();
                _customTo.ValueChanged += async (s, e) => await RefreshAsync();
                winRow.Controls.Add(_window); winRow.Controls.Add(_customFrom); winRow.Controls.Add(_customTo);
            }
            else
            {
                _customFrom = new DateTimePicker(); _customTo = new DateTimePicker(); // unused, avoids null
                _windowFixed = new Label { Text = "Current term", AutoSize = true, ForeColor = UiTheme.Navy, Margin = new Padding(0, 7, 0, 0), Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
                winRow.Controls.Add(_windowFixed);
                winRow.Controls.Add(new Label { Text = "(only the Director can change the reporting window)", AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(12, 8, 0, 0), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic) });
            }

            var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = false, Padding = new Padding(0, 8, 0, 0) };
            _lblIncome = AccentCard(cards, "TOTAL INCOME", UiTheme.Success);
            _lblExpenses = AccentCard(cards, "TOTAL EXPENSES", Color.FromArgb(190, 18, 60));
            _lblFund = AccentCard(cards, "TOTAL FUND", UiTheme.Navy);

            band.Controls.Add(cards);
            band.Controls.Add(winRow);

            // ── Record-expense card ──────────────────────────────────────
            var entryHost = new Panel { Dock = DockStyle.Top, Height = 116, BackColor = UiTheme.Page, Padding = new Padding(22, 2, 22, 8) };
            var entryCard = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(14, 8, 14, 8) };
            entryCard.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, entryCard.ClientRectangle, UiTheme.Border, ButtonBorderStyle.Solid);
            entryCard.Controls.Add(new Label { Text = "Record Expense", Dock = DockStyle.Top, Height = 22, ForeColor = UiTheme.Navy, Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold) });

            var entry = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, BackColor = UiTheme.Surface };
            _name = Field(entry, "Name", 170);
            _category = new ComboBox { Width = 150, Margin = new Padding(0, 20, 10, 0) };
            entry.Controls.Add(Captioned("Category", _category));
            _amount = Field(entry, "Amount (GHS)", 110);
            _date = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 20, 10, 0) };
            entry.Controls.Add(Captioned("Date", _date));
            _payee = Field(entry, "Payee", 140);
            _description = Field(entry, "Description", 190);
            _btnRecord = PrimaryButton("Record", 104);
            _btnRecord.Click += async (s, e) => await SaveAsync();
            var clear = NeutralButton("Clear", 74);
            clear.Click += (s, e) => ClearEntry();
            _btnDelete = DangerButton("Delete", 84);
            _btnDelete.Enabled = false;
            _btnDelete.Click += async (s, e) => await DeleteAsync();
            entry.Controls.Add(_btnRecord); entry.Controls.Add(clear); entry.Controls.Add(_btnDelete);
            entryCard.Controls.Add(entry);
            entryHost.Controls.Add(entryCard);

            // ── Grid + footer ────────────────────────────────────────────
            var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page, Padding = new Padding(22, 0, 22, 0) };
            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
            UiTheme.StyleDataGrid(_grid, fillColumns: true);
            _grid.SelectionChanged += (s, e) => LoadSelectedRow();
            gridHost.Controls.Add(_grid);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = UiTheme.Surface };
            footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = UiTheme.Border });
            _total = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), ForeColor = UiTheme.Navy, Padding = new Padding(0, 0, 24, 0) };
            footer.Controls.Add(_total);

            Controls.Add(gridHost);
            Controls.Add(footer);
            Controls.Add(entryHost);
            Controls.Add(band);
            Controls.Add(header);
        }

        private void UpdateCustomVisibility()
        {
            if (!_isDirector) return;
            bool custom = _window.SelectedIndex == 3;
            _customFrom.Visible = custom; _customTo.Visible = custom;
        }

        // ── Styling helpers ──────────────────────────────────────────────
        private static Label AccentCard(FlowLayoutPanel host, string caption, Color accent)
        {
            var panel = new Panel { Width = 248, Height = 86, BackColor = UiTheme.Surface, Margin = new Padding(0, 0, 14, 0) };
            panel.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, UiTheme.Border, ButtonBorderStyle.Solid);
            panel.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent });
            panel.Controls.Add(new Label { Text = caption, Location = new Point(18, 12), AutoSize = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) });
            var val = new Label { Text = "GHS 0.00", Location = new Point(16, 34), AutoSize = true, ForeColor = accent, Font = new Font("Segoe UI", 19F, FontStyle.Bold) };
            panel.Controls.Add(val);
            val.BringToFront();
            host.Controls.Add(panel);
            return val;
        }

        private static TextBox Field(FlowLayoutPanel host, string caption, int width)
        {
            var t = new TextBox { Width = width, Font = new Font("Segoe UI", 9.75F) };
            host.Controls.Add(Captioned(caption, t));
            return t;
        }

        private static Control Captioned(string caption, Control c)
        {
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 10, 0), BackColor = Color.Transparent };
            p.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold), Margin = new Padding(2, 0, 0, 2) });
            p.Controls.Add(c);
            return p;
        }

        private static Button PrimaryButton(string text, int w) => StyledButton(text, w, UiTheme.Navy, Color.White);
        private static Button DangerButton(string text, int w) => StyledButton(text, w, Color.FromArgb(190, 18, 60), Color.White);
        private static Button NeutralButton(string text, int w) => StyledButton(text, w, UiTheme.SurfaceAlt, UiTheme.Text);

        private static Button StyledButton(string text, int w, Color back, Color fore)
        {
            var b = new Button { Text = text, Width = w, Height = 34, Margin = new Padding(0, 18, 6, 0),
                BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
            b.FlatAppearance.BorderColor = back == UiTheme.SurfaceAlt ? UiTheme.Border : back;
            b.FlatAppearance.BorderSize = 1;
            return b;
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
                if (_windowFixed != null) _windowFixed.Text = w.Label;

                var fin = await _dash.GetFinanceSummaryAsync(w.From, w.To);
                _lblIncome.Text = "GHS " + fin.Income.ToString("N2");
                _lblExpenses.Text = "GHS " + fin.Expenses.ToString("N2");
                _lblFund.Text = "GHS " + fin.Fund.ToString("N2");
                _lblFund.ForeColor = fin.Fund < 0 ? Color.FromArgb(190, 18, 60) : UiTheme.Success;

                var list = await _repo.GetByRangeAsync(w.From, w.To, null);
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("Date"); dt.Columns.Add("Name"); dt.Columns.Add("Category");
                dt.Columns.Add("Amount", typeof(decimal)); dt.Columns.Add("Payee"); dt.Columns.Add("Payer"); dt.Columns.Add("Description");
                foreach (var e in list)
                    dt.Rows.Add(e.Id, e.Date.ToString("dd MMM yyyy"), e.Name, e.Category, e.Amount, e.Payee, e.Payer, e.Description);
                _grid.DataSource = dt;
                if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
                if (_grid.Columns.Contains("Amount"))
                {
                    _grid.Columns["Amount"].HeaderText = "Amount (GHS)";
                    _grid.Columns["Amount"].DefaultCellStyle.Format = "#,##0.00";
                    _grid.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                _total.Text = $"Window: {w.Label}     •     {list.Count} expense(s)     •     Total: GHS {list.Sum(x => x.Amount):N2}";
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
