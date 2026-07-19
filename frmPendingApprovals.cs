using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
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
        private static readonly TimeSpan ApprovalSlowWarningAfter = TimeSpan.FromSeconds(20);

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

            SetBusy(true, $"Approving admission for {d.FullName}...");
            try
            {
                string bursar = AuthService.CurrentUser?.DisplayName;
                if (string.IsNullOrWhiteSpace(bursar))
                {
                    bursar = AuthService.CurrentUser?.Username ?? "Accountant";
                }

                var approvalTask = _service.ApproveAsync(d.DraftID, bursar);
                if (await Task.WhenAny(approvalTask, Task.Delay(ApprovalSlowWarningAfter)) != approvalTask)
                {
                    _statusLabel.Text = "Approval is still running. SQL Server is taking longer than expected...";
                    LoggerHelper.LogWarning($"Admission approval for draft {d.DraftID} is still waiting after {ApprovalSlowWarningAfter.TotalSeconds:N0} seconds.");
                }

                var res = await approvalTask;
                if (!res.Ok)
                {
                    _statusLabel.Text = "Approval failed.";
                    UIHelper.ShowError(res.Message, "Approval");
                    return;
                }

                await LoadPendingAsync();
                _statusLabel.Text = $"Approved {res.Student.FullName}.";

                SetBusy(false, $"Approved {res.Student.FullName}. Sending admission SMS in the background...");
                _ = SendAdmissionNotificationsBestEffortAsync(res.Student, d);
                UIHelper.ShowInfo(
                    "Admission approved.\n\nThe emergency-contact SMS is being processed in the background. Check the status bar or SMS log/outbox for delivery status.",
                    "Approval");

                if (UIHelper.ShowConfirmation("Admission approved. Preview receipts now?", "Approval") == DialogResult.Yes)
                {
                    try
                    {
                        PreviewReceipts(res.Student, d, bursar);
                    }
                    catch (Exception previewEx)
                    {
                        LoggerHelper.LogError("Admission approved, but receipt preview failed", previewEx);
                        UIHelper.ShowError("Admission was approved, but the receipt preview could not open: " + previewEx.Message, "Receipts");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Admission approval failed for draft {d.DraftID}", ex);
                _statusLabel.Text = "Approval failed.";
                UIHelper.ShowError("Approval failed: " + ex.Message, "Approval");
            }
            finally
            {
                SetBusy(false, _statusLabel.Text);
            }
        }

        private async Task SendAdmissionNotificationsBestEffortAsync(Student student, DraftAdmission d)
        {
            var smsResult = await SendAdmissionSmsAsync(student, d);
            LoggerHelper.LogInfo($"Admission SMS result for student {student?.StudentID ?? "unknown"}: {smsResult.Status} {smsResult.Message}");
            UpdateStatusSafe($"Approved {student?.FullName ?? "student"}. {smsResult.Status}");
            _ = SendAdmissionEmailBestEffortAsync(student, d);
        }

        private void UpdateStatusSafe(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateStatusSafe(text)));
                return;
            }

            _statusLabel.Text = text;
        }

        private void SetBusy(bool busy, string status)
        {
            UseWaitCursor = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            _grid.Enabled = !busy;
            _approveBtn.Enabled = !busy;
            _rejectBtn.Enabled = !busy;
            _refreshBtn.Enabled = !busy;
            _statusLabel.Text = status;
        }

        private static async Task<(bool Success, string Status, string Message)> SendAdmissionSmsAsync(Student student, DraftAdmission d)
        {
            try
            {
                if (student == null)
                {
                    return (false, "SMS skipped.", "Admission SMS was skipped because the approved student record was empty.");
                }

                if (string.IsNullOrWhiteSpace(student.EmergencyContact))
                {
                    return (false, "SMS skipped.", "Admission SMS was skipped because the student emergency contact number is empty.");
                }

                var normalized = PhoneNumberGh.NormalizeGh(student.EmergencyContact);
                if (normalized == null)
                {
                    return (false,
                        "SMS skipped: invalid phone.",
                        $"Admission SMS was skipped because '{student.EmergencyContact}' is not a valid Ghana mobile number.");
                }

                var result = await SmsService.SendStudentAdmissionAsync(
                    student.EmergencyContact, student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal);

                if (result.Success && SmsService.IsLive)
                {
                    return (true, "SMS sent.", result.Message);
                }

                if (result.Success)
                {
                    return (false,
                        "SMS logged only.",
                        "Admission SMS was recorded in the local SMS log/outbox, but live SMS sending is disabled or the SMS API key is not configured.");
                }

                return (false, "SMS failed.", result.Message);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Admission SMS failed for student {student?.StudentID}", ex);
                return (false, "SMS failed.", "Admission SMS failed: " + ex.Message);
            }
        }

        private static async Task SendAdmissionEmailBestEffortAsync(Student student, DraftAdmission d)
        {
            try
            {
                if (student == null || string.IsNullOrWhiteSpace(student.GuardianEmail)) return;

                await NotificationService.SendEmailAsync(student.GuardianEmail,
                    $"Admission Confirmation - {Common.SchoolProfile.DisplayName}",
                    SmsService.BuildStudentAdmissionMessage(student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal),
                    NotificationService.NotificationType.GeneralAnnouncement);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Admission email failed for student {student?.StudentID}", ex);
            }
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
                Common.PrintBranding.DrawGraphicsFooter(g, b);
            }
            navy.Dispose();
        }
    }
}
