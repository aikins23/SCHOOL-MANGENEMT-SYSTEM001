using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class GenerateReportCardsForm : Form
    {
        private readonly ReportCardManager _reportCardManager;
        private List<string> _selectedStudentIds;

        public GenerateReportCardsForm(ReportCardManager reportCardManager)
        {
            InitializeComponent();
            _reportCardManager = reportCardManager;
            UiTheme.Apply(this);
            LoadFilters();
        }

        private void LoadFilters()
        {
            // Load terms: TERM 1, TERM 2, TERM 3
            cmbTerm.Items.AddRange(new[] { "TERM 1", "TERM 2", "TERM 3", "All Terms" });
            cmbTerm.SelectedIndex = 2;  // Default to TERM 3

            // Load years
            cmbYear.Items.AddRange(new[] { "2024/2025", "2025/2026", "All Years" });
            cmbYear.SelectedIndex = 0;

            // Load classes from database
            LoadClassesAsync();
        }

        private async void LoadClassesAsync()
        {
            try
            {
                var classes = await GetAllClassesAsync();
                cmbClass.Items.Clear();
                cmbClass.Items.Add("All Classes");
                foreach (var cls in classes)
                    cmbClass.Items.Add(cls);
                cmbClass.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error loading classes: {ex.Message}", "Generate Report Cards");
            }
        }

        private async Task<List<string>> GetAllClassesAsync()
        {
            var classes = new List<string>();
            using (var connection = new System.Data.OleDb.OleDbConnection(AppConfig.ConnectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT DISTINCT ClassID FROM Student ORDER BY ClassID";
                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            classes.Add(reader["ClassID"].ToString());
                    }
                }
            }
            return classes;
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                if (chkPrintAll.Checked)
                {
                    _selectedStudentIds = await GetAllStudentIdsAsync();
                }
                else
                {
                    _selectedStudentIds = await GetFilteredStudentIdsAsync();
                }

                if (_selectedStudentIds.Count == 0)
                {
                    UIHelper.ShowWarning("No students found matching the selected criteria.", "Generate Report Cards");
                    return;
                }

                // Ask user: Print or Save
                var result = MessageBox.Show(
                    $"Generate report cards for {_selectedStudentIds.Count} students?\n\nPrint to Printer or Save to Folder?",
                    "Generate Report Cards",
                    MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes)
                {
                    // Print all
                    await GenerateAndPrintAsync();
                }
                else if (result == DialogResult.No)
                {
                    // Save to folder
                    var folderDialog = new FolderBrowserDialog();
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        await GenerateAndSaveAsync(folderDialog.SelectedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error: {ex.Message}", "Generate Report Cards");
            }
        }

        private async Task GenerateAndPrintAsync()
        {
            prgProgress.Visible = true;
            prgProgress.Maximum = _selectedStudentIds.Count;
            prgProgress.Value = 0;

            var term = cmbTerm.SelectedItem.ToString();
            var year = cmbYear.SelectedItem.ToString();

            var progress = new Progress<BatchProgressReport>(report =>
            {
                prgProgress.Value = report.Current;
                lblStatus.Text = $"Generating {report.Current} of {report.Total}...";
            });

            await _reportCardManager.GenerateBatchAsync(_selectedStudentIds, term, year, "", progress);
            UIHelper.ShowSuccess($"Successfully generated {_selectedStudentIds.Count} report cards", "Generate Report Cards");
            this.Close();
        }

        private async Task GenerateAndSaveAsync(string folderPath)
        {
            prgProgress.Visible = true;
            prgProgress.Maximum = _selectedStudentIds.Count;
            prgProgress.Value = 0;

            var term = cmbTerm.SelectedItem.ToString();
            var year = cmbYear.SelectedItem.ToString();

            var progress = new Progress<BatchProgressReport>(report =>
            {
                prgProgress.Value = report.Current;
                lblStatus.Text = $"Saving {report.Current} of {report.Total}...";
            });

            await _reportCardManager.GenerateBatchAsync(_selectedStudentIds, term, year, folderPath, progress);
            UIHelper.ShowSuccess($"Report cards saved to {folderPath}", "Generate Report Cards");
            this.Close();
        }

        private async Task<List<string>> GetAllStudentIdsAsync()
        {
            var students = new List<string>();
            using (var connection = new System.Data.OleDb.OleDbConnection(AppConfig.ConnectionString))
            {
                await connection.OpenAsync();
                const string query = "SELECT StudentID FROM Student ORDER BY StudentID";
                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            students.Add(reader["StudentID"].ToString());
                    }
                }
            }
            return students;
        }

        private async Task<List<string>> GetFilteredStudentIdsAsync()
        {
            var students = new List<string>();
            var classFilter = cmbClass.SelectedItem.ToString();

            using (var connection = new System.Data.OleDb.OleDbConnection(AppConfig.ConnectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT StudentID FROM Student WHERE 1=1";
                if (classFilter != "All Classes")
                    query += " AND ClassID = @ClassID";

                using (var cmd = new System.Data.OleDb.OleDbCommand(query, connection))
                {
                    if (classFilter != "All Classes")
                        cmd.Parameters.AddWithValue("@ClassID", classFilter);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            students.Add(reader["StudentID"].ToString());
                    }
                }
            }

            return students;
        }
    }
}
