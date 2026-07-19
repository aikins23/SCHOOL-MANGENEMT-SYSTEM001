using System.Security.Claims;

namespace KingdomPrep.Web.Core.Auth;

public static class WebPermission
{
    public const string WriteAccess = "WriteAccess";
    public const string StudentsWrite = "Students.Write";
    public const string FeePaymentRecord = "Finance.FeePayment.Record";
    public const string ExpenseManage = "Finance.Expense.Manage";
    public const string AdmissionsManage = "Admissions.Manage";
    public const string LeaveApprove = "Leave.Approve";
    public const string AcademicClassStructureManage = "Academics.ClassStructure.Manage";
    public const string SettingsExamSetupManage = "Settings.ExamSetup.Manage";
    public const string AcademicResultsPublish = "Academics.Results.Publish";
    public const string AdminUsersManage = "Admin.Users.Manage";
    public const string ParentFeePayment = "Parent.FeePayment";
    public const string ParentRequestSubmit = "Parent.Request.Submit";
    public const string PaymentSettingsManage = "Payments.Settings.Manage";
    public const string TeacherAttendanceRecord = "Teacher.Attendance.Record";
    public const string TeacherGradesManage = "Teacher.Grades.Manage";
    public const string TeacherAssignmentsManage = "Teacher.Assignments.Manage";
    public const string TeacherLeaveSubmit = "Teacher.Leave.Submit";
    public const string AccountSelfManage = "Account.Self.Manage";
    public const string StaffAttendanceSelf = "Staff.Attendance.Self";
    public const string StaffAttendanceMonitor = "Staff.Attendance.Monitor";

    private static readonly IReadOnlyDictionary<string, UserRole[]> RolesByPolicy =
        new Dictionary<string, UserRole[]>(StringComparer.OrdinalIgnoreCase)
        {
            [WriteAccess] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher },
            [StudentsWrite] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            [FeePaymentRecord] = new[] { UserRole.Accountant },
            [ExpenseManage] = new[] { UserRole.Director, UserRole.Accountant },
            [AdmissionsManage] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            [LeaveApprove] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            [AcademicClassStructureManage] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            [SettingsExamSetupManage] = new[] { UserRole.Director, UserRole.Administrator },
            [AcademicResultsPublish] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            [AdminUsersManage] = new[] { UserRole.Director, UserRole.Administrator },
            [ParentFeePayment] = new[] { UserRole.Parent },
            [ParentRequestSubmit] = new[] { UserRole.Parent },
            [PaymentSettingsManage] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
            [TeacherAttendanceRecord] = new[] { UserRole.Teacher },
            [TeacherGradesManage] = new[] { UserRole.Teacher },
            [TeacherAssignmentsManage] = new[] { UserRole.Teacher },
            [TeacherLeaveSubmit] = new[] { UserRole.Teacher },
            [AccountSelfManage] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant, UserRole.Parent },
            [StaffAttendanceSelf] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster, UserRole.Teacher, UserRole.Accountant },
            [StaffAttendanceMonitor] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
        };

    public static IEnumerable<string> Names => RolesByPolicy.Keys;

    public static bool HasPermission(ClaimsPrincipal? user, string policyName)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!RolesByPolicy.TryGetValue(policyName, out var allowedRoles))
        {
            return false;
        }

        return allowedRoles.Contains(ParseRole(user));
    }

    public static bool CanRenderAction(ClaimsPrincipal? user, string policyName)
    {
        return HasPermission(user, policyName);
    }

    public static IReadOnlyDictionary<string, bool> GetActionVisibility(ClaimsPrincipal? user, IEnumerable<string> policyNames)
    {
        return policyNames
            .Where(policyName => !string.IsNullOrWhiteSpace(policyName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(policyName => policyName, policyName => CanRenderAction(user, policyName), StringComparer.OrdinalIgnoreCase);
    }

    public static UserRole ParseRole(ClaimsPrincipal user)
    {
        foreach (var role in Enum.GetValues<UserRole>())
        {
            if (role != UserRole.Unknown && user.IsInRole(role.ToString()))
            {
                return role;
            }
        }

        var roleClaim = user.FindAll(ClaimTypes.Role)
            .Select(claim => RoleParser.Parse(claim.Value))
            .Where(role => role != UserRole.Unknown)
            .Cast<UserRole?>()
            .FirstOrDefault();

        if (roleClaim.HasValue)
        {
            return roleClaim.Value;
        }

        var shortRoleClaim = user.FindAll("role")
            .Select(claim => RoleParser.Parse(claim.Value))
            .Where(role => role != UserRole.Unknown)
            .Cast<UserRole?>()
            .FirstOrDefault();

        return shortRoleClaim ?? UserRole.Unknown;
    }
}
