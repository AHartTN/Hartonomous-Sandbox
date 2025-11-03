using System;

namespace Hartonomous.Core.Configuration;

public sealed class RateLimitRuleOptions
{
    public string Name { get; set; } = string.Empty;

    public RateLimitScope Scope { get; set; } = RateLimitScope.Tenant;

    public string? Operation { get; set; }
        = null;

    public int PermitLimit { get; set; } = 60;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    public string? TenantId { get; set; }
        = null;

    public string? PrincipalId { get; set; }
        = null;
}
