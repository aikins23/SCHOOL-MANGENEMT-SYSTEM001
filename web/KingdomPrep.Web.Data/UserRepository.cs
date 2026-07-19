using KingdomPrep.Web.Data.Entities;
using KingdomPrep.Web.Core.Auth;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IUserRepository
{
    Task<UserEntity?> FindByUsernameAsync(string username);
}

public class UserRepository(IDbContextFactory<AppDbContext> factory) : IUserRepository
{
    public async Task<UserEntity?> FindByUsernameAsync(string username)
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
    }
}

public interface IUserAccountService
{
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword, Guid? schoolId = null, string? actorUsername = null);
    Task<List<UserAccountSummary>> GetUsersAsync(string? searchTerm = null, string? roleFilter = null, Guid? schoolId = null);
}

public class UserAccountService(IDbContextFactory<AppDbContext> factory, IAuditLogService? audit = null) : IUserAccountService
{
    public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword, Guid? schoolId = null, string? actorUsername = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(currentPassword)) throw new ArgumentException("Current password is required.", nameof(currentPassword));
        if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException("New password is required.", nameof(newPassword));
        if (newPassword.Length < 8) throw new ArgumentException("New password must be at least 8 characters.", nameof(newPassword));

        using var db = await factory.CreateDbContextAsync();
        var query = db.Users.Where(u => u.Username == username);
        if (schoolId.HasValue) query = query.Where(u => u.SchoolId == schoolId.Value);

        var user = await query.FirstOrDefaultAsync();
        if (user == null) throw new InvalidOperationException("User account was not found for the current school.");
        if (!PasswordHasher.Verify(currentPassword, user.Password)) throw new InvalidOperationException("Current password is incorrect.");

        user.Password = PasswordHasher.Hash(newPassword);
        db.Users.Update(user);
        await db.SaveChangesAsync();

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? username,
                "PasswordChanged",
                "User",
                username,
                "User password changed.",
                schoolId ?? user.SchoolId));
        }
    }

    public async Task<List<UserAccountSummary>> GetUsersAsync(string? searchTerm = null, string? roleFilter = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Users.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(u => u.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.UserType != null && u.UserType.ToLower().Contains(term)) ||
                (u.EmploymentID.HasValue && u.EmploymentID.Value.ToString().Contains(term)));
        }

        var users = await query.OrderBy(u => u.Username).ToListAsync();
        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var parsedFilter = RoleParser.Parse(roleFilter);
            users = users.Where(u => RoleParser.Parse(u.UserType) == parsedFilter).ToList();
        }

        return users.Select(u => new UserAccountSummary(
            u.Username,
            string.IsNullOrWhiteSpace(u.UserType) ? "Unknown" : u.UserType,
            RoleParser.Parse(u.UserType).ToString(),
            u.EmploymentID,
            u.SchoolId,
            GetPasswordFormat(u.Password))).ToList();
    }

    private static string GetPasswordFormat(string? storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword)) return "Missing";
        if (storedPassword.StartsWith("P3$", StringComparison.Ordinal)) return "Current hash";
        if (storedPassword.StartsWith("P2$", StringComparison.Ordinal)) return "Legacy hash";
        return "Legacy plain text";
    }
}

public sealed record UserAccountSummary(
    string Username,
    string UserType,
    string Role,
    int? EmploymentId,
    Guid? SchoolId,
    string PasswordFormat);
