using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Configure examination types for the school. Beyond End of Term, schools can
    /// add Mock (e.g. BECE mock for Basic 9), Mid-Term, Class Tests, Quizzes, etc.
    /// Each type carries its own weight toward report-card calculations.
    /// </summary>
    public class frmExamTypeManager : Form
    {
        private readonly IExamTypeRepository _repo;
        private ListView lvTypes;
        private TextBox txtName;
        private TextBox txtCode;
        private TextBox txtDescription;
        private NumericUpDown nudWeight;
        private CheckBox chkGraded;
        private CheckBox chkReportCard;
        private NumericUpDown nudOrder;
        private Button btnNew;
        private Button btnSave;
        private Button btnDeactivate;
        private Label lblTotalWeight;
        private Label lblStatus;
        private List<ExamType> _types = new List<ExamType>();
        private ExamType _selected;

        public frmExamTypeManager() : this(new ExamTypeRepository()) { }

        public frmExamTypeManager(IExamTypeRepository repo)
        {
            _repo = repo;
            InitializeComponent();
            this.Load += async (s, e) => await OnLoadAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Examination Types Configuration";
            this.Size = new Size(1050, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.MinimumSize = new Size(900, 560);

            var title = new Label
            {
                Text = "Examination Types Configuration",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(700, 28)
            };
            this.Controls.Add(title);

            var subtitle = new Label
            {
                Text = "Configure types like End of Term, Mid-Term, Mock (BECE), Class Test, Quiz.",
                Location = new Point(20, 45),
                Size = new Size(800, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            this.Controls.Add(subtitle);

            lvTypes = new ListView
            {
                Location = new Point(20, 75),
                Size = new Size(600, 420),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9F)
            };
            lvTypes.Columns.Add("Code", 70);
            lvTypes.Columns.Add("Name", 170);
            lvTypes.Columns.Add("Weight %", 80);
            lvTypes.Columns.Add("Graded", 70);
            lvTypes.Columns.Add("Report Card", 100);
            lvTypes.Columns.Add("Active", 70);
            lvTypes.SelectedIndexChanged += LvTypes_SelectedIndexChanged;
            this.Controls.Add(lvTypes);

            lblTotalWeight = new Label
            {
                Location = new Point(20, 500),
                Size = new Size(600, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210)
            };
            this.Controls.Add(lblTotalWeight);

            int x = 650, y = 75;
            AddLabel("Code:", x, y);
            txtCode = new TextBox { Location = new Point(x + 110, y - 3), Size = new Size(260, 25) };
            this.Controls.Add(txtCode);

            y += 35;
            AddLabel("Name:", x, y);
            txtName = new TextBox { Location = new Point(x + 110, y - 3), Size = new Size(260, 25) };
            this.Controls.Add(txtName);

            y += 35;
            AddLabel("Description:", x, y);
            txtDescription = new TextBox { Location = new Point(x + 110, y - 3), Size = new Size(260, 70), Multiline = true };
            this.Controls.Add(txtDescription);

            y += 80;
            AddLabel("Weight (%):", x, y);
            nudWeight = new NumericUpDown
            {
                Location = new Point(x + 110, y - 3),
                Size = new Size(100, 25),
                Maximum = 100,
                Minimum = 0,
                DecimalPlaces = 2,
                Increment = 5
            };
            this.Controls.Add(nudWeight);

            y += 35;
            AddLabel("Display Order:", x, y);
            nudOrder = new NumericUpDown
            {
                Location = new Point(x + 110, y - 3),
                Size = new Size(100, 25),
                Maximum = 100,
                Minimum = 0
            };
            this.Controls.Add(nudOrder);

            y += 35;
            chkGraded = new CheckBox
            {
                Text = "Is a Graded Exam",
                Location = new Point(x + 110, y),
                Size = new Size(260, 22),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkGraded);

            y += 28;
            chkReportCard = new CheckBox
            {
                Text = "Include in Report Card",
                Location = new Point(x + 110, y),
                Size = new Size(260, 22),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkReportCard);

            y += 40;
            btnNew = new Button
            {
                Text = "+ New",
                Location = new Point(x, y),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNew.Click += BtnNew_Click;
            this.Controls.Add(btnNew);

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(x + 125, y),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += async (s, e) => await SaveAsync();
            this.Controls.Add(btnSave);

            btnDeactivate = new Button
            {
                Text = "Deactivate",
                Location = new Point(x + 250, y),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(198, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDeactivate.Click += async (s, e) => await DeactivateAsync();
            this.Controls.Add(btnDeactivate);

            lblStatus = new Label
            {
                Location = new Point(20, 555),
                Size = new Size(1000, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblStatus);
        }

        private void AddLabel(string text, int x, int y)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(100, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });
        }

        private async Task OnLoadAsync()
        {
            try
            {
                await _repo.EnsureTableAsync();
                await _repo.SeedSystemTypesAsync();
                await ReloadAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load exam types: " + ex.Message);
            }
        }

        private async Task ReloadAsync()
        {
            _types = await _repo.GetAllAsync();
            lvTypes.Items.Clear();
            foreach (var t in _types)
            {
                var item = new ListViewItem(t.Code);
                item.SubItems.Add(t.Name + (t.IsSystemType ? " (System)" : ""));
                item.SubItems.Add(t.WeightPercentage.ToString("0.00"));
                item.SubItems.Add(t.IsGradedExam ? "Yes" : "No");
                item.SubItems.Add(t.IncludeInReportCard ? "Yes" : "No");
                item.SubItems.Add(t.IsActive ? "Yes" : "No");
                item.Tag = t;
                if (!t.IsActive) item.ForeColor = Color.Gray;
                lvTypes.Items.Add(item);
            }
            var total = await _repo.GetTotalWeightAsync();
            lblTotalWeight.Text = $"Total Active Weight (Report Card): {total:0.00}%  " +
                                   (total == 100 ? "✓ Balanced" : $"  (target 100%, diff {(total - 100):+0.00;-0.00;0.00})");
            lblTotalWeight.ForeColor = total == 100 ? Color.FromArgb(46, 125, 50) : Color.FromArgb(198, 40, 40);
        }

        private void LvTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvTypes.SelectedItems.Count == 0) return;
            _selected = (ExamType)lvTypes.SelectedItems[0].Tag;
            txtCode.Text = _selected.Code;
            txtCode.ReadOnly = _selected.IsSystemType;
            txtName.Text = _selected.Name;
            txtDescription.Text = _selected.Description ?? string.Empty;
            nudWeight.Value = _selected.WeightPercentage;
            nudOrder.Value = _selected.DisplayOrder;
            chkGraded.Checked = _selected.IsGradedExam;
            chkReportCard.Checked = _selected.IncludeInReportCard;
            btnDeactivate.Enabled = !_selected.IsSystemType && _selected.IsActive;
            SetStatus($"Editing '{_selected.Name}'");
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            _selected = null;
            ClearForm();
            txtCode.Focus();
            SetStatus("Creating new exam type.");
        }

        private void ClearForm()
        {
            txtCode.Text = string.Empty;
            txtCode.ReadOnly = false;
            txtName.Text = string.Empty;
            txtDescription.Text = string.Empty;
            nudWeight.Value = 0;
            nudOrder.Value = 0;
            chkGraded.Checked = true;
            chkReportCard.Checked = true;
            btnDeactivate.Enabled = false;
            lvTypes.SelectedItems.Clear();
        }

        private async Task SaveAsync()
        {
            var code = txtCode.Text.Trim().ToUpper();
            var name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Code and Name are required.");
                return;
            }
            try
            {
                if (_selected == null)
                {
                    var t = new ExamType
                    {
                        Code = code,
                        Name = name,
                        Description = txtDescription.Text.Trim(),
                        WeightPercentage = nudWeight.Value,
                        IsGradedExam = chkGraded.Checked,
                        IncludeInReportCard = chkReportCard.Checked,
                        DisplayOrder = (int)nudOrder.Value,
                        IsActive = true,
                        CreatedBy = AuthService.CurrentUser?.Username ?? "SYSTEM"
                    };
                    t.ExamTypeId = await _repo.CreateAsync(t);
                    SetStatus($"Created '{name}'.");
                }
                else
                {
                    _selected.Name = name;
                    _selected.Description = txtDescription.Text.Trim();
                    _selected.WeightPercentage = nudWeight.Value;
                    _selected.IsGradedExam = chkGraded.Checked;
                    _selected.IncludeInReportCard = chkReportCard.Checked;
                    _selected.DisplayOrder = (int)nudOrder.Value;
                    _selected.IsActive = true;
                    await _repo.UpdateAsync(_selected);
                    SetStatus($"Updated '{name}'.");
                }
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        private async Task DeactivateAsync()
        {
            if (_selected == null || _selected.IsSystemType) return;
            var confirm = MessageBox.Show($"Deactivate '{_selected.Name}'? Existing exams will keep this type.",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            try
            {
                await _repo.DeactivateAsync(_selected.ExamTypeId);
                await ReloadAsync();
                ClearForm();
                SetStatus("Deactivated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Deactivate failed: " + ex.Message);
            }
        }

        private void SetStatus(string msg) => lblStatus.Text = msg;
    }
}
