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
        private readonly ExamService _examService;
        private readonly StudentService _studentService;

        private Guna.UI.WinForms.GunaTextBox studentIdBox;
        private Guna.UI.WinForms.GunaTextBox studentNameBox;
        private Guna.UI.WinForms.GunaTextBox classBox;
        private Guna.UI.WinForms.GunaComboBox termBox;
        private Guna.UI.WinForms.GunaTextBox yearBox;
        private Label statusLabel;
        private Label completionLabel;
        private Label averageLabel;
        private TableLayoutPanel subjectGrid;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SurfaceAlt = UiTheme.SurfaceAlt;
        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color Gold = UiTheme.Gold;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        private readonly string[] subjects =
        {
            "MATHEMATICS", "INT. SCIENCE", "ENGLISH LANGUAGE", "SOCIAL STUDIES",
            "COMPUTING", "REL. & MORAL EDU.", "CARRER TECH.", "CREATIVE ART", "GHANAIAN LANG."
        };

        private readonly Dictionary<string, SubjectRows> subjectRows = new Dictionary<string, SubjectRows>();

        private class SubjectRows
        {
            public Guna.UI.WinForms.GunaTextBox Cat1;
            public Guna.UI.WinForms.GunaTextBox Cat2;
            public Guna.UI.WinForms.GunaTextBox Cat3;
            public Guna.UI.WinForms.GunaTextBox Exam;
            public Label Total;
            public Label Grade;
            public Label Remark;
        }

        public EXAMS()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var examRepo = new ExamRepository(AppConfig.ConnectionString);
            _examService = new ExamService(examRepo);
            
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepo = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepo);

            BuildExamSubmissionView();
            NavigationSidebar.AddTo(this);
            UiTheme.Apply(this);
        }

        private void BuildExamSubmissionView()
        {
            SuspendLayout();
            Controls.Clear();
            subjectRows.Clear();

            Text = "Exam Submission";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1250, 780);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = PageBackColor,
                Padding = new Padding(252, 24, 24, 22)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildStudentPanel(), 0, 1);
            root.Controls.Add(BuildSubjectEntryPanel(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            title.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "Exam Submission",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            title.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Enter all subject scores once, review calculated grades, then publish results",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 0, 0)
            };
            nav.Controls.Add(CreateButton("View Results", () => new EXAMSVIEW().Show(), true, 124));
            nav.Controls.Add(CreateButton("Dashboard", () => new frmDashboard().Show(), false, 112));

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(nav, 1, 0);
            return header;
        }

        private Control BuildStudentPanel()
        {
            var panel = CreateSurfacePanel(new Padding(20), new Padding(0, 0, 0, 14));
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, BackColor = SurfaceColor };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            studentIdBox = CreateTextInput();
            studentIdBox.TextChanged += (sender, args) => LookupStudent();
            studentNameBox = CreateTextInput(true);
            classBox = CreateTextInput(true);
            termBox = CreateTermInput();
            yearBox = CreateTextInput();
            yearBox.Text = DateTime.Today.Year.ToString();

            layout.Controls.Add(CreateField("Student ID", studentIdBox), 0, 0);
            layout.Controls.Add(CreateField("Student Name", studentNameBox), 1, 0);
            layout.Controls.Add(CreateField("Class", classBox), 2, 0);
            layout.Controls.Add(CreateField("Term", termBox), 3, 0);
            layout.Controls.Add(CreateField("Year", yearBox), 4, 0);

            var summary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = SurfaceColor };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            completionLabel = CreateSummaryLabel("0 of " + subjects.Length + " subjects ready");
            averageLabel = CreateSummaryLabel("Average: --");
            summary.Controls.Add(completionLabel, 0, 0);
            summary.Controls.Add(averageLabel, 1, 0);
            layout.Controls.Add(summary, 5, 0);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Enter a student ID to load the learner before submitting scores.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(statusLabel, 0, 1);
            layout.SetColumnSpan(statusLabel, 6);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildSubjectEntryPanel()
        {
            var panel = CreateSurfacePanel(new Padding(0), Padding.Empty);
            var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = SurfaceColor };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            shell.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Subject Scores",
                Padding = new Padding(20, 0, 0, 0),
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            subjectGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = subjects.Length + 1,
                BackColor = SurfaceColor,
                Padding = new Padding(14, 0, 14, 14)
            };
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
            subjectGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            for (int i = 0; i < subjects.Length; i++) subjectGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / subjects.Length));

            string[] headers = { "Subject", "Test (40)", "Group (10)", "Project (10)", "Exam (100)", "Total", "Grade", "Remark" };
            for (int i = 0; i < headers.Length; i++)
            {
                subjectGrid.Controls.Add(CreateGridHeader(headers[i]), i, 0);
            }

            for (int i = 0; i < subjects.Length; i++)
            {
                AddSubjectRow(subjects[i], i + 1);
            }

            shell.Controls.Add(subjectGrid, 0, 1);
            panel.Controls.Add(shell);
            return panel;
        }

        private void AddSubjectRow(string subject, int rowIndex)
        {
            subjectGrid.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = subject,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = rowIndex % 2 == 0 ? SurfaceAlt : SurfaceColor
            }, 0, rowIndex);

            var row = new SubjectRows
            {
                Cat1 = CreateScoreInput(),
                Cat2 = CreateScoreInput(),
                Cat3 = CreateScoreInput(),
                Exam = CreateScoreInput(),
                Total = CreateGridValue("-"),
                Grade = CreateGridValue("-"),
                Remark = CreateGridValue("-")
            };

            row.Cat1.TextChanged += (sender, args) => CalculateRow(subject);
            row.Cat2.TextChanged += (sender, args) => CalculateRow(subject);
            row.Cat3.TextChanged += (sender, args) => CalculateRow(subject);
            row.Exam.TextChanged += (sender, args) => CalculateRow(subject);

            subjectGrid.Controls.Add(row.Cat1, 1, rowIndex);
            subjectGrid.Controls.Add(row.Cat2, 2, rowIndex);
            subjectGrid.Controls.Add(row.Cat3, 3, rowIndex);
            subjectGrid.Controls.Add(row.Exam, 4, rowIndex);
            subjectGrid.Controls.Add(row.Total, 5, rowIndex);
            subjectGrid.Controls.Add(row.Grade, 6, rowIndex);
            subjectGrid.Controls.Add(row.Remark, 7, rowIndex);
            subjectRows[subject] = row;
        }

        private Control BuildActions()
        {
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 0, 0)
            };
            actions.Controls.Add(CreateButton("Save All Results", SaveAllResults, true, 160));
            actions.Controls.Add(CreateButton("Clear Form", ClearForm, false, 120));
            actions.Controls.Add(CreateButton("View Reports", () => new EXAMSVIEW().Show(), false, 124));
            return actions;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = padding, Margin = margin };
        }

        private Control CreateField(string label, Control input)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = SurfaceColor, Padding = new Padding(0, 0, 10, 0) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 8.75F), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            input.Dock = DockStyle.Fill;
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private Label CreateSummaryLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = SurfaceAlt,
                Margin = new Padding(6, 20, 0, 0)
            };
        }

        private Label CreateGridHeader(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                BackColor = Navy,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 1, 1)
            };
        }

        private Label CreateGridValue(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(2),
                BackColor = SurfaceAlt
            };
        }

        private Guna.UI.WinForms.GunaTextBox CreateTextInput(bool readOnly = false)
        {
            return new Guna.UI.WinForms.GunaTextBox
            {
                Height = 34,
                ReadOnly = readOnly,
                BaseColor = readOnly ? SurfaceAlt : Color.White,
                BorderColor = BorderColor,
                FocusedBorderColor = Gold,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10F),
                Radius = 4
            };
        }

        private Guna.UI.WinForms.GunaComboBox CreateTermInput()
        {
            var combo = new Guna.UI.WinForms.GunaComboBox
            {
                Height = 34,
                BaseColor = Color.White,
                BorderColor = BorderColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Radius = 4
            };
            combo.Items.AddRange(new object[] { "TERM 1", "TERM 2", "TERM 3" });
            return combo;
        }

        private Guna.UI.WinForms.GunaTextBox CreateScoreInput()
        {
            var input = CreateTextInput();
            input.TextAlign = HorizontalAlignment.Center;
            input.Margin = new Padding(2);
            return input;
        }

        private Button CreateButton(string text, Action action, bool primary, int width)
        {
            var button = new Button
            {
                Width = width,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = primary ? Navy : SurfaceColor,
                ForeColor = primary ? Color.White : TextColor
            };
            button.FlatAppearance.BorderColor = primary ? Navy : BorderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            button.Click += (sender, args) => action();
            return button;
        }

        private void CalculateRow(string subject)
        {
            var row = subjectRows[subject];
            if (!TryReadScore(row.Cat1, 40m, out _) ||
                !TryReadScore(row.Cat2, 10m, out _) ||
                !TryReadScore(row.Cat3, 10m, out _) ||
                !TryReadScore(row.Exam, 100m, out _))
            {
                row.Total.Text = "-";
                row.Grade.Text = "-";
                row.Remark.Text = "-";
                UpdateSummary();
                return;
            }

            var tempResult = new ExamResult
            {
                Category1 = decimal.Parse(row.Cat1.Text),
                Category2 = decimal.Parse(row.Cat2.Text),
                Category3 = decimal.Parse(row.Cat3.Text),
                ExamScore = decimal.Parse(row.Exam.Text)
            };
            tempResult.Calculate();

            row.Total.Text = tempResult.TotalScore.ToString("0.0");
            row.Grade.Text = tempResult.Grade;
            row.Remark.Text = tempResult.Remark;
            UpdateSummary();
        }

        private bool TryReadScore(Guna.UI.WinForms.GunaTextBox input, decimal max, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(input.Text)) return false;
            return decimal.TryParse(input.Text.Trim(), out value) && value >= 0m && value <= max;
        }

        private void UpdateSummary()
        {
            int ready = 0;
            decimal total = 0m;
            foreach (var row in subjectRows.Values)
            {
                if (decimal.TryParse(row.Total.Text, out decimal score))
                {
                    ready++;
                    total += score;
                }
            }

            completionLabel.Text = ready + " of " + subjects.Length + " subjects ready";
            averageLabel.Text = ready == 0 ? "Average: --" : "Average: " + (total / ready).ToString("0.0");
        }

        private async void LookupStudent()
        {
            string studentId = studentIdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(studentId))
            {
                studentNameBox.Text = "";
                classBox.Text = "";
                return;
            }

            try
            {
                var student = await _studentService.GetStudentAsync(studentId);
                if (student != null)
                {
                    studentNameBox.Text = student.FullName;
                    classBox.Text = student.ClassID;
                    statusLabel.Text = "Student loaded. Checking for existing results...";
                    await LoadExistingResults(studentId);
                    return;
                }

                studentNameBox.Text = "";
                classBox.Text = "";
                statusLabel.Text = "Student not found.";
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
                var table = await _examService.GetExistingResultsForStudentAsync(studentId, termBox.Text, yearBox.Text.Trim());
                if (table.Rows.Count > 0)
                {
                    foreach (DataRow dr in table.Rows)
                    {
                        string subject = dr["subject"].ToString();
                        if (subjectRows.ContainsKey(subject))
                        {
                            var row = subjectRows[subject];
                            row.Cat1.Text = dr["cat1"].ToString();
                            row.Cat2.Text = dr["cat2"].ToString();
                            row.Cat3.Text = dr["cat3"].ToString();
                            row.Exam.Text = dr["exam_score"].ToString();
                        }
                    }
                    statusLabel.Text = $"Loaded {table.Rows.Count} existing result(s) for this term.";
                }
            }
            catch { /* Best effort */ }
        }

        private async void SaveAllResults()
        {
            if (string.IsNullOrWhiteSpace(studentNameBox.Text) || string.IsNullOrWhiteSpace(classBox.Text))
            {
                UIHelper.ShowWarning("Load a valid student first.", "Exam Submission");
                return;
            }

            if (termBox.SelectedIndex < 0 || string.IsNullOrWhiteSpace(yearBox.Text))
            {
                UIHelper.ShowWarning("Select the term and enter the academic year.", "Exam Submission");
                return;
            }

            var results = new List<ExamResult>();
            foreach (var entry in subjectRows)
            {
                var row = entry.Value;
                if (!decimal.TryParse(row.Total.Text, out _)) continue;

                results.Add(new ExamResult
                {
                    StudentId = studentIdBox.Text.Trim(),
                    StudentName = studentNameBox.Text.Trim(),
                    ClassId = classBox.Text.Trim(),
                    Subject = entry.Key,
                    Term = termBox.Text,
                    Year = yearBox.Text.Trim(),
                    Category1 = decimal.Parse(row.Cat1.Text),
                    Category2 = decimal.Parse(row.Cat2.Text),
                    Category3 = decimal.Parse(row.Cat3.Text),
                    ExamScore = decimal.Parse(row.Exam.Text)
                });
            }

            if (results.Count == 0)
            {
                UIHelper.ShowWarning("No valid subject scores to save.", "Exam Submission");
                return;
            }

            statusLabel.Text = "Saving results...";
            var (success, message) = await _examService.SaveResultsAsync(results);

            if (success)
            {
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Exam Submission");
            }
            else
            {
                statusLabel.Text = "Save failed.";
                UIHelper.ShowError(message, "Exam Submission");
            }
        }

        private void ClearForm()
        {
            studentIdBox.Text = "";
            studentNameBox.Text = "";
            classBox.Text = "";
            if (termBox.Items.Count > 0) termBox.SelectedIndex = -1;
            yearBox.Text = DateTime.Today.Year.ToString();

            foreach (var row in subjectRows.Values)
            {
                row.Cat1.Text = "";
                row.Cat2.Text = "";
                row.Cat3.Text = "";
                row.Exam.Text = "";
                row.Total.Text = "-";
                row.Grade.Text = "-";
                row.Remark.Text = "-";
            }

            statusLabel.Text = "Form cleared. Enter a student ID to begin.";
            UpdateSummary();
        }

        private async void termBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(studentIdBox.Text))
            {
                await LoadExistingResults(studentIdBox.Text.Trim());
            }
        }
    }
}
