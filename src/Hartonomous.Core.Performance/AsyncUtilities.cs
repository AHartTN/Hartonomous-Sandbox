using System.Runtime.CompilerServices;

namespace Hartonomous.Core.Performance;

/// <summary>
/// Extensions and utilities for high-performance async operations.
/// Optimized for ValueTask, ConfigureAwait, and zero-allocation patterns.
/// </summary>
public static class AsyncUtilities
{
    /// <summary>
    /// Execute async operation with timeout and cancellation.
    /// </summary>
    public static async ValueTask<T> WithTimeout<T>(
        this ValueTask<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await task.AsTask().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Execute async operation with timeout (no cancellation).
    /// </summary>
    public static async ValueTask<T> WithTimeout<T>(this ValueTask<T> task, TimeSpan timeout)
    {
        var delayTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task.AsTask(), delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalMilliseconds}ms");
        }

        return await task;
    }

    /// <summary>
    /// Execute multiple ValueTasks in parallel and return results.
    /// More efficient than Task.WhenAll for ValueTask.
    /// </summary>
    public static async ValueTask<T[]> WhenAll<T>(params ValueTask<T>[] tasks)
    {
        if (tasks.Length == 0) return Array.Empty<T>();

        // Fast path for single task
        if (tasks.Length == 1)
        {
            return new[] { await tasks[0] };
        }

        // Convert to Task[] for parallel execution
        var taskArray = new Task<T>[tasks.Length];
        for (int i = 0; i < tasks.Length; i++)
        {
            taskArray[i] = tasks[i].AsTask();
        }

        return await Task.WhenAll(taskArray);
    }

    /// <summary>
    /// Execute multiple ValueTasks in parallel (no result).
    /// </summary>
    public static async ValueTask WhenAll(params ValueTask[] tasks)
    {
        if (tasks.Length == 0) return;

        if (tasks.Length == 1)
        {
            await tasks[0];
            return;
        }

        var taskArray = new Task[tasks.Length];
        for (int i = 0; i < tasks.Length; i++)
        {
            taskArray[i] = tasks[i].AsTask();
        }

        await Task.WhenAll(taskArray);
    }

    /// <summary>
    /// Retry async operation with exponential backoff.
    /// </summary>
    public static async ValueTask<T> RetryWithBackoff<T>(
        Func<ValueTask<T>> operation,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {maxRetries + 1} attempts",
            lastException);
    }

    /// <summary>
    /// Cache the result of an async operation (memoization).
    /// Thread-safe lazy initialization.
    /// </summary>
    public static Func<ValueTask<T>> Memoize<T>(Func<ValueTask<T>> factory)
    {
        var lazy = new Lazy<Task<T>>(() => factory().AsTask());
        return () => new ValueTask<T>(lazy.Value);
    }

    /// <summary>
    /// Execute async operation with circuit breaker pattern.
    /// </summary>
    public static async ValueTask<T> WithCircuitBreaker<T>(
        this ValueTask<T> task,
        CircuitBreakerState state)
    {
        if (state.IsOpen)
        {
            throw new InvalidOperationException("Circuit breaker is open");
        }

        try
        {
            var result = await task;
            state.RecordSuccess();
            return result;
        }
        catch (Exception)
        {
            state.RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Convert IAsyncEnumerable to List efficiently.
    /// </summary>
    public static async ValueTask<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// Convert IAsyncEnumerable to array efficiently.
    /// </summary>
    public static async ValueTask<T[]> ToArrayAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = await ToListAsync(source, cancellationToken);
        return list.ToArray();
    }

    /// <summary>
    /// Execute async operation and suppress exceptions (returns default on error).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<T?> SuppressExceptions<T>(
        this ValueTask<T> task,
        T? defaultValue = default)
    {
        try
        {
            return await task;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Fire and forget async operation with exception logging.
    /// </summary>
    public static void FireAndForget(
        this ValueTask task,
        Action<Exception>? errorHandler = null)
    {
        _ = task.AsTask().ContinueWith(t =>
        {
            if (t.IsFaulted && errorHandler != null)
            {
                errorHandler(t.Exception!.GetBaseException());
            }
        }, TaskScheduler.Default);
    }
}

/// <summary>
/// Circuit breaker state for fault tolerance.
/// Thread-safe implementation.
/// </summary>
public sealed class CircuitBreakerState
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private readonly object _lock = new();

    public CircuitBreakerState(int failureThreshold = 5, TimeSpan? resetTimeout = null)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout ?? TimeSpan.FromMinutes(1);
    }

    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                // Auto-reset after timeout
                if (_failureCount >= _failureThreshold &&
                    DateTime.UtcNow - _lastFailureTime > _resetTimeout)
                {
                    _failureCount = 0;
                }

                return _failureCount >= _failureThreshold;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
        }
    }
}

/// <summary>
/// Async lazy initialization with thread safety.
/// </summary>
public sealed class AsyncLazy<T>
{
    private readonly Lazy<Task<T>> _lazy;

    public AsyncLazy(Func<Task<T>> factory)
    {
        _lazy = new Lazy<Task<T>>(factory);
    }

    public AsyncLazy(Func<ValueTask<T>> factory)
    {
        _lazy = new Lazy<Task<T>>(() => factory().AsTask());
    }

    public ValueTask<T> GetValueAsync() => new(_lazy.Value);

    public bool IsValueCreated => _lazy.IsValueCreated;
}

/// <summary>
/// Rate limiter for async operations.
/// Token bucket algorithm.
/// </summary>
public sealed class AsyncRateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private readonly Timer _refillTimer;
    private int _availableTokens;
    private readonly object _lock = new();

    public AsyncRateLimiter(int maxTokens, TimeSpan refillInterval)
    {
        _maxTokens = maxTokens;
        _availableTokens = maxTokens;
        _refillInterval = refillInterval;
        _semaphore = new SemaphoreSlim(maxTokens, maxTokens);

        _refillTimer = new Timer(
            _ => RefillTokens(),
            null,
            refillInterval,
            refillInterval);
    }

    public async ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        lock (_lock)
        {
            _availableTokens--;
        }

        return new RateLimitToken(_semaphore);
    }

    private void RefillTokens()
    {
        lock (_lock)
        {
            int tokensToAdd = _maxTokens - _availableTokens;
            if (tokensToAdd > 0)
            {
                _semaphore.Release(tokensToAdd);
                _availableTokens = _maxTokens;
            }
        }
    }

    public void Dispose()
    {
        _refillTimer?.Dispose();
        _semaphore?.Dispose();
    }

    private sealed class RateLimitToken : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public RateLimitToken(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            // Token is returned in RefillTokens, not here
        }
    }
}
