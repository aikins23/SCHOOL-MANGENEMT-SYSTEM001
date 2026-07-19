using System;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Centralized UI visibility layer for role/permission-aware WinForms controls.
    /// It only controls whether an action is rendered; click handlers must still call
    /// AuthService.RequireAccess / RequireWriteAccess before executing protected work.
    /// </summary>
    public static class UiPermissionService
    {
        public static bool CanShowForm(string formKey)
        {
            return AuthService.CanRenderForm(formKey);
        }

        public static bool CanShowAction(string actionKey)
        {
            return AuthService.CanRenderAction(actionKey);
        }

        public static bool ApplyFormVisibility(Control control, string formKey)
        {
            return ApplyVisibility(control, CanShowForm(formKey));
        }

        public static bool ApplyActionVisibility(Control control, string actionKey)
        {
            return ApplyVisibility(control, CanShowAction(actionKey));
        }

        public static bool ApplyActionVisibility(ToolStripItem item, string actionKey)
        {
            if (item == null) return false;
            var visible = CanShowAction(actionKey);
            item.Visible = visible;
            return visible;
        }

        private static bool ApplyVisibility(Control control, bool visible)
        {
            if (control == null) return false;
            control.Visible = visible;
            return visible;
        }
    }
}
