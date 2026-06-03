namespace KingdomPrep.Web.Core.Auth;

public record AuthUser(string Username, UserRole Role, int? EmploymentId);
