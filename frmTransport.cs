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
    /// <summary>
    /// Transport configuration: buses and routes (fee + payment term). Director/Admin/Headmaster.
    /// </summary>
    public class frmTransport : Form
    {
        private readonly TransportRepository _repo = new TransportRepository(AppConfig.ConnectionString);
        private DataGridView _busGrid, _routeGrid;
        private TextBox _busSearch;
        private Label _status;

        public frmTransport()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmTransport", this)) return;
            Load += async (s, e) => await InitAsync();
        }

        private void BuildUi()
        {
            Text = "Transport";
            Size = new Size(920, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Transport",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildBusesTab());
            tabs.TabPages.Add(BuildRoutesTab());
            _status = new Label { Dock = DockStyle.Bottom, Height = 24, ForeColor = AppConfig.Colors.MutedTextColor, Padding = new Padding(12, 0, 0, 0) };

            Controls.Add(tabs);
            Controls.Add(_status);
            Controls.Add(title);
        }

        private TabPage BuildBusesTab()
        {
            var tab = new TabPage("Buses") { BackColor = Color.White };
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
            _busSearch = new TextBox { Dock = DockStyle.Left, Width = 280, Font = new Font("Segoe UI", 10F) };
            _busSearch.TextChanged += async (s, e) => await LoadBusesAsync();
            var add = new Button { Dock = DockStyle.Right, Width = 90, Text = "Add", FlatStyle = FlatStyle.Flat };
            var edit = new Button { Dock = DockStyle.Right, Width = 90, Text = "Edit", FlatStyle = FlatStyle.Flat };
            var del = new Button { Dock = DockStyle.Right, Width = 90, Text = "Delete", FlatStyle = FlatStyle.Flat };
            add.Click += async (s, e) => { var b = BusDialog(null); if (b != null) { await _repo.AddBusAsync(b); await LoadBusesAsync(); } };
            edit.Click += async (s, e) => await EditBusAsync();
            del.Click += async (s, e) => await DeleteBusAsync();
            bar.Controls.Add(_busSearch); bar.Controls.Add(del); bar.Controls.Add(edit); bar.Controls.Add(add);

            _busGrid = NewGrid();
            tab.Controls.Add(_busGrid);
            tab.Controls.Add(bar);
            return tab;
        }

        private TabPage BuildRoutesTab()
        {
            var tab = new TabPage("Routes") { BackColor = Color.White };
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
            var add = new Button { Dock = DockStyle.Right, Width = 90, Text = "Add", FlatStyle = FlatStyle.Flat };
            var edit = new Button { Dock = DockStyle.Right, Width = 90, Text = "Edit", FlatStyle = FlatStyle.Flat };
            var del = new Button { Dock = DockStyle.Right, Width = 90, Text = "Delete", FlatStyle = FlatStyle.Flat };
            add.Click += async (s, e) => { var r = await RouteDialogAsync(null); if (r != null) { await _repo.AddRouteAsync(r); await LoadRoutesAsync(); } };
            edit.Click += async (s, e) => await EditRouteAsync();
            del.Click += async (s, e) => await DeleteRouteAsync();
            bar.Controls.Add(del); bar.Controls.Add(edit); bar.Controls.Add(add);

            _routeGrid = NewGrid();
            tab.Controls.Add(_routeGrid);
            tab.Controls.Add(bar);
            return tab;
        }

        private static DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
        };

        private async Task InitAsync()
        {
            try { await _repo.EnsureTablesAsync(); await LoadBusesAsync(); await LoadRoutesAsync(); }
            catch (Exception ex) { UIHelper.ShowError("Could not load transport: " + ex.Message, "Transport"); }
        }

        private async Task LoadBusesAsync()
        {
            var buses = await _repo.GetBusesAsync(_busSearch.Text);
            _busGrid.DataSource = buses.Select(b => new { b.BusId, b.Label, Reg = b.RegNumber, Driver = b.DriverName, Contact = b.DriverContact, b.Capacity }).ToList();
            if (_busGrid.Columns.Contains("BusId")) _busGrid.Columns["BusId"].Visible = false;
            _status.Text = buses.Count + " bus(es).";
        }

        private async Task LoadRoutesAsync()
        {
            var routes = await _repo.GetRoutesAsync();
            _routeGrid.DataSource = routes.Select(r => new { r.RouteId, Route = r.RouteName, Fee = r.Fee.ToString("N2"), Term = r.PaymentTerm, Bus = r.BusLabel, r.Stops }).ToList();
            if (_routeGrid.Columns.Contains("RouteId")) _routeGrid.Columns["RouteId"].Visible = false;
        }

        private Bus SelectedBus()
        {
            if (_busGrid.CurrentRow?.Cells["BusId"].Value == null) return null;
            return new Bus
            {
                BusId = Convert.ToInt32(_busGrid.CurrentRow.Cells["BusId"].Value),
                Label = _busGrid.CurrentRow.Cells["Label"].Value?.ToString() ?? "",
                RegNumber = _busGrid.CurrentRow.Cells["Reg"].Value?.ToString() ?? "",
                DriverName = _busGrid.CurrentRow.Cells["Driver"].Value?.ToString() ?? "",
                DriverContact = _busGrid.CurrentRow.Cells["Contact"].Value?.ToString() ?? "",
                Capacity = Convert.ToInt32(_busGrid.CurrentRow.Cells["Capacity"].Value ?? 0)
            };
        }

        private async Task EditBusAsync()
        {
            var existing = SelectedBus();
            if (existing == null) { UIHelper.ShowWarning("Select a bus to edit.", "Transport"); return; }
            var edited = BusDialog(existing);
            if (edited == null) return;
            edited.BusId = existing.BusId;
            await _repo.UpdateBusAsync(edited);
            await LoadBusesAsync();
        }

        private async Task DeleteBusAsync()
        {
            var b = SelectedBus();
            if (b == null) { UIHelper.ShowWarning("Select a bus to delete.", "Transport"); return; }
            if (UIHelper.ShowConfirmation($"Delete bus \"{b.Label}\"?", "Transport") != DialogResult.Yes) return;
            if (!await _repo.DeleteBusAsync(b.BusId)) { UIHelper.ShowWarning("Cannot delete: the bus is assigned to a route.", "Transport"); return; }
            await LoadBusesAsync();
        }

        private async Task EditRouteAsync()
        {
            if (_routeGrid.CurrentRow?.Cells["RouteId"].Value == null) { UIHelper.ShowWarning("Select a route to edit.", "Transport"); return; }
            int id = Convert.ToInt32(_routeGrid.CurrentRow.Cells["RouteId"].Value);
            var existing = (await _repo.GetRoutesAsync()).FirstOrDefault(x => x.RouteId == id);
            if (existing == null) return;
            var edited = await RouteDialogAsync(existing);
            if (edited == null) return;
            edited.RouteId = id;
            await _repo.UpdateRouteAsync(edited);
            await LoadRoutesAsync();
        }

        private async Task DeleteRouteAsync()
        {
            if (_routeGrid.CurrentRow?.Cells["RouteId"].Value == null) { UIHelper.ShowWarning("Select a route to delete.", "Transport"); return; }
            int id = Convert.ToInt32(_routeGrid.CurrentRow.Cells["RouteId"].Value);
            string name = _routeGrid.CurrentRow.Cells["Route"].Value?.ToString() ?? "";
            if (UIHelper.ShowConfirmation($"Delete route \"{name}\"?", "Transport") != DialogResult.Yes) return;
            await _repo.DeleteRouteAsync(id);
            await LoadRoutesAsync();
        }

        // ── Dialogs ──────────────────────────────────────────────────────────────
        private Bus BusDialog(Bus existing)
        {
            var dlg = new Form { Text = existing == null ? "Add Bus" : "Edit Bus", Size = new Size(380, 340), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            TextBox Mk(string label, int y, string val) { dlg.Controls.Add(new Label { Left = 14, Top = y + 3, Width = 100, Text = label }); var t = new TextBox { Left = 120, Top = y, Width = 220, Text = val ?? "" }; dlg.Controls.Add(t); return t; }
            var t1 = Mk("Label", 16, existing?.Label);
            var t2 = Mk("Reg number", 52, existing?.RegNumber);
            var t3 = Mk("Driver", 88, existing?.DriverName);
            var t4 = Mk("Driver contact", 124, existing?.DriverContact);
            dlg.Controls.Add(new Label { Left = 14, Top = 163, Width = 100, Text = "Capacity" });
            var nc = new NumericUpDown { Left = 120, Top = 160, Width = 80, Minimum = 0, Maximum = 200, Value = existing == null ? 0 : Math.Min(200, existing.Capacity) };
            dlg.Controls.Add(nc);
            var t5 = Mk("Notes", 196, existing?.Notes);
            var ok = new Button { Left = 120, Top = 240, Width = 100, Height = 32, Text = "Save", DialogResult = DialogResult.OK, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Left = 232, Top = 240, Width = 100, Height = 32, Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            dlg.Controls.Add(ok); dlg.Controls.Add(cancel); dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return null;
            if (string.IsNullOrWhiteSpace(t1.Text)) { UIHelper.ShowWarning("Label is required.", "Transport"); return null; }
            return new Bus { Label = t1.Text.Trim(), RegNumber = t2.Text.Trim(), DriverName = t3.Text.Trim(), DriverContact = t4.Text.Trim(), Capacity = (int)nc.Value, Notes = t5.Text.Trim() };
        }

        private async Task<BusRoute> RouteDialogAsync(BusRoute existing)
        {
            var buses = await _repo.GetBusesAsync(null);
            var dlg = new Form { Text = existing == null ? "Add Route" : "Edit Route", Size = new Size(400, 360), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            TextBox Mk(string label, int y, string val) { dlg.Controls.Add(new Label { Left = 14, Top = y + 3, Width = 100, Text = label }); var t = new TextBox { Left = 120, Top = y, Width = 240, Text = val ?? "" }; dlg.Controls.Add(t); return t; }
            var name = Mk("Route name", 16, existing?.RouteName);
            dlg.Controls.Add(new Label { Left = 14, Top = 55, Width = 100, Text = "Fee" });
            var fee = new NumericUpDown { Left = 120, Top = 52, Width = 120, DecimalPlaces = 2, Minimum = 0, Maximum = 100000, Value = existing == null ? 0 : Math.Min(100000, existing.Fee) };
            dlg.Controls.Add(fee);
            dlg.Controls.Add(new Label { Left = 14, Top = 91, Width = 100, Text = "Payment term" });
            var term = new ComboBox { Left = 120, Top = 88, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            term.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly" });
            term.SelectedItem = existing?.PaymentTerm ?? "Monthly"; if (term.SelectedIndex < 0) term.SelectedIndex = 2;
            dlg.Controls.Add(term);
            dlg.Controls.Add(new Label { Left = 14, Top = 127, Width = 100, Text = "Bus" });
            var bus = new ComboBox { Left = 120, Top = 124, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            bus.Items.Add("(none)");
            foreach (var b in buses) bus.Items.Add(b);
            bus.SelectedIndex = 0;
            if (existing?.BusId != null) { var match = buses.FirstOrDefault(x => x.BusId == existing.BusId.Value); if (match != null) bus.SelectedItem = match; }
            var stops = Mk("Stops", 160, existing?.Stops);
            var notes = Mk("Notes", 196, existing?.Notes);
            var ok = new Button { Left = 120, Top = 250, Width = 100, Height = 32, Text = "Save", DialogResult = DialogResult.OK, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Left = 232, Top = 250, Width = 100, Height = 32, Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            dlg.Controls.Add(ok); dlg.Controls.Add(cancel); dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return null;
            if (string.IsNullOrWhiteSpace(name.Text)) { UIHelper.ShowWarning("Route name is required.", "Transport"); return null; }
            int? busId = bus.SelectedItem is Bus sb ? (int?)sb.BusId : null;
            return new BusRoute { RouteName = name.Text.Trim(), Fee = fee.Value, PaymentTerm = term.SelectedItem.ToString(), BusId = busId, Stops = stops.Text.Trim(), Notes = notes.Text.Trim() };
        }
    }
}
