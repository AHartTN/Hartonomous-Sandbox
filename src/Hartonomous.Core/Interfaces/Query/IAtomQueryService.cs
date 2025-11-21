using Hartonomous.Shared.Contracts.DTOs;

namespace Hartonomous.Core.Interfaces.Query;

/// <summary>
/// Service for querying atom data with semantic connections
/// </summary>
public interface IAtomQueryService
{
    /// <summary>
    /// Get detailed atom information including semantic relationships
    /// </summary>
    /// <param name="atomId">The unique identifier of the atom</param>
    /// <returns>Atom details with parent/child relationships</returns>
    Task<AtomDetailDTO?> GetAtomAsync(long atomId);

    /// <summary>
    /// Get atoms by content hash
    /// </summary>
    /// <param name="contentHash">The SHA-256 content hash</param>
    /// <returns>Atoms with matching content hash</returns>
    Task<IEnumerable<AtomDetailDTO>> GetAtomsByHashAsync(byte[] contentHash);

    /// <summary>
    /// Get atoms by tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>Paginated list of atoms</returns>
    Task<IEnumerable<AtomDetailDTO>> GetAtomsByTenantAsync(int tenantId, int skip = 0, int take = 100);
}
