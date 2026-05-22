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
    public partial class Form1 : Form
    {
        private readonly StudentService _studentService;

        public Form1()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepo = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepo);

            UiTheme.Apply(this);
            UiTheme.StyleDataGrid(data);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadRolledOutStudents();
        }

        private async System.Threading.Tasks.Task LoadRolledOutStudents()
        {
            try
            {
                DataTable dt = await _studentService.GetRolledOutStudentsTableAsync();
                data.DataSource = dt;
                
                if (data.Columns["STUDENT PIC"] is DataGridViewImageColumn imageCol)
                {
                    imageCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading student history: " + ex.Message, "Student History");
            }
        }

        private async void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = data.Columns[e.ColumnIndex].Name;

            if (columnName == "VIEW")
            {
                string id = data.Rows[e.RowIndex].Cells["ID"].Value.ToString();
                DataTable result = await _studentService.GetRolledOutStudentsTableAsync();
                result.DefaultView.RowFilter = $"ID = '{id}'";
                
                frmStdDetails detailViewForm = new frmStdDetails(result.DefaultView.ToTable());
                detailViewForm.ShowDialog();
            }
        }

        private void gunaPictureBox6_Click(object sender, EventArgs e) { this.Close(); }
    }
}
    

