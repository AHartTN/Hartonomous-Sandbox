using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Services;

/// <summary>
/// Base class for background event processors using template method pattern.
/// Provides common lifecycle management, error handling, and logging.
/// Derived classes override ProcessBatchAsync to implement specific processing logic.
/// </summary>
public abstract class BaseEventProcessor
{
    protected readonly ILogger Logger;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _errorRetryDelay;

    protected BaseEventProcessor(
        ILogger logger,
        TimeSpan? pollInterval = null,
        TimeSpan? errorRetryDelay = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        _errorRetryDelay = errorRetryDelay ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Starts the processor's main loop. Template method that orchestrates initialization,
    /// processing, and shutdown.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting {ProcessorType}", GetType().Name);

        await OnStartingAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(cancellationToken);
                await Task.Delay(_pollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("{ProcessorType} stopping due to cancellation", GetType().Name);
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in {ProcessorType} processing loop", GetType().Name);
                await OnErrorAsync(ex, cancellationToken);
                await Task.Delay(_errorRetryDelay, cancellationToken);
            }
        }

        await OnStoppingAsync(cancellationToken);
        Logger.LogInformation("{ProcessorType} stopped", GetType().Name);
    }

    /// <summary>
    /// Override to implement specific batch processing logic.
    /// Called repeatedly in the main processing loop.
    /// </summary>
    protected abstract Task ProcessBatchAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Override to perform initialization before the processing loop starts.
    /// Default implementation does nothing.
    /// </summary>
    protected virtual Task OnStartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to perform cleanup after the processing loop ends.
    /// Default implementation does nothing.
    /// </summary>
    protected virtual Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to handle errors that occur during processing.
    /// Default implementation logs the error (already done by base class).
    /// </summary>
    protected virtual Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
