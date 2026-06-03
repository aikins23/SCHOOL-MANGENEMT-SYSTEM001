using KingdomPrep.Web.Core.Auth;

namespace KingdomPrep.Web.Data;

/// <summary>Adapts the EF UserRepository to the Core IUserLookup contract.</summary>
public class UserLookupAdapter(IUserRepository repo) : IUserLookup
{
    public async Task<(string Password, string? UserType, int? EmploymentId)?> FindAsync(string username)
    {
        var u = await repo.FindByUsernameAsync(username);
        return u is null ? null : (u.Password, u.UserType, u.EmploymentID);
    }
}
