using System.Collections.Concurrent;

namespace KingdomPrep.Web.Core.Auth;

/// <summary>
/// In-memory <see cref="ILoginThrottle"/>. Counts consecutive failures per
/// username; once <see cref="MaxAttempts"/> is reached the username is locked
/// for <see cref="LockoutDuration"/>. A successful sign-in (or the lock expiring)
/// resets the count. Sufficient for a single-instance deployment; a multi-instance
/// host would back this with a shared store.
/// </summary>
public sealed class LoginThrottle : ILoginThrottle
{
    private sealed class Entry
    {
        public int Failures;
        public DateTimeOffset? LockedUntil;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<DateTimeOffset> _now;

    public int MaxAttempts { get; }
    public TimeSpan LockoutDuration { get; }

    public LoginThrottle(int maxAttempts = 5, TimeSpan? lockoutDuration = null, Func<DateTimeOffset>? now = null)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        MaxAttempts = maxAttempts;
        LockoutDuration = lockoutDuration ?? TimeSpan.FromMinutes(15);
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsLockedOut(string username, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        var key = Normalize(username);
        if (key.Length == 0 || !_entries.TryGetValue(key, out var entry)) return false;

        if (entry.LockedUntil is { } until)
        {
            var remaining = until - _now();
            if (remaining > TimeSpan.Zero)
            {
                retryAfter = remaining;
                return true;
            }
            // Lock expired — clear so the user gets a fresh set of attempts.
            _entries.TryRemove(key, out _);
        }
        return false;
    }

    public void RecordFailure(string username)
    {
        var key = Normalize(username);
        if (key.Length == 0) return;

        var entry = _entries.GetOrAdd(key, _ => new Entry());
        lock (entry)
        {
            // If a previous lock already elapsed, start counting again from zero.
            if (entry.LockedUntil is { } until && until <= _now())
            {
                entry.Failures = 0;
                entry.LockedUntil = null;
            }
            entry.Failures++;
            if (entry.Failures >= MaxAttempts)
                entry.LockedUntil = _now() + LockoutDuration;
        }
    }

    public void RecordSuccess(string username)
    {
        var key = Normalize(username);
        if (key.Length != 0) _entries.TryRemove(key, out _);
    }

    private static string Normalize(string? username) => username?.Trim() ?? "";
}
