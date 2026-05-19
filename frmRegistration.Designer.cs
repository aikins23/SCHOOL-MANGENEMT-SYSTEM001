namespace kingdom_Preparatory_School_Management_System
{
    partial class frmRegistration
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmRegistration));
            this.lab_Log_in = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.Check_Pass = new System.Windows.Forms.CheckBox();
            this.Cmb_userTY = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.BTN_Register = new Guna.UI.WinForms.GunaButton();
            this.TXTCON_Pass = new Guna.UI2.WinForms.Guna2TextBox();
            this.TXTPass = new Guna.UI2.WinForms.Guna2TextBox();
            this.TXTUsers = new Guna.UI2.WinForms.Guna2TextBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label8 = new System.Windows.Forms.Label();
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.guna2Panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lab_Log_in
            // 
            this.lab_Log_in.AutoSize = true;
            this.lab_Log_in.BackColor = System.Drawing.Color.Transparent;
            this.lab_Log_in.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lab_Log_in.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lab_Log_in.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lab_Log_in.Location = new System.Drawing.Point(56, 629);
            this.lab_Log_in.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lab_Log_in.Name = "lab_Log_in";
            this.lab_Log_in.Size = new System.Drawing.Size(102, 29);
            this.lab_Log_in.TabIndex = 11;
            this.lab_Log_in.Text = "LOG IN";
            this.lab_Log_in.Click += new System.EventHandler(this.lab_Log_in_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Roboto", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(56, 581);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(170, 17);
            this.label7.TabIndex = 10;
            this.label7.Text = "Already have an Account?";
            // 
            // Check_Pass
            // 
            this.Check_Pass.AutoSize = true;
            this.Check_Pass.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Check_Pass.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Check_Pass.Font = new System.Drawing.Font("Roboto Cn", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Check_Pass.Location = new System.Drawing.Point(255, 453);
            this.Check_Pass.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Check_Pass.Name = "Check_Pass";
            this.Check_Pass.Size = new System.Drawing.Size(129, 21);
            this.Check_Pass.TabIndex = 8;
            this.Check_Pass.Text = "SHOW PASSWORD";
            this.Check_Pass.UseVisualStyleBackColor = true;
            this.Check_Pass.CheckedChanged += new System.EventHandler(this.Check_Pass_CheckedChanged);
            // 
            // Cmb_userTY
            // 
            this.Cmb_userTY.BackColor = System.Drawing.Color.MidnightBlue;
            this.Cmb_userTY.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.Cmb_userTY.Font = new System.Drawing.Font("Roboto Cn", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Cmb_userTY.ForeColor = System.Drawing.Color.White;
            this.Cmb_userTY.FormattingEnabled = true;
            this.Cmb_userTY.Items.AddRange(new object[] {
            "Admin",
            "Normal User"});
            this.Cmb_userTY.Location = new System.Drawing.Point(56, 411);
            this.Cmb_userTY.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Cmb_userTY.Name = "Cmb_userTY";
            this.Cmb_userTY.Size = new System.Drawing.Size(339, 37);
            this.Cmb_userTY.TabIndex = 1;
            this.Cmb_userTY.Text = "---SELECT USER TYPE--";
            this.Cmb_userTY.SelectedIndexChanged += new System.EventHandler(this.Cmb_userTY_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label5.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label5.Location = new System.Drawing.Point(56, 383);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 25);
            this.label5.TabIndex = 7;
            this.label5.Text = "UserType";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label4.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label4.Location = new System.Drawing.Point(56, 284);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(171, 25);
            this.label4.TabIndex = 5;
            this.label4.Text = "Confirm Password";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Roboto", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(151, 431);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(308, 138);
            this.label11.TabIndex = 12;
            this.label11.Text = "-Student Admission/Registration\r\n-Employers Registration\r\n-Financial Analysis\r\n-E" +
    "xams with Report Cards Printout\r\n-summary DashBord\r\n-Many More";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.White;
            this.label10.Font = new System.Drawing.Font("Roboto Cn", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label10.Location = new System.Drawing.Point(105, 368);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(442, 57);
            this.label10.TabIndex = 11;
            this.label10.Text = "SOFTWARE FEATURES";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.MidnightBlue;
            this.label9.Font = new System.Drawing.Font("Roboto Cn", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(89, 70);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(445, 41);
            this.label9.TabIndex = 9;
            this.label9.Text = "SCHOOL MANGEMENT SYSTEM";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Roboto Cn", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.MidnightBlue;
            this.label6.Location = new System.Drawing.Point(71, 20);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(488, 41);
            this.label6.TabIndex = 8;
            this.label6.Text = "KINGDOM PREPARATORY SCHOOL";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label3.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label3.Location = new System.Drawing.Point(56, 194);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 25);
            this.label3.TabIndex = 3;
            this.label3.Text = "Password";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label2.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label2.Location = new System.Drawing.Point(56, 114);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 25);
            this.label2.TabIndex = 1;
            this.label2.Text = "Username";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.Color.MidnightBlue;
            this.label12.Font = new System.Drawing.Font("Roboto Cn", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.White;
            this.label12.Location = new System.Drawing.Point(103, 574);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(208, 24);
            this.label12.TabIndex = 13;
            this.label12.Text = "DARKTECH IMPLICATION";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Roboto Cn", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.MidnightBlue;
            this.label1.Location = new System.Drawing.Point(72, 54);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(252, 41);
            this.label1.TabIndex = 0;
            this.label1.Text = "GET REGISTERED";
            // 
            // panel1
            // 
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panel1.Controls.Add(this.BTN_Register);
            this.panel1.Controls.Add(this.TXTCON_Pass);
            this.panel1.Controls.Add(this.TXTPass);
            this.panel1.Controls.Add(this.TXTUsers);
            this.panel1.Controls.Add(this.pictureBox2);
            this.panel1.Controls.Add(this.lab_Log_in);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.Check_Pass);
            this.panel1.Controls.Add(this.Cmb_userTY);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(599, 16);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(416, 686);
            this.panel1.TabIndex = 7;
            // 
            // BTN_Register
            // 
            this.BTN_Register.AnimationHoverSpeed = 0.07F;
            this.BTN_Register.AnimationSpeed = 0.03F;
            this.BTN_Register.BackColor = System.Drawing.Color.Transparent;
            this.BTN_Register.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.BTN_Register.BaseColor = System.Drawing.Color.MidnightBlue;
            this.BTN_Register.BorderColor = System.Drawing.Color.Black;
            this.BTN_Register.DialogResult = System.Windows.Forms.DialogResult.None;
            this.BTN_Register.FocusedColor = System.Drawing.Color.Empty;
            this.BTN_Register.Font = new System.Drawing.Font("Roboto Cn", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BTN_Register.ForeColor = System.Drawing.Color.White;
            this.BTN_Register.Image = null;
            this.BTN_Register.ImageSize = new System.Drawing.Size(25, 25);
            this.BTN_Register.Location = new System.Drawing.Point(101, 481);
            this.BTN_Register.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.BTN_Register.Name = "BTN_Register";
            this.BTN_Register.OnHoverBaseColor = System.Drawing.Color.MidnightBlue;
            this.BTN_Register.OnHoverBorderColor = System.Drawing.Color.White;
            this.BTN_Register.OnHoverForeColor = System.Drawing.Color.White;
            this.BTN_Register.OnHoverImage = null;
            this.BTN_Register.OnPressedColor = System.Drawing.Color.Black;
            this.BTN_Register.OnPressedDepth = 100;
            this.BTN_Register.Size = new System.Drawing.Size(195, 32);
            this.BTN_Register.TabIndex = 31;
            this.BTN_Register.Text = "REGISTER";
            this.BTN_Register.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.BTN_Register.Click += new System.EventHandler(this.BTN_Register_Click_1);
            // 
            // TXTCON_Pass
            // 
            this.TXTCON_Pass.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(231)))), ((int)(((byte)(233)))));
            this.TXTCON_Pass.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.TXTCON_Pass.DefaultText = "";
            this.TXTCON_Pass.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.TXTCON_Pass.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.TXTCON_Pass.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.TXTCON_Pass.DisabledState.Parent = this.TXTCON_Pass;
            this.TXTCON_Pass.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.TXTCON_Pass.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(231)))), ((int)(((byte)(233)))));
            this.TXTCON_Pass.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.TXTCON_Pass.FocusedState.Parent = this.TXTCON_Pass;
            this.TXTCON_Pass.Font = new System.Drawing.Font("Roboto Cn", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TXTCON_Pass.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.TXTCON_Pass.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.TXTCON_Pass.HoverState.Parent = this.TXTCON_Pass;
            this.TXTCON_Pass.Location = new System.Drawing.Point(56, 313);
            this.TXTCON_Pass.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TXTCON_Pass.Name = "TXTCON_Pass";
            this.TXTCON_Pass.PasswordChar = '\0';
            this.TXTCON_Pass.PlaceholderText = "";
            this.TXTCON_Pass.SelectedText = "";
            this.TXTCON_Pass.ShadowDecoration.Parent = this.TXTCON_Pass;
            this.TXTCON_Pass.Size = new System.Drawing.Size(340, 54);
            this.TXTCON_Pass.TabIndex = 28;
            this.TXTCON_Pass.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.TXTCON_Pass.TextChanged += new System.EventHandler(this.TXTCON_Pass_TextChanged);
            // 
            // TXTPass
            // 
            this.TXTPass.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(231)))), ((int)(((byte)(233)))));
            this.TXTPass.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.TXTPass.DefaultText = "";
            this.TXTPass.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.TXTPass.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.TXTPass.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.TXTPass.DisabledState.Parent = this.TXTPass;
            this.TXTPass.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.TXTPass.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(231)))), ((int)(((byte)(233)))));
            this.TXTPass.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.TXTPass.FocusedState.Parent = this.TXTPass;
            this.TXTPass.Font = new System.Drawing.Font("Roboto Cn", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TXTPass.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.TXTPass.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.TXTPass.HoverState.Parent = this.TXTPass;
            this.TXTPass.Location = new System.Drawing.Point(56, 218);
            this.TXTPass.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TXTPass.Name = "TXTPass";
            this.TXTPass.PasswordChar = '*';
            this.TXTPass.PlaceholderText = "";
            this.TXTPass.SelectedText = "";
            this.TXTPass.ShadowDecoration.Parent = this.TXTPass;
            this.TXTPass.Size = new System.Drawing.Size(340, 54);
            this.TXTPass.TabIndex = 27;
            this.TXTPass.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TXTUsers
            // 
            this.TXTUsers.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(231)))), ((int)(((byte)(233)))));
            this.TXTUsers.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.TXTUsers.DefaultText = "";
            this.TXTUsers.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.TXTUsers.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.TXTUsers.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.TXTUsers.DisabledState.Parent = this.TXTUsers;
            this.TXTUsers.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.TXTUsers.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(231)))), ((int)(((byte)(233)))));
            this.TXTUsers.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.TXTUsers.FocusedState.Parent = this.TXTUsers;
            this.TXTUsers.Font = new System.Drawing.Font("Roboto Cn", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TXTUsers.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.TXTUsers.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.TXTUsers.HoverState.Parent = this.TXTUsers;
            this.TXTUsers.Location = new System.Drawing.Point(56, 137);
            this.TXTUsers.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TXTUsers.Name = "TXTUsers";
            this.TXTUsers.PasswordChar = '\0';
            this.TXTUsers.PlaceholderText = "";
            this.TXTUsers.SelectedText = "";
            this.TXTUsers.ShadowDecoration.Parent = this.TXTUsers;
            this.TXTUsers.Size = new System.Drawing.Size(340, 54);
            this.TXTUsers.TabIndex = 26;
            this.TXTUsers.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(255, 612);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(133, 62);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 12;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(173, 134);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(293, 231);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.UseWaitCursor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.MidnightBlue;
            this.label8.Font = new System.Drawing.Font("Roboto Cn", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(224, 618);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(110, 24);
            this.label8.TabIndex = 14;
            this.label8.Text = "0548369261";
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.Controls.Add(this.label12);
            this.guna2Panel1.Location = new System.Drawing.Point(65, 16);
            this.guna2Panel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.ShadowDecoration.Parent = this.guna2Panel1;
            this.guna2Panel1.Size = new System.Drawing.Size(525, 651);
            this.guna2Panel1.TabIndex = 15;
            // 
            // frmRegistration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1048, 719);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.guna2Panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "frmRegistration";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.frmRegistration_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.guna2Panel1.ResumeLayout(false);
            this.guna2Panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lab_Log_in;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox Check_Pass;
        private System.Windows.Forms.ComboBox Cmb_userTY;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label label8;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2TextBox TXTUsers;
        private Guna.UI2.WinForms.Guna2TextBox TXTPass;
        private Guna.UI2.WinForms.Guna2TextBox TXTCON_Pass;
        private Guna.UI.WinForms.GunaButton BTN_Register;
    }
}

