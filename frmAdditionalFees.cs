using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmAdditionalFees : Form
    {
        private readonly AdditionalFeeRepository _repository;
        private readonly AdditionalFeeService _service;

        private Guna2TextBox _feeNameBox;
        private Guna2TextBox _descriptionBox;
        private Guna2TextBox _academicYearBox;
        private Guna2ComboBox _termBox;
        private Guna2ComboBox _modeBox;
        private DateTimePicker _dueDatePicker;
        private NumericUpDown _defaultAmountBox;
        private Label _defaultAmountLabel;
        private CheckBox _compulsoryBox;
        private CheckBox _partPaymentBox;
        private CheckBox _notifySmsBox;
        private DataGridView _scopeGrid;
        private Panel _scopeContainer;
        private DataGridView _previewGrid;
        private DataGridView _historyGrid;
        private Label _summaryLabel;
        private Button _saveDraftBtn;
        private Button _previewBtn;
        private Button _submitBtn;
        private Button _approveBtn;
        private Button _rejectBtn;
        private Button _refreshBtn;

        private int _currentFeeId;
        private bool _hasUnsavedEntry;

        public frmAdditionalFees()
        {
            _repository = new AdditionalFeeRepository(AppConfig.ConnectionString);
            _service = new AdditionalFeeService(_repository);
            BuildUi();
            SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmAdditionalFees", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "Additional Fees & One-Off Charges";
            Size = new Size(1380, 900);
            MinimumSize = new Size(1180, 760);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 246, 249);
            Font = new Font("Segoe UI", 9.5F);
            if (Branding.AppIcon != null) Icon = Branding.AppIcon;

            // Main scrollable container for the entire page
            var pageContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(244, 246, 249)
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(26, 18, 38, 18),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // Hero Header Card
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Editor Card
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));  // Affected Students Preview Card
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));  // History & Approvals Card

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildEditorCard(), 0, 1);
            root.Controls.Add(BuildGridCard("Affected Students Preview", "Review student headcount and expected revenue before submission", true, out _previewGrid), 0, 2);
            root.Controls.Add(BuildGridCard("Additional Fee Requests & Approval History", "Manage submitted one-off fee requests and track approval workflow", false, out _historyGrid), 0, 3);

            pageContainer.Controls.Add(root);
            Controls.Add(pageContainer);

            // Keep the exact sidebar design untouched
            NavigationSidebar.AddTo(this);
            ApplyPermissions();
            UiTheme.Apply(this);

            // Apply custom aesthetics & attach visible modern scrollbars AFTER UiTheme.Apply
            StyleLocalControls(pageContainer);
        }

        private Control BuildHeader()
        {
            var outerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 246, 249),
                Padding = new Padding(0, 0, 0, 12)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 12, 20, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var lblTag = new Label
            {
                Text = "FINANCE MODULE - BILLING CHARGES",
                Dock = DockStyle.Top,
                Height = 17,
                ForeColor = Color.FromArgb(210, 151, 35),
                Font = new Font("Segoe UI Semibold", 8.2F, FontStyle.Bold)
            };
            var lblTitle = new Label
            {
                Text = "Additional Fees & One-Off Charges",
                Dock = DockStyle.Top,
                Height = 32,
                ForeColor = Color.FromArgb(0, 24, 74),
                Font = new Font("Segoe UI Semibold", 16.5F, FontStyle.Bold)
            };
            var lblSub = new Label
            {
                Text = "Create custom fee structures, preview affected students, submit for approval, and post charges directly to ledgers.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.2F)
            };

            leftPanel.Controls.Add(lblSub);
            leftPanel.Controls.Add(lblTitle);
            leftPanel.Controls.Add(lblTag);

            layout.Controls.Add(leftPanel, 0, 0);
            card.Controls.Add(layout);
            outerPanel.Controls.Add(card);
            return outerPanel;
        }

        private Control BuildEditorCard()
        {
            var cardMargin = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.FromArgb(244, 246, 249),
                Padding = new Padding(0, 0, 0, 14)
            };

            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Card Header Bar
            var cardHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 253),
                Padding = new Padding(18, 0, 18, 0)
            };
            var accentBar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Color.FromArgb(210, 151, 35) };
            var lblHeader = new Label
            {
                Text = "1. Configure Fee Structure & Assignment Scope",
                Dock = DockStyle.Left,
                Width = 420,
                Padding = new Padding(12, 0, 0, 0),
                Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 24, 74),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblHeaderSub = new Label
            {
                Text = "All monetary amounts are billed in Ghana Cedis (GHS)",
                Dock = DockStyle.Right,
                Width = 350,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleRight
            };
            cardHeader.Controls.Add(lblHeaderSub);
            cardHeader.Controls.Add(lblHeader);
            cardHeader.Controls.Add(accentBar);

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 10)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Row 0: Primary Fee Details
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            // Row 1: Amount, Mode, Description
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            // Row 2: Options Pill Bar
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            // Row 3: Scope container (auto size)
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Row 4: Modern Action Toolbar
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // Modern Guna2 Inputs
            _feeNameBox = CreateGunaTextBox("e.g. PTA Building Fund, Sports Levy...");
            _termBox = CreateGunaCombo("First Term", "Second Term", "Third Term");
            _academicYearBox = CreateGunaTextBox(DateTime.Today.Year.ToString(CultureInfo.InvariantCulture));
            _dueDatePicker = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Height = 34, Font = new Font("Segoe UI", 10F) };
            _defaultAmountBox = new NumericUpDown { Dock = DockStyle.Top, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true, Height = 34, Font = new Font("Segoe UI", 10F) };

            _descriptionBox = CreateGunaTextBox("Optional note or breakdown for fee receipt...");
            _modeBox = CreateGunaCombo(AdditionalFeeAssignmentModes.Flat, AdditionalFeeAssignmentModes.Class, AdditionalFeeAssignmentModes.Department);
            _modeBox.SelectedIndexChanged += (s, e) => PopulateScopeGrid();

            _compulsoryBox = new CheckBox { Text = "Compulsory Fee (Billed to all selected)", Checked = true, AutoSize = true, Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            _partPaymentBox = new CheckBox { Text = "Allow Part-Payment / Installments", Checked = true, AutoSize = true, Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            _notifySmsBox = new CheckBox { Text = "Send SMS Notification Upon Approval", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            WireEntryDirtyTracking();

            // Row 0: Primary Fee Details
            AddField(grid, "FEE NAME", _feeNameBox, 0, 0, 1);
            AddField(grid, "TERM", _termBox, 1, 0, 1);
            AddField(grid, "ACADEMIC YEAR", _academicYearBox, 2, 0, 1);
            AddField(grid, "DUE DATE", _dueDatePicker, 3, 0, 1);

            // Row 1: Amount, Mode, Description (span 2)
            AddField(grid, "FLAT AMOUNT (GHS)", _defaultAmountBox, 0, 1, 1, out _defaultAmountLabel);
            AddField(grid, "ASSIGNMENT MODE", _modeBox, 1, 1, 1);
            AddField(grid, "DESCRIPTION", _descriptionBox, 2, 1, 2);

            // Row 2: Options Box (structured into 3 equal columns so no text is cut off)
            var optionsContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 16, 6), BackColor = Color.White };
            var optionsPill = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 253),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16, 6, 16, 6)
            };
            var optionsTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
            optionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            optionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            optionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            optionsTable.Controls.Add(_compulsoryBox, 0, 0);
            optionsTable.Controls.Add(_partPaymentBox, 1, 0);
            optionsTable.Controls.Add(_notifySmsBox, 2, 0);

            optionsPill.Controls.Add(optionsTable);
            optionsContainer.Controls.Add(optionsPill);

            grid.Controls.Add(optionsContainer, 0, 2);
            grid.SetColumnSpan(optionsContainer, 4);

            // Row 3: Scope container (cleanly hidden when mode is Flat so no horizontal line appears)
            _scopeContainer = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = Color.White, Padding = new Padding(0, 4, 16, 6) };
            _scopeGrid = new DataGridView { Dock = DockStyle.Top, Height = 170, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false };
            _scopeGrid.CellValueChanged += (s, e) => MarkEntryDirty();
            _scopeGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_scopeGrid.IsCurrentCellDirty)
                {
                    _scopeGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _scopeContainer.Controls.Add(_scopeGrid);
            grid.Controls.Add(_scopeContainer, 0, 3);
            grid.SetColumnSpan(_scopeContainer, 4);

            // Row 4: Modern Action Toolbar
            var actionsBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(0, 6, 16, 0)
            };

            _refreshBtn = CreateModernButton("Refresh", Color.FromArgb(210, 151, 35), Color.FromArgb(0, 24, 74), async (s, e) => await LoadAsync(), 110);
            _rejectBtn = CreateModernButton("Reject", Color.FromArgb(225, 29, 72), Color.White, async (s, e) => await RejectSelectedAsync(), 110);
            _approveBtn = CreateModernButton("Approve", Color.FromArgb(16, 185, 129), Color.White, async (s, e) => await ApproveSelectedAsync(), 110);
            _submitBtn = CreateModernButton("Submit for Approval", Color.FromArgb(0, 24, 74), Color.White, async (s, e) => await SubmitSelectedAsync(), 165);
            _previewBtn = CreateModernButton("Preview Affected", Color.FromArgb(238, 244, 255), Color.FromArgb(0, 24, 74), async (s, e) => await PreviewCurrentAsync(), 150);
            _saveDraftBtn = CreateModernButton("Save Draft", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), async (s, e) => await SaveDraftAsync(), 115);

            actionsBar.Controls.AddRange(new Control[] { _refreshBtn, _rejectBtn, _approveBtn, _submitBtn, _previewBtn, _saveDraftBtn });
            grid.Controls.Add(actionsBar, 0, 4);
            grid.SetColumnSpan(actionsBar, 4);

            card.Controls.Add(grid);
            card.Controls.Add(cardHeader);
            cardMargin.Controls.Add(card);

            PopulateScopeGrid();
            _hasUnsavedEntry = false;
            return cardMargin;
        }

        private Panel BuildGridCard(string title, string subtitle, bool isPreviewCard, out DataGridView grid)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 246, 249), Padding = new Padding(0, 0, 0, 14) };
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 253),
                Padding = new Padding(16, 0, 18, 0)
            };
            var accent = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = isPreviewCard ? Color.FromArgb(210, 151, 35) : Color.FromArgb(0, 24, 74) };

            var headerTextPanel = new Panel { Dock = DockStyle.Left, Width = 620, BackColor = Color.Transparent, Padding = new Padding(14, 4, 0, 4) };
            headerTextPanel.Controls.Add(new Label
            {
                Text = subtitle,
                Dock = DockStyle.Bottom,
                Height = 16,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.8F)
            });
            headerTextPanel.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 19,
                ForeColor = Color.FromArgb(0, 24, 74),
                Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold)
            });
            header.Controls.Add(headerTextPanel);
            header.Controls.Add(accent);

            if (isPreviewCard)
            {
                var summaryContainer = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(0, 5, 0, 5) };
                var summaryPill = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(254, 249, 195),
                    BorderStyle = BorderStyle.FixedSingle
                };
                _summaryLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "0 student(s) - Expected Total: GHS 0.00",
                    ForeColor = Color.FromArgb(113, 63, 18),
                    Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                summaryPill.Controls.Add(_summaryLabel);
                summaryContainer.Controls.Add(summaryPill);
                header.Controls.Add(summaryContainer);
            }

            var gridBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = !isPreviewCard,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            if (!isPreviewCard)
                grid.SelectionChanged += (s, e) => SelectHistoryFee();

            gridBody.Controls.Add(grid);
            card.Controls.Add(gridBody);
            card.Controls.Add(header);
            wrapper.Controls.Add(card);
            return wrapper;
        }

        private static Guna2TextBox CreateGunaTextBox(string placeholder = "")
        {
            return new Guna2TextBox
            {
                Dock = DockStyle.Top,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                PlaceholderText = placeholder,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
        }

        private static Guna2ComboBox CreateGunaCombo(params string[] values)
        {
            var combo = new Guna2ComboBox
            {
                Dock = DockStyle.Top,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            combo.Items.AddRange(values);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            return combo;
        }

        private static Button CreateModernButton(string text, Color backColor, Color foreColor, EventHandler click, int width)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 36,
                Margin = new Padding(8, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += click;
            return button;
        }

        private static Panel AddField(TableLayoutPanel grid, string label, Control input, int column, int row, int span)
        {
            Label labelControl;
            return AddField(grid, label, input, column, row, span, out labelControl);
        }

        private static Panel AddField(TableLayoutPanel grid, string label, Control input, int column, int row, int span, out Label labelControl)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 8), BackColor = Color.White };
            panel.Controls.Add(input);
            labelControl = new Label
            {
                Text = label,
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 8.4F, FontStyle.Bold)
            };
            panel.Controls.Add(labelControl);
            input.Dock = DockStyle.Top;
            grid.Controls.Add(panel, column, row);
            if (span > 1) grid.SetColumnSpan(panel, span);
            return panel;
        }

        private void StyleLocalControls(Panel pageContainer)
        {
            StyleGridModern(_scopeGrid);
            StyleGridModern(_previewGrid);
            StyleGridModern(_historyGrid);

            StyleButtonModern(_submitBtn, Color.FromArgb(0, 24, 74), Color.White);
            StyleButtonModern(_previewBtn, Color.FromArgb(238, 244, 255), Color.FromArgb(0, 24, 74));
            StyleButtonModern(_saveDraftBtn, Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85));
            StyleButtonModern(_refreshBtn, Color.FromArgb(210, 151, 35), Color.FromArgb(0, 24, 74));
            StyleButtonModern(_approveBtn, Color.FromArgb(16, 185, 129), Color.White);
            StyleButtonModern(_rejectBtn, Color.FromArgb(225, 29, 72), Color.White);

            // Attach visible modern scrollbars to tables and page
            AttachVisibleGridScrollbar(_scopeGrid);
            AttachVisibleGridScrollbar(_previewGrid);
            AttachVisibleGridScrollbar(_historyGrid);
            AttachVisiblePageScrollbar(pageContainer);
        }

        private static void StyleButtonModern(Button btn, Color backColor, Color foreColor)
        {
            if (btn == null) return;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
        }

        private static void StyleGridModern(DataGridView grid)
        {
            if (grid == null) return;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            grid.AllowUserToResizeRows = true;
            grid.ScrollBars = ScrollBars.Both;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 34;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 244, 255);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(11, 31, 73);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            grid.GridColor = Color.FromArgb(226, 232, 240);
        }

        private static void AttachVisibleGridScrollbar(DataGridView grid)
        {
            if (grid == null || grid.Parent == null) return;
            try
            {
                // Remove invisible 1px scrollbar added by UiTheme
                for (int i = grid.Parent.Controls.Count - 1; i >= 0; i--)
                {
                    var c = grid.Parent.Controls[i];
                    if (c is Guna2VScrollBar old && old.Width <= 2)
                    {
                        grid.Parent.Controls.RemoveAt(i);
                        old.Dispose();
                    }
                }

                var scrollbar = new Guna2VScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = 14,
                    BorderRadius = 6,
                    FillColor = Color.FromArgb(241, 245, 249),
                    ThumbColor = Color.FromArgb(148, 163, 184)
                };
                grid.Parent.Controls.Add(scrollbar);
                scrollbar.BringToFront();

                var helper = new Guna.UI2.WinForms.Helpers.DataGridViewScrollHelper(grid, scrollbar, true);
                helper.UpdateScrollBar();
            }
            catch { }
        }

        private static void AttachVisiblePageScrollbar(Panel pageContainer)
        {
            if (pageContainer == null) return;
            try
            {
                var scrollbar = new Guna2VScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = 14,
                    BorderRadius = 6,
                    FillColor = Color.FromArgb(238, 242, 248),
                    ThumbColor = Color.FromArgb(148, 163, 184)
                };
                pageContainer.Controls.Add(scrollbar);
                scrollbar.BringToFront();

                var helper = new Guna.UI2.WinForms.Helpers.PanelScrollHelper(pageContainer, scrollbar, true);
                helper.UpdateScrollBar();
            }
            catch { }
        }

        private void ApplyPermissions()
        {
            bool canCreate = AuthService.CanWrite("Finance.AdditionalFees.Create");
            bool canApprove = AuthService.CanWrite("Finance.AdditionalFees.Approve");
            _saveDraftBtn.Visible = canCreate;
            _submitBtn.Visible = canCreate;
            _approveBtn.Visible = canApprove;
            _rejectBtn.Visible = canApprove;
        }

        private void SetBusyState(bool busy, string approveText = null)
        {
            UseWaitCursor = busy;
            _saveDraftBtn.Enabled = !busy;
            _previewBtn.Enabled = !busy;
            _submitBtn.Enabled = !busy;
            _approveBtn.Enabled = !busy;
            _rejectBtn.Enabled = !busy;
            _refreshBtn.Enabled = !busy;
            _approveBtn.Text = busy && !string.IsNullOrWhiteSpace(approveText) ? approveText : "Approve";
        }

        private async Task LoadAsync()
        {
            try
            {
                await _repository.EnsureSchemaAsync();
                await RefreshHistoryAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load additional fees.\n\n" + ex.Message, "Additional Fees");
            }
        }

        private async Task SaveDraftAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.AdditionalFees.Create", "Create additional fee")) return;
            try
            {
                SetBusyState(true);
                int feeId = await CreateDraftFromCurrentEntryAsync(showSuccess: true);
                if (feeId <= 0) return;
                await RefreshHistoryAsync();
                await PreviewCurrentAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save the additional fee draft.\n\n" + ex.Message, "Additional Fees");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task PreviewCurrentAsync()
        {
            try
            {
                SetBusyState(true);
                int feeId = GetSelectedOrCurrentFeeId();
                if (feeId <= 0)
                {
                    UIHelper.ShowInfo("Save a draft or select an existing fee before previewing.", "Additional Fees");
                    return;
                }

                var preview = await _service.PreviewAsync(feeId);
                _previewGrid.DataSource = ToPreviewTable(preview);
                _summaryLabel.Text = $"{preview.StudentCount:N0} student(s) - Expected Total: GHS {preview.ExpectedTotal:N2}";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not preview affected students.\n\n" + ex.Message, "Additional Fees");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task SubmitSelectedAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.AdditionalFees.Create", "Submit additional fee")) return;
            int feeId = _hasUnsavedEntry
                ? await CreateDraftFromCurrentEntryAsync(showSuccess: false)
                : GetSelectedOrCurrentFeeId();
            if (feeId <= 0)
            {
                UIHelper.ShowInfo("Enter an additional fee or select an existing draft first.", "Additional Fees");
                return;
            }

            try
            {
                SetBusyState(true);
                var result = await _service.SubmitForApprovalAsync(feeId, CurrentActor());
                ShowServiceResult(result.Success, result.Message);
                await RefreshHistoryAsync();
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task ApproveSelectedAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.AdditionalFees.Approve", "Approve additional fee")) return;
            int feeId = GetSelectedOrCurrentFeeId();
            if (feeId <= 0)
            {
                UIHelper.ShowInfo("Select an additional fee first.", "Additional Fees");
                return;
            }

            if (UIHelper.ShowConfirmation("Approve this additional fee and post it to affected student accounts?", "Approve Additional Fee") != DialogResult.Yes)
                return;

            try
            {
                SetBusyState(true, "Approving...");
                var result = await _service.ApproveAsync(feeId, CurrentActor());
                ShowServiceResult(result.Success, result.Message);
                await RefreshHistoryAsync();
                await PreviewCurrentAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not approve this additional fee.\n\n" + ex.Message, "Additional Fees");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task RejectSelectedAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.AdditionalFees.Approve", "Reject additional fee")) return;
            int feeId = GetSelectedOrCurrentFeeId();
            if (feeId <= 0)
            {
                UIHelper.ShowInfo("Select an additional fee first.", "Additional Fees");
                return;
            }

            string reason = PromptForText("Reject Additional Fee", "Reason for rejection");
            if (string.IsNullOrWhiteSpace(reason)) return;

            try
            {
                SetBusyState(true);
                var result = await _service.RejectAsync(feeId, CurrentActor(), reason);
                ShowServiceResult(result.Success, result.Message);
                await RefreshHistoryAsync();
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private AdditionalFee BuildFeeFromInputs()
        {
            string mode = Convert.ToString(_modeBox.SelectedItem) ?? AdditionalFeeAssignmentModes.Flat;
            bool isFlat = string.Equals(mode, AdditionalFeeAssignmentModes.Flat, StringComparison.OrdinalIgnoreCase);
            var fee = new AdditionalFee
            {
                FeeName = _feeNameBox.Text.Trim(),
                Description = _descriptionBox.Text.Trim(),
                AcademicYear = _academicYearBox.Text.Trim(),
                TermName = Convert.ToString(_termBox.SelectedItem) ?? "",
                DueDate = _dueDatePicker.Checked ? (DateTime?)_dueDatePicker.Value.Date : null,
                AssignmentMode = mode,
                DefaultAmount = isFlat ? _defaultAmountBox.Value : 0m,
                IsCompulsory = _compulsoryBox.Checked,
                AllowsPartPayment = _partPaymentBox.Checked,
                NotifyParentsBySms = _notifySmsBox.Checked,
                CreatedBy = CurrentActor()
            };

            if (!isFlat)
            {
                foreach (DataGridViewRow row in _scopeGrid.Rows)
                {
                    string scopeKey = Convert.ToString(row.Cells["ScopeKey"].Value);
                    decimal amount;
                    if (string.IsNullOrWhiteSpace(scopeKey) || !decimal.TryParse(Convert.ToString(row.Cells["Amount"].Value), out amount) || amount <= 0)
                        continue;

                    fee.Amounts.Add(new AdditionalFeeAmount
                    {
                        ScopeType = string.Equals(mode, AdditionalFeeAssignmentModes.Class, StringComparison.OrdinalIgnoreCase) ? AdditionalFeeScopeTypes.Class : AdditionalFeeScopeTypes.Department,
                        ScopeKey = scopeKey.Trim(),
                        Amount = amount
                    });
                }
            }

            return fee;
        }

        private async Task<int> CreateDraftFromCurrentEntryAsync(bool showSuccess)
        {
            var result = await _service.CreateDraftAsync(BuildFeeFromInputs());
            if (!result.Success)
            {
                UIHelper.ShowWarning(result.Message, "Additional Fees");
                return 0;
            }

            _currentFeeId = result.AdditionalFeeId;
            _hasUnsavedEntry = false;
            if (showSuccess)
            {
                UIHelper.ShowSuccess(result.Message, "Additional Fees");
            }
            return result.AdditionalFeeId;
        }

        private void WireEntryDirtyTracking()
        {
            _feeNameBox.TextChanged += (s, e) => MarkEntryDirty();
            _descriptionBox.TextChanged += (s, e) => MarkEntryDirty();
            _academicYearBox.TextChanged += (s, e) => MarkEntryDirty();
            _termBox.SelectedIndexChanged += (s, e) => MarkEntryDirty();
            _modeBox.SelectedIndexChanged += (s, e) => MarkEntryDirty();
            _dueDatePicker.ValueChanged += (s, e) => MarkEntryDirty();
            _defaultAmountBox.ValueChanged += (s, e) => MarkEntryDirty();
            _compulsoryBox.CheckedChanged += (s, e) => MarkEntryDirty();
            _partPaymentBox.CheckedChanged += (s, e) => MarkEntryDirty();
            _notifySmsBox.CheckedChanged += (s, e) => MarkEntryDirty();
        }

        private void MarkEntryDirty()
        {
            _hasUnsavedEntry = true;
            _currentFeeId = 0;
        }

        private void PopulateScopeGrid()
        {
            if (_scopeGrid == null || _modeBox == null) return;
            string mode = Convert.ToString(_modeBox.SelectedItem) ?? AdditionalFeeAssignmentModes.Flat;

            bool isFlat = string.Equals(mode, AdditionalFeeAssignmentModes.Flat, StringComparison.OrdinalIgnoreCase);
            if (_scopeContainer != null)
            {
                _scopeContainer.Visible = !isFlat;
            }

            var table = new DataTable();
            table.Columns.Add("ScopeKey", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));

            if (string.Equals(mode, AdditionalFeeAssignmentModes.Class, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var className in AppConfig.ClassNames) table.Rows.Add(className, 0m);
            }
            else if (string.Equals(mode, AdditionalFeeAssignmentModes.Department, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var department in TimetableDepartments.Names) table.Rows.Add(department, 0m);
            }

            _scopeGrid.DataSource = table;
            _scopeGrid.ReadOnly = isFlat;
            _scopeGrid.Enabled = !_scopeGrid.ReadOnly;
            _defaultAmountBox.Enabled = isFlat;
            if (_defaultAmountLabel != null)
            {
                _defaultAmountLabel.Text = isFlat
                    ? "FLAT AMOUNT (GHS)"
                    : "AMOUNT SOURCE";
            }
            if (isFlat)
            {
                _defaultAmountBox.BackColor = Color.White;
            }
            else
            {
                _defaultAmountBox.Value = 0m;
                _defaultAmountBox.BackColor = Color.FromArgb(248, 250, 252);
            }
        }

        private async Task RefreshHistoryAsync()
        {
            var fees = await _repository.GetRecentAsync(80);
            _historyGrid.DataSource = ToHistoryTable(fees);
            if (_historyGrid.Columns.Contains("AdditionalFeeId"))
                _historyGrid.Columns["AdditionalFeeId"].Visible = false;
        }

        private void SelectHistoryFee()
        {
            int feeId = GetSelectedFeeId();
            if (feeId > 0) _currentFeeId = feeId;
        }

        private int GetSelectedOrCurrentFeeId()
        {
            int selected = GetSelectedFeeId();
            return selected > 0 ? selected : _currentFeeId;
        }

        private int GetSelectedFeeId()
        {
            if (_historyGrid == null || _historyGrid.CurrentRow == null || !_historyGrid.Columns.Contains("AdditionalFeeId")) return 0;
            int id;
            return int.TryParse(Convert.ToString(_historyGrid.CurrentRow.Cells["AdditionalFeeId"].Value), out id) ? id : 0;
        }

        private static DataTable ToPreviewTable(AdditionalFeePreview preview)
        {
            var table = new DataTable();
            table.Columns.Add("Student ID");
            table.Columns.Add("Student");
            table.Columns.Add("Class");
            table.Columns.Add("Amount", typeof(decimal));
            foreach (var student in preview.Students)
                table.Rows.Add(student.StudentId, student.StudentName, student.ClassId, student.Amount);
            return table;
        }

        private static DataTable ToHistoryTable(System.Collections.Generic.IReadOnlyList<AdditionalFee> fees)
        {
            var table = new DataTable();
            table.Columns.Add("AdditionalFeeId", typeof(int));
            table.Columns.Add("Fee");
            table.Columns.Add("Term");
            table.Columns.Add("Year");
            table.Columns.Add("Mode");
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("SMS");
            table.Columns.Add("Status");
            table.Columns.Add("Created By");
            table.Columns.Add("Created");
            foreach (var fee in fees)
            {
                table.Rows.Add(fee.AdditionalFeeId, fee.FeeName, fee.TermName, fee.AcademicYear, fee.AssignmentMode,
                    fee.DefaultAmount, fee.NotifyParentsBySms ? "Yes" : "No", fee.Status, fee.CreatedBy,
                    fee.CreatedDate.ToString("dd MMM yyyy HH:mm"));
            }
            return table;
        }

        private static void ShowServiceResult(bool success, string message)
        {
            if (success) UIHelper.ShowSuccess(message, "Additional Fees");
            else UIHelper.ShowWarning(message, "Additional Fees");
        }

        private static string PromptForText(string title, string label)
        {
            using (var form = new Form())
            using (var input = new TextBox())
            using (var ok = new Button())
            using (var cancel = new Button())
            using (var caption = new Label())
            {
                form.Text = title;
                form.Size = new Size(460, 170);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                caption.Text = label;
                caption.SetBounds(16, 16, 410, 22);
                input.SetBounds(16, 44, 410, 28);
                ok.Text = "OK";
                ok.SetBounds(246, 86, 84, 30);
                ok.DialogResult = DialogResult.OK;
                cancel.Text = "Cancel";
                cancel.SetBounds(342, 86, 84, 30);
                cancel.DialogResult = DialogResult.Cancel;
                form.Controls.AddRange(new Control[] { caption, input, ok, cancel });
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                return form.ShowDialog() == DialogResult.OK ? input.Text.Trim() : "";
            }
        }

        private static string CurrentActor()
        {
            return AuthService.CurrentUser == null ? Environment.UserName : AuthService.CurrentUser.DisplayName;
        }
    }
}
