using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmNotice : Form
    {
        private NoticeRepository _repo = new NoticeRepository(AppConfig.ConnectionString);

        public frmNotice()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("frmNotice", this)) return;
        }

                private void frmNotice_Load(object sender, EventArgs e)
        {
            // Attach click events to sidebar buttons
            btnAll.Click += (s, ev) => LoadNotices("All");
            btnEvents.Click += (s, ev) => LoadNotices("Events");
            btnAcademic.Click += (s, ev) => LoadNotices("Academic");
            btnHolidays.Click += (s, ev) => LoadNotices("Holidays");

            LoadNotices("All");
        }

        private async void LoadNotices(string category)
        {
            try {
                flowLayoutPanel1.Controls.Clear();
                var notices = await _repo.GetAllAsync();

                // If there are no notices, show a placeholder
                if (!notices.Any()) {
                    AddNoticeCard("No Notices Found", DateTime.Now.ToString("yyyy-MM-dd"), "There are currently no announcements or notices available.", Color.Gray);
                    return;
                }

                foreach (var n in notices) {
                    Color c = Color.Green;
                    if (n.Target == "Parents") c = Color.Orange;
                    else if (n.Target == "Employees") c = Color.Blue;
                    else if (n.Target == "Specific Class") c = Color.Purple;

                    AddNoticeCard(n.Title, n.SentDate.ToString("yyyy-MM-dd HH:mm") + " - To: " + n.Target + (string.IsNullOrEmpty(n.TargetClass) ? "" : " (" + n.TargetClass + ")"), n.Message, c);
                }
            } catch (Exception ex) {
                MessageBox.Show("Failed to load notices: " + ex.Message);
            }
        }
private void AddNoticeCard(string title, string date, string content, Color priorityColor)
        {
            Guna.UI2.WinForms.Guna2ShadowPanel card = new Guna.UI2.WinForms.Guna2ShadowPanel();
            card.Size = new Size(flowLayoutPanel1.Width - 60, 150);
            card.BackColor = Color.Transparent;
            card.FillColor = Color.White;
            card.Radius = 10;
            card.ShadowColor = Color.LightGray;
            card.Padding = new Padding(10);
            card.Margin = new Padding(0, 0, 0, 20);

            Panel priorityLine = new Panel();
            priorityLine.BackColor = priorityColor;
            priorityLine.Dock = DockStyle.Left;
            priorityLine.Width = 5;
            card.Controls.Add(priorityLine);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Roboto Cn", 14, FontStyle.Bold);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            card.Controls.Add(lblTitle);

            Label lblDate = new Label();
            lblDate.Text = date;
            lblDate.Font = new Font("Roboto", 9, FontStyle.Italic);
            lblDate.ForeColor = Color.Gray;
            lblDate.Location = new Point(20, 45);
            lblDate.AutoSize = true;
            card.Controls.Add(lblDate);

            Label lblContent = new Label();
            lblContent.Text = content;
            lblContent.Font = new Font("Roboto", 11);
            lblContent.Location = new Point(20, 75);
            lblContent.Size = new Size(card.Width - 40, 60);
            lblContent.AutoEllipsis = true;
            card.Controls.Add(lblContent);

            flowLayoutPanel1.Controls.Add(card);
        }

        private void gunaPictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddNotice_Click(object sender, EventArgs e)
        {
            if (!AuthService.RequireWriteAccess("Admin.Notice.Send", "Open Send Notice"))
                return;

            new frmSendNotice().Show();
        }
    }
}
