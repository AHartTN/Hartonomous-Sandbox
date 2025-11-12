using System;
using Hartonomous.Core.Interfaces;

namespace Hartonomous.Infrastructure.Services.Tenant;

/// <summary>
/// Provides a fixed tenant context for background processing and system operations.
/// Use this for jobs that run outside HTTP request context (CDC consumers, scheduled tasks, etc.).
/// </summary>
public class FixedTenantContext : ITenantContext
{
    private readonly int _tenantId;

    /// <summary>
    /// Creates a tenant context with a fixed tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID to use for all operations.</param>
    public FixedTenantContext(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive", nameof(tenantId));
            
        _tenantId = tenantId;
    }

    public int? GetCurrentTenantId() => _tenantId;

    public bool HasTenantContext => true;
}
