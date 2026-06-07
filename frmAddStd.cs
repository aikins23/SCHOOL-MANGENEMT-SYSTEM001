using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAddStd : Form
    {
        private readonly StudentService _studentService;
        private Label statusLabel;
        private Panel pageHost;
        private Panel _stepPanel;          // custom-drawn step-dot indicator
        private RadioButton _rbBusYes, _rbBusNo;
        private ComboBox _cmbRoute;
        private Button previousPageButton;
        private Button nextPageButton;
        private Button[] stepButtons;
        private Control[] formPages;
        private int currentPageIndex;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color AccentColor = UiTheme.GoldSoft;
        private static readonly Color GoldColor = UiTheme.Gold;
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        // Magic number constants
        private const int MIN_STUDENT_ID_LENGTH = 3;
        private const int MIN_STUDENT_AGE = 5;
        private const int MAX_STUDENT_AGE = 80;
        private const int DEFAULT_STUDENT_AGE = 15;

        // Drag state variables
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point formStartPoint;

        public frmAddStd()
        {
            InitializeComponent();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmAddStd", this)) return;

            // Initialize modern architecture
            var studentRepository = new StudentRepository(AppConfig.ConnectionString);
            var feeRepository = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepository, feeRepository);

            BuildModernAdmissionView();
            NavigationSidebar.AddTo(this);
            EnableFormDragging();
            Load += frmAddStd_Load;

            // Wire events commented-out in designer
            upload.Click            += upload_Click;
            gunaPictureBox1.Click   += gunaPictureBox1_Click;
            gunaButton1.Click       += gunaButton1_Click;
        }

        private void BuildModernAdmissionView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Student Admission";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 760);
            ClientSize = new Size(1320, 820);

            PrepareInputs();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(34, 26, 34, 24)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFormBody(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private void PrepareInputs()
        {
            txtStdID.ReadOnly = true;

            foreach (Control control in new Control[] { txtStdID, txtFN, txtLN, txtEM, txtHT, txtRD, txtAG, txtEC, txtGN, txtGE, txtGL, cmbGN, cmbCID, dateDOB, dateAD })
            {
                StyleInput(control);
            }

            txtStdID.FillColor = UiTheme.SurfaceAlt;

            cmbGN.Items.Clear();
            cmbGN.Items.AddRange(AppConfig.GenderOptions);
            cmbGN.SelectedIndex = 0;

            cmbCID.Items.Clear();
            cmbCID.Items.AddRange(AppConfig.ClassNames);
            cmbCID.SelectedIndex = 0;

            dateDOB.Value = DateTime.Today.AddYears(-5);
            dateAD.Value = DateTime.Today;
            std_pic.SizeMode = PictureBoxSizeMode.Zoom;
            std_pic.BackColor = AccentColor;
            std_pic.BorderStyle = BorderStyle.None;
            upload.Text = "Upload Photo";
            upload.FillColor = GoldColor;
            upload.ForeColor = TextColor;
            upload.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            upload.BorderColor = BorderColor;
            upload.BorderThickness = 1;
            upload.BorderRadius = 4;
        }

        private void StyleInput(Control control)
        {
            control.Font = new Font("Segoe UI", 10F);
            control.Margin = Padding.Empty;

            if (control is Guna.UI2.WinForms.Guna2TextBox textBox)
            {
                textBox.Multiline = false;
                textBox.FillColor = SurfaceColor;
                textBox.BorderColor = BorderColor;
                textBox.FocusedState.BorderColor = GoldColor;
                textBox.BorderRadius = 4;
                textBox.BorderThickness = 1;
                textBox.ForeColor = TextColor;
                textBox.PlaceholderForeColor = MutedTextColor;
                textBox.Height = 36;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2ComboBox comboBox)
            {
                comboBox.FillColor = SurfaceColor;
                comboBox.BorderColor = BorderColor;
                comboBox.FocusedState.BorderColor = GoldColor;
                comboBox.BorderRadius = 4;
                comboBox.ForeColor = TextColor;
                comboBox.ItemHeight = 30;
                comboBox.Height = 36;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2DateTimePicker datePicker)
            {
                datePicker.FillColor = SurfaceColor;
                datePicker.BorderColor = BorderColor;
                datePicker.BorderRadius = 4;
                datePicker.BorderThickness = 1;
                datePicker.ForeColor = TextColor;
                datePicker.Height = 36;
            }
        }

        private Control BuildHeader()
        {
            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Student Admission",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Register learners, assign class, guardian details, and opening fee records",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return titleBlock;
        }

        private Control BuildFormBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Margin = Padding.Empty
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            pageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                Margin = Padding.Empty,
                AutoScroll = true
            };

            formPages = new[]
            {
                BuildPersonalPanel(),
                BuildGuardianPanel(),
                BuildPhotoPanel()
            };
            stepButtons = BuildStepButtons(new[] { "Learner", "Guardian", "Photo" });
            currentPageIndex = 0;

            body.Controls.Add(BuildStepStrip(stepButtons), 0, 0);
            body.Controls.Add(pageHost, 0, 1);
            ShowStudentPage(currentPageIndex);

            return body;
        }

        private Button[] BuildStepButtons(string[] labels)
        {
            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int pageIndex = i;
                buttons[i] = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(i == 0 ? 0 : 8, 0, 0, 8),
                    Text = $"{i + 1}. {labels[i]}",
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                buttons[i].FlatAppearance.BorderSize = 1;
                buttons[i].Click += (sender, args) => ShowStudentPage(pageIndex);
            }
            return buttons;
        }

        private Control BuildStepStrip(Button[] buttons)
        {
            var strip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = buttons.Length,
                BackColor = PageBackColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (int i = 0; i < buttons.Length; i++)
            {
                strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / buttons.Length));
                strip.Controls.Add(buttons[i], i, 0);
            }
            return strip;
        }

        private Control BuildPersonalPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = CreateSectionLayout("Learner Details", 6, 2);

            layout.Controls.Add(CreateField("Student ID", txtStdID), 0, 1);
            layout.Controls.Add(CreateField("Class", cmbCID), 1, 1);
            layout.Controls.Add(CreateField("First Name", txtFN), 0, 2);
            layout.Controls.Add(CreateField("Last Name", txtLN), 1, 2);
            layout.Controls.Add(CreateField("Date of Birth", dateDOB), 0, 3);
            layout.Controls.Add(CreateField("Gender", cmbGN), 1, 3);
            layout.Controls.Add(CreateField("Email", txtEM), 0, 4);
            layout.Controls.Add(CreateField("Emergency Contact", txtEC), 1, 4);
            layout.Controls.Add(CreateField("Home Town", txtHT), 0, 5);
            layout.Controls.Add(CreateField("Residence", txtRD), 1, 5);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildGuardianPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = CreateSectionLayout("Guardian and Admission", 5, 2);

            layout.Controls.Add(CreateField("Guardian Name", txtGN), 0, 1);
            layout.Controls.Add(CreateField("Guardian Email", txtGE), 1, 1);
            layout.Controls.Add(CreateField("Guardian Location", txtGL), 0, 2);
            layout.Controls.Add(CreateField("Admission Date", dateAD), 1, 2);
            layout.Controls.Add(CreateField("Allergies / Medical Notes", txtAG), 0, 3);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 3), 2);

            var busPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = SurfaceColor };
            _rbBusNo = new RadioButton { Text = "No", Checked = true, AutoSize = true, Margin = new Padding(0, 8, 16, 0), ForeColor = TextColor };
            _rbBusYes = new RadioButton { Text = "Yes", AutoSize = true, Margin = new Padding(0, 8, 0, 0), ForeColor = TextColor };
            busPanel.Controls.Add(_rbBusNo);
            busPanel.Controls.Add(_rbBusYes);
            _cmbRoute = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            _rbBusYes.CheckedChanged += (s, e) => _cmbRoute.Enabled = _rbBusYes.Checked;
            layout.Controls.Add(CreateField("School Bus?", busPanel), 0, 4);
            layout.Controls.Add(CreateField("Bus Route", _cmbRoute), 1, 4);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildPhotoPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Photo",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            std_pic.Dock = DockStyle.Fill;
            std_pic.Margin = new Padding(0, 0, 0, 10);
            upload.Dock = DockStyle.Fill;
            upload.Margin = new Padding(0, 0, 0, 12);
            upload.Click -= upload_Click;
            upload.Click += upload_Click;

            layout.Controls.Add(std_pic, 0, 1);
            layout.Controls.Add(upload, 0, 2);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Fee setup",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 3);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "The selected class creates the opening tuition fee and payment balance.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = false
            }, 0, 4);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Use a clear portrait photo for easier identification in student records.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft,
                AutoEllipsis = false
            }, 0, 5);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = padding,
                Margin = margin
            };
        }

        private TableLayoutPanel CreateSectionLayout(string title, int rows, int columns)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = rows + 1,
                ColumnCount = columns,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            for (int i = 1; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < columns; i++)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            }

            var titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                UseMnemonic = false,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(titleLabel, 0, 0);
            layout.SetColumnSpan(titleLabel, columns);

            return layout;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(0, 2, 12, 4),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            input.Dock = DockStyle.Fill;
            input.Height = 38;
            panel.Controls.Add(label, 0, 0);
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private Control BuildStatusBar()
        {
            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Ready."
            };
            return statusLabel;
        }

        private Control BuildActions()
        {
            // Outer wrapper: [navigator | spacer | CRUD buttons]
            var wrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 0, 6)
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));   // old footer pager hidden; steps are above the card
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // spacer
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500)); // CRUD

            // ── Step navigator: [← Back] [step dots] [Next →] ────────────────
            var nav = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = PageBackColor,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));  // ← Back
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // step dots
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));  // Next →

            previousPageButton = MakeNavBtn("← Back", () => MoveStudentPage(-1), false);
            nextPageButton      = MakeNavBtn("Next →",  () => MoveStudentPage(1),  true);

            _stepPanel = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            _stepPanel.Paint += (s, e) => PaintStepDots(e.Graphics, _stepPanel.ClientRectangle, formPages.Length);

            nav.Controls.Add(previousPageButton, 0, 0);
            nav.Controls.Add(_stepPanel,          1, 0);
            nav.Controls.Add(nextPageButton,      2, 0);

            // ── CRUD buttons ──────────────────────────────────────────────────
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                BackColor = PageBackColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            actions.Controls.Add(CreateSecondaryButton("New",      async () => await NewStudent()),       0, 0);
            actions.Controls.Add(CreatePrimaryButton( "Save",      async () => await SaveStudent()),      1, 0);
            actions.Controls.Add(CreateSecondaryButton("Update",   async () => await UpdateStudent()),    2, 0);
            actions.Controls.Add(CreateDangerButton(  "Roll Out",  async () => await RollOutStudent()),   3, 0);

            wrapper.Controls.Add(nav,     0, 0);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor }, 1, 0);
            wrapper.Controls.Add(actions, 2, 0);
            ShowStudentPage(currentPageIndex);
            return wrapper;
        }

        /// <summary>Flat-styled navigation button that fills its TableLayoutPanel cell.</summary>
        private Button MakeNavBtn(string text, Action action, bool primary)
        {
            var btn = new Button
            {
                Dock      = DockStyle.Fill,
                Margin    = new Padding(primary ? 6 : 0, 0, primary ? 0 : 6, 0),
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                BackColor = primary ? PrimaryColor : SurfaceColor,
                ForeColor = primary ? Color.White : TextColor
            };
            btn.FlatAppearance.BorderColor       = primary ? PrimaryColor : BorderColor;
            btn.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            btn.Click += (s, e) => action();
            return btn;
        }

        /// <summary>
        /// Paints compact numbered step dots with connecting lines.
        /// Completed steps: Navy fill. Active step: Navy + Gold ring.
        /// Upcoming steps: light border fill.
        /// </summary>
        private void PaintStepDots(Graphics g, Rectangle bounds, int pageCount)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            const int dotD   = 12;  // dot diameter
            const int connW  = 22;  // connector width between dot edges
            int totalW = pageCount * dotD + (pageCount - 1) * connW;
            int startX = (bounds.Width  - totalW) / 2;
            int cy     = bounds.Height / 2;

            for (int i = 0; i < pageCount; i++)
            {
                int dx = startX + i * (dotD + connW);

                // Connector to the next dot
                if (i < pageCount - 1)
                {
                    bool done = i < currentPageIndex;
                    using (var pen = new Pen(done ? UiTheme.Navy : UiTheme.Border, 2))
                        g.DrawLine(pen, dx + dotD, cy, dx + dotD + connW, cy);
                }

                var rect = new Rectangle(dx, cy - dotD / 2, dotD, dotD);

                if (i < currentPageIndex)
                {
                    // ● Completed — solid Navy
                    using (var br = new SolidBrush(UiTheme.Navy))
                        g.FillEllipse(br, rect);
                    // White checkmark text
                    using (var f  = new Font("Segoe UI", 7F, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.White))
                    {
                        var sf = new System.Drawing.StringFormat
                        { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                        g.DrawString("✓", f, br, rect, sf);
                    }
                }
                else if (i == currentPageIndex)
                {
                    // ◉ Active — Navy fill + Gold ring
                    using (var br = new SolidBrush(UiTheme.Navy))
                        g.FillEllipse(br, rect);
                    using (var pen = new Pen(UiTheme.Gold, 2.5f))
                        g.DrawEllipse(pen, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                    // White step number
                    using (var f  = new Font("Segoe UI Semibold", 7F, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.White))
                    {
                        var sf = new System.Drawing.StringFormat
                        { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                        g.DrawString((i + 1).ToString(), f, br, rect, sf);
                    }
                }
                else
                {
                    // ○ Upcoming — light-gray fill + border
                    using (var br = new SolidBrush(Color.FromArgb(226, 230, 238)))
                        g.FillEllipse(br, rect);
                    using (var pen = new Pen(UiTheme.Border, 1.5f))
                        g.DrawEllipse(pen, rect);
                    // Muted step number
                    using (var f  = new Font("Segoe UI Semibold", 7F, FontStyle.Bold))
                    using (var br = new SolidBrush(MutedTextColor))
                    {
                        var sf = new System.Drawing.StringFormat
                        { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                        g.DrawString((i + 1).ToString(), f, br, rect, sf);
                    }
                }
            }
        }

        private void MoveStudentPage(int direction)
        {
            ShowStudentPage(currentPageIndex + direction);
        }

        private void ShowStudentPage(int pageIndex)
        {
            if (pageHost == null || formPages == null || formPages.Length == 0) return;

            currentPageIndex = Math.Max(0, Math.Min(pageIndex, formPages.Length - 1));
            pageHost.Controls.Clear();

            var page = formPages[currentPageIndex];
            page.Dock = DockStyle.Top;
            page.Height = GetStudentPageHeight(currentPageIndex);
            pageHost.Controls.Add(page);

            bool isFirst = currentPageIndex == 0;
            bool isLast  = currentPageIndex == formPages.Length - 1;

            if (previousPageButton != null)
            {
                previousPageButton.Enabled   = !isFirst;
                previousPageButton.ForeColor = !isFirst ? TextColor : MutedTextColor;
                previousPageButton.FlatAppearance.BorderColor = !isFirst ? BorderColor : Color.FromArgb(235, 237, 242);
            }
            if (nextPageButton != null)
            {
                nextPageButton.Enabled   = !isLast;
                nextPageButton.BackColor = !isLast ? PrimaryColor : Color.FromArgb(160, 174, 192);
                nextPageButton.Text      = isLast ? "Done ✓" : "Next →";
            }

            if (stepButtons != null)
            {
                for (int i = 0; i < stepButtons.Length; i++)
                {
                    bool active = i == currentPageIndex;
                    stepButtons[i].BackColor = active ? PrimaryColor : SurfaceColor;
                    stepButtons[i].ForeColor = active ? Color.White : TextColor;
                    stepButtons[i].FlatAppearance.BorderColor = active ? PrimaryColor : BorderColor;
                }
            }
        }

        private int GetStudentPageHeight(int pageIndex)
        {
            // Base (96-DPI) heights. The Guardian page (index 1) has a title row (52) + 4 field rows
            // (78 each) + surface padding (20+22) = 406px of content; 440 gives slack.
            // IMPORTANT: the inner rows are DPI-scaled by the form's auto-scale, but this height is
            // applied at runtime and is NOT, so on a high-DPI display a fixed value clips the last
            // row (the School Bus / Bus Route row). Scale by the device DPI to keep them in sync.
            int baseHeight = pageIndex == 1 ? 440 : (pageIndex == 2 ? 520 : 500);
            float scale = DeviceDpi / 96f;
            if (scale < 1f) scale = 1f;
            return (int)Math.Ceiling(baseHeight * scale);
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;
            return button;
        }

        private Button CreatePrimaryButton(string text, Func<Task> asyncAction)
        {
            var button = CreateButton(text, asyncAction);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = AccentColor;
            return button;
        }

        private Button CreateSecondaryButton(string text, Func<Task> asyncAction)
        {
            var button = CreateButton(text, asyncAction);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = AccentColor;
            return button;
        }

        private Button CreateDangerButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = DangerColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 241, 242);
            return button;
        }

        private Button CreateDangerButton(string text, Func<Task> asyncAction)
        {
            var button = CreateButton(text, asyncAction);
            button.BackColor = SurfaceColor;
            button.ForeColor = DangerColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 241, 242);
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button
            {
                Width = text == "Roll Out" ? 118 : 104,
                Height = 42,
                Margin = new Padding(8, 2, 0, 2),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                FlatAppearance = { BorderSize = 1 }
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private Button CreateButton(string text, Func<Task> asyncAction)
        {
            var button = new Button
            {
                Width = text == "Roll Out" ? 118 : 104,
                Height = 42,
                Margin = new Padding(8, 2, 0, 2),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                FlatAppearance = { BorderSize = 1 }
            };
            button.Click += async (sender, args) => await asyncAction();
            return button;
        }

        private async void frmAddStd_Load(object sender, EventArgs e)
        {
            try
            {
                LoadClassDropdown();
                LoadGenderDropdown();
                SetDateOfBirthRange();
                await LoadBusRoutesAsync();
                txtStdID.Focus();
                LoggerHelper.LogInfo("frmAddStd loaded successfully");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading form: " + ex.Message, "Student Registration");
                LoggerHelper.LogError("frmAddStd_Load failed", ex);
            }
        }

        private async System.Threading.Tasks.Task LoadBusRoutesAsync()
        {
            try
            {
                var repo = new Data.TransportRepository(AppConfig.ConnectionString);
                await repo.EnsureTablesAsync();
                var routes = await repo.GetRoutesAsync();
                _cmbRoute.Items.Clear();
                foreach (var r in routes) _cmbRoute.Items.Add(new RouteItem(r));
                if (_cmbRoute.Items.Count > 0) _cmbRoute.SelectedIndex = 0;
            }
            catch (Exception ex) { LoggerHelper.LogWarning("Could not load bus routes: " + ex.Message); }
        }

        private sealed class RouteItem
        {
            public Models.BusRoute Route { get; }
            public RouteItem(Models.BusRoute r) { Route = r; }
            public override string ToString() =>
                Route.RouteName + " — GHS " + Route.Fee.ToString("N2") + " / " + Route.PaymentTerm;
        }

        private void LoadClassDropdown()
        {
            cmbCID.Items.Clear();
            cmbCID.Items.Add("Select Class");
            foreach (var className in AppConfig.ClassNames)
            {
                cmbCID.Items.Add(className);
            }
            cmbCID.SelectedIndex = 0;
        }

        private void LoadGenderDropdown()
        {
            cmbGN.Items.Clear();
            cmbGN.Items.Add("Select Gender");
            cmbGN.Items.Add("Male");
            cmbGN.Items.Add("Female");
            cmbGN.Items.Add("Other");
            cmbGN.SelectedIndex = 0;
        }

        private void SetDateOfBirthRange()
        {
            dateDOB.MaxDate = DateTime.Now.AddYears(-MIN_STUDENT_AGE);
            dateDOB.MinDate = DateTime.Now.AddYears(-MAX_STUDENT_AGE);
            dateDOB.Value = DateTime.Now.AddYears(-DEFAULT_STUDENT_AGE);
        }

        private void ClearStudentDetails()
        {
            txtFN.Text = "";
            txtLN.Text = "";
            cmbCID.SelectedIndex = 0;
            dateDOB.Value = DateTime.Now.AddYears(-DEFAULT_STUDENT_AGE);
            txtEM.Text = "";
            txtEC.Text = "";
            txtHT.Text = "";
            txtRD.Text = "";
            cmbGN.SelectedIndex = 0;
            txtAG.Text = "";
            txtGN.Text = "";
            txtGE.Text = "";
            txtGL.Text = "";
            dateAD.Value = DateTime.Today;
            std_pic.Image = null;
            if (_rbBusNo != null) _rbBusNo.Checked = true;
            if (_cmbRoute != null) { _cmbRoute.SelectedIndex = _cmbRoute.Items.Count > 0 ? 0 : -1; _cmbRoute.Enabled = false; }
        }

        private async System.Threading.Tasks.Task SetNextStudentId()
        {
            try
            {
                txtStdID.Text = await _studentService.GenerateNextStudentIdAsync();
                statusLabel.Text = "Ready for a new admission.";
                LoggerHelper.LogInfo($"Next student ID generated: {txtStdID.Text}");
            }
            catch (ApplicationException appEx)
            {
                LoggerHelper.LogWarning($"Student ID generation issue: {appEx.Message}");
                statusLabel.Text = "Could not generate student ID.";
                UIHelper.ShowWarning("Could not generate student ID. Database may be unreachable.", "ID Generation");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("SetNextStudentId failed", ex);
                statusLabel.Text = "Could not prepare the next student ID.";
                UIHelper.ShowError("Unexpected error generating student ID", "Error");
            }
        }

        public void upload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var fileInfo = new System.IO.FileInfo(dialog.FileName);
                        if (fileInfo.Length > AppConfig.MaxPhotoSizeBytes)
                        {
                            var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                            LoggerHelper.LogWarning($"Photo upload rejected: {sizeInMB:F2}MB exceeds {AppConfig.MaxPhotoSizeMB}MB limit");
                            UIHelper.ShowWarning($"Photo must be smaller than {AppConfig.MaxPhotoSizeMB}MB");
                            return;
                        }

                        using (Image selectedImage = Image.FromFile(dialog.FileName))
                        {
                            Image previousImage = std_pic.Image;
                            std_pic.Image = new Bitmap(selectedImage);
                            previousImage?.Dispose();
                        }

                        statusLabel.Text = "Photo selected.";
                        LoggerHelper.LogInfo($"Photo uploaded successfully for student {txtStdID.Text}");
                    }
                    catch (Exception ex)
                    {
                        statusLabel.Text = "Photo was not accepted.";
                        LoggerHelper.LogError("Photo upload failed", ex);
                        UIHelper.ShowWarning(ex.Message, "Student Admission");
                    }
                }
            }
        }

        private Student MapFormToStudent()
        {
            return new Student
            {
                StudentID = txtStdID.Text,
                FirstName = txtFN.Text.Trim(),
                LastName = txtLN.Text.Trim(),
                DateOfBirth = dateDOB.Value.Date,
                Gender = cmbGN.Text.Trim(),
                Email = txtEM.Text.Trim(),
                ClassID = cmbCID.Text.Trim(),
                HomeTown = txtHT.Text.Trim(),
                Residence = txtRD.Text.Trim(),
                Allergies = txtAG.Text.Trim(),
                EmergencyContact = txtEC.Text.Trim(),
                GuardianName = txtGN.Text.Trim(),
                GuardianEmail = txtGE.Text.Trim(),
                GuardianLocation = txtGL.Text.Trim(),
                AdmissionDate = dateAD.Value.Date,
                ProfilePhoto = std_pic.Image != null ? ImageHelper.ImageToBytes(std_pic.Image) : null
            };
        }

        private bool ValidateStudentFields()
        {
            if (!FormValidationHelper.ValidateRequired(txtStdID, "Student ID"))
                return false;

            if (!FormValidationHelper.ValidateRequired(txtFN, "First Name"))
                return false;

            if (!FormValidationHelper.ValidateComboBox(cmbCID, "Class"))
                return false;

            if (dateDOB.Value == null)
            {
                UIHelper.ShowError("Date of Birth is required", "Validation");
                return false;
            }

            if (!FormValidationHelper.ValidateEmail(txtEM))
                return false;

            return true;
        }

        private async Task SaveStudent()
        {
            try
            {
                if (!ValidateStudentFields())
                    return;

                // Confirmation
                string action = statusLabel.Text.Contains("New") ? "add new" : "update";
                if (!ConfirmationHelper.ConfirmSave($"This will {action} student {txtFN.Text}?"))
                    return;

                var student = MapFormToStudent();
                bool isNew = await _studentService.GetStudentAsync(student.StudentID) == null;

                if (isNew)
                {
                    if (_rbBusYes.Checked && !(_cmbRoute.SelectedItem is RouteItem))
                    { UIHelper.ShowWarning("Select a bus route, or choose No.", "Student Registration"); return; }

                    // New admission: don't save yet. Collect the admission + school-fee
                    // payment, then submit a draft for bursar approval. The student,
                    // receipts, and SMS are created only when the bursar approves.
                    var draft = new Models.DraftAdmission
                    {
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        DateOfBirth = student.DateOfBirth,
                        Gender = student.Gender,
                        ClassID = student.ClassID,
                        Email = student.Email,
                        HomeTown = student.HomeTown,
                        Residence = student.Residence,
                        Allergies = student.Allergies,
                        GuardianName = student.GuardianName,
                        GuardianEmail = student.GuardianEmail,
                        GuardianLocation = student.GuardianLocation,
                        EmergencyContact = student.EmergencyContact,
                        AdmissionDate = student.AdmissionDate,
                        ProfilePhoto = student.ProfilePhoto,
                        TermTotal = _studentService.GetFeeForClass(student.ClassID),
                        AdmissionFee = Common.AdmissionFees.Amount,
                        SubmittedBy = AuthService.CurrentUser.Username,
                        BusRouteId = (_rbBusYes.Checked && _cmbRoute.SelectedItem is RouteItem ri) ? ri.Route.RouteId : (int?)null
                    };
                    using (var pay = new frmFessPayment(draft))
                    {
                        pay.ShowDialog();
                    }
                    txtStdID.Text = "";
                    ClearStudentDetails();
                    txtStdID.Focus();
                    return;
                }

                var (success, message) = await _studentService.UpdateStudentAsync(student);
                if (success)
                {
                    ConfirmationHelper.ShowInfo("Student updated successfully");
                    LoggerHelper.LogInfo($"Student {student.StudentID} updated");
                    txtStdID.Text = "";
                    ClearStudentDetails();
                    txtStdID.Focus();
                }
                else
                {
                    UIHelper.ShowError(string.IsNullOrWhiteSpace(message) ? "Could not save student" : message, "Error");
                    LoggerHelper.LogError($"Student update returned failure for {student?.StudentID}: {message}", null);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Save failed: " + ex.Message, "Student Registration");
                LoggerHelper.LogError("Student save failed", ex);
            }
        }

        private async Task UpdateStudent()
        {
            try
            {
                if (!ValidateStudentFields())
                    return;

                // Confirmation
                if (!ConfirmationHelper.ConfirmSave($"This will update student {txtFN.Text}?"))
                    return;

                statusLabel.Text = "Updating student...";
                var student = MapFormToStudent();
                var (success, message) = await _studentService.UpdateStudentAsync(student);

                if (success)
                {
                    ConfirmationHelper.ShowInfo(message);
                    LoggerHelper.LogInfo($"Student {student.StudentID} updated");
                    statusLabel.Text = message;
                }
                else
                {
                    statusLabel.Text = "Update failed.";
                    UIHelper.ShowWarning(message, "Student Admission");
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Update failed: " + ex.Message, "Student Registration");
                LoggerHelper.LogError("Student update failed", ex);
            }
        }

        private async Task RollOutStudent()
        {
            try
            {
                if (!FormValidationHelper.ValidateRequired(txtStdID, "Student ID"))
                    return;

                if (!ConfirmationHelper.ConfirmDelete("Student",
                    $"ID: {txtStdID.Text}\nName: {txtFN.Text}"))
                {
                    return;
                }

                statusLabel.Text = "Rolling out student...";
                var (success, message) = await _studentService.DeleteStudentAsync(txtStdID.Text.Trim());

                if (success)
                {
                    ConfirmationHelper.ShowInfo("Student deleted successfully");
                    LoggerHelper.LogInfo($"Student {txtStdID.Text} deleted");
                    await NewStudent();
                }
                else
                {
                    UIHelper.ShowError("Could not delete student", "Error");
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Delete failed: " + ex.Message, "Error");
                LoggerHelper.LogError("Student delete failed", ex);
            }
        }

        private async System.Threading.Tasks.Task NewStudent()
        {
            try
            {
                txtStdID.Text = "";
                ClearStudentDetails();
                await SetNextStudentId();
                txtStdID.Focus();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Clear button failed", ex);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                txtStdID.Text = "";
                ClearStudentDetails();
                statusLabel.Text = "Form cleared. Ready for new student.";
                txtStdID.Focus();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Clear button failed", ex);
            }
        }

        private async void txtStdID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtStdID.Text.Length < MIN_STUDENT_ID_LENGTH)
                {
                    ClearStudentDetails();
                    return;
                }

                string studentId = txtStdID.Text.Trim();
                var existingStudent = await _studentService.GetStudentAsync(studentId);

                if (existingStudent != null)
                {
                    // Student exists - load for editing
                    txtFN.Text = existingStudent.FirstName ?? "";
                    txtLN.Text = existingStudent.LastName ?? "";
                    cmbCID.Text = existingStudent.ClassID ?? "";
                    dateDOB.Value = existingStudent.DateOfBirth;
                    txtEM.Text = existingStudent.Email ?? "";
                    txtEC.Text = existingStudent.EmergencyContact ?? "";
                    txtHT.Text = existingStudent.HomeTown ?? "";
                    txtRD.Text = existingStudent.Residence ?? "";
                    cmbGN.Text = existingStudent.Gender ?? "";
                    txtAG.Text = existingStudent.Allergies ?? "";
                    txtGN.Text = existingStudent.GuardianName ?? "";
                    txtGE.Text = existingStudent.GuardianEmail ?? "";
                    txtGL.Text = existingStudent.GuardianLocation ?? "";
                    dateAD.Value = existingStudent.AdmissionDate;

                    if (existingStudent.ProfilePhoto != null && existingStudent.ProfilePhoto.Length > 0)
                    {
                        std_pic.Image = ImageHelper.BytesToImage(existingStudent.ProfilePhoto);
                    }

                    statusLabel.Text = "Student found. Ready to update.";
                }
                else
                {
                    // New student - clear details
                    ClearStudentDetails();
                    statusLabel.Text = "New student ID. Ready to add.";
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Student ID lookup failed", ex);
                statusLabel.Text = "Error looking up student.";
            }
        }

        private async void btnNew_Click(object sender, EventArgs e) { await NewStudent(); }
        private async void btnSave_Click_1(object sender, EventArgs e) { await SaveStudent(); }
        private async void btn_Update_Click(object sender, EventArgs e) { await UpdateStudent(); }
        private async void btnDel_Click(object sender, EventArgs e) { await RollOutStudent(); }
        private void gunaButton1_Click(object sender, EventArgs e) { Close(); new frmStdView().Show(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Application.Exit(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { new frmAddStd().Show(); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { new frmEmployee().Show(); }
        private void classToolStripMenuItem_Click(object sender, EventArgs e) { new EXAMS().Show(); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { new frmStdView().Show(); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { new frmEmpView().Show(); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { new frmFessPayment().Show(); }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) { new frmAbout().Show(); }

        /// <summary>
        /// Enables form dragging functionality by subscribing to mouse events on all controls
        /// </summary>
        private void EnableFormDragging()
        {
            // Subscribe to form's own mouse events
            SubscribeToDragEvents(this);

            // Subscribe to all child controls recursively
            foreach (Control control in GetAllControls(this))
            {
                if (!IsInteractiveControl(control))
                {
                    SubscribeToDragEvents(control);
                }
            }
        }

        /// <summary>
        /// Subscribe a control to drag mouse events
        /// </summary>
        private void SubscribeToDragEvents(Control control)
        {
            control.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStartPoint = e.Location;
                    formStartPoint = this.Location;
                }
            };

            control.MouseMove += (s, e) =>
            {
                if (isDragging && e.Button == MouseButtons.Left)
                {
                    int deltaX = e.X - dragStartPoint.X;
                    int deltaY = e.Y - dragStartPoint.Y;
                    this.Location = new Point(formStartPoint.X + deltaX, formStartPoint.Y + deltaY);
                }
            };

            control.MouseUp += (s, e) =>
            {
                isDragging = false;
            };
        }

        /// <summary>
        /// Recursively gets all controls on the form
        /// </summary>
        private System.Collections.Generic.List<Control> GetAllControls(Control container)
        {
            var controls = new System.Collections.Generic.List<Control>();
            foreach (Control control in container.Controls)
            {
                controls.Add(control);
                controls.AddRange(GetAllControls(control));
            }
            return controls;
        }

        /// <summary>
        /// Checks if a control should not have drag enabled
        /// </summary>
        private bool IsInteractiveControl(Control control)
        {
            Type controlType = control.GetType();
            // Check standard controls
            if (controlType == typeof(TextBox) ||
                   controlType == typeof(ComboBox) ||
                   controlType == typeof(Button) ||
                   controlType == typeof(CheckBox) ||
                   controlType == typeof(RadioButton) ||
                   controlType == typeof(DataGridView) ||
                   controlType == typeof(ListBox) ||
                   controlType == typeof(TreeView) ||
                   controlType == typeof(RichTextBox) ||
                   controlType.Name.Contains("NumericUpDown") ||
                   controlType.Name.Contains("DateTimePicker"))
            {
                return true;
            }

            // Check Guna2 UI components
            string typeName = controlType.Name;
            return typeName.Contains("Guna2TextBox") ||
                   typeName.Contains("Guna2ComboBox") ||
                   typeName.Contains("Guna2Button") ||
                   typeName.Contains("Guna2DateTimePicker") ||
                   typeName.Contains("Guna2CheckBox") ||
                   typeName.Contains("Guna2RadioButton") ||
                   typeName.Contains("Guna2DataGridView");
        }

    }
}

