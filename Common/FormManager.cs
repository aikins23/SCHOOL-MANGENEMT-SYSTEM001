using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Manages the lifecycle and visibility of forms in the application
    /// Keeps track of open forms, prevents duplicate forms, and handles clean closure
    /// </summary>
    public static class FormManager
    {
        private static Dictionary<string, Form> _openForms = new Dictionary<string, Form>();
        private static Form _mainDashboard;
        private static bool _isClosingAllForms = false;

        /// <summary>
        /// Sets the main dashboard form that should remain active
        /// </summary>
        public static void SetMainDashboard(Form dashboard)
        {
            _mainDashboard = dashboard;
        }

        /// <summary>
        /// Gets the main dashboard form
        /// </summary>
        public static Form GetMainDashboard()
        {
            return _mainDashboard;
        }

        /// <summary>
        /// Opens or shows an existing form of the specified type
        /// If a form of this type is already open, it brings it to front instead of creating a new one
        /// </summary>
        public static T OpenForm<T>(Func<T> formFactory) where T : Form
        {
            Type formType = typeof(T);
            string cacheKey = GetCacheKey(formType);

            // Check if form already exists and is not disposed
            if (_openForms.ContainsKey(cacheKey) && _openForms[cacheKey] != null && !_openForms[cacheKey].IsDisposed)
            {
                Form existingForm = _openForms[cacheKey];
                existingForm.Show();
                existingForm.BringToFront();
                existingForm.Focus();
                return (T)existingForm;
            }

            // Create new form instance
            T form = formFactory();
            _openForms[cacheKey] = form;

            // Subscribe to form closed event
            form.FormClosed += (s, e) => OnFormClosed(cacheKey);

            form.Show();
            form.BringToFront();
            form.Focus();

            return form;
        }

        /// <summary>
        /// Opens or shows an existing form with an ID key (for forms that can have multiple instances)
        /// </summary>
        public static T OpenForm<T>(string key, Func<T> formFactory) where T : Form
        {
            string cacheKey = GetCacheKey(typeof(T), key);

            if (_openForms.ContainsKey(cacheKey) && _openForms[cacheKey] != null && !_openForms[cacheKey].IsDisposed)
            {
                Form existingForm = _openForms[cacheKey];
                existingForm.Show();
                existingForm.BringToFront();
                existingForm.Focus();
                return (T)existingForm;
            }

            T form = formFactory();
            _openForms[cacheKey] = form;

            form.FormClosed += (s, e) => OnFormClosed(cacheKey);
            form.Show();
            form.BringToFront();
            form.Focus();

            return form;
        }

        /// <summary>
        /// Closes a specific form type
        /// </summary>
        public static void CloseForm<T>() where T : Form
        {
            Type formType = typeof(T);
            string cacheKey = GetCacheKey(formType);
            if (_openForms.ContainsKey(cacheKey) && _openForms[cacheKey] != null)
            {
                if (!_openForms[cacheKey].IsDisposed)
                {
                    _openForms[cacheKey].Close();
                }
            }
        }

        /// <summary>
        /// Closes all child forms but keeps the main dashboard open
        /// </summary>
        public static void CloseAllChildForms()
        {
            var formsToClose = _openForms.Values
                .Where(f => f != null && !f.IsDisposed && f != _mainDashboard)
                .ToList();

            foreach (var form in formsToClose)
            {
                try
                {
                    form.Close();
                }
                catch
                {
                    // Form already closed or disposed
                }
            }
        }

        /// <summary>
        /// Closes all forms including the dashboard
        /// </summary>
        public static void CloseAllForms()
        {
            if (_isClosingAllForms) return;

            _isClosingAllForms = true;
            try
            {
                var formsToClose = _openForms.Values.Where(f => f != null && !f.IsDisposed).ToList();

                foreach (var form in formsToClose)
                {
                    try
                    {
                        form.Close();
                    }
                    catch
                    {
                        // Form already closed or disposed
                    }
                }

                _openForms.Clear();
            }
            finally
            {
                _isClosingAllForms = false;
            }
        }

        /// <summary>
        /// Shows the main dashboard
        /// </summary>
        public static void ShowDashboard()
        {
            if (_mainDashboard != null && !_mainDashboard.IsDisposed)
            {
                _mainDashboard.Show();
                _mainDashboard.BringToFront();
                _mainDashboard.Focus();
            }
        }

        /// <summary>
        /// Navigates back to the current user's dashboard. Reuses the registered
        /// main dashboard when it's still alive; otherwise creates the one that
        /// matches the logged-in role (teachers get frmTeacherDashboard, everyone
        /// else gets frmDashboard). This avoids opening the admin dashboard for a
        /// teacher, which would trigger a permission-denied dialog.
        /// </summary>
        public static void GoToDashboard()
        {
            if (_mainDashboard != null && !_mainDashboard.IsDisposed)
            {
                _mainDashboard.Show();
                _mainDashboard.BringToFront();
                _mainDashboard.Focus();
                return;
            }

            Form dash = Services.AuthService.CurrentUser.Role == Services.AuthService.UserRole.Teacher
                ? (Form)new frmTeacherDashboard()
                : new frmDashboard();
            dash.Show();
        }

        /// <summary>
        /// Hides all child forms (useful for dashboard-only view)
        /// </summary>
        public static void HideAllChildForms()
        {
            var childForms = _openForms.Values
                .Where(f => f != null && !f.IsDisposed && f != _mainDashboard && f.Visible)
                .ToList();

            foreach (var form in childForms)
            {
                form.Hide();
            }
        }

        /// <summary>
        /// Gets count of open child forms
        /// </summary>
        public static int GetOpenChildFormCount()
        {
            return _openForms.Values
                .Count(f => f != null && !f.IsDisposed && f != _mainDashboard && f.Visible);
        }

        /// <summary>
        /// Gets list of all open form names
        /// </summary>
        public static List<string> GetOpenFormNames()
        {
            return _openForms
                .Where(kvp => kvp.Value != null && !kvp.Value.IsDisposed && kvp.Value.Visible)
                .Select(kvp => kvp.Value.Text)
                .ToList();
        }

        /// <summary>
        /// Checks if a form of the specified type is currently open
        /// </summary>
        public static bool IsFormOpen<T>() where T : Form
        {
            Type formType = typeof(T);
            string cacheKey = GetCacheKey(formType);
            return _openForms.ContainsKey(cacheKey) &&
                   _openForms[cacheKey] != null &&
                   !_openForms[cacheKey].IsDisposed &&
                   _openForms[cacheKey].Visible;
        }

        /// <summary>
        /// Gets a specific open form of the given type
        /// </summary>
        public static T GetOpenForm<T>() where T : Form
        {
            Type formType = typeof(T);
            string cacheKey = GetCacheKey(formType);
            if (_openForms.ContainsKey(cacheKey) && _openForms[cacheKey] != null && !_openForms[cacheKey].IsDisposed)
            {
                return (T)_openForms[cacheKey];
            }
            return null;
        }

        /// <summary>
        /// Shows a form and hides the source form.
        /// </summary>
        public static void ShowForm<T>(Form source) where T : Form, new()
        {
            T target = OpenForm(() => new T());
            if (source != null && source != target)
            {
                source.Hide();
            }
        }

        /// <summary>
        /// Called when a form is closed
        /// </summary>
        private static void OnFormClosed(string cacheKey)
        {
            if (_openForms.ContainsKey(cacheKey))
            {
                _openForms[cacheKey] = null;
            }
        }

        /// <summary>
        /// Clears all closed/disposed form references
        /// </summary>
        public static void CleanupClosedForms()
        {
            var closedForms = _openForms.Where(kvp => kvp.Value == null || kvp.Value.IsDisposed).Select(kvp => kvp.Key).ToList();
            foreach (var formType in closedForms)
            {
                _openForms.Remove(formType);
            }
        }

        private static string GetCacheKey(Type formType, string key = null)
        {
            return string.IsNullOrEmpty(key)
                ? formType.FullName
                : formType.FullName + "_" + key;
        }
    }
}
