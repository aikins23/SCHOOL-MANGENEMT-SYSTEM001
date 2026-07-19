namespace kingdom_Preparatory_School_Management_System
{
    partial class frmNotice
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
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.gunaLabel1 = new Guna.UI.WinForms.GunaLabel();
            this.gunaPictureBox1 = new Guna.UI.WinForms.GunaPictureBox();
            this.pnlSidebar = new Guna.UI2.WinForms.Guna2Panel();
            this.btnAll = new Guna.UI.WinForms.GunaButton();
            this.btnEvents = new Guna.UI.WinForms.GunaButton();
            this.btnAcademic = new Guna.UI.WinForms.GunaButton();
            this.btnHolidays = new Guna.UI.WinForms.GunaButton();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAddNotice = new Guna.UI.WinForms.GunaButton();
            this.guna2Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gunaPictureBox1)).BeginInit();
            this.pnlSidebar.SuspendLayout();
            this.SuspendLayout();
            //
            // guna2Panel1
            //
            this.guna2Panel1.BackColor = System.Drawing.Color.MidnightBlue;
            this.guna2Panel1.Controls.Add(this.gunaLabel1);
            this.guna2Panel1.Controls.Add(this.gunaPictureBox1);
            this.guna2Panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.guna2Panel1.Location = new System.Drawing.Point(0, 0);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.ShadowDecoration.Parent = this.guna2Panel1;
            this.guna2Panel1.Size = new System.Drawing.Size(900, 60);
            this.guna2Panel1.TabIndex = 0;
            //
            // gunaLabel1
            //
            this.gunaLabel1.AutoSize = true;
            this.gunaLabel1.Font = new System.Drawing.Font("Roboto Cn", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel1.ForeColor = System.Drawing.Color.White;
            this.gunaLabel1.Location = new System.Drawing.Point(12, 13);
            this.gunaLabel1.Name = "gunaLabel1";
            this.gunaLabel1.Size = new System.Drawing.Size(193, 33);
            this.gunaLabel1.TabIndex = 1;
            this.gunaLabel1.Text = "NOTICE BOARD";
            //
            // gunaPictureBox1
            //
            this.gunaPictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gunaPictureBox1.BaseColor = System.Drawing.Color.White;
            this.gunaPictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.gunaPictureBox1.Location = new System.Drawing.Point(853, 12);
            this.gunaPictureBox1.Name = "gunaPictureBox1";
            this.gunaPictureBox1.Size = new System.Drawing.Size(35, 35);
            this.gunaPictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.gunaPictureBox1.TabIndex = 0;
            this.gunaPictureBox1.TabStop = false;
            this.gunaPictureBox1.Click += new System.EventHandler(this.gunaPictureBox1_Click);
            //
            // pnlSidebar
            //
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pnlSidebar.Controls.Add(this.btnAddNotice);
            this.pnlSidebar.Controls.Add(this.btnHolidays);
            this.pnlSidebar.Controls.Add(this.btnAcademic);
            this.pnlSidebar.Controls.Add(this.btnEvents);
            this.pnlSidebar.Controls.Add(this.btnAll);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 60);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.ShadowDecoration.Parent = this.pnlSidebar;
            this.pnlSidebar.Size = new System.Drawing.Size(200, 540);
            this.pnlSidebar.TabIndex = 1;
            //
            // btnAll
            //
            this.btnAll.AnimationHoverSpeed = 0.07F;
            this.btnAll.AnimationSpeed = 0.03F;
            this.btnAll.BaseColor = System.Drawing.Color.Transparent;
            this.btnAll.BorderColor = System.Drawing.Color.Black;
            this.btnAll.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnAll.FocusedColor = System.Drawing.Color.Empty;
            this.btnAll.Font = new System.Drawing.Font("Roboto", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAll.ForeColor = System.Drawing.Color.Black;
            this.btnAll.Image = null;
            this.btnAll.ImageSize = new System.Drawing.Size(20, 20);
            this.btnAll.Location = new System.Drawing.Point(0, 20);
            this.btnAll.Name = "btnAll";
            this.btnAll.OnHoverBaseColor = System.Drawing.Color.MidnightBlue;
            this.btnAll.OnHoverBorderColor = System.Drawing.Color.Black;
            this.btnAll.OnHoverForeColor = System.Drawing.Color.White;
            this.btnAll.OnHoverImage = null;
            this.btnAll.OnPressedColor = System.Drawing.Color.Black;
            this.btnAll.Size = new System.Drawing.Size(200, 45);
            this.btnAll.TabIndex = 0;
            this.btnAll.Text = "All Notices";
            this.btnAll.TextOffsetX = 10;
            //
            // btnEvents
            //
            this.btnEvents.AnimationHoverSpeed = 0.07F;
            this.btnEvents.AnimationSpeed = 0.03F;
            this.btnEvents.BaseColor = System.Drawing.Color.Transparent;
            this.btnEvents.BorderColor = System.Drawing.Color.Black;
            this.btnEvents.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnEvents.FocusedColor = System.Drawing.Color.Empty;
            this.btnEvents.Font = new System.Drawing.Font("Roboto", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEvents.ForeColor = System.Drawing.Color.Black;
            this.btnEvents.Image = null;
            this.btnEvents.ImageSize = new System.Drawing.Size(20, 20);
            this.btnEvents.Location = new System.Drawing.Point(0, 71);
            this.btnEvents.Name = "btnEvents";
            this.btnEvents.OnHoverBaseColor = System.Drawing.Color.MidnightBlue;
            this.btnEvents.OnHoverBorderColor = System.Drawing.Color.Black;
            this.btnEvents.OnHoverForeColor = System.Drawing.Color.White;
            this.btnEvents.OnHoverImage = null;
            this.btnEvents.OnPressedColor = System.Drawing.Color.Black;
            this.btnEvents.Size = new System.Drawing.Size(200, 45);
            this.btnEvents.TabIndex = 1;
            this.btnEvents.Text = "Events";
            this.btnEvents.TextOffsetX = 10;
            //
            // btnAcademic
            //
            this.btnAcademic.AnimationHoverSpeed = 0.07F;
            this.btnAcademic.AnimationSpeed = 0.03F;
            this.btnAcademic.BaseColor = System.Drawing.Color.Transparent;
            this.btnAcademic.BorderColor = System.Drawing.Color.Black;
            this.btnAcademic.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnAcademic.FocusedColor = System.Drawing.Color.Empty;
            this.btnAcademic.Font = new System.Drawing.Font("Roboto", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAcademic.ForeColor = System.Drawing.Color.Black;
            this.btnAcademic.Image = null;
            this.btnAcademic.ImageSize = new System.Drawing.Size(20, 20);
            this.btnAcademic.Location = new System.Drawing.Point(0, 122);
            this.btnAcademic.Name = "btnAcademic";
            this.btnAcademic.OnHoverBaseColor = System.Drawing.Color.MidnightBlue;
            this.btnAcademic.OnHoverBorderColor = System.Drawing.Color.Black;
            this.btnAcademic.OnHoverForeColor = System.Drawing.Color.White;
            this.btnAcademic.OnHoverImage = null;
            this.btnAcademic.OnPressedColor = System.Drawing.Color.Black;
            this.btnAcademic.Size = new System.Drawing.Size(200, 45);
            this.btnAcademic.TabIndex = 2;
            this.btnAcademic.Text = "Academic";
            this.btnAcademic.TextOffsetX = 10;
            //
            // btnHolidays
            //
            this.btnHolidays.AnimationHoverSpeed = 0.07F;
            this.btnHolidays.AnimationSpeed = 0.03F;
            this.btnHolidays.BaseColor = System.Drawing.Color.Transparent;
            this.btnHolidays.BorderColor = System.Drawing.Color.Black;
            this.btnHolidays.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnHolidays.FocusedColor = System.Drawing.Color.Empty;
            this.btnHolidays.Font = new System.Drawing.Font("Roboto", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHolidays.ForeColor = System.Drawing.Color.Black;
            this.btnHolidays.Image = null;
            this.btnHolidays.ImageSize = new System.Drawing.Size(20, 20);
            this.btnHolidays.Location = new System.Drawing.Point(0, 173);
            this.btnHolidays.Name = "btnHolidays";
            this.btnHolidays.OnHoverBaseColor = System.Drawing.Color.MidnightBlue;
            this.btnHolidays.OnHoverBorderColor = System.Drawing.Color.Black;
            this.btnHolidays.OnHoverForeColor = System.Drawing.Color.White;
            this.btnHolidays.OnHoverImage = null;
            this.btnHolidays.OnPressedColor = System.Drawing.Color.Black;
            this.btnHolidays.Size = new System.Drawing.Size(200, 45);
            this.btnHolidays.TabIndex = 3;
            this.btnHolidays.Text = "Holidays";
            this.btnHolidays.TextOffsetX = 10;
            //
            // flowLayoutPanel1
            //
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(200, 60);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(20);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(700, 540);
            this.flowLayoutPanel1.TabIndex = 2;
            //
            // btnAddNotice
            //
            this.btnAddNotice.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddNotice.AnimationHoverSpeed = 0.07F;
            this.btnAddNotice.AnimationSpeed = 0.03F;
            this.btnAddNotice.BaseColor = System.Drawing.Color.MidnightBlue;
            this.btnAddNotice.BorderColor = System.Drawing.Color.Black;
            this.btnAddNotice.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnAddNotice.FocusedColor = System.Drawing.Color.Empty;
            this.btnAddNotice.Font = new System.Drawing.Font("Roboto Cn", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddNotice.ForeColor = System.Drawing.Color.White;
            this.btnAddNotice.Image = null;
            this.btnAddNotice.ImageSize = new System.Drawing.Size(20, 20);
            this.btnAddNotice.Location = new System.Drawing.Point(12, 483);
            this.btnAddNotice.Name = "btnAddNotice";
            this.btnAddNotice.OnHoverBaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.btnAddNotice.OnHoverBorderColor = System.Drawing.Color.Black;
            this.btnAddNotice.OnHoverForeColor = System.Drawing.Color.White;
            this.btnAddNotice.OnHoverImage = null;
            this.btnAddNotice.OnPressedColor = System.Drawing.Color.Black;
            this.btnAddNotice.Size = new System.Drawing.Size(176, 45);
            this.btnAddNotice.TabIndex = 4;
            this.btnAddNotice.Text = "POST NOTICE";
            this.btnAddNotice.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.btnAddNotice.Click += new System.EventHandler(this.btnAddNotice_Click);
            //
            // frmNotice
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.pnlSidebar);
            this.Controls.Add(this.guna2Panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmNotice";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Notice Board";
            this.Load += new System.EventHandler(this.frmNotice_Load);
            this.guna2Panel1.ResumeLayout(false);
            this.guna2Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gunaPictureBox1)).EndInit();
            this.pnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI.WinForms.GunaLabel gunaLabel1;
        private Guna.UI.WinForms.GunaPictureBox gunaPictureBox1;
        private Guna.UI2.WinForms.Guna2Panel pnlSidebar;
        private Guna.UI.WinForms.GunaButton btnAll;
        private Guna.UI.WinForms.GunaButton btnEvents;
        private Guna.UI.WinForms.GunaButton btnAcademic;
        private Guna.UI.WinForms.GunaButton btnHolidays;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private Guna.UI.WinForms.GunaButton btnAddNotice;
    }
}
