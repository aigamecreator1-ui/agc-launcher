using System.Collections.Concurrent;

namespace AGC.Server.Services;

/// <summary>
/// Basic in-memory throttle on owner-code verification attempts, keyed by the
/// requesting IP. The owner code is a static 6-digit value, so it must not be
/// trivially brute-forceable. Resets on server restart — acceptable for this scale.
/// </summary>
public sealed class OwnerCodeAttemptLimiter
{
    private const int MaxFailuresInWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _failures = new();
    private readonly Lock _lock = new();

    public bool IsBlocked(string key)
    {
        lock (_lock)
        {
            Prune(key);
            return _failures.TryGetValue(key, out var attempts) && attempts.Count >= MaxFailuresInWindow;
        }
    }

    public void RecordFailure(string key)
    {
        lock (_lock)
        {
            Prune(key);
            var attempts = _failures.GetOrAdd(key, _ => []);
            attempts.Add(DateTimeOffset.UtcNow);
        }
    }

    public void RecordSuccess(string key)
    {
        lock (_lock)
        {
            _failures.TryRemove(key, out _);
        }
    }

    private void Prune(string key)
    {
        if (!_failures.TryGetValue(key, out var attempts))
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - Window;
        attempts.RemoveAll(a => a < cutoff);
    }
}
