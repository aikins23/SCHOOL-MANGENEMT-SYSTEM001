using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    internal static class UiTheme
    {
        private static readonly Font BaseFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        private static readonly Font ButtonFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        public static readonly Color Page = Color.FromArgb(245, 247, 250);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(238, 242, 247);
        public static readonly Color Border = Color.FromArgb(216, 222, 232);
        public static readonly Color Text = Color.FromArgb(17, 24, 39);
        public static readonly Color Muted = Color.FromArgb(91, 105, 119);
        public static readonly Color Navy = Color.FromArgb(25, 25, 112);
        public static readonly Color NavyHover = Color.FromArgb(18, 18, 86);
        public static readonly Color Gold = Color.FromArgb(255, 215, 0);
        public static readonly Color GoldSoft = Color.FromArgb(255, 248, 204);

        public static void Apply(Form form)
        {
            if (form == null)
            {
                return;
            }

            form.BackColor = Page;
            form.Font = BaseFont;
            form.MinimumSize = new Size(Math.Max(form.MinimumSize.Width, 1000), Math.Max(form.MinimumSize.Height, 650));
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
                }
                else if (control is MenuStrip menu)
                {
                    StyleMenu(menu);
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
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ScrollBars = ScrollBars.Both;
            grid.ColumnHeadersHeight = 42;
            grid.RowTemplate.Height = 34;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = ButtonFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.Font = BaseFont;
            grid.DefaultCellStyle.SelectionBackColor = GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;
            grid.DataBindingComplete -= Grid_DataBindingComplete;
            grid.DataBindingComplete += Grid_DataBindingComplete;
        }

        private static void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is DataGridView grid)
            {
                try
                {
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
    }
}
