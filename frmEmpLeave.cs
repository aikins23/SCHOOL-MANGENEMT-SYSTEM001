using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmEmpLeave : Form
    {
        private readonly LeaveService _leaveService;
        private readonly EmployeeService _employeeService;
        private Label statusLabel;

        private static readonly Color PageBackColor  = UiTheme.Page;
        private static readonly Color SurfaceColor   = UiTheme.Surface;
        private static readonly Color Navy            = UiTheme.Navy;
        private static readonly Color PrimaryColor    = UiTheme.Navy;
        private static readonly Color TextColor       = UiTheme.Text;
        private static readonly Color MutedTextColor  = UiTheme.Muted;
        private static readonly Color BorderColor     = UiTheme.Border;

        public frmEmpLeave()
        {
            InitializeComponent();
            if (!AuthService.RequireAccess("frmEmpLeave", this)) return;

            // Initialize modern architecture
            var leaveRepo = new LeaveRepository(AppConfig.ConnectionString);
            _leaveService = new LeaveService(leaveRepo);
            
            var employeeRepo = new EmployeeRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(employeeRepo);

            BuildModernLeaveView();
            NavigationSidebar.AddTo(this);
        }

        private void BuildModernLeaveView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Employee Leave";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1120, 680);

            PrepareInputs();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private void PrepareInputs()
        {
            // ── Editable ID field ─────────────────────────────────────────────
            // Designer sets MidnightBlue/White; override to match the app theme.
            txtEmployeeId.BackColor = SurfaceColor;
            txtEmployeeId.ForeColor = TextColor;
            txtEmployeeId.Font      = new Font("Segoe UI", 10.5F);
            txtEmployeeId.BorderStyle = BorderStyle.FixedSingle;

            // ── Read-only lookup fields ───────────────────────────────────────
            Color readOnlyBg = UiTheme.SurfaceAlt;
            foreach (var tb in new[] { txtName, txtdepartment, txtposition })
            {
                tb.ReadOnly   = true;
                tb.BackColor  = readOnlyBg;
                tb.ForeColor  = TextColor;
                tb.Font       = new Font("Segoe UI", 10.5F);
                tb.BorderStyle = BorderStyle.FixedSingle;
            }

            // ── Leave-option defaults ─────────────────────────────────────────
            rdpay.Checked    = true;
            rdoSick.Checked  = true;
            dtpdatestart.Value = DateTime.Today;
            dtpenddate.Value   = DateTime.Today;
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Employee Leave",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Look up an employee, choose leave details, and submit for approval",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Fixed-column layout so buttons never wrap to a second row
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = PageBackColor,
                Padding = new Padding(0, 18, 0, 0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // spacer
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108)); // Leave View
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // Submit Leave (primary)

            actions.Controls.Add(new Panel { BackColor = PageBackColor }, 0, 0);
            actions.Controls.Add(MakeHeaderBtn("Leave View",    () => FormManager.ShowForm<EmpleaveView>(this), false), 1, 0);
            actions.Controls.Add(MakeHeaderBtn("Submit Leave",  SubmitLeave, true), 2, 0);

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

            body.Controls.Add(BuildEmployeePanel(), 0, 0);
            body.Controls.Add(BuildLeavePanel(), 1, 0);
            return body;
        }

        private Control BuildEmployeePanel()
        {
            var panel = CreateSurfacePanel(new Padding(18), new Padding(0, 0, 14, 0));
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            for (int i = 1; i < 5; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(MakeCardHeader("Employee Lookup"), 0, 0);

            layout.Controls.Add(CreateField("Employee ID", txtEmployeeId), 0, 1);
            layout.Controls.Add(CreateField("Name", txtName), 0, 2);
            layout.Controls.Add(CreateField("Department", txtdepartment), 0, 3);
            layout.Controls.Add(CreateField("Position", txtposition), 0, 4);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Type the employee ID to fill the staff details automatically.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 5);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildLeavePanel()
        {
            var panel = CreateSurfacePanel(new Padding(18), Padding.Empty);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                BackColor = SurfaceColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var leaveTitle = MakeCardHeader("Leave Details");
            layout.Controls.Add(leaveTitle, 0, 0);
            layout.SetColumnSpan(leaveTitle, 2);

            layout.Controls.Add(CreateOptionGroup("Leave Type", new Control[]
            {
                rdoSick,
                rdoVacation,
                rdoFuneral,
                rdoPaternity,
                rdoMaternity,
                rdoAcidentOnDuty
            }), 0, 1);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 1), 2);

            layout.Controls.Add(CreateOptionGroup("Pay Option", new Control[] { rdpay, rdwPay }), 0, 2);
            layout.Controls.Add(CreateDateGroup(), 1, 2);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Submitted applications are stored as PENDING for approval.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 4);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 4), 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control CreateDateGroup()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                BackColor = SurfaceColor,
                Padding = new Padding(10, 0, 0, 0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Leave Dates",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(label, 0, 0);
            panel.SetColumnSpan(label, 2);
            panel.Controls.Add(CreateField("Start", dtpdatestart), 0, 1);
            panel.Controls.Add(CreateField("End", dtpenddate), 1, 1);
            return panel;
        }

        private Control CreateOptionGroup(string title, Control[] options)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 0, 10, 8)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                WrapContents = true
            };

            foreach (Control option in options)
            {
                option.Font = new Font("Segoe UI", 9.5F);
                option.ForeColor = TextColor;
                option.Width = 155;
                option.Height = 28;
                flow.Controls.Add(option);
            }

            panel.Controls.Add(flow, 0, 1);
            return panel;
        }

        /// <summary>
        /// Card-section heading: a 3 px Navy left accent bar + bold title, matching
        /// the metric cards used across the app.
        /// </summary>
        private Control MakeCardHeader(string title)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Navy, Padding = new Padding(3, 0, 0, 0) };
            var inner   = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };
            inner.Controls.Add(new Label
            {
                Dock      = DockStyle.Fill,
                Text      = title,
                ForeColor = TextColor,
                Font      = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            });
            wrapper.Controls.Add(inner);
            return wrapper;
        }

        /// <summary>
        /// Header-row button that fills its TableLayoutPanel cell (Dock.Fill, no fixed Width).
        /// </summary>
        private Button MakeHeaderBtn(string text, Action action, bool primary)
        {
            var btn = new Button
            {
                Dock      = DockStyle.Fill,
                Margin    = new Padding(8, 0, 0, 0),
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                BackColor = primary ? PrimaryColor : SurfaceColor,
                ForeColor = primary ? Color.White : TextColor
            };
            btn.FlatAppearance.BorderColor       = primary ? PrimaryColor : BorderColor;
            btn.FlatAppearance.MouseOverBackColor = primary ? UiTheme.NavyHover : UiTheme.GoldSoft;
            btn.Click += (s, e) => action();
            return btn;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = padding,
                Margin = margin
            };
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 10, 8),
                BackColor = SurfaceColor
            };
            panel.Controls.Add(input);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = labelText,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            input.Dock = DockStyle.Bottom;
            input.Height = 30;
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
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = PageBackColor
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148)); // Submit Leave (primary)
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112)); // Clear
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // fill

            actions.Controls.Add(CreatePrimaryButton("Submit Leave", SubmitLeave), 0, 0);
            actions.Controls.Add(CreateSecondaryButton("Clear", ClearForm), 1, 0);
            return actions;
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(23, 82, 172);
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 242, 255);
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private async void SubmitLeave()
        {
            try
            {
                // 1. Validation guards
                if (!FormValidationHelper.ValidateRequired(txtEmployeeId, "Employee ID")) return;
                if (!FormValidationHelper.ValidateRequired(txtName, "Employee Name")) return;

                if (dtpenddate.Value.Date < dtpdatestart.Value.Date)
                {
                    ConfirmationHelper.ShowWarning("End date cannot be before start date.", "Leave Application");
                    return;
                }

                // Balance warning: requested days vs remaining balance for current term
                int requestedDays = (dtpenddate.Value.Date - dtpdatestart.Value.Date).Days + 1;
                var preBalance = await _leaveService.GetLeaveBalanceAsync(txtEmployeeId.Text.Trim());
                if (requestedDays > preBalance.Remaining)
                {
                    var proceed = MessageBox.Show(
                        $"This request is for {requestedDays} day(s), but only {preBalance.Remaining} day(s) " +
                        $"remain in {preBalance.TermName}.\n\nSubmit anyway?",
                        "Insufficient Leave Balance",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (proceed != DialogResult.Yes) return;
                }

                // 2. Confirmation
                if (!ConfirmationHelper.ConfirmSave(
                    $"Submit leave application for {txtName.Text.Trim()} ({GetSelectedReason()}, " +
                    $"{dtpdatestart.Value.ToShortDateString()} to {dtpenddate.Value.ToShortDateString()})?")) return;

                // 3. Business logic
                string employeeId = txtEmployeeId.Text.Trim();

                var request = new LeaveRequest
                {
                    EmployeeID = employeeId,
                    EmployeeName = txtName.Text.Trim(),
                    Department = txtdepartment.Text.Trim(),
                    Position = txtposition.Text.Trim(),
                    LeaveOption = rdpay.Checked ? "With Pay" : "Without Pay",
                    Reason = GetSelectedReason(),
                    StartDate = dtpdatestart.Value.Date,
                    EndDate = dtpenddate.Value.Date
                };

                statusLabel.Text = "Submitting application...";
                var (success, message) = await _leaveService.ApplyForLeaveAsync(request);

                if (success)
                {
                    statusLabel.Text = message;
                    LoggerHelper.LogInfo($"Leave application submitted for employee {employeeId} ({request.Reason})");
                    UIHelper.ShowSuccess(message, "Leave Application");
                    ClearForm();
                }
                else
                {
                    statusLabel.Text = "Submission failed.";
                    LoggerHelper.LogWarning("Leave submission failed: " + message);
                    UIHelper.ShowError(message, "Leave Application");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("SubmitLeave failed", ex);
                UIHelper.ShowError("Submit leave failed: " + ex.Message, "Employee Leave");
            }
        }

        private string GetSelectedReason()
        {
            if (rdoSick.Checked) return "Sick";
            if (rdoVacation.Checked) return "Vacation";
            if (rdoFuneral.Checked) return "Funeral";
            if (rdoPaternity.Checked) return "Paternity";
            if (rdoMaternity.Checked) return "Maternity";
            if (rdoAcidentOnDuty.Checked) return "Accident On Duty";
            return "Other";
        }

        private async void txtEmployeeId_TextChanged(object sender, EventArgs e)
        {
            string employeeId = txtEmployeeId.Text.Trim();
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                txtName.Text = "";
                txtdepartment.Text = "";
                txtposition.Text = "";
                return;
            }

            try
            {
                var employee = await _employeeService.GetEmployeeAsync(employeeId);
                if (employee != null)
                {
                    txtName.Text = employee.FullName;
                    txtdepartment.Text = employee.Department;
                    txtposition.Text = employee.Position;

                    var balance = await _leaveService.GetLeaveBalanceAsync(employeeId, employee.FullName);
                    statusLabel.Text =
                        $"Employee loaded. {balance.TermName}: {balance.Remaining} of {balance.Entitlement} days remaining " +
                        $"(used {balance.DaysUsed}).";
                }
                else
                {
                    txtName.Text = "";
                    txtdepartment.Text = "";
                    txtposition.Text = "";
                    statusLabel.Text = "Employee not found.";
                }
            }
            catch { statusLabel.Text = "Lookup error."; }
        }

        private void ClearForm()
        {
            txtEmployeeId.Text = "";
            txtName.Text = "";
            txtdepartment.Text = "";
            txtposition.Text = "";
            rdpay.Checked = true;
            rdoSick.Checked = true;
            dtpdatestart.Value = DateTime.Today;
            dtpenddate.Value = DateTime.Today;
            statusLabel.Text = "Ready.";
        }

        private void btnClear_Click(object sender, EventArgs e) { ClearForm(); }
    }
}
