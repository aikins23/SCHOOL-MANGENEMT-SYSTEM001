using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class examsviewdetails : Form
    {
        private readonly Dictionary<string, string> rowData;
        private DataGridView subjectsGrid;
        private Label totalLabel;
        private Label rankLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SurfaceAlt = UiTheme.SurfaceAlt;
        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public examsviewdetails(Dictionary<string, string> data)
        {
            InitializeComponent();
            rowData = data ?? new Dictionary<string, string>();
            BuildReportCardView();
        }

        private void BuildReportCardView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Report Card";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 720);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = PageBackColor, Padding = new Padding(26) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildStudentSummary(), 0, 1);
            root.Controls.Add(BuildSubjectsPanel(), 0, 2);
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
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 40, Text = "Report Card Preview", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = Value("NAME") + " - " + Value("CLASS") + " - " + Value("TERMS") + " " + Value("YEAR"), ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreateButton("Generate PDF", GeneratePdf, true, 132));
            actions.Controls.Add(CreateButton("Results", () => { Close(); new EXAMSVIEW().Show(); }, false, 96));
            actions.Controls.Add(CreateButton("Enter Scores", () => new EXAMS().Show(), false, 120));

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildStudentSummary()
        {
            var panel = CreateSurfacePanel(new Padding(18), new Padding(0, 0, 0, 14));
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = SurfaceColor };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));

            totalLabel = new Label();
            rankLabel = new Label();
            totalLabel.Text = Value("TOTAL_SCORE");
            rankLabel.Text = FormatRank(Value("TOTAL_RANK"));

            layout.Controls.Add(CreateMetric("Student", Value("NAME")), 0, 0);
            layout.Controls.Add(CreateMetric("Class", Value("CLASS")), 1, 0);
            layout.Controls.Add(CreateMetric("Term", Value("TERMS")), 2, 0);
            layout.Controls.Add(CreateMetric("Total Score", totalLabel.Text), 3, 0);
            layout.Controls.Add(CreateMetric("Class Rank", rankLabel.Text), 4, 0);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildSubjectsPanel()
        {
            var panel = CreateSurfacePanel(new Padding(1), Padding.Empty);
            subjectsGrid = new DataGridView
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
                EnableHeadersVisualStyles = false
            };
            UiTheme.StyleDataGrid(subjectsGrid);
            subjectsGrid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            subjectsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            subjectsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            subjectsGrid.ColumnHeadersHeight = 38;
            subjectsGrid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            subjectsGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            subjectsGrid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;
            subjectsGrid.GridColor = BorderColor;
            subjectsGrid.Columns.Add("Subject", "Subject");
            subjectsGrid.Columns.Add("Score", "Score");
            subjectsGrid.Columns.Add("Grade", "Grade");
            subjectsGrid.Columns.Add("Position", "Position");
            subjectsGrid.Columns.Add("Remark", "Remark");

            AddSubject("English Language", "ENG");
            AddSubject("Mathematics", "MATHS");
            AddSubject("Integrated Science", "SCI");
            AddSubject("Social Studies", "SOCIAL");
            AddSubject("Computing", "COMP");
            AddSubject("Career Tech", "CAREER");
            AddSubject("Creative Art", "CRE_ART");
            AddSubject("Ghanaian Language", "GHA_LANG");
            AddSubject("R.M.E", "RME");
            panel.Controls.Add(subjectsGrid);
            return panel;
        }

        private Control BuildActions()
        {
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreateButton("Generate PDF Report", GeneratePdf, true, 174));
            actions.Controls.Add(CreateButton("Close", Close, false, 92));
            return actions;
        }

        private Control CreateMetric(string title, string value)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceAlt, Padding = new Padding(12), Margin = new Padding(0, 0, 10, 0) };
            panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = value, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true });
            panel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 22, Text = title, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 8.75F), TextAlign = ContentAlignment.MiddleLeft });
            return panel;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = padding, Margin = margin };
        }

        private Button CreateButton(string text, Action action, bool primary, int width)
        {
            var button = new Button { Width = width, Height = 38, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = primary ? Navy : SurfaceColor, ForeColor = primary ? Color.White : TextColor };
            button.FlatAppearance.BorderColor = primary ? Navy : BorderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            button.Click += (sender, args) => action();
            return button;
        }

        private void AddSubject(string subject, string key)
        {
            subjectsGrid.Rows.Add(subject, Value(key), Value(key + "_GRADE"), FormatRank(Value(key + "_POS")), Value(key + "_REMARK"));
        }

        private void GeneratePdf()
        {
            Services.ReportCardPdfService.Export(rowData);
        }

        private string Value(string key)
        {
            return rowData.ContainsKey(key) ? rowData[key] : "";
        }

        private string FormatRank(string value)
        {
            if (!int.TryParse(value, out int rank) || rank <= 0) return value;
            int lastTwo = rank % 100;
            if (lastTwo >= 11 && lastTwo <= 13) return rank + "th";
            switch (rank % 10)
            {
                case 1: return rank + "st";
                case 2: return rank + "nd";
                case 3: return rank + "rd";
                default: return rank + "th";
            }
        }

        private void btn_view_Click(object sender, EventArgs e) { GeneratePdf(); }
        private void gunaLabel1_Click(object sender, EventArgs e) { }
    }
}
