using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EXAMS : Form
    {
        private readonly kum Aikins = new kum();
        private TextBox studentIdBox;
        private TextBox studentNameBox;
        private TextBox classBox;
        private ComboBox subjectBox;
        private ComboBox termBox;
        private TextBox yearBox;
        private TextBox cat1Box;
        private TextBox cat2Box;
        private TextBox cat3Box;
        private TextBox totalCatBox;
        private TextBox examScoreBox;
        private TextBox grandTotalBox;
        private Label gradeValueLabel;
        private Label remarkValueLabel;
        private Label statusLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SidebarBackColor = UiTheme.Navy;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color AccentColor = UiTheme.GoldSoft;
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        private readonly string[] subjects =
        {
            "MATHEMATICS",
            "INT. SCIENCE",
            "ENGLISH LANGUAGE",
            "SOCIAL STUDIES",
            "COMPUTING",
            "REL. & MORAL EDU.",
            "CARRER TECH.",
            "CREATIVE ART",
            "GHANAIAN LANG."
        };

        public EXAMS()
        {
            InitializeComponent();
            BuildModernExamView();
        }

        private void BuildModernExamView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Exams";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 680);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildStudentPanel(), 0, 1);
            root.Controls.Add(BuildScoresPanel(), 0, 2);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Exam Results",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Enter scores, calculate grades, and publish results",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 0, 0)
            };
            actions.Controls.Add(CreatePrimaryButton("View Results", () => new EXAMSVIEW().Show()));
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () =>
            {
                Close();
                new frmDashboard().Show();
            }));

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildStudentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(18)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
                BackColor = SurfaceColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            studentIdBox = CreateTextBox();
            studentNameBox = CreateTextBox(true);
            classBox = CreateTextBox(true);
            subjectBox = CreateComboBox(subjects);
            termBox = CreateComboBox(new[] { "TERM 1", "TERM 2", "TERM 3" });
            yearBox = CreateTextBox();
            yearBox.Text = DateTime.Today.Year.ToString();

            studentIdBox.TextChanged += (sender, args) => LookupStudent();

            layout.Controls.Add(CreateField("Student ID", studentIdBox), 0, 0);
            layout.Controls.Add(CreateField("Student Name", studentNameBox), 1, 0);
            layout.Controls.Add(CreateField("Class", classBox), 2, 0);
            layout.Controls.Add(CreateField("Subject", subjectBox), 3, 0);
            layout.Controls.Add(CreateField("Term", termBox), 4, 0);
            layout.Controls.Add(CreateField("Year", yearBox), 5, 0);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F),
                Text = "Enter a student ID to load the student name and class."
            };
            layout.Controls.Add(statusLabel, 0, 1);
            layout.SetColumnSpan(statusLabel, 6);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildScoresPanel()
        {
            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

            shell.Controls.Add(BuildScoreEntryPanel(), 0, 0);
            shell.Controls.Add(BuildResultSummaryPanel(), 1, 0);
            return shell;
        }

        private Control BuildScoreEntryPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(18),
                Margin = new Padding(0, 14, 14, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                BackColor = SurfaceColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            cat1Box = CreateTextBox();
            cat2Box = CreateTextBox();
            cat3Box = CreateTextBox();
            totalCatBox = CreateTextBox(true);
            examScoreBox = CreateTextBox();
            grandTotalBox = CreateTextBox(true);

            cat1Box.TextChanged += (sender, args) => TryCalculateScores(false);
            cat2Box.TextChanged += (sender, args) => TryCalculateScores(false);
            cat3Box.TextChanged += (sender, args) => TryCalculateScores(false);
            examScoreBox.TextChanged += (sender, args) => TryCalculateScores(false);

            layout.Controls.Add(CreateField("CAT 1 (30)", cat1Box), 0, 0);
            layout.Controls.Add(CreateField("CAT 2 (30)", cat2Box), 1, 0);
            layout.Controls.Add(CreateField("CAT 3 (40)", cat3Box), 2, 0);
            layout.Controls.Add(CreateField("Total CAT", totalCatBox), 0, 1);
            layout.Controls.Add(CreateField("Exam Score (100)", examScoreBox), 1, 1);
            layout.Controls.Add(CreateField("Grand Total", grandTotalBox), 2, 1);
            layout.Controls.Add(CreatePrimaryButton("Calculate", () => TryCalculateScores(true)), 0, 2);
            layout.Controls.Add(CreatePrimaryButton("Save Result", SaveResult), 1, 2);
            layout.Controls.Add(CreateSecondaryButton("Clear", ClearForm), 2, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildResultSummaryPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(18),
                Margin = new Padding(0, 14, 0, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Result Summary",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            gradeValueLabel = CreateResultLabel("Grade");
            remarkValueLabel = CreateResultLabel("Remark");
            layout.Controls.Add(gradeValueLabel, 0, 1);
            layout.Controls.Add(remarkValueLabel, 0, 2);
            layout.Controls.Add(CreateSecondaryButton("Update Result", UpdateResult), 0, 3);
            layout.Controls.Add(CreateDangerButton("Delete Result", DeleteResult), 0, 4);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Records are matched by student, subject, term, and year.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 5);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0, 0, 12, 10),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            }, 0, 0);
            input.Dock = DockStyle.Fill;
            input.Height = 32;
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private TextBox CreateTextBox(bool readOnly = false)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = readOnly,
                BackColor = readOnly ? UiTheme.SurfaceAlt : SurfaceColor
            };
        }

        private ComboBox CreateComboBox(string[] items)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F)
            };
            combo.Items.AddRange(items);
            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            return combo;
        }

        private Label CreateResultLabel(string title)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = title + ": -",
                ForeColor = TextColor,
                BackColor = AccentColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 12)
            };
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = AccentColor;
            return button;
        }

        private Button CreateDangerButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = DangerColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 241, 242);
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private void LookupStudent()
        {
            if (studentIdBox == null || !int.TryParse(studentIdBox.Text.Trim(), out int studentId))
            {
                if (studentNameBox != null) studentNameBox.Text = "";
                if (classBox != null) classBox.Text = "";
                if (statusLabel != null) statusLabel.Text = "Enter a numeric student ID.";
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand("SELECT [FirstName], [LastName], [ClassID] FROM [Students] WHERE [StudentID] = ?", con))
                {
                    command.Parameters.AddWithValue("?", studentId);
                    con.Open();

                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            studentNameBox.Text = "";
                            classBox.Text = "";
                            statusLabel.Text = "Student not found.";
                            return;
                        }

                        studentNameBox.Text = reader["FirstName"] + " " + reader["LastName"];
                        classBox.Text = reader["ClassID"].ToString();
                        statusLabel.Text = "Student loaded.";
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Lookup failed.";
                MessageBox.Show("An error occurred: " + ex.Message, "Exams", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TryCalculateScores(bool showValidation)
        {
            if (!ReadScore(cat1Box, "CAT 1", 30m, showValidation, out decimal cat1) ||
                !ReadScore(cat2Box, "CAT 2", 30m, showValidation, out decimal cat2) ||
                !ReadScore(cat3Box, "CAT 3", 40m, showValidation, out decimal cat3) ||
                !ReadScore(examScoreBox, "Exam score", 100m, showValidation, out decimal examScore))
            {
                totalCatBox.Text = "";
                grandTotalBox.Text = "";
                gradeValueLabel.Text = "Grade: -";
                remarkValueLabel.Text = "Remark: -";
                return false;
            }

            decimal totalCat = cat1 + cat2 + cat3;
            decimal grandTotal = totalCat + examScore;
            string grade;
            string remark;

            if (grandTotal >= 150m)
            {
                grade = "A";
                remark = "EXCELLENT";
            }
            else if (grandTotal >= 140m)
            {
                grade = "B";
                remark = "VERY GOOD";
            }
            else if (grandTotal >= 120m)
            {
                grade = "C";
                remark = "CREDIT";
            }
            else if (grandTotal >= 90m)
            {
                grade = "D";
                remark = "PASS";
            }
            else
            {
                grade = "F";
                remark = "FAIL";
            }

            totalCatBox.Text = totalCat.ToString("0.##");
            grandTotalBox.Text = grandTotal.ToString("0.##");
            gradeValueLabel.Text = "Grade: " + grade;
            remarkValueLabel.Text = "Remark: " + remark;
            statusLabel.Text = "Scores calculated.";
            return true;
        }

        private bool ReadScore(TextBox box, string label, decimal maximum, bool showValidation, out decimal value)
        {
            value = 0m;
            if (box == null || string.IsNullOrWhiteSpace(box.Text))
            {
                return false;
            }

            if (!decimal.TryParse(box.Text.Trim(), out value) || value < 0m || value > maximum)
            {
                if (showValidation)
                {
                    MessageBox.Show(label + " must be between 0 and " + maximum.ToString("0") + ".", "Exams", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }

            return true;
        }

        private bool ValidateResultInput()
        {
            if (!int.TryParse(studentIdBox.Text.Trim(), out _))
            {
                MessageBox.Show("Enter a valid student ID.", "Exams", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(studentNameBox.Text) || string.IsNullOrWhiteSpace(classBox.Text))
            {
                MessageBox.Show("Load a valid student before saving the result.", "Exams", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (subjectBox.SelectedIndex < 0 || termBox.SelectedIndex < 0 || string.IsNullOrWhiteSpace(yearBox.Text))
            {
                MessageBox.Show("Select subject, term, and year.", "Exams", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return TryCalculateScores(true);
        }

        private void SaveResult()
        {
            if (!ValidateResultInput())
            {
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    if (ResultExists(con))
                    {
                        DialogResult result = MessageBox.Show("This result already exists. Update it now?", "Duplicate Result", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            UpdateResult(con);
                        }
                        return;
                    }

                    InsertResult(con);
                    statusLabel.Text = "Result saved.";
                    MessageBox.Show("Result saved successfully.", "Exams", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Result could not be saved: " + ex.Message, "Exams", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ResultExists(OleDbConnection con)
        {
            string query = "SELECT COUNT(*) FROM [dbo].[examss] WHERE [std_id] = ? AND [subject] = ? AND [term] = ? AND [year] = ?";
            using (OleDbCommand command = new OleDbCommand(query, con))
            {
                command.Parameters.AddWithValue("?", studentIdBox.Text.Trim());
                command.Parameters.AddWithValue("?", subjectBox.Text);
                command.Parameters.AddWithValue("?", termBox.Text);
                command.Parameters.AddWithValue("?", yearBox.Text.Trim());
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void InsertResult(OleDbConnection con)
        {
            string query = @"
INSERT INTO [dbo].[examss]
    ([std_id], [std_name], [std_class], [subject], [term], [year], [cat1], [cat2], [cat3], [tl_cat], [exam_score], [gt], [grade], [remark])
VALUES
    (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (OleDbCommand command = CreateResultCommand(query, con, includeWhereKeys: false))
            {
                command.ExecuteNonQuery();
            }
        }

        private void UpdateResult()
        {
            if (!ValidateResultInput())
            {
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    UpdateResult(con);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Result could not be updated: " + ex.Message, "Exams", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateResult(OleDbConnection con)
        {
            string query = @"
UPDATE [dbo].[examss]
SET [std_name] = ?, [std_class] = ?, [cat1] = ?, [cat2] = ?, [cat3] = ?, [tl_cat] = ?, [exam_score] = ?, [gt] = ?, [grade] = ?, [remark] = ?
WHERE [std_id] = ? AND [subject] = ? AND [term] = ? AND [year] = ?";

            using (OleDbCommand command = CreateResultCommand(query, con, includeWhereKeys: true))
            {
                int rows = command.ExecuteNonQuery();
                statusLabel.Text = rows > 0 ? "Result updated." : "No matching result found.";
                MessageBox.Show(rows > 0 ? "Result updated successfully." : "No matching result found.", "Exams", MessageBoxButtons.OK, rows > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        private OleDbCommand CreateResultCommand(string query, OleDbConnection con, bool includeWhereKeys)
        {
            var command = new OleDbCommand(query, con);
            if (includeWhereKeys)
            {
                command.Parameters.AddWithValue("?", studentNameBox.Text.Trim());
                command.Parameters.AddWithValue("?", classBox.Text.Trim());
                command.Parameters.AddWithValue("?", Convert.ToDecimal(cat1Box.Text));
                command.Parameters.AddWithValue("?", Convert.ToDecimal(cat2Box.Text));
                command.Parameters.AddWithValue("?", Convert.ToDecimal(cat3Box.Text));
                command.Parameters.AddWithValue("?", Convert.ToDecimal(totalCatBox.Text));
                command.Parameters.AddWithValue("?", Convert.ToDecimal(examScoreBox.Text));
                command.Parameters.AddWithValue("?", Convert.ToDecimal(grandTotalBox.Text));
                command.Parameters.AddWithValue("?", CurrentGrade());
                command.Parameters.AddWithValue("?", CurrentRemark());
                command.Parameters.AddWithValue("?", studentIdBox.Text.Trim());
                command.Parameters.AddWithValue("?", subjectBox.Text);
                command.Parameters.AddWithValue("?", termBox.Text);
                command.Parameters.AddWithValue("?", yearBox.Text.Trim());
                return command;
            }

            command.Parameters.AddWithValue("?", studentIdBox.Text.Trim());
            command.Parameters.AddWithValue("?", studentNameBox.Text.Trim());
            command.Parameters.AddWithValue("?", classBox.Text.Trim());
            command.Parameters.AddWithValue("?", subjectBox.Text);
            command.Parameters.AddWithValue("?", termBox.Text);
            command.Parameters.AddWithValue("?", yearBox.Text.Trim());
            command.Parameters.AddWithValue("?", Convert.ToDecimal(cat1Box.Text));
            command.Parameters.AddWithValue("?", Convert.ToDecimal(cat2Box.Text));
            command.Parameters.AddWithValue("?", Convert.ToDecimal(cat3Box.Text));
            command.Parameters.AddWithValue("?", Convert.ToDecimal(totalCatBox.Text));
            command.Parameters.AddWithValue("?", Convert.ToDecimal(examScoreBox.Text));
            command.Parameters.AddWithValue("?", Convert.ToDecimal(grandTotalBox.Text));
            command.Parameters.AddWithValue("?", CurrentGrade());
            command.Parameters.AddWithValue("?", CurrentRemark());
            return command;
        }

        private void DeleteResult()
        {
            if (!int.TryParse(studentIdBox.Text.Trim(), out _))
            {
                MessageBox.Show("Enter the student ID for the result to delete.", "Exams", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show("Delete this result record?", "Delete Result", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand("DELETE FROM [dbo].[examss] WHERE [std_id] = ? AND [subject] = ? AND [term] = ? AND [year] = ?", con))
                {
                    command.Parameters.AddWithValue("?", studentIdBox.Text.Trim());
                    command.Parameters.AddWithValue("?", subjectBox.Text);
                    command.Parameters.AddWithValue("?", termBox.Text);
                    command.Parameters.AddWithValue("?", yearBox.Text.Trim());
                    con.Open();
                    int rows = command.ExecuteNonQuery();
                    statusLabel.Text = rows > 0 ? "Result deleted." : "No matching result found.";
                    MessageBox.Show(rows > 0 ? "Result deleted successfully." : "No matching result found.", "Exams", MessageBoxButtons.OK, rows > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Result could not be deleted: " + ex.Message, "Exams", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string CurrentGrade()
        {
            return gradeValueLabel.Text.Replace("Grade:", "").Trim();
        }

        private string CurrentRemark()
        {
            return remarkValueLabel.Text.Replace("Remark:", "").Trim();
        }

        private void ClearForm()
        {
            studentIdBox.Text = "";
            studentNameBox.Text = "";
            classBox.Text = "";
            subjectBox.SelectedIndex = 0;
            termBox.SelectedIndex = 0;
            yearBox.Text = DateTime.Today.Year.ToString();
            cat1Box.Text = "";
            cat2Box.Text = "";
            cat3Box.Text = "";
            totalCatBox.Text = "";
            examScoreBox.Text = "";
            grandTotalBox.Text = "";
            gradeValueLabel.Text = "Grade: -";
            remarkValueLabel.Text = "Remark: -";
            statusLabel.Text = "Ready.";
        }

        private void EXAMS_Load(object sender, EventArgs e) { }
        private void guna2GroupBox1_Click(object sender, EventArgs e) { }
        private void gunaTextBox5_TextChanged(object sender, EventArgs e) { LookupStudent(); }
        private void label10_Click(object sender, EventArgs e) { }
        private void btn_Update_Click(object sender, EventArgs e) { UpdateResult(); }
        private void pay_Click(object sender, EventArgs e) { SaveResult(); }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void gunaPictureBox6_Click(object sender, EventArgs e) { Close(); }
        private void gunaTextBox9_TextChanged(object sender, EventArgs e) { }
        private void gunaButton6_Click(object sender, EventArgs e) { TryCalculateScores(true); }
        private void gunaTextBox5_TextChanged_1(object sender, EventArgs e) { LookupStudent(); }
        private void label16_Click(object sender, EventArgs e) { }
        private void gunaComboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void gunaButton4_Click(object sender, EventArgs e) { SaveResult(); }
        private void gunaButton7_Click(object sender, EventArgs e) { ClearForm(); }
        private void gunaButton5_Click(object sender, EventArgs e) { new EXAMSVIEW().Show(); }
        private void gunaButton3_Click(object sender, EventArgs e) { DeleteResult(); }
        private void gunaButton2_Click(object sender, EventArgs e) { UpdateResult(); }
    }
}
