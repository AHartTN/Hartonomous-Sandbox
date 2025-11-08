using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Messaging;

public interface IServiceBrokerResilienceStrategy
{
    Task ExecutePublishAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteReceiveAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
