using System;

namespace Hartonomous.Core.Security;

public sealed class ThrottleRejectedException : Exception
{
    public ThrottleRejectedException(string policy, TimeSpan retryAfter)
        : base($"Rate limit exceeded for policy '{policy}'. Retry after {retryAfter}.")
    {
        Policy = policy;
        RetryAfter = retryAfter;
    }

    public string Policy { get; }

    public TimeSpan RetryAfter { get; }
}
