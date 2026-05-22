using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using kingdom_Preparatory_School_Management_System.Services;


namespace kingdom_Preparatory_School_Management_System
{
   

    public partial class frmRegistration : Form
    {
        private Label statusLabel;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color SidebarColor = Color.FromArgb(17, 35, 58);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);

        public frmRegistration()
        {
            InitializeComponent();
            BuildModernRegistrationView();
        }

        private void BuildModernRegistrationView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Register";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(980, 640);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

            root.Controls.Add(BuildBrandPanel(), 0, 0);
            root.Controls.Add(BuildRegistrationPanel(), 1, 0);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildBrandPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarColor,
                Padding = new Padding(34)
            };

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Text = "Neat Academy",
                ForeColor = Color.FromArgb(191, 219, 254),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            });

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 150,
                Text = "Create User Account",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            });

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 96,
                Text = "Register staff access for the school management system.",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.TopLeft
            });

            return panel;
        }

        private Control BuildRegistrationPanel()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                Padding = new Padding(62, 46, 62, 46)
            };

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 10,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Padding = new Padding(34)
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Register",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Create a secure application account.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);

            TXTUsers.PlaceholderText = "";
            TXTPass.PlaceholderText = "";
            TXTCON_Pass.PlaceholderText = "";
            TXTPass.PasswordChar = '*';
            TXTCON_Pass.PasswordChar = '*';
            Cmb_userTY.Dock = DockStyle.Bottom;
            Cmb_userTY.Height = 32;
            Cmb_userTY.DropDownStyle = ComboBoxStyle.DropDownList;
            if (Cmb_userTY.Items.Count > 0 && Cmb_userTY.SelectedIndex < 0)
            {
                Cmb_userTY.SelectedIndex = 0;
            }

            card.Controls.Add(CreateField("Username", TXTUsers), 0, 2);
            card.Controls.Add(CreateField("Password", TXTPass), 0, 3);
            card.Controls.Add(CreateField("Confirm Password", TXTCON_Pass), 0, 4);
            card.Controls.Add(CreateField("User Type", Cmb_userTY), 0, 5);

            Check_Pass.Text = "Show passwords";
            Check_Pass.Dock = DockStyle.Fill;
            Check_Pass.ForeColor = MutedTextColor;
            Check_Pass.Font = new Font("Segoe UI", 9.5F);
            card.Controls.Add(Check_Pass, 0, 6);

            BTN_Register.Text = "Create account";
            BTN_Register.Dock = DockStyle.Fill;
            BTN_Register.BaseColor = PrimaryColor;
            BTN_Register.ForeColor = Color.White;
            card.Controls.Add(BTN_Register, 0, 7);

            lab_Log_in.Text = "Back to login";
            lab_Log_in.Dock = DockStyle.Fill;
            lab_Log_in.ForeColor = PrimaryColor;
            lab_Log_in.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lab_Log_in.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lab_Log_in, 0, 8);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft
            };
            card.Controls.Add(statusLabel, 0, 9);

            shell.Controls.Add(card);
            return shell;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 10),
                BackColor = SurfaceColor
            };
            panel.Controls.Add(input);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = labelText,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            });
            input.Dock = DockStyle.Bottom;
            input.Height = 34;
            return panel;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lab_Log_in_Click(object sender, EventArgs e)
        {
            new frmlogin().Show();
            this.Hide();
        }

        private void BTN_Register_Click(object sender, EventArgs e)
        {
            RegisterUser();
        }

        private void Check_Pass_CheckedChanged(object sender, EventArgs e)
        {
            if (Check_Pass.Checked)
            {
                TXTPass.PasswordChar = '\0';
                TXTCON_Pass.PasswordChar = '\0';
            }
            else
            {
                TXTPass.PasswordChar = '*';
                TXTCON_Pass.PasswordChar = '*';
            }
        }

        private void frmRegistration_Load(object sender, EventArgs e)
        {

        }

        private void Cmb_userTY_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void RegisterUser()
        {
            string username = TXTUsers.Text.Trim();
            string password = TXTPass.Text;
            string confirmPassword = TXTCON_Pass.Text;
            string userType = Cmb_userTY.Text.Trim();

            if (statusLabel != null) statusLabel.Text = "Creating account...";

            var (success, message) = await AuthService.RegisterAsync(username, password, confirmPassword, userType);

            if (success)
            {
                if (statusLabel != null) statusLabel.Text = "Registration successful.";
                UIHelper.ShowSuccess("Your registration was successful. You can now log in.", "Congratulations");
                ClearRegistrationForm();
            }
            else
            {
                if (statusLabel != null) statusLabel.Text = message;
                UIHelper.ShowWarning(message, "Registration Failed");
                if (message.Contains("username")) TXTUsers.Focus();
            }
        }

        private void ClearRegistrationForm()
        {
            TXTUsers.Text = "";
            TXTPass.Text = "";
            TXTCON_Pass.Text = "";
            if (Cmb_userTY.Items.Count > 0)
            {
                Cmb_userTY.SelectedIndex = 0;
            }
            TXTUsers.Focus();
        }

        private void BTN_Register_Click_1(object sender, EventArgs e)
        {
            RegisterUser();
        }

        private void TXTCON_Pass_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
