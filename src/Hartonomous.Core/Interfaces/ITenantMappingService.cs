using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides safe mapping between Azure Entra External ID tenant GUIDs and internal integer tenant IDs.
/// Replaces unsafe GetHashCode() approach with database-backed mapping.
/// </summary>
public interface ITenantMappingService
{
    /// <summary>
    /// Resolves Azure Entra tenant GUID to internal integer tenant ID.
    /// Optionally auto-registers new tenants if not found.
    /// </summary>
    /// <param name="tenantGuid">Azure Entra External ID tenant GUID from claims</param>
    /// <param name="tenantName">Optional tenant name for auto-registration</param>
    /// <param name="autoRegister">Whether to auto-register unknown tenants (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Internal tenant ID, or null if not found and auto-registration disabled</returns>
    Task<int?> ResolveTenantGuidAsync(
        Guid tenantGuid,
        string tenantName = null,
        bool autoRegister = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant GUID from internal tenant ID (reverse lookup).
    /// </summary>
    /// <param name="tenantId">Internal tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Azure Entra tenant GUID, or null if not found</returns>
    Task<Guid?> GetTenantGuidAsync(
        int tenantId,
        CancellationToken cancellationToken = default);
}
