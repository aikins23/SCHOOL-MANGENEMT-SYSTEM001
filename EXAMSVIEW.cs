using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class EXAMSVIEW : Form
    {
        private readonly kum Aikins = new kum();
        private TextBox searchBox;
        private ComboBox classFilter;
        private DataGridView resultsGrid;
        private Label resultLabel;
        private DataTable resultsTable;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color SidebarBackColor = Color.FromArgb(17, 35, 58);
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        private const string ResultsQuery = @"
WITH RankedExams AS (
    SELECT
        std_name AS NAME,
        std_class AS CLASS,
        term AS TERMS,
        year AS YEAR,
        subject,
        gt / 2 AS gt,
        grade,
        remark,
        ROW_NUMBER() OVER (PARTITION BY std_class, term, year, subject ORDER BY gt DESC) AS RANK
    FROM dbo.examss
),
TotalScores AS (
    SELECT
        NAME,
        CLASS,
        TERMS,
        YEAR,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN gt END) AS ENG,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN grade END) AS ENG_GRADE,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN remark END) AS ENG_REMARK,
        MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN RANK END) AS ENG_POS,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN gt END) AS SCI,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN grade END) AS SCI_GRADE,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN remark END) AS SCI_REMARK,
        MAX(CASE WHEN subject = 'INT. SCIENCE' THEN RANK END) AS SCI_POS,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN gt END) AS MATHS,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN grade END) AS MATHS_GRADE,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN remark END) AS MATHS_REMARK,
        MAX(CASE WHEN subject = 'MATHEMATICS' THEN RANK END) AS MATHS_POS,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN gt END) AS SOCIAL,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN grade END) AS SOCIAL_GRADE,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN remark END) AS SOCIAL_REMARK,
        MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN RANK END) AS SOCIAL_POS,
        MAX(CASE WHEN subject = 'COMPUTING' THEN gt END) AS COMP,
        MAX(CASE WHEN subject = 'COMPUTING' THEN grade END) AS COMP_GRADE,
        MAX(CASE WHEN subject = 'COMPUTING' THEN remark END) AS COMP_REMARK,
        MAX(CASE WHEN subject = 'COMPUTING' THEN RANK END) AS COMP_POS,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN gt END) AS CAREER,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN grade END) AS CAREER_GRADE,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN remark END) AS CAREER_REMARK,
        MAX(CASE WHEN subject = 'CARRER TECH.' THEN RANK END) AS CAREER_POS,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN gt END) AS CRE_ART,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN grade END) AS CRE_ART_GRADE,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN remark END) AS CRE_ART_REMARK,
        MAX(CASE WHEN subject = 'CREATIVE ART' THEN RANK END) AS CRE_ART_POS,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN gt END) AS GHA_LANG,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN grade END) AS GHA_LANG_GRADE,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN remark END) AS GHA_LANG_REMARK,
        MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN RANK END) AS GHA_LANG_POS,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN gt END) AS RME,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN grade END) AS RME_GRADE,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN remark END) AS RME_REMARK,
        MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN RANK END) AS RME_POS,
        COALESCE(MAX(CASE WHEN subject = 'ENGLISH LANGUAGE' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'INT. SCIENCE' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'MATHEMATICS' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'SOCIAL STUDIES' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'COMPUTING' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'CARRER TECH.' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'CREATIVE ART' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'GHANAIAN LANG.' THEN gt END), 0) +
        COALESCE(MAX(CASE WHEN subject = 'REL. & MORAL EDU.' THEN gt END), 0) AS TOTAL_SCORE
    FROM RankedExams
    GROUP BY NAME, CLASS, TERMS, YEAR
)
SELECT
    UPPER(NAME) AS NAME,
    UPPER(CLASS) AS CLASS,
    UPPER(TERMS) AS TERMS,
    UPPER(YEAR) AS YEAR,
    COALESCE(ENG, 0) AS ENG,
    COALESCE(ENG_GRADE, '') AS ENG_GRADE,
    COALESCE(ENG_REMARK, '') AS ENG_REMARK,
    COALESCE(ENG_POS, 0) AS ENG_POS,
    COALESCE(SCI, 0) AS SCI,
    COALESCE(SCI_GRADE, '') AS SCI_GRADE,
    COALESCE(SCI_REMARK, '') AS SCI_REMARK,
    COALESCE(SCI_POS, 0) AS SCI_POS,
    COALESCE(MATHS, 0) AS MATHS,
    COALESCE(MATHS_GRADE, '') AS MATHS_GRADE,
    COALESCE(MATHS_REMARK, '') AS MATHS_REMARK,
    COALESCE(MATHS_POS, 0) AS MATHS_POS,
    COALESCE(SOCIAL, 0) AS SOCIAL,
    COALESCE(SOCIAL_GRADE, '') AS SOCIAL_GRADE,
    COALESCE(SOCIAL_REMARK, '') AS SOCIAL_REMARK,
    COALESCE(SOCIAL_POS, 0) AS SOCIAL_POS,
    COALESCE(COMP, 0) AS COMP,
    COALESCE(COMP_GRADE, '') AS COMP_GRADE,
    COALESCE(COMP_REMARK, '') AS COMP_REMARK,
    COALESCE(COMP_POS, 0) AS COMP_POS,
    COALESCE(CAREER, 0) AS CAREER,
    COALESCE(CAREER_GRADE, '') AS CAREER_GRADE,
    COALESCE(CAREER_REMARK, '') AS CAREER_REMARK,
    COALESCE(CAREER_POS, 0) AS CAREER_POS,
    COALESCE(CRE_ART, 0) AS CRE_ART,
    COALESCE(CRE_ART_GRADE, '') AS CRE_ART_GRADE,
    COALESCE(CRE_ART_REMARK, '') AS CRE_ART_REMARK,
    COALESCE(CRE_ART_POS, 0) AS CRE_ART_POS,
    COALESCE(GHA_LANG, 0) AS GHA_LANG,
    COALESCE(GHA_LANG_GRADE, '') AS GHA_LANG_GRADE,
    COALESCE(GHA_LANG_REMARK, '') AS GHA_LANG_REMARK,
    COALESCE(GHA_LANG_POS, 0) AS GHA_LANG_POS,
    COALESCE(RME, 0) AS RME,
    COALESCE(RME_GRADE, '') AS RME_GRADE,
    COALESCE(RME_REMARK, '') AS RME_REMARK,
    COALESCE(RME_POS, 0) AS RME_POS,
    TOTAL_SCORE,
    RANK() OVER (PARTITION BY CLASS, TERMS, YEAR ORDER BY TOTAL_SCORE DESC) AS TOTAL_RANK
