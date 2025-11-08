namespace Hartonomous.Api.DTOs.Generation;

public class GenerationResponse
{
    /// <summary>Generation job ID for async tracking</summary>
    public required long JobId { get; init; }

    /// <summary>Prompt used for generation</summary>
    public required string Prompt { get; init; }

    /// <summary>Content type: 'text', 'image', 'audio', 'video'</summary>
    public required string ContentType { get; init; }

    /// <summary>Generation status: 'pending', 'generating', 'completed', 'failed'</summary>
    public required string Status { get; init; }

    /// <summary>Generated content (text) or reference ID (image/audio/video)</summary>
    public string? GeneratedContent { get; init; }

    /// <summary>Atom ID if content was stored</summary>
    public long? AtomId { get; init; }

    /// <summary>Model IDs used for generation</summary>
    public required List<int> ModelIds { get; init; }

    /// <summary>Generation metadata (dimensions, duration, etc.)</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>Timestamp when generation started</summary>
    public required DateTime StartedAt { get; init; }

    /// <summary>Timestamp when generation completed</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Error message if generation failed</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Job status for async generation tracking
/// </summary>
