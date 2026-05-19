using System;
using System.ComponentModel;
using System.Data.OleDb;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class Form1 : Form
    {
        kum Aikins = new kum();

        public Form1()
        {
            InitializeComponent();
            UiTheme.Apply(this);
          

        }

        private void APPROVE_Click(object sender, EventArgs e)
        {
            ///downContextMenuStrip.Show(approveButton, new Point(0, approveButton.Height));
        }

        private void DOWN_Opening(object sender, CancelEventArgs e)
        {
            // You can handle any special logic when the context menu is opening here, if needed.
        }

        private void gunaPictureBox6_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Aikins.query = @"
    SELECT 
StudentID AS 'ID',
        CONCAT('KPS', RIGHT('000' + CAST(StudentID AS VARCHAR), 3)) AS 'UNIQUE ID',
        FirstName AS 'FIRST NAME',LastName AS 'LAST NAME',
        DOB AS 'DATE OF BIRTH', 
        Gender AS 'GENDER',
        Email AS 'EMAIL',
        ClassID AS 'CLASS ID',
        HomeTown AS 'HOME TOWN',
        Residence AS 'RESIDENCE',
        Allegies AS 'ALLERGIES',
        EmergencyConatct AS 'EMERGENCY CONTACT',
        GuidanceName AS 'GUIDANCE NAME',
        GuidianceEmail AS 'GUIDANCE EMAIL', -- Corrected column name
        Guidiance_Location AS 'GUIDANCE LOCATION',
        admission_date AS 'ADMISSION DATE',
        Std_pic AS 'STUDENT PIC'
    FROM 
        Rolled_Out_Students";

                Aikins.cmd = new OleDbCommand(Aikins.query, Aikins.con);
                Aikins.adp = new OleDbDataAdapter(Aikins.cmd);
                Aikins.ds = new DataSet();
                Aikins.adp.Fill(Aikins.ds, "Students");

                // Check if the column exists before removing it
                if (Aikins.ds.Tables["Students"].Columns.Contains("STUDENT ID"))
                    Aikins.ds.Tables["Students"].Columns.Remove("STUDENT ID");

                data.DataSource = Aikins.ds.Tables["Students"];

                // Set the image layout mode to Zoom for the "STUDENT PIC" column
                DataGridViewImageColumn imageCol = (DataGridViewImageColumn)data.Columns["STUDENT PIC"];
                imageCol.ImageLayout = DataGridViewImageCellLayout.Zoom;

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string columnName = data.Columns[e.ColumnIndex].Name;

                // Check which button was clicked based on the column header text
                switch (columnName)
                {
                    case "DEL":
                        MessageBox.Show("Delete button clicked in row " + e.RowIndex);
                        break;
                    case "VIEW":
                        int viewIndex = e.RowIndex;
                        DataGridViewRow selectedRow = data.Rows[viewIndex];

                        // Assuming the column containing the ID is named "ID"
                        int id = Convert.ToInt32(selectedRow.Cells["ID"].Value);

                        // Call the method to retrieve data from the database using the ID
                        DataTable result = GetDataFromDatabase(id);

                        // Pass the DataTable to your detail form
                        frmStdDetails detailViewForm = new frmStdDetails(result);
                        detailViewForm.ShowDialog();
                       
                        break;


                    default:
                        break;
                }
            }
        }

        private DataTable GetDataFromDatabase(int id)
        {
            DataTable dataTable = new DataTable();

            using (OleDbConnection connection = new OleDbConnection(Aikins.constr))
            {
                string query = "SELECT * FROM Rolled_Out_Students WHERE StudentID = ?";

                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("?", id);

                    connection.Open();

                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    adapter.Fill(dataTable);

                    // Now you can use the dataTable containing the results
                }
            }

            return dataTable;
        }


    }
}
    

