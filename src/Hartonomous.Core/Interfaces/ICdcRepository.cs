using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository interface for Change Data Capture (CDC) operations.
/// Provides access to SQL Server 2025 Change Event Streaming data.
/// </summary>
public interface ICdcRepository
{
    /// <summary>
    /// Gets change events from CDC tables since the specified LSN.
    /// </summary>
    /// <param name="lastLsn">The last processed LSN, or null to get all events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of change events with their data.</returns>
    Task<IList<CdcChangeEvent>> GetChangeEventsSinceAsync(string? lastLsn, CancellationToken cancellationToken);
}