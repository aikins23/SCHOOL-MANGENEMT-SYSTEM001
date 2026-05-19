using System;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Base form class that enables forms to be dragged by their title bar
    /// Provides drag functionality for all child forms without built-in Windows title bar
    /// </summary>
    public partial class DraggableForm : Form
    {
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point formStartPoint;

        public DraggableForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EnableDragging();
        }

        /// <summary>
        /// Enables drag functionality for the form
        /// </summary>
        protected virtual void EnableDragging()
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
                    dragStartPoint = new Point(e.X, e.Y);
                    formStartPoint = this.Location;
                }
            };

            control.MouseMove += (s, e) =>
            {
                if (isDragging && e.Button == MouseButtons.Left)
                {
                    // Calculate the distance moved
                    int deltaX = e.X - dragStartPoint.X;
                    int deltaY = e.Y - dragStartPoint.Y;

                    // Calculate new form position
                    int newX = formStartPoint.X + deltaX;
                    int newY = formStartPoint.Y + deltaY;

                    // Keep form within screen bounds
                    Screen screen = Screen.FromPoint(this.Location);
                    Rectangle workingArea = screen.WorkingArea;

                    if (newX < workingArea.Left)
                        newX = workingArea.Left;
                    if (newY < workingArea.Top)
                        newY = workingArea.Top;
                    if (newX + this.Width > workingArea.Right)
                        newX = workingArea.Right - this.Width;
                    if (newY + this.Height > workingArea.Bottom)
                        newY = workingArea.Bottom - this.Height;

                    this.Location = new Point(newX, newY);
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
        /// Checks if a control should have drag disabled
        /// </summary>
        private bool IsInteractiveControl(Control control)
        {
            Type controlType = control.GetType();

            // Don't allow dragging from interactive controls
            return controlType == typeof(TextBox) ||
                   controlType == typeof(ComboBox) ||
                   controlType == typeof(Button) ||
                   controlType == typeof(CheckBox) ||
                   controlType == typeof(RadioButton) ||
                   controlType == typeof(DataGridView) ||
                   controlType == typeof(ListBox) ||
                   controlType == typeof(TreeView) ||
                   controlType == typeof(RichTextBox) ||
                   controlType.Name.Contains("NumericUpDown") ||
                   controlType.Name.Contains("DateTimePicker");
        }

        /// <summary>
        /// Safely closes the form
        /// </summary>
        public new void Close()
        {
            try
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (ObjectDisposedException)
            {
                // Form already disposed
            }
        }

        /// <summary>
        /// Hides the form instead of closing it (for persistent windows like dashboard)
        /// </summary>
        public void HideForm()
        {
            this.Hide();
        }

        /// <summary>
        /// Shows the form and brings it to front
        /// </summary>
        public new void Show()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => base.Show()));
            }
            else
            {
                base.Show();
                this.BringToFront();
                this.Focus();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}
