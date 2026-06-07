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
    /// Grading scheme settings: edit the grade bands (Min %, Code, Label). Director/Administrator.
    /// Saving persists the bands and refreshes the GradingScheme cache.
    /// </summary>
    public class frmGradingScheme : Form
    {
        private readonly GradingSchemeRepository _repo = new GradingSchemeRepository(AppConfig.ConnectionString);
        private DataGridView _grid;
        private Button _addBtn, _removeBtn, _saveBtn, _cancelBtn;
        private Label _status;

        public frmGradingScheme()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmGradingScheme", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "Grading Scheme";
            Size = new Size(620, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowIcon = false;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Grading Scheme",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            var hint = new Label
            {
                Dock = DockStyle.Top, Height = 24,
                Text = "  Score >= Min % maps to this Code (grade) and Label (remark). Include one row with Min % = 0.",
                ForeColor = AppConfig.Colors.MutedTextColor, Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Color.White,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinScore", HeaderText = "Min %", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Code", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label / Remark", FillWeight = 180 });

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 92, BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12) };
            _addBtn = new Button { Text = "Add Row", Left = 12, Top = 8, Width = 100, Height = 32, FlatStyle = FlatStyle.Flat };
            _removeBtn = new Button { Text = "Remove Row", Left = 120, Top = 8, Width = 110, Height = 32, FlatStyle = FlatStyle.Flat };
            _saveBtn = new Button { Text = "Save", Left = 12, Top = 48, Width = 120, Height = 34, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cancelBtn = new Button { Text = "Cancel", Left = 140, Top = 48, Width = 100, Height = 34, FlatStyle = FlatStyle.Flat };
            _status = new Label { Left = 250, Top = 54, Width = 340, Height = 24, ForeColor = AppConfig.Colors.MutedTextColor };
            _addBtn.Click += (s, e) => _grid.Rows.Add("0", "", "");
            _removeBtn.Click += (s, e) => { if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow) _grid.Rows.Remove(_grid.CurrentRow); };
            _saveBtn.Click += async (s, e) => await SaveAsync();
            _cancelBtn.Click += (s, e) => Close();
            bar.Controls.Add(_addBtn); bar.Controls.Add(_removeBtn);
            bar.Controls.Add(_saveBtn); bar.Controls.Add(_cancelBtn); bar.Controls.Add(_status);

            Controls.Add(_grid);
            Controls.Add(bar);
            Controls.Add(hint);
            Controls.Add(title);
        }

        private async Task LoadAsync()
        {
            try
            {
                await _repo.EnsureTableAsync();
                var bands = await _repo.GetBandsAsync();
                if (bands.Count == 0) bands = GradingSchemeRepository.LegacyBands();
                _grid.Rows.Clear();
                foreach (var b in bands)
                    _grid.Rows.Add(b.MinScore.ToString(), b.Code, b.Label);
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load grading scheme: " + ex.Message, "Grading Scheme");
            }
        }

        private async Task SaveAsync()
        {
            var bands = new List<GradeBand>();
            var seenMins = new HashSet<int>();
            bool hasFloor = false;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                string minText = row.Cells["MinScore"].Value?.ToString();
                if (!int.TryParse(minText, out int min) || min < 0 || min > 100)
                { UIHelper.ShowWarning("Each Min % must be a whole number between 0 and 100.", "Grading Scheme"); return; }
                if (!seenMins.Add(min))
                { UIHelper.ShowWarning($"Duplicate Min % {min}. Each band needs a distinct Min %.", "Grading Scheme"); return; }
                if (min == 0) hasFloor = true;
                bands.Add(new GradeBand
                {
                    MinScore = min,
                    Code = row.Cells["Code"].Value?.ToString() ?? "",
                    Label = row.Cells["Label"].Value?.ToString() ?? ""
                });
            }
            if (bands.Count == 0)
            { UIHelper.ShowWarning("Add at least one grade band.", "Grading Scheme"); return; }
            if (!hasFloor)
            { UIHelper.ShowWarning("Include one catch-all band with Min % = 0.", "Grading Scheme"); return; }

            _saveBtn.Enabled = false;
            try
            {
                await _repo.SaveBandsAsync(bands.OrderByDescending(b => b.MinScore));
                Common.GradingScheme.Refresh();
                _status.Text = "Saved " + DateTime.Now.ToString("HH:mm:ss") + ".";
                UIHelper.ShowSuccess("Grading scheme saved.", "Grading Scheme");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save: " + ex.Message, "Grading Scheme");
            }
            finally { _saveBtn.Enabled = true; }
        }
    }
}
