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
}