FROM TotalScores";

        public EXAMSVIEW()
        {
            InitializeComponent();
            BuildModernResultsView();
        }

        private void BuildModernResultsView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Exam Results";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 720);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = PageBackColor, Padding = new Padding(26) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFilterBar(), 0, 1);
            root.Controls.Add(BuildGridShell(), 0, 2);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 38, Text = "Exam Results", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Review student result summaries and open printable details", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreatePrimaryButton("Add Result", () => new EXAMS().Show()));
            actions.Controls.Add(CreateSecondaryButton("Refresh", LoadResults));

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildFilterBar()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Padding = new Padding(18), BorderStyle = BorderStyle.FixedSingle };
            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = SurfaceColor };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

            searchBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), BorderStyle = BorderStyle.FixedSingle };
            searchBox.TextChanged += (sender, args) => ApplyFilters();
            classFilter = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            classFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();
            resultLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9.5F) };

            filters.Controls.Add(searchBox, 0, 0);
            filters.Controls.Add(classFilter, 1, 0);
            filters.Controls.Add(CreateSecondaryButton("Clear", ClearFilters), 2, 0);
            filters.Controls.Add(resultLabel, 3, 0);
            panel.Controls.Add(filters);
            return panel;
        }

        private Control BuildGridShell()
        {
            var shell = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(1) };
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
            resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            resultsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            resultsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            resultsGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            resultsGrid.GridColor = BorderColor;
            resultsGrid.CellDoubleClick += ResultsGrid_CellDoubleClick;
            shell.Controls.Add(resultsGrid);
            return shell;
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button { Width = 112, Height = 36, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            button.Click += (sender, args) => action();
            return button;
        }

        private void LoadResults()
        {
            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand(ResultsQuery, con))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                {
                    resultsTable = new DataTable();
                    adapter.Fill(resultsTable);
                    resultsGrid.DataSource = resultsTable;
                    LoadClasses();
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Results", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadClasses()
        {
            string selected = classFilter.Text;
            classFilter.Items.Clear();
            classFilter.Items.Add("All classes");
            if (resultsTable != null)
            {
                foreach (DataRow row in resultsTable.DefaultView.ToTable(true, "CLASS").Rows)
                {
                    classFilter.Items.Add(row["CLASS"].ToString());
                }
            }
            classFilter.SelectedIndex = classFilter.Items.Contains(selected) ? classFilter.Items.IndexOf(selected) : 0;
        }

        private void ApplyFilters()
        {
            if (resultsTable == null) return;
            var filters = new List<string>();
            string search = searchBox.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(search))
            {
                filters.Add("NAME LIKE '%" + search + "%'");
            }
            if (classFilter.SelectedIndex > 0)
            {
                filters.Add("CLASS = '" + classFilter.Text.Replace("'", "''") + "'");
            }
            resultsTable.DefaultView.RowFilter = string.Join(" AND ", filters);
            resultLabel.Text = resultsTable.DefaultView.Count + " result record(s)";
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            if (classFilter.Items.Count > 0) classFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        private void OpenSelectedResult()
        {
            if (resultsGrid.CurrentRow == null) return;
            var rowData = new Dictionary<string, string>();
            foreach (DataGridViewCell cell in resultsGrid.CurrentRow.Cells)
            {
                if (cell.OwningColumn != null)
                {
                    rowData[cell.OwningColumn.Name] = cell.Value?.ToString() ?? "";
                }
            }
            new examsviewdetails(rowData).Show();
        }

        private void ResultsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) OpenSelectedResult();
        }

        private void EXAMSVIEW_Load(object sender, EventArgs e) { LoadResults(); }
        private void gunaButton2_Click(object sender, EventArgs e) { LoadResults(); }
        private void txtID_TextChanged(object sender, EventArgs e) { ApplyFilters(); }
        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e) { OpenSelectedResult(); }

        private void EXAMSVIEW_Load_1(object sender, EventArgs e)
        {

        }
    }
}
