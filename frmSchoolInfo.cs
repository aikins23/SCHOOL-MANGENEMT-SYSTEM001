using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    /// School Information settings: identity (name, address, P.O. Box, GP address, phones,
    /// email, logo) and fees (admission fee + per-class term fees). Director/Administrator.
    /// Saving persists to the database and refreshes the SchoolProfile cache.
    /// </summary>
    public class frmSchoolInfo : Form
    {
        private readonly SchoolInfoRepository _repo = new SchoolInfoRepository(AppConfig.ConnectionString);

        private TextBox _name, _address, _poBox, _gps, _phone1, _phone2, _email, _admissionFee;
        private PictureBox _logo;
        private byte[] _logoBytes;
        private DataGridView _feeGrid;
        private Button _saveBtn, _cancelBtn, _uploadBtn, _previewBtn;
        private Panel _primarySwatch, _accentSwatch, _secondarySwatch;
        private Label _status;

        public frmSchoolInfo()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmSchoolInfo", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "School Information";
            Size = new Size(640, 760);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowIcon = false;
            BackColor = AppConfig.Colors.PageBackColor;
            AutoScroll = true;

            var title = new Label
            {
                Text = "  School Information", Dock = DockStyle.Top, Height = 44,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            int y = 56, lblX = 18, boxX = 170, boxW = 420, rowH = 32, gap = 8;
            Func<string, TextBox> addRow = caption =>
            {
                var l = new Label { Text = caption, Left = lblX, Top = y + 4, Width = boxX - lblX - 6, Font = new Font("Segoe UI", 10F) };
                var t = new TextBox { Left = boxX, Top = y, Width = boxW, Font = new Font("Segoe UI", 10F) };
                Controls.Add(l); Controls.Add(t);
                y += rowH + gap;
                return t;
            };

            Controls.Add(title);
            var idHdr = new Label { Text = "Identity", Left = lblX, Top = y, Width = 300, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppConfig.Colors.PrimaryColor };
            Controls.Add(idHdr); y += 28;

            _name = addRow("School Name");
            _address = addRow("Address");
            _poBox = addRow("P. O. Box");
            _gps = addRow("GP Address (Ghana Post GPS)");
            _phone1 = addRow("Phone 1");
            _phone2 = addRow("Phone 2");
            _email = addRow("Email");

            // Logo
            var logoLbl = new Label { Text = "Logo", Left = lblX, Top = y + 4, Width = boxX - lblX - 6, Font = new Font("Segoe UI", 10F) };
            _logo = new PictureBox { Left = boxX, Top = y, Width = 96, Height = 96, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
            _uploadBtn = new Button { Text = "Upload Logo…", Left = boxX + 110, Top = y + 30, Width = 130, Height = 32, FlatStyle = FlatStyle.Flat };
            _uploadBtn.Click += (s, e) => UploadLogo();
            Controls.Add(logoLbl); Controls.Add(_logo); Controls.Add(_uploadBtn);
            y += 96 + gap;

            // Fees
            var feeHdr = new Label { Text = "Fees", Left = lblX, Top = y, Width = 300, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppConfig.Colors.PrimaryColor };
            Controls.Add(feeHdr); y += 28;
            _admissionFee = addRow("Admission Fee (GHS)");

            var feeGridLbl = new Label { Text = "Per-Class Term Fees (GHS)", Left = lblX, Top = y, Width = 400, Font = new Font("Segoe UI", 10F) };
            Controls.Add(feeGridLbl); y += 26;
            _feeGrid = new DataGridView
            {
                Left = lblX, Top = y, Width = boxW + boxX - lblX, Height = 200,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
            };
            _feeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Class", HeaderText = "Class", ReadOnly = true });
            _feeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TermFee", HeaderText = "Term Fee" });
            Controls.Add(_feeGrid); y += _feeGrid.Height + gap;

            // Report Card Colours
            var colHdr = new Label { Text = "Report Card Colours", Left = lblX, Top = y, Width = 300, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppConfig.Colors.PrimaryColor };
            Controls.Add(colHdr); y += 28;

            _primarySwatch = AddColourRow("Primary (header)", ref y, lblX, boxX, rowH, gap);
            _accentSwatch = AddColourRow("Accent (logo / labels)", ref y, lblX, boxX, rowH, gap);
            _secondarySwatch = AddColourRow("Secondary (row tint)", ref y, lblX, boxX, rowH, gap);

            _previewBtn = new Button { Text = "Preview Report Card", Left = boxX, Top = y, Width = 200, Height = 32, FlatStyle = FlatStyle.Flat };
            _previewBtn.Click += async (s, e) => await PreviewAsync();
            Controls.Add(_previewBtn); y += rowH + gap + 6;

            _status = new Label { Left = lblX, Top = y, Width = boxW + boxX, Height = 22, ForeColor = AppConfig.Colors.MutedTextColor };
            Controls.Add(_status); y += 28;

            _saveBtn = new Button { Text = "Save", Left = boxX, Top = y, Width = 130, Height = 38, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cancelBtn = new Button { Text = "Cancel", Left = boxX + 140, Top = y, Width = 110, Height = 38, FlatStyle = FlatStyle.Flat };
            _saveBtn.Click += async (s, e) => await SaveAsync();
            _cancelBtn.Click += (s, e) => Close();
            Controls.Add(_saveBtn); Controls.Add(_cancelBtn);
        }

        private Panel AddColourRow(string caption, ref int y, int lblX, int boxX, int rowH, int gap)
        {
            var l = new Label { Text = caption, Left = lblX, Top = y + 4, Width = boxX - lblX - 6, Font = new Font("Segoe UI", 10F) };
            var swatch = new Panel { Left = boxX, Top = y, Width = 64, Height = 26, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            var btn = new Button { Text = "Change…", Left = boxX + 74, Top = y - 2, Width = 90, Height = 30, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => PickColour(swatch);
            Controls.Add(l); Controls.Add(swatch); Controls.Add(btn);
            y += rowH + gap;
            return swatch;
        }

        private void PickColour(Panel swatch)
        {
            using (var dlg = new ColorDialog { FullOpen = true, Color = swatch.BackColor })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) swatch.BackColor = dlg.Color;
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                await _repo.EnsureTablesAsync();
                var info = await _repo.GetAsync();
                var fees = await _repo.GetClassFeesAsync();

                _name.Text = info.Name; _address.Text = info.Address; _poBox.Text = info.PoBox;
                _gps.Text = info.GpsAddress; _phone1.Text = info.Phone1; _phone2.Text = info.Phone2;
                _email.Text = info.Email; _admissionFee.Text = info.AdmissionFee.ToString("0.##");
                _logoBytes = info.Logo;
                SetLogoPreview(info.Logo);

                _primarySwatch.BackColor = Color.FromArgb(info.PrimaryColorArgb);
                _accentSwatch.BackColor = Color.FromArgb(info.AccentColorArgb);
                _secondarySwatch.BackColor = Color.FromArgb(info.SecondaryColorArgb);

                _feeGrid.Rows.Clear();
                foreach (var className in AppConfig.ClassNames)
                {
                    decimal fee = fees.TryGetValue(className, out var f) ? f : SchoolInfoRepository.LegacyFeeForClass(className);
                    _feeGrid.Rows.Add(className, fee.ToString("0.##"));
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load school information: " + ex.Message, "School Information");
            }
        }

        private void SetLogoPreview(byte[] bytes)
        {
            try
            {
                if (bytes != null && bytes.Length > 0)
                {
                    using (var ms = new MemoryStream(bytes)) _logo.Image = Image.FromStream(ms);
                    return;
                }
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app_logo.png");
                if (File.Exists(path)) _logo.Image = Image.FromFile(path);
            }
            catch { /* preview is best-effort */ }
        }

        private void UploadLogo()
        {
            using (var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                if (!AppConfig.AllowedImageExtensions.Contains(ext))
                { UIHelper.ShowWarning("Unsupported image type.", "Logo"); return; }
                var bytes = File.ReadAllBytes(dlg.FileName);
                if (bytes.LongLength > AppConfig.MaxPhotoSizeBytes)
                { UIHelper.ShowWarning($"Logo must be under {AppConfig.MaxPhotoSizeMB} MB.", "Logo"); return; }
                _logoBytes = bytes;
                SetLogoPreview(bytes);
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            { UIHelper.ShowWarning("School name is required.", "School Information"); return; }
            if (!decimal.TryParse(_admissionFee.Text, out var admission) || admission < 0)
            { UIHelper.ShowWarning("Admission fee must be a number ≥ 0.", "School Information"); return; }

            var fees = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _feeGrid.Rows)
            {
                if (row.IsNewRow) continue;
                string cls = row.Cells["Class"].Value?.ToString();
                if (!decimal.TryParse(row.Cells["TermFee"].Value?.ToString(), out var fee) || fee < 0)
                { UIHelper.ShowWarning($"Term fee for {cls} must be a number ≥ 0.", "School Information"); return; }
                fees[cls] = fee;
            }

            _saveBtn.Enabled = false;
            try
            {
                var info = new SchoolInformation
                {
                    Name = _name.Text.Trim(), Address = _address.Text.Trim(), PoBox = _poBox.Text.Trim(),
                    GpsAddress = _gps.Text.Trim(), Phone1 = _phone1.Text.Trim(), Phone2 = _phone2.Text.Trim(),
                    Email = _email.Text.Trim(), Logo = _logoBytes, AdmissionFee = admission,
                    PrimaryColorArgb = _primarySwatch.BackColor.ToArgb(),
                    AccentColorArgb = _accentSwatch.BackColor.ToArgb(),
                    SecondaryColorArgb = _secondarySwatch.BackColor.ToArgb()
                };
                await _repo.SaveAsync(info);
                await _repo.SaveClassFeesAsync(fees);
                SchoolProfile.Refresh();
                _status.Text = "Saved " + DateTime.Now.ToString("HH:mm:ss") + ".";
                UIHelper.ShowSuccess("School information saved.", "School Information");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save: " + ex.Message, "School Information");
            }
            finally { _saveBtn.Enabled = true; }
        }

        private async Task PreviewAsync()
        {
            _previewBtn.Enabled = false;
            SchoolProfile.ReportColorOverride =
                (_primarySwatch.BackColor, _accentSwatch.BackColor, _secondarySwatch.BackColor);
            try
            {
                var sample = BuildSampleReportCard();
                var bytes = await new ReportCardPDFGenerator().GeneratePDFAsync(sample);
                string path = Path.Combine(Path.GetTempPath(),
                    "ReportCardPreview_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf");
                File.WriteAllBytes(path, bytes);
                try { System.Diagnostics.Process.Start(path); }
                catch { UIHelper.ShowWarning("Preview saved to:\n" + path + "\n(No PDF viewer found to open it.)", "Preview"); }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not generate preview: " + ex.Message, "Preview");
            }
            finally
            {
                SchoolProfile.ReportColorOverride = null;
                _previewBtn.Enabled = true;
            }
        }

        private ReportCardData BuildSampleReportCard()
        {
            return new ReportCardData
            {
                StudentID = "0",
                StudentName = "Sample Student",
                ClassID = "BASIC 5",
                Gender = "Male",
                Term = "TERM 3",
                Year = "2024/2025",
                PresentDays = 58,
                TotalSchoolDays = 60,
                OverallPosition = 3,
                TotalStudentsInClass = 30,
                SubjectResults = new List<SubjectResult>
                {
                    new SubjectResult { Subject = "Mathematics", ClassScore = 52, ExamScore = 88, TotalScore = 84, PositionInClass = 2 },
                    new SubjectResult { Subject = "English Language", ClassScore = 48, ExamScore = 74, TotalScore = 72, PositionInClass = 5 },
                    new SubjectResult { Subject = "Integrated Science", ClassScore = 40, ExamScore = 66, TotalScore = 66, PositionInClass = 8 }
                },
                Remarks = new StudentTermRemarks { StudentID = "0", Term = "TERM 3", Year = "2024/2025" },
                SchoolInfo = new SchoolInfo
                {
                    Name = SchoolProfile.Name,
                    Location = SchoolProfile.Address,
                    PhoneNumbers = SchoolProfile.Phones,
                    Logo = SchoolProfile.Logo
                }
            };
        }
    }
}
