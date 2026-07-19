using System;
using System.Collections.Generic;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a role that can be assigned to users.
    /// System roles (Director, Administrator, etc.) cannot be deleted but can have permissions adjusted.
    /// Custom roles can be created by school directors for school-specific needs.
    /// </summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? SchoolId { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; }

        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }

    public class RolePermission
    {
        public int RolePermissionId { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; }
        public DateTime AssignedDate { get; set; }
        public string AssignedBy { get; set; }
    }

    public class UserRoleAssignment
    {
        public int UserRoleId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public DateTime AssignedDate { get; set; }
        public string AssignedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public static class SystemRoles
    {
        public const string Director = "Director";
        public const string Administrator = "Administrator";
        public const string Headmaster = "Headmaster";
        public const string Teacher = "Teacher";
        public const string Accountant = "Accountant";
        public const string Parent = "Parent";
    }
}
