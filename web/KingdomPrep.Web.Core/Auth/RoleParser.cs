namespace KingdomPrep.Web.Core.Auth;

/// <summary>
/// Maps the Users.User_Type string to a UserRole. Ported verbatim from the
/// desktop AuthService.ParseRole (including the DIRECTORS/ADMIN/GUARDIAN aliases)
/// so existing accounts resolve to the same role on the web.
/// </summary>
public static class RoleParser
{
    public static UserRole Parse(string? userType)
    {
        if (string.IsNullOrWhiteSpace(userType)) return UserRole.Unknown;
        switch (userType.Trim().ToUpperInvariant())
        {
            case "DIRECTOR":
            case "DIRECTORS": return UserRole.Director;
            case "ADMIN":
            case "ADMINISTRATOR": return UserRole.Administrator;
            case "TEACHER": return UserRole.Teacher;
            case "ACCOUNTANT": return UserRole.Accountant;
            case "HEADMASTER": return UserRole.Headmaster;
            case "PARENT":
            case "GUARDIAN": return UserRole.Parent;
            default: return UserRole.Unknown;
        }
    }
}
