using System;

namespace Hartonomous.Core.Security;

public sealed class PolicyDeniedException : Exception
{
    public PolicyDeniedException(string policy, string reason)
        : base($"Access denied by policy '{policy}': {reason}")
    {
        Policy = policy;
        Reason = reason;
    }

    public string Policy { get; }

    public string Reason { get; }
}
