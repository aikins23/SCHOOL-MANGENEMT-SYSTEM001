namespace KingdomPrep.Web.Core.Auth;

/// <summary>
/// Brute-force protection for the internet-facing login: after too many failed
/// attempts for a username within a window, that username is temporarily locked.
/// Implementations must be thread-safe and registered as a singleton so state
/// persists across requests.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>
    /// True if the username is currently locked out. <paramref name="retryAfter"/>
    /// is the remaining lock time (zero when not locked).
    /// </summary>
    bool IsLockedOut(string username, out TimeSpan retryAfter);

    /// <summary>Record a failed sign-in for the username.</summary>
    void RecordFailure(string username);

    /// <summary>Clear all failure state for the username (call on success).</summary>
    void RecordSuccess(string username);
}
