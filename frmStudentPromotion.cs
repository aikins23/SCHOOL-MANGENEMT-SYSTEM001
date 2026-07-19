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
        private bool CanPromoteStudents => AuthService.CanWrite("Students.Promote");
        private Guna.UI2.WinForms.Guna2DataGridView studentGrid;
        private Guna.UI2.WinForms.Guna2ComboBox comboSourceClass;
        private Guna.UI2.WinForms.Guna2ComboBox comboTargetClass;
        private Guna.UI.WinForms.GunaLabel lblCount;
        private Guna.UI2.WinForms.Guna2Button _promoteButton;
        private Guna.UI2.WinForms.Guna2Button _selectAllButton;
        private Guna.UI2.WinForms.Guna2Button _clearSelectionButton;
        private Label _gridTitleLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color Navy = UiTheme.Navy;

        public frmStudentPromotion()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("frmStudentPromotion", this)) return;

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
            Size = new Size(1360, 820);
            MinimumSize = new Size(1220, 740);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackColor;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(250, 24, 28, 28),
                BackColor = PageBackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header Panel - Classic enterprise banner
            var pnlHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBackColor,
                Margin = Padding.Empty
            };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));

            var headerText = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            headerText.Controls.Add(new Label
            {
                Text = "Bulk Student Promotion",
                Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold),
                ForeColor = Navy,
                AutoSize = true,
                Location = new Point(0, 4)
            });
            headerText.Controls.Add(new Label
            {
                Text = "Advance selected students into the next class for the new academic year.",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(2, 45)
            });

            _promoteButton = CreatePrimaryButton(CanPromoteStudents ? "Promote Selected" : "Read only", UiTheme.Success);
            _promoteButton.Dock = DockStyle.Top;
            _promoteButton.Height = 44;
            _promoteButton.Margin = new Padding(0, 16, 0, 0);
            _promoteButton.Enabled = CanPromoteStudents;
            _promoteButton.Click += async (s, e) => await PromoteStudentsAsync();

            pnlHeader.Controls.Add(headerText, 0, 0);
            pnlHeader.Controls.Add(_promoteButton, 1, 0);
            root.Controls.Add(pnlHeader, 0, 0);

            // Selection Bar - Structured clean card
            var pnlSelection = new Guna.UI2.WinForms.Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = SurfaceColor,
                BorderColor = UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = 8,
                Padding = new Padding(20, 14, 20, 14),
                Margin = new Padding(0, 0, 0, 16)
            };
            pnlSelection.ShadowDecoration.Enabled = true;
            pnlSelection.ShadowDecoration.Depth = 2;
            pnlSelection.ShadowDecoration.Color = Color.FromArgb(18, 25, 25, 112);

            var flow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = Color.Transparent };
            flow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            flow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            flow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            flow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            flow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

            comboSourceClass = new Guna.UI2.WinForms.Guna2ComboBox { Dock = DockStyle.Top, Height = 38 };
            comboSourceClass.SelectedIndexChanged += async (s, e) => await LoadStudentList();

            comboTargetClass = new Guna.UI2.WinForms.Guna2ComboBox { Dock = DockStyle.Top, Height = 38 };

            lblCount = new Guna.UI.WinForms.GunaLabel
            {
                Text = "0 students",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Navy,
                AutoSize = true,
                Margin = new Padding(0, 26, 0, 0)
            };

            _selectAllButton = CreateSecondaryButton("Select all");
            _selectAllButton.Click += (s, e) => SetAllPromotionChecks(true);
            _clearSelectionButton = CreateSecondaryButton("Clear selection");
            _clearSelectionButton.Click += (s, e) => SetAllPromotionChecks(false);

            var actionBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent, Padding = new Padding(0, 24, 0, 0) };
            actionBar.Controls.Add(_selectAllButton);
            actionBar.Controls.Add(_clearSelectionButton);

            flow.Controls.Add(CreateFieldBlock("From Class", comboSourceClass), 0, 0);
            flow.Controls.Add(CreateFieldBlock("To Class", comboTargetClass), 1, 0);
            flow.Controls.Add(lblCount, 2, 0);
            flow.Controls.Add(new Label { Dock = DockStyle.Fill }, 3, 0);
            flow.Controls.Add(actionBar, 4, 0);

            pnlSelection.Controls.Add(flow);
            root.Controls.Add(pnlSelection, 0, 1);

            // Grid Card - Classic enterprise grid with no top padding gap
            var gridPanel = new Guna.UI2.WinForms.Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = SurfaceColor,
                BorderColor = UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = 8,
                Padding = new Padding(1),
                Margin = Padding.Empty
            };
            gridPanel.ShadowDecoration.Enabled = true;
            gridPanel.ShadowDecoration.Depth = 2;
            gridPanel.ShadowDecoration.Color = Color.FromArgb(18, 25, 25, 112);

            _gridTitleLabel = new Label
            {
                Text = "Students to Promote",
                Height = 44,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 0, 0, 0),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Navy,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var goldLine = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = UiTheme.Gold };

            studentGrid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 40,
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            UiTheme.StyleDataGrid(studentGrid, true);
            studentGrid.ThemeStyle.HeaderStyle.BackColor = Navy;
            studentGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            studentGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (studentGrid.IsCurrentCellDirty)
                {
                    studentGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            studentGrid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && studentGrid.Columns[e.ColumnIndex].Name == "SelectCol")
                {
                    UpdateSelectedCount();
                }
            };

            gridPanel.Controls.Add(studentGrid);
            gridPanel.Controls.Add(goldLine);
            gridPanel.Controls.Add(_gridTitleLabel);
            root.Controls.Add(gridPanel, 0, 2);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private static Control CreateFieldBlock(string label, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 0, 18, 0),
                BackColor = Color.Transparent
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            var labelControl = new Label
            {
                Text = label.ToUpperInvariant(),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            input.Dock = DockStyle.Fill;

            panel.Controls.Add(labelControl, 0, 0);
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private static Guna.UI2.WinForms.Guna2Button CreatePrimaryButton(string text, Color fill)
        {
            return new Guna.UI2.WinForms.Guna2Button
            {
                Text = text,
                FillColor = fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                BorderRadius = 7,
                Cursor = Cursors.Hand
            };
        }

        private static Guna.UI2.WinForms.Guna2Button CreateSecondaryButton(string text)
        {
            return new Guna.UI2.WinForms.Guna2Button
            {
                Text = text,
                Width = 105,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
                FillColor = Color.White,
                ForeColor = UiTheme.Navy,
                BorderColor = UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = 6,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
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

                if (comboSourceClass.Items.Count > 0)
                {
                    comboSourceClass.SelectedIndex = 0;
                }

                if (comboTargetClass.Items.Count > 1)
                {
                    comboTargetClass.SelectedIndex = 1;
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Failed to load classes in frmStudentPromotion", ex);
            }
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
                        ReadOnly = !CanPromoteStudents,
                        DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                    };
                    studentGrid.Columns.Insert(0, checkCol);
                }

                foreach (DataGridViewRow row in studentGrid.Rows) row.Cells["SelectCol"].Value = true;

                FormatStudentGridColumns();
                UpdateSelectedCount();
            }
            catch (Exception ex) { UIHelper.ShowError("Error loading students: " + ex.Message, "Promotion"); }
        }

        private void FormatStudentGridColumns()
        {
            if (studentGrid.Columns.Contains("SelectCol"))
            {
                studentGrid.Columns["SelectCol"].DisplayIndex = 0;
                studentGrid.Columns["SelectCol"].HeaderText = "Promote";
            }

            if (studentGrid.Columns.Contains("ID")) studentGrid.Columns["ID"].HeaderText = "Student ID";
            if (studentGrid.Columns.Contains("FIRST NAME")) studentGrid.Columns["FIRST NAME"].HeaderText = "First Name";
            if (studentGrid.Columns.Contains("LAST NAME")) studentGrid.Columns["LAST NAME"].HeaderText = "Last Name";
            if (studentGrid.Columns.Contains("GENDER")) studentGrid.Columns["GENDER"].HeaderText = "Gender";
            if (studentGrid.Columns.Contains("CLASS ID")) studentGrid.Columns["CLASS ID"].HeaderText = "Current Class";

            if (studentGrid.Columns.Contains("ID")) studentGrid.Columns["ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            if (studentGrid.Columns.Contains("ID")) studentGrid.Columns["ID"].Width = 110;
            if (studentGrid.Columns.Contains("GENDER")) studentGrid.Columns["GENDER"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            if (studentGrid.Columns.Contains("GENDER")) studentGrid.Columns["GENDER"].Width = 110;
            if (studentGrid.Columns.Contains("CLASS ID")) studentGrid.Columns["CLASS ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            if (studentGrid.Columns.Contains("CLASS ID")) studentGrid.Columns["CLASS ID"].Width = 140;
        }

        private void SetAllPromotionChecks(bool selected)
        {
            if (studentGrid?.Rows == null || !studentGrid.Columns.Contains("SelectCol")) return;

            foreach (DataGridViewRow row in studentGrid.Rows)
            {
                if (!row.IsNewRow)
                {
                    row.Cells["SelectCol"].Value = selected;
                }
            }

            studentGrid.EndEdit();
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            if (studentGrid?.Rows == null || !studentGrid.Columns.Contains("SelectCol"))
            {
                lblCount.Text = "0 selected";
                return;
            }

            var total = 0;
            var selected = 0;
            foreach (DataGridViewRow row in studentGrid.Rows)
            {
                if (row.IsNewRow) continue;
                total++;
                if (Convert.ToBoolean(row.Cells["SelectCol"].Value ?? false))
                {
                    selected++;
                }
            }

            lblCount.Text = $"{selected} selected / {total} students";
            if (_gridTitleLabel != null)
            {
                _gridTitleLabel.Text = total == 0
                    ? "Students to Promote"
                    : $"Students to Promote - {comboSourceClass.Text}";
            }
        }

        private async System.Threading.Tasks.Task PromoteStudentsAsync()
        {
            try
            {
                if (!AuthService.RequireWriteAccess("Students.Promote", "Promote students")) return;
                if (!FormValidationHelper.ValidateComboBox(comboSourceClass, "Source Class")) return;
                if (!FormValidationHelper.ValidateComboBox(comboTargetClass, "Target Class")) return;

                if (comboSourceClass.Text == comboTargetClass.Text)
                {
                    ConfirmationHelper.ShowWarning("Target class must be different from Source class.", "Promotion");
                    return;
                }

                var selectedIds = new List<string>();
                foreach (DataGridViewRow row in studentGrid.Rows)
                {
                    if (Convert.ToBoolean(row.Cells["SelectCol"].Value))
                    {
                        selectedIds.Add(row.Cells["ID"].Value.ToString());
                    }
                }

                if (selectedIds.Count == 0)
                {
                    ConfirmationHelper.ShowWarning("Select at least one student to promote.", "Promotion");
                    return;
                }

                if (!ConfirmationHelper.ConfirmBulkOperation($"promote to {comboTargetClass.Text}", selectedIds.Count)) return;

                SetPromotionBusy(true, "Promoting selected students...");
                var (success, message) = await _studentService.PromoteStudentsAsync(selectedIds, comboTargetClass.Text);

                if (success)
                {
                    UIHelper.ShowSuccess(message, "Promotion");
                    await LoadStudentList();
                }
                else UIHelper.ShowError(message, "Promotion");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("PromoteStudents failed", ex);
                UIHelper.ShowError("Promote students failed: " + ex.Message, "Student Promotion");
            }
            finally
            {
                SetPromotionBusy(false, lblCount.Text);
            }
        }

        private void SetPromotionBusy(bool busy, string status)
        {
            UseWaitCursor = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (_promoteButton != null) _promoteButton.Enabled = !busy && CanPromoteStudents;
            if (_selectAllButton != null) _selectAllButton.Enabled = !busy;
            if (_clearSelectionButton != null) _clearSelectionButton.Enabled = !busy;
            if (comboSourceClass != null) comboSourceClass.Enabled = !busy;
            if (comboTargetClass != null) comboTargetClass.Enabled = !busy;
            if (studentGrid != null) studentGrid.Enabled = !busy;
            if (lblCount != null && !string.IsNullOrWhiteSpace(status)) lblCount.Text = status;
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
