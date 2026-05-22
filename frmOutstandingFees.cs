using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmOutstandingFees : Form
    {
        private readonly IFeeRepository _feeRepository;
        private readonly StudentService _studentService;
        private Guna.UI2.WinForms.Guna2DataGridView feesGrid;
        private Guna.UI2.WinForms.Guna2ComboBox classFilter;
        private Guna.UI2.WinForms.Guna2TextBox searchBox;
        private Label lblSummary;
        private DataTable feesTable;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color Navy = UiTheme.Navy;

        public frmOutstandingFees()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            _feeRepository = new FeeRepository(AppConfig.ConnectionString);
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepoForStudentService = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepoForStudentService);

            BuildModernLayout();
            NavigationSidebar.AddTo(this);
            UiTheme.Apply(this);
        }

        private async void frmOutstandingFees_Load(object sender, EventArgs e)
        {
            await LoadClasses();
            await LoadOutstandingData();
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            Text = "Outstanding Fees Report";
            Size = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackColor;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(230, 20, 20, 20) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Fill };
            pnlHeader.Controls.Add(new Label { Text = "Outstanding Fees", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Navy, AutoSize = true, Location = new Point(0, 5) });
            pnlHeader.Controls.Add(new Label { Text = "Monitor unpaid balances and generate defaulters lists", Font = new Font("Segoe UI", 10), ForeColor = UiTheme.Muted, AutoSize = true, Location = new Point(2, 40) });
            
            var btnExport = new Guna.UI2.WinForms.Guna2Button { 
                Text = "Export Defaulters", 
                FillColor = Color.FromArgb(220, 53, 69), 
                Width = 160, 
                Height = 36, 
                Location = new Point(750, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand 
            };
            btnExport.Click += (s, e) => ExportDefaulters();
            pnlHeader.Controls.Add(btnExport);

            root.Controls.Add(pnlHeader, 0, 0);

            // Filter Bar
            var pnlFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 10, 0, 0) };
            
            classFilter = new Guna.UI2.WinForms.Guna2ComboBox { Width = 150, Height = 36 };
            classFilter.Items.Add("All Classes");
            classFilter.SelectedIndex = 0;
            classFilter.SelectedIndexChanged += (s, e) => ApplyFilters();

            searchBox = new Guna.UI2.WinForms.Guna2TextBox { Width = 200, Height = 36, PlaceholderText = "Search Student...", Margin = new Padding(10, 0, 0, 0) };
            searchBox.TextChanged += (s, e) => ApplyFilters();

            lblSummary = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = Navy, Margin = new Padding(20, 10, 0, 0) };

            pnlFilters.Controls.Add(new Label { Text = "Filter Class:", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
            pnlFilters.Controls.Add(classFilter);
            pnlFilters.Controls.Add(searchBox);
            pnlFilters.Controls.Add(lblSummary);

            root.Controls.Add(pnlFilters, 0, 1);

            // Grid
            feesGrid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 40,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            feesGrid.ThemeStyle.HeaderStyle.BackColor = Navy;
            feesGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            feesGrid.ThemeStyle.RowsStyle.Height = 35;
            
            root.Controls.Add(feesGrid, 0, 2);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private async System.Threading.Tasks.Task LoadClasses()
        {
            try
            {
                var classes = await _studentService.GetAllStudentsAsync();
                var uniqueClasses = classes.Select(s => s.ClassID).Distinct().OrderBy(c => c).ToList();
                foreach (var cls in uniqueClasses) classFilter.Items.Add(cls);
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadOutstandingData()
        {
            try
            {
                lblSummary.Text = "Loading...";
                feesTable = await _feeRepository.GetOutstandingBalancesTableAsync();
                feesGrid.DataSource = feesTable;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading fee data: " + ex.Message, "Outstanding Fees");
            }
        }

        private void ApplyFilters()
        {
            if (feesTable == null) return;

            var filters = new List<string>();
            string search = searchBox.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(search))
            {
                filters.Add($@"([Student Name] LIKE '%{search}%' OR Convert(ID, 'System.String') LIKE '%{search}%')");
            }

            if (classFilter.SelectedIndex > 0)
            {
                filters.Add($@"Class = '{classFilter.Text.Replace("'", "''")}'");
            }

            feesTable.DefaultView.RowFilter = string.Join(" AND ", filters);
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            decimal total = 0;
            int count = feesTable.DefaultView.Count;
            foreach (DataRowView row in feesTable.DefaultView)
            {
                total += Convert.ToDecimal(row["Balance Owed"]);
            }
            lblSummary.Text = $"Defaulters: {count} | Total Outstanding: GHS {total:N2}";
        }

        private void ExportDefaulters()
        {
            if (feesTable == null || feesTable.DefaultView.Count == 0)
            {
                UIHelper.ShowInfo("No data to export.", "Outstanding Fees");
                return;
            }

            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path = Path.Combine(desktop, $"Defaulters_List_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine("ID,Student Name,Class,Balance Owed,Last Payment");
                    foreach (DataRowView row in feesTable.DefaultView)
                    {
                        sw.WriteLine($"{row["ID"]},{row["Student Name"]},{row["Class"]},{row["Balance Owed"]},{row["Last Payment"]}");
                    }
                }
                
                UIHelper.ShowSuccess($"Defaulters list exported to Desktop:\n{Path.GetFileName(path)}", "Export Success");
            }
            catch (Exception ex) { UIHelper.ShowError("Export failed: " + ex.Message, "Outstanding Fees"); }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1164, 681);
            this.Name = "frmOutstandingFees";
            this.Load += new System.EventHandler(this.frmOutstandingFees_Load);
            this.ResumeLayout(false);
        }
    }
}
