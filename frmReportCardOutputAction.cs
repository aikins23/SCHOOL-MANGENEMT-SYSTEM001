using System;
using System.Drawing;
using System.Windows.Forms;
using KingdomPrep.Shared.Models;
using Guna.UI2.WinForms;

namespace kingdom_Preparatory_School_Management_System
{
    public enum OutputDialogAction
    {
        Cancel,
        Print,
        Save,
        Email
    }

    public partial class frmReportCardOutputAction : Form
    {
        public OutputDialogAction SelectedAction { get; private set; } = OutputDialogAction.Cancel;

        public frmReportCardOutputAction(int studentCount)
        {
            InitializeComponent();
            this.Icon = Common.Branding.AppIcon;

            Text = "Select Output Action";
            Size = new Size(400, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var lblPrompt = new Label
            {
                Text = $"Generate report cards for {studentCount} students. What would you like to do?",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnPrint = new Guna2Button { Text = "Send to Printer", Width = 300, Height = 45, FillColor = UiTheme.Navy, Margin = new Padding(40, 10, 40, 10) };
            btnPrint.Click += (s, e) => { SelectedAction = OutputDialogAction.Print; DialogResult = DialogResult.OK; Close(); };

            var btnSave = new Guna2Button { Text = "Save to Folder (PDFs)", Width = 300, Height = 45, FillColor = UiTheme.Navy, Margin = new Padding(40, 10, 40, 10) };
            btnSave.Click += (s, e) => { SelectedAction = OutputDialogAction.Save; DialogResult = DialogResult.OK; Close(); };

            var btnEmail = new Guna2Button { Text = "Bulk Email to Parents (PDFs)", Width = 300, Height = 45, FillColor = Color.SeaGreen, Margin = new Padding(40, 10, 40, 10) };
            btnEmail.Click += (s, e) => { SelectedAction = OutputDialogAction.Email; DialogResult = DialogResult.OK; Close(); };

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(0, 10, 0, 0)
            };

            pnlButtons.Controls.Add(btnPrint);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnEmail);

            Controls.Add(pnlButtons);
            Controls.Add(lblPrompt);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(400, 320);
            this.Name = "frmReportCardOutputAction";
            this.ResumeLayout(false);
        }
    }
}
