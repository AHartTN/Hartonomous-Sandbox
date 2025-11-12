using System;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides access to the current tenant context for multi-tenant operations.
/// Implementations should read from Azure Entra External ID claims in HTTP context
/// or provide ambient tenant ID for background processes.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the authenticated user's claims.
    /// For Azure Entra External ID, this is typically the "tenant_id" or "tid" claim.
    /// </summary>
    /// <returns>The tenant ID, or null if no tenant context is available (e.g., system operations).</returns>
    int? GetCurrentTenantId();

    /// <summary>
    /// Gets whether the current operation is running in a tenant-specific context.
    /// </summary>
    bool HasTenantContext { get; }
}
