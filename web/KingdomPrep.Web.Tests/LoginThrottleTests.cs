using KingdomPrep.Web.Core.Auth;
using Xunit;

public class LoginThrottleTests
{
    private sealed class Clock
    {
        public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public Func<DateTimeOffset> Func => () => Now;
        public void Advance(TimeSpan by) => Now += by;
    }

    [Fact]
    public void NotLockedOut_Initially()
    {
        var t = new LoginThrottle();
        Assert.False(t.IsLockedOut("jane", out var retry));
        Assert.Equal(TimeSpan.Zero, retry);
    }

    [Fact]
    public void LocksOut_AfterMaxAttempts()
    {
        var t = new LoginThrottle(maxAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(15));
        t.RecordFailure("jane");
        t.RecordFailure("jane");
        Assert.False(t.IsLockedOut("jane", out _)); // 2 < 3
        t.RecordFailure("jane");
        Assert.True(t.IsLockedOut("jane", out var retry));
        Assert.True(retry > TimeSpan.Zero && retry <= TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void LockKeyedPerUsername()
    {
        var t = new LoginThrottle(maxAttempts: 2);
        t.RecordFailure("jane");
        t.RecordFailure("jane");
        Assert.True(t.IsLockedOut("jane", out _));
        Assert.False(t.IsLockedOut("john", out _));
    }

    [Fact]
    public void UsernameMatch_IsCaseInsensitive()
    {
        var t = new LoginThrottle(maxAttempts: 2);
        t.RecordFailure("Jane");
        t.RecordFailure("jANE");
        Assert.True(t.IsLockedOut("jane", out _));
    }

    [Fact]
    public void Success_ResetsFailureCount()
    {
        var t = new LoginThrottle(maxAttempts: 3);
        t.RecordFailure("jane");
        t.RecordFailure("jane");
        t.RecordSuccess("jane");
        t.RecordFailure("jane");
        Assert.False(t.IsLockedOut("jane", out _)); // count restarted
    }

    [Fact]
    public void LockExpires_AfterDuration()
    {
        var clock = new Clock();
        var t = new LoginThrottle(maxAttempts: 2, lockoutDuration: TimeSpan.FromMinutes(15), now: clock.Func);
        t.RecordFailure("jane");
        t.RecordFailure("jane");
        Assert.True(t.IsLockedOut("jane", out _));

        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.False(t.IsLockedOut("jane", out var retry));
        Assert.Equal(TimeSpan.Zero, retry);
    }

    [Fact]
    public void FailuresAfterLockExpiry_StartFresh()
    {
        var clock = new Clock();
        var t = new LoginThrottle(maxAttempts: 2, lockoutDuration: TimeSpan.FromMinutes(15), now: clock.Func);
        t.RecordFailure("jane");
        t.RecordFailure("jane");          // locked
        clock.Advance(TimeSpan.FromMinutes(16));
        t.RecordFailure("jane");          // first failure of a new window
        Assert.False(t.IsLockedOut("jane", out _));
    }

    [Fact]
    public void BlankUsername_IsIgnored()
    {
        var t = new LoginThrottle(maxAttempts: 1);
        t.RecordFailure("");
        t.RecordFailure("   ");
        Assert.False(t.IsLockedOut("", out _));
    }
}
