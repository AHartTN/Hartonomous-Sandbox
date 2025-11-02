using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Manages CDC checkpoint state for resumable processing
/// </summary>
public interface ICdcCheckpointManager
{
    /// <summary>
    /// Gets the last successfully processed LSN
    /// </summary>
    Task<string?> GetLastProcessedLsnAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last successfully processed LSN
    /// </summary>
    Task UpdateLastProcessedLsnAsync(string lsn, CancellationToken cancellationToken = default);
}
