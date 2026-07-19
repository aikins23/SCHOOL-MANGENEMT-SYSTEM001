using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EXAMSVIEW : Form
    {
        private readonly ExamService _examService;
        private TextBox searchBox;
        private ComboBox classFilter;
        private ComboBox termFilter;
        private DataGridView resultsGrid;
        private Label resultLabel;
        private Label totalReportsLabel;
        private Label averageScoreLabel;
        private Label topStudentLabel;
        private DataTable resultsTable;
        private DataView _activeView;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SurfaceAlt = UiTheme.SurfaceAlt;
        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        // ── Win32 placeholder helper ──────────────────────────────────────────
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        public EXAMSVIEW()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("EXAMSVIEW", this)) return;

            // Initialize modern architecture
            var examRepo = new ExamRepository(AppConfig.ConnectionString);
            _examService = new ExamService(examRepo);

            BuildResultsView();
            NavigationSidebar.AddTo(this);
        }

        private void BuildResultsView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Exam Results";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1220, 760);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = PageBackColor, Padding = new Padding(26) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildInsightBar(), 0, 1);
            root.Controls.Add(BuildFilterBar(), 0, 2);
            root.Controls.Add(BuildGridShell(), 0, 3);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = PageBackColor, Padding = new Padding(0, 0, 0, 8) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

            var title = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = PageBackColor };
            title.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            title.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            title.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Exam Results", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            title.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Search results, review rankings, and generate report cards", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft }, 0, 1);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = PageBackColor,
                Padding = new Padding(0, 18, 6, 0)
            };
            actions.Controls.Add(MakeHeaderBtn("Refresh", async () => await LoadResults(), false, 84));
            actions.Controls.Add(MakeHeaderBtn("View Details", OpenSelectedResult, false, 104));
            actions.Controls.Add(CreatePrintReportCardButton());
            actions.Controls.Add(MakeHeaderBtn("Enter Scores", () => new EXAMS().Show(), true, 112));

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Button MakeHeaderBtn(string text, Action action, bool primary, int width)
        {
            var btn = new Button
            {
                Width = width,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = primary ? PrimaryColor : SurfaceColor,
                ForeColor = primary ? Color.White : TextColor
            };
            btn.FlatAppearance.BorderColor = primary ? PrimaryColor : BorderColor;
            btn.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            btn.Click += (s, e) => action();
            return btn;
        }

        private Control BuildInsightBar()
        {
            var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = PageBackColor, Padding = new Padding(0, 8, 0, 14) };
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            totalReportsLabel = new Label();
            averageScoreLabel = new Label();
            topStudentLabel = new Label();
            metrics.Controls.Add(CreateMetricCard("Report Cards", totalReportsLabel, "Grouped by student, class, term, and year"), 0, 0);
            metrics.Controls.Add(CreateMetricCard("Average Score", averageScoreLabel, "Average total score in current view"), 1, 0);
            metrics.Controls.Add(CreateMetricCard("Top Student", topStudentLabel, "Highest total in current view"), 2, 0);
            return metrics;
        }

        private Control BuildFilterBar()
        {
            var panel = CreateSurfacePanel(new Padding(18), Padding.Empty);
            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, BackColor = SurfaceColor };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));

            searchBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), BorderStyle = BorderStyle.FixedSingle };
            searchBox.TextChanged += (sender, args) => ApplyFilters();
            searchBox.HandleCreated += (s, e) =>
                SendMessage(searchBox.Handle, EM_SETCUEBANNER, 1, "Search by student name…");
            classFilter = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(10, 0, 0, 0) };
            classFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();
            termFilter = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(10, 0, 0, 0) };
            termFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();
            resultLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9.5F) };

            filters.Controls.Add(searchBox, 0, 0);
            filters.Controls.Add(classFilter, 1, 0);
            filters.Controls.Add(termFilter, 2, 0);
            filters.Controls.Add(CreateButton("Clear", ClearFilters, false, 96), 3, 0);
            filters.Controls.Add(resultLabel, 4, 0);
            panel.Controls.Add(filters);
            return panel;
        }

        private Control BuildGridShell()
        {
            var shell = CreateSurfacePanel(new Padding(1), Padding.Empty);
            resultsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, // Disabled for performance
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                VirtualMode = true // High-performance virtual loading
            };

            resultsGrid.CellValueNeeded += ResultsGrid_CellValueNeeded;
            resultsGrid.CellDoubleClick += (sender, args) => { if (args.RowIndex >= 0) OpenSelectedResult(); };
            resultsGrid.CellFormatting += ResultsGrid_CellFormatting;

            UiTheme.StyleDataGrid(resultsGrid);
            resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            resultsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            resultsGrid.ColumnHeadersHeight = 38;
            resultsGrid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            resultsGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;
            resultsGrid.GridColor = BorderColor;

            shell.Controls.Add(resultsGrid);
            return shell;
        }

        private void ResultsGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_activeView == null || e.RowIndex >= _activeView.Count) return;
            try
            {
                string colName = resultsGrid.Columns[e.ColumnIndex].Name;
                e.Value = FormatVirtualGridValue(colName, _activeView[e.RowIndex][colName]);
            }
            catch { e.Value = null; }
        }

        private static object FormatVirtualGridValue(string columnName, object value)
        {
            if (value == null || value == DBNull.Value) return value;

            decimal parsed;
            if (!ShouldFormatVirtualGridNumber(columnName, value, out parsed))
                return value;

            return parsed.ToString("0.00", CultureInfo.CurrentCulture);
        }

        private static bool ShouldFormatVirtualGridNumber(string columnName, object value, out decimal parsed)
        {
            parsed = 0m;
            string text = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed) &&
                !decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return false;
            }

            string key = (columnName ?? string.Empty).Trim().ToUpperInvariant();
            if (key == "STUDENTID" || key == "ID" || key == "YEAR" || key == "CLASS" || key == "TERMS" ||
                key == "TERM" || key == "NAME" || key.Contains("RANK") || key.Contains("POSITION") ||
                key.EndsWith(" POS"))
            {
                return false;
            }

            return true;
        }

        private Control CreateMetricCard(string title, Label valueLabel, string caption)
        {
            // Outer wrapper: 3 px Navy left padding = accent bar; the card fills the rest
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Navy, Padding = new Padding(3, 0, 0, 0), Margin = new Padding(0, 0, 12, 0) };
            var card = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Padding = new Padding(14, 10, 14, 10) };
            wrapper.Controls.Add(card);

            // Title row
            var titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = title,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Big value number
            valueLabel.Dock = DockStyle.Top;
            valueLabel.Height = 44;
            valueLabel.Text = "--";
            valueLabel.ForeColor = Navy;
            valueLabel.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;

            // Caption row (subtitle)
            var captionLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Text = caption,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Controls added bottom→top for correct Dock stacking
            card.Controls.Add(captionLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return wrapper;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = padding, Margin = margin };
        }

        private Button CreateButton(string text, Action action, bool primary, int width)
        {
            var button = new Button { Width = width, Height = 36, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = primary ? PrimaryColor : SurfaceColor, ForeColor = primary ? Color.White : TextColor };
            button.FlatAppearance.BorderColor = primary ? PrimaryColor : BorderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            button.Click += (sender, args) => action();
            return button;
        }

        private Control CreatePrintReportCardButton()
        {
            var btn = new Button
            {
                Text = "Print PDF",
                Width = 94,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = SurfaceColor,
                ForeColor = TextColor
            };
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.MouseOverBackColor = UiTheme.GoldSoft;

            btn.Click += async (sender, args) =>
            {
                if (!btn.Enabled) return;

                if (resultsGrid.SelectedRows.Count == 0)
                {
                    UIHelper.ShowWarning("Please select a student first", "Generate Report Card");
                    return;
                }

                var selected = GetSelectedRowView();
                if (selected == null)
                {
                    UIHelper.ShowWarning("Please select a student first", "Generate Report Card");
                    return;
                }

                var studentName = selected.Row.Table.Columns.Contains("NAME") ? selected["NAME"]?.ToString() ?? "" : "";
                var term = termFilter.SelectedItem?.ToString() ?? "All terms";
                var year = selected.Row.Table.Columns.Contains("YEAR") ? selected["YEAR"]?.ToString() ?? CurrentAcademicYearLabel() : CurrentAcademicYearLabel();
                string studentId = selected.Row.Table.Columns.Contains("StudentID") ? selected["StudentID"]?.ToString() ?? "" : "";

                if (string.IsNullOrEmpty(studentId))
                {
                    UIHelper.ShowError("Student ID could not be identified for this record.", "Generate Report Card");
                    return;
                }

                try
                {
                    btn.Enabled = false;
                    Cursor = Cursors.WaitCursor;

                    if (term == "All terms" || string.IsNullOrEmpty(term))
                    {
                        UIHelper.ShowWarning("Please select a specific term from the filter first.", "Generate Report Card");
                        return;
                    }

                    if (resultLabel != null) resultLabel.Text = $"Generating report for {studentName}...";

                    var remarksRepository = new StudentTermRemarksRepository(AppConfig.ConnectionString);
                    var dataService = new ReportCardDataService(AppConfig.ConnectionString, remarksRepository);
                    var pdfGenerator = new ReportCardPDFGenerator();
                    var printer = new ReportCardPrinter();
                    var manager = new ReportCardManager(dataService, pdfGenerator, printer);

                    var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var folder = Path.Combine(
                        string.IsNullOrWhiteSpace(documents) ? Path.GetTempPath() : documents,
                        "Nyansapo Report Cards");
                    Directory.CreateDirectory(folder);

                    var action = new ReportCardOutputAction { Type = OutputType.Save, SavePath = folder };
                    var savedPath = await Task.Run(async () =>
                        await manager.GenerateAndOutputAsync(studentId, term, year, action));
                    UIHelper.ShowSuccess(
                        $"Report card generated successfully for {studentName}.\n\nSaved to:\n{savedPath}",
                        "Generate Report Card");

                    if (resultLabel != null) resultLabel.Text = "Ready.";
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Report generation failed", ex);
                    UIHelper.ShowError($"Failed to generate report: {ex.Message}", "Generate Report Card");
                    if (resultLabel != null) resultLabel.Text = "Generation failed.";
                }
                finally
                {
                    Cursor = Cursors.Default;
                    btn.Enabled = true;
                }
            };

            return btn;
        }

        private DataRowView GetSelectedRowView()
        {
            if (_activeView == null || resultsGrid == null) return null;
            int index = resultsGrid.SelectedRows.Count > 0
                ? resultsGrid.SelectedRows[0].Index
                : resultsGrid.CurrentRow?.Index ?? -1;
            if (index < 0 || index >= _activeView.Count) return null;
            return _activeView[index];
        }

        private async System.Threading.Tasks.Task LoadResults()
        {
            try
            {
                if (_examService == null) return;
                if (resultLabel != null) resultLabel.Text = "Loading academic reports...";

                resultsTable = await _examService.GetResultsReportTableAsync();
                _activeView = resultsTable?.DefaultView;

                // 1. Build columns manually for Virtual Mode (first time only)
                if (resultsTable != null && resultsGrid.Columns.Count == 0)
                {
                    resultsGrid.Columns.Clear();
                    foreach (DataColumn col in resultsTable.Columns)
                    {
                        resultsGrid.Columns.Add(col.ColumnName, col.ColumnName);
                    }
                }

                // 2. Configure visibility and headers
                ConfigureGridColumns();

                // 3. Setup filters
                LoadFilterValues();
                await ApplyTeacherScopeAsync();
                ApplyFilters();

                if (resultsTable == null || resultsTable.Rows.Count == 0)
                {
                    if (resultLabel != null) resultLabel.Text = "No exam records found.";
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("LoadResults failed", ex);
                UIHelper.ShowError("Results could not be loaded: " + ex.Message, "Exam Results");
            }
        }

        private void ConfigureGridColumns()
        {
            if (resultsGrid.Columns.Count == 0) return;

            // Define protected/system columns that should always be visible (or specifically hidden)
            var systemColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "NAME", "CLASS", "TERMS", "YEAR", "TOTAL_SCORE", "TOTAL_RANK" };

            foreach (DataGridViewColumn column in resultsGrid.Columns)
            {
                string name = column.Name.ToUpperInvariant();

                // 1. Hide internal ID
                if (name == "STUDENTID") { column.Visible = false; continue; }

                // 2. Hide metadata columns (Grade, Remark, Position) from the main overview
                if (name.EndsWith(" GRADE") || name.EndsWith(" REMARK") || name.EndsWith(" POS"))
                {
                    column.Visible = false;
                    continue;
                }

                // 3. Show everything else (system columns + all subject scores)
                column.Visible = true;

                // 4. Set pretty headers for system columns
                if (name == "NAME") column.HeaderText = "Student";
                else if (name == "TERMS") column.HeaderText = "Term";
                else if (name == "TOTAL_SCORE") column.HeaderText = "Average";
                else if (name == "TOTAL_RANK") column.HeaderText = "Rank";
                else if (name == "YEAR") column.HeaderText = "Year";
                else if (name == "CLASS") column.HeaderText = "Class";
                else
                {
                    // For subjects, we just use the name as-is but ensure it's not too wide
                    column.MinimumWidth = 80;
                }
            }

            // Move Rank and Total to the end
            if (resultsGrid.Columns.Contains("TOTAL_SCORE")) resultsGrid.Columns["TOTAL_SCORE"].DisplayIndex = resultsGrid.Columns.Count - 2;
            if (resultsGrid.Columns.Contains("TOTAL_RANK")) resultsGrid.Columns["TOTAL_RANK"].DisplayIndex = resultsGrid.Columns.Count - 1;
        }

        private void SetHeader(string name, string header)
        {
            if (resultsGrid.Columns.Contains(name)) resultsGrid.Columns[name].HeaderText = header;
        }

        private void LoadFilterValues()
        {
            LoadCombo(classFilter, "All classes", "CLASS");
            LoadCombo(termFilter, "All terms", "TERMS");
        }

        private async System.Threading.Tasks.Task ApplyTeacherScopeAsync()
        {
            if (!AuthService.IsTeacher) return;
            string myClass = await AuthService.GetCurrentTeacherClassAsync();
            if (string.IsNullOrEmpty(myClass)) return;
            int idx = classFilter.Items.IndexOf(myClass);
            if (idx >= 0)
            {
                classFilter.SelectedIndex = idx;
                classFilter.Enabled = false;
            }
        }

        private void LoadCombo(ComboBox combo, string allText, string column)
        {
            string selected = combo.Text;
            combo.Items.Clear();
            combo.Items.Add(allText);
            if (resultsTable != null)
            {
                foreach (DataRow row in resultsTable.DefaultView.ToTable(true, column).Rows)
                {
                    combo.Items.Add(row[column].ToString());
                }
            }
            combo.SelectedIndex = combo.Items.Contains(selected) ? combo.Items.IndexOf(selected) : 0;
        }

        private void ApplyFilters()
        {
            if (resultsTable == null || _activeView == null) return;
            var filters = new List<string>();
            string search = searchBox.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(search)) filters.Add("NAME LIKE '%" + search + "%'");
            if (classFilter.SelectedIndex > 0) filters.Add("CLASS = '" + classFilter.Text.Replace("'", "''") + "'");
            if (termFilter.SelectedIndex > 0) filters.Add("TERMS = '" + termFilter.Text.Replace("'", "''") + "'");

            _activeView.RowFilter = string.Join(" AND ", filters);
            resultsGrid.RowCount = _activeView.Count;
            resultsGrid.Invalidate();

            if (resultLabel != null) resultLabel.Text = _activeView.Count + " report card(s) shown";
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            int count = _activeView == null ? 0 : _activeView.Count;
            totalReportsLabel.Text = count.ToString();
            if (count == 0)
            {
                averageScoreLabel.Text = "--";
                topStudentLabel.Text = "No data";
                return;
            }

            decimal total = 0m;
            decimal bestScore = decimal.MinValue;
            string bestName = "";
            foreach (DataRowView view in _activeView)
            {
                decimal score = Convert.ToDecimal(view["TOTAL_SCORE"]);
                total += score;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestName = view["NAME"].ToString();
                }
            }

            averageScoreLabel.Text = (total / count).ToString("0.00");
            topStudentLabel.Text = bestName;
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            if (classFilter.Items.Count > 0) classFilter.SelectedIndex = 0;
            if (termFilter.Items.Count > 0) termFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        private void OpenSelectedResult()
        {
            Dictionary<string, string> rowData = SelectedRowData();
            if (rowData == null)
            {
                // UIHelper.ShowInfo("Select a result first.", "Exam Results"); // TODO: Implement
                return;
            }

            new examsviewdetails(rowData).Show();
        }

        private async void ExportSelectedReportCard()
        {
            Dictionary<string, string> rowData = SelectedRowData();
            if (rowData == null)
            {
                // UIHelper.ShowInfo("Select a student result row first, then click Print Report Card.", "Print Report Card"); // TODO: Implement
                return;
            }

            await UIHelper.RunBusyAsync(
                this,
                resultLabel,
                "Generating report card PDF...",
                new Control[] { resultsGrid },
                async () => await ReportCardPdfService.ExportAsync(rowData));
        }

        private static string CurrentAcademicYearLabel()
        {
            int year = DateTime.Today.Year;
            return $"{year}/{year + 1}";
        }

        private Dictionary<string, string> SelectedRowData()
        {
            if (resultsGrid.CurrentRow == null || _activeView == null) return null;

            int idx = resultsGrid.CurrentRow.Index;
            if (idx < 0 || idx >= _activeView.Count) return null;

            var rowData = new Dictionary<string, string>();
            DataRowView row = _activeView[idx];

            foreach (DataGridViewColumn col in resultsGrid.Columns)
            {
                rowData[col.Name] = FormatVirtualGridValue(col.Name, row[col.Name])?.ToString() ?? "";
            }
            return rowData;
        }

        private void ResultsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;

            if (resultsGrid.Columns[e.ColumnIndex].Name == "TOTAL_RANK" && e.Value != null && int.TryParse(e.Value.ToString(), out int rank))
            {
                e.Value = rank + GetOrdinalSuffix(rank);
                e.FormattingApplied = true;
                return;
            }

            if (IsTwoDecimalColumn(resultsGrid.Columns[e.ColumnIndex].Name))
            {
                decimal value;
                if (decimal.TryParse(e.Value.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out value)
                    || decimal.TryParse(e.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    e.Value = value.ToString("0.00", CultureInfo.CurrentCulture);
                    e.FormattingApplied = true;
                }
            }
        }

        private static bool IsTwoDecimalColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;

            var key = columnName.ToUpperInvariant();
            if (key.Contains("ID") || key == "YEAR" || key.Contains("RANK") || key.Contains("POSITION"))
            {
                return false;
            }

            return key.Contains("SCORE")
                || key.Contains("AVERAGE")
                || key.Contains("TOTAL")
                || key.Contains("BALANCE")
                || key.Contains("PAID")
                || key.Contains("AMOUNT")
                || key == "GT"
                || key.StartsWith("CAT", StringComparison.OrdinalIgnoreCase)
                || key.Contains("EXAM");
        }

        private string GetOrdinalSuffix(int number)
        {
            if (number <= 0) return "";
            switch (number % 100)
            {
                case 11:
                case 12:
                case 13:
                    return "th";
            }
            switch (number % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        private async void EXAMSVIEW_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadResults();
                LoggerHelper.LogInfo("EXAMSVIEW loaded successfully");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading exams: " + ex.Message, "EXAMSVIEW");
                LoggerHelper.LogError("EXAMSVIEW_Load failed", ex);
            }
        }
    }
}
