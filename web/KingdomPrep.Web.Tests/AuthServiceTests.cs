using KingdomPrep.Web.Core.Auth;
using Xunit;

public class AuthServiceTests
{
    private sealed class FakeLookup((string, string?, int?)? row) : IUserLookup
    {
        public Task<(string Password, string? UserType, int? EmploymentId)?> FindAsync(string username)
            => Task.FromResult(row is null ? ((string, string?, int?)?)null
                                           : (row.Value.Item1, row.Value.Item2, row.Value.Item3));
    }

    [Fact]
    public async Task Authenticate_ValidTeacher_ReturnsUser()
    {
        string hash = PasswordHasher.Hash("pw");
        var svc = new AuthService(new FakeLookup((hash, "Teacher", 5)));
        var user = await svc.AuthenticateAsync("jane", "pw");
        Assert.NotNull(user);
        Assert.Equal(UserRole.Teacher, user!.Role);
        Assert.Equal(5, user.EmploymentId);
        Assert.Equal("jane", user.Username);
    }

    [Fact]
    public async Task Authenticate_BadPassword_ReturnsNull()
    {
        string hash = PasswordHasher.Hash("pw");
        var svc = new AuthService(new FakeLookup((hash, "Teacher", 5)));
        Assert.Null(await svc.AuthenticateAsync("jane", "wrong"));
    }

    [Fact]
    public async Task Authenticate_UnknownUser_ReturnsNull()
    {
        var svc = new AuthService(new FakeLookup(null));
        Assert.Null(await svc.AuthenticateAsync("ghost", "pw"));
    }

    [Fact]
    public async Task Authenticate_BlankInput_ReturnsNull()
    {
        var svc = new AuthService(new FakeLookup(("x", "Teacher", null)));
        Assert.Null(await svc.AuthenticateAsync("", "pw"));
        Assert.Null(await svc.AuthenticateAsync("jane", ""));
    }
}
