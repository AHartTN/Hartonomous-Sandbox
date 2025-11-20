using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Base class for all services providing common infrastructure:
/// logging, telemetry, validation, and error handling patterns.
/// </summary>
/// <typeparam name="TService">The concrete service type (for logging category).</typeparam>
public abstract class ServiceBase<TService> where TService : class
{
    /// <summary>
    /// Gets the logger instance for this service.
    /// </summary>
    protected ILogger<TService> Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBase{TService}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected ServiceBase(ILogger<TService> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with comprehensive error handling, logging, and telemetry.
    /// Template method pattern for consistent cross-cutting concerns.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="operationName">The name of the operation for logging/telemetry.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    protected async Task<TResult> ExecuteWithTelemetryAsync<TResult>(
        string operationName,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            Logger.LogInformation(
                "[{ServiceName}] Starting operation: {OperationName}",
                typeof(TService).Name,
                operationName);

            // Execute the operation
            var result = await operation().ConfigureAwait(false);

            stopwatch.Stop();

            Logger.LogInformation(
                "[{ServiceName}] Completed operation: {OperationName} in {ElapsedMs}ms",
                typeof(TService).Name,
                operationName,
                stopwatch.ElapsedMilliseconds);

            // Hook for custom telemetry (override in derived classes)
            await OnOperationCompletedAsync(operationName, stopwatch.ElapsedMilliseconds, true, cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            Logger.LogWarning(
                "[{ServiceName}] Operation cancelled: {OperationName} after {ElapsedMs}ms",
                typeof(TService).Name,
                operationName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(
                ex,
                "[{ServiceName}] Operation failed: {OperationName} after {ElapsedMs}ms - {ErrorMessage}",
                typeof(TService).Name,
                operationName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            // Hook for custom telemetry (override in derived classes)
            await OnOperationFailedAsync(operationName, stopwatch.ElapsedMilliseconds, ex, cancellationToken)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Called when an operation completes successfully.
    /// Override to add custom telemetry (e.g., Application Insights custom metrics).
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual Task OnOperationCompletedAsync(
        string operationName,
        long elapsedMilliseconds,
        bool success,
        CancellationToken cancellationToken = default)
    {
        // Default implementation: no-op
        // Override in derived classes to add Application Insights custom events/metrics
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an operation fails with an exception.
    /// Override to add custom telemetry (e.g., Application Insights exception tracking).
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual Task OnOperationFailedAsync(
        string operationName,
        long elapsedMilliseconds,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        // Default implementation: no-op
        // Override in derived classes to add Application Insights exception tracking
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates input parameters and throws ArgumentException if validation fails.
    /// Override in derived classes to add domain-specific validation.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="validationFunc">The validation function that returns error message on failure, null on success.</param>
    protected void ValidateParameter(string parameterName, object? value, Func<object?, string?> validationFunc)
    {
        var errorMessage = validationFunc(value);
        if (errorMessage != null)
        {
            Logger.LogWarning(
                "[{ServiceName}] Parameter validation failed: {ParameterName} - {ErrorMessage}",
                typeof(TService).Name,
                parameterName,
                errorMessage);

            throw new ArgumentException(errorMessage, parameterName);
        }
    }

    /// <summary>
    /// Validates a string parameter is not null or whitespace.
    /// </summary>
    protected void ValidateNotNullOrWhiteSpace(string? value, string parameterName)
    {
        if (value == null)
        {
            Logger.LogWarning(
                "[{ServiceName}] Parameter validation failed: {ParameterName} cannot be null",
                typeof(TService).Name,
                parameterName);

            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            Logger.LogWarning(
                "[{ServiceName}] Parameter validation failed: {ParameterName} cannot be empty or whitespace",
                typeof(TService).Name,
                parameterName);

            throw new ArgumentException($"{parameterName} cannot be empty or whitespace.", parameterName);
        }
    }

    /// <summary>
    /// Validates a numeric parameter is within a specified range.
    /// </summary>
    protected void ValidateRange(long value, string parameterName, long min, long max)
    {
        if (value < min || value > max)
        {
            Logger.LogWarning(
                "[{ServiceName}] Parameter validation failed: {ParameterName} must be between {Min} and {Max}, but was {Value}",
                typeof(TService).Name,
                parameterName,
                min,
                max,
                value);

            throw new ArgumentOutOfRangeException(parameterName, value,
                $"{parameterName} must be between {min} and {max}, but was {value}.");
        }
    }
}
