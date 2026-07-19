using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EXAMS : Form
    {
        // â”€â”€ Services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private readonly ExamService    _examService;
        private readonly StudentService _studentService;

        // â”€â”€ Header fields (plain WinForms â€” no Guna dependency) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private ComboBox studentComboBox;
        private ComboBox termBox;
        private TextBox  yearBox;

        // â”€â”€ Status / summary labels â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Label statusLabel;
        private Label completionLabel;
        private Label averageLabel;

        // â”€â”€ Subject grid â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private TableLayoutPanel subjectGrid;

        // â”€â”€ Palette â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Color PageBack  => UiTheme.Page;
        private Color Surface   => UiTheme.Surface;
        private Color SurfaceAlt=> UiTheme.SurfaceAlt;
        private Color Navy      => UiTheme.Navy;
        private Color TextCol   => UiTheme.Text;
        private Color Muted     => UiTheme.Muted;
        private Color Border    => UiTheme.Border;
        private static readonly Color Primary   = Color.FromArgb(31, 99, 198);

        // â”€â”€ Subject list â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Current subjects shown in the grid; replaced per class on lookup.
        private string[] subjects = Common.SubjectCatalog.StandardSubjectsForClass("BASIC 7").ToArray();

        // â”€â”€ Per-subject row controls â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private readonly Dictionary<string, SubjectRows> subjectRows =
            new Dictionary<string, SubjectRows>();

        private class SubjectRows
        {
            public TextBox Cat1, Cat2, Cat3, RawExam, Remark;
            public Label   SbaTotal, ScaledExam, Total, Grade;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private string _entryMode = "Student";
        private bool _isSavingResults;
        private List<KingdomPrep.Shared.Models.Student> _currentStudents = new List<KingdomPrep.Shared.Models.Student>();
        private ComboBox classComboBox;
        private ComboBox subjectComboBox;
        private Control classBoxWrapper;
        private Button btnModeStudent;
        private Button btnModeSubject;
        private TableLayoutPanel studentInputPanel;
        private TableLayoutPanel subjectInputPanel;
        private Control termBoxWrapper;
        private Control yearBoxWrapper;

        public EXAMS()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("EXAMS", this)) return;
            this.Load += EXAMS_Load;

            var examRepo    = new ExamRepository(AppConfig.ConnectionString);
            _examService    = new ExamService(examRepo);

            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepo     = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepo);

            BuildExamSubmissionView();
            NavigationSidebar.AddTo(this);
        }

        private async void EXAMS_Load(object sender, EventArgs e)
        {
            await LoadClassListAsync();
            await LoadActiveExamSetupAsync();
            await System.Threading.Tasks.Task.Run(() => _ = Common.GradingScheme.Bands);
            SwitchMode("Student");
        }

        private async System.Threading.Tasks.Task LoadClassListAsync()
        {
            if (classComboBox == null) return;

            try
            {
                var repo = new SchoolInfoRepository(AppConfig.ConnectionString);
                await repo.EnsureTablesAsync();
                var classes = await repo.GetClassNamesAsync();
                if (classes.Count == 0)
                    classes.AddRange(AppConfig.ClassNames);

                var previousClass = classComboBox.Text;
                classComboBox.Items.Clear();
                foreach (var className in classes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase))
                    classComboBox.Items.Add(className);

                if (classComboBox.Items.Count > 0)
                {
                    var match = classComboBox.Items
                        .Cast<object>()
                        .FirstOrDefault(item => string.Equals(item?.ToString(), previousClass, StringComparison.OrdinalIgnoreCase));
                    classComboBox.SelectedItem = match ?? classComboBox.Items[0];
                }

                await RefreshSubjectComboForClassAsync();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Exam class list fell back to configured defaults: " + ex.Message);
                classComboBox.Items.Clear();
                classComboBox.Items.AddRange(AppConfig.ClassNames.Cast<object>().ToArray());
                if (classComboBox.Items.Count > 0)
                    classComboBox.SelectedIndex = 0;
                await RefreshSubjectComboForClassAsync();
            }
        }

        private async System.Threading.Tasks.Task LoadActiveExamSetupAsync()
        {
            try
            {
                var activeSetup = await Services.ExamSetupManager.GetActiveSetupAsync();
                if (activeSetup != null)
                {
                    if (termBox != null && yearBox != null)
                    {
                        termBox.Text = activeSetup.Term;
                        yearBox.Text = activeSetup.Year;
                        termBox.Enabled = false;
                        yearBox.Enabled = false;
                    }

                    if (AuthService.IsTeacher)
                    {
                        string myClass = await AuthService.GetCurrentTeacherClassAsync();
                        if (!string.IsNullOrEmpty(myClass))
                        {
                            if (classComboBox != null)
                            {
                                classComboBox.Text = myClass;
                                classComboBox.Enabled = false;
                            }
                        }
                    }
                }
                else
                {
                    ApplyDefaultTermAndYear();
                }
            }
            catch (Exception ex)
            {
                ApplyDefaultTermAndYear();
                if (statusLabel != null)
                {
                    statusLabel.Text = "Failed to verify exam setup: " + ex.Message;
                    statusLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Layout
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void ApplyDefaultTermAndYear()
        {
            if (termBox != null && termBox.SelectedIndex < 0 && termBox.Items.Count > 0)
                termBox.SelectedIndex = 0;
            if (yearBox != null && string.IsNullOrWhiteSpace(yearBox.Text))
                yearBox.Text = DateTime.Today.Year.ToString();
        }

        private void BuildExamSubmissionView()
        {
            SuspendLayout();
            Controls.Clear();
            subjectRows.Clear();

            Text          = "Exam Submission";
            BackColor     = PageBack;
            Font          = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize   = new Size(1180, 740);

            var root = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 4,
                BackColor   = PageBack,
                Padding     = new Padding(26)   // sidebar added via NavigationSidebar.AddTo
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));  // student panel
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // subject grid
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));   // bottom actions

            root.Controls.Add(BuildHeader(),              0, 0);
            root.Controls.Add(BuildStudentPanel(),        0, 1);
            root.Controls.Add(BuildSubjectEntryPanel(),   0, 2);
            root.Controls.Add(BuildBottomActions(),       0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                BackColor   = PageBack
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBack };
            title.Controls.Add(new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 26,
                Text      = "Enter continuous assessments and exams by Subject or Student",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });
            title.Controls.Add(new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Grading",
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var nav = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = PageBack,
                Padding       = new Padding(0, 14, 0, 0)
            };

            var saveBtn = MakePrimaryBtn("Save Grades", async () => await SaveCurrentDataAsync(), 112);
            btnModeStudent = MakeSecondaryBtn("By Student", () => SwitchMode("Student"), 104);
            btnModeSubject = MakeSecondaryBtn("By Subject", () => SwitchMode("Subject"), 104);

            if (AuthService.CanWrite("Academics.ExamResults.Manage"))
            {
                nav.Controls.Add(saveBtn);
            }
            nav.Controls.Add(btnModeStudent);
            nav.Controls.Add(btnModeSubject);

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(nav,   1, 0);
            return header;
        }

        private Control BuildStudentPanel()
        {
            var card = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding     = new Padding(20, 12, 20, 0),
                Margin      = new Padding(0, 0, 0, 10)
            };

            var inputsWrapper = new Panel { Dock = DockStyle.Fill, BackColor = Surface };

            // Common Controls
            classComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0) };
            classBoxWrapper = MakeLabeledField("Class", classComboBox);

            termBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0) };
            termBox.Items.AddRange(new object[] { "First Term", "Second Term", "Third Term" });
            termBox.SelectedIndexChanged += (s, e) => LoadGridData();
            termBoxWrapper = MakeLabeledField("Academic Term", termBox);

            yearBox = MakeField(false);
            yearBox.Text = DateTime.Today.Year.ToString();
            yearBoxWrapper = MakeLabeledField("Academic Year", yearBox);

            // Student Input Panel
            studentInputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0) };
            studentInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            studentInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            studentInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            studentInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            studentComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0) };
            studentComboBox.SelectedIndexChanged += (s, e) => LoadGridData();
            var studentComboWrapper = MakeLabeledField("Student", studentComboBox);

            studentInputPanel.Controls.Add(classBoxWrapper, 0, 0);
            studentInputPanel.Controls.Add(studentComboWrapper, 1, 0);
            studentInputPanel.Controls.Add(termBoxWrapper, 2, 0);
            studentInputPanel.Controls.Add(yearBoxWrapper, 3, 0);

            // Subject Input Panel
            subjectInputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0), Visible = false };
            subjectInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            subjectInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            subjectInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            subjectInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            subjectComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0) };
            _ = RefreshSubjectComboForClassAsync();
            subjectComboBox.SelectedIndexChanged += (s, e) => LoadGridData();
            var subjectComboWrapper = MakeLabeledField("Subject", subjectComboBox);

            subjectInputPanel.Controls.Add(classBoxWrapper, 0, 0);
            subjectInputPanel.Controls.Add(subjectComboWrapper, 1, 0);
            subjectInputPanel.Controls.Add(termBoxWrapper, 2, 0);
            subjectInputPanel.Controls.Add(yearBoxWrapper, 3, 0);

            classComboBox.SelectedIndexChanged += async (s, e) => {
                await RefreshSubjectComboForClassAsync();
                if (_entryMode == "Student") {
                    await LoadStudentsForClassAsync();
                } else {
                    LoadGridData();
                }
            };

            inputsWrapper.Controls.Add(studentInputPanel);
            inputsWrapper.Controls.Add(subjectInputPanel);

            var summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                ColumnCount = 3,
                BackColor = Surface
            };
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

            statusLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Select mode to begin.",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            completionLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = $"0 of {subjects.Length} subjects",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            averageLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Average: --",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            summaryPanel.Controls.Add(statusLabel, 0, 0);
            summaryPanel.Controls.Add(completionLabel, 1, 0);
            summaryPanel.Controls.Add(averageLabel, 2, 0);
            card.Controls.Add(summaryPanel);

            card.Controls.Add(inputsWrapper);
            return card;
        }

        private async System.Threading.Tasks.Task LoadStudentsForClassAsync()
        {
            if (string.IsNullOrWhiteSpace(classComboBox.Text)) return;
            var classStudents = await _studentService.GetStudentsByClassAsync(classComboBox.Text);
            _currentStudents.Clear();
            studentComboBox.Items.Clear();
            foreach (var s in classStudents)
            {
                _currentStudents.Add(s);
                studentComboBox.Items.Add(s.FullName);
            }
            if (studentComboBox.Items.Count > 0) studentComboBox.SelectedIndex = 0;
            else LoadGridData();
        }

        private async System.Threading.Tasks.Task SaveCurrentDataAsync()
        {
            SaveAllResults();
        }

        private void SwitchMode(string mode)
        {
            _entryMode = mode;
            if (mode == "Student")
            {
                btnModeStudent.BackColor = Primary; btnModeStudent.ForeColor = Color.White; btnModeStudent.FlatAppearance.BorderColor = Primary;
                btnModeSubject.BackColor = SurfaceAlt; btnModeSubject.ForeColor = TextCol; btnModeSubject.FlatAppearance.BorderColor = Border;
                subjectInputPanel.Visible = false;
                studentInputPanel.Controls.Add(classBoxWrapper, 0, 0);
                studentInputPanel.Controls.Add(termBoxWrapper, 2, 0);
                studentInputPanel.Controls.Add(yearBoxWrapper, 3, 0);
                studentInputPanel.Visible = true;
                _ = LoadStudentsForClassAsync();
            }
            else
            {
                btnModeSubject.BackColor = Primary; btnModeSubject.ForeColor = Color.White; btnModeSubject.FlatAppearance.BorderColor = Primary;
                btnModeStudent.BackColor = SurfaceAlt; btnModeStudent.ForeColor = TextCol; btnModeStudent.FlatAppearance.BorderColor = Border;
                studentInputPanel.Visible = false;
                subjectInputPanel.Controls.Add(classBoxWrapper, 0, 0);
                subjectInputPanel.Controls.Add(termBoxWrapper, 2, 0);
                subjectInputPanel.Controls.Add(yearBoxWrapper, 3, 0);
                subjectInputPanel.Visible = true;
                LoadGridData();
            }
        }

        private async System.Threading.Tasks.Task RefreshSubjectComboForClassAsync()
        {
            if (subjectComboBox == null) return;

            var selectedSubject = subjectComboBox.Text;
            var className = string.IsNullOrWhiteSpace(classComboBox?.Text) ? "BASIC 7" : classComboBox.Text;
            var classSubjects = await System.Threading.Tasks.Task.Run(() => Common.SubjectCatalog.SubjectsForClass(className).ToList());
            subjectComboBox.Items.Clear();
            foreach (var subject in classSubjects)
                subjectComboBox.Items.Add(subject);

            if (subjectComboBox.Items.Count == 0)
                return;

            var match = subjectComboBox.Items
                .Cast<object>()
                .FirstOrDefault(item => string.Equals(item?.ToString(), selectedSubject, StringComparison.OrdinalIgnoreCase));
            subjectComboBox.SelectedItem = match ?? subjectComboBox.Items[0];
        }

        private async void LoadGridData()
        {
            if (string.IsNullOrWhiteSpace(termBox.Text) || string.IsNullOrWhiteSpace(yearBox.Text))
                return;

            if (_entryMode == "Student")
            {
                if (studentComboBox.SelectedIndex < 0)
                {
                    RebuildGrid(new string[0], "Subject Name");
                    return;
                }
                var student = _currentStudents[studentComboBox.SelectedIndex];
                if (student != null)
                {
                    var subs = await System.Threading.Tasks.Task.Run(() => Common.SubjectCatalog.SubjectsForClass(student.ClassID).ToList());
                    RebuildGrid(subs, "Subject Name");
                    await LoadExistingResults(student.StudentID);
                }
            }
            else
            {
                if (classComboBox.SelectedIndex < 0 || subjectComboBox.SelectedIndex < 0)
                {
                    RebuildGrid(new string[0], "Student Name");
                    return;
                }

                string sClass = classComboBox.Text;
                var classStudents = await _studentService.GetStudentsByClassAsync(sClass);
                _currentStudents.Clear();
                foreach(var s in classStudents) _currentStudents.Add(s);
                var stdNames = new List<string>();
                foreach(var s in classStudents) stdNames.Add(s.FullName);
                RebuildGrid(stdNames, "Student Name");

                // For By Subject, load existing results for all students in this class for this subject
                try
                {
                    var table = await _examService.GetExistingResultsForClassSubjectAsync(sClass, subjectComboBox.Text, termBox.Text, yearBox.Text.Trim());
                    if (table.Rows.Count > 0)
                    {
                        foreach (System.Data.DataRow dr in table.Rows)
                        {
                            string sid = dr["student_id"].ToString();
                            var st = _currentStudents.Find(x => x.StudentID == sid);
                            if (st != null && subjectRows.ContainsKey(st.FullName))
                            {
                                var row = subjectRows[st.FullName];
                                row.Cat1.Text = FormatExamNumber(dr["cat1"]);
                                row.Cat2.Text = FormatExamNumber(dr["cat2"]);
                                row.Cat3.Text = FormatExamNumber(dr["cat3"]);
                                if (decimal.TryParse(dr["exam_score"].ToString(), out decimal savedExam))
                                {
                                    row.RawExam.Text = (savedExam * 2).ToString("0.00");
                                }
                                row.Remark.Text = dr["remark"].ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogWarning("Existing exam scores could not be loaded into the grading grid: " + ex.Message);
                    if (statusLabel != null)
                    {
                        statusLabel.Text = "Existing scores could not be loaded. You can still enter new scores.";
                        statusLabel.ForeColor = UiTheme.WarningText;
                    }
                }
            }
        }


        private Control BuildSubjectEntryPanel()
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Surface, BorderStyle = BorderStyle.FixedSingle };

            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            UiTheme.HideNativeScrollbarsFor(scrollPanel);

            subjectGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 10,
                RowCount = 1,
                BackColor = Surface,
                Padding = new Padding(14),
                MinimumSize = new Size(1200, 0)
            };

            RebuildGrid(new string[0], "Subject Name");

            scrollPanel.Controls.Add(subjectGrid);
            card.Controls.Add(scrollPanel);

            return card;
        }

        private void RebuildGrid(IReadOnlyList<string> items, string firstColName)
        {
            subjectGrid.SuspendLayout();
            subjectGrid.Controls.Clear();
            subjectGrid.RowStyles.Clear();
            subjectGrid.ColumnStyles.Clear();
            subjectRows.Clear();

            subjectGrid.RowCount = items.Count + 1;
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); // Name
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112)); // Class test
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112)); // Group work
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // Project work
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));  // 50% SBA
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112)); // Exams
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));  // 50% Exam
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104)); // Total
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));  // Grade
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // Remark

            subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            if (items.Count == 0)
            {
                subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
                subjectGrid.RowCount = 2;
                var emptyLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Select a class and student to load subjects.",
                    ForeColor = Muted,
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Surface
                };
                subjectGrid.Controls.Add(emptyLabel, 0, 1);
                subjectGrid.SetColumnSpan(emptyLabel, 10);
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                    subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            }

            string[] headers = { firstColName, "Class Test\n(40)", "Group Work\n(10)", "Project Work\n(10)", "50% SBA", "Exams\n(100%)", "50% Exam", "100% Total", "Grade", "Remark" };
            for (int i = 0; i < headers.Length; i++)
            {
                var lbl = MakeGridHeader(headers[i]);
                if (i == 0) { lbl.BackColor = Color.White; lbl.ForeColor = TextCol; }
                else if (i >= 1 && i <= 3) { lbl.BackColor = SurfaceAlt; lbl.ForeColor = TextCol; }
                else if (i == 4) { lbl.BackColor = Navy; lbl.ForeColor = Color.White; }
                else if (i == 5) { lbl.BackColor = Color.FromArgb(200, 40, 40); lbl.ForeColor = Color.White; }
                else if (i == 6) { lbl.BackColor = Color.FromArgb(200, 40, 40); lbl.ForeColor = Color.White; }
                else if (i == 7) { lbl.BackColor = Color.FromArgb(10, 20, 40); lbl.ForeColor = Color.White; }
                else { lbl.BackColor = SurfaceAlt; lbl.ForeColor = TextCol; }
                subjectGrid.Controls.Add(lbl, i, 0);
            }

            for (int i = 0; i < items.Count; i++)
                AddSubjectRow(items[i], i + 1);

            subjectGrid.ResumeLayout(true);
        }

        private Control BuildBottomActions()
        {
            var actions = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = PageBack,
                Padding       = new Padding(0, 12, 0, 0)
            };
            if (AuthService.CanWrite("Academics.ExamResults.Manage"))
            {
                actions.Controls.Add(MakePrimaryBtn("Save All Results", SaveAllResults, 148));
            }
            actions.Controls.Add(MakeSecondaryBtn("Clear Form",  ClearForm,               112));
            actions.Controls.Add(MakeSecondaryBtn("View Reports", () => FormManager.ShowForm<EXAMSVIEW>(this), 116));
            return actions;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Control factories
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private TextBox MakeField(bool readOnly = false)
        {
            return new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 10F),
                ReadOnly    = readOnly,
                BackColor   = readOnly ? SurfaceAlt : Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin      = new Padding(0, 0, 8, 0)
            };
        }

        private Control MakeLabeledField(string label, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                BackColor   = Surface,
                Padding     = new Padding(0, 0, 8, 0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label
            {
                Dock      = DockStyle.Fill,
                Text      = label,
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            input.Dock = DockStyle.Fill;
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private TextBox MakeScoreInput()
        {
            return new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign   = HorizontalAlignment.Center,
                Margin      = new Padding(2)
            };
        }

        private Label MakeSummaryChip(string text)
        {
            return new Label
            {
                Dock      = DockStyle.Fill,
                Text      = text,
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = SurfaceAlt,
                Margin    = new Padding(6, 0, 0, 0)
            };
        }

        private Label MakeGridHeader(string text)
        {
            return new Label
            {
                Dock      = DockStyle.Fill,
                Text      = text,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin    = new Padding(0, 0, 1, 1)
            };
        }

        private Label MakeGridValue(string text)
        {
            return new Label
            {
                Dock      = DockStyle.Fill,
                Text      = text,
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin    = new Padding(2),
                BackColor = SurfaceAlt
            };
        }

        private Button MakePrimaryBtn(string text, Action action, int width = 120)
        {
            var btn = new Button
            {
                Text      = text,
                Width     = width,
                Height    = 38,
                Margin    = new Padding(8, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                BackColor = Navy,
                ForeColor = Color.White
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;
            btn.Click += (s, e) => action();
            return btn;
        }

        private Button MakeSecondaryBtn(string text, Action action, int width = 110)
        {
            var btn = new Button
            {
                Text      = text,
                Width     = width,
                Height    = 38,
                Margin    = new Padding(8, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                BackColor = Surface,
                ForeColor = TextCol
            };
            btn.FlatAppearance.BorderColor         = Border;
            btn.FlatAppearance.MouseOverBackColor  = UiTheme.GoldSoft;
            btn.Click += (s, e) => action();
            return btn;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Subject row building
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void AddSubjectRow(string subject, int rowIndex)
        {
            bool odd = rowIndex % 2 != 0;
            subjectGrid.Controls.Add(new Label
            {
                Dock      = DockStyle.Fill,
                Text      = subject,
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0),
                BackColor = odd ? SurfaceAlt : Surface
            }, 0, rowIndex);

            var row = new SubjectRows
            {
                Cat1       = MakeScoreInput(),
                Cat2       = MakeScoreInput(),
                Cat3       = MakeScoreInput(),
                RawExam    = MakeScoreInput(),
                SbaTotal   = MakeGridValue("-"),
                ScaledExam = MakeGridValue("-"),
                Total      = MakeGridValue("-"),
                Grade      = MakeGridValue("-"),
                Remark     = MakeField(false) // editable remark
            };

            row.Cat1.TextChanged    += (s, e) => CalculateRow(subject);
            row.Cat2.TextChanged    += (s, e) => CalculateRow(subject);
            row.Cat3.TextChanged    += (s, e) => CalculateRow(subject);
            row.RawExam.TextChanged += (s, e) => CalculateRow(subject);

            subjectGrid.Controls.Add(row.Cat1,       1, rowIndex);
            subjectGrid.Controls.Add(row.Cat2,       2, rowIndex);
            subjectGrid.Controls.Add(row.Cat3,       3, rowIndex);
            subjectGrid.Controls.Add(row.SbaTotal,   4, rowIndex);
            subjectGrid.Controls.Add(row.RawExam,    5, rowIndex);
            subjectGrid.Controls.Add(row.ScaledExam, 6, rowIndex);
            subjectGrid.Controls.Add(row.Total,      7, rowIndex);
            subjectGrid.Controls.Add(row.Grade,      8, rowIndex);
            subjectGrid.Controls.Add(row.Remark,     9, rowIndex);
            subjectRows[subject] = row;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Business logic (unchanged; Guna property access is gone)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void CalculateRow(string subject)
        {
            var row = subjectRows[subject];

            // Check if entirely empty
            if (string.IsNullOrWhiteSpace(row.Cat1.Text) &&
                string.IsNullOrWhiteSpace(row.Cat2.Text) &&
                string.IsNullOrWhiteSpace(row.Cat3.Text) &&
                string.IsNullOrWhiteSpace(row.RawExam.Text))
            {
                row.SbaTotal.Text   = "-";
                row.ScaledExam.Text = "-";
                row.Total.Text      = "-";
                row.Grade.Text      = "-";

                // Keep the manual remark if any, but if it was auto-set to a grade remark, clear it
                if (!string.IsNullOrWhiteSpace(row.Remark.Text) && (row.Remark.Text == "Advance" || row.Remark.Text == "Proficiency" || row.Remark.Text == "Approaching Proficiency" || row.Remark.Text == "Developing" || row.Remark.Text == "Beginning"))
                {
                    row.Remark.Text = "";
                }

                UpdateSummary();
                return;
            }

            // If inputs are empty or invalid, try to parse what we can or treat as 0 for calculations
            TryReadScore(row.Cat1, 40m, out decimal c1);
            TryReadScore(row.Cat2, 10m, out decimal c2);
            TryReadScore(row.Cat3, 10m, out decimal c3);
            TryReadScore(row.RawExam, 100m, out decimal rawExam);

            decimal rawSbaTotal = c1 + c2 + c3;
            decimal sbaTotal = Math.Round(rawSbaTotal / 60m * 50m, 2);
            decimal scaledExam = Math.Round(rawExam / 2.0m, 2);
            decimal finalTotal = Math.Min(100m, sbaTotal + scaledExam);

            row.SbaTotal.Text = sbaTotal.ToString("0.00");
            row.ScaledExam.Text = scaledExam.ToString("0.00");
            row.Total.Text = finalTotal.ToString("0.00");

            // Match Web App Auto Grade logic
            string grade = kingdom_Preparatory_School_Management_System.Common.GradingScheme.CodeForScore(finalTotal);
            string autoRemark = kingdom_Preparatory_School_Management_System.Common.GradingScheme.LabelForScore(finalTotal);

            row.Grade.Text = grade;

            // Only auto-update remark if the teacher hasn't typed anything else
            if (string.IsNullOrWhiteSpace(row.Remark.Text) || row.Remark.Text == "-" || row.Remark.Text == "Advance" || row.Remark.Text == "Proficiency" || row.Remark.Text == "Approaching Proficiency" || row.Remark.Text == "Developing" || row.Remark.Text == "Beginning" || row.Remark.Text.Contains(autoRemark) || autoRemark.Contains(row.Remark.Text) || kingdom_Preparatory_School_Management_System.Common.GradingScheme.Bands.Any(b => b.Label == row.Remark.Text))
            {
                row.Remark.Text = autoRemark;
            }

            UpdateSummary();
        }

        private bool TryReadScore(TextBox input, decimal max, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(input.Text)) return false;
            if (decimal.TryParse(input.Text.Trim(), out value))
            {
                bool changed = false;
                if (value < 0m) { value = 0m; changed = true; }
                if (value > max) { value = max; changed = true; }
                if (changed)
                {
                    input.Text = value.ToString("0.00");
                    input.SelectionStart = input.Text.Length;
                }
                return true;
            }
            return false;
        }

        private void UpdateSummary()
        {
            if (completionLabel == null || averageLabel == null) return;
            int ready = 0; decimal total = 0m;
            foreach (var row in subjectRows.Values)
            {
                if (decimal.TryParse(row.Total.Text, out decimal score))
                { ready++; total += score; }
            }
            completionLabel.Text = $"{ready} of {subjectRows.Count} {(subjectRows.Count == 1 ? "entry" : "entries")}";
            averageLabel.Text    = ready == 0 ? "Average: --" : $"Average: {(total / ready):0.00}";
        }



        private async System.Threading.Tasks.Task LoadExistingResults(string studentId)
        {
            if (termBox.SelectedIndex < 0) return;
            try
            {
                var table = await _examService.GetExistingResultsForStudentAsync(
                    studentId, termBox.Text, yearBox.Text.Trim());

                if (table.Rows.Count > 0)
                {
                    foreach (DataRow dr in table.Rows)
                    {
                        string subject = dr["subject"].ToString();
                        if (!subjectRows.ContainsKey(subject)) continue;
                        var row    = subjectRows[subject];
                        row.Cat1.Text = FormatExamNumber(dr["cat1"]);
                        row.Cat2.Text = FormatExamNumber(dr["cat2"]);
                        row.Cat3.Text = FormatExamNumber(dr["cat3"]);
                        // Assuming exam_score from db is 50% scaled, multiply by 2 to get raw, or if it was raw, just display it.
                        // Based on Grading.razor, the DB stores the 50% scaled score!
                        if (decimal.TryParse(dr["exam_score"].ToString(), out decimal savedExam))
                        {
                            row.RawExam.Text = (savedExam * 2).ToString("0.00");
                        }
                        row.Remark.Text = dr["remark"].ToString();
                    }
                    statusLabel.Text = $"Loaded {table.Rows.Count} existing result(s) for this term.";
                }
            }
            catch { /* best effort */ }
        }

        private static string FormatExamNumber(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            decimal parsed;
            return decimal.TryParse(value.ToString(), out parsed)
                ? parsed.ToString("0.00")
                : value.ToString();
        }

        private async void SaveAllResults()
        {
            if (_isSavingResults) return;

            try
            {
                if (!AuthService.RequireWriteAccess("Academics.ExamResults.Manage", "Save Exam Results"))
                    return;

                SetExamBusy(true, "Saving results...");
                if (string.IsNullOrWhiteSpace(termBox.Text)) return;
                if (!FormValidationHelper.ValidateRequired(yearBox, "Year")) return;

                bool isValidExamPeriod = false;
                try
                {
                    using (var conn = new Microsoft.Data.SqlClient.SqlConnection(Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
                    {
                        await conn.OpenAsync();
                        var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM ExamSetups WHERE Term = @Term AND [Year] = @Year", conn);
                        cmd.Parameters.AddWithValue("@Term", termBox.Text);
                        cmd.Parameters.AddWithValue("@Year", yearBox.Text);
                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        if (count > 0) isValidExamPeriod = true;
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Exam setup verification failed", ex);
                    ConfirmationHelper.ShowWarning(
                        "The system could not verify the selected exam setup because the database check failed. Please reload the page and try again.",
                        "Exam Setup Check Failed");
                    return;
                }

                if (!isValidExamPeriod)
                {
                    ConfirmationHelper.ShowWarning($"No active Exam Setup found for Term: '{termBox.Text}' and Year: '{yearBox.Text}'. Please configure it in Exam Setups first.", "Invalid Exam Period");
                    return;
                }

                statusLabel.Text = "Saving results...";
                statusLabel.ForeColor = TextCol;

                var results = new List<KingdomPrep.Shared.Models.ExamResult>();

                if (_entryMode == "Student")
                {
                    if (studentComboBox.SelectedIndex < 0)
                    {
                        UIHelper.ShowWarning("Please select a student.", "Missing Student");
                        return;
                    }
                    var student = _currentStudents[studentComboBox.SelectedIndex];

                    foreach (var kvp in subjectRows)
                    {
                        string subject = kvp.Key;
                        var row = kvp.Value;
                        if (!decimal.TryParse(row.SbaTotal.Text, out decimal _) && !decimal.TryParse(row.RawExam.Text, out decimal _))
                            continue;

                        decimal cat1 = decimal.TryParse(row.Cat1.Text, out decimal c1) ? c1 : 0;
                        decimal cat2 = decimal.TryParse(row.Cat2.Text, out decimal c2) ? c2 : 0;
                        decimal cat3 = decimal.TryParse(row.Cat3.Text, out decimal c3) ? c3 : 0;
                        decimal examRaw = decimal.TryParse(row.RawExam.Text, out decimal ex) ? ex : 0;
                        decimal examScaled = Math.Round(Math.Min(100m, Math.Max(0m, examRaw)) / 2m, 2);


                        results.Add(new KingdomPrep.Shared.Models.ExamResult
                        {
                            StudentId  = student.StudentID,
                            ClassId    = student.ClassID,
                            Subject    = subject,
                            Term       = termBox.Text,
                            Year       = yearBox.Text.Trim(),
                            Category1       = cat1,
                            Category2       = cat2,
                            Category3       = cat3,
                            ExamScore  = examScaled,


                            Remark     = row.Remark.Text
                        });
                    }
                }
                else
                {
                    if (classComboBox.SelectedIndex < 0) return;
                    if (subjectComboBox.SelectedIndex < 0) return;

                    string sClass = classComboBox.Text;
                    string subject = subjectComboBox.Text;

                    foreach (var kvp in subjectRows)
                    {
                        string studentName = kvp.Key;
                        var row = kvp.Value;
                        if (!decimal.TryParse(row.SbaTotal.Text, out decimal _) && !decimal.TryParse(row.RawExam.Text, out decimal _))
                            continue;

                        var st = _currentStudents.Find(x => x.FullName == studentName);
                        if (st == null) continue;

                        decimal cat1 = decimal.TryParse(row.Cat1.Text, out decimal c1) ? c1 : 0;
                        decimal cat2 = decimal.TryParse(row.Cat2.Text, out decimal c2) ? c2 : 0;
                        decimal cat3 = decimal.TryParse(row.Cat3.Text, out decimal c3) ? c3 : 0;
                        decimal examRaw = decimal.TryParse(row.RawExam.Text, out decimal ex) ? ex : 0;
                        decimal examScaled = Math.Round(Math.Min(100m, Math.Max(0m, examRaw)) / 2m, 2);


                        results.Add(new KingdomPrep.Shared.Models.ExamResult
                        {
                            StudentId  = st.StudentID,
                            ClassId    = sClass,
                            Subject    = subject,
                            Term       = termBox.Text,
                            Year       = yearBox.Text.Trim(),
                            Category1       = cat1,
                            Category2       = cat2,
                            Category3       = cat3,
                            ExamScore  = examScaled,


                            Remark     = row.Remark.Text
                        });
                    }
                }

                if (results.Count == 0)
                {
                    ConfirmationHelper.ShowWarning("No valid scores to save.", "Exam Submission");
                    statusLabel.Text = "No valid scores to save.";
                    return;
                }

                var response = await _examService.SaveResultsAsync(results);
                bool success = response.Success;
                if (success)
                {
                    UIHelper.ShowSuccess("Exam results submitted successfully. They will appear on the parent portal after headmaster upload.", "Exam Submission");
                    statusLabel.Text = $"Successfully submitted {results.Count} result(s).";
                }
                else
                {
                    UIHelper.ShowError("Failed to save results.", "Exam Submission");
                    statusLabel.Text = "Failed to save results.";
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("SaveAllResults failed", ex);
                statusLabel.Text = "Save error.";
                UIHelper.ShowError("Save exam results failed: " + ex.Message, "Exam Results");
            }
            finally
            {
                SetExamBusy(false);
            }
        }

        private void SetExamBusy(bool busy, string status = null)
        {
            _isSavingResults = busy;
            UseWaitCursor = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (subjectGrid != null) subjectGrid.Enabled = !busy;
            if (studentComboBox != null) studentComboBox.Enabled = !busy;
            if (classComboBox != null) classComboBox.Enabled = !busy;
            if (subjectComboBox != null) subjectComboBox.Enabled = !busy;
            if (termBox != null) termBox.Enabled = !busy;
            if (yearBox != null) yearBox.Enabled = !busy;
            if (statusLabel != null && !string.IsNullOrWhiteSpace(status))
                statusLabel.Text = status;
        }

        private void ClearForm()
        {
            studentComboBox.SelectedIndex = -1;
            termBox.SelectedIndex = -1;
            yearBox.Text        = DateTime.Today.Year.ToString();

            foreach (var row in subjectRows.Values)
            {
                row.Cat1.Text       = "";
                row.Cat2.Text       = "";
                row.Cat3.Text       = "";
                row.RawExam.Text    = "";
                row.SbaTotal.Text   = "-";
                row.ScaledExam.Text = "-";
                row.Total.Text      = "-";
                row.Grade.Text      = "-";
                row.Remark.Text     = "";
            }

            statusLabel.Text = "Form cleared. Enter a student ID to begin.";
            UpdateSummary();
        }
    }
}
