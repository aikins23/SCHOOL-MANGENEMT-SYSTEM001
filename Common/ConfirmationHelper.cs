using System;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Provides confirmation dialog utilities for destructive operations
    /// </summary>
    public static class ConfirmationHelper
    {
        private static readonly string AppName = "Kingdom Prep";

        /// <summary>
        /// Ask for confirmation before deleting a record
        /// </summary>
        public static bool ConfirmDelete(string recordType, string recordDetails)
        {
            string message = $"Are you sure you want to delete this {recordType}?\n\n" +
                            $"{recordDetails}\n\n" +
                            "This action cannot be undone.";

            DialogResult result = MessageBox.Show(
                message,
                $"Delete {recordType}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2  // Default to "No"
            );

            bool confirmed = result == DialogResult.Yes;

            if (confirmed)
                LoggerHelper.LogInfo($"User confirmed deletion of {recordType}: {recordDetails}");
            else
                LoggerHelper.LogInfo($"User cancelled deletion of {recordType}");

            return confirmed;
        }

        /// <summary>
        /// Ask for confirmation before major changes
        /// </summary>
        public static bool ConfirmSave(string changeDescription)
        {
            string message = $"Save changes?\n\n{changeDescription}";

            DialogResult result = MessageBox.Show(
                message,
                "Confirm Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1  // Default to "Yes"
            );

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Ask for confirmation before bulk operations
        /// </summary>
        public static bool ConfirmBulkOperation(string operationType, int recordCount)
        {
            string message = $"This will {operationType} {recordCount} record(s).\n\n" +
                            "This action cannot be undone.\n\n" +
                            "Continue?";

            DialogResult result = MessageBox.Show(
                message,
                $"Confirm {operationType}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2  // Default to "No"
            );

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Show confirmation for administrative actions
        /// </summary>
        public static bool ConfirmAdminAction(string action, string details)
        {
            string message = $"Administrator Action\n\n" +
                            $"Action: {action}\n" +
                            $"Details: {details}\n\n" +
                            "Confirm this action?";

            DialogResult result = MessageBox.Show(
                message,
                "Confirm Action",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (result == DialogResult.Yes)
                LoggerHelper.LogInfo($"Admin action confirmed: {action} - {details}");

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Show info message
        /// </summary>
        public static void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Show warning message
        /// </summary>
        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
