using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmTransportPayments : Form
    {
        private readonly TransportRepository _transport = new TransportRepository(AppConfig.ConnectionString);
        private readonly StudentService _students =
            new StudentService(new StudentRepository(AppConfig.ConnectionString), new FeeRepository(AppConfig.ConnectionString));

        private TextBox _txtId;
        private Label _lblName, _lblRoute, _lblTerm, _lblPeriod, _lblFee, _lblPaid, _lblBalance;
        private TextBox _txtAmount;
        private Button _btnRecord;
        private DataGridView _history, _arrears;
        private CheckBox _unpaidOnly;

        private int _studentId;
        private BusRoute _route;
        private (string Key, DateTime Start, DateTime End) _period;
        private decimal _paid;

        public frmTransportPayments()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmTransportPayments", this)) return;
            Load += async (s, e) =>
            {
                try { await _transport.EnsureTablesAsync(); await RefreshArrearsAsync(); }
                catch (Exception ex) { LoggerHelper.LogError("Transport payments init", ex); }
            };
        }

        private void BuildUi()
        {
            Text = "Transport Payments";
            Size = new Size(940, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 46, Text = "  Transport Payments",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildRecordTab());
            tabs.TabPages.Add(BuildArrearsTab());

            Controls.Add(tabs);
            Controls.Add(title);
        }

        private TabPage BuildRecordTab()
        {
            var tab = new TabPage("Record Payment") { BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12) };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            top.Controls.Add(new Label { Text = "Student ID:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _txtId = new TextBox { Width = 160, Margin = new Padding(0, 6, 8, 0) };
            var find = new Button { Text = "Find", Width = 90, Height = 28, Margin = new Padding(0, 5, 0, 0) };
            find.Click += async (s, e) => await LookupAsync();
            top.Controls.Add(_txtId);
            top.Controls.Add(find);

            var info = new TableLayoutPanel { Dock = DockStyle.Top, Height = 200, ColumnCount = 2, BackColor = Color.White, Padding = new Padding(12) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _lblName = AddInfoRow(info, "Name");
            _lblRoute = AddInfoRow(info, "Route");
            _lblTerm = AddInfoRow(info, "Term");
            _lblPeriod = AddInfoRow(info, "Current period");
            _lblFee = AddInfoRow(info, "Fee");
            _lblPaid = AddInfoRow(info, "Paid this period");
            _lblBalance = AddInfoRow(info, "Balance");

            var pay = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
            pay.Controls.Add(new Label { Text = "Amount:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _txtAmount = new TextBox { Width = 140, Margin = new Padding(0, 6, 8, 0) };
            _btnRecord = new Button { Text = "Record Payment", Width = 150, Height = 30, Enabled = false, BackColor = AppConfig.Colors.PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnRecord.Click += async (s, e) => await RecordAsync();
            pay.Controls.Add(_txtAmount);
            pay.Controls.Add(_btnRecord);

            _history = MakeGrid();

            tab.Controls.Add(_history);
            tab.Controls.Add(pay);
            tab.Controls.Add(info);
            tab.Controls.Add(top);
            return tab;
        }

        private TabPage BuildArrearsTab()
        {
            var tab = new TabPage("Arrears") { BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12) };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _unpaidOnly = new CheckBox { Text = "Unpaid only", Checked = true, AutoSize = true, Margin = new Padding(0, 9, 12, 0) };
            _unpaidOnly.CheckedChanged += async (s, e) => await RefreshArrearsAsync();
            var refresh = new Button { Text = "Refresh", Width = 90, Height = 28, Margin = new Padding(0, 5, 0, 0) };
            refresh.Click += async (s, e) => await RefreshArrearsAsync();
            bar.Controls.Add(_unpaidOnly);
            bar.Controls.Add(refresh);

            _arrears = MakeGrid();
            tab.Controls.Add(_arrears);
            tab.Controls.Add(bar);
            return tab;
        }

        private static DataGridView MakeGrid() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
        };

        private static Label AddInfoRow(TableLayoutPanel t, string caption)
        {
            int row = t.RowCount;
            t.RowCount = row + 1;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            t.Controls.Add(new Label { Text = caption + ":", AutoSize = true, ForeColor = AppConfig.Colors.MutedTextColor, Margin = new Padding(0, 4, 0, 0) }, 0, row);
            var val = new Label { Text = "-", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Margin = new Padding(0, 4, 0, 0) };
            t.Controls.Add(val, 1, row);
            return val;
        }

        private async Task LookupAsync()
        {
            try
            {
                string idText = StudentId.Parse(_txtId.Text);
                if (!int.TryParse(idText, out _studentId) || _studentId <= 0)
                { UIHelper.ShowError("Enter a valid student ID.", "Transport"); return; }

                var student = await _students.GetStudentAsync(idText);
                _route = await _transport.GetStudentRouteAsync(_studentId);
                if (student == null) { UIHelper.ShowError("Student not found.", "Transport"); ResetInfo(); return; }
                if (_route == null)
                {
                    _lblName.Text = (student.FirstName + " " + student.LastName).Trim();
                    ConfirmationHelper.ShowInfo("This student is not assigned to a bus route.", "Transport");
                    ResetInfo(keepName: true);
                    return;
                }

                _period = TransportPeriod.Current(_route.PaymentTerm, DateTime.Today);
                _paid = await _transport.GetPaidForPeriodAsync(_studentId, _period.Key);
                decimal balance = Math.Max(0m, _route.Fee - _paid);

                _lblName.Text = (student.FirstName + " " + student.LastName).Trim();
                _lblRoute.Text = _route.RouteName;
                _lblTerm.Text = _route.PaymentTerm;
                _lblPeriod.Text = _period.Key + "  (" + _period.Start.ToString("dd MMM") + " - " + _period.End.ToString("dd MMM") + ")";
                _lblFee.Text = "GHS " + _route.Fee.ToString("N2");
                _lblPaid.Text = "GHS " + _paid.ToString("N2");
                _lblBalance.Text = "GHS " + balance.ToString("N2");
                _btnRecord.Enabled = true;

                _history.DataSource = await _transport.GetStudentTransportHistoryAsync(_studentId);
            }
            catch (Exception ex) { LoggerHelper.LogError("Transport lookup", ex); UIHelper.ShowError("Lookup failed: " + ex.Message, "Transport"); }
        }

        private async Task RecordAsync()
        {
            try
            {
                if (_route == null || _studentId <= 0) return;
                if (!decimal.TryParse(_txtAmount.Text, out decimal amount) || amount <= 0m)
                { UIHelper.ShowError("Enter a payment amount greater than zero.", "Transport"); return; }

                string cashier = AuthService.CurrentUser?.DisplayName ?? "";
                bool ok = await _transport.AddTransportPaymentAsync(_studentId, _route.RouteId, _period.Key,
                    _period.Start, _period.End, amount, DateTime.Now, cashier, "");
                if (!ok) { UIHelper.ShowError("Could not save the transport payment.", "Transport"); return; }

                _txtAmount.Clear();
                ConfirmationHelper.ShowInfo("Transport payment recorded.", "Transport");
                await LookupAsync();          // refresh balance + history
                await RefreshArrearsAsync();
            }
            catch (Exception ex) { LoggerHelper.LogError("Transport record", ex); UIHelper.ShowError("Save failed: " + ex.Message, "Transport"); }
        }

        private async Task RefreshArrearsAsync()
        {
            try
            {
                var list = await _transport.GetArrearsAsync(DateTime.Today, includeDaily: true);
                var dt = new DataTable();
                dt.Columns.Add("Student"); dt.Columns.Add("Route"); dt.Columns.Add("Term");
                dt.Columns.Add("Period"); dt.Columns.Add("Fee", typeof(decimal));
                dt.Columns.Add("Paid", typeof(decimal)); dt.Columns.Add("Balance", typeof(decimal));
                dt.Columns.Add("Present days", typeof(int));
                foreach (var a in list)
                {
                    if (_unpaidOnly != null && _unpaidOnly.Checked && a.Balance <= 0m) continue;
                    dt.Rows.Add(StudentId.Display(a.StudentID) + " - " + a.StudentName, a.RouteName, a.Term,
                        a.Period, a.Fee, a.Paid, a.Balance, a.PresentDays);
                }
                if (_arrears != null) _arrears.DataSource = dt;
            }
            catch (Exception ex) { LoggerHelper.LogError("Transport arrears", ex); }
        }

        private void ResetInfo(bool keepName = false)
        {
            if (!keepName) _lblName.Text = "-";
            _lblRoute.Text = _lblTerm.Text = _lblPeriod.Text = _lblFee.Text = _lblPaid.Text = _lblBalance.Text = "-";
            _btnRecord.Enabled = false;
            _history.DataSource = null;
        }
    }
}
