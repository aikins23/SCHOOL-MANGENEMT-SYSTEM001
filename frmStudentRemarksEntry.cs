using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Teachers use this form to enter remarks for each student during the report card session.
    /// Includes pre-defined template selection plus free-text override.
    /// Supports navigation between students in a class.
    /// </summary>
    public class frmStudentRemarksEntry : Form
    {
        private readonly IStudentTermRemarksRepository _remarksRepo;
        private readonly IRemarkTemplateRepository _templateRepo;
        private readonly IStudentRepository _studentRepo;

        private ComboBox cmbClass;
        private ComboBox cmbTerm;
        private ComboBox cmbYear;
        private Button btnLoad;
        private ListBox lstStudents;
        private Label lblStudentInfo;
        private ComboBox cmbConductTemplate;
        private TextBox txtConduct;
        private ComboBox cmbEffortTemplate;
        private TextBox txtEffort;
        private ComboBox cmbAttendanceTemplate;
        private TextBox txtAttendance;
        private TextBox txtClassTeacherRemarks;
        private TextBox txtHeadTeacherRemarks;
        private Button btnPrevious;
        private Button btnSave;
        private Button btnNext;
        private Label lblStatus;
        private CheckBox chkApplyToAll;

        private List<DataRow> _students = new List<DataRow>();
        private int _currentIndex = -1;
        private List<RemarkTemplate> _allTemplates = new List<RemarkTemplate>();

        public frmStudentRemarksEntry()
            : this(new StudentTermRemarksRepository(AppConfig.ConnectionString),
                   new RemarkTemplateRepository(),
                   new StudentRepository(AppConfig.ConnectionString))
        { }

        public frmStudentRemarksEntry(IStudentTermRemarksRepository remarksRepo,
            IRemarkTemplateRepository templateRepo, IStudentRepository studentRepo)
        {
            _remarksRepo = remarksRepo;
            _templateRepo = templateRepo;
            _studentRepo = studentRepo;
            InitializeComponent();
            this.Load += async (s, e) => await OnLoadAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Student Remarks Entry";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.MinimumSize = new Size(1000, 650);

            var title = new Label
            {
                Text = "Student Remarks Entry - Report Card Session",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(600, 28)
            };
            this.Controls.Add(title);

            int y = 55;
            AddLabel("Class:", 20, y);
            cmbClass = new ComboBox { Location = new Point(80, y - 3), Size = new Size(180, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(cmbClass);

            AddLabel("Term:", 280, y);
            cmbTerm = new ComboBox { Location = new Point(330, y - 3), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTerm.Items.AddRange(new object[] { "Term 1", "Term 2", "Term 3" });
            this.Controls.Add(cmbTerm);

            AddLabel("Year:", 470, y);
            cmbYear = new ComboBox { Location = new Point(515, y - 3), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            for (int yr = DateTime.Now.Year - 2; yr <= DateTime.Now.Year + 1; yr++)
                cmbYear.Items.Add($"{yr}/{yr + 1}");
            cmbYear.SelectedIndex = 2;
            this.Controls.Add(cmbYear);

            btnLoad = new Button
            {
                Text = "Load Students",
                Location = new Point(650, y - 5),
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLoad.Click += async (s, e) => await LoadStudentsAsync();
            this.Controls.Add(btnLoad);

            AddLabel("Students:", 20, 95, FontStyle.Bold);
            lstStudents = new ListBox { Location = new Point(20, 120), Size = new Size(260, 500) };
            lstStudents.SelectedIndexChanged += async (s, e) => await LstStudents_SelectedIndexChangedAsync();
            this.Controls.Add(lstStudents);

            lblStudentInfo = new Label
            {
                Location = new Point(300, 95),
                Size = new Size(860, 30),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210)
            };
            this.Controls.Add(lblStudentInfo);

            int rowY = 135;
            int colA = 300, colALabelW = 130, colATplW = 220, colATxtX = 660;

            AddLabel("Conduct:", colA, rowY, FontStyle.Bold);
            cmbConductTemplate = NewTemplateCombo(colA + colALabelW, rowY - 3, colATplW);
            cmbConductTemplate.SelectedIndexChanged += (s, e) => ApplyTemplate(cmbConductTemplate, txtConduct);
            this.Controls.Add(cmbConductTemplate);
            txtConduct = NewMultilineText(colATxtX - 360, rowY + 22, 820, 50);
            this.Controls.Add(txtConduct);

            rowY += 85;
            AddLabel("Effort:", colA, rowY, FontStyle.Bold);
            cmbEffortTemplate = NewTemplateCombo(colA + colALabelW, rowY - 3, colATplW);
            cmbEffortTemplate.SelectedIndexChanged += (s, e) => ApplyTemplate(cmbEffortTemplate, txtEffort);
            this.Controls.Add(cmbEffortTemplate);
            txtEffort = NewMultilineText(colATxtX - 360, rowY + 22, 820, 50);
            this.Controls.Add(txtEffort);

            rowY += 85;
            AddLabel("Attendance:", colA, rowY, FontStyle.Bold);
            cmbAttendanceTemplate = NewTemplateCombo(colA + colALabelW, rowY - 3, colATplW);
            cmbAttendanceTemplate.SelectedIndexChanged += (s, e) => ApplyTemplate(cmbAttendanceTemplate, txtAttendance);
            this.Controls.Add(cmbAttendanceTemplate);
            txtAttendance = NewMultilineText(colATxtX - 360, rowY + 22, 820, 50);
            this.Controls.Add(txtAttendance);

            rowY += 85;
            AddLabel("Class Teacher's Remarks:", colA, rowY, FontStyle.Bold);
            txtClassTeacherRemarks = NewMultilineText(colA, rowY + 22, 820, 70);
            this.Controls.Add(txtClassTeacherRemarks);

            rowY += 105;
            AddLabel("Head Teacher's Remarks:", colA, rowY, FontStyle.Bold);
            txtHeadTeacherRemarks = NewMultilineText(colA, rowY + 22, 820, 70);
            txtHeadTeacherRemarks.ReadOnly = !AuthService.CanAccess("frmLeaveApproval");
            this.Controls.Add(txtHeadTeacherRemarks);

            rowY += 110;
            chkApplyToAll = new CheckBox
            {
                Text = "Apply this template selection to all remaining students",
                Location = new Point(colA, rowY),
                Size = new Size(420, 22),
                Font = new Font("Segoe UI", 8.5F)
            };
            this.Controls.Add(chkApplyToAll);

            btnPrevious = new Button
            {
                Text = "< Previous Student",
                Location = new Point(colA, 660),
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(96, 125, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPrevious.Click += async (s, e) => await MoveAsync(-1);
            this.Controls.Add(btnPrevious);

            btnSave = new Button
            {
                Text = "Save Remarks",
                Location = new Point(490, 660),
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSave.Click += async (s, e) => await SaveAsync();
            this.Controls.Add(btnSave);

            btnNext = new Button
            {
                Text = "Next Student >",
                Location = new Point(940, 660),
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNext.Click += async (s, e) => await MoveAsync(1);
            this.Controls.Add(btnNext);

            lblStatus = new Label
            {
                Location = new Point(20, 625),
                Size = new Size(1140, 22),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            this.Controls.Add(lblStatus);
        }

        private void AddLabel(string text, int x, int y, FontStyle style = FontStyle.Regular)
        {
            var l = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(150, 22),
                Font = new Font("Segoe UI", 9F, style)
            };
            this.Controls.Add(l);
        }

        private ComboBox NewTemplateCombo(int x, int y, int w)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F)
            };
        }

        private TextBox NewMultilineText(int x, int y, int w, int h)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                Multiline = true,
                Font = new Font("Segoe UI", 9F),
                ScrollBars = ScrollBars.Vertical
            };
        }

        private async Task OnLoadAsync()
        {
            try
            {
                await _templateRepo.EnsureTableAsync();
                await _templateRepo.SeedDefaultsAsync();
                _allTemplates = await _templateRepo.GetAllAsync();
                PopulateTemplateCombos();
                await LoadClassesAsync();
                cmbTerm.SelectedIndex = 0;
                SetStatus("Ready. Select class, term, year and click Load Students.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initialization failed: " + ex.Message);
            }
        }

        private void PopulateTemplateCombos()
        {
            PopulateCombo(cmbConductTemplate, RemarkCategories.Conduct);
            PopulateCombo(cmbEffortTemplate, RemarkCategories.Effort);
            PopulateCombo(cmbAttendanceTemplate, RemarkCategories.Attendance);
        }

        private void PopulateCombo(ComboBox cmb, string category)
        {
            cmb.Items.Clear();
            cmb.Items.Add("(Select template or type custom)");
            foreach (var t in _allTemplates.Where(x => x.Category == category)
                                            .OrderBy(x => x.SubCategory).ThenBy(x => x.DisplayOrder))
            {
                cmb.Items.Add(new TemplateItem(t));
            }
            cmb.SelectedIndex = 0;
        }

        private async Task LoadClassesAsync()
        {
            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT DISTINCT ClassName FROM ClassAdmin ORDER BY ClassName", conn))
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        cmbClass.Items.Clear();
                        while (await r.ReadAsync())
                            cmbClass.Items.Add(r.GetString(0));
                    }
                    if (cmbClass.Items.Count > 0) cmbClass.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("LoadClassesAsync failed: " + ex.Message);
            }
        }

        private async Task LoadStudentsAsync()
        {
            if (cmbClass.SelectedItem == null || cmbTerm.SelectedItem == null || cmbYear.SelectedItem == null)
            {
                MessageBox.Show("Please select Class, Term and Year.");
                return;
            }
            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                {
                    await conn.OpenAsync();
                    const string sql = @"SELECT StudentID, FirstName, LastName FROM Students
                                          WHERE Class = @c ORDER BY LastName, FirstName";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", cmbClass.SelectedItem.ToString());
                        using (var da = new Microsoft.Data.SqlClient.SqlDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);
                            _students = dt.Rows.Cast<DataRow>().ToList();
                        }
                    }
                }
                lstStudents.Items.Clear();
                foreach (var row in _students)
                    lstStudents.Items.Add($"{row["LastName"]}, {row["FirstName"]}  [{row["StudentID"]}]");
                _currentIndex = _students.Count > 0 ? 0 : -1;
                if (_currentIndex == 0) lstStudents.SelectedIndex = 0;
                SetStatus($"Loaded {_students.Count} students.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load students failed: " + ex.Message);
            }
        }

        private async Task LstStudents_SelectedIndexChangedAsync()
        {
            if (lstStudents.SelectedIndex < 0 || lstStudents.SelectedIndex >= _students.Count) return;
            _currentIndex = lstStudents.SelectedIndex;
            await LoadStudentRemarksAsync();
        }

        private async Task LoadStudentRemarksAsync()
        {
            if (_currentIndex < 0 || _currentIndex >= _students.Count) return;
            var row = _students[_currentIndex];
            string sid = row["StudentID"].ToString();
            string name = $"{row["FirstName"]} {row["LastName"]}";
            lblStudentInfo.Text = $"{name}  ·  {sid}  ·  {cmbClass.SelectedItem}  ·  {cmbTerm.SelectedItem} {cmbYear.SelectedItem}";

            try
            {
                var existing = await _remarksRepo.GetAsync(sid, cmbTerm.SelectedItem.ToString(), cmbYear.SelectedItem.ToString());
                if (existing != null)
                {
                    txtConduct.Text = existing.Conduct ?? string.Empty;
                    txtEffort.Text = existing.Interest ?? string.Empty;
                    txtAttendance.Text = existing.Attitude ?? string.Empty;
                    txtClassTeacherRemarks.Text = existing.ClassTeacherRemarks ?? string.Empty;
                    txtHeadTeacherRemarks.Text = existing.HeadTeacherRemarks ?? string.Empty;
                }
                else
                {
                    txtConduct.Clear();
                    txtEffort.Clear();
                    txtAttendance.Clear();
                    txtClassTeacherRemarks.Clear();
                    txtHeadTeacherRemarks.Clear();
                }
                SetStatus($"Editing {name}");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("LoadStudentRemarks: " + ex.Message);
            }
        }

        private void ApplyTemplate(ComboBox cmb, TextBox tx)
        {
            if (cmb.SelectedItem is TemplateItem ti)
                tx.Text = ti.Template.RemarkText;
        }

        private async Task SaveAsync()
        {
            if (_currentIndex < 0)
            {
                MessageBox.Show("Select a student first.");
                return;
            }
            var row = _students[_currentIndex];
            var remarks = new StudentTermRemarks
            {
                StudentID = row["StudentID"].ToString(),
                Term = cmbTerm.SelectedItem.ToString(),
                Year = cmbYear.SelectedItem.ToString(),
                Conduct = txtConduct.Text.Trim(),
                Interest = txtEffort.Text.Trim(),
                Attitude = txtAttendance.Text.Trim(),
                ClassTeacherRemarks = txtClassTeacherRemarks.Text.Trim(),
                HeadTeacherRemarks = txtHeadTeacherRemarks.Text.Trim()
            };
            try
            {
                var existing = await _remarksRepo.GetAsync(remarks.StudentID, remarks.Term, remarks.Year);
                bool ok;
                if (existing == null) ok = await _remarksRepo.AddAsync(remarks);
                else
                {
                    remarks.ID = existing.ID;
                    ok = await _remarksRepo.UpdateAsync(remarks);
                }
                SetStatus(ok ? "Saved." : "Save returned 0 rows.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        private async Task MoveAsync(int direction)
        {
            await SaveAsync();
            int next = _currentIndex + direction;
            if (next < 0 || next >= _students.Count)
            {
                SetStatus(direction < 0 ? "At first student." : "At last student.");
                return;
            }
            lstStudents.SelectedIndex = next;
        }

        private void SetStatus(string msg) => lblStatus.Text = msg;

        private class TemplateItem
        {
            public RemarkTemplate Template { get; }
            public TemplateItem(RemarkTemplate t) { Template = t; }
            public override string ToString() => $"[{Template.SubCategory}] {Truncate(Template.RemarkText, 60)}";
            private static string Truncate(string s, int n) =>
                string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= n ? s : s.Substring(0, n) + "...");
        }
    }
}
