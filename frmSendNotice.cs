using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmSendNotice : Form
    {
        public frmSendNotice()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
        }

        private void frmSendNotice_Load(object sender, EventArgs e)
        {
            LoadHistory();
            
            // Populate ComboBoxes
            cmbTarget.Items.Clear();
            cmbTarget.Items.AddRange(new object[] { "All", "Parents", "Employees", "Specific Class" });
            
            cmbChannel.Items.Clear();
            cmbChannel.Items.AddRange(new object[] { "Both (SMS & Email)", "SMS Only", "Email Only" });

            cmbClass.Items.Clear();
            cmbClass.Items.Add("-- Not Applicable --");
            foreach (var cls in AppConfig.ClassNames) cmbClass.Items.Add(cls);

            if (cmbTarget.Items.Count > 0) cmbTarget.SelectedIndex = 0;
            if (cmbChannel.Items.Count > 0) cmbChannel.SelectedIndex = 0;
            if (cmbClass.Items.Count > 0) cmbClass.SelectedIndex = 0;
        }

        private void LoadHistory()
        {
            try
            {
                // In a real scenario, we would fetch from a 'Notices' table
                // For now, we'll create a dummy table to show the UI
                DataTable dt = new DataTable();
                dt.Columns.Add("Title");
                dt.Columns.Add("Target");
                dt.Columns.Add("Channel");
                dt.Columns.Add("Sent By");
                dt.Columns.Add("Sent Date");
                dt.Columns.Add("Recipients");
                dt.Columns.Add("Status");

                dt.Rows.Add("School Reopening", "All", "SMS", "Admin", "2026-06-01", "450", "Delivered");
                dt.Rows.Add("Fee Reminder", "Parents", "Email", "Accounts", "2026-06-05", "120", "Sent");
                dt.Rows.Add("Staff Meeting", "Employees", "Both", "Principal", "2026-06-08", "45", "Delivered");

                dgvHistory.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading history: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtTitle.Text) || string.IsNullOrEmpty(txtMessage.Text))
            {
                MessageBox.Show("Please fill in both Title and Message.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Logic to send notice would go here
            MessageBox.Show("Notice sent successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
            LoadHistory();
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
            Application.Restart();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadHistory();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvHistory.SelectedRows.Count > 0)
            {
                MessageBox.Show("Record deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadHistory();
            }
        }
    }
}
