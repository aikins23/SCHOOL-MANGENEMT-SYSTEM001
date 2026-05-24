namespace kingdom_Preparatory_School_Management_System
{
    partial class GenerateReportCardsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox cmbClass;
        private System.Windows.Forms.ComboBox cmbTerm;
        private System.Windows.Forms.ComboBox cmbYear;
        private System.Windows.Forms.CheckBox chkPrintAll;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.ProgressBar prgProgress;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblClass;
        private System.Windows.Forms.Label lblTerm;
        private System.Windows.Forms.Label lblYear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Generate Report Cards";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Size = new System.Drawing.Size(500, 350);
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Class label and combo
            this.lblClass = new System.Windows.Forms.Label();
            this.lblClass.Text = "Class:";
            this.lblClass.Location = new System.Drawing.Point(20, 20);
            this.lblClass.AutoSize = true;

            this.cmbClass = new System.Windows.Forms.ComboBox();
            this.cmbClass.Location = new System.Drawing.Point(100, 20);
            this.cmbClass.Size = new System.Drawing.Size(150, 21);
            this.cmbClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // Term label and combo
            this.lblTerm = new System.Windows.Forms.Label();
            this.lblTerm.Text = "Term:";
            this.lblTerm.Location = new System.Drawing.Point(20, 60);
            this.lblTerm.AutoSize = true;

            this.cmbTerm = new System.Windows.Forms.ComboBox();
            this.cmbTerm.Location = new System.Drawing.Point(100, 60);
            this.cmbTerm.Size = new System.Drawing.Size(150, 21);
            this.cmbTerm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // Year label and combo
            this.lblYear = new System.Windows.Forms.Label();
            this.lblYear.Text = "Year:";
            this.lblYear.Location = new System.Drawing.Point(20, 100);
            this.lblYear.AutoSize = true;

            this.cmbYear = new System.Windows.Forms.ComboBox();
            this.cmbYear.Location = new System.Drawing.Point(100, 100);
            this.cmbYear.Size = new System.Drawing.Size(150, 21);
            this.cmbYear.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // Print all checkbox
            this.chkPrintAll = new System.Windows.Forms.CheckBox();
            this.chkPrintAll.Text = "Print All Students (Entire School)";
            this.chkPrintAll.Location = new System.Drawing.Point(20, 140);
            this.chkPrintAll.AutoSize = true;

            // Generate button
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.Location = new System.Drawing.Point(350, 20);
            this.btnGenerate.Size = new System.Drawing.Size(120, 30);
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);

            // Progress bar
            this.prgProgress = new System.Windows.Forms.ProgressBar();
            this.prgProgress.Location = new System.Drawing.Point(20, 200);
            this.prgProgress.Size = new System.Drawing.Size(450, 20);
            this.prgProgress.Visible = false;

            // Status label
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatus.Text = "Ready";
            this.lblStatus.Location = new System.Drawing.Point(20, 230);
            this.lblStatus.Size = new System.Drawing.Size(450, 20);

            // Add controls to form
            this.Controls.Add(this.lblClass);
            this.Controls.Add(this.cmbClass);
            this.Controls.Add(this.lblTerm);
            this.Controls.Add(this.cmbTerm);
            this.Controls.Add(this.lblYear);
            this.Controls.Add(this.cmbYear);
            this.Controls.Add(this.chkPrintAll);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.prgProgress);
            this.Controls.Add(this.lblStatus);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
