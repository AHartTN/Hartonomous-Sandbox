using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for ILogger to provide consistent logging patterns.
/// Adds timing, operation tracking, and structured logging helpers.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs operation with automatic timing and scope tracking.
    /// Usage: using var operation = logger.BeginOperation("ProcessModel", modelId);
    /// Automatically logs start, duration, and errors.
    /// </summary>
    public static LogOperation BeginOperation(
        this ILogger logger,
        string operationName,
        params object?[] parameters)
    {
        return new LogOperation(logger, operationName, parameters);
    }

    /// <summary>
    /// Logs operation with explicit entity ID for correlation.
    /// </summary>
    public static LogOperation BeginOperation<TId>(
        this ILogger logger,
        string operationName,
        TId entityId,
        params object?[] parameters) where TId : notnull
    {
        var allParams = new object?[parameters.Length + 2];
        allParams[0] = "EntityId";
        allParams[1] = entityId;
        Array.Copy(parameters, 0, allParams, 2, parameters.Length);

        return new LogOperation(logger, operationName, allParams);
    }

    /// <summary>
    /// Logs error with operation context and structured exception data.
    /// </summary>
    public static void LogOperationError(
        this ILogger logger,
        Exception exception,
        string operationName,
        params object?[] parameters)
    {
        var state = new Dictionary<string, object?>
        {
            ["Operation"] = operationName,
            ["ExceptionType"] = exception.GetType().Name,
            ["Message"] = exception.Message
        };

        for (int i = 0; i < parameters.Length; i += 2)
        {
            if (i + 1 < parameters.Length)
            {
                state[parameters[i]?.ToString() ?? $"Param{i}"] = parameters[i + 1];
            }
        }

        logger.LogError(exception, "Operation {Operation} failed", operationName);
    }

    /// <summary>
    /// Logs performance metric with standardized structure.
    /// </summary>
    public static void LogPerformanceMetric(
        this ILogger logger,
        string metricName,
        double value,
        string unit = "ms",
        params object?[] context)
    {
        var parameters = new object?[context.Length + 6];
        parameters[0] = "Metric";
        parameters[1] = metricName;
        parameters[2] = "Value";
        parameters[3] = value;
        parameters[4] = "Unit";
        parameters[5] = unit;
        Array.Copy(context, 0, parameters, 6, context.Length);

        logger.LogInformation("Performance: {Metric}={Value}{Unit}", metricName, value, unit);
    }

    /// <summary>
    /// Logs progress for long-running operations.
    /// </summary>
    public static void LogProgress(
        this ILogger logger,
        string operationName,
        int current,
        int total,
        params object?[] additionalContext)
    {
        var percentage = total > 0 ? (current * 100 / total) : 0;

        logger.LogInformation(
            "Progress: {Operation} {Current}/{Total} ({Percentage}%)",
            operationName, current, total, percentage);
    }

    /// <summary>
    /// Logs structured data event (for analytics/telemetry).
    /// </summary>
    public static void LogDataEvent(
        this ILogger logger,
        string eventName,
        Dictionary<string, object?> properties)
    {
        using (logger.BeginScope(properties))
        {
            logger.LogInformation("Event: {EventName}", eventName);
        }
    }

    /// <summary>
    /// Logs cache hit/miss with key and source.
    /// </summary>
    public static void LogCacheAccess(
        this ILogger logger,
        string cacheKey,
        bool isHit,
        string? source = null)
    {
        if (isHit)
        {
            logger.LogDebug("Cache HIT: {CacheKey} (Source: {Source})", cacheKey, source ?? "Unknown");
        }
        else
        {
            logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Logs database query with timing (for performance analysis).
    /// </summary>
    public static void LogDatabaseQuery(
        this ILogger logger,
        string queryName,
        long durationMs,
        int? rowCount = null)
    {
        if (rowCount.HasValue)
        {
            logger.LogDebug(
                "Query: {QueryName} completed in {DurationMs}ms, returned {RowCount} rows",
                queryName, durationMs, rowCount.Value);
        }
        else
        {
            logger.LogDebug(
                "Query: {QueryName} completed in {DurationMs}ms",
                queryName, durationMs);
        }
    }
}

/// <summary>
/// Disposable wrapper for logging operation timing and outcome.
/// Automatically logs start, success/failure, and duration.
/// </summary>
public sealed class LogOperation : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly object?[] _parameters;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;
    private bool _completed;

    internal LogOperation(ILogger logger, string operationName, object?[] parameters)
    {
        _logger = logger;
        _operationName = operationName;
        _parameters = parameters;
        _stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Starting: {Operation}", _operationName);
    }

    /// <summary>
    /// Marks operation as successfully completed (prevents error logging on dispose).
    /// </summary>
    public void Complete()
    {
        _completed = true;
    }

    /// <summary>
    /// Marks operation as failed with exception.
    /// </summary>
    public void Fail(Exception exception)
    {
        _stopwatch.Stop();

        _logger.LogError(
            exception,
            "Failed: {Operation} after {DurationMs}ms",
            _operationName,
            _stopwatch.ElapsedMilliseconds);

        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _stopwatch.Stop();

        if (_completed)
        {
            _logger.LogInformation(
                "Completed: {Operation} in {DurationMs}ms",
                _operationName,
                _stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Uncompleted: {Operation} after {DurationMs}ms (did you forget Complete()?)",
                _operationName,
                _stopwatch.ElapsedMilliseconds);
        }

        _disposed = true;
    }
}
