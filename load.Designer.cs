namespace kingdom_Preparatory_School_Management_System
{
    partial class load
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(load));
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.labelSchoolName = new System.Windows.Forms.Label();
            this.labelTagline = new System.Windows.Forms.Label();
            this.labelLoadingDots = new System.Windows.Forms.Label();
            this.labelCredit = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // pictureBoxLogo
            this.pictureBoxLogo.Image = Properties.Resources.school_logo;
            this.pictureBoxLogo.Location = new System.Drawing.Point(293, 35);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(120, 120);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLogo.TabIndex = 0;
            this.pictureBoxLogo.TabStop = false;

            // labelSchoolName
            this.labelSchoolName.AutoSize = false;
            this.labelSchoolName.Font = new System.Drawing.Font("Segoe UI", 34F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSchoolName.ForeColor = System.Drawing.Color.White;
            this.labelSchoolName.Location = new System.Drawing.Point(103, 160);
            this.labelSchoolName.Name = "labelSchoolName";
            this.labelSchoolName.Size = new System.Drawing.Size(500, 50);
            this.labelSchoolName.TabIndex = 1;
            this.labelSchoolName.Text = "Kingdom Preparatory School";
            this.labelSchoolName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // labelTagline
            this.labelTagline.AutoSize = false;
            this.labelTagline.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTagline.ForeColor = System.Drawing.Color.LightGray;
            this.labelTagline.Location = new System.Drawing.Point(103, 220);
            this.labelTagline.Name = "labelTagline";
            this.labelTagline.Size = new System.Drawing.Size(500, 30);
            this.labelTagline.TabIndex = 2;
            this.labelTagline.Text = "KNOWLEDGE IS POWER";
            this.labelTagline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // labelLoadingDots
            this.labelLoadingDots.AutoSize = false;
            this.labelLoadingDots.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLoadingDots.ForeColor = System.Drawing.Color.White;
            this.labelLoadingDots.Location = new System.Drawing.Point(253, 260);
            this.labelLoadingDots.Name = "labelLoadingDots";
            this.labelLoadingDots.Size = new System.Drawing.Size(200, 40);
            this.labelLoadingDots.TabIndex = 3;
            this.labelLoadingDots.Text = "● ● ● ●";
            this.labelLoadingDots.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // labelCredit
            this.labelCredit.AutoSize = false;
            this.labelCredit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCredit.ForeColor = System.Drawing.Color.LightGray;
            this.labelCredit.Location = new System.Drawing.Point(103, 345);
            this.labelCredit.Name = "labelCredit";
            this.labelCredit.Size = new System.Drawing.Size(500, 20);
            this.labelCredit.TabIndex = 4;
            this.labelCredit.Text = "DEVELOPED BY: DARKTECH HUB";
            this.labelCredit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // load
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(706, 375);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Controls.Add(this.labelCredit);
            this.Controls.Add(this.labelLoadingDots);
            this.Controls.Add(this.labelTagline);
            this.Controls.Add(this.labelSchoolName);
            this.Controls.Add(this.pictureBoxLogo);
            this.Font = new System.Drawing.Font("Roboto Cn", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "load";
            this.Text = "load";
            this.Load += new System.EventHandler(this.load_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelSchoolName;
        private System.Windows.Forms.Label labelTagline;
        private System.Windows.Forms.Label labelLoadingDots;
        private System.Windows.Forms.Label labelCredit;
    }
}
