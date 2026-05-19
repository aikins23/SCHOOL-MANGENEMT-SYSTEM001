using System;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Custom exception for validation errors.
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Custom exception for database/repository errors.
    /// </summary>
    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }
        public DataAccessException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// Custom exception for business logic errors.
    /// </summary>
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message) { }
        public BusinessLogicException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// Centralized error handling utility for consistent error management across the application.
    /// Provides methods for handling different types of errors and displaying user-friendly messages.
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handles and displays error messages to the user.
        /// </summary>
        public static void HandleError(string title, string message, Exception ex = null)
        {
            try
            {
                // Log the error (console for now, could be extended to file/database)
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}: {message}";
                if (ex != null)
                {
                    logMessage += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
                }
                System.Diagnostics.Debug.WriteLine(logMessage);

                // Show user-friendly error dialog
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch
            {
                // Fallback if error handling itself fails
                MessageBox.Show(
                    "An unexpected error occurred.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles validation errors with user-friendly messaging.
        /// </summary>
        public static void HandleValidationError(string fieldName, string message)
        {
            string fullMessage = $"Invalid {fieldName}: {message}";
            HandleError("Validation Error", fullMessage);
        }

        /// <summary>
        /// Handles database/data access errors.
        /// </summary>
        public static void HandleDataAccessError(string operation, Exception ex = null)
        {
            string message = $"Unable to {operation}. Please check your connection and try again.";
            HandleError("Database Error", message, ex);
        }

        /// <summary>
        /// Handles business logic errors.
        /// </summary>
        public static void HandleBusinessLogicError(string message)
        {
            HandleError("Operation Error", message);
        }

        /// <summary>
        /// Handles authentication errors.
        /// </summary>
        public static void HandleAuthenticationError()
        {
            HandleError(
                "Authentication Failed",
                "Invalid username or password. Please try again."
            );
        }

        /// <summary>
        /// Handles authorization errors.
        /// </summary>
        public static void HandleAuthorizationError()
        {
            HandleError(
                "Access Denied",
                "You do not have permission to perform this action."
            );
        }

        /// <summary>
        /// Displays an informational message to the user.
        /// </summary>
        public static void ShowInfo(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Displays a warning message to the user.
        /// </summary>
        public static void ShowWarning(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Displays a confirmation dialog and returns user's choice.
        /// </summary>
        public static bool ShowConfirmation(string title, string message)
        {
            DialogResult result = MessageBox.Show(
                message,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Safely executes an action with error handling.
        /// </summary>
        public static void TryExecute(Action action, string errorTitle = "Error")
        {
            try
            {
                action?.Invoke();
            }
            catch (ValidationException ex)
            {
                HandleError(errorTitle, ex.Message);
            }
            catch (DataAccessException ex)
            {
                HandleError("Database Error", ex.Message, ex.InnerException);
            }
            catch (BusinessLogicException ex)
            {
                HandleError(errorTitle, ex.Message);
            }
            catch (Exception ex)
            {
                HandleError("Unexpected Error", $"An unexpected error occurred: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Safely executes a function with error handling and returns a result.
        /// </summary>
        public static T TryExecute<T>(Func<T> function, T defaultValue = default, 
            string errorTitle = "Error") where T : class
        {
            try
            {
                return function?.Invoke();
            }
            catch (ValidationException ex)
            {
                HandleError(errorTitle, ex.Message);
                return defaultValue;
            }
            catch (DataAccessException ex)
            {
                HandleError("Database Error", ex.Message, ex.InnerException);
                return defaultValue;
            }
            catch (BusinessLogicException ex)
            {
                HandleError(errorTitle, ex.Message);
                return defaultValue;
            }
            catch (Exception ex)
            {
                HandleError("Unexpected Error", $"An unexpected error occurred: {ex.Message}", ex);
                return defaultValue;
            }
        }
    }
}
