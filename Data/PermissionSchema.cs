using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Ensures the dynamic permission system tables exist.
    /// Tables: Permissions, Roles, RolePermissions, UserRoleAssignments.
    /// Idempotent and best-effort: safe to call on every startup.
    /// </summary>
    public static class PermissionSchema
    {
        public static async Task EnsurePermissionTablesAsync()
        {
            string connStr = AppConfig.ConnectionString;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                await EnsurePermissionsTableAsync(conn);
                await EnsureRolesTableAsync(conn);
                await EnsureRolePermissionsTableAsync(conn);
                await EnsureUserRoleAssignmentsTableAsync(conn);
                await SeedSystemPermissionsAsync(conn);
                await SeedSystemRolesAsync(conn);
            }
        }

        private static async Task EnsurePermissionsTableAsync(SqlConnection conn)
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Permissions' AND xtype='U')
BEGIN
    CREATE TABLE Permissions (
        PermissionId INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(150) NOT NULL UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        Module NVARCHAR(100) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        IsSystemPermission BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Permissions_Module ON Permissions(Module);
    CREATE INDEX IX_Permissions_Code ON Permissions(Code);
END";
            using (var cmd = new SqlCommand(sql, conn))
                await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRolesTableAsync(SqlConnection conn)
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Roles' AND xtype='U')
BEGIN
    CREATE TABLE Roles (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        SchoolId INT NULL,
        IsSystemRole BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedDate DATETIME NULL,
        CreatedBy NVARCHAR(100) NULL,
        CONSTRAINT UQ_Roles_Name_School UNIQUE (Name, SchoolId)
    );
    CREATE INDEX IX_Roles_SchoolId ON Roles(SchoolId);
END";
            using (var cmd = new SqlCommand(sql, conn))
                await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRolePermissionsTableAsync(SqlConnection conn)
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RolePermissions' AND xtype='U')
BEGIN
    CREATE TABLE RolePermissions (
        RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
        RoleId INT NOT NULL,
        PermissionId INT NOT NULL,
        IsGranted BIT NOT NULL DEFAULT 1,
        AssignedDate DATETIME NOT NULL DEFAULT GETDATE(),
        AssignedBy NVARCHAR(100) NULL,
        CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE,
        CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES Permissions(PermissionId) ON DELETE CASCADE,
        CONSTRAINT UQ_RolePermissions UNIQUE (RoleId, PermissionId)
    );
    CREATE INDEX IX_RolePermissions_RoleId ON RolePermissions(RoleId);
END";
            using (var cmd = new SqlCommand(sql, conn))
                await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureUserRoleAssignmentsTableAsync(SqlConnection conn)
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserRoleAssignments' AND xtype='U')
BEGIN
    CREATE TABLE UserRoleAssignments (
        UserRoleId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        RoleId INT NOT NULL,
        AssignedDate DATETIME NOT NULL DEFAULT GETDATE(),
        AssignedBy NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_UserRoleAssignments_Role FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
        CONSTRAINT UQ_UserRoleAssignments UNIQUE (Username, RoleId)
    );
    CREATE INDEX IX_UserRoleAssignments_Username ON UserRoleAssignments(Username);
END";
            using (var cmd = new SqlCommand(sql, conn))
                await cmd.ExecuteNonQueryAsync();
        }

        private static async Task SeedSystemPermissionsAsync(SqlConnection conn)
        {
            string[,] permissions = new string[,]
            {
                {"Students.View", "View Students", "View student records and lists", "Students", "View"},
                {"Students.Register", "Register Students", "Add new student admissions", "Students", "Create"},
                {"Students.Edit", "Edit Students", "Modify student information", "Students", "Edit"},
                {"Students.Delete", "Delete Students", "Remove student records", "Students", "Delete"},
                {"Students.Promote", "Promote Students", "Promote students between classes", "Students", "Edit"},
                {"Students.Import", "Import Students", "Bulk import student data", "Students", "Create"},
                {"Students.AddRemarks", "Add Student Remarks", "Add teacher remarks for report cards", "Students", "Edit"},
                {"Students.ApproveRemarks", "Approve Student Remarks", "Approve teacher remarks", "Students", "Approve"},

                {"Employees.View", "View Employees", "View employee records", "Employees", "View"},
                {"Employees.Register", "Register Employees", "Add new staff members", "Employees", "Create"},
                {"Employees.Edit", "Edit Employees", "Modify employee information", "Employees", "Edit"},
                {"Employees.Delete", "Delete Employees", "Remove employee records", "Employees", "Delete"},

                {"Finance.View", "View Finance", "View financial records", "Finance", "View"},
                {"Finance.FeePayment.Record", "Record Fee Payments", "Process fee payments", "Finance", "Create"},
                {"Finance.FeePayment.Approve", "Approve Fee Payments", "Approve pending payments", "Finance", "Approve"},
                {"Finance.Expense.Manage", "Manage Expenses", "Add and manage expenses", "Finance", "Edit"},
                {"Finance.Scholarship.Manage", "Manage Scholarships", "Setup scholarships", "Finance", "Edit"},
                {"Finance.Scholarship.Approve", "Approve Scholarships", "Approve scholarship requests", "Finance", "Approve"},
                {"Finance.AdditionalFees.View", "View Additional Fees", "View additional fee requests", "Finance", "View"},
                {"Finance.AdditionalFees.Create", "Create Additional Fees", "Create and submit additional fee requests", "Finance", "Create"},
                {"Finance.AdditionalFees.Approve", "Approve Additional Fees", "Approve or reject additional fee requests", "Finance", "Approve"},
                {"Finance.Reports.View", "View Financial Reports", "Access financial reports", "Finance", "View"},

                {"Academics.Exams.Create", "Create Exams", "Create examinations", "Academics", "Create"},
                {"Academics.Exams.Edit", "Edit Exams", "Modify exam details and grades", "Academics", "Edit"},
                {"Academics.Exams.View", "View Exams", "View examinations and results", "Academics", "View"},
                {"Academics.Exams.ConfigureTypes", "Configure Exam Types", "Create and manage exam types", "Academics", "Admin"},
                {"Academics.Attendance.Record", "Record Attendance", "Mark student attendance", "Academics", "Create"},
                {"Academics.Attendance.View", "View Attendance", "View attendance records", "Academics", "View"},
                {"Academics.ReportCards.Generate", "Generate Report Cards", "Generate student report cards", "Academics", "Create"},
                {"Academics.ReportCards.Approve", "Approve Report Cards", "Approve report cards", "Academics", "Approve"},
                {"Academics.Performance.Submit", "Submit Performance Reports", "Submit class performance reports", "Academics", "Create"},
                {"Academics.Performance.Approve", "Approve Performance Reports", "Approve performance reports", "Academics", "Approve"},
                {"Academics.Performance.View", "View Performance Reports", "View class performance", "Academics", "View"},
                {"Academics.WorkOutput.Submit", "Submit Work Output", "Submit weekly work output", "Academics", "Create"},
                {"Academics.WorkOutput.Approve", "Approve Work Output", "Approve work output reports", "Academics", "Approve"},
                {"Academics.WorkOutput.View", "View Work Output", "View work output reports", "Academics", "View"},
                {"Academics.Timetable.Manage", "Manage Timetable", "Create and edit timetables", "Academics", "Admin"},
                {"Academics.Calendar.Manage", "Manage Calendar", "Manage academic calendar", "Academics", "Admin"},

                {"Leave.Submit", "Submit Leave Request", "Submit own leave request", "Leave", "Create"},
                {"Leave.Approve", "Approve Leave Requests", "Approve staff leave", "Leave", "Approve"},
                {"Leave.View", "View Leave Records", "View leave history", "Leave", "View"},

                {"Settings.Roles.Manage", "Manage Roles & Permissions", "Configure roles and permissions", "Settings", "Admin"},
                {"Settings.SchoolProfile.Manage", "Manage School Profile", "Edit school information", "Settings", "Admin"},
                {"Settings.GradingScheme.Manage", "Manage Grading Scheme", "Configure grading", "Settings", "Admin"},
                {"Settings.Subjects.Manage", "Manage Subjects", "Configure subjects", "Settings", "Admin"},

                {"System.Users.Manage", "Manage Users", "Create and manage user accounts", "System", "Admin"},
                {"System.Backup.Manage", "Manage Backups", "Database backup operations", "System", "Admin"},
                {"System.Sync.Manage", "Manage Sync", "Configure synchronization", "System", "Admin"},
                {"System.AuditLog.View", "View Audit Logs", "View system audit trail", "System", "View"},

                {"Communications.Notice.Send", "Send Notices", "Send school notices", "Communications", "Create"},
                {"Communications.SMS.Send", "Send SMS", "Send bulk SMS messages", "Communications", "Create"},
            };

            for (int i = 0; i < permissions.GetLength(0); i++)
            {
                const string upsert = @"
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = @code)
BEGIN
    INSERT INTO Permissions (Code, Name, Description, Module, Category, IsSystemPermission)
    VALUES (@code, @name, @desc, @module, @category, 1)
END";
                using (var cmd = new SqlCommand(upsert, conn))
                {
                    cmd.Parameters.AddWithValue("@code", permissions[i, 0]);
                    cmd.Parameters.AddWithValue("@name", permissions[i, 1]);
                    cmd.Parameters.AddWithValue("@desc", permissions[i, 2]);
                    cmd.Parameters.AddWithValue("@module", permissions[i, 3]);
                    cmd.Parameters.AddWithValue("@category", permissions[i, 4]);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task SeedSystemRolesAsync(SqlConnection conn)
        {
            string[,] roles = new string[,]
            {
                {"Director", "School Director - Full system access"},
                {"Administrator", "System Administrator - Full operational access"},
                {"Headmaster", "Headmaster - Academic and staff oversight"},
                {"Teacher", "Teacher - Classroom and student management"},
                {"Accountant", "Accountant - Financial operations"},
                {"Parent", "Parent - View child's records (web only)"}
            };

            for (int i = 0; i < roles.GetLength(0); i++)
            {
                const string upsert = @"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = @name AND SchoolId IS NULL)
BEGIN
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive, CreatedBy)
    VALUES (@name, @desc, 1, 1, 'SYSTEM')
END";
                using (var cmd = new SqlCommand(upsert, conn))
                {
                    cmd.Parameters.AddWithValue("@name", roles[i, 0]);
                    cmd.Parameters.AddWithValue("@desc", roles[i, 1]);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
