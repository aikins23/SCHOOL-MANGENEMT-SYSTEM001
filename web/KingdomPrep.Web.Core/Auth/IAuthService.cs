namespace KingdomPrep.Web.Core.Auth;

public interface IAuthService
{
    /// <summary>Returns the authenticated user, or null on bad credentials.</summary>
    Task<AuthUser?> AuthenticateAsync(string username, string password);
}

/// <summary>
/// Thin user-lookup abstraction implemented by the Data layer, so Core has no EF dependency.
/// </summary>
public interface IUserLookup
{
    Task<(string Password, string? UserType, int? EmploymentId)?> FindAsync(string username);
}
