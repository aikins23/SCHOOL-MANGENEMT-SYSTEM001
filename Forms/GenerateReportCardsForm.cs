using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class GenerateReportCardsForm : Form
    {
        private readonly ReportCardManager _reportCardManager;
        private List<string> _selectedStudentIds;
        private bool _isGenerating;

        public GenerateReportCardsForm(ReportCardManager reportCardManager)
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("GenerateReportCardsForm", this)) return;
            _reportCardManager = reportCardManager;
            UiTheme.Apply(this);
            btnGenerate.Click += btnGenerate_Click;
            Shown += async (s, e) => await LoadFiltersAsync();
        }

        private async Task LoadFiltersAsync()
        {
            btnGenerate.Enabled = false;
            lblStatus.Text = "Loading academic filters...";

            try
            {
                var sessionService = new AcademicSessionService();
                var terms = await sessionService.GetTermsAsync();
                var years = await sessionService.GetYearsAsync();
                var activeTerm = await sessionService.GetActiveTermAsync();

                cmbTerm.Items.Clear();
                foreach (var name in terms
                    .Select(t => t.TermName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    cmbTerm.Items.Add(name);
                }
                cmbTerm.Items.Add("All Terms");
                cmbTerm.SelectedItem = activeTerm?.TermName;
                if (cmbTerm.SelectedIndex < 0 && cmbTerm.Items.Count > 0)
                    cmbTerm.SelectedIndex = 0;

                cmbYear.Items.Clear();
                foreach (var year in years
                    .Select(y => y.DisplayName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    cmbYear.Items.Add(year);
                }
                cmbYear.Items.Add("All Years");
                cmbYear.SelectedItem = activeTerm?.AcademicYearName;
                if (cmbYear.SelectedIndex < 0 && cmbYear.Items.Count > 0)
                    cmbYear.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Falling back to static report card filters: " + ex.Message);
                LoadFallbackFilters();
            }

            await LoadClassesAsync();
            lblStatus.Text = "Ready";
            btnGenerate.Enabled = true;
        }

        private void LoadFallbackFilters()
        {
            cmbTerm.Items.Clear();
            cmbTerm.Items.AddRange(new object[] { "TERM 1", "TERM 2", "TERM 3", "All Terms" });
            cmbTerm.SelectedIndex = 2;

            cmbYear.Items.Clear();
            cmbYear.Items.AddRange(new object[] { "2024/2025", "2025/2026", "All Years" });
            cmbYear.SelectedIndex = 0;
        }

        private async Task LoadClassesAsync()
        {
            try
            {
                var classes = await GetAllClassesAsync();
                cmbClass.Items.Clear();
                cmbClass.Items.Add("All Classes");
                foreach (var cls in classes)
                    cmbClass.Items.Add(cls);
                cmbClass.SelectedIndex = 0;

                await ApplyTeacherScopeAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error loading classes: {ex.Message}", "Generate Report Cards");
            }
        }

        /// <summary>
        /// Teachers may only generate report cards for their own class. Locks the
        /// class dropdown to their assigned class and disables "Print All" so they
        /// can't bypass the filter.
        /// </summary>
        private async Task ApplyTeacherScopeAsync()
        {
            if (!AuthService.IsTeacher) return;
            string myClass = await AuthService.GetCurrentTeacherClassAsync();
            if (string.IsNullOrEmpty(myClass)) return;
            int idx = cmbClass.Items.IndexOf(myClass);
            if (idx >= 0)
            {
                cmbClass.SelectedIndex = idx;
                cmbClass.Enabled = false;
            }
            if (chkPrintAll != null)
            {
                chkPrintAll.Checked = false;
                chkPrintAll.Enabled = false;
            }
        }

        private async Task<List<string>> GetAllClassesAsync()
        {
            var classes = new List<string>();
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await connection.OpenAsync();
                var query = "SELECT DISTINCT ClassID FROM Students WHERE 1=1";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                if (tenant)
                    query += TenantContext.FilterClauseSql();
                query += " ORDER BY ClassID";
                using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, connection))
                {
                    if (tenant)
                        TenantContext.AddSchoolParameter(cmd);
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
            if (_isGenerating) return;

            try
            {
                // For Teacher: always route through the filtered path so the
                // teacher-scope cmbClass selection is honoured. Belt-and-braces —
                // we also disable the chkPrintAll checkbox in ApplyTeacherScopeAsync,
                // but defending here too in case the UI state is bypassed.
                if (chkPrintAll.Checked && !AuthService.IsTeacher)
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

                var selectedTerm = cmbTerm.Text;
                var selectedYear = cmbYear.Text;
                if (selectedTerm == "All Terms" || selectedYear == "All Years")
                {
                    UIHelper.ShowWarning("Select a specific academic term and year before generating report cards.", "Generate Report Cards");
                    return;
                }

                // Ask user: Print, Save, or Email
                var dialog = new frmReportCardOutputAction(_selectedStudentIds.Count);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _isGenerating = true;
                    if (dialog.SelectedAction == OutputDialogAction.Print)
                    {
                        await UIHelper.RunBusyAsync(
                            this,
                            lblStatus,
                            "Preparing report cards for printing...",
                            new Control[] { btnGenerate, cmbClass, cmbTerm, cmbYear, chkPrintAll },
                            GenerateAndPrintAsync);
                    }
                    else if (dialog.SelectedAction == OutputDialogAction.Save)
                    {
                        var folderDialog = new FolderBrowserDialog();
                        if (folderDialog.ShowDialog() == DialogResult.OK)
                        {
                            await UIHelper.RunBusyAsync(
                                this,
                                lblStatus,
                                "Generating report card PDFs...",
                                new Control[] { btnGenerate, cmbClass, cmbTerm, cmbYear, chkPrintAll },
                                async () => await GenerateAndSaveAsync(folderDialog.SelectedPath));
                        }
                    }
                    else if (dialog.SelectedAction == OutputDialogAction.Email)
                    {
                        if (!AppConfig.Email.IsConfigured)
                        {
                            UIHelper.ShowError("Email service is not configured. Please set up email settings first.", "Configuration Error");
                            return;
                        }

                        if (ConfirmationHelper.ConfirmBulkOperation("email report cards to parents", _selectedStudentIds.Count))
                        {
                            await UIHelper.RunBusyAsync(
                                this,
                                lblStatus,
                                "Emailing report cards...",
                                new Control[] { btnGenerate, cmbClass, cmbTerm, cmbYear, chkPrintAll },
                                GenerateAndEmailAsync);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error: {ex.Message}", "Generate Report Cards");
            }
            finally
            {
                _isGenerating = false;
            }
        }

        private async Task GenerateAndEmailAsync()
        {
            try
            {
                int successCount = 0;
                int failureCount = 0;

                string term = cmbTerm.Text;
                string year = cmbYear.Text;
                if (term == "All Terms" || year == "All Years")
                {
                    UIHelper.ShowWarning("Please select a specific Term and Year for email distribution.");
                    return;
                }

                foreach (var studentId in _selectedStudentIds)
                {
                    lblStatus.Text = $"Emailing report card {successCount + failureCount + 1} of {_selectedStudentIds.Count}...";
                    try
                    {
                        // 1. Fetch student data to get email
                        var studentRepo = new StudentRepository(AppConfig.ConnectionString);
                        var student = await studentRepo.GetByIdAsync(studentId);

                        if (student == null || string.IsNullOrWhiteSpace(student.GuardianEmail))
                        {
                            failureCount++;
                            LoggerHelper.LogWarning($"Skipping {studentId} - No guardian email found.");
                            continue;
                        }

                        // 2. Generate PDF
                        var data = await _reportCardManager.GetReportCardDataAsync(studentId, term, year);
                        var pdfGenerator = new ReportCardPDFGenerator();
                        byte[] pdfBytes = await pdfGenerator.GeneratePDFAsync(data);

                        // 3. Email PDF
                        var result = await NotificationService.SendReportCardAsync(
                            student.FullName, student.GuardianEmail, term, year, pdfBytes);

                        if (result.Success) successCount++;
                        else failureCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        LoggerHelper.LogError($"Failed to email report for {studentId}", ex);
                    }
                }

                UIHelper.ShowInfo($"Email dispatch complete.\nSent: {successCount}\nFailed/No Email: {failureCount}", "Report Cards");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Bulk email failed: {ex.Message}", "Generate Report Cards");
            }
        }

        private async Task GenerateAndPrintAsync()
        {
            prgProgress.Visible = true;
            prgProgress.Maximum = _selectedStudentIds.Count;
            prgProgress.Value = 0;

            var term = cmbTerm.SelectedItem.ToString();
            var year = cmbYear.SelectedItem.ToString();
            var printer = new ReportCardPrinter();
            if (!printer.ShowPrintDialog(out string printerName))
            {
                prgProgress.Visible = false;
                lblStatus.Text = "Print cancelled.";
                return;
            }

            var action = new ReportCardOutputAction
            {
                Type = OutputType.Print,
                PrinterName = printerName
            };

            var savedFallbacks = new List<string>();
            for (int i = 0; i < _selectedStudentIds.Count; i++)
            {
                var studentId = _selectedStudentIds[i];
                lblStatus.Text = $"Generating report card {i + 1} of {_selectedStudentIds.Count}...";
                var fallbackPath = await Task.Run(async () =>
                    await _reportCardManager.GenerateAndOutputAsync(studentId, term, year, action));
                if (!string.IsNullOrWhiteSpace(fallbackPath))
                    savedFallbacks.Add(fallbackPath);
                prgProgress.Value = i + 1;
                lblStatus.Text = $"Printing {i + 1} of {_selectedStudentIds.Count}...";
            }

            if (savedFallbacks.Count == 0)
            {
                UIHelper.ShowSuccess($"Successfully sent {_selectedStudentIds.Count} report cards to the printer", "Generate Report Cards");
            }
            else
            {
                UIHelper.ShowInfo(
                    $"{savedFallbacks.Count} report card(s) were generated and saved as PDF files.\n\nWindows did not allow direct printing from the app on this computer, so open the PDFs and print them from the viewer.\n\nSaved in:\n{System.IO.Path.GetDirectoryName(savedFallbacks[0])}",
                    "Generate Report Cards");
            }
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

            await Task.Run(async () =>
                await _reportCardManager.GenerateBatchAsync(_selectedStudentIds, term, year, folderPath, progress));
            UIHelper.ShowSuccess($"Report cards saved to {folderPath}", "Generate Report Cards");
            this.Close();
        }

        private async Task<List<string>> GetAllStudentIdsAsync()
        {
            var students = new List<string>();
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await connection.OpenAsync();
                var query = "SELECT CAST(StudentID AS NVARCHAR(50)) AS StudentID FROM Students WHERE 1=1";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                if (tenant)
                    query += TenantContext.FilterClauseSql();
                query += " ORDER BY StudentID";
                using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, connection))
                {
                    if (tenant)
                        TenantContext.AddSchoolParameter(cmd);
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

            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await connection.OpenAsync();

                var query = "SELECT CAST(StudentID AS NVARCHAR(50)) AS StudentID FROM Students WHERE 1=1";
                var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                if (tenant)
                    query += TenantContext.FilterClauseSql();
                if (classFilter != "All Classes")
                    query += " AND ClassID = @ClassID";
                query += " ORDER BY StudentID";

                using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, connection))
                {
                    if (tenant)
                        TenantContext.AddSchoolParameter(cmd);
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
