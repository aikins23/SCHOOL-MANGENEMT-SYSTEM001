using System;
using System.Text.RegularExpressions;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Centralized input validation helper for common data types and patterns.
    /// Provides reusable validation methods for forms and services.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that a string is not null or empty.
        /// </summary>
        public static bool IsNotEmpty(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates phone number format (basic validation: digits and common separators only).
        /// </summary>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Allow digits, spaces, hyphens, parentheses, and plus sign
            string pattern = @"^[\d\s\-\(\)\+]+$";
            return Regex.IsMatch(phoneNumber, pattern) && phoneNumber.Length >= 7;
        }

        /// <summary>
        /// Validates that a string contains only numeric digits.
        /// </summary>
        public static bool IsNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Regex.IsMatch(value, @"^\d+$");
        }

        /// <summary>
        /// Validates that a string is a valid positive decimal (for fees, amounts, etc.).
        /// </summary>
        public static bool IsValidAmount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return decimal.TryParse(value, out decimal result) && result > 0;
        }

        /// <summary>
        /// Validates that a string is a valid date.
        /// </summary>
        public static bool IsValidDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return DateTime.TryParse(value, out DateTime result);
        }

        /// <summary>
        /// Validates that a date is not in the future (for birth dates, admission dates, etc.).
        /// </summary>
        public static bool IsNotFutureDate(string value)
        {
            if (!IsValidDate(value))
                return false;

            return DateTime.TryParse(value, out DateTime result) && result <= DateTime.Now;
        }

        /// <summary>
        /// Validates that a string meets minimum length requirement.
        /// </summary>
        public static bool HasMinimumLength(string value, int minLength)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length >= minLength;
        }

        /// <summary>
        /// Validates that a string does not exceed maximum length.
        /// </summary>
        public static bool IsWithinMaxLength(string value, int maxLength)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length <= maxLength;
        }

        /// <summary>
        /// Validates username format (alphanumeric and underscores only, 3-20 characters).
        /// </summary>
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            // Allow only alphanumeric and underscore, 3-20 characters
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,20}$");
        }

        /// <summary>
        /// Validates password strength (minimum 8 chars, at least one uppercase, one lowercase, one digit).
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            bool hasUpperCase = Regex.IsMatch(password, @"[A-Z]");
            bool hasLowerCase = Regex.IsMatch(password, @"[a-z]");
            bool hasDigit = Regex.IsMatch(password, @"\d");

            return hasUpperCase && hasLowerCase && hasDigit;
        }

        /// <summary>
        /// Validates student ID format (typically numeric).
        /// </summary>
        public static bool IsValidStudentId(string studentId)
        {
            return IsNumeric(studentId) && HasMinimumLength(studentId, 3);
        }

        /// <summary>
        /// Validates employee ID format (typically numeric).
        /// </summary>
        public static bool IsValidEmployeeId(string employeeId)
        {
            return IsNumeric(employeeId) && HasMinimumLength(employeeId, 3);
        }

        /// <summary>
        /// Validates name field (letters, spaces, and common punctuation only).
        /// </summary>
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Allow letters, spaces, hyphens, and apostrophes
            return Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$") && HasMinimumLength(name, 2);
        }
    }
}
