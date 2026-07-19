using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
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
        private bool CanManageExpenses => AuthService.CanWrite("Finance.Expense.Manage");

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
            var entryHost = new Panel { Dock = DockStyle.Top, Height = 208, BackColor = UiTheme.Page, Padding = new Padding(22, 2, 22, 12) };
            var entryCard = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(16, 10, 16, 12) };
            entryCard.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, entryCard.ClientRectangle, UiTheme.Border, ButtonBorderStyle.Solid);

            _name = NewTextBox();
            _category = new ComboBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9.75F) };
            _amount = NewTextBox();
            _date = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Short };
            _payee = NewTextBox();
            _description = NewTextBox();

            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2, BackColor = UiTheme.Surface };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            fields.Controls.Add(CaptionedFill("Name", _name), 0, 0);
            fields.Controls.Add(CaptionedFill("Category", _category), 1, 0);
            fields.Controls.Add(CaptionedFill("Amount (GHS)", _amount), 2, 0);
            fields.Controls.Add(CaptionedFill("Date", _date), 3, 0);
            fields.Controls.Add(CaptionedFill("Payee", _payee), 4, 0);

            var descCell = CaptionedFill("Description", _description);
            fields.Controls.Add(descCell, 0, 1);
            fields.SetColumnSpan(descCell, 3);

            _btnRecord = PrimaryButton("Record", 110); _btnRecord.Click += async (s, e) => await SaveAsync();
            var clear = NeutralButton("Clear", 80); clear.Click += (s, e) => ClearEntry();
            _btnDelete = DangerButton("Delete", 90); _btnDelete.Enabled = false; _btnDelete.Click += async (s, e) => await DeleteAsync();
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = UiTheme.Surface, Padding = new Padding(0, 14, 2, 0) };
            buttons.Controls.Add(_btnDelete); buttons.Controls.Add(clear); buttons.Controls.Add(_btnRecord);
            fields.Controls.Add(buttons, 3, 1);
            fields.SetColumnSpan(buttons, 2);

            var entryTitle = new Label { Text = "Record Expense", Dock = DockStyle.Top, Height = 24, ForeColor = UiTheme.Navy, Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold) };
            entryCard.Controls.Add(fields);       // Fill first
            entryCard.Controls.Add(entryTitle);   // Top last → docks above the fields
            entryHost.Controls.Add(entryCard);
            ApplyWriteAccess();

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

        private static TextBox NewTextBox() =>
            new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9.75F) };

        // A label-over-input cell that fills its grid column width.
        private static Control CaptionedFill(string caption, Control input)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = new Padding(0, 0, 12, 8) };
            input.Dock = DockStyle.Top;
            p.Controls.Add(input);   // added first → sits below
            p.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top, Height = 17, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold) });
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
            if (!AuthService.RequireWriteAccess("Finance.Expense.Manage", _editingId > 0 ? "Update expense" : "Record expense")) return;
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
            _amount.Text = Convert.ToDecimal(r.Cells["Amount"].Value).ToString("0.00");
            _payee.Text = r.Cells["Payee"].Value?.ToString();
            _description.Text = r.Cells["Description"].Value?.ToString();
            DateTime dt; if (DateTime.TryParse(r.Cells["Date"].Value?.ToString(), out dt)) _date.Value = dt;
            _btnRecord.Text = CanManageExpenses ? "Update" : "Read only";
            _btnDelete.Enabled = CanManageExpenses;
        }

        private async Task DeleteAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.Expense.Manage", "Delete expense")) return;
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
            _btnRecord.Text = CanManageExpenses ? "Record" : "Read only";
            _btnDelete.Enabled = false;
        }

        private void ApplyWriteAccess()
        {
            bool canWrite = CanManageExpenses;
            _name.ReadOnly = !canWrite;
            _amount.ReadOnly = !canWrite;
            _payee.ReadOnly = !canWrite;
            _description.ReadOnly = !canWrite;
            _category.Enabled = canWrite;
            _date.Enabled = canWrite;
            _btnRecord.Enabled = canWrite;
            _btnRecord.Text = canWrite ? "Record" : "Read only";
            _btnDelete.Enabled = false;
        }
    }
}
