using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    internal static class UiTheme
    {
        private static readonly Font BaseFont = new Font("Segoe UI", 9.25F, FontStyle.Regular);
        private static readonly Font ButtonFont = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold);
        private const int SB_BOTH = 3;
        public static bool IsDarkMode { get; set; } = false;

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        public static Color Page => IsDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(244, 246, 248); // Soft Mist Grey
        public static Color Surface => IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.White;
        public static Color SurfaceAlt => IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(244, 246, 248);
        public static Color Border => IsDarkMode ? Color.FromArgb(71, 85, 105) : Color.FromArgb(226, 232, 240); // Solid opaque light slate
        public static Color Text => IsDarkMode ? Color.FromArgb(248, 250, 252) : Color.FromArgb(0, 24, 74); // Deep Navy
        public static Color Muted => IsDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(153, 0, 24, 74); // 60% Navy
        public static Color Navy => IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.FromArgb(0, 24, 74); // Deep Navy
        public static Color NavyHover => IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(0, 16, 51); // Darkened Navy
        public static Color NavyDark => IsDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(0, 8, 26);
        public static Color NavySoft => IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(0, 24, 74);
        public static Color Gold => IsDarkMode ? Color.FromArgb(250, 204, 21) : Color.FromArgb(210, 151, 35); // Rich Gold
        public static Color GoldSoft => IsDarkMode ? Color.FromArgb(113, 63, 18) : Color.FromArgb(210, 151, 35);
        public static Color Ink => IsDarkMode ? Color.FromArgb(248, 250, 252) : Color.FromArgb(0, 24, 74);
        public static Color MutedInk => IsDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(153, 0, 24, 74);
        public static Color Success => IsDarkMode ? Color.FromArgb(16, 185, 129) : Color.FromArgb(16, 185, 129);
        public static Color Danger => IsDarkMode ? Color.FromArgb(244, 63, 94) : Color.FromArgb(225, 29, 72);
        public static Color SuccessSoft => IsDarkMode ? Color.FromArgb(6, 78, 59) : Color.FromArgb(236, 253, 245);
        public static Color SuccessText => IsDarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(6, 95, 70);
        public static Color DangerSoft => IsDarkMode ? Color.FromArgb(136, 19, 55) : Color.FromArgb(255, 241, 242);
        public static Color WarningText => IsDarkMode ? Color.FromArgb(251, 191, 36) : Color.FromArgb(146, 64, 14);
        public static Color DisabledBack => IsDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);
        public static Color DisabledText => IsDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);

        public static void Apply(Form form)
        {
            if (form == null) return;

            form.BackColor = Page;
            form.Font = BaseFont;
            form.Icon = Common.Branding.AppIcon;

            // Ensure responsive min size unless specifically small (dialogs)
            if (form.FormBorderStyle != FormBorderStyle.FixedDialog && form.MaximizeBox)
            {
                form.MinimumSize = new Size(Math.Max(form.MinimumSize.Width, 1000), Math.Max(form.MinimumSize.Height, 650));
            }

            ApplyToControls(form.Controls, false);
        }

        public static void StyleDataGrid(DataGridView grid, bool fillColumns = false)
        {
            StyleGrid(grid, fillColumns);
        }

        private static void ApplyToControls(Control.ControlCollection controls, bool inNavigationArea)
        {
            foreach (Control control in controls)
            {
                bool navigationArea = inNavigationArea || IsNavigationContainer(control);

                control.Font = BaseFont;

                if (control is Panel || IsType(control, "GunaPanel") || IsType(control, "Guna2Panel"))
                {
                    control.BackColor = navigationArea ? Navy : Surface;
                }
                else if (control is GroupBox || IsType(control, "GunaGroupBox") || IsType(control, "Guna2GroupBox"))
                {
                    control.BackColor = Surface;
                    control.ForeColor = Text;
                }
                else if (control is Label)
                {
                    control.ForeColor = navigationArea ? Color.White : Text;
                }
                else if (control is TextBox || control is ComboBox || control is DateTimePicker || IsInputControl(control))
                {
                    control.BackColor = Surface;
                    control.ForeColor = Text;
                    SetIfExists(control, "BorderColor", Border);
                    SetIfExists(control, "FocusedBorderColor", Gold);
                    SetIfExists(control, "FillColor", Surface);
                    SetIfExists(control, "FocusedState.BorderColor", Gold);
                }
                else if (control is Button || IsButtonControl(control))
                {
                    StyleButton(control, navigationArea);
                }
                else if (control is DataGridView grid)
                {
                    StyleGrid(grid, false);
                    AttachGunaScrollbar(grid, navigationArea);
                }
                else if (control is MenuStrip menu)
                {
                    StyleMenu(menu);
                }

                if ((control is Panel || control is FlowLayoutPanel || control is TableLayoutPanel) && ((ScrollableControl)control).AutoScroll)
                {
                    AttachGunaScrollbar(control, navigationArea);
                }

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls, navigationArea);
                }
            }
        }

        private static void StyleButton(Control control, bool navigationArea)
        {
            control.Font = ButtonFont;
            control.ForeColor = Color.White;
            control.BackColor = Navy;

            SetIfExists(control, "BaseColor", Navy);
            SetIfExists(control, "OnHoverBaseColor", navigationArea ? NavyHover : Gold);
            SetIfExists(control, "OnHoverBorderColor", navigationArea ? Gold : Gold);
            SetIfExists(control, "OnHoverForeColor", Color.White);
            SetIfExists(control, "BorderColor", Navy);
            SetIfExists(control, "BorderRadius", 6);
            SetIfExists(control, "BorderThickness", 1);
            SetIfExists(control, "FillColor", Navy);
            SetIfExists(control, "HoverState.FillColor", Gold);
        }

        private static void StyleGrid(DataGridView grid, bool fillColumns)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Border;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = fillColumns ? DataGridViewAutoSizeColumnsMode.Fill : DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ScrollBars = fillColumns ? ScrollBars.Vertical : ScrollBars.Both;
            grid.ColumnHeadersHeight = 40;
            grid.RowTemplate.Height = 38;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = ButtonFont;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.Font = BaseFont;
            grid.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            grid.DefaultCellStyle.SelectionBackColor = GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;
            grid.CellFormatting -= Grid_TwoDecimalCellFormatting;
            grid.CellFormatting += Grid_TwoDecimalCellFormatting;
            grid.DataBindingComplete -= Grid_DataBindingComplete;
            grid.DataBindingComplete += Grid_DataBindingComplete;
        }

        private static void Grid_TwoDecimalCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null || e.Value == DBNull.Value)
            {
                return;
            }

            var grid = sender as DataGridView;
            if (grid == null || e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count)
            {
                return;
            }

            if (!ShouldFormatAsDecimal(grid.Columns[e.ColumnIndex].Name, grid.Columns[e.ColumnIndex].HeaderText))
            {
                return;
            }

            decimal value;
            if (decimal.TryParse(Convert.ToString(e.Value), NumberStyles.Any, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(Convert.ToString(e.Value), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                e.Value = value.ToString("0.00", CultureInfo.CurrentCulture);
                e.FormattingApplied = true;
            }
        }

        private static bool ShouldFormatAsDecimal(string columnName, string headerText)
        {
            string key = ((columnName ?? "") + " " + (headerText ?? "")).ToUpperInvariant();
            if (key.Contains("ID") || key.Contains("YEAR") || key.Contains("DATE")
                || key.Contains("PHONE") || key.Contains("CONTACT") || key.Contains("COUNT")
                || key.Contains("RANK") || key.Contains("POSITION") || key.Contains("ORDER"))
            {
                return false;
            }

            return key.Contains("AMOUNT") || key.Contains("BALANCE") || key.Contains("PAID")
                || key.Contains("FEE") || key.Contains("TOTAL") || key.Contains("SCORE")
                || key.Contains("AVERAGE") || key.Contains("PERCENT") || key.Contains("EXPENSE")
                || key.Contains("INCOME") || key.Contains("FUND") || key.Contains("SALARY")
                || key.Contains("WEIGHT");
        }

        private static void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is DataGridView grid)
            {
                try
                {
                    if (grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill)
                    {
                        return;
                    }

                    grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    foreach (DataGridViewColumn column in grid.Columns)
                    {
                        if (column.Width < 90)
                        {
                            column.Width = 90;
                        }
                        if (column.Width > 280)
                        {
                            column.Width = 280;
                        }
                    }
                }
                catch
                {
                    // Some grids are mid-bind while third-party controls apply their own theme.
                }
            }
        }

        private static void StyleMenu(MenuStrip menu)
        {
            menu.BackColor = Navy;
            menu.ForeColor = Color.White;
            menu.Font = ButtonFont;
        }

        private static bool IsNavigationContainer(Control control)
        {
            string name = control.Name.ToLowerInvariant();
            return name.Contains("menu") || name.Contains("side") || name.Contains("nav") || name.Contains("left");
        }

        private static bool IsButtonControl(Control control)
        {
            string typeName = control.GetType().Name;
            return typeName.Contains("Button");
        }

        private static bool IsInputControl(Control control)
        {
            string typeName = control.GetType().Name;
            return typeName.Contains("TextBox") || typeName.Contains("ComboBox") || typeName.Contains("DateTimePicker");
        }

        private static bool IsType(Control control, string typeName)
        {
            return control.GetType().Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetIfExists(object target, string propertyName, object value)
        {
            try
            {
                string[] parts = propertyName.Split('.');
                object current = target;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    PropertyInfo nested = current.GetType().GetProperty(parts[i]);
                    if (nested == null)
                    {
                        return;
                    }

                    current = nested.GetValue(current, null);
                    if (current == null)
                    {
                        return;
                    }
                }

                PropertyInfo property = current.GetType().GetProperty(parts[parts.Length - 1]);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(current, value, null);
                }
            }
            catch
            {
                // Some third-party controls expose read-only style objects.
            }
        }

        public static void AttachModernScrollbar(Control target, bool isNavigationArea = false)
        {
            AttachGunaScrollbar(target, isNavigationArea);
        }

        public static void HideNativeScrollbarsFor(Control target)
        {
            KeepNativeScrollbarsHidden(target);
        }

        private static void AttachGunaScrollbar(Control target, bool isNavigationArea)
        {
            if (target == null || target.Parent == null) return;

            // Check if one already exists for this target
            foreach (Control c in target.Parent.Controls)
            {
                if (c.GetType().Name == "Guna2VScrollBar" && c.Tag == target)
                {
                    return;
                }
            }

            try
            {
                var scrollbar = new Guna.UI2.WinForms.Guna2VScrollBar
                {
                    Tag = target,
                    Dock = DockStyle.Right,
                    Width = 1,
                    FillColor = Color.Transparent,
                    ThumbColor = Color.Transparent,
                    BorderRadius = 4,
                    ThumbSize = 1f
                };

                target.Parent.Controls.Add(scrollbar);
                scrollbar.BringToFront();
                KeepNativeScrollbarsHidden(target);

                if (target is DataGridView grid)
                {
                    var helper = new Guna.UI2.WinForms.Helpers.DataGridViewScrollHelper(grid, scrollbar, true);
                    helper.UpdateScrollBar();
                }
                else if (target is Panel panel)
                {
                    var helper = new Guna.UI2.WinForms.Helpers.PanelScrollHelper(panel, scrollbar, true);
                    helper.UpdateScrollBar();
                }
            }
            catch { }
        }

        private static void KeepNativeScrollbarsHidden(Control target)
        {
            HideNativeScrollbars(target);

            target.HandleCreated += (sender, args) => HideNativeScrollbars(target);
            target.Resize += (sender, args) => HideNativeScrollbars(target);
            target.VisibleChanged += (sender, args) => HideNativeScrollbars(target);

            if (target is ScrollableControl scrollable)
            {
                scrollable.Scroll += (sender, args) => HideNativeScrollbars(target);
            }

            if (target is DataGridView grid)
            {
                grid.Scroll += (sender, args) => HideNativeScrollbars(target);
                grid.DataBindingComplete += (sender, args) => HideNativeScrollbars(target);
            }
        }

        private static void HideNativeScrollbars(Control target)
        {
            if (target == null || target.IsDisposed)
            {
                return;
            }

            if (!target.IsHandleCreated)
            {
                return;
            }

            try
            {
                ShowScrollBar(target.Handle, SB_BOTH, false);
                target.BeginInvoke((MethodInvoker)(() =>
                {
                    if (!target.IsDisposed && target.IsHandleCreated)
                    {
                        ShowScrollBar(target.Handle, SB_BOTH, false);
                    }
                }));
            }
            catch { }
        }
    }
}
