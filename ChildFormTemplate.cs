using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Template for creating draggable child forms.
    /// 
    /// Usage:
    /// 1. Inherit from Form (or DraggableForm for auto-drag support)
    /// 2. Add this code to your form's Load event or constructor
    /// 3. Forms will automatically be draggable and independently closable
    /// 
    /// Example:
    /// public partial class MyChildForm : Form
    /// {
    ///     public MyChildForm()
    ///     {
    ///         InitializeComponent();
    ///         EnableFormDragging();  // Add this line
    ///     }
    /// }
    /// </summary>
    public partial class ChildFormTemplate : Form
    {
        public ChildFormTemplate()
        {
            InitializeComponent();
            EnableFormDragging();
        }

        /// <summary>
        /// Enables dragging for this form using mouse events
        /// Call this in your child form constructor or Load event
        /// </summary>
        protected void EnableFormDragging()
        {
            // Track drag state
            bool isDragging = false;
            Point dragStartPoint = Point.Empty;
            Point formStartPoint = Point.Empty;

            // Handle mouse down
            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStartPoint = e.Location;
                    formStartPoint = this.Location;
                }
            };

            // Handle mouse move
            this.MouseMove += (s, e) =>
            {
                if (isDragging && e.Button == MouseButtons.Left)
                {
                    int deltaX = e.X - dragStartPoint.X;
                    int deltaY = e.Y - dragStartPoint.Y;
                    this.Location = new Point(formStartPoint.X + deltaX, formStartPoint.Y + deltaY);
                }
            };

            // Handle mouse up
            this.MouseUp += (s, e) =>
            {
                isDragging = false;
            };
        }

        /// <summary>
        /// Safely closes the form
        /// </summary>
        protected void SafeClose()
        {
            try
            {
                this.Close();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
        }

        /// <summary>
        /// Shows the form with proper focus
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
