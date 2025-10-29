using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// Represents a single change event from CDC.
/// </summary>
public class CdcChangeEvent
{
    /// <summary>
    /// Gets or sets the LSN of the change.
    /// </summary>
    public string Lsn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type (1=Delete, 2=Insert, 3=Update before, 4=Update after).
    /// </summary>
    public int Operation { get; set; }

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the change data as a dictionary of column names to values.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}