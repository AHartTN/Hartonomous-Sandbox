using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Service for bulk insertion of atoms and their compositions into the database.
/// Optimized for high-throughput streaming scenarios.
/// </summary>
public interface IAtomBulkInsertService
{
    /// <summary>
    /// Bulk inserts an atomization result (atoms + compositions) into the database.
    /// </summary>
    /// <param name="result">The atomization result containing atoms and compositions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of atoms inserted</returns>
    Task<int> BulkInsertAsync(AtomizationResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk insert atoms with automatic deduplication based on ContentHash.
    /// Returns mapping of ContentHash → AtomId for composition linking.
    /// </summary>
    /// <param name="atoms">List of atoms to insert</param>
    /// <param name="tenantId">Tenant ID for multi-tenancy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping ContentHash (Base64) to AtomId</returns>
    Task<Dictionary<string, long>> BulkInsertAtomsAsync(
        List<AtomData> atoms,
        int tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bulk insert atom compositions (parent-child relationships).
    /// </summary>
    /// <param name="compositions">List of compositions to insert</param>
    /// <param name="atomIdMap">Mapping of ContentHash to AtomId</param>
    /// <param name="tenantId">Tenant ID for multi-tenancy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BulkInsertCompositionsAsync(
        List<AtomComposition> compositions,
        Dictionary<string, long> atomIdMap,
        int tenantId,
        CancellationToken cancellationToken);
}
