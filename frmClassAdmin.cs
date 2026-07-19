using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmClassAdmin : Form
    {
        private readonly ClassService _classService;
        private Guna.UI2.WinForms.Guna2DataGridView classGrid;
        private Guna.UI2.WinForms.Guna2TextBox txtClassName;
        private Guna.UI2.WinForms.Guna2TextBox txtFee;
        private Guna.UI2.WinForms.Guna2TextBox txtLevel;
#pragma warning disable CS0169
        private Label statusLabel;
#pragma warning restore CS0169

        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color Surface = UiTheme.Surface;

        public frmClassAdmin()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("frmClassAdmin", this)) return;

            // Initialize modern architecture
            var repository = new ClassRepository(AppConfig.ConnectionString);
            _classService = new ClassService(repository);
        }

        private async void frmClassAdmin_Load(object sender, EventArgs e)
        {
            await _classService.InitializeAsync();
            BuildLayout();
            NavigationSidebar.AddTo(this);
            UiTheme.Apply(this);
            await LoadClasses();
        }

        private void BuildLayout()
        {
            SuspendLayout();
            Text = "Class Administration";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(230, 20, 20, 20) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            // Left Side: Entry Form
            var entryPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            txtClassName = CreateInput("Class Name (e.g. BASIC 1)", 0);
            txtFee = CreateInput("Tuition Fee (GHS)", 40);
            txtLevel = CreateInput("Promotion Order (1, 2, 3...)", 80);

            var btnSave = new Guna.UI2.WinForms.Guna2Button { Text = "Save Class", FillColor = Navy, Width = 150, Height = 40, Location = new Point(10, 140), Cursor = Cursors.Hand };
            btnSave.Click += (s, e) => SaveClass();

            var btnDelete = new Guna.UI2.WinForms.Guna2Button { Text = "Delete", FillColor = Color.Firebrick, Width = 100, Height = 40, Location = new Point(170, 140), Cursor = Cursors.Hand };
            btnDelete.Click += (s, e) => DeleteClass();
            bool canManageClassStructure = AuthService.CanWrite("Academics.ClassStructure.Manage");
            txtClassName.Enabled = canManageClassStructure;
            txtFee.Enabled = canManageClassStructure;
            txtLevel.Enabled = canManageClassStructure;
            btnSave.Visible = canManageClassStructure;
            btnDelete.Visible = canManageClassStructure;

            entryPanel.Controls.Add(new Label { Text = "Add/Edit Class", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Navy, AutoSize = true });
            entryPanel.Controls.Add(txtClassName);
            entryPanel.Controls.Add(txtFee);
            entryPanel.Controls.Add(txtLevel);
            entryPanel.Controls.Add(btnSave);
            entryPanel.Controls.Add(btnDelete);
            root.Controls.Add(entryPanel, 0, 0);

            // Right Side: Grid
            classGrid = new Guna.UI2.WinForms.Guna2DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false };
            classGrid.ThemeStyle.HeaderStyle.BackColor = Navy;
            classGrid.SelectionChanged += (s, e) => {
                if (classGrid.CurrentRow != null) {
                    txtClassName.Text = classGrid.CurrentRow.Cells[0].Value.ToString();
                    txtFee.Text = classGrid.CurrentRow.Cells[1].Value.ToString();
                    txtLevel.Text = classGrid.CurrentRow.Cells[2].Value.ToString();
                }
            };
            root.Controls.Add(classGrid, 1, 0);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Guna.UI2.WinForms.Guna2TextBox CreateInput(string hint, int y)
        {
            return new Guna.UI2.WinForms.Guna2TextBox { PlaceholderText = hint, Width = 260, Height = 36, Location = new Point(10, 40 + y) };
        }

        private async System.Threading.Tasks.Task LoadClasses()
        {
            try {
                classGrid.DataSource = await _classService.GetClassesTableAsync();
            } catch (Exception ex) { UIHelper.ShowError(ex.Message, "Class Admin"); }
        }

        private async void SaveClass()
        {
            try
            {
                if (!AuthService.RequireWriteAccess("Academics.ClassStructure.Manage", "Save Class"))
                    return;

                if (!FormValidationHelper.ValidateRequired(txtClassName, "Class Name")) return;
                if (!FormValidationHelper.ValidateNumeric(txtFee, "Tuition Fee", out decimal fee)) return;
                if (!FormValidationHelper.ValidateNumeric(txtLevel, "Promotion Order", out decimal levelDec)) return;

                int level = (int)levelDec;

                if (!ConfirmationHelper.ConfirmSave($"Save class '{txtClassName.Text.Trim().ToUpperInvariant()}'?")) return;

                var config = new ClassConfig {
                    ClassName = txtClassName.Text.Trim().ToUpperInvariant(),
                    TuitionFee = fee,
                    PromotionLevel = level
                };

                var (success, message) = await _classService.SaveClassAsync(config);
                if (success) {
                    await LoadClasses();
                    UIHelper.ShowSuccess(message, "Class Admin");
                } else UIHelper.ShowError(message, "Class Admin");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("SaveClass failed", ex);
                UIHelper.ShowError("Save class failed: " + ex.Message, "Class Administration");
            }
        }

        private async void DeleteClass()
        {
            try
            {
                if (!AuthService.RequireWriteAccess("Academics.ClassStructure.Manage", "Delete Class"))
                    return;

                if (!FormValidationHelper.ValidateRequired(txtClassName, "Class Name")) return;
                if (!ConfirmationHelper.ConfirmDelete("Class", $"Class Name: {txtClassName.Text.Trim().ToUpperInvariant()}")) return;

                var (success, message) = await _classService.DeleteClassAsync(txtClassName.Text.Trim());
                if (success) {
                    await LoadClasses();
                    UIHelper.ShowSuccess(message, "Class Admin");
                } else UIHelper.ShowError(message, "Class Admin");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("DeleteClass failed", ex);
                UIHelper.ShowError("Delete class failed: " + ex.Message, "Class Administration");
            }
        }

        private void InitializeComponent() {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1084, 661);
            this.Name = "frmClassAdmin";
            this.Load += new System.EventHandler(this.frmClassAdmin_Load);
            this.ResumeLayout(false);
        }
    }
}
