using KingdomPrep.Web.Core.Auth;
using Xunit;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_RoundTrip_True()
    {
        string hash = PasswordHasher.Hash("Secret123!");
        Assert.StartsWith("P3$", hash);
        Assert.True(PasswordHasher.Verify("Secret123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_False()
    {
        string hash = PasswordHasher.Hash("Secret123!");
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_KnownDesktopVector_True()
    {
        // Legacy P2 generated with .NET Framework Rfc2898DeriveBytes (default = HMAC-SHA1),
        // password "password", salt 0x0102030405060708, 100000 iters, 16-byte hash —
        // identical to what the desktop AuthService writes.
        const string stored = "P2$100000$AQIDBAUGBwg=$X6JRGXIqKPfnF+YJRvyUmQ==";
        Assert.True(PasswordHasher.Verify("password", stored));
        Assert.False(PasswordHasher.Verify("Password", stored)); // case-sensitive
    }

    [Fact]
    public void Verify_LegacyPlaintext_FallsBack()
    {
        Assert.True(PasswordHasher.Verify("plainpw", "plainpw"));
        Assert.False(PasswordHasher.Verify("plainpw", "different"));
    }

    [Fact]
    public void Verify_MalformedStored_False()
    {
        Assert.False(PasswordHasher.Verify("x", "P2$notanumber$AQ==$AQ=="));
        Assert.False(PasswordHasher.Verify("x", "P2$100000$@@@$@@@"));
        Assert.False(PasswordHasher.Verify("x", ""));
    }
}
