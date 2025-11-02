using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Abstraction for consuming events from messaging infrastructure
/// </summary>
public interface IEventConsumer
{
    /// <summary>
    /// Starts event consumption with a handler</summary>
    Task StartAsync(Func<object, CancellationToken, Task> eventHandler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops event consumption
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
