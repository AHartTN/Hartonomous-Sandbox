using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hartonomous.Core.Pipelines;

/// <summary>
/// Execution context for pipeline operations with correlation tracking and telemetry.
/// Immutable context passed through all pipeline steps for observability.
/// </summary>
public interface IPipelineContext
{
    /// <summary>
    /// Unique correlation ID for distributed tracing (propagated across service boundaries).
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Parent trace activity for OpenTelemetry integration.
    /// </summary>
    Activity? TraceActivity { get; }

    /// <summary>
    /// Key-value metadata for context propagation (user ID, tenant ID, etc.).
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Timestamp when the pipeline context was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Creates a new context with additional properties (immutable pattern).
    /// </summary>
    IPipelineContext WithProperty(string key, object value);

    /// <summary>
    /// Creates a child context for nested pipeline execution.
    /// </summary>
    IPipelineContext CreateChild(string childActivityName);
}

/// <summary>
/// Default implementation of pipeline context with OpenTelemetry support.
/// </summary>
public sealed class PipelineContext : IPipelineContext
{
    public string CorrelationId { get; }
    public Activity? TraceActivity { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }
    public DateTime CreatedAt { get; }

    private PipelineContext(
        string correlationId,
        Activity? traceActivity,
        IReadOnlyDictionary<string, object> properties,
        DateTime createdAt)
    {
        CorrelationId = correlationId;
        TraceActivity = traceActivity;
        Properties = properties;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new root pipeline context with OpenTelemetry activity.
    /// </summary>
    /// <param name="activitySource">ActivitySource for distributed tracing.</param>
    /// <param name="activityName">Name of the trace activity.</param>
    /// <param name="correlationId">Optional correlation ID (generated if null).</param>
    public static PipelineContext Create(
        ActivitySource? activitySource = null,
        string activityName = "pipeline-execution",
        string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        var activity = activitySource?.StartActivity(activityName);

        activity?.SetTag("correlation_id", id);

        return new PipelineContext(
            id,
            activity,
            new Dictionary<string, object>(),
            DateTime.UtcNow);
    }

    public IPipelineContext WithProperty(string key, object value)
    {
        var newProperties = new Dictionary<string, object>(Properties)
        {
            [key] = value
        };

        return new PipelineContext(CorrelationId, TraceActivity, newProperties, CreatedAt);
    }

    public IPipelineContext CreateChild(string childActivityName)
    {
        // Create child activity linked to parent
        var childActivity = TraceActivity?.Source?.StartActivity(
            childActivityName,
            ActivityKind.Internal,
            TraceActivity.Context);

        childActivity?.SetTag("parent_correlation_id", CorrelationId);
        childActivity?.SetTag("correlation_id", CorrelationId); // Propagate same correlation ID

        return new PipelineContext(
            CorrelationId,
            childActivity,
            Properties, // Inherit parent properties
            DateTime.UtcNow);
    }
}
