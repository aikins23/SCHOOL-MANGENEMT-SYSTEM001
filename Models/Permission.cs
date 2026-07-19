using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a granular permission that can be assigned to roles.
    /// Permissions follow the pattern: Module.Resource.Action (e.g. "Finance.FeePayment.Record")
    /// </summary>
    public class Permission
    {
        public int PermissionId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Module { get; set; }
        public string Category { get; set; }
        public bool IsSystemPermission { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public static class PermissionCategories
    {
        public const string View = "View";
        public const string Create = "Create";
        public const string Edit = "Edit";
        public const string Delete = "Delete";
        public const string Approve = "Approve";
        public const string Export = "Export";
        public const string Admin = "Admin";
    }

    public static class PermissionModules
    {
        public const string Students = "Students";
        public const string Employees = "Employees";
        public const string Finance = "Finance";
        public const string Academics = "Academics";
        public const string Leave = "Leave";
        public const string Reports = "Reports";
        public const string Settings = "Settings";
        public const string System = "System";
        public const string Communications = "Communications";
    }
}
