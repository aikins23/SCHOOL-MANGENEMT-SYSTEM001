using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EmpleaveView : Form
    {
        kum Aikins = new kum();

        public EmpleaveView()
        {
            InitializeComponent();
            UiTheme.Apply(this);
            cmb_cd.SelectedIndexChanged += cmb_cd_SelectedIndexChanged; // Wire up event handler
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
            new frmEmpView().Show();
        }

        private void classToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new EXAMSVIEW().Show();
        }

        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmFessPayment().Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmAbout().Show();
        }

        private void EmpleaveView_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void gunaPictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void gunaButton2_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void txtID_TextChanged(object sender, EventArgs e)
        {
            LoadData(txtID.Text);
        }

        private void cmb_cd_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void LoadData(string employmentID = null, string department = null)
        {
            try
            {
                using (Aikins.con = new OleDbConnection(Aikins.constr))
                {
                    string query = @"
        SELECT 
            [SN] AS 'SN',
employmentID AS 'ID',
            CONCAT('EMP', RIGHT('0000' + CAST(employmentID AS VARCHAR), 4)) AS 'EMPLOYEES ID',
            [name] AS 'NAME',
            [department] AS 'DEPARTMENT',
            [position] AS 'POSITION',
            [Leave_op] AS 'LEAVE OPTION',
            [Reasons] AS 'REASONS',
            [Start_Date] AS 'START DATE',
            [End_Date] AS 'END DATE',
            [status] AS 'STATUS'
        FROM 
            [emp_leave]
        WHERE 1=1"; // Start with a dummy condition that's always true

                    if (!string.IsNullOrEmpty(employmentID))
                    {
                        query += " AND employmentID = @employmentID";
                    }

                    if (!string.IsNullOrEmpty(department))
                    {
                        query += " AND department = @department";
                    }

                    using (Aikins.cmd = new OleDbCommand(query, Aikins.con))
                    {
                        if (!string.IsNullOrEmpty(employmentID))
                        {
                            Aikins.cmd.Parameters.AddWithValue("@employmentID", employmentID);
                        }

                        if (!string.IsNullOrEmpty(department))
                        {
                            Aikins.cmd.Parameters.AddWithValue("@department", department);
                        }

                        using (Aikins.adp = new OleDbDataAdapter(Aikins.cmd))
                        {
                            Aikins.ds = new DataSet();
                            Aikins.adp.Fill(Aikins.ds, "emp_leave");
                            data.DataSource = Aikins.ds.Tables["emp_leave"];

                            // Set conditional formatting
                            foreach (DataGridViewRow row in data.Rows)
                            {
                                if (row.Cells["STATUS"].Value != null)
                                {
                                    string status = row.Cells["STATUS"].Value.ToString();
                                    if (status == "APPROVED")
                                    {
                                        row.DefaultCellStyle.BackColor = Color.Green;
                                        row.DefaultCellStyle.ForeColor = Color.White;
                                    }
                                    else if (status == "PENDING")
                                    {
                                        row.DefaultCellStyle.BackColor = Color.Black;
                                        row.DefaultCellStyle.ForeColor = Color.White;
                                    }
                                    else if (status == "DENIED")
                                    {
                                        row.DefaultCellStyle.BackColor = Color.Red;
                                        row.DefaultCellStyle.ForeColor = Color.White;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Get the name of the clicked column
            string columnName = data.Columns[e.ColumnIndex].Name;
            Console.WriteLine($"Column Clicked: {columnName}, Row: {e.RowIndex}");

            switch (columnName)
            {
                case "DEL":
                    MessageBox.Show("Delete button clicked in row " + e.RowIndex);
                    break;
                case "VIEW":
                    try
                    {
                        int viewIndex = e.RowIndex;
                        DataGridViewRow selectedRow = data.Rows[viewIndex];

                        // Create a dictionary to store the row data
                        var rowData = new Dictionary<string, string>
            {
                { "ID", selectedRow.Cells["ID"].Value.ToString() },
                { "NAME", selectedRow.Cells["NAME"].Value.ToString() },
                { "DEPARTMENT", selectedRow.Cells["DEPARTMENT"].Value.ToString() },
                { "POSITION", selectedRow.Cells["POSITION"].Value.ToString() },
                { "LEAVE OPTION", selectedRow.Cells["LEAVE OPTION"].Value.ToString() },
                { "REASONS", selectedRow.Cells["REASONS"].Value.ToString() },
                { "START DATE", selectedRow.Cells["START DATE"].Value.ToString() },
                { "END DATE", selectedRow.Cells["END DATE"].Value.ToString() },
                { "STATUS", selectedRow.Cells["STATUS"].Value.ToString() }
            };

                        // Open the target form and pass the data
                        frmLeaveDetails detailForm = new frmLeaveDetails(rowData);
                        detailForm.Show();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
            }

        }

        private void cmb_cd_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}
