using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
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
    /// Bursar approval queue for pending admissions. Approving promotes the draft to a
    /// real student + payment records, prints two receipts (admission + school fees),
    /// and sends the admission SMS/email with the payment lines. Rejecting discards it.
    /// </summary>
    public class frmPendingApprovals : Form
    {
        private readonly DraftAdmissionService _service;
        private DataGridView _grid;
        private Button _approveBtn;
        private Button _rejectBtn;
        private Button _refreshBtn;
        private Label _statusLabel;
        private List<DraftAdmission> _pending = new List<DraftAdmission>();

        public frmPendingApprovals()
        {
            var fees = new FeeRepository(AppConfig.ConnectionString);
            _service = new DraftAdmissionService(
                new DraftAdmissionRepository(AppConfig.ConnectionString),
                new StudentService(new StudentRepository(AppConfig.ConnectionString), fees),
                fees);

            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmPendingApprovals", this)) return;
            Load += async (s, e) => await LoadPendingAsync();
        }

        private void BuildUi()
        {
            Text = "Pending Admission Approvals";
            Size = new Size(900, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Pending Admission Approvals",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 25, 112), TextAlign = ContentAlignment.MiddleLeft
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BackgroundColor = Color.White
            };

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White, Padding = new Padding(10) };
            _approveBtn = new Button { Text = "Approve", Width = 130, Height = 36, Dock = DockStyle.Right, BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _rejectBtn = new Button { Text = "Reject", Width = 110, Height = 36, Dock = DockStyle.Right, BackColor = Color.FromArgb(190, 18, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _refreshBtn = new Button { Text = "Refresh", Width = 100, Height = 36, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            _approveBtn.Click += async (s, e) => await ApproveSelectedAsync();
            _rejectBtn.Click += async (s, e) => await RejectSelectedAsync();
            _refreshBtn.Click += async (s, e) => await LoadPendingAsync();
            bar.Controls.Add(_approveBtn);
            bar.Controls.Add(_rejectBtn);
            bar.Controls.Add(_refreshBtn);

            _statusLabel = new Label { Dock = DockStyle.Bottom, Height = 24, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };

            Controls.Add(_grid);
            Controls.Add(_statusLabel);
            Controls.Add(bar);
            Controls.Add(title);
        }

        private async Task LoadPendingAsync()
        {
            try
            {
                _pending = (await _service.GetPendingAsync()).ToList();
                _grid.DataSource = _pending.Select(d => new
                {
                    d.DraftID,
                    Student = d.FullName,
                    Class = d.ClassID,
                    Admission = d.AdmissionFee.ToString("N2"),
                    SchoolPaid = d.SchoolFeePaid.ToString("N2"),
                    Term = d.TermTotal.ToString("N2"),
                    By = d.SubmittedBy,
                    Submitted = d.SubmittedDate.ToString("dd/MM/yyyy HH:mm")
                }).ToList();
                if (_grid.Columns.Contains("DraftID")) _grid.Columns["DraftID"].Visible = false;
                _statusLabel.Text = $"{_pending.Count} pending admission(s).";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load pending approvals: " + ex.Message, "Approvals");
            }
        }

        private DraftAdmission SelectedDraft()
        {
            if (_grid.CurrentRow == null) return null;
            var idCell = _grid.CurrentRow.Cells["DraftID"]?.Value;
            if (idCell == null) return null;
            int id = Convert.ToInt32(idCell);
            return _pending.FirstOrDefault(d => d.DraftID == id);
        }

        private async Task ApproveSelectedAsync()
        {
            var d = SelectedDraft();
            if (d == null) { UIHelper.ShowWarning("Select a pending admission first.", "Approvals"); return; }

            _approveBtn.Enabled = false;
            try
            {
                string bursar = AuthService.CurrentUser.DisplayName;
                var res = await _service.ApproveAsync(d.DraftID, bursar);
                if (!res.Ok) { UIHelper.ShowError(res.Message, "Approval"); return; }

                // Notifications with the payment lines (fire-and-forget).
                if (!string.IsNullOrWhiteSpace(res.Student.EmergencyContact))
                    _ = SmsService.SendStudentAdmissionAsync(res.Student.EmergencyContact, res.Student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal);
                if (!string.IsNullOrWhiteSpace(res.Student.GuardianEmail))
                    _ = NotificationService.SendEmailAsync(res.Student.GuardianEmail,
                        $"Admission Confirmation - {Common.SchoolProfile.DisplayName}",
                        SmsService.BuildStudentAdmissionMessage(res.Student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal),
                        NotificationService.NotificationType.GeneralAnnouncement);

                PreviewReceipts(res.Student, d, bursar);
                await LoadPendingAsync();
                UIHelper.ShowSuccess("Approved. SMS sent and receipts ready to print.", "Approval");
            }
            finally { _approveBtn.Enabled = true; }
        }

        private async Task RejectSelectedAsync()
        {
            var d = SelectedDraft();
            if (d == null) { UIHelper.ShowWarning("Select a pending admission first.", "Approvals"); return; }
            if (UIHelper.ShowConfirmation($"Discard the admission for {d.FullName}?", "Reject") != DialogResult.Yes) return;
            await _service.RejectAsync(d.DraftID);
            await LoadPendingAsync();
        }

        // ── Two-receipt print preview (admission + school fees) ──────────────────
        private void PreviewReceipts(Student student, DraftAdmission d, string bursar)
        {
            var receipts = new[]
            {
                new ReceiptLine("ADMISSION FEE", d.AdmissionFee, 0m),
                new ReceiptLine("SCHOOL FEES", d.SchoolFeePaid, Math.Max(0m, d.TermTotal - d.SchoolFeePaid))
            };
            int idx = 0;
            var doc = new PrintDocument();
            doc.PrintPage += (s, e) =>
            {
                DrawReceipt(e.Graphics, e.MarginBounds, student, receipts[idx], bursar);
                idx++;
                e.HasMorePages = idx < receipts.Length;
            };
            using (var preview = new PrintPreviewDialog { Document = doc, Width = 720, Height = 820 })
                preview.ShowDialog(this);
        }

        private sealed class ReceiptLine
        {
            public string Title; public decimal Paid; public decimal Balance;
            public ReceiptLine(string t, decimal p, decimal b) { Title = t; Paid = p; Balance = b; }
        }

        private static void DrawReceipt(Graphics g, Rectangle b, Student s, ReceiptLine r, string bursar)
        {
            var navy = new SolidBrush(Color.FromArgb(25, 25, 112));
            var black = Brushes.Black;
            using (var h1 = new Font("Segoe UI Semibold", 16F, FontStyle.Bold))
            using (var h2 = new Font("Segoe UI Semibold", 12F, FontStyle.Bold))
            using (var f = new Font("Segoe UI", 11F))
            {
                int x = b.Left + 10, y = b.Top + 10;
                g.DrawString("NYANSAPO SCHOOL ERP", h1, navy, x, y); y += 30;
                g.DrawString("P. O. BOX 7 AKIM ODA", f, black, x, y); y += 22;
                g.DrawString("OFFICIAL RECEIPT - " + r.Title, h2, navy, x, y); y += 30;
                g.DrawLine(Pens.Gray, x, y, b.Right - 10, y); y += 14;
                g.DrawString($"Student ID: {s.StudentID}", f, black, x, y); y += 22;
                g.DrawString($"Received from: {s.GuardianName}", f, black, x, y); y += 22;
                g.DrawString($"Student: {s.FullName}   Class: {s.ClassID}", f, black, x, y); y += 22;
                g.DrawString($"Being: {r.Title}", f, black, x, y); y += 22;
                g.DrawString($"Amount Paid: GHS {r.Paid:N2}", h2, navy, x, y); y += 26;
                g.DrawString($"Outstanding Balance: GHS {r.Balance:N2}", f, black, x, y); y += 22;
                g.DrawString($"Date: {DateTime.Today:dd/MM/yyyy}    Bursar: {bursar}", f, black, x, y); y += 40;
                g.DrawString("........................................", f, black, x, y); y += 18;
                g.DrawString("Signature / Stamp", f, black, x, y);
            }
            navy.Dispose();
        }
    }
}
