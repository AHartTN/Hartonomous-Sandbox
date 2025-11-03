namespace Hartonomous.Core.Security;

public sealed class AccessPolicyResult
{
    private AccessPolicyResult(bool isAllowed, string? reason, string? policy)
    {
        IsAllowed = isAllowed;
        Reason = reason;
        Policy = policy;
    }

    public bool IsAllowed { get; }

    public string? Reason { get; }

    public string? Policy { get; }

    public static AccessPolicyResult Allow()
        => new(true, null, null);

    public static AccessPolicyResult Deny(string policy, string reason)
        => new(false, reason, policy);
}
