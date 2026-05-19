using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class examsviewdetails : Form
    {
        private Dictionary<string, string> rowData;

        public examsviewdetails(Dictionary<string, string> data)
        {
            InitializeComponent();
            rowData = data;

            LoadDetails();
        }

        private void LoadDetails()
        {
            Text = "Student Result Details";
            BackColor = Color.White;
            Size = new Size(900, 700);

            RichTextBox detailsBox = new RichTextBox();
            detailsBox.Dock = DockStyle.Fill;
            detailsBox.Font = new Font("Segoe UI", 10);
            detailsBox.ReadOnly = true;

            foreach (var item in rowData)
            {
                detailsBox.AppendText(item.Key + " : " + item.Value + Environment.NewLine);
            }

            Controls.Add(detailsBox);
        }
    }
}