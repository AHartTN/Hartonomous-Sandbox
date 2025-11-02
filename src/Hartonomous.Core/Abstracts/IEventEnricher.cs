using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Abstraction for enriching events with additional metadata and semantic information
/// </summary>
public interface IEventEnricher
{
    /// <summary>
    /// Enriches a single event with semantic metadata
    /// </summary>
    Task EnrichAsync(BaseEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches a batch of events
    /// </summary>
    Task EnrichBatchAsync(IEnumerable<BaseEvent> events, CancellationToken cancellationToken = default);
}
