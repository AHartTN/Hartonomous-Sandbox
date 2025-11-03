using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Resilience;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly RetryPolicyOptions _options;
    private readonly ITransientErrorDetector _errorDetector;
    private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;

    public ExponentialBackoffRetryPolicy(
        RetryPolicyOptions options,
        ITransientErrorDetector errorDetector,
        ILogger<ExponentialBackoffRetryPolicy> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _errorDetector = errorDetector ?? throw new ArgumentNullException(nameof(errorDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        => ExecuteAsync(async ct =>
        {
            await operation(ct).ConfigureAwait(false);
            return true;
        }, cancellationToken);

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (_errorDetector.IsTransient(ex))
            {
                lastException = ex;
                if (attempt >= _options.MaxAttempts)
                {
                    break;
                }

                var delay = CalculateDelay(attempt);
                _logger.LogWarning(ex, "Transient failure on attempt {Attempt} of {MaxAttempts}. Retrying in {Delay}.", attempt, _options.MaxAttempts, delay);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry policy failed without capturing an exception.");
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var exponentialDelay = _options.BaseDelay.TotalMilliseconds * Math.Pow(_options.ExponentialFactor, attempt - 1);
        exponentialDelay = Math.Min(exponentialDelay, _options.MaxDelay.TotalMilliseconds);
    var jitterSample = RandomNumberGenerator.GetInt32(0, int.MaxValue) / (double)int.MaxValue;
    var jitter = exponentialDelay * _options.JitterFactor * (jitterSample - 0.5) * 2;
        var final = Math.Max(_options.BaseDelay.TotalMilliseconds, exponentialDelay + jitter);
        return TimeSpan.FromMilliseconds(final);
    }
}
