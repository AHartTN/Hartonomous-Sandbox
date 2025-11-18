using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Performance;

/// <summary>
/// Enterprise-grade performance monitoring service using OpenTelemetry metrics.
/// Provides structured timing, metrics collection, and distributed tracing integration.
/// </summary>
public class PerformanceMonitor : IDisposable
{
    private readonly Meter _meter;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<PerformanceMonitor> _logger;

    // Metrics
    private readonly Histogram<double> _operationDuration;
    private readonly Counter<long> _operationCount;
    private readonly Counter<long> _operationErrors;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _meter = new Meter("Hartonomous.Api.Database", "1.0.0");
        _activitySource = new ActivitySource("Hartonomous.Api.Database");

        // Define metrics
        _operationDuration = _meter.CreateHistogram<double>(
            "db_operation_duration_ms",
            description: "Duration of database operations in milliseconds",
            unit: "ms");

        _operationCount = _meter.CreateCounter<long>(
            "db_operation_total",
            description: "Total number of database operations");

        _operationErrors = _meter.CreateCounter<long>(
            "db_operation_errors_total",
            description: "Total number of database operation errors");
    }

    /// <summary>
    /// Monitors a database operation with automatic timing, metrics, and tracing.
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operationName">Name of the operation (e.g., "sql_graph_traverse")</param>
    /// <param name="operation">The async operation to monitor</param>
    /// <param name="tags">Additional tags for metrics and tracing</param>
    /// <returns>Operation result and timing information</returns>
    public async Task<OperationResult<T>> MonitorAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        params KeyValuePair<string, object?>[] tags)
    {
        var startTime = Stopwatch.GetTimestamp();
        var activityTags = new ActivityTagsCollection(tags);

        using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal, default(ActivityContext), activityTags);

        try
        {
            _logger.LogDebug("Starting database operation: {OperationName}", operationName);

            var result = await operation();

            var durationMs = GetElapsedMilliseconds(startTime);

            // Record metrics
            var metricTags = new TagList(tags);
            _operationDuration.Record(durationMs, metricTags);
            _operationCount.Add(1, metricTags);

            // Add timing to activity
            activity?.SetTag("duration_ms", durationMs);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Database operation completed: {OperationName} in {DurationMs:F2}ms",
                operationName, durationMs);

            return new OperationResult<T>(result, durationMs);
        }
        catch (Exception ex)
        {
            var durationMs = GetElapsedMilliseconds(startTime);

            // Record error metrics
            var errorTags = new TagList(tags);
            errorTags.Add("error", "true");
            errorTags.Add("error_type", ex.GetType().Name);
            _operationErrors.Add(1, errorTags);

            // Mark activity as failed
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", "true");
            activity?.SetTag("error_type", ex.GetType().Name);
            activity?.SetTag("duration_ms", durationMs);

            _logger.LogError(ex,
                "Database operation failed: {OperationName} after {DurationMs:F2}ms",
                operationName, durationMs);

            throw;
        }
    }

    /// <summary>
    /// Monitors a synchronous database operation.
    /// </summary>
    public OperationResult<T> Monitor<T>(
        string operationName,
        Func<T> operation,
        params KeyValuePair<string, object?>[] tags)
    {
        var startTime = Stopwatch.GetTimestamp();
        var activityTags = new ActivityTagsCollection(tags);

        using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal, default(ActivityContext), activityTags);

        try
        {
            _logger.LogDebug("Starting database operation: {OperationName}", operationName);

            var result = operation();

            var durationMs = GetElapsedMilliseconds(startTime);

            // Record metrics
            var metricTags = new TagList(tags);
            _operationDuration.Record(durationMs, metricTags);
            _operationCount.Add(1, metricTags);

            // Add timing to activity
            activity?.SetTag("duration_ms", durationMs);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Database operation completed: {OperationName} in {DurationMs:F2}ms",
                operationName, durationMs);

            return new OperationResult<T>(result, durationMs);
        }
        catch (Exception ex)
        {
            var durationMs = GetElapsedMilliseconds(startTime);

            // Record error metrics
            var errorTags = new TagList(tags);
            errorTags.Add("error", "true");
            errorTags.Add("error_type", ex.GetType().Name);
            _operationErrors.Add(1, errorTags);

            // Mark activity as failed
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", "true");
            activity?.SetTag("error_type", ex.GetType().Name);
            activity?.SetTag("duration_ms", durationMs);

            _logger.LogError(ex,
                "Database operation failed: {OperationName} after {DurationMs:F2}ms",
                operationName, durationMs);

            throw;
        }
    }

    private static double GetElapsedMilliseconds(long startTimestamp)
    {
        var endTimestamp = Stopwatch.GetTimestamp();
        return (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }
}

/// <summary>
/// Result of a monitored operation with timing information.
/// </summary>
public readonly struct OperationResult<T>
{
    public T Result { get; }
    public double DurationMs { get; }

    public OperationResult(T result, double durationMs)
    {
        Result = result;
        DurationMs = durationMs;
    }

    public void Deconstruct(out T result, out double durationMs)
    {
        result = Result;
        durationMs = DurationMs;
    }
}
