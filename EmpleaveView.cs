using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EmpleaveView : Form
    {
        private readonly LeaveService _leaveService;

        public EmpleaveView()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var repository = new LeaveRepository(AppConfig.ConnectionString);
            _leaveService = new LeaveService(repository);

            UiTheme.Apply(this);
            UiTheme.StyleDataGrid(data);
        }

        private async void EmpleaveView_Load(object sender, EventArgs e) { await LoadData(); }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                string statusFilter = cmb_cd.SelectedIndex > 0 ? cmb_cd.Text : "ALL";
                DataTable dt = await _leaveService.GetLeaveRequestsTableAsync(statusFilter);
                
                data.DataSource = dt;
                ApplyConditionalFormatting();
            }
            catch (Exception ex) { UIHelper.ShowError("Error loading leave data: " + ex.Message, "Leave View"); }
        }

        private void ApplyConditionalFormatting()
        {
            foreach (DataGridViewRow row in data.Rows)
            {
                if (row.Cells["STATUS"].Value == null) continue;
                string status = row.Cells["STATUS"].Value.ToString().ToUpperInvariant();
                
                if (status == "APPROVED")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(220, 252, 231);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                }
                else if (status == "PENDING")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 249, 195);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(113, 63, 18);
                }
                else if (status == "DENIED" || status == "REJECTED")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 228, 230);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(159, 18, 57);
                }
            }
        }

        private async void txtID_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtID.Text)) await LoadData();
            else
            {
                // Simple client-side search for ID on the existing table
                if (data.DataSource is DataTable dt)
                {
                    dt.DefaultView.RowFilter = $"ID LIKE '%{txtID.Text.Trim().Replace("'", "''")}%'";
                }
            }
        }

        private async void cmb_cd_SelectedIndexChanged(object sender, EventArgs e) { await LoadData(); }

        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = data.Columns[e.ColumnIndex].Name;

            if (columnName == "VIEW")
            {
                var rowData = new Dictionary<string, string>();
                foreach (DataGridViewCell cell in data.Rows[e.RowIndex].Cells)
                {
                    if (cell.OwningColumn != null) rowData[cell.OwningColumn.Name] = cell.Value?.ToString() ?? "";
                }

                var detailForm = new frmLeaveDetails(rowData);
                if (detailForm.ShowDialog() == DialogResult.OK)
                {
                    _ = LoadData();
                }
            }
        }

        private void gunaPictureBox1_Click(object sender, EventArgs e) { this.Close(); }
        private void gunaButton2_Click(object sender, EventArgs e) { _ = LoadData(); }
    }
}
