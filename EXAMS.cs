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
        private Guna.UI.WinForms.GunaTextBox studentIdBox;
        private Guna.UI.WinForms.GunaTextBox studentNameBox;
        private Guna.UI.WinForms.GunaTextBox classBox;
        private Guna.UI.WinForms.GunaComboBox subjectBox;
        private Guna.UI.WinForms.GunaComboBox termBox;
        private Guna.UI.WinForms.GunaTextBox yearBox;
        private Guna.UI.WinForms.GunaTextBox cat1Box;
        private Guna.UI.WinForms.GunaTextBox cat2Box;
        private Guna.UI.WinForms.GunaTextBox cat3Box;
        private Guna.UI.WinForms.GunaTextBox totalCatBox;
        private Guna.UI.WinForms.GunaTextBox examScoreBox;
        private Guna.UI.WinForms.GunaTextBox grandTotalBox;
        private Guna.UI.WinForms.GunaLabel gradeValueLabel;
        private Guna.UI.WinForms.GunaLabel remarkValueLabel;
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
            UiTheme.Apply(this);
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
            actions.Controls.Add(CreateGunaPrimaryButton("View Results", () => new EXAMSVIEW().Show()));
            actions.Controls.Add(CreateGunaSecondaryButton("Dashboard", () =>
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

            studentIdBox = CreateGunaTextBox();
            studentNameBox = CreateGunaTextBox(true);
            classBox = CreateGunaTextBox(true);
            subjectBox = CreateGunaComboBox(subjects);
            termBox = CreateGunaComboBox(new[] { "TERM 1", "TERM 2", "TERM 3" });
            yearBox = CreateGunaTextBox();
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

            cat1Box = CreateGunaTextBox();
            cat2Box = CreateGunaTextBox();
            cat3Box = CreateGunaTextBox();
            totalCatBox = CreateGunaTextBox(true);
            examScoreBox = CreateGunaTextBox();
            grandTotalBox = CreateGunaTextBox(true);

            cat1Box.TextChanged += (sender, args) => TryCalculateScores(false);
            cat2Box.TextChanged += (sender, args) => TryCalculateScores(false);
            cat3Box.TextChanged += (sender, args) => TryCalculateScores(false);
            examScoreBox.TextChanged += (sender, args) => TryCalculateScores(false);

            layout.Controls.Add(CreateField("CLASSTEST I & II (40 MARKS)", cat1Box), 0, 0);
            layout.Controls.Add(CreateField("GROUP WORK  (10 MARKS)", cat2Box), 1, 0);
            layout.Controls.Add(CreateField("PROJECT WORK  (10 MARKS)", cat3Box), 2, 0);
            layout.Controls.Add(CreateField("TOTAL CLASS SCORE(60 MARKS) ", totalCatBox), 0, 1);
            layout.Controls.Add(CreateField("EXAM SCORE (100%)", examScoreBox), 1, 1);
            layout.Controls.Add(CreateField("GRAND TOTAL ", grandTotalBox), 2, 1);
            layout.Controls.Add(CreateGunaPrimaryButton("CALCULATE ", () => TryCalculateScores(true)), 0, 2);
            layout.Controls.Add(CreateGunaPrimaryButton("SAVE RESULT ", SaveResult), 1, 2);
            layout.Controls.Add(CreateGunaSecondaryButton("CLEAR ", ClearForm), 2, 2);

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

            gradeValueLabel = CreateGunaResultLabel("Grade");
            remarkValueLabel = CreateGunaResultLabel("Remark");
            layout.Controls.Add(gradeValueLabel, 0, 1);
            layout.Controls.Add(remarkValueLabel, 0, 2);
            layout.Controls.Add(CreateGunaSecondaryButton("Update Result", UpdateResult), 0, 3);
            layout.Controls.Add(CreateGunaDangerButton("Delete Result", DeleteResult), 0, 4);
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

        private Guna.UI.WinForms.GunaTextBox CreateGunaTextBox(bool readOnly = false)
        {
            return new Guna.UI.WinForms.GunaTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                ReadOnly = readOnly,
                BaseColor = readOnly ? UiTheme.SurfaceAlt : Color.White,
                BorderColor = Color.Silver,
                FocusedBorderColor = PrimaryColor,
                ForeColor = TextColor
            };
        }

        private Guna.UI.WinForms.GunaComboBox CreateGunaComboBox(string[] items)
        {
            var combo = new Guna.UI.WinForms.GunaComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F),
                BaseColor = Color.White,
                BorderColor = Color.Silver,
                FocusedColor = PrimaryColor
            };
            combo.Items.AddRange(items);
            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            return combo;
        }

        private Guna.UI.WinForms.GunaLabel CreateGunaResultLabel(string title)
        {
            return new Guna.UI.WinForms.GunaLabel
            {
                Dock = DockStyle.Fill,
                Text = title + ": -",
                ForeColor = TextColor,
                BackColor = AccentColor,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 12)
            };
        }

        private Guna.UI2.WinForms.Guna2Button CreateGunaPrimaryButton(string text, Action action)
        {
            var button = CreateGunaButton(text, action);
            button.FillColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.HoverState.FillColor = UiTheme.NavyHover;
            button.HoverState.ForeColor = Color.White;
            button.PressedColor = Color.Black;
            button.BorderColor = PrimaryColor;
            button.BorderThickness = 1;
            return button;
        }

        private Guna.UI2.WinForms.Guna2Button CreateGunaSecondaryButton(string text, Action action)
        {
            var button = CreateGunaButton(text, action);
            button.FillColor = Color.White;
            button.ForeColor = TextColor;
            button.BorderColor = BorderColor;
            button.BorderThickness = 1;
            button.HoverState.FillColor = AccentColor;
            button.HoverState.ForeColor = TextColor;
            button.PressedColor = Color.Black;
            return button;
        }

        private Guna.UI2.WinForms.Guna2Button CreateGunaDangerButton(string text, Action action)
        {
            var button = CreateGunaButton(text, action);
            button.FillColor = Color.White;
            button.ForeColor = DangerColor;
            button.BorderColor = Color.FromArgb(254, 205, 211);
            button.BorderThickness = 1;
            button.HoverState.FillColor = Color.FromArgb(255, 241, 242);
            button.HoverState.ForeColor = DangerColor;
            button.PressedColor = Color.Black;
            return button;
        }

        private Guna.UI2.WinForms.Guna2Button CreateGunaButton(string text, Action action)
        {
            var button = new Guna.UI2.WinForms.Guna2Button
            {
                Dock = DockStyle.Fill,
                Height = 44,
                Margin = new Padding(8, 3, 0, 3),
                Text = text.Trim(),
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Animated = true,
                BorderRadius = 8,
                ShadowDecoration =
                {
                    Enabled = false
                }
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
            try
            {
                // Attempt to parse the values from the textboxes
                if (decimal.TryParse(cat1Box.Text, out decimal value1) &&
                    decimal.TryParse(cat3Box.Text, out decimal value2) && // gunaTextBox2 is CAT 3
                    decimal.TryParse(cat2Box.Text, out decimal value3) && // gunaTextBox3 is CAT 2
                    decimal.TryParse(examScoreBox.Text, out decimal value4))
                {
                    // Check if the values exceed their respective limits
                    if (value1 > 40)
                    {
                        MessageBox.Show("CLASSTEST 1 & 2 MARKS should not be more than 40.", "Input Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (value2 > 10)
                    {
                        MessageBox.Show("GROUP WORKS MARKS should not be more than 10.", "Input Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (value3 > 10)
                    {
                        MessageBox.Show("PROJECT WORK MARKS should not be more than 30.", "Input Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (value4 > 100)
                    {
                        MessageBox.Show("EXAMS SCORE'S MARKS should not be more than 100.", "Input Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        // Calculate the sum
                        decimal sum = value1 + value3 + value2; // CAT 1 + CAT 2 + CAT 3
                        decimal sum1 = ((sum/60)*50) + ((value4/100)*50);

                        // Display the result in gunaTextBox4 and gunaTextBox7
                        totalCatBox.Text = sum.ToString();
                        grandTotalBox.Text = sum1.ToString();

                        // Determine the grade and remarks based on sum1
                        string grade;
                        string remarks;

                        if (sum1 >= 80)
                        {
                            grade = "1";
                            remarks = "ADVANCE";
                        }
                        else if (sum1 >= 75)
                        {
                            grade = "2";
                            remarks = "PROFICIENCY";
                        }
                        else if (sum1 >= 70)
                        {
                            grade = "3";
                            remarks = "APPROACHING PROFICIENCY";
                        }
                        else if (sum1 >= 65)
                        {
                            grade = "4";
                            remarks = "DEVELOPING";
                        }
                        else
                        {
                            grade = "5";
                            remarks = "BEGINING";
                        }

                        // Display grade and remarks
                        gradeValueLabel.Text = $"{grade}";
                        remarkValueLabel.Text = $"{remarks}";
                        return true;
                    }
                }
                else if (showValidation)
                {
                    // Handle the case where any of the inputs are invalid
                    MessageBox.Show("Please enter valid numbers in all the score textboxes.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private void SaveResult()
        {
            // Initialize the connection and query strings
            Aikins.con = new OleDbConnection(Aikins.constr);

            // Check if the entry already exists based on student ID, subject, and term
            Aikins.query = "SELECT COUNT(*) FROM [dbo].[examss] WHERE [std_id] = ? AND [subject] = ? AND [term] = ?";

            try
            {
                // Create and configure the command within a using statement to ensure disposal
                using (OleDbCommand command = new OleDbCommand(Aikins.query, Aikins.con))
                {
                    // Add parameters to the command
                    command.Parameters.AddWithValue("@p1", studentIdBox.Text); // std_id
                    command.Parameters.AddWithValue("@p2", subjectBox.SelectedItem?.ToString() ?? ""); // subject
                    command.Parameters.AddWithValue("@p3", termBox.SelectedItem?.ToString() ?? ""); // term

                    // Open the connection
                    Aikins.con.Open();

                    // Execute scalar query to check if a record already exists
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    // Insert or update logic based on the result
                    if (count > 0)
                    {
                        // Record exists, ask user if they want to update
                        DialogResult dialogResult = MessageBox.Show("Same record already exists. Do you want to update the record?", "Duplicate Record", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (dialogResult == DialogResult.Yes)
                        {
                            UpdateExistingResult(Aikins.con);
                        }
                    }
                    else
                    {
                        Aikins.query = "INSERT INTO [dbo].[examss] ([std_id], [std_name], [std_class], [subject], [term], [year], [cat1], [cat2], [cat3], [tl_cat], [exam_score], [gt], [grade], [remark]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        using (OleDbCommand insertCommand = new OleDbCommand(Aikins.query, Aikins.con))
                        {
                            // Add parameters to the insert command
                            insertCommand.Parameters.AddWithValue("@p1", studentIdBox.Text); // std_id
                            insertCommand.Parameters.AddWithValue("@p2", studentNameBox.Text); // std_name
                            insertCommand.Parameters.AddWithValue("@p3", classBox.Text); // std_class
                            insertCommand.Parameters.AddWithValue("@p4", subjectBox.SelectedItem?.ToString() ?? ""); // subject
                            insertCommand.Parameters.AddWithValue("@p5", termBox.SelectedItem?.ToString() ?? ""); // term
                            insertCommand.Parameters.AddWithValue("@p6", yearBox.Text); // year
                            insertCommand.Parameters.AddWithValue("@p7", cat1Box.Text); // cat1
                            insertCommand.Parameters.AddWithValue("@p8", cat2Box.Text); // cat2
                            insertCommand.Parameters.AddWithValue("@p9", cat3Box.Text); // cat3
                            insertCommand.Parameters.AddWithValue("@p10", totalCatBox.Text); // tl_cat
                            insertCommand.Parameters.AddWithValue("@p11", examScoreBox.Text); // exam_score
                            insertCommand.Parameters.AddWithValue("@p12", grandTotalBox.Text); // gt
                            insertCommand.Parameters.AddWithValue("@p13", gradeValueLabel.Text);  // grade
                            insertCommand.Parameters.AddWithValue("@p14", remarkValueLabel.Text); // remark

                            int rowsAffected = insertCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Record inserted successfully.");
                            }
                            else
                            {
                                MessageBox.Show("Failed to insert record.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                if (Aikins.con.State == ConnectionState.Open)
                {
                    Aikins.con.Close();
                }
            }
        }

        private void UpdateResult()
        {
            Aikins.con = new OleDbConnection(Aikins.constr);
            try
            {
                Aikins.con.Open();
                UpdateResultDirect(Aikins.con);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                if (Aikins.con.State == ConnectionState.Open)
                {
                    Aikins.con.Close();
                }
            }
        }

        private void UpdateResultDirect(OleDbConnection con)
        {
            Aikins.query = "UPDATE [dbo].[examss] SET [std_name] = ?, [std_class] = ?, [subject] = ?, [term] = ?, [year] = ?, [cat1] = ?, [cat2] = ?, [cat3] = ?, [tl_cat] = ?, [exam_score] = ?, [gt] = ?, [grade] = ?, [remark] = ? WHERE [std_id] = ?";

            using (OleDbCommand command = new OleDbCommand(Aikins.query, con))
            {
                command.Parameters.AddWithValue("@std_name", studentNameBox.Text);
                command.Parameters.AddWithValue("@std_class", classBox.Text);
                command.Parameters.AddWithValue("@subject", subjectBox.SelectedItem?.ToString() ?? "");
                command.Parameters.AddWithValue("@term", termBox.SelectedItem?.ToString() ?? "");
                command.Parameters.AddWithValue("@year", yearBox.Text);
                command.Parameters.AddWithValue("@cat1", cat1Box.Text);
                command.Parameters.AddWithValue("@cat2", cat2Box.Text);
                command.Parameters.AddWithValue("@cat3", cat3Box.Text);
                command.Parameters.AddWithValue("@tl_cat", totalCatBox.Text);
                command.Parameters.AddWithValue("@exam_score", examScoreBox.Text);
                command.Parameters.AddWithValue("@gt", grandTotalBox.Text);
                command.Parameters.AddWithValue("@grade", gradeValueLabel.Text);
                command.Parameters.AddWithValue("@remark", remarkValueLabel.Text);
                command.Parameters.AddWithValue("@std_id", studentIdBox.Text);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Student's exams record successfully updated.");
                }
                else
                {
                    MessageBox.Show("No matching record found to update.");
                }
            }
        }

        private void UpdateExistingResult(OleDbConnection con)
        {
            Aikins.query = "UPDATE [dbo].[examss] SET [std_name] = ?, [std_class] = ?, [cat1] = ?, [cat2] = ?, [cat3] = ?, [tl_cat] = ?, [exam_score] = ?, [gt] = ?, [grade] = ?, [remark] = ? WHERE [std_id] = ? AND [subject] = ? AND [term] = ?";

            using (OleDbCommand updateCommand = new OleDbCommand(Aikins.query, con))
            {
                updateCommand.Parameters.AddWithValue("@p1", studentNameBox.Text); // std_name
                updateCommand.Parameters.AddWithValue("@p2", classBox.Text); // std_class
                updateCommand.Parameters.AddWithValue("@p3", cat1Box.Text); // cat1
                updateCommand.Parameters.AddWithValue("@p4", cat2Box.Text); // cat2
                updateCommand.Parameters.AddWithValue("@p5", cat3Box.Text); // cat3
                updateCommand.Parameters.AddWithValue("@p6", totalCatBox.Text); // tl_cat
                updateCommand.Parameters.AddWithValue("@p7", examScoreBox.Text); // exam_score
                updateCommand.Parameters.AddWithValue("@p8", grandTotalBox.Text); // gt
                updateCommand.Parameters.AddWithValue("@p9", gradeValueLabel.Text);  // grade
                updateCommand.Parameters.AddWithValue("@p10", remarkValueLabel.Text); // remark
                updateCommand.Parameters.AddWithValue("@p11", studentIdBox.Text); // std_id
                updateCommand.Parameters.AddWithValue("@p12", subjectBox.SelectedItem?.ToString() ?? ""); // subject
                updateCommand.Parameters.AddWithValue("@p13", termBox.SelectedItem?.ToString() ?? ""); // term

                int rowsAffected = updateCommand.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Record updated successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to update record.");
                }
            }
        }

        private void DeleteResult()
        {
            Aikins.con = new OleDbConnection(Aikins.constr);
            Aikins.query = "DELETE FROM [dbo].[examss] WHERE [std_id] = ? AND [std_name] = ? AND [std_class] = ? AND [subject] = ? AND [term] = ? AND [year] = ? AND [cat1] = ? AND [cat2] = ? AND [cat3] = ? AND [tl_cat] = ? AND [exam_score] = ? AND [gt] = ? AND [grade] = ? AND [remark] = ?";

            try
            {
                using (OleDbCommand command = new OleDbCommand(Aikins.query, Aikins.con))
                {
                    Aikins.con.Open();
                    command.Parameters.AddWithValue("@std_id", studentIdBox.Text);
                    command.Parameters.AddWithValue("@std_name", studentNameBox.Text);
                    command.Parameters.AddWithValue("@std_class", classBox.Text);
                    command.Parameters.AddWithValue("@subject", subjectBox.SelectedItem?.ToString() ?? "");
                    command.Parameters.AddWithValue("@term", termBox.SelectedItem?.ToString() ?? "");
                    command.Parameters.AddWithValue("@year", yearBox.Text);
                    command.Parameters.AddWithValue("@cat1", cat1Box.Text);
                    command.Parameters.AddWithValue("@cat2", cat2Box.Text);
                    command.Parameters.AddWithValue("@cat3", cat3Box.Text);
                    command.Parameters.AddWithValue("@tl_cat", totalCatBox.Text);
                    command.Parameters.AddWithValue("@exam_score", examScoreBox.Text);
                    command.Parameters.AddWithValue("@gt", grandTotalBox.Text);
                    command.Parameters.AddWithValue("@grade", gradeValueLabel.Text);
                    command.Parameters.AddWithValue("@remark", remarkValueLabel.Text);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Student's exams record successfully deleted.");
                    }
                    else
                    {
                        MessageBox.Show("No matching record found to delete.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                if (Aikins.con.State == ConnectionState.Open)
                {
                    Aikins.con.Close();
                }
            }
        }

        private void ClearForm()
        {
            studentIdBox.Text = string.Empty;
            studentNameBox.Text = string.Empty;
            classBox.Text = string.Empty;
            subjectBox.SelectedIndex = -1;
            termBox.SelectedIndex = -1;
            yearBox.Text = string.Empty;
            cat1Box.Text = string.Empty;
            cat2Box.Text = string.Empty; // gunaTextBox3
            cat3Box.Text = string.Empty; // gunaTextBox2
            totalCatBox.Text = string.Empty;
            examScoreBox.Text = string.Empty;
            grandTotalBox.Text = string.Empty;
            gradeValueLabel.Text = string.Empty;
            remarkValueLabel.Text = string.Empty;
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

        private void gunaTextBox4_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
