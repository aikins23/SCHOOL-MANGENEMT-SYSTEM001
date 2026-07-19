using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Utility class for UI message handling
    /// </summary>
    public static class UIHelper
    {
        public static void ShowSuccess(string message, string title = "Success")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static DialogResult ShowConfirmation(string message, string title = "Confirm")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public static void SetControlError(Control control, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                control.BackColor = System.Drawing.Color.White;
            }
            else
            {
                control.BackColor = System.Drawing.Color.FromArgb(255, 200, 200);
            }
        }

        public static void ClearFormErrors(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                SetControlError(control, "");
                if (control.HasChildren)
                {
                    ClearFormErrors(control);
                }
            }
        }

        public static async Task RunBusyAsync(
            Form owner,
            Label statusLabel,
            string busyText,
            IEnumerable<Control> controlsToDisable,
            Func<Task> action,
            string completedText = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var previousCursor = owner.Cursor;
            var previousStatus = statusLabel == null ? null : statusLabel.Text;
            var disabled = new List<Control>();

            try
            {
                owner.UseWaitCursor = true;
                owner.Cursor = Cursors.WaitCursor;
                Cursor.Current = Cursors.WaitCursor;

                if (statusLabel != null && !string.IsNullOrWhiteSpace(busyText))
                    statusLabel.Text = busyText;

                if (controlsToDisable != null)
                {
                    foreach (var control in controlsToDisable)
                    {
                        if (control == null || !control.Enabled) continue;
                        control.Enabled = false;
                        disabled.Add(control);
                    }
                }

                await action();

                if (statusLabel != null && !string.IsNullOrWhiteSpace(completedText))
                    statusLabel.Text = completedText;
            }
            finally
            {
                foreach (var control in disabled)
                    if (control != null && !control.IsDisposed)
                        control.Enabled = true;

                if (statusLabel != null && string.IsNullOrWhiteSpace(completedText) && previousStatus != null)
                    statusLabel.Text = previousStatus;

                owner.UseWaitCursor = false;
                owner.Cursor = previousCursor;
                Cursor.Current = Cursors.Default;
            }
        }
    }
}
