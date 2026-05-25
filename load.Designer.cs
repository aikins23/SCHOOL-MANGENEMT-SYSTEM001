namespace kingdom_Preparatory_School_Management_System
{
    partial class load
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// The actual UI is built entirely in load.cs :: BuildSplashScreen().
        /// This stub only initialises the PictureBox field (used for the logo)
        /// and sets the bare-minimum form properties so the designer does not
        /// complain.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // pictureBoxLogo — sized and positioned by BuildSplashScreen
            this.pictureBoxLogo.Name     = "pictureBoxLogo";
            this.pictureBoxLogo.TabIndex = 0;
            this.pictureBoxLogo.TabStop  = false;

            // Form basics — overridden in BuildSplashScreen
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize          = new System.Drawing.Size(680, 420);
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.None;
            this.Name                = "load";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text                = "";

            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.PictureBox pictureBoxLogo;
    }
}
