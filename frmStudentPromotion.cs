using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmStudentPromotion : Form
    {
        private readonly StudentService _studentService;
        private Guna.UI2.WinForms.Guna2DataGridView studentGrid;
        private Guna.UI2.WinForms.Guna2ComboBox comboSourceClass;
        private Guna.UI2.WinForms.Guna2ComboBox comboTargetClass;
        private Guna.UI.WinForms.GunaLabel lblCount;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color Navy = UiTheme.Navy;

        public frmStudentPromotion()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepo = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepo);

            BuildModernLayout();
            NavigationSidebar.AddTo(this);
            UiTheme.Apply(this);
        }

        private async void frmStudentPromotion_Load(object sender, EventArgs e)
        {
            await LoadClasses();
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            Text = "Student Promotion System";
            Size = new Size(1180, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackColor;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(230, 20, 20, 20) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Fill };
            pnlHeader.Controls.Add(new Label { Text = "Bulk Student Promotion", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Navy, AutoSize = true, Location = new Point(0, 5) });
            pnlHeader.Controls.Add(new Label { Text = "Advance students to the next grade level for the new academic year", Font = new Font("Segoe UI", 10), ForeColor = UiTheme.Muted, AutoSize = true, Location = new Point(2, 40) });
            root.Controls.Add(pnlHeader, 0, 0);

            // Selection Bar
            var pnlSelection = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Padding = new Padding(15), BorderStyle = BorderStyle.FixedSingle };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            
            comboSourceClass = new Guna.UI2.WinForms.Guna2ComboBox { Width = 180, Height = 36 };
            comboSourceClass.SelectedIndexChanged += async (s, e) => await LoadStudentList();

            comboTargetClass = new Guna.UI2.WinForms.Guna2ComboBox { Width = 180, Height = 36, Margin = new Padding(30, 0, 0, 0) };

            lblCount = new Guna.UI.WinForms.GunaLabel { Text = "0 students", Font = new Font("Segoe UI", 10), ForeColor = Navy, AutoSize = true, Margin = new Padding(20, 10, 0, 0) };

            flow.Controls.Add(new Label { Text = "FROM CLASS:", AutoSize = true, Margin = new Padding(0, 10, 5, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            flow.Controls.Add(comboSourceClass);
            flow.Controls.Add(new Label { Text = "TO CLASS:", AutoSize = true, Margin = new Padding(15, 10, 5, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            flow.Controls.Add(comboTargetClass);
            flow.Controls.Add(lblCount);

            pnlSelection.Controls.Add(flow);
            root.Controls.Add(pnlSelection, 0, 1);

            // Grid
            studentGrid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 40,
                ReadOnly = false,
                AllowUserToAddRows = false
            };
            studentGrid.ThemeStyle.HeaderStyle.BackColor = Navy;
            studentGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            
            root.Controls.Add(studentGrid, 0, 2);

            // Promotion Button
            var btnPromote = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Promote Selected Students",
                FillColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Width = 220,
                Height = 40,
                Location = new Point(900, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                BorderRadius = 5
            };
            btnPromote.Click += (s, e) => PromoteStudents();
            pnlHeader.Controls.Add(btnPromote);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private async System.Threading.Tasks.Task LoadClasses()
        {
            try
            {
                var classes = await _studentService.GetAllStudentsAsync();
                var uniqueClasses = classes.Select(s => s.ClassID).Distinct().OrderBy(c => c).ToList();
                
                foreach (var cid in uniqueClasses)
                {
                    comboSourceClass.Items.Add(cid);
                    comboTargetClass.Items.Add(cid);
                }
                comboTargetClass.Items.Add("GRADUATED");
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadStudentList()
        {
            if (comboSourceClass.SelectedIndex < 0) return;

            try
            {
                lblCount.Text = "Loading...";
                DataTable dt = await _studentService.GetStudentsTableAsync(null, comboSourceClass.Text);

                studentGrid.DataSource = dt;
                
                // Keep only ID, Name, Gender, Class columns
                foreach (DataGridViewColumn col in studentGrid.Columns)
                {
                    col.Visible = (col.Name == "ID" || col.Name == "FIRST NAME" || col.Name == "LAST NAME" || col.Name == "GENDER" || col.Name == "CLASS ID");
                }

                if (!studentGrid.Columns.Contains("SelectCol"))
                {
                    var checkCol = new DataGridViewCheckBoxColumn
                    {
                        Name = "SelectCol",
                        HeaderText = "Promote?",
                        Width = 80,
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                        DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                    };
                    studentGrid.Columns.Insert(0, checkCol);
                }

                foreach (DataGridViewRow row in studentGrid.Rows) row.Cells["SelectCol"].Value = true;
                lblCount.Text = $"{dt.Rows.Count} students found";
            }
            catch (Exception ex) { UIHelper.ShowError("Error loading students: " + ex.Message, "Promotion"); }
        }

        private async void PromoteStudents()
        {
            if (comboTargetClass.SelectedIndex < 0) { UIHelper.ShowWarning("Select a Target Class.", "Promotion"); return; }
            if (comboSourceClass.Text == comboTargetClass.Text) { UIHelper.ShowWarning("Target class must be different from Source class.", "Promotion"); return; }

            var selectedIds = new List<string>();
            foreach (DataGridViewRow row in studentGrid.Rows)
            {
                if (Convert.ToBoolean(row.Cells["SelectCol"].Value))
                {
                    selectedIds.Add(row.Cells["ID"].Value.ToString());
                }
            }

            if (selectedIds.Count == 0) { UIHelper.ShowWarning("Select at least one student to promote.", "Promotion"); return; }

            if (UIHelper.ShowConfirmation($"Promote {selectedIds.Count} students to {comboTargetClass.Text}?", "Confirm Promotion") != DialogResult.Yes) return;

            try
            {
                lblCount.Text = "Promoting...";
                var (success, message) = await _studentService.PromoteStudentsAsync(selectedIds, comboTargetClass.Text);

                if (success)
                {
                    UIHelper.ShowSuccess(message, "Promotion");
                    await LoadStudentList();
                }
                else UIHelper.ShowError(message, "Promotion");
            }
            catch (Exception ex) { UIHelper.ShowError("Promotion failed: " + ex.Message, "Promotion"); }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1164, 711);
            this.Name = "frmStudentPromotion";
            this.Load += new System.EventHandler(this.frmStudentPromotion_Load);
            this.ResumeLayout(false);
        }
    }
}
