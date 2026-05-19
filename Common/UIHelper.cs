using System;
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
    }
}
