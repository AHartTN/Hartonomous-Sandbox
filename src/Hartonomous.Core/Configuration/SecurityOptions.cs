using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public HashSet<string> BannedTenants { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> BannedPrincipals { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, HashSet<string>> TenantOperationDenyList { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<RateLimitRuleOptions> RateLimits { get; } = new();
}
