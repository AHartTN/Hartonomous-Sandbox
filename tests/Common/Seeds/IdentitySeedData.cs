using System;
using System.Collections.Generic;

namespace Hartonomous.Testing.Common.Seeds;

public sealed record TenantSeed(Guid TenantId, string Slug, string Name, string BillingPlan, string ContactEmail, bool IsActive);

public sealed record PrincipalSeed(Guid PrincipalId, Guid TenantId, string Upn, IReadOnlyList<string> Roles, string Status);

public sealed record PolicyEntry(string PolicyKey, double? Value, int? Limit, int? WindowSeconds);

public sealed record PolicySeed(Guid TenantId, IReadOnlyList<PolicyEntry> Policies);

public sealed record IdentitySeedData(IReadOnlyList<TenantSeed> Tenants, IReadOnlyList<PrincipalSeed> Principals, IReadOnlyList<PolicySeed> Policies);
