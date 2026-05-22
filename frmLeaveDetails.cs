using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmLeaveDetails : Form
    {
        private readonly LeaveService _leaveService;

        public frmLeaveDetails()
        {
            InitializeComponent();
            var repository = new LeaveRepository(AppConfig.ConnectionString);
            _leaveService = new LeaveService(repository);
            UiTheme.Apply(this);
        }

        public frmLeaveDetails(Dictionary<string, string> rowData) : this()
        {
            SetData(rowData);
        }

        private void SetData(Dictionary<string, string> rowData)
        {
            txtEmployeeId.Text = rowData["ID"];
            txtName.Text = rowData["NAME"];
            txtdepartment.Text = rowData["DEPARTMENT"];
            txtposition.Text = rowData["POSITION"];

            if (rowData["LEAVE OPTION"] == "With Pay") rdpay.Checked = true;
            else if (rowData["LEAVE OPTION"] == "Without Pay") rdwPay.Checked = true;

            switch (rowData["REASONS"])
            {
                case "Sick": rdoSick.Checked = true; break;
                case "Vacation": rdoVacation.Checked = true; break;
                case "Funeral": rdoFuneral.Checked = true; break;
                case "Paternity": rdoPaternity.Checked = true; break;
                case "Maternity": rdoMaternity.Checked = true; break;
                case "Accident On Duty": rdoAcidentOnDuty.Checked = true; break;
            }

            if (DateTime.TryParse(rowData["START DATE"], out DateTime startDate)) dtpdatestart.Value = startDate;
            if (DateTime.TryParse(rowData["END DATE"], out DateTime endDate)) dtpenddate.Value = endDate;
            status.Text = rowData["STATUS"];
        }

        private async void gunaButton3_Click(object sender, EventArgs e)
        {
            try
            {
                var request = new LeaveRequest
                {
                    EmployeeID = txtEmployeeId.Text.Trim(),
                    EmployeeName = txtName.Text.Trim(),
                    Department = txtdepartment.Text.Trim(),
                    Position = txtposition.Text.Trim(),
                    LeaveOption = rdpay.Checked ? "With Pay" : "Without Pay",
                    Reason = GetSelectedReason(),
                    StartDate = dtpdatestart.Value.Date,
                    EndDate = dtpenddate.Value.Date,
                    Status = status.Text.Trim()
                };

                var (success, message) = await _leaveService.UpdateLeaveStatusAsync(request, request.Status);
                if (success)
                {
                    UIHelper.ShowSuccess("Leave application updated successfully.", "Leave Details");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else UIHelper.ShowError(message, "Leave Details");
            }
            catch (Exception ex) { UIHelper.ShowError(ex.Message, "Leave Details"); }
        }

        private string GetSelectedReason()
        {
            if (rdoSick.Checked) return "Sick";
            if (rdoVacation.Checked) return "Vacation";
            if (rdoFuneral.Checked) return "Funeral";
            if (rdoPaternity.Checked) return "Paternity";
            if (rdoMaternity.Checked) return "Maternity";
            if (rdoAcidentOnDuty.Checked) return "Accident On Duty";
            return "Other";
        }

        private void gunaPictureBox2_Click(object sender, EventArgs e) { this.Close(); }
    }
}
