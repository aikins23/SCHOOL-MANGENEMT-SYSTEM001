namespace KingdomPrep.Web.Core.Auth;

public class AuthService(IUserLookup users) : IAuthService
{
    public async Task<AuthUser?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;

        var row = await users.FindAsync(username.Trim());
        if (row is null) return null;
        if (!PasswordHasher.Verify(password, row.Value.Password)) return null;

        return new AuthUser(username.Trim(), RoleParser.Parse(row.Value.UserType), row.Value.EmploymentId);
    }
}
