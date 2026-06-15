using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmNotice : Form
    {
        public frmNotice()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
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

        private void LoadNotices(string category)
        {
            flowLayoutPanel1.Controls.Clear();
            
            if (category == "All" || category == "Academic")
                AddNoticeCard("School Reopening", "2026-06-15", "School will reopen for the next term on Monday. Please ensure all fees are paid.", Color.Green);
            
            if (category == "All" || category == "Events")
                AddNoticeCard("Mid-Term Exams", "2026-07-10", "Mid-term examinations will commence on July 10th. Timetables are available at the office.", Color.Orange);
            
            if (category == "All" || category == "Holidays")
                AddNoticeCard("Holiday Notice", "2026-08-04", "The school will be closed on August 4th in observance of Founders' Day.", Color.Red);
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
            new frmSendNotice().Show();
        }
    }
}
