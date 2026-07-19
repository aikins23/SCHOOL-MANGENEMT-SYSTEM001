using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Standalone, searchable payment-history browser matching the modern Payment Ledger UI design.
    /// Access: Accountant / Director / Administrator / Headmaster.
    /// </summary>
    public class frmPaymentHistory : Form
    {
        private readonly FeeRepository _feeRepository = new FeeRepository(AppConfig.ConnectionString);
        private TextBox _searchBox;
        private DataGridView _grid;
        private DataGridView _ledgerGrid;
        private DataGridView _studentPaymentsGrid;
        private Label _countLbl;
        private Button _prevPageBtn;
        private Button _nextPageBtn;
        private Button _pageIndicatorBtn;
        private Button _refreshBtn;

        // Selected learner card elements
        private CircleBadge _learnerAvatarBadge;
        private Label _learnerNameLbl;
        private Label _metaStudentIdLbl;
        private Label _metaClassLbl;
        private Label _metaTermLbl;

        // Summary metric cards
        private Label _previousValueLbl;
        private Label _currentValueLbl;
        private Label _paidValueLbl;
        private Label _balanceValueLbl;
        private Label _termValueLbl;

        private int _currentPage = 1;
        private const int PageSize = 10;
        private int _totalRows;
        private Timer _searchTimer;
        private string _selectedStudentId;
        private string _initialStudentId;
        private bool _initialSelectionApplied;

        private Control _mainGridContainer;
        private Control _bottomPanelsContainer;
        private Button _tabAccountSummary;
        private Button _tabTermLedger;
        private Button _tabPaymentTransactions;

        public frmPaymentHistory() : this(null)
        {
        }

        public frmPaymentHistory(string studentId)
        {
            _initialStudentId = Common.StudentId.Parse(studentId);
            BuildUi();
            Common.NavigationSidebar.AddTo(this);
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmPaymentHistory", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "Payment History - Payment Ledger";
            Size = new Size(1420, 880);
            MinimumSize = new Size(1160, 760);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);

            _grid = CreateLedgerGrid();
            _grid.SelectionChanged += async (s, e) => await LoadSelectedStudentAsync();
            Common.StudentId.AttachGridFormatting(_grid, "STUDENT ID");

            _ledgerGrid = CreateLedgerGrid();
            _studentPaymentsGrid = CreateLedgerGrid();
            Common.StudentId.AttachGridFormatting(_studentPaymentsGrid, "STUDENT ID");

            _grid.CellFormatting += Grid_CellFormatting;
            _ledgerGrid.CellFormatting += Grid_CellFormatting;
            _studentPaymentsGrid.CellFormatting += Grid_CellFormatting;

            _grid.CellPainting += Grid_CellPainting;
            _studentPaymentsGrid.CellPainting += Grid_CellPainting;

            var mainScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(32, 24, 32, 32)
            };

            var pageLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var titleLabel = new Label
            {
                Text = "Payment Ledger",
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var subtitleLabel = new Label
            {
                Text = "Review learner payments, track balances, and audit transactions.",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16)
            };

            var headingBox = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            headingBox.Controls.Add(titleLabel, 0, 0);
            headingBox.Controls.Add(subtitleLabel, 0, 1);

            var toolbar = BuildToolbar();
            toolbar.Margin = new Padding(0, 0, 0, 20);

            var learnerCard = BuildLearnerHeaderPanel();
            learnerCard.Margin = new Padding(0, 0, 0, 20);

            var summaryCards = BuildSummaryCards();
            summaryCards.Margin = new Padding(0, 0, 0, 24);

            var tabsAndGridSection = BuildTabsAndGridContainer();
            tabsAndGridSection.Height = 360;
            tabsAndGridSection.Margin = new Padding(0, 0, 0, 24);

            var bottomLedgerSection = BuildBottomLedgerPanel();
            bottomLedgerSection.Height = 240;

            pageLayout.Controls.Add(headingBox, 0, 0);
            pageLayout.Controls.Add(toolbar, 0, 1);
            pageLayout.Controls.Add(learnerCard, 0, 2);
            pageLayout.Controls.Add(summaryCards, 0, 3);
            pageLayout.Controls.Add(tabsAndGridSection, 0, 4);
            pageLayout.Controls.Add(bottomLedgerSection, 0, 5);

            mainScrollPanel.Controls.Add(pageLayout);
            Controls.Add(mainScrollPanel);
        }

        private Control BuildToolbar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var searchContainer = new RoundedPanel(22, Color.White, Color.FromArgb(226, 232, 240))
            {
                Width = 380,
                Height = 42,
                Left = 0,
                Top = 2
            };

            var searchIconLabel = new Label
            {
                Text = "\uE721",
                Font = new Font("Segoe MDL2 Assets", 11F),
                Width = 36,
                Height = 42,
                Left = 8,
                Top = 8,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            _searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(30, 41, 59),
                Left = 44,
                Top = 11,
                Width = 320
            };
            SetCueBanner(_searchBox, "Search by learner name, student ID or class");
            _searchBox.TextChanged += (s, e) => QueueHistoryReload();

            _searchTimer = new Timer { Interval = 350 };
            _searchTimer.Tick += async (s, e) =>
            {
                _searchTimer.Stop();
                await LoadAsync();
            };

            searchContainer.Controls.Add(searchIconLabel);
            searchContainer.Controls.Add(_searchBox);

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 2, 0, 0)
            };

            var filtersBtn = CreateStyledPillButton("Filters", "\uE71C", Color.White, Color.FromArgb(51, 65, 85), Color.FromArgb(226, 232, 240), 108);
            var dateFilterCombo = CreateStyledDateDropdown("01/01/2025 - 31/12/2026", 210);
            var exportBtn = CreateStyledPillButton("Export", "\uE896", Color.White, Color.FromArgb(51, 65, 85), Color.FromArgb(226, 232, 240), 108);

            exportBtn.Click += async (s, e) => await ExportLedgerAsync(exportBtn);

            _refreshBtn = CreateStyledPillButton("Refresh", "\uE72C", Color.FromArgb(15, 23, 42), Color.White, Color.FromArgb(15, 23, 42), 102);
            _refreshBtn.Click += async (s, e) => await LoadAsync();

            rightFlow.Controls.Add(filtersBtn);
            rightFlow.Controls.Add(dateFilterCombo);
            rightFlow.Controls.Add(exportBtn);
            rightFlow.Controls.Add(_refreshBtn);

            bar.Controls.Add(searchContainer);
            bar.Controls.Add(rightFlow);
            return bar;
        }

        private Button CreateStyledPillButton(string text, string icon, Color backColor, Color foreColor, Color borderColor, int width)
        {
            var btn = new Button
            {
                Text = text,
                Tag = icon,
                Width = width,
                Height = 40,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btn.FlatAppearance.BorderColor = borderColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using (var path = GetRoundedRectPath(rect, 8))
                using (var brush = new SolidBrush(btn.BackColor))
                using (var pen = new Pen(btn.FlatAppearance.BorderColor, 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
                string glyph = Convert.ToString(btn.Tag);
                using (var iconFont = new Font("Segoe MDL2 Assets", 11F))
                {
                    int iconWidth = 18;
                    int gap = 8;
                    int textWidth = TextRenderer.MeasureText(btn.Text, btn.Font).Width;
                    int contentWidth = iconWidth + gap + textWidth;
                    int startX = Math.Max(8, (btn.Width - contentWidth) / 2);
                    var iconRect = new Rectangle(startX, 0, iconWidth, btn.Height);
                    var textRect = new Rectangle(startX + iconWidth + gap, 0, textWidth + 4, btn.Height);
                    TextRenderer.DrawText(e.Graphics, glyph, iconFont, iconRect, btn.ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, textRect, btn.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            };
            return btn;
        }

        private Control CreateStyledDateDropdown(string text, int width)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 40,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btn.FlatAppearance.BorderSize = 1;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using (var path = GetRoundedRectPath(rect, 8))
                using (var brush = new SolidBrush(btn.BackColor))
                using (var pen = new Pen(btn.FlatAppearance.BorderColor, 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
                using (var iconFont = new Font("Segoe MDL2 Assets", 10.5F))
                {
                    TextRenderer.DrawText(e.Graphics, "\uE787", iconFont, new Rectangle(12, 0, 22, btn.Height), btn.ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(42, 0, btn.Width - 72, btn.Height), btn.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(e.Graphics, "\uE70D", iconFont, new Rectangle(btn.Width - 28, 0, 18, btn.Height), btn.ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            };
            return btn;
        }

        private Control BuildLearnerHeaderPanel()
        {
            var card = new RoundedPanel(12, Color.White, Color.FromArgb(226, 232, 240))
            {
                Dock = DockStyle.Top,
                Height = 124,
                Padding = new Padding(24, 18, 24, 18)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _learnerAvatarBadge = new CircleBadge(Color.FromArgb(15, 23, 42), "--", new Font("Segoe UI Semibold", 18F, FontStyle.Bold), Color.White)
            {
                Width = 58,
                Height = 58,
                Margin = new Padding(0, 6, 16, 0)
            };

            var identityPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.White };
            identityPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            identityPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _learnerNameLbl = new Label
            {
                Text = "SELECT A LEARNER",
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };

            var metaTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.White,
                Margin = new Padding(0, 4, 0, 0)
            };
            metaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            metaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            metaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            metaTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metaTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var capStudentId = new Label { Text = "Student ID", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };
            var capClass = new Label { Text = "Class", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };
            var capTerm = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            capTerm.Controls.Add(new Label
            {
                Text = "\uE787",
                Font = new Font("Segoe MDL2 Assets", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0, 1, 4, 0)
            });
            capTerm.Controls.Add(new Label
            {
                Text = "Active Term",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = Padding.Empty
            });

            _metaStudentIdLbl = new Label { Text = "-", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), AutoSize = true };
            _metaClassLbl = new Label { Text = "-", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), AutoSize = true };
            _metaTermLbl = new Label { Text = "No active term", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), AutoSize = true };

            metaTable.Controls.Add(capStudentId, 0, 0);
            metaTable.Controls.Add(capClass, 1, 0);
            metaTable.Controls.Add(capTerm, 2, 0);

            metaTable.Controls.Add(_metaStudentIdLbl, 0, 1);
            metaTable.Controls.Add(_metaClassLbl, 1, 1);
            metaTable.Controls.Add(_metaTermLbl, 2, 1);

            identityPanel.Controls.Add(_learnerNameLbl, 0, 0);
            identityPanel.Controls.Add(metaTable, 0, 1);

            var chevron = new Label
            {
                Dock = DockStyle.Fill,
                Text = "\uE70D",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe MDL2 Assets", 12F),
                TextAlign = ContentAlignment.MiddleRight
            };

            layout.Controls.Add(_learnerAvatarBadge, 0, 0);
            layout.Controls.Add(identityPanel, 1, 0);
            layout.Controls.Add(chevron, 2, 0);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildSummaryCards()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 104,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _previousValueLbl = AddSummaryMetricCard(grid, "Previous Debt", "GHS 0.00", 0, Color.FromArgb(239, 246, 255), Color.FromArgb(59, 130, 246), "\uE8A5");
            _currentValueLbl = AddSummaryMetricCard(grid, "Current Fee", "GHS 0.00", 1, Color.FromArgb(236, 253, 245), Color.FromArgb(16, 185, 129), "\uE8EF");
            _paidValueLbl = AddSummaryMetricCard(grid, "Paid", "GHS 0.00", 2, Color.FromArgb(243, 232, 255), Color.FromArgb(139, 92, 246), "\uE8C7");
            _balanceValueLbl = AddSummaryMetricCard(grid, "Balance", "GHS 0.00", 3, Color.FromArgb(255, 247, 237), Color.FromArgb(217, 119, 6), "\uE9D2", isBalanceCard: true);
            _termValueLbl = new Label { Text = "-" };
            return grid;
        }

        private Label AddSummaryMetricCard(TableLayoutPanel parent, string title, string value, int column, Color iconBg, Color accentColor, string iconText, bool isBalanceCard = false)
        {
            var card = new RoundedPanel(12, Color.White, isBalanceCard ? Color.FromArgb(251, 191, 36) : Color.FromArgb(226, 232, 240))
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(column == 0 ? 0 : 10, 0, column == 3 ? 0 : 10, 0),
                Padding = new Padding(18, 14, 18, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var iconBadge = new CircleBadge(iconBg, iconText, new Font("Segoe MDL2 Assets", 14F), accentColor)
            {
                Width = 44,
                Height = 44,
                Margin = new Padding(0, 10, 12, 0)
            };

            var textLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.White };
            textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = new Label
            {
                Text = title,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            var valueLabel = new Label
            {
                Text = value,
                ForeColor = isBalanceCard ? Color.FromArgb(217, 119, 6) : Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI Semibold", 16.5F, FontStyle.Bold),
                AutoSize = true,
                AutoEllipsis = false
            };
            textLayout.Controls.Add(caption, 0, 0);
            textLayout.Controls.Add(valueLabel, 0, 1);

            layout.Controls.Add(iconBadge, 0, 0);
            layout.Controls.Add(textLayout, 1, 0);
            card.Controls.Add(layout);
            parent.Controls.Add(card, column, 0);
            return valueLabel;
        }

        private Control BuildTabsAndGridContainer()
        {
            var container = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var tabFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _tabAccountSummary = CreateTabButton("Account Summary", false);
            _tabTermLedger = CreateTabButton("Term Ledger", false);
            _tabPaymentTransactions = CreateTabButton("Payment Transactions", true);

            _tabAccountSummary.Click += (s, e) => SelectTab(_tabAccountSummary);
            _tabTermLedger.Click += (s, e) => SelectTab(_tabTermLedger);
            _tabPaymentTransactions.Click += (s, e) => SelectTab(_tabPaymentTransactions);

            tabFlow.Controls.Add(_tabAccountSummary);
            tabFlow.Controls.Add(_tabTermLedger);
            tabFlow.Controls.Add(_tabPaymentTransactions);
            tabBar.Controls.Add(tabFlow);

            var tabSeparator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };

            var gridCard = new RoundedPanel(12, Color.White, Color.FromArgb(226, 232, 240))
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };

            var gridInnerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            gridInnerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            gridInnerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;

            var footerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _countLbl = new Label
            {
                Text = "Showing 1-10 of 10",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Left = 24,
                Top = 16,
                AutoSize = true
            };

            var paginationFlow = new FlowLayoutPanel
            {
                Width = 240,
                Height = 36,
                Top = 8,
                Left = gridCard.Width - 260,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _prevPageBtn = CreatePaginationButton("Previous", false, false);
            _pageIndicatorBtn = CreatePaginationButton("1", true, true);
            _nextPageBtn = CreatePaginationButton("Next", false, false);

            _prevPageBtn.Click += async (s, e) => await MoveHistoryPageAsync(-1);
            _nextPageBtn.Click += async (s, e) => await MoveHistoryPageAsync(1);

            paginationFlow.Controls.Add(_prevPageBtn);
            paginationFlow.Controls.Add(_pageIndicatorBtn);
            paginationFlow.Controls.Add(_nextPageBtn);

            footerPanel.Controls.Add(_countLbl);
            footerPanel.Controls.Add(paginationFlow);
            footerPanel.Resize += (s, e) => { paginationFlow.Left = footerPanel.Width - paginationFlow.Width - 24; };

            gridInnerLayout.Controls.Add(_grid, 0, 0);
            gridInnerLayout.Controls.Add(footerPanel, 0, 1);
            gridCard.Controls.Add(gridInnerLayout);

            container.Controls.Add(gridCard);
            container.Controls.Add(tabSeparator);
            container.Controls.Add(tabBar);
            gridCard.BringToFront();

            _mainGridContainer = container;
            return container;
        }

        private Button CreateTabButton(string text, bool isActive)
        {
            var btn = new Button
            {
                Text = text,
                Width = TextRenderer.MeasureText(text, new Font("Segoe UI Semibold", 10.5F)).Width + 36,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5F, isActive ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isActive ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) =>
            {
                if (btn.Tag != null && (bool)btn.Tag)
                {
                    using (var pen = new Pen(Color.FromArgb(217, 119, 6), 3))
                    {
                        e.Graphics.DrawLine(pen, 0, btn.Height - 2, btn.Width, btn.Height - 2);
                    }
                }
            };
            btn.Tag = isActive;
            return btn;
        }

        private void SelectTab(Button activeTab)
        {
            foreach (var tab in new[] { _tabAccountSummary, _tabTermLedger, _tabPaymentTransactions })
            {
                bool isTarget = (tab == activeTab);
                tab.Tag = isTarget;
                tab.Font = new Font("Segoe UI Semibold", 10.5F, isTarget ? FontStyle.Bold : FontStyle.Regular);
                tab.ForeColor = isTarget ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139);
                tab.Invalidate();
            }

            if (activeTab == _tabTermLedger)
            {
                _grid.DataSource = _ledgerGrid.DataSource;
                ApplyLedgerColumnLayout();
            }
            else if (activeTab == _tabAccountSummary)
            {
                _grid.DataSource = _studentPaymentsGrid.DataSource;
                ApplyStudentPaymentColumnLayout();
            }
            else
            {
                _ = LoadAsync();
            }
        }

        private Button CreatePaginationButton(string text, bool isActivePage, bool isSquare)
        {
            var btn = new Button
            {
                Text = text,
                Width = isSquare ? 36 : 80,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = isActivePage ? Color.White : Color.FromArgb(100, 116, 139),
                BackColor = isActivePage ? Color.FromArgb(217, 119, 6) : Color.White,
                Cursor = isActivePage ? Cursors.Default : Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderColor = isActivePage ? Color.FromArgb(217, 119, 6) : Color.FromArgb(226, 232, 240);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private Control BuildBottomLedgerPanel()
        {
            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 240,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var termCard = BuildLedgerTableCard("Term History (All Terms)", "\uE81C", _ledgerGrid);
            var recentCard = BuildLedgerTableCard("Recent Learner Payments", "\uE8A5", _studentPaymentsGrid);

            termCard.Margin = new Padding(0, 0, 10, 0);
            recentCard.Margin = new Padding(10, 0, 0, 0);

            bottom.Controls.Add(termCard, 0, 0);
            bottom.Controls.Add(recentCard, 1, 0);
            _bottomPanelsContainer = bottom;
            return bottom;
        }

        private Panel BuildLedgerTableCard(string title, string icon, DataGridView grid)
        {
            var card = new RoundedPanel(12, Color.White, Color.FromArgb(226, 232, 240))
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe MDL2 Assets", 10.5F),
                ForeColor = Color.FromArgb(15, 23, 42),
                Left = 18,
                Top = 14,
                Width = 20,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Left = 46,
                Top = 12,
                AutoSize = true
            };
            var viewAllLabel = new Label
            {
                Text = "View all",
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Top = 13,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            headerPanel.Controls.Add(iconLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(viewAllLabel);
            headerPanel.Resize += (s, e) => { viewAllLabel.Left = headerPanel.Width - viewAllLabel.Width - 18; };

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;

            layout.Controls.Add(headerPanel, 0, 0);
            layout.Controls.Add(grid, 0, 1);
            card.Controls.Add(layout);
            return card;
        }

        private DataGridView CreateLedgerGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 40 }
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5F),
                SelectionBackColor = Color.FromArgb(210, 151, 35),
                SelectionForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12, 4, 12, 4),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };

            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(226, 232, 240);
            return grid;
        }

        private async Task LoadAsync()
        {
            try
            {
                string search = _searchBox?.Text?.Trim();
                if (!_initialSelectionApplied && string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(_initialStudentId))
                {
                    search = _initialStudentId;
                }

                var page = await _feeRepository.GetPaymentHistoryPageAsync(_currentPage, PageSize, search);
                DataTable rawTable = page.Items;
                _totalRows = page.TotalCount;

                // Format table for exact Payment Transactions mockup look when in default view
                DataTable formattedTable = TransformToTransactionMockup(rawTable);
                _grid.DataSource = formattedTable;
                ApplyColumnLayout();

                if (!_initialSelectionApplied && !string.IsNullOrWhiteSpace(_initialStudentId))
                {
                    SelectStudentInHistory(_initialStudentId);
                    await LoadStudentLedgerAsync(_initialStudentId);
                    _initialSelectionApplied = true;
                }
                else if (rawTable != null && rawTable.Rows.Count > 0 && string.IsNullOrWhiteSpace(_selectedStudentId))
                {
                    string colName = null;
                    if (rawTable.Columns.Contains("STUDENT ID")) colName = "STUDENT ID";
                    else if (rawTable.Columns.Contains("StudentID")) colName = "StudentID";
                    else if (rawTable.Columns.Contains("student_id")) colName = "student_id";

                    if (colName != null)
                    {
                        var firstId = Common.StudentId.Parse(rawTable.Rows[0][colName]?.ToString());
                        if (!string.IsNullOrWhiteSpace(firstId))
                        {
                            await LoadStudentLedgerAsync(firstId);
                        }
                    }
                }
                UpdateHistoryPageStatus();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("History load error: " + ex.Message, "Payment History");
            }
        }

        private async Task ExportLedgerAsync(Button exportButton)
        {
            using (var dialog = new SaveFileDialog
            {
                Title = "Export Payment Ledger",
                Filter = "CSV file (*.csv)|*.csv",
                DefaultExt = "csv",
                AddExtension = true,
                FileName = "PaymentLedger_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string originalText = exportButton.Text;
                exportButton.Enabled = false;
                exportButton.Text = "Exporting";
                exportButton.Invalidate();

                try
                {
                    const int exportPageSize = 500;
                    int pageNumber = 1;
                    int exportedRows = 0;
                    int totalRows = 0;
                    bool headerWritten = false;
                    string search = _searchBox?.Text?.Trim();
                    var csv = new StringBuilder();

                    do
                    {
                        var page = await _feeRepository.GetPaymentHistoryPageAsync(pageNumber, exportPageSize, search);
                        totalRows = page.TotalCount;
                        var table = TransformToTransactionMockup(page.Items);

                        if (!headerWritten)
                        {
                            for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                            {
                                if (columnIndex > 0) csv.Append(',');
                                csv.Append(EscapeCsv(table.Columns[columnIndex].ColumnName));
                            }
                            csv.AppendLine();
                            headerWritten = true;
                        }

                        foreach (DataRow row in table.Rows)
                        {
                            for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                            {
                                if (columnIndex > 0) csv.Append(',');
                                csv.Append(EscapeCsv(row[columnIndex]));
                            }
                            csv.AppendLine();
                        }

                        exportedRows += table.Rows.Count;
                        pageNumber++;
                        if (table.Rows.Count == 0) break;
                    }
                    while (exportedRows < totalRows);

                    string outputPath = dialog.FileName;
                    string csvContent = csv.ToString();
                    await Task.Run(() => File.WriteAllText(outputPath, csvContent, new UTF8Encoding(true)));
                    UIHelper.ShowInfo($"Exported {exportedRows:N0} payment ledger record(s).\n\n{outputPath}", "Export Ledger");
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError("Payment ledger export failed: " + ex.Message, "Export Ledger");
                }
                finally
                {
                    exportButton.Text = originalText;
                    exportButton.Enabled = true;
                    exportButton.Invalidate();
                }
            }
        }

        private static string EscapeCsv(object value)
        {
            string text = value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value);
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private DataTable TransformToTransactionMockup(DataTable source)
        {
            if (source == null) return new DataTable();
            var dt = new DataTable();
            dt.Columns.Add("STUDENT ID", typeof(string));
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Transaction Type", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Debit (GHS)", typeof(string));
            dt.Columns.Add("Credit (GHS)", typeof(string));
            dt.Columns.Add("Balance (GHS)", typeof(string));
            dt.Columns.Add("Payment Mode", typeof(string));
            dt.Columns.Add("Recorded By", typeof(string));

            foreach (DataRow row in source.Rows)
            {
                string studentId = row.Table.Columns.Contains("STUDENT ID") ? row["STUDENT ID"]?.ToString() : "";
                string dateStr = row.Table.Columns.Contains("PAYMENT DATE") ? ConvertToDateString(row["PAYMENT DATE"]) : "";
                string timeStr = row.Table.Columns.Contains("PAYMENT TIME") ? ConvertToTimeString(row["PAYMENT TIME"]) : "";
                string fullDate = $"{dateStr} {timeStr}".Trim();

                decimal amountPaid = row.Table.Columns.Contains("AMOUNT PAID") ? ParseDecimal(row["AMOUNT PAID"]) : 0m;
                decimal balance = row.Table.Columns.Contains("BALANCE") ? ParseDecimal(row["BALANCE"]) : 0m;
                decimal previousBalance = row.Table.Columns.Contains("PREVIOUS BALANCE") ? ParseDecimal(row["PREVIOUS BALANCE"]) : 0m;
                string mode = row.Table.Columns.Contains("PAYMENT MODE") ? row["PAYMENT MODE"]?.ToString()?.Trim() : "Cash";
                string bursar = row.Table.Columns.Contains("BURSAR NAME") ? row["BURSAR NAME"]?.ToString()?.Trim() : "System";

                string txType = "PAYMENT";
                string desc = string.IsNullOrWhiteSpace(mode) ? "Fee Payment" : mode;
                decimal balanceIncrease = Math.Max(0m, balance - previousBalance);
                decimal debit = balanceIncrease;
                decimal credit = Math.Max(0m, amountPaid);

                if (mode != null && (mode.IndexOf("CHARGE", StringComparison.OrdinalIgnoreCase) >= 0 || mode.IndexOf("Posted", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    txType = "CHARGE";
                    credit = 0m;
                    if (string.IsNullOrWhiteSpace(desc) || desc.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase)) desc = "Additional Fee Posted";
                }
                else if (mode != null && mode.IndexOf("Adjustment", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    txType = "ADJUSTMENT";
                    desc = "Enrollment Adjustment";
                }
                else if (amountPaid <= 0m && balanceIncrease > 0m)
                {
                    txType = "CHARGE";
                    desc = "Opening or fee charge";
                    credit = 0m;
                }

                string displayMode = mode;
                if (txType == "CHARGE" || txType == "ADJUSTMENT")
                {
                    displayMode = "SYSTEM";
                }
                else if (string.IsNullOrWhiteSpace(displayMode) || displayMode.Equals("Additional Fee Posted", StringComparison.OrdinalIgnoreCase))
                {
                    displayMode = "Cash";
                }

                dt.Rows.Add(
                    studentId,
                    fullDate,
                    txType,
                    desc,
                    "GHS " + debit.ToString("N2"),
                    "GHS " + credit.ToString("N2"),
                    "GHS " + balance.ToString("N2"),
                    displayMode,
                    string.IsNullOrWhiteSpace(bursar) ? "System" : bursar
                );
            }
            return dt;
        }

        private static string ConvertToDateString(object val)
        {
            if (val == null || val == DBNull.Value) return "";
            if (DateTime.TryParse(val.ToString(), out DateTime d)) return d.ToString("dd/MM/yyyy");
            return val.ToString();
        }

        private static string ConvertToTimeString(object val)
        {
            if (val == null || val == DBNull.Value) return "";
            if (val is TimeSpan ts) return ts.ToString(@"hh\:mm");
            if (DateTime.TryParse(val.ToString(), out DateTime d)) return d.ToString("HH:mm");
            return val.ToString();
        }

        private static decimal ParseDecimal(object val)
        {
            if (val == null || val == DBNull.Value) return 0m;
            decimal.TryParse(val.ToString().Replace("GHS", "").Trim(), out decimal d);
            return d;
        }

        private void QueueHistoryReload()
        {
            _currentPage = 1;
            if (_searchTimer == null)
            {
                _ = LoadAsync();
                return;
            }

            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async Task MoveHistoryPageAsync(int delta)
        {
            int maxPage = Math.Max(1, (int)Math.Ceiling(_totalRows / (double)PageSize));
            int next = Math.Max(1, Math.Min(maxPage, _currentPage + delta));
            if (next == _currentPage) return;
            _currentPage = next;
            await LoadAsync();
        }

        private void UpdateHistoryPageStatus()
        {
            int shown = _grid?.Rows?.Count ?? 0;
            int maxPage = Math.Max(1, (int)Math.Ceiling(_totalRows / (double)PageSize));
            if (_currentPage > maxPage) _currentPage = maxPage;

            int start = _totalRows == 0 ? 0 : ((_currentPage - 1) * PageSize) + 1;
            int end = _totalRows == 0 ? 0 : Math.Min(_totalRows, start + shown - 1);
            if (_countLbl != null)
            {
                _countLbl.Text = _totalRows == 0 ? "Showing 0 records" : $"Showing {start}-{end} of {_totalRows}";
            }
            if (_pageIndicatorBtn != null) _pageIndicatorBtn.Text = _currentPage.ToString();
            if (_prevPageBtn != null) _prevPageBtn.Enabled = _currentPage > 1;
            if (_nextPageBtn != null) _nextPageBtn.Enabled = _currentPage < maxPage;
        }

        private async Task LoadSelectedStudentAsync()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.DataBoundItem == null) return;
            var rowView = _grid.CurrentRow.DataBoundItem as DataRowView;
            if (rowView == null || rowView.Row == null || rowView.Row.Table == null) return;

            string colName = null;
            if (rowView.Row.Table.Columns.Contains("STUDENT ID")) colName = "STUDENT ID";
            else if (rowView.Row.Table.Columns.Contains("StudentID")) colName = "StudentID";
            else if (rowView.Row.Table.Columns.Contains("student_id")) colName = "student_id";

            if (colName == null) return;

            string studentId = Common.StudentId.Parse(rowView[colName]?.ToString());
            if (string.IsNullOrWhiteSpace(studentId) || studentId == _selectedStudentId) return;
            await LoadStudentLedgerAsync(studentId);
        }

        private async Task LoadStudentLedgerAsync(string studentId)
        {
            _selectedStudentId = Common.StudentId.Parse(studentId);
            if (string.IsNullOrWhiteSpace(_selectedStudentId)) return;

            try
            {
                string displayId = Common.StudentId.Display(_selectedStudentId);
                UpdateSelectedLearnerHeader(displayId);

                var session = new AcademicSessionService();
                var billingTask = session.GetStudentBillingBreakdownAsync(_selectedStudentId);
                var ledgerTask = _feeRepository.GetStudentLedgerTableAsync(_selectedStudentId);
                var paymentsTask = _feeRepository.GetStudentPaymentHistoryTableAsync(_selectedStudentId);

                await Task.WhenAll(billingTask, ledgerTask, paymentsTask);
                var billing = await billingTask;
                var ledger = await ledgerTask;
                var payments = await paymentsTask;

                _ledgerGrid.DataSource = ledger;
                _studentPaymentsGrid.DataSource = payments;
                ApplyLedgerColumnLayout();
                ApplyStudentPaymentColumnLayout();

                string termName = string.IsNullOrWhiteSpace(billing?.TermName) ? "No active term" : billing.TermName;
                _termValueLbl.Text = termName;
                _previousValueLbl.Text = FormatCurrency(billing?.PreviousBalance ?? GetFirstDecimal(ledger, "PREVIOUS DEBT"));
                _currentValueLbl.Text = FormatCurrency(billing?.CurrentTermFee ?? GetFirstDecimal(ledger, "CURRENT FEE"));
                _paidValueLbl.Text = FormatCurrency(billing?.AmountPaid ?? GetFirstDecimal(ledger, "PAID"));
                _balanceValueLbl.Text = FormatCurrency(billing?.Balance ?? GetFirstDecimal(ledger, "BALANCE"));
                UpdateSelectedLearnerHeader(displayId);
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Ledger load error: " + ex.Message, "Payment Ledger");
            }
        }

        private void UpdateSelectedLearnerHeader(string displayId)
        {
            string name = "Selected learner";
            string className = "Not available";

            var rowView = _grid?.CurrentRow?.DataBoundItem as DataRowView;
            if (rowView != null && rowView.Row != null && rowView.Row.Table != null)
            {
                if (rowView.Row.Table.Columns.Contains("STUDENT NAME"))
                {
                    name = Convert.ToString(rowView["STUDENT NAME"])?.Trim();
                }
                else if (rowView.Row.Table.Columns.Contains("student_name"))
                {
                    name = Convert.ToString(rowView["student_name"])?.Trim();
                }

                if (rowView.Row.Table.Columns.Contains("CLASS ID"))
                {
                    className = Convert.ToString(rowView["CLASS ID"])?.Trim();
                }
                else if (rowView.Row.Table.Columns.Contains("ClassID"))
                {
                    className = Convert.ToString(rowView["ClassID"])?.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Selected learner";
            }

            if (_learnerNameLbl != null)
            {
                _learnerNameLbl.Text = name.ToUpperInvariant();
            }

            if (_learnerAvatarBadge != null)
            {
                _learnerAvatarBadge.BadgeText = BuildInitials(name);
            }

            if (_metaStudentIdLbl != null) _metaStudentIdLbl.Text = string.IsNullOrWhiteSpace(displayId) ? "Not available" : displayId;
            if (_metaClassLbl != null) _metaClassLbl.Text = string.IsNullOrWhiteSpace(className) ? "Not available" : className;
            if (_metaTermLbl != null) _metaTermLbl.Text = string.IsNullOrWhiteSpace(_termValueLbl?.Text) || _termValueLbl.Text == "-" ? "No active term" : _termValueLbl.Text;
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "KO";
            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        private void SelectStudentInHistory(string studentId)
        {
            if (!(_grid.DataSource is DataTable table)) return;
            string colName = null;
            if (table.Columns.Contains("STUDENT ID")) colName = "STUDENT ID";
            else if (table.Columns.Contains("StudentID")) colName = "StudentID";
            else if (table.Columns.Contains("student_id")) colName = "student_id";

            if (colName == null) return;

            string parsed = Common.StudentId.Parse(studentId);
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var rowView = row.DataBoundItem as DataRowView;
                if (rowView != null && Common.StudentId.Parse(rowView[colName]?.ToString()) == parsed)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) _grid.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private void ApplyColumnLayout()
        {
            if (_grid.Columns.Count == 0) return;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (_grid.Columns.Contains("STUDENT ID")) _grid.Columns["STUDENT ID"].Visible = false;

            SetColumn("Date", 100, 90, DataGridViewContentAlignment.MiddleCenter);
            SetColumn("Transaction Type", 115, 100, DataGridViewContentAlignment.MiddleCenter);
            SetColumn("Description", 160, 140);
            SetColumn("Debit (GHS)", 105, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn("Credit (GHS)", 105, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn("Balance (GHS)", 110, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn("Payment Mode", 120, 105);
            SetColumn("Recorded By", 110, 95);
        }

        private void ApplyLedgerColumnLayout()
        {
            if (_ledgerGrid.Columns.Count == 0) return;
            SetColumn(_ledgerGrid, "ACADEMIC YEAR", 120, 105);
            SetColumn(_ledgerGrid, "TERM", 100, 90);
            SetColumn(_ledgerGrid, "PREVIOUS DEBT", 110, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn(_ledgerGrid, "CURRENT FEE", 110, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn(_ledgerGrid, "TOTAL EXPECTED", 115, 100, DataGridViewContentAlignment.MiddleRight);
            SetColumn(_ledgerGrid, "PAID", 100, 90, DataGridViewContentAlignment.MiddleRight);
            SetColumn(_ledgerGrid, "BALANCE", 100, 90, DataGridViewContentAlignment.MiddleRight);
        }

        private void ApplyStudentPaymentColumnLayout()
        {
            if (_studentPaymentsGrid.Columns.Count == 0) return;
            SetColumn(_studentPaymentsGrid, "STUDENT ID", 80, 75);
            SetColumn(_studentPaymentsGrid, "CLASS ID", 80, 75);
            SetColumn(_studentPaymentsGrid, "STUDENT NAME", 150, 130);
            SetColumn(_studentPaymentsGrid, "AMOUNT PAID", 110, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn(_studentPaymentsGrid, "BALANCE", 110, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn(_studentPaymentsGrid, "PAYMENT MODE", 120, 100);
            SetColumn(_studentPaymentsGrid, "BURSAR NAME", 130, 110);
            SetColumn(_studentPaymentsGrid, "DATE", 105, 90, DataGridViewContentAlignment.MiddleCenter);
        }

        private void SetColumn(string name, int fillWeight, int minWidth,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            SetColumn(_grid, name, fillWeight, minWidth, alignment);
        }

        private void SetColumn(DataGridView grid, string name, int fillWeight, int minWidth,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            if (grid == null || !grid.Columns.Contains(name)) return;
            var c = grid.Columns[name];
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            c.FillWeight = fillWeight;
            c.MinimumWidth = minWidth;
            c.DefaultCellStyle.Alignment = alignment;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.Value == null || e.Value == DBNull.Value || e.ColumnIndex < 0) return;
            string columnName = grid.Columns[e.ColumnIndex].Name;

            if (columnName == "AMOUNT PAID" || columnName == "BALANCE" || columnName == "BALANCE AFTER"
                || columnName == "PREVIOUS DEBT" || columnName == "CURRENT FEE" || columnName == "TOTAL EXPECTED"
                || columnName == "PAID")
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal amount))
                {
                    e.Value = "GHS " + amount.ToString("N2");
                    e.FormattingApplied = true;
                }
                return;
            }
            if (columnName == "PAYMENT DATE" || columnName == "DATE" || columnName == "TERM START" || columnName == "TERM END")
            {
                if (DateTime.TryParse(e.Value.ToString(), out DateTime dateValue))
                {
                    e.Value = dateValue.ToString("dd/MM/yyyy");
                    e.FormattingApplied = true;
                }
                return;
            }
            if (columnName == "PAYMENT TIME")
            {
                if (e.Value is TimeSpan timeValue)
                {
                    e.Value = timeValue.ToString(@"hh\:mm");
                    e.FormattingApplied = true;
                }
                else if (DateTime.TryParse(e.Value.ToString(), out DateTime dateTimeValue))
                {
                    e.Value = dateTimeValue.ToString("HH:mm");
                    e.FormattingApplied = true;
                }
            }
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = grid.Columns[e.ColumnIndex].Name;

            if ((columnName == "PAYMENT MODE" || columnName == "Transaction Type") && e.Value != null && e.Value != DBNull.Value)
            {
                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                string mode = e.Value.ToString().Trim();
                Color pillBg = Color.FromArgb(209, 250, 229);
                Color pillText = Color.FromArgb(6, 95, 70);

                if (mode.Equals("CHARGE", StringComparison.OrdinalIgnoreCase) || mode.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) || mode.Contains("CHARGE"))
                {
                    pillBg = Color.FromArgb(210, 151, 35);
                    pillText = Color.FromArgb(180, 83, 9);
                }
                else if (mode.Equals("ADJUSTMENT", StringComparison.OrdinalIgnoreCase) || mode.Equals("Adjustment", StringComparison.OrdinalIgnoreCase))
                {
                    pillBg = Color.FromArgb(224, 242, 254);
                    pillText = Color.FromArgb(3, 105, 161);
                }

                int pillWidth = Math.Min(e.CellBounds.Width - 16, TextRenderer.MeasureText(mode, grid.Font).Width + 24);
                int pillHeight = 24;
                var pillRect = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - pillWidth) / 2, e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2, pillWidth, pillHeight);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = GetRoundedRectPath(pillRect, 12))
                using (var brush = new SolidBrush(pillBg))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, mode, new Font(grid.Font.FontFamily, 8.5F, FontStyle.Bold), pillRect, pillText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private static decimal GetFirstDecimal(DataTable table, string column)
        {
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains(column)) return 0m;
            return decimal.TryParse(table.Rows[0][column]?.ToString(), out decimal value) ? value : 0m;
        }

        private static string FormatCurrency(decimal amount)
        {
            return "GHS " + amount.ToString("N2");
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private static void SetCueBanner(TextBox textBox, string text)
        {
            if (textBox == null) return;
            if (textBox.IsHandleCreated)
            {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, 1, text);
            }
            textBox.HandleCreated += (s, e) => SendMessage(textBox.Handle, EM_SETCUEBANNER, 1, text);
        }

        private class CircleBadge : Panel
        {
            private Color _circleColor;
            private string _text;
            private Font _textFont;
            private Color _textColor;

            public Color CircleColor
            {
                get => _circleColor;
                set { _circleColor = value; Invalidate(); }
            }
            public string BadgeText
            {
                get => _text;
                set { _text = value; Invalidate(); }
            }
            public Font BadgeFont
            {
                get => _textFont;
                set { _textFont = value; Invalidate(); }
            }
            public Color BadgeTextColor
            {
                get => _textColor;
                set { _textColor = value; Invalidate(); }
            }

            public CircleBadge(Color circleColor, string text, Font font, Color textColor)
            {
                _circleColor = circleColor;
                _text = text;
                _textFont = font;
                _textColor = textColor;
                DoubleBuffered = true;
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var brush = new SolidBrush(_circleColor))
                {
                    e.Graphics.FillEllipse(brush, r);
                }
                if (!string.IsNullOrEmpty(_text))
                {
                    TextRenderer.DrawText(e.Graphics, _text, _textFont, r, _textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private class RoundedPanel : Panel
        {
            private readonly int _radius;
            private readonly Color _fillColor;
            private readonly Color _borderColor;

            public RoundedPanel(int radius, Color fillColor, Color borderColor)
            {
                _radius = radius;
                _fillColor = fillColor;
                _borderColor = borderColor;
                DoubleBuffered = true;
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = GetRoundedRectPath(rect, _radius))
                using (var brush = new SolidBrush(_fillColor))
                using (var pen = new Pen(_borderColor, 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }
}
