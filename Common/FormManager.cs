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
        private static Dictionary<Type, Form> _openForms = new Dictionary<Type, Form>();
        private static Form _mainDashboard;

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

            // Check if form already exists and is not disposed
            if (_openForms.ContainsKey(formType) && _openForms[formType] != null && !_openForms[formType].IsDisposed)
            {
                Form existingForm = _openForms[formType];
                existingForm.Show();
                existingForm.BringToFront();
                existingForm.Focus();
                return (T)existingForm;
            }

            // Create new form instance
            T form = formFactory();
            _openForms[formType] = form;

            // Subscribe to form closed event
            form.FormClosed += (s, e) => OnFormClosed(formType);

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
            string cacheKey = typeof(T).FullName + "_" + key;

            if (_openForms.ContainsKey(typeof(T)) && _openForms[typeof(T)] != null && !_openForms[typeof(T)].IsDisposed)
            {
                Form existingForm = _openForms[typeof(T)];
                existingForm.Show();
                existingForm.BringToFront();
                existingForm.Focus();
                return (T)existingForm;
            }

            T form = formFactory();
            _openForms[typeof(T)] = form;

            form.FormClosed += (s, e) => OnFormClosed(typeof(T));
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
            if (_openForms.ContainsKey(formType) && _openForms[formType] != null)
            {
                if (!_openForms[formType].IsDisposed)
                {
                    _openForms[formType].Close();
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
            return _openForms.ContainsKey(formType) && 
                   _openForms[formType] != null && 
                   !_openForms[formType].IsDisposed &&
                   _openForms[formType].Visible;
        }

        /// <summary>
        /// Gets a specific open form of the given type
        /// </summary>
        public static T GetOpenForm<T>() where T : Form
        {
            Type formType = typeof(T);
            if (_openForms.ContainsKey(formType) && _openForms[formType] != null && !_openForms[formType].IsDisposed)
            {
                return (T)_openForms[formType];
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
        private static void OnFormClosed(Type formType)
        {
            if (_openForms.ContainsKey(formType))
            {
                _openForms[formType] = null;
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
    }
}
