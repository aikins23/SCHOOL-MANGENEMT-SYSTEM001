using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class examsviewdetails : Form
    {
        private readonly Dictionary<string, string> rowData;
        private DataGridView subjectsGrid;
        private Label totalLabel;
        private Label rankLabel;
        private Dictionary<string, string> normalizedRowData;
        private List<GradeBand> previewGradeBands = Data.GradingSchemeRepository.LegacyBands();
        private bool previewRowsLoaded;
        private bool isGeneratingPdf;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SurfaceAlt = UiTheme.SurfaceAlt;
        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;
        private static readonly HashSet<string> MetadataColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "STUDENTID", "ID", "STDID", "NAME", "STUDENT", "CLASS", "TERMS", "TERM", "YEAR",
            "TOTALSCORE", "TOTALRANK", "CLASSRANK", "AVERAGESCORE"
        };

        private static readonly SubjectDefinition[] SubjectDefinitions =
        {
            new SubjectDefinition("English Language", "ENG", "ENGLISH", "ENGLISH LANGUAGE"),
            new SubjectDefinition("Mathematics", "MATH", "MATHS", "MATHEMATICS"),
            new SubjectDefinition("Integrated Science", "SCI", "SCIENCE", "INT. SCIENCE", "INTEGRATED SCIENCE"),
            new SubjectDefinition("Social Studies", "SOCIAL", "SOCIAL STUDIES"),
            new SubjectDefinition("Computing", "COMP", "COMPUTING", "ICT"),
            new SubjectDefinition("Career Technology", "CAREER", "CAREER TECH", "CAREER TECH.", "CARRER TECH.", "CAREER TECHNOLOGY"),
            new SubjectDefinition("Creative Art", "CRE_ART", "CREATIVE ART", "CREATIVE ARTS", "CREATIVE ARTS AND DESIGN"),
            new SubjectDefinition("Ghanaian Language", "GHA_LANG", "GHANAIAN LANGUAGE", "GHANAIAN LANG.", "GHANAIAN LANG"),
            new SubjectDefinition("R.M.E", "RME", "R.M.E", "REL. & MORAL EDU.", "RELIGIOUS AND MORAL EDUCATION")
        };

        public examsviewdetails(Dictionary<string, string> data)
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("examsviewdetails", this)) return;
            rowData = data ?? new Dictionary<string, string>();
            normalizedRowData = BuildNormalizedLookup(rowData);
            BuildReportCardView();
            Shown += async (sender, args) => await LoadPreviewSubjectRowsAsync();
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
            actions.Controls.Add(CreateButton("Generate PDF", async () => await GeneratePdfAsync(), true, 132));
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
            totalLabel.Text = FormatScore(Value("TOTAL_SCORE"));
            rankLabel.Text = FormatRank(Value("TOTAL_RANK"));

            layout.Controls.Add(CreateMetric("Student", Value("NAME")), 0, 0);
            layout.Controls.Add(CreateMetric("Class", Value("CLASS")), 1, 0);
            layout.Controls.Add(CreateMetric("Term", Value("TERMS")), 2, 0);
            layout.Controls.Add(CreateMetric("Average Score", totalLabel.Text), 3, 0);
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
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

            subjectsGrid.Columns["Subject"].FillWeight = 32;
            subjectsGrid.Columns["Score"].FillWeight = 12;
            subjectsGrid.Columns["Grade"].FillWeight = 12;
            subjectsGrid.Columns["Position"].FillWeight = 14;
            subjectsGrid.Columns["Remark"].FillWeight = 30;

            subjectsGrid.Rows.Add("Loading subject results...", "", "", "", "");
            panel.Controls.Add(subjectsGrid);
            return panel;
        }

        private Control BuildActions()
        {
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreateButton("Generate PDF Report", async () => await GeneratePdfAsync(), true, 174));
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

        private Button CreateButton(string text, Func<Task> action, bool primary, int width)
        {
            var button = new Button { Width = width, Height = 38, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = primary ? Navy : SurfaceColor, ForeColor = primary ? Color.White : TextColor };
            button.FlatAppearance.BorderColor = primary ? Navy : BorderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            button.Click += async (sender, args) => await action();
            return button;
        }

        private void PopulateSubjectRows()
        {
            var usedScoreKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var subject in SubjectDefinitions)
            {
                string actualScoreKey;
                string score = FindSubjectValue(subject.Aliases, "", out actualScoreKey);
                string grade = FindSubjectValue(subject.Aliases, "GRADE");
                string position = FindSubjectValue(subject.Aliases, "POS");
                string remark = FindSubjectValue(subject.Aliases, "REMARK");

                if (!string.IsNullOrWhiteSpace(actualScoreKey))
                {
                    usedScoreKeys.Add(NormalizeKey(actualScoreKey));
                }

                if (IsMeaningfulSubjectRow(score, grade, position, remark))
                {
                    subjectsGrid.Rows.Add(
                        subject.DisplayName,
                        FormatScore(score),
                        GradeForPreview(score, grade),
                        FormatRank(position),
                        RemarkForPreview(score, remark));
                }
            }

            foreach (var pair in rowData)
            {
                string normalizedKey = NormalizeKey(pair.Key);
                if (MetadataColumns.Contains(normalizedKey) || usedScoreKeys.Contains(normalizedKey) || IsRelatedSubjectColumn(normalizedKey))
                {
                    continue;
                }

                string score = pair.Value ?? "";
                if (!LooksLikeScore(score))
                {
                    continue;
                }

                string grade = FindSubjectValue(new[] { pair.Key }, "GRADE");
                string position = FindSubjectValue(new[] { pair.Key }, "POS");
                string remark = FindSubjectValue(new[] { pair.Key }, "REMARK");
                if (!IsMeaningfulSubjectRow(score, grade, position, remark))
                {
                    continue;
                }

                subjectsGrid.Rows.Add(
                    PrettySubjectName(pair.Key),
                    FormatScore(score),
                    GradeForPreview(score, grade),
                    FormatRank(position),
                    RemarkForPreview(score, remark));
            }

            if (subjectsGrid.Rows.Count == 0)
            {
                subjectsGrid.Rows.Add("No subject scores found", "", "", "", "");
            }
        }

        private async Task LoadPreviewSubjectRowsAsync()
        {
            if (previewRowsLoaded || subjectsGrid == null || IsDisposed)
            {
                return;
            }

            previewRowsLoaded = true;
            try
            {
                var repo = new Data.GradingSchemeRepository(AppConfig.ConnectionString);
                await repo.EnsureTableAsync();
                var bands = await repo.GetBandsAsync();
                previewGradeBands = bands != null && bands.Count > 0
                    ? bands.OrderByDescending(b => b.MinScore).ToList()
                    : Data.GradingSchemeRepository.LegacyBands();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Report-card preview grading scheme load skipped: " + ex.Message);
                previewGradeBands = Data.GradingSchemeRepository.LegacyBands();
            }

            if (IsDisposed || subjectsGrid == null)
            {
                return;
            }

            subjectsGrid.Rows.Clear();
            PopulateSubjectRows();
        }

        private async Task GeneratePdfAsync()
        {
            if (isGeneratingPdf) return;

            try
            {
                isGeneratingPdf = true;
                Cursor = Cursors.WaitCursor;

                string studentId = Value("StudentID");
                string studentName = Value("NAME");
                string term = Value("TERMS");
                string year = Value("YEAR");

                if (string.IsNullOrEmpty(studentId))
                {
                    UIHelper.ShowError("Student ID is missing. Cannot generate full report card.", "Generate PDF");
                    return;
                }

                if (string.IsNullOrEmpty(term))
                {
                    UIHelper.ShowWarning("Please ensure a term is selected.", "Generate PDF");
                    return;
                }

                // Show Save Dialog
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "PDF Files (*.pdf)|*.pdf";
                    sfd.FileName = $"ReportCard_{studentName.Replace(" ", "_")}_{term}_{year.Replace("/", "-")}.pdf";
                    sfd.Title = "Save Report Card PDF";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var remarksRepository = new Data.StudentTermRemarksRepository(AppConfig.ConnectionString);
                        var dataService = new Services.ReportCardDataService(AppConfig.ConnectionString, remarksRepository);
                        var pdfGenerator = new Services.ReportCardPDFGenerator();
                        var printer = new Services.ReportCardPrinter();
                        var savedPath = await Task.Run(async () =>
                        {
                            var reportData = await dataService.GetStudentReportCardDataAsync(studentId, term, year);
                            var pdfBytes = await pdfGenerator.GeneratePDFAsync(reportData);
                            return await printer.SaveToFileAsync(pdfBytes, sfd.FileName);
                        });

                        UIHelper.ShowSuccess($"Report card saved successfully to:\n{savedPath}", "Generate PDF");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("PDF Generation from preview failed", ex);
                UIHelper.ShowError("Failed to generate PDF: " + ex.Message, "Generate PDF");
            }
            finally
            {
                Cursor = Cursors.Default;
                isGeneratingPdf = false;
            }
        }

        private string Value(string key)
        {
            string normalized = NormalizeKey(key);
            if (normalizedRowData != null && normalizedRowData.TryGetValue(normalized, out string value))
            {
                return value ?? "";
            }

            return rowData.ContainsKey(key) ? rowData[key] : "";
        }

        private static Dictionary<string, string> BuildNormalizedLookup(Dictionary<string, string> source)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source)
            {
                string normalized = NormalizeKey(pair.Key);
                if (!lookup.ContainsKey(normalized))
                {
                    lookup[normalized] = pair.Value ?? "";
                }
            }

            return lookup;
        }

        private string FindSubjectValue(IEnumerable<string> aliases, string suffix)
        {
            string ignored;
            return FindSubjectValue(aliases, suffix, out ignored);
        }

        private string FindSubjectValue(IEnumerable<string> aliases, string suffix, out string actualKey)
        {
            actualKey = "";
            foreach (string alias in aliases)
            {
                foreach (string candidate in BuildCandidateKeys(alias, suffix))
                {
                    string normalized = NormalizeKey(candidate);
                    foreach (var pair in rowData)
                    {
                        if (string.Equals(NormalizeKey(pair.Key), normalized, StringComparison.OrdinalIgnoreCase))
                        {
                            actualKey = pair.Key;
                            return pair.Value ?? "";
                        }
                    }
                }
            }

            return "";
        }

        private static IEnumerable<string> BuildCandidateKeys(string alias, string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                yield return alias;
                yield break;
            }

            yield return alias + " " + suffix;
            yield return alias + "_" + suffix;
            yield return alias + suffix;
        }

        private static bool IsMeaningfulSubjectRow(string score, string grade, string position, string remark)
        {
            decimal parsedScore;
            if (TryParseScore(score, out parsedScore) && parsedScore > 0m)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(grade) || !string.IsNullOrWhiteSpace(remark))
            {
                return true;
            }

            int parsedPosition;
            return int.TryParse(position, out parsedPosition) && parsedPosition > 0;
        }

        private static bool LooksLikeScore(string value)
        {
            decimal ignored;
            return TryParseScore(value, out ignored);
        }

        private string GradeForPreview(string score, string storedGrade)
        {
            decimal parsedScore;
            if (TryParseScore(score, out parsedScore))
            {
                var band = PreviewBandForScore(parsedScore);
                return band == null ? "" : band.Code;
            }

            return storedGrade ?? "";
        }

        private string RemarkForPreview(string score, string storedRemark)
        {
            decimal parsedScore;
            if (TryParseScore(score, out parsedScore))
            {
                var band = PreviewBandForScore(parsedScore);
                return band == null ? "" : band.Label;
            }

            return storedRemark ?? "";
        }

        private GradeBand PreviewBandForScore(decimal score)
        {
            if (previewGradeBands == null || previewGradeBands.Count == 0)
            {
                previewGradeBands = Data.GradingSchemeRepository.LegacyBands();
            }

            foreach (var band in previewGradeBands)
            {
                if (score >= band.MinScore)
                {
                    return band;
                }
            }

            return previewGradeBands[previewGradeBands.Count - 1];
        }

        private static bool IsRelatedSubjectColumn(string normalizedKey)
        {
            return normalizedKey.EndsWith("GRADE", StringComparison.OrdinalIgnoreCase)
                || normalizedKey.EndsWith("REMARK", StringComparison.OrdinalIgnoreCase)
                || normalizedKey.EndsWith("POS", StringComparison.OrdinalIgnoreCase)
                || normalizedKey.EndsWith("POSITION", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatScore(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            decimal score;
            if (TryParseScore(value, out score))
            {
                return score == decimal.Truncate(score)
                    ? score.ToString("0.00", CultureInfo.CurrentCulture)
                    : score.ToString("0.00", CultureInfo.CurrentCulture);
            }

            return value;
        }

        private static bool TryParseScore(string value, out decimal score)
        {
            score = 0m;
            if (string.IsNullOrWhiteSpace(value)) return false;

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out score)
                || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out score);
        }

        private static string PrettySubjectName(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(key.Replace("_", " ").ToLowerInvariant());
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";

            var chars = new List<char>(key.Length);
            foreach (char c in key)
            {
                if (char.IsLetterOrDigit(c))
                {
                    chars.Add(char.ToUpperInvariant(c));
                }
            }

            return new string(chars.ToArray());
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

        private async void btn_view_Click(object sender, EventArgs e) { await GeneratePdfAsync(); }
        private void gunaLabel1_Click(object sender, EventArgs e) { }

        private sealed class SubjectDefinition
        {
            public SubjectDefinition(string displayName, params string[] aliases)
            {
                DisplayName = displayName;
                Aliases = aliases ?? new string[0];
            }

            public string DisplayName { get; private set; }
            public string[] Aliases { get; private set; }
        }
    }
}
