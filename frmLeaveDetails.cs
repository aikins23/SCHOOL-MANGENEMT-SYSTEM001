using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmLeaveDetails : Form
    {
        kum Aikins = new kum();

        // Default constructor
        public frmLeaveDetails()
        {
            InitializeComponent();
            UiTheme.Apply(this);
        }

        // Constructor that accepts a dictionary of data
        public frmLeaveDetails(Dictionary<string, string> rowData)
        {
            InitializeComponent();
            UiTheme.Apply(this);
            SetData(rowData);
        }

        // Method to set data in the form controls
        private void SetData(Dictionary<string, string> rowData)
        {
            txtEmployeeId.Text = rowData["ID"];
            txtName.Text = rowData["NAME"];
            txtdepartment.Text = rowData["DEPARTMENT"];
            txtposition.Text = rowData["POSITION"];

            if (rowData["LEAVE OPTION"] == "With Pay")
            {
                rdpay.Checked = true;
            }
            else if (rowData["LEAVE OPTION"] == "Without Pay")
            {
                rdwPay.Checked = true;
            }

            switch (rowData["REASONS"])
            {
                case "Sick":
                    rdoSick.Checked = true;
                    break;
                case "Vacation":
                    rdoVacation.Checked = true;
                    break;
                case "Funeral":
                    rdoFuneral.Checked = true;
                    break;
                case "Paternity":
                    rdoPaternity.Checked = true;
                    break;
                case "Maternity":
                    rdoMaternity.Checked = true;
                    break;
                case "Accident On Duty":
                    rdoAcidentOnDuty.Checked = true;
                    break;
            }

            if (DateTime.TryParse(rowData["START DATE"], out DateTime startDate))
            {
                dtpdatestart.Value = startDate;
            }

            if (DateTime.TryParse(rowData["END DATE"], out DateTime endDate))
            {
                dtpenddate.Value = endDate;
            }

            status.Text = rowData["STATUS"];
        }

        private void gunaButton3_Click(object sender, EventArgs e)
        {
            try
            {
                string leaveFormat = "";
                string leaveApplied = "";

                if (rdwPay.Checked)
                {
                    leaveFormat = "Without Pay";
                }
                else if (rdpay.Checked)
                {
                    leaveFormat = "With Pay";
                }

                if (rdoSick.Checked)
                {
                    leaveApplied = "Sick";
                }
                else if (rdoVacation.Checked)
                {
                    leaveApplied = "Vacation";
                }
                else if (rdoFuneral.Checked)
                {
                    leaveApplied = "Funeral";
                }
                else if (rdoPaternity.Checked)
                {
                    leaveApplied = "Paternity";
                }
                else if (rdoMaternity.Checked)
                {
                    leaveApplied = "Maternity";
                }
                else if (rdoAcidentOnDuty.Checked)
                {
                    leaveApplied = "Accident On Duty";
                }

                Aikins.con = new OleDbConnection(Aikins.constr);
                Aikins.query = "UPDATE[emp_leave] SET[name] = ?, [department] = ?, [position] = ?, [Leave_op] = ?, [Reasons] = ?, [Start_Date] = ?, [End_Date] = ?, [status] = ? WHERE[employmentID] = ? ";

using (OleDbCommand cmd = new OleDbCommand(Aikins.query, Aikins.con))
                {
                    cmd.Parameters.AddWithValue("name", txtName.Text);
                    cmd.Parameters.AddWithValue("department", txtdepartment.Text);
                    cmd.Parameters.AddWithValue("position", txtposition.Text);
                    cmd.Parameters.AddWithValue("LeaveOp", leaveFormat);
                    cmd.Parameters.AddWithValue("Reasons", leaveApplied);
                    cmd.Parameters.AddWithValue("StartDate", dtpdatestart.Value);
                    cmd.Parameters.AddWithValue("EndDate", dtpenddate.Value);
                    cmd.Parameters.AddWithValue("status", status.Text);
                    cmd.Parameters.AddWithValue("EmploymentID", txtEmployeeId.Text);


                    Aikins.con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("LEAVE APPLICATION UPDATE WAS SUCCESSFUL.");
                    }
                    else
                    {
                        MessageBox.Show("LEAVE APPLICATION UPDATE WAS UNSUCCESSFUL.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                if (Aikins.con != null && Aikins.con.State == ConnectionState.Open)
                {
                    Aikins.con.Close();
                }
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Handle the event here if needed
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            // Handle the event here if needed
        }

        private void studentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmAddStd().Show();
        }

        private void employersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmEmployee().Show();
        }

        private void classToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new EXAMS().Show();
        }

        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new frmStdView().Show();
        }

        private void employersToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new frmEmployee().Show();
        }

        private void classToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new EXAMSVIEW().Show();
        }

        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmFessPayment().Show();
        }

        private void gunaPictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
