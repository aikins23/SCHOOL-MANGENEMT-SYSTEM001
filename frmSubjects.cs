using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Per-class subject editor: pick a class, edit/reorder its subjects, Save. Director/Admin/Headmaster.
    /// Persists to ClassSubjects and refreshes the SubjectCatalog cache.
    /// </summary>
    public class frmSubjects : Form
    {
        private readonly SubjectRepository _repo = new SubjectRepository(AppConfig.ConnectionString);
        private ListBox _classList, _subjectList;
        private TextBox _newSubject;
        private Button _addBtn, _removeBtn, _upBtn, _downBtn, _saveBtn, _cancelBtn;
        private Label _status;
        private string _currentClass;
        private bool _dirty;

        public frmSubjects()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmSubjects", this)) return;
            Load += async (s, e) => await InitAsync();
        }

        private void BuildUi()
        {
            Text = "Subjects";
            Size = new Size(720, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Subjects (per class)",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            _classList = new ListBox { Left = 16, Top = 60, Width = 200, Height = 400, Font = new Font("Segoe UI", 10F) };
            _classList.SelectedIndexChanged += async (s, e) => await OnClassChangedAsync();

            _subjectList = new ListBox { Left = 232, Top = 60, Width = 320, Height = 360, Font = new Font("Segoe UI", 10F) };

            _newSubject = new TextBox { Left = 232, Top = 428, Width = 200, Font = new Font("Segoe UI", 10F) };
            _addBtn = new Button { Left = 440, Top = 426, Width = 112, Height = 28, Text = "Add", FlatStyle = FlatStyle.Flat };
            _upBtn = new Button { Left = 564, Top = 60, Width = 120, Height = 30, Text = "Move Up", FlatStyle = FlatStyle.Flat };
            _downBtn = new Button { Left = 564, Top = 96, Width = 120, Height = 30, Text = "Move Down", FlatStyle = FlatStyle.Flat };
            _removeBtn = new Button { Left = 564, Top = 140, Width = 120, Height = 30, Text = "Remove", FlatStyle = FlatStyle.Flat };
            _saveBtn = new Button { Left = 232, Top = 470, Width = 130, Height = 36, Text = "Save Class", BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cancelBtn = new Button { Left = 372, Top = 470, Width = 100, Height = 36, Text = "Close", FlatStyle = FlatStyle.Flat };
            _status = new Label { Left = 16, Top = 470, Width = 200, Height = 36, ForeColor = AppConfig.Colors.MutedTextColor };

            _addBtn.Click += (s, e) => AddSubject();
            _removeBtn.Click += (s, e) => { if (_subjectList.SelectedIndex >= 0) { _subjectList.Items.RemoveAt(_subjectList.SelectedIndex); _dirty = true; } };
            _upBtn.Click += (s, e) => MoveSelected(-1);
            _downBtn.Click += (s, e) => MoveSelected(1);
            _saveBtn.Click += async (s, e) => await SaveAsync();
            _cancelBtn.Click += (s, e) => Close();

            Controls.Add(_classList); Controls.Add(_subjectList); Controls.Add(_newSubject);
            Controls.Add(_addBtn); Controls.Add(_upBtn); Controls.Add(_downBtn); Controls.Add(_removeBtn);
            Controls.Add(_saveBtn); Controls.Add(_cancelBtn); Controls.Add(_status);
            Controls.Add(title);
        }

        private async Task InitAsync()
        {
            try
            {
                await _repo.EnsureTableAsync();
                _classList.Items.Clear();
                foreach (var cls in AppConfig.ClassNames) _classList.Items.Add(cls);
                if (_classList.Items.Count > 0) _classList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load subjects: " + ex.Message, "Subjects");
            }
        }

        private async Task OnClassChangedAsync()
        {
            if (_classList.SelectedItem == null) return;
            string next = _classList.SelectedItem.ToString();
            if (_dirty && _currentClass != null && next != _currentClass)
            {
                if (UIHelper.ShowConfirmation($"Discard unsaved changes to {_currentClass}?", "Subjects") != DialogResult.Yes)
                {
                    _classList.SelectedItem = _currentClass; // revert selection
                    return;
                }
            }
            _currentClass = next;
            _dirty = false;
            try
            {
                var subs = await _repo.GetSubjectsForClassAsync(_currentClass);
                if (subs.Count == 0) subs = new List<string>(SubjectCatalog.LegacySubjects);
                _subjectList.Items.Clear();
                foreach (var s in subs) _subjectList.Items.Add(s);
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load class subjects: " + ex.Message, "Subjects");
            }
        }

        private void AddSubject()
        {
            string s = (_newSubject.Text ?? "").Trim();
            if (s.Length == 0) return;
            if (_subjectList.Items.Cast<object>().Any(x => string.Equals(x.ToString(), s, StringComparison.OrdinalIgnoreCase)))
            { UIHelper.ShowWarning("That subject is already in the list.", "Subjects"); return; }
            _subjectList.Items.Add(s);
            _newSubject.Text = "";
            _dirty = true;
        }

        private void MoveSelected(int delta)
        {
            int i = _subjectList.SelectedIndex;
            if (i < 0) return;
            int j = i + delta;
            if (j < 0 || j >= _subjectList.Items.Count) return;
            var item = _subjectList.Items[i];
            _subjectList.Items.RemoveAt(i);
            _subjectList.Items.Insert(j, item);
            _subjectList.SelectedIndex = j;
            _dirty = true;
        }

        private async Task SaveAsync()
        {
            if (_currentClass == null) return;
            var subs = _subjectList.Items.Cast<object>().Select(x => x.ToString().Trim())
                                   .Where(x => x.Length > 0).ToList();
            if (subs.Count == 0)
            { UIHelper.ShowWarning("Add at least one subject before saving.", "Subjects"); return; }

            _saveBtn.Enabled = false;
            try
            {
                await _repo.SaveSubjectsForClassAsync(_currentClass, subs);
                SubjectCatalog.Refresh();
                _dirty = false;
                _status.Text = "Saved " + DateTime.Now.ToString("HH:mm:ss") + ".";
                UIHelper.ShowSuccess($"Subjects saved for {_currentClass}.", "Subjects");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save: " + ex.Message, "Subjects");
            }
            finally { _saveBtn.Enabled = true; }
        }
    }
}
