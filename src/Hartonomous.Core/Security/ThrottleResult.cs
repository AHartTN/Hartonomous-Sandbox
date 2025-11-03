using System;

namespace Hartonomous.Core.Security;

public sealed class ThrottleResult
{
    private ThrottleResult(bool isThrottled, TimeSpan retryAfter, string? policy)
    {
        IsThrottled = isThrottled;
        RetryAfter = retryAfter;
        Policy = policy;
    }

    public bool IsThrottled { get; }

    public TimeSpan RetryAfter { get; }

    public string? Policy { get; }

    public static ThrottleResult Allow() => new(false, TimeSpan.Zero, null);

    public static ThrottleResult Deny(string policy, TimeSpan retryAfter)
        => new(true, retryAfter < TimeSpan.Zero ? TimeSpan.Zero : retryAfter, policy);
}
