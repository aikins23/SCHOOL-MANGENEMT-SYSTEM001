namespace KingdomPrep.Web.Core.Auth;

public class AuthService(IUserLookup users, IAuthSecurityOptions? security = null) : IAuthService
{
    public async Task<AuthUser?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;

        var row = await users.FindAsync(username.Trim());
        if (row is null) return null;
        if (!PasswordHasher.Verify(password, row.Value.Password, security?.AllowLegacyPlainTextPasswords ?? true)) return null;

        return new AuthUser(username.Trim(), RoleParser.Parse(row.Value.UserType), row.Value.EmploymentId, row.Value.SchoolId);
    }
}

public interface IAuthSecurityOptions
{
    bool AllowLegacyPlainTextPasswords { get; }
}

public sealed class AuthSecurityOptions(bool allowLegacyPlainTextPasswords) : IAuthSecurityOptions
{
    public bool AllowLegacyPlainTextPasswords { get; } = allowLegacyPlainTextPasswords;
}
