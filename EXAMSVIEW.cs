using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
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

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SurfaceAlt = UiTheme.SurfaceAlt;
        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public EXAMSVIEW()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var examRepo = new ExamRepository(AppConfig.ConnectionString);
            _examService = new ExamService(examRepo);

            BuildResultsView();
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
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
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 40, Text = "Exam Results", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Search results, review rankings, and generate report cards", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreateButton("Enter Scores", () => new EXAMS().Show(), true, 126));
            actions.Controls.Add(CreateButton("Report Card", OpenSelectedResult, false, 126));
            actions.Controls.Add(CreatePrintReportCardButton());
            actions.Controls.Add(CreateButton("Dashboard", () => new frmDashboard().Show(), false, 112));
            actions.Controls.Add(CreateButton("Refresh", async () => await LoadResults(), false, 96));

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            UiTheme.StyleDataGrid(resultsGrid);
            resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            resultsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            resultsGrid.ColumnHeadersHeight = 38;
            resultsGrid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            resultsGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;
            resultsGrid.GridColor = BorderColor;
            resultsGrid.CellDoubleClick += (sender, args) => { if (args.RowIndex >= 0) OpenSelectedResult(); };
            resultsGrid.CellFormatting += ResultsGrid_CellFormatting;
            shell.Controls.Add(resultsGrid);
            return shell;
        }

        private Control CreateMetricCard(string title, Label valueLabel, string caption)
        {
            var panel = CreateSurfacePanel(new Padding(16), new Padding(0, 0, 12, 0));
            panel.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 24, Text = caption, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.BottomLeft });
            valueLabel.Dock = DockStyle.Top;
            valueLabel.Height = 36;
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            panel.Controls.Add(valueLabel);
            panel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 22, Text = title, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleLeft });
            return panel;
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
                Text = "Print Report Card",
                Height = 36,
                Width = 144,
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
                if (resultsGrid.SelectedRows.Count == 0)
                {
                    UIHelper.ShowWarning("Please select a student first", "Print Report Card");
                    return;
                }

                var selectedRow = resultsGrid.SelectedRows[0];
                var studentName = selectedRow.Cells["NAME"].Value.ToString();
                var term = termFilter.SelectedItem?.ToString() ?? "All terms";
                var year = selectedRow.Cells["YEAR"].Value?.ToString() ?? "2024/2025";

                // Try to find StudentID column, or use NAME if not available
                var studentId = selectedRow.Cells.Contains("StudentID")
                    ? selectedRow.Cells["StudentID"].Value.ToString()
                    : studentName;

                try
                {
                    if (term == "All terms")
                    {
                        UIHelper.ShowWarning("Please select a specific term", "Print Report Card");
                        return;
                    }

                    var remarksRepository = new StudentTermRemarksRepository(AppConfig.ConnectionString);
                    var dataService = new ReportCardDataService(AppConfig.ConnectionString, remarksRepository);
                    var pdfGenerator = new ReportCardPDFGenerator();
                    var printer = new ReportCardPrinter();
                    var manager = new ReportCardManager(dataService, pdfGenerator, printer);

                    // Show print dialog
                    if (printer.ShowPrintDialog(out var selectedPrinter))
                    {
                        var action = new ReportCardOutputAction { Type = OutputType.Print, PrinterName = selectedPrinter };
                        await manager.GenerateAndOutputAsync(studentId, term, year, action);
                        UIHelper.ShowSuccess($"Report card printed for {studentName}", "Print Report Card");
                    }
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Error: {ex.Message}", "Print Report Card");
                }
            };

            return btn;
        }

        private async System.Threading.Tasks.Task LoadResults()
        {
            try
            {
                resultLabel.Text = "Loading academic reports...";
                resultsTable = await _examService.GetResultsReportTableAsync();
                resultsGrid.DataSource = resultsTable;
                
                ConfigureGridColumns();
                LoadFilterValues();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Results could not be loaded: " + ex.Message, "Exam Results");
            }
        }

        private void ConfigureGridColumns()
        {
            foreach (DataGridViewColumn column in resultsGrid.Columns)
            {
                column.Visible = column.Name == "NAME" || column.Name == "CLASS" || column.Name == "TERMS" ||
                                 column.Name == "YEAR" || column.Name == "TOTAL_SCORE" || column.Name == "TOTAL_RANK" ||
                                 column.Name == "ENG" || column.Name == "MATHS" || column.Name == "SCI" || column.Name == "SOCIAL";
            }

            SetHeader("NAME", "Student");
            SetHeader("TERMS", "Term");
            SetHeader("TOTAL_SCORE", "Total");
            SetHeader("TOTAL_RANK", "Rank");
            SetHeader("ENG", "English");
            SetHeader("MATHS", "Maths");
            SetHeader("SCI", "Science");
            SetHeader("SOCIAL", "Social");
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
            if (resultsTable == null) return;
            var filters = new List<string>();
            string search = searchBox.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(search)) filters.Add("NAME LIKE '%" + search + "%'");
            if (classFilter.SelectedIndex > 0) filters.Add("CLASS = '" + classFilter.Text.Replace("'", "''") + "'");
            if (termFilter.SelectedIndex > 0) filters.Add("TERMS = '" + termFilter.Text.Replace("'", "''") + "'");

            resultsTable.DefaultView.RowFilter = string.Join(" AND ", filters);
            resultLabel.Text = resultsTable.DefaultView.Count + " report card(s) shown";
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            int count = resultsTable == null ? 0 : resultsTable.DefaultView.Count;
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
            foreach (DataRowView view in resultsTable.DefaultView)
            {
                decimal score = Convert.ToDecimal(view["TOTAL_SCORE"]);
                total += score;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestName = view["NAME"].ToString();
                }
            }

            averageScoreLabel.Text = (total / count).ToString("0.0");
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
                UIHelper.ShowInfo("Select a result first.", "Exam Results");
                return;
            }

            new examsviewdetails(rowData).Show();
        }

        private Dictionary<string, string> SelectedRowData()
        {
            if (resultsGrid.CurrentRow == null) return null;
            var rowData = new Dictionary<string, string>();
            foreach (DataGridViewCell cell in resultsGrid.CurrentRow.Cells)
            {
                if (cell.OwningColumn != null)
                {
                    rowData[cell.OwningColumn.Name] = cell.Value?.ToString() ?? "";
                }
            }
            return rowData;
        }

        private void ResultsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (resultsGrid.Columns[e.ColumnIndex].Name == "TOTAL_RANK" && e.Value != null && int.TryParse(e.Value.ToString(), out int rank))
            {
                e.Value = rank + GetOrdinalSuffix(rank);
                e.FormattingApplied = true;
            }
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

        private async void EXAMSVIEW_Load_1(object sender, EventArgs e) { await LoadResults(); }
        private void EXAMSVIEW_Load(object sender, EventArgs e) { }
    }
}
