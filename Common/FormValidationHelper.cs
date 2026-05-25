using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Provides validation utilities and UI feedback for form inputs
    /// </summary>
    public static class FormValidationHelper
    {
        private static readonly Color ErrorColor = Color.FromArgb(255, 200, 200);
        private static readonly Color SuccessColor = Color.White;

        /// <summary>
        /// Validates a required text field and displays error if empty
        /// </summary>
        public static bool ValidateRequired(Control control, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(control.Text))
            {
                ShowFieldError(control, $"{fieldName} is required");
                return false;
            }

            ClearFieldError(control);
            return true;
        }

        /// <summary>
        /// Validates numeric input
        /// </summary>
        public static bool ValidateNumeric(Control control, string fieldName, out decimal value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(control.Text))
            {
                ShowFieldError(control, $"{fieldName} is required");
                return false;
            }

            if (!decimal.TryParse(control.Text, out value))
            {
                ShowFieldError(control, $"{fieldName} must be a valid number");
                return false;
            }

            if (value < 0)
            {
                ShowFieldError(control, $"{fieldName} cannot be negative");
                return false;
            }

            ClearFieldError(control);
            return true;
        }

        /// <summary>
        /// Validates numeric range
        /// </summary>
        public static bool ValidateRange(Control control, string fieldName,
            decimal min, decimal max, out decimal value)
        {
            value = 0;

            if (!ValidateNumeric(control, fieldName, out value))
                return false;

            if (value < min || value > max)
            {
                ShowFieldError(control, $"{fieldName} must be between {min} and {max}");
                return false;
            }

            ClearFieldError(control);
            return true;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public static bool ValidateEmail(Control control)
        {
            if (string.IsNullOrWhiteSpace(control.Text))
            {
                ClearFieldError(control);
                return true;  // Email is optional
            }

            try
            {
                var email = new System.Net.Mail.MailAddress(control.Text);
                ClearFieldError(control);
                return true;
            }
            catch
            {
                ShowFieldError(control, "Invalid email format");
                return false;
            }
        }

        /// <summary>
        /// Validates date picker selection
        /// </summary>
        public static bool ValidateDate(DateTimePicker control, string fieldName)
        {
            if (control.Value == null)
            {
                ShowFieldError(control, $"{fieldName} is required");
                return false;
            }

            if (control.Value > DateTime.Now)
            {
                ShowFieldError(control, $"{fieldName} cannot be in the future");
                return false;
            }

            ClearFieldError(control);
            return true;
        }

        /// <summary>
        /// Validates ComboBox selection
        /// </summary>
        public static bool ValidateComboBox(ComboBox control, string fieldName)
        {
            if (control.SelectedIndex < 0)
            {
                ShowFieldError(control, $"Please select a {fieldName}");
                return false;
            }

            ClearFieldError(control);
            return true;
        }

        /// <summary>
        /// Display error state on control with tooltip
        /// </summary>
        private static void ShowFieldError(Control control, string errorMessage)
        {
            control.BackColor = ErrorColor;

            var tooltip = new ToolTip();
            tooltip.Show(errorMessage, control, 0, control.Height);

            LoggerHelper.LogWarning($"Validation error on {control.Name}: {errorMessage}");
        }

        /// <summary>
        /// Clear error state from control
        /// </summary>
        private static void ClearFieldError(Control control)
        {
            control.BackColor = SuccessColor;
        }

        /// <summary>
        /// Validate all required fields on a form at once
        /// </summary>
        public static bool ValidateForm(Dictionary<Control, string> fieldNames)
        {
            bool allValid = true;

            foreach (var kvp in fieldNames)
            {
                if (!ValidateRequired(kvp.Key, kvp.Value))
                    allValid = false;
            }

            return allValid;
        }
    }
}
