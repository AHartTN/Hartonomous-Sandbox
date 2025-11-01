namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents an inference request for auditing and performance monitoring.
/// </summary>
public class InferenceRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the inference request.
    /// </summary>
    public long InferenceId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request was received.
    /// </summary>
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the type of inference task (e.g., 'text-generation', 'embedding', 'classification').
    /// </summary>
    public string? TaskType { get; set; }

    /// <summary>
    /// Gets or sets the input data as JSON (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the input data for deduplication.
    /// </summary>
    public byte[]? InputHash { get; set; }

    /// <summary>
    /// Gets or sets the models used in this inference as a JSON array (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? ModelsUsed { get; set; }

    /// <summary>
    /// Gets or sets the ensemble strategy used (e.g., 'weighted_average', 'voting', 'cascading').
    /// </summary>
    public string? EnsembleStrategy { get; set; }

    /// <summary>
    /// Gets or sets the output data as JSON (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the output metadata as JSON (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? OutputMetadata { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the inference in milliseconds.
    /// </summary>
    public int? TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the result was served from cache.
    /// </summary>
    public bool CacheHit { get; set; } = false;

    /// <summary>
    /// Gets or sets the user rating (0-5) for the inference result.
    /// </summary>
    public byte? UserRating { get; set; }

    /// <summary>
    /// Gets or sets the user's textual feedback on the inference result.
    /// </summary>
    public string? UserFeedback { get; set; }

    /// <summary>
    /// Gets or sets the collection of detailed steps executed during this inference.
    /// </summary>
    public ICollection<InferenceStep> Steps { get; set; } = new List<InferenceStep>();
}
