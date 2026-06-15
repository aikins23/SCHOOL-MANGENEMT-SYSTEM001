using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EXAMS : Form
    {
        // ── Services ─────────────────────────────────────────────────────────
        private readonly ExamService    _examService;
        private readonly StudentService _studentService;

        // ── Header fields (plain WinForms — no Guna dependency) ──────────────
        private TextBox  studentIdBox;
        private TextBox  studentNameBox;
        private TextBox  classBox;
        private ComboBox termBox;
        private TextBox  yearBox;

        // ── Status / summary labels ───────────────────────────────────────────
        private Label statusLabel;
        private Label completionLabel;
        private Label averageLabel;

        // ── Subject grid ──────────────────────────────────────────────────────
        private TableLayoutPanel subjectGrid;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color PageBack  = UiTheme.Page;
        private static readonly Color Surface   = UiTheme.Surface;
        private static readonly Color SurfaceAlt= UiTheme.SurfaceAlt;
        private static readonly Color Navy      = UiTheme.Navy;
        private static readonly Color TextCol   = UiTheme.Text;
        private static readonly Color Muted     = UiTheme.Muted;
        private static readonly Color Border    = UiTheme.Border;
        private static readonly Color Primary   = Color.FromArgb(31, 99, 198);

        // ── Subject list ──────────────────────────────────────────────────────
        // Current subjects shown in the grid; defaults to the legacy list, replaced per class on lookup.
        private string[] subjects = Common.SubjectCatalog.LegacySubjects;

        // ── Per-subject row controls ──────────────────────────────────────────
        private readonly Dictionary<string, SubjectRows> subjectRows =
            new Dictionary<string, SubjectRows>();

        private class SubjectRows
        {
            public TextBox Cat1, Cat2, Cat3, Exam;
            public Label   Total, Grade, Remark;
        }

        // ─────────────────────────────────────────────────────────────────────
        public EXAMS()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("EXAMS", this)) return;

            var examRepo    = new ExamRepository(AppConfig.ConnectionString);
            _examService    = new ExamService(examRepo);

            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepo     = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepo);

            BuildExamSubmissionView();
            NavigationSidebar.AddTo(this);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Layout
        // ─────────────────────────────────────────────────────────────────────

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
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBack };
            title.Controls.Add(new Label
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                Text      = "Exam Submission",
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            title.Controls.Add(new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 26,
                Text      = "Enter subject scores, review calculated grades, then publish results",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var nav = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = PageBack,
                Padding       = new Padding(0, 14, 0, 0)
            };
            // Navigation handled by sidebar — only task-specific actions here
            nav.Controls.Add(MakePrimaryBtn("View Results", () => FormManager.ShowForm<EXAMSVIEW>(this), 112));
            nav.Controls.Add(MakeSecondaryBtn("Dashboard",  () => FormManager.ShowForm<frmDashboard>(this), 104));

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

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 7,
                RowCount    = 2,
                BackColor   = Surface
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Student ID
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   32));  // Student Name
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   18));  // Class
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));  // Term
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  90));  // Year
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,   8));  // gap
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   50));  // summary
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ── Inputs ────────────────────────────────────────────────────────
            studentIdBox           = MakeField(false);
            studentIdBox.TextChanged += (s, e) => LookupStudent();

            studentNameBox         = MakeField(readOnly: true);
            classBox               = MakeField(readOnly: true);

            termBox = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 10F),
                Margin        = new Padding(0, 0, 8, 0)
            };
            termBox.Items.AddRange(new object[] { "TERM 1", "TERM 2", "TERM 3" });
            termBox.SelectedIndexChanged += async (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(studentIdBox.Text))
                    await LoadExistingResults(studentIdBox.Text.Trim());
            };

            yearBox      = MakeField(false);
            yearBox.Text = DateTime.Today.Year.ToString();

            layout.Controls.Add(MakeLabeledField("Student ID",   studentIdBox), 0, 0);
            layout.Controls.Add(MakeLabeledField("Student Name", studentNameBox), 1, 0);
            layout.Controls.Add(MakeLabeledField("Class",        classBox), 2, 0);
            layout.Controls.Add(MakeLabeledField("Term",         termBox), 3, 0);
            layout.Controls.Add(MakeLabeledField("Year",         yearBox), 4, 0);
            // col 5 = gap

            // ── Summary chips ──────────────────────────────────────────────
            var summaryRow = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                BackColor   = Surface,
                Padding     = new Padding(0, 8, 0, 0)
            };
            summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            completionLabel = MakeSummaryChip("0 of " + subjects.Length + " subjects");
            averageLabel    = MakeSummaryChip("Average: --");
            summaryRow.Controls.Add(completionLabel, 0, 0);
            summaryRow.Controls.Add(averageLabel,    1, 0);
            layout.Controls.Add(summaryRow, 6, 0);

            // ── Status bar ────────────────────────────────────────────────────
            statusLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Enter a student ID to load the learner before submitting scores.",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(statusLabel, 0, 1);
            layout.SetColumnSpan(statusLabel, 7);

            card.Controls.Add(layout);
            return card;
        }

        private Control BuildSubjectEntryPanel()
        {
            var card = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding     = new Padding(0)
            };

            var shell = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                BackColor   = Surface
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            shell.Controls.Add(new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Subject Scores",
                Padding   = new Padding(20, 0, 0, 0),
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            subjectGrid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 8,
                RowCount    = subjects.Length + 1,
                BackColor   = Surface,
                Padding     = new Padding(14, 0, 14, 14)
            };
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // Subject name
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   13)); // Test (40)
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   13)); // Group (10)
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   13)); // Project (10)
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   13)); // Exam (100)
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   13)); // Total
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,    9)); // Grade
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   26)); // Remark
            RebuildSubjectGrid(subjects);

            shell.Controls.Add(subjectGrid, 0, 1);
            card.Controls.Add(shell);
            return card;
        }

        private void RebuildSubjectGrid(IReadOnlyList<string> subjectList)
        {
            subjects = new List<string>(subjectList).ToArray();

            subjectGrid.SuspendLayout();
            subjectGrid.Controls.Clear();
            subjectGrid.RowStyles.Clear();
            subjectRows.Clear();

            subjectGrid.RowCount = subjects.Length + 1;
            subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            for (int i = 0; i < subjects.Length; i++)
                subjectGrid.RowStyles.Add(new RowStyle(SizeType.Percent,
                    subjects.Length == 0 ? 100F : 100F / subjects.Length));

            string[] headers = { "Subject", "Test (40)", "Group (10)", "Project (10)", "Exam (100)", "Total", "Grade", "Remark" };
            for (int i = 0; i < headers.Length; i++)
                subjectGrid.Controls.Add(MakeGridHeader(headers[i]), i, 0);

            for (int i = 0; i < subjects.Length; i++)
                AddSubjectRow(subjects[i], i + 1);

            if (completionLabel != null)
                completionLabel.Text = "0 of " + subjects.Length + " subjects";

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
            actions.Controls.Add(MakePrimaryBtn("Save All Results", SaveAllResults, 148));
            actions.Controls.Add(MakeSecondaryBtn("Clear Form",  ClearForm,               112));
            actions.Controls.Add(MakeSecondaryBtn("View Reports", () => FormManager.ShowForm<EXAMSVIEW>(this), 116));
            return actions;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Control factories
        // ─────────────────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────────────────
        // Subject row building
        // ─────────────────────────────────────────────────────────────────────

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
                Cat1   = MakeScoreInput(),
                Cat2   = MakeScoreInput(),
                Cat3   = MakeScoreInput(),
                Exam   = MakeScoreInput(),
                Total  = MakeGridValue("-"),
                Grade  = MakeGridValue("-"),
                Remark = MakeGridValue("-")
            };

            row.Cat1.TextChanged += (s, e) => CalculateRow(subject);
            row.Cat2.TextChanged += (s, e) => CalculateRow(subject);
            row.Cat3.TextChanged += (s, e) => CalculateRow(subject);
            row.Exam.TextChanged += (s, e) => CalculateRow(subject);

            subjectGrid.Controls.Add(row.Cat1,   1, rowIndex);
            subjectGrid.Controls.Add(row.Cat2,   2, rowIndex);
            subjectGrid.Controls.Add(row.Cat3,   3, rowIndex);
            subjectGrid.Controls.Add(row.Exam,   4, rowIndex);
            subjectGrid.Controls.Add(row.Total,  5, rowIndex);
            subjectGrid.Controls.Add(row.Grade,  6, rowIndex);
            subjectGrid.Controls.Add(row.Remark, 7, rowIndex);
            subjectRows[subject] = row;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Business logic (unchanged; Guna property access is gone)
        // ─────────────────────────────────────────────────────────────────────

        private void CalculateRow(string subject)
        {
            var row = subjectRows[subject];
            if (!TryReadScore(row.Cat1, 40m,  out _) ||
                !TryReadScore(row.Cat2, 10m,  out _) ||
                !TryReadScore(row.Cat3, 10m,  out _) ||
                !TryReadScore(row.Exam, 100m, out _))
            {
                row.Total.Text  = "-";
                row.Grade.Text  = "-";
                row.Remark.Text = "-";
                UpdateSummary();
                return;
            }

            var result = new ExamResult
            {
                Category1 = decimal.Parse(row.Cat1.Text),
                Category2 = decimal.Parse(row.Cat2.Text),
                Category3 = decimal.Parse(row.Cat3.Text),
                ExamScore = decimal.Parse(row.Exam.Text)
            };
            result.Calculate();

            row.Total.Text  = result.TotalScore.ToString("0.0");
            row.Grade.Text  = result.Grade;
            row.Remark.Text = result.Remark;
            UpdateSummary();
        }

        private bool TryReadScore(TextBox input, decimal max, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(input.Text)) return false;
            return decimal.TryParse(input.Text.Trim(), out value) && value >= 0m && value <= max;
        }

        private void UpdateSummary()
        {
            int ready = 0; decimal total = 0m;
            foreach (var row in subjectRows.Values)
            {
                if (decimal.TryParse(row.Total.Text, out decimal score))
                { ready++; total += score; }
            }
            completionLabel.Text = $"{ready} of {subjects.Length} subjects";
            averageLabel.Text    = ready == 0 ? "Average: --" : $"Average: {(total / ready):0.0}";
        }

        private async void LookupStudent()
        {
            string sid = studentIdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(sid))
            {
                studentNameBox.Text = "";
                classBox.Text       = "";
                return;
            }

            try
            {
                var student = await _studentService.GetStudentAsync(sid);
                if (student != null)
                {
                    if (AuthService.IsTeacher)
                    {
                        string myClass = await AuthService.GetCurrentTeacherClassAsync();
                        if (!string.IsNullOrEmpty(myClass) &&
                            !string.Equals(student.ClassID, myClass, StringComparison.OrdinalIgnoreCase))
                        {
                            studentNameBox.Text = "";
                            classBox.Text       = "";
                            statusLabel.Text    = $"Student {sid} is in {student.ClassID}, not your class ({myClass}).";
                            UIHelper.ShowWarning(
                                $"You can only enter scores for students in {myClass}. {student.FullName} is in {student.ClassID}.",
                                "Out of scope");
                            return;
                        }
                    }

                    studentNameBox.Text = student.FullName;
                    classBox.Text       = student.ClassID;
                    RebuildSubjectGrid(Common.SubjectCatalog.SubjectsForClass(student.ClassID));
                    statusLabel.Text    = "Student loaded. Checking for existing results…";
                    await LoadExistingResults(sid);
                    return;
                }

                studentNameBox.Text = "";
                classBox.Text       = "";
                statusLabel.Text    = "Student not found.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Lookup failed.";
                UIHelper.ShowError("Lookup error: " + ex.Message, "Exams");
            }
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
                        row.Cat1.Text = dr["cat1"].ToString();
                        row.Cat2.Text = dr["cat2"].ToString();
                        row.Cat3.Text = dr["cat3"].ToString();
                        row.Exam.Text = dr["exam_score"].ToString();
                    }
                    statusLabel.Text = $"Loaded {table.Rows.Count} existing result(s) for this term.";
                }
            }
            catch { /* best effort */ }
        }

        private async void SaveAllResults()
        {
            try
            {
                if (!FormValidationHelper.ValidateRequired(studentIdBox,   "Student ID"))   return;
                if (!FormValidationHelper.ValidateRequired(studentNameBox, "Student Name")) return;
                if (!FormValidationHelper.ValidateRequired(classBox,       "Class"))        return;
                if (!FormValidationHelper.ValidateComboBox(termBox,        "Term"))         return;
                if (!FormValidationHelper.ValidateRequired(yearBox,        "Year"))         return;

                var results = new List<ExamResult>();
                foreach (var entry in subjectRows)
                {
                    var row = entry.Value;
                    if (!decimal.TryParse(row.Total.Text, out _)) continue;

                    results.Add(new ExamResult
                    {
                        StudentId   = studentIdBox.Text.Trim(),
                        StudentName = studentNameBox.Text.Trim(),
                        ClassId     = classBox.Text.Trim(),
                        Subject     = entry.Key,
                        Term        = termBox.Text,
                        Year        = yearBox.Text.Trim(),
                        Category1   = decimal.Parse(entry.Value.Cat1.Text),
                        Category2   = decimal.Parse(entry.Value.Cat2.Text),
                        Category3   = decimal.Parse(entry.Value.Cat3.Text),
                        ExamScore   = decimal.Parse(entry.Value.Exam.Text)
                    });
                }

                if (results.Count == 0)
                {
                    ConfirmationHelper.ShowWarning("No valid subject scores to save.", "Exam Submission");
                    return;
                }

                if (!ConfirmationHelper.ConfirmBulkOperation("save exam results", results.Count)) return;

                statusLabel.Text = "Saving results…";
                var (success, message) = await _examService.SaveResultsAsync(results);

                if (success)
                {
                    LoggerHelper.LogInfo($"Saved {results.Count} exam results for student {studentIdBox.Text}");
                    statusLabel.Text = message;
                    UIHelper.ShowSuccess(message, "Exam Submission");
                }
                else
                {
                    statusLabel.Text = "Save failed.";
                    UIHelper.ShowError(message, "Exam Submission");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("SaveAllResults failed", ex);
                statusLabel.Text = "Save error.";
                UIHelper.ShowError("Save exam results failed: " + ex.Message, "Exam Results");
            }
        }

        private void ClearForm()
        {
            studentIdBox.Text   = "";
            studentNameBox.Text = "";
            classBox.Text       = "";
            termBox.SelectedIndex = -1;
            yearBox.Text        = DateTime.Today.Year.ToString();

            foreach (var row in subjectRows.Values)
            {
                row.Cat1.Text   = "";
                row.Cat2.Text   = "";
                row.Cat3.Text   = "";
                row.Exam.Text   = "";
                row.Total.Text  = "-";
                row.Grade.Text  = "-";
                row.Remark.Text = "-";
            }

            statusLabel.Text = "Form cleared. Enter a student ID to begin.";
            UpdateSummary();
        }
    }
}
