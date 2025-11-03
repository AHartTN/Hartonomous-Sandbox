namespace Hartonomous.Core.Security;

public sealed class ThrottleContext
{
    public string TenantId { get; init; } = string.Empty;

    public string PrincipalId { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string MessageType { get; init; } = string.Empty;
}
