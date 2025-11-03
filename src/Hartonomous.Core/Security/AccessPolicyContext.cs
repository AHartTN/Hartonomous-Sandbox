using System.Collections.Generic;

namespace Hartonomous.Core.Security;

public sealed class AccessPolicyContext
{
    public string TenantId { get; init; } = string.Empty;

    public string PrincipalId { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string MessageType { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, object> Attributes { get; init; } = new Dictionary<string, object>();
}
