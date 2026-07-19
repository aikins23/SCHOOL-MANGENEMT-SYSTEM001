using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmSendNotice : Form
    {
        private readonly NoticeRepository _repo = new NoticeRepository(AppConfig.ConnectionString);
        private readonly NoticeService _service;
        private bool _isBusy;

        public frmSendNotice()
        {
            InitializeComponent();
            _service = new NoticeService(_repo);
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("frmSendNotice", this)) return;
            btnSend.Visible = AuthService.CanWrite("Admin.Notice.Send");
            btnDelete.Visible = AuthService.CanWrite("Admin.Notice.Send");

            btnSend.Click += btnSend_Click;
            btnClear.Click += btnClear_Click;
            btnClose.Click += btnClose_Click;
            btnSignOut.Click += btnSignOut_Click;
            btnDashboardSidebar.Click += (s, e) => Close();
            btnNoticeSidebar.Click += async (s, e) => await LoadHistoryAsync();
            btnStudents.Click += (s, e) => OpenChild(new frmStdView());
            btnPromotion.Click += (s, e) => OpenChild(new frmStudentPromotion());
            btnMainMenu.Click += (s, e) => Close();
        }

        private async void frmSendNotice_Load(object sender, EventArgs e)
        {
            // Populate ComboBoxes
            cmbTarget.Items.Clear();
            cmbTarget.Items.AddRange(new object[] { "All", "Parents", "Employees", "Specific Class" });

            cmbChannel.Items.Clear();
            cmbChannel.Items.AddRange(new object[] { "Both (SMS & Email)", "SMS Only", "Email Only" });

            cmbClass.Items.Clear();
            cmbClass.Items.Add("-- Not Applicable --");

            var dynamicClasses = await new kingdom_Preparatory_School_Management_System.Data.SchoolInfoRepository(AppConfig.ConnectionString).GetClassNamesAsync();
            if (dynamicClasses.Count == 0) dynamicClasses.AddRange(AppConfig.ClassNames);
            foreach (var cls in dynamicClasses) cmbClass.Items.Add(cls);

            if (cmbTarget.Items.Count > 0) cmbTarget.SelectedIndex = 0;
            if (cmbChannel.Items.Count > 0) cmbChannel.SelectedIndex = 0;
            if (cmbClass.Items.Count > 0) cmbClass.SelectedIndex = 0;

            await LoadHistoryAsync();
        }

        private async void LoadHistory()
        {
            await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                btnRefresh.Enabled = false;
                dgvHistory.DataSource = await _repo.GetAsTableAsync();
                if (dgvHistory.Columns.Contains("NoticeID"))
                {
                    dgvHistory.Columns["NoticeID"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading notice history: " + ex.Message, "Notice History", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            if (!AuthService.RequireWriteAccess("Admin.Notice.Send", "Send Notice"))
                return;

            if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                MessageBox.Show("Please fill in both Title and Message.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbTarget.Text == "Specific Class" && (cmbClass.SelectedIndex <= 0 || string.IsNullOrWhiteSpace(cmbClass.Text)))
            {
                MessageBox.Show("Please select a class for a class-specific notice.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetNoticeBusy(true, "Sending notice. Please wait...");
                var notice = new Notice
                {
                    Title = txtTitle.Text.Trim(),
                    Message = txtMessage.Text.Trim(),
                    Target = cmbTarget.Text,
                    TargetClass = cmbTarget.Text == "Specific Class" ? cmbClass.Text : "",
                    Channel = cmbChannel.Text,
                    SentBy = AuthService.CurrentUser?.Username ?? "System",
                    SentDate = DateTime.Now,
                    Status = "Saved"
                };

                var result = await Task.Run(async () => await _service.SendNoticeAsync(notice));
                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Send Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show(result.Message, "Notice Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save notice: " + ex.Message, "Send Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetNoticeBusy(false, "Ready");
            }
        }

        private void SetNoticeBusy(bool busy, string status = null)
        {
            _isBusy = busy;
            UseWaitCursor = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            btnSend.Enabled = !busy;
            btnClear.Enabled = !busy;
            btnDelete.Enabled = !busy;
            btnRefresh.Enabled = !busy;
            cmbTarget.Enabled = !busy;
            cmbChannel.Enabled = !busy;
            cmbClass.Enabled = !busy;
            txtTitle.Enabled = !busy;
            txtMessage.Enabled = !busy;
            if (!string.IsNullOrWhiteSpace(status))
                lblStatus.Text = status;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            txtTitle.Clear();
            txtMessage.Clear();
            cmbTarget.SelectedIndex = 0;
            cmbChannel.SelectedIndex = 0;
            cmbClass.SelectedIndex = 0;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSignOut_Click(object sender, EventArgs e)
        {
            FormManager.SignOut();
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (!AuthService.RequireWriteAccess("Admin.Notice.Send", "Delete Notice History"))
                return;

            if (dgvHistory.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a notice to delete.", "Delete Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvHistory.SelectedRows[0];
            if (!dgvHistory.Columns.Contains("NoticeID") || row.Cells["NoticeID"].Value == null ||
                !int.TryParse(row.Cells["NoticeID"].Value.ToString(), out var noticeId))
            {
                MessageBox.Show("Selected notice does not have a valid database ID.", "Delete Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Delete the selected notice?", "Delete Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            btnDelete.Enabled = false;
            try
            {
                var deleted = await _repo.DeleteAsync(noticeId);
                MessageBox.Show(deleted ? "Notice deleted successfully." : "Notice was not found.", "Delete Notice", MessageBoxButtons.OK,
                    deleted ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not delete notice: " + ex.Message, "Delete Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDelete.Enabled = true;
            }
        }

        private void OpenChild(Form form)
        {
            try
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Show();
                form.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open form: " + ex.Message, "Navigation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
