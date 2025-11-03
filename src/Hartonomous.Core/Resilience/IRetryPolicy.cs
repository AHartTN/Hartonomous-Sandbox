using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Resilience;

public interface IRetryPolicy
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
