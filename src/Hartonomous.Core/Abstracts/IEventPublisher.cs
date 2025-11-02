using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Abstraction for publishing events to messaging infrastructure
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single event
    /// </summary>
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Publishes multiple events in batches
    /// </summary>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : class;
}
