using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Models;

/// <summary>
/// Base event model for all events in the Hartonomous platform.
/// Follows the CNCF CloudEvents v1.0 specification for interoperability and standardization.
/// </summary>
/// <remarks>
/// This class provides a standardized format for all events flowing through the system,
/// ensuring consistency across CDC events, model inference events, and other platform events.
/// The CloudEvents specification ensures compatibility with industry-standard event systems.
/// Specification: https://cloudevents.io/
/// </remarks>
public class BaseEvent
{
    /// <summary>
    /// CloudEvents specification version (always "1.0")
    /// </summary>
    public string SpecVersion { get; set; } = "1.0";

    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// URI identifying the context in which the event occurred
    /// </summary>
    public Uri Source { get; set; } = null!;

    /// <summary>
    /// Event type identifier (reverse DNS notation recommended)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Optional subject of the event in the context of the event producer
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Content type of the data attribute (e.g., "application/json")
    /// </summary>
    public string? DataContentType { get; set; } = "application/json";

    /// <summary>
    /// Optional URI of schema that the data adheres to
    /// </summary>
    public Uri? DataSchema { get; set; }

    /// <summary>
    /// Event payload
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Extension attributes as per CloudEvents spec
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new();
}
