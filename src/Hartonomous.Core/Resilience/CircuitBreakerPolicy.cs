using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Resilience;

public sealed class CircuitBreakerPolicy : ICircuitBreakerPolicy
{
    private readonly CircuitBreakerOptions _options;
    private readonly ITransientErrorDetector _errorDetector;
    private readonly ILogger<CircuitBreakerPolicy> _logger;

    private int _failureCount;
    private int _halfOpenSuccessCount;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTime _openUntilUtc;
    private bool _halfOpenProbeActive;
    private readonly object _lock = new();

    public CircuitBreakerPolicy(
        CircuitBreakerOptions options,
        ITransientErrorDetector errorDetector,
        ILogger<CircuitBreakerPolicy> logger)
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

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureCanExecute();

            try
            {
                var result = await operation(cancellationToken).ConfigureAwait(false);
                OnSuccess();
                return result;
            }
            catch (Exception ex) when (_errorDetector.IsTransient(ex))
            {
                OnFailure(ex);
                throw;
            }
        }
    }

    private void EnsureCanExecute()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow < _openUntilUtc)
                {
                    throw new CircuitBreakerOpenException();
                }

                _state = CircuitBreakerState.HalfOpen;
                _halfOpenSuccessCount = 0;
                _halfOpenProbeActive = false;
            }
            else if (_state == CircuitBreakerState.HalfOpen && _halfOpenSuccessCount >= _options.HalfOpenSuccessThreshold)
            {
                TransitionToClosed();
            }

            if (_state == CircuitBreakerState.HalfOpen)
            {
                if (_halfOpenProbeActive)
                {
                    throw new CircuitBreakerOpenException();
                }

                _halfOpenProbeActive = true;
            }
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _halfOpenSuccessCount++;
                if (_halfOpenSuccessCount >= _options.HalfOpenSuccessThreshold)
                {
                    TransitionToClosed();
                }
                else
                {
                    _halfOpenProbeActive = false;
                }
            }
            else
            {
                _failureCount = 0;
            }
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;

            if (_state == CircuitBreakerState.Closed && _failureCount >= _options.FailureThreshold)
            {
                TransitionToOpen(exception);
            }
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionToOpen(exception);
            }
        }
    }

    private void TransitionToClosed()
    {
        _logger.LogInformation("Circuit breaker closed");
        _state = CircuitBreakerState.Closed;
        _failureCount = 0;
        _halfOpenSuccessCount = 0;
        _halfOpenProbeActive = false;
    }

    private void TransitionToOpen(Exception exception)
    {
        _state = CircuitBreakerState.Open;
        _openUntilUtc = DateTime.UtcNow.Add(_options.BreakDuration);
        _failureCount = 0;
        _halfOpenSuccessCount = 0;
        _halfOpenProbeActive = false;
        _logger.LogWarning(exception, "Circuit breaker opened for {Duration}", _options.BreakDuration);
    }
}

internal enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
