using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmFess : Form
    {
        private readonly IFeeRepository _feeRepository;
        private DataGridView feesGrid;
        private TextBox searchBox;
        private Label resultLabel;
        private DataTable feesTable;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SidebarBackColor = UiTheme.Navy;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public frmFess()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            _feeRepository = new FeeRepository(AppConfig.ConnectionString);

            BuildModernFeesView();
            NavigationSidebar.AddTo(this);
        }
        private void BuildModernFeesView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Fees";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1120, 680);

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
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 38, Text = "Fees", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Review fee records and open payment collection", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreatePrimaryButton("Make Payment", () => new frmFessPayment().Show()));
            actions.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadFees()));
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildFilterBar()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = SurfaceColor };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            searchBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), BorderStyle = BorderStyle.FixedSingle };
            searchBox.TextChanged += (sender, args) => ApplyFilter();
            resultLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9.5F) };
            layout.Controls.Add(searchBox, 0, 0);
            layout.Controls.Add(CreateSecondaryButton("Clear", () => { searchBox.Text = ""; ApplyFilter(); }), 1, 0);
            layout.Controls.Add(resultLabel, 2, 0);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildGridShell()
        {
            var shell = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(1) };
            feesGrid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = SurfaceColor, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false };
            UiTheme.StyleDataGrid(feesGrid);
            shell.Controls.Add(feesGrid);
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
            var button = new Button { Width = 120, Height = 36, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            button.Click += (sender, args) => action();
            return button;
        }

        private async System.Threading.Tasks.Task LoadFees()
        {
            try
            {
                resultLabel.Text = "Loading fees...";
                feesTable = await _feeRepository.GetFeesTableAsync();
                feesGrid.DataSource = feesTable;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("An error occurred: " + ex.Message, "Fees");
            }
        }

        private void ApplyFilter()
        {
            if (feesTable == null) return;
            string search = searchBox.Text.Trim().Replace("'", "''");
            feesTable.DefaultView.RowFilter = string.IsNullOrWhiteSpace(search) ? "" : "Convert([STUDENT ID], 'System.String') LIKE '%" + search + "%' OR [CLASS ID] LIKE '%" + search + "%' OR [FEE NAME] LIKE '%" + search + "%'";
            resultLabel.Text = feesTable.DefaultView.Count + " fee record(s)";
        }

        private async void frmFess_Load(object sender, EventArgs e) { await LoadFees(); }
        private void btnEdit_Click(object sender, EventArgs e) { new frmFessPayment().Show(); }
        private async void btnNew_Click(object sender, EventArgs e) { await LoadFees(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Close(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
    }
}
