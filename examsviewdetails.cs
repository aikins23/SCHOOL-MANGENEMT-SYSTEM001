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

            PopulateData();
            UiTheme.Apply(this);
            
            // Re-apply specific styling that the theme might have over-generalized
            StyleHeader();
            StyleSubjectLabels();
        }

        private void StyleHeader()
        {
            gunaPanel1.BackColor = UiTheme.Navy;
            gunaLabel1.ForeColor = Color.White;
            gunaLabel1.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        }

        private void StyleSubjectLabels()
        {
            // Subject Name labels (Blue background) need White text
            var subjectLabels = new[] { 
                gunaLabel2, gunaLabel7, gunaLabel11, gunaLabel15, 
                gunaLabel19, gunaLabel35, gunaLabel23, gunaLabel31, gunaLabel27 
            };

            foreach (var label in subjectLabels)
            {
                label.BackColor = Color.FromArgb(31, 99, 198); // Matches EXAMSVIEW PrimaryColor
                label.ForeColor = Color.White;
                label.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            }

            // Data labels (White background) should be centered and muted/navy
            var dataLabels = new[] {
                gunaLabel3, gunaLabel4, gunaLabel38, gunaLabel5,    // English
                gunaLabel9, gunaLabel8, gunaLabel47, gunaLabel6,    // Math
                gunaLabel13, gunaLabel12, gunaLabel48, gunaLabel10, // Science
                gunaLabel17, gunaLabel16, gunaLabel49, gunaLabel14, // Social
                gunaLabel21, gunaLabel20, gunaLabel50, gunaLabel18, // Computing
                gunaLabel37, gunaLabel36, gunaLabel51, gunaLabel34, // Career
                gunaLabel25, gunaLabel24, gunaLabel52, gunaLabel22, // Creative
                gunaLabel33, gunaLabel32, gunaLabel53, gunaLabel30, // RME
                gunaLabel29, gunaLabel28, gunaLabel54, gunaLabel26  // Ghanaian
            };

            foreach (var label in dataLabels)
            {
                label.ForeColor = UiTheme.Navy;
                label.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                label.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        private void PopulateData()
        {
            if (rowData == null) return;

            // Student Info
            guna2TextBox1.Text = GetVal("NAME");
            guna2TextBox2.Text = GetVal("TERMS");
            guna2TextBox3.Text = GetVal("YEAR");
            guna2TextBox4.Text = GetVal("CLASS");
            guna2TextBox5.Text = GetVal("TOTAL_RANK");

            // English
            gunaLabel3.Text = GetVal("ENG");
            gunaLabel4.Text = GetVal("ENG_POS");
            gunaLabel38.Text = GetVal("ENG_GRADE");
            gunaLabel5.Text = GetVal("ENG_REMARK");

            // Math
            gunaLabel9.Text = GetVal("MATHS");
            gunaLabel8.Text = GetVal("MATHS_POS");
            gunaLabel47.Text = GetVal("MATHS_GRADE");
            gunaLabel6.Text = GetVal("MATHS_REMARK");

            // Science
            gunaLabel13.Text = GetVal("SCI");
            gunaLabel12.Text = GetVal("SCI_POS");
            gunaLabel48.Text = GetVal("SCI_GRADE");
            gunaLabel10.Text = GetVal("SCI_REMARK");

            // Social Studies
            gunaLabel17.Text = GetVal("SOCIAL");
            gunaLabel16.Text = GetVal("SOCIAL_POS");
            gunaLabel49.Text = GetVal("SOCIAL_GRADE");
            gunaLabel14.Text = GetVal("SOCIAL_REMARK");

            // Computing
            gunaLabel21.Text = GetVal("COMP");
            gunaLabel20.Text = GetVal("COMP_POS");
            gunaLabel50.Text = GetVal("COMP_GRADE");
            gunaLabel18.Text = GetVal("COMP_REMARK");

            // Career Tech
            gunaLabel37.Text = GetVal("CAREER");
            gunaLabel36.Text = GetVal("CAREER_POS");
            gunaLabel51.Text = GetVal("CAREER_GRADE");
            gunaLabel34.Text = GetVal("CAREER_REMARK");

            // Creative Art
            gunaLabel25.Text = GetVal("CRE_ART");
            gunaLabel24.Text = GetVal("CRE_ART_POS");
            gunaLabel52.Text = GetVal("CRE_ART_GRADE");
            gunaLabel22.Text = GetVal("CRE_ART_REMARK");

            // RME
            gunaLabel33.Text = GetVal("RME");
            gunaLabel32.Text = GetVal("RME_POS");
            gunaLabel53.Text = GetVal("RME_GRADE");
            gunaLabel30.Text = GetVal("RME_REMARK");

            // Ghanaian Language
            gunaLabel29.Text = GetVal("GHA_LANG");
            gunaLabel28.Text = GetVal("GHA_LANG_POS");
            gunaLabel54.Text = GetVal("GHA_LANG_GRADE");
            gunaLabel26.Text = GetVal("GHA_LANG_REMARK");
        }

        private string GetVal(string key)
        {
            return rowData.ContainsKey(key) ? rowData[key] : "";
        }
    }
}