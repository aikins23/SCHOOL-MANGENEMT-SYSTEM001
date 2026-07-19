using System.Security.Claims;
using KingdomPrep.Web.Core.Auth;

public class WebPermissionTests
{
    [Theory]
    [InlineData(WebPermission.WriteAccess, UserRole.Accountant, false)]
    [InlineData(WebPermission.WriteAccess, UserRole.Parent, false)]
    [InlineData(WebPermission.WriteAccess, UserRole.Teacher, true)]
    [InlineData(WebPermission.StudentsWrite, UserRole.Headmaster, true)]
    [InlineData(WebPermission.StudentsWrite, UserRole.Accountant, false)]
    [InlineData(WebPermission.FeePaymentRecord, UserRole.Accountant, true)]
    [InlineData(WebPermission.FeePaymentRecord, UserRole.Director, false)]
    [InlineData(WebPermission.ExpenseManage, UserRole.Accountant, true)]
    [InlineData(WebPermission.ExpenseManage, UserRole.Director, true)]
    [InlineData(WebPermission.ExpenseManage, UserRole.Administrator, false)]
    [InlineData(WebPermission.AdmissionsManage, UserRole.Headmaster, true)]
    [InlineData(WebPermission.AdmissionsManage, UserRole.Accountant, false)]
    [InlineData(WebPermission.SettingsExamSetupManage, UserRole.Administrator, true)]
    [InlineData(WebPermission.SettingsExamSetupManage, UserRole.Headmaster, false)]
    [InlineData(WebPermission.AdminUsersManage, UserRole.Director, true)]
    [InlineData(WebPermission.AdminUsersManage, UserRole.Headmaster, false)]
    [InlineData(WebPermission.ParentFeePayment, UserRole.Parent, true)]
    [InlineData(WebPermission.ParentFeePayment, UserRole.Accountant, false)]
    [InlineData(WebPermission.ParentRequestSubmit, UserRole.Parent, true)]
    [InlineData(WebPermission.ParentRequestSubmit, UserRole.Headmaster, false)]
    [InlineData(WebPermission.TeacherAttendanceRecord, UserRole.Teacher, true)]
    [InlineData(WebPermission.TeacherAttendanceRecord, UserRole.Parent, false)]
    [InlineData(WebPermission.TeacherGradesManage, UserRole.Teacher, true)]
    [InlineData(WebPermission.TeacherGradesManage, UserRole.Administrator, false)]
    [InlineData(WebPermission.TeacherAssignmentsManage, UserRole.Teacher, true)]
    [InlineData(WebPermission.TeacherAssignmentsManage, UserRole.Headmaster, false)]
    [InlineData(WebPermission.TeacherLeaveSubmit, UserRole.Teacher, true)]
    [InlineData(WebPermission.TeacherLeaveSubmit, UserRole.Accountant, false)]
    [InlineData(WebPermission.AccountSelfManage, UserRole.Director, true)]
    [InlineData(WebPermission.AccountSelfManage, UserRole.Parent, true)]
    [InlineData(WebPermission.AccountSelfManage, UserRole.Unknown, false)]
    public void HasPermission_UsesExpectedRoleMatrix(string policyName, UserRole role, bool expected)
    {
        var user = CreatePrincipal(role);

        Assert.Equal(expected, WebPermission.HasPermission(user, policyName));
    }

    [Fact]
    public void HasPermission_DeniesUnauthenticatedUsers()
    {
        Assert.False(WebPermission.HasPermission(new ClaimsPrincipal(new ClaimsIdentity()), WebPermission.WriteAccess));
    }

    [Fact]
    public void HasPermission_DeniesAuthenticatedUsersWithoutRoleClaims()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "test-user")
        }, "Test"));

        Assert.False(WebPermission.HasPermission(user, WebPermission.WriteAccess));
    }

    [Fact]
    public void HasPermission_DeniesUnknownPolicies()
    {
        Assert.False(WebPermission.HasPermission(CreatePrincipal(UserRole.Director), "Unknown.Policy"));
    }

    private static ClaimsPrincipal CreatePrincipal(UserRole role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, role.ToString())
        }, "Test");

        return new ClaimsPrincipal(identity);
    }
}
