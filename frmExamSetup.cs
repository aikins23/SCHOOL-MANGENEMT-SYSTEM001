using System;
using System.Collections.Generic;
using System.Data;

using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmExamSetup : Form
    {
        private bool CanManageExamSetup => AuthService.CanWrite("Settings.ExamSetup.Manage");
        private readonly IExamTypeRepository _examTypeRepo = new ExamTypeRepository();
        private List<ExamType> _examTypes = new List<ExamType>();
        private DataGridView _grid;
        private ComboBox _termCombo, _yearCombo, _examTypeCombo;
        private DateTimePicker _startPicker, _endPicker;
        private NumericUpDown _assessmentNumber;
        private NumericUpDown _classWeight;
        private NumericUpDown _examWeight;
        private Label _classWeightLabel;
        private Label _examWeightLabel;
        private int? _editingSetupId = null;
        private Label _status;
        private Label _assessmentPreview;
        private Button _saveBtn, _deleteBtn, _cancelBtn;

        public frmExamSetup()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmExamSetup", this)) return;
            Load += async (s, e) => await InitAsync();
        }

        private void BuildUi()
        {
            Text = "Exam Setup";
            Size = new Size(900, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Exam Period Setup",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            _grid = new DataGridView
            {
                Left = 16, Top = 60, Width = 410, Height = 440,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _grid.SelectionChanged += (s, e) => {
                _deleteBtn.Enabled = CanManageExamSetup && _grid.SelectedRows.Count > 0;
                if (_grid.SelectedRows.Count > 0) {
                    var row = _grid.SelectedRows[0];
                    _editingSetupId = Convert.ToInt32(row.Cells["SetupID"].Value);
                    _termCombo.Text = row.Cells["Term"].Value?.ToString();
                    _yearCombo.Text = row.Cells["Year"].Value?.ToString();
                    SelectExamType(row.Cells["ExamTypeId"].Value);
                    if (decimal.TryParse(row.Cells["AssessmentNumber"].Value?.ToString(), out decimal an) && an >= _assessmentNumber.Minimum)
                        _assessmentNumber.Value = Math.Min(an, _assessmentNumber.Maximum);
                    else
                        _assessmentNumber.Value = 1;
                    if (DateTime.TryParse(row.Cells["StartDate"].Value?.ToString(), out DateTime sd)) _startPicker.Value = sd;
                    if (DateTime.TryParse(row.Cells["EndDate"].Value?.ToString(), out DateTime ed)) _endPicker.Value = ed;
                    if (decimal.TryParse(row.Cells["ClassWeight"].Value?.ToString(), out decimal cw)) _classWeight.Value = cw;
                    if (decimal.TryParse(row.Cells["ExamWeight"].Value?.ToString(), out decimal ew)) _examWeight.Value = ew;
                    _saveBtn.Text = CanManageExamSetup ? "Update" : "Read only";
                } else {
                    _editingSetupId = null;
                    _saveBtn.Text = CanManageExamSetup ? "Save" : "Read only";
                }
            };

            var panel = new Panel { Left = 445, Top = 60, Width = 410, Height = 460 };

            int y = 0;
            panel.Controls.Add(new Label { Text = "Term", Left = 0, Top = y, Width = 150 });
            _termCombo = new ComboBox { Left = 160, Top = y, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _termCombo.Items.AddRange(new[] { "First Term", "Second Term", "Third Term" });
            _termCombo.SelectedIndex = 0;
            panel.Controls.Add(_termCombo);

            y += 40;
            panel.Controls.Add(new Label { Text = "Academic Year", Left = 0, Top = y, Width = 150 });
            _yearCombo = new ComboBox { Left = 160, Top = y, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _yearCombo.Items.AddRange(new[] { "2023/2024", "2024/2025", "2025/2026", "2026/2027" });
            _yearCombo.SelectedIndex = 1;
            panel.Controls.Add(_yearCombo);

            y += 40;
            panel.Controls.Add(new Label { Text = "Exam Type", Left = 0, Top = y, Width = 150 });
            _examTypeCombo = new ComboBox { Left = 160, Top = y, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _examTypeCombo.SelectedIndexChanged += (s, e) => UpdateAssessmentControls();
            panel.Controls.Add(_examTypeCombo);
            var btnTypes = new Button { Left = 326, Top = y - 2, Width = 64, Height = 28, Text = "Types", FlatStyle = FlatStyle.Flat };
            btnTypes.Click += async (s, e) =>
            {
                using (var form = new frmExamTypeManager())
                {
                    form.ShowDialog(this);
                }
                await LoadExamTypesAsync();
            };
            panel.Controls.Add(btnTypes);

            y += 40;
            panel.Controls.Add(new Label { Text = "Assessment No.", Left = 0, Top = y, Width = 150 });
            _assessmentNumber = new NumericUpDown { Left = 160, Top = y, Width = 80, Minimum = 1, Maximum = 20, Value = 1 };
            _assessmentNumber.ValueChanged += (s, e) => UpdateAssessmentControls();
            panel.Controls.Add(_assessmentNumber);
            _assessmentPreview = new Label { Left = 250, Top = y + 3, Width = 140, Height = 24, ForeColor = AppConfig.Colors.MutedTextColor };
            panel.Controls.Add(_assessmentPreview);

            y += 40;
            panel.Controls.Add(new Label { Text = "Start Date", Left = 0, Top = y, Width = 150 });
            _startPicker = new DateTimePicker { Left = 160, Top = y, Width = 230, Format = DateTimePickerFormat.Short };
            panel.Controls.Add(_startPicker);

            y += 40;
            panel.Controls.Add(new Label { Text = "End Date", Left = 0, Top = y, Width = 150 });
            _endPicker = new DateTimePicker { Left = 160, Top = y, Width = 230, Format = DateTimePickerFormat.Short };
            _endPicker.Value = DateTime.Today.AddDays(14);
            panel.Controls.Add(_endPicker);

            y += 40;
            _classWeightLabel = new Label { Text = "Class Weight (%)", Left = 0, Top = y, Width = 150 };
            panel.Controls.Add(_classWeightLabel);
            _classWeight = new NumericUpDown { Left = 160, Top = y, Width = 100, Minimum = 0, Maximum = 100, Value = 50 };
            panel.Controls.Add(_classWeight);

            y += 40;
            _examWeightLabel = new Label { Text = "Exam Weight (%)", Left = 0, Top = y, Width = 150 };
            panel.Controls.Add(_examWeightLabel);
            _examWeight = new NumericUpDown { Left = 160, Top = y, Width = 100, Minimum = 0, Maximum = 1000, Value = 50 };
            panel.Controls.Add(_examWeight);

            bool updating = false;
            _classWeight.ValueChanged += (s, e) => {
                if (updating || !IsSelectedEndOfTerm()) return;
                updating = true;
                _examWeight.Value = Math.Max(0, Math.Min(100, 100 - _classWeight.Value));
                updating = false;
            };
            _examWeight.ValueChanged += (s, e) => {
                if (updating || !IsSelectedEndOfTerm()) return;
                updating = true;
                _classWeight.Value = Math.Max(0, Math.Min(100, 100 - _examWeight.Value));
                updating = false;
            };

            var btnSubjects = new Button { Left = 280, Top = y - 40, Width = 110, Height = 65, Text = "Manage\nSubjects", FlatStyle = FlatStyle.Flat };
            btnSubjects.Click += (s, e) => new frmSubjects().ShowDialog();
            panel.Controls.Add(btnSubjects);

            y += 60;
            _saveBtn = new Button { Left = 160, Top = y, Width = 110, Height = 36, Text = "Save Setup", BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _deleteBtn = new Button { Left = 280, Top = y, Width = 110, Height = 36, Text = "Delete", BackColor = AppConfig.Colors.DangerColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };

            y += 45;
            _cancelBtn = new Button { Left = 160, Top = y, Width = 230, Height = 36, Text = "Close", FlatStyle = FlatStyle.Flat };

            _saveBtn.Click += async (s, e) => await SaveAsync();
            _deleteBtn.Click += async (s, e) => await DeleteAsync();
            _cancelBtn.Click += (s, e) => Close();

            panel.Controls.Add(_saveBtn);
            panel.Controls.Add(_deleteBtn);
            panel.Controls.Add(_cancelBtn);
            ApplyWriteAccess();

            _status = new Label { Left = 16, Top = 525, Width = 820, Height = 36, ForeColor = AppConfig.Colors.MutedTextColor };

            Controls.Add(title);
            Controls.Add(_grid);
            Controls.Add(panel);
            Controls.Add(_status);
        }

        private async Task InitAsync()
        {
            try
            {
                await _examTypeRepo.EnsureTableAsync();
                await _examTypeRepo.SeedSystemTypesAsync();
                await LoadExamTypesAsync();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading setups: " + ex.Message, "Exams");
            }
        }

        private async Task LoadGridAsync()
        {
            using (var conn = new Microsoft.Data.SqlClient.SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await conn.OpenAsync();
                var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT es.SetupID, es.Term, es.[Year], es.ExamTypeId,
                           COALESCE(es.AssessmentLabel, et.Name, 'End of Term') AS Assessment,
                           es.AssessmentNumber, COALESCE(et.Name, 'End of Term') AS ExamType,
                           es.IsPublishedToPortal AS Published,
                           es.StartDate, es.EndDate, es.ClassWeight, es.ExamWeight
                    FROM ExamSetups es
                    LEFT JOIN ExamTypes et ON et.ExamTypeId = es.ExamTypeId
                    ORDER BY es.SetupID DESC", conn);
                var dt = new DataTable();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
                _grid.DataSource = dt;
                _grid.Columns["SetupID"].Visible = false;
                _grid.Columns["ExamTypeId"].Visible = false;
                _grid.Columns["AssessmentNumber"].Visible = false;
                _grid.Columns["StartDate"].Visible = false;
                _grid.Columns["ClassWeight"].Visible = false;
                _grid.Columns["ExamWeight"].Visible = false;
            }
        }

        private async Task LoadExamTypesAsync()
        {
            _examTypes = await _examTypeRepo.GetActiveAsync();
            _examTypeCombo.DisplayMember = "Name";
            _examTypeCombo.ValueMember = "ExamTypeId";
            _examTypeCombo.DataSource = _examTypes;
            var endOfTerm = _examTypes.Find(t => string.Equals(t.Code, SystemExamTypeCodes.EndOfTerm, StringComparison.OrdinalIgnoreCase));
            if (endOfTerm != null) _examTypeCombo.SelectedValue = endOfTerm.ExamTypeId;
            UpdateAssessmentControls();
        }

        private void SelectExamType(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                var endOfTerm = _examTypes.Find(t => string.Equals(t.Code, SystemExamTypeCodes.EndOfTerm, StringComparison.OrdinalIgnoreCase));
                if (endOfTerm != null) _examTypeCombo.SelectedValue = endOfTerm.ExamTypeId;
                return;
            }

            int id;
            if (int.TryParse(value.ToString(), out id))
                _examTypeCombo.SelectedValue = id;
            UpdateAssessmentControls();
        }

        private async Task SaveAsync()
        {
            if (!AuthService.RequireWriteAccess("Settings.ExamSetup.Manage", _editingSetupId.HasValue ? "Update exam setup" : "Create exam setup")) return;
            try
            {
                _saveBtn.Enabled = false;
                _status.Text = _editingSetupId.HasValue ? "Updating..." : "Saving...";
                var wasEditing = _editingSetupId.HasValue;
                var savedSetupId = _editingSetupId.GetValueOrDefault();

                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await conn.OpenAsync();
                    Microsoft.Data.SqlClient.SqlCommand cmd;
                    var assessmentNumber = IsSelectedEndOfTerm() ? (object)DBNull.Value : (int)_assessmentNumber.Value;
                    var assessmentLabel = SelectedAssessmentLabel();
                    if (_editingSetupId.HasValue) {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                            UPDATE ExamSetups SET Term=@Term, [Year]=@Year, ExamTypeId=@ExamTypeId,
                                AssessmentNumber=@AssessmentNumber, AssessmentLabel=@AssessmentLabel,
                                StartDate=@StartDate, EndDate=@EndDate, ClassWeight=@ClassWeight, ExamWeight=@ExamWeight,
                                UpdatedAt=SYSUTCDATETIME()
                            WHERE SetupID=@SetupID", conn);
                    } else {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                            INSERT INTO ExamSetups (Term, [Year], ExamTypeId, AssessmentNumber, AssessmentLabel, StartDate, EndDate, ClassWeight, ExamWeight, SyncId, UpdatedAt)
                            OUTPUT INSERTED.SetupID
                            VALUES (@Term, @Year, @ExamTypeId, @AssessmentNumber, @AssessmentLabel, @StartDate, @EndDate, @ClassWeight, @ExamWeight, NEWID(), SYSUTCDATETIME())", conn);
                    }
                    cmd.Parameters.AddWithValue("@Term", _termCombo.Text);
                    cmd.Parameters.AddWithValue("@Year", _yearCombo.Text);
                    cmd.Parameters.AddWithValue("@ExamTypeId", SelectedExamTypeIdOrDbNull());
                    cmd.Parameters.AddWithValue("@AssessmentNumber", assessmentNumber);
                    cmd.Parameters.AddWithValue("@AssessmentLabel", assessmentLabel);
                    cmd.Parameters.AddWithValue("@StartDate", _startPicker.Value.Date);
                    cmd.Parameters.AddWithValue("@EndDate", _endPicker.Value.Date);
                    cmd.Parameters.AddWithValue("@ClassWeight", (int)_classWeight.Value);
                    cmd.Parameters.AddWithValue("@ExamWeight", (int)_examWeight.Value);

                    if (_editingSetupId.HasValue) {
                        cmd.Parameters.AddWithValue("@SetupID", _editingSetupId.Value);
                    }

                    if (_editingSetupId.HasValue)
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        savedSetupId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }

                await TryRecordExamSetupSyncAsync(savedSetupId, wasEditing ? "Update" : "Insert");

                if (!wasEditing) {
                    // Automatically generate a notice for teachers ONLY when CREATING
                    try {
                        var repo = new NoticeRepository(AppConfig.ConnectionString);
                        var notice = new Notice {
                            Title = $"New Exam Period: {SelectedAssessmentLabel()} - {_termCombo.Text} {_yearCombo.Text}",
                            Message = $"{SelectedAssessmentLabel()} for {_termCombo.Text} {_yearCombo.Text} has been scheduled from {_startPicker.Value:dd MMM yyyy} to {_endPicker.Value:dd MMM yyyy}. Teachers can now begin entering assessment marks for this period.",
                            Target = "Employees",
                            TargetClass = "",
                            Channel = "Both",
                            SentBy = kingdom_Preparatory_School_Management_System.Services.AuthService.CurrentUser?.Username ?? "Admin",
                            SentDate = DateTime.Now,
                            RecipientCount = 0,
                            Status = "Delivered"
                        };
                        await repo.AddAsync(notice);
                    } catch { /* Ignore if notice fails */ }
                }

                _status.Text = wasEditing ? "Exam period updated successfully." : "Exam period created successfully.";
                _status.ForeColor = Color.Green;

                // Clear selection
                _grid.ClearSelection();
                _editingSetupId = null;
                _saveBtn.Text = CanManageExamSetup ? "Save" : "Read only";
                SelectExamType(null);

                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                _status.Text = "Failed to save: " + ex.Message;
                _status.ForeColor = Color.Red;
            }
            finally
            {
                _saveBtn.Enabled = CanManageExamSetup;
            }
        }

        private object SelectedExamTypeIdOrDbNull()
        {
            if (_examTypeCombo.SelectedValue == null) return DBNull.Value;
            int id;
            return int.TryParse(_examTypeCombo.SelectedValue.ToString(), out id) && id > 0
                ? (object)id
                : DBNull.Value;
        }

        private string SelectedExamTypeName()
        {
            var selected = _examTypeCombo.SelectedItem as ExamType;
            return selected == null || string.IsNullOrWhiteSpace(selected.Name) ? "Exam" : selected.Name;
        }

        private bool IsSelectedEndOfTerm()
        {
            var selected = _examTypeCombo.SelectedItem as ExamType;
            return selected == null || string.Equals(selected.Code, SystemExamTypeCodes.EndOfTerm, StringComparison.OrdinalIgnoreCase);
        }

        private string SelectedAssessmentLabel()
        {
            if (IsSelectedEndOfTerm()) return SelectedExamTypeName();
            return $"{SelectedExamTypeName()} {(int)_assessmentNumber.Value}";
        }

        private void UpdateAssessmentControls()
        {
            if (_assessmentNumber == null || _assessmentPreview == null) return;
            var isEndOfTerm = IsSelectedEndOfTerm();
            _assessmentNumber.Enabled = CanManageExamSetup && !isEndOfTerm;
            _assessmentNumber.Visible = !isEndOfTerm;
            _assessmentPreview.Text = isEndOfTerm ? "Terminal" : SelectedAssessmentLabel();

            if (_classWeight != null && _examWeight != null && _classWeightLabel != null && _examWeightLabel != null)
            {
                _classWeightLabel.Visible = isEndOfTerm;
                _classWeight.Visible = isEndOfTerm;
                _classWeight.Enabled = CanManageExamSetup && isEndOfTerm;

                if (isEndOfTerm)
                {
                    _examWeightLabel.Text = "Exam Weight (%)";
                    _examWeight.Maximum = 100;
                    if (_classWeight.Value + _examWeight.Value != 100)
                    {
                        _classWeight.Value = 50;
                        _examWeight.Value = 50;
                    }
                }
                else
                {
                    _examWeightLabel.Text = "Total Score (Marks)";
                    _examWeight.Maximum = 1000;
                    _classWeight.Value = 0;
                    if (_examWeight.Value == 0) _examWeight.Value = 100;
                }
            }
        }

        private async Task DeleteAsync()
        {
            if (!AuthService.RequireWriteAccess("Settings.ExamSetup.Manage", "Delete exam setup")) return;
            if (_grid.SelectedRows.Count == 0) return;
            var setupId = _grid.SelectedRows[0].Cells["SetupID"].Value;
            if (MessageBox.Show("Are you sure you want to delete this Exam Setup?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                _deleteBtn.Enabled = false;
                _status.Text = "Deleting...";
                await TryRecordExamSetupDeleteAsync(setupId);

                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await conn.OpenAsync();
                    var cmd = new Microsoft.Data.SqlClient.SqlCommand("DELETE FROM ExamSetups WHERE SetupID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", setupId);
                    await cmd.ExecuteNonQueryAsync();
                }

                _status.Text = "Exam setup deleted.";
                _status.ForeColor = Color.Green;
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                _status.Text = "Failed to delete: " + ex.Message;
                _status.ForeColor = Color.Red;
            }
            finally
            {
                _deleteBtn.Enabled = CanManageExamSetup && _grid.SelectedRows.Count > 0;
            }
        }

        private async Task TryRecordExamSetupSyncAsync(int setupId, string operation)
        {
            if (setupId <= 0) return;
            try
            {
                await new SyncChangeRecorder(AppConfig.ConnectionString).RecordUpsertAsync("ExamSetups", "SetupID", setupId, operation);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Exam setup sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordExamSetupDeleteAsync(object setupId)
        {
            try
            {
                await new SyncChangeRecorder(AppConfig.ConnectionString).RecordDeleteAsync("ExamSetups", "SetupID", setupId);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Exam setup delete sync capture skipped: " + ex.Message);
            }
        }

        private void ApplyWriteAccess()
        {
            bool canWrite = CanManageExamSetup;
            _termCombo.Enabled = canWrite;
            _yearCombo.Enabled = canWrite;
            _examTypeCombo.Enabled = canWrite;
            _assessmentNumber.Enabled = canWrite && !IsSelectedEndOfTerm();
            _startPicker.Enabled = canWrite;
            _endPicker.Enabled = canWrite;
            _classWeight.Enabled = canWrite;
            _examWeight.Enabled = canWrite;
            _saveBtn.Enabled = canWrite;
            _saveBtn.Text = canWrite ? "Save Setup" : "Read only";
            _deleteBtn.Enabled = false;
        }
    }
}
