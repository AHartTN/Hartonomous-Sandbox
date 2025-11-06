namespace Hartonomous.Api.DTOs.Generation;

/// <summary>
/// Base request for all generation operations
/// </summary>
public abstract class GenerationRequestBase
{
    /// <summary>Text prompt describing what to generate</summary>
    public required string Prompt { get; init; }
    
    /// <summary>Optional: Specific model IDs to use (comma-separated)</summary>
    public string? ModelIds { get; init; }
    
    /// <summary>Top-K sampling for model selection</summary>
    public int TopK { get; init; } = 5;
}

/// <summary>
/// Request to generate text from a prompt
/// </summary>
public class GenerateTextRequest : GenerationRequestBase
{
    /// <summary>Maximum tokens to generate</summary>
    public int MaxTokens { get; init; } = 64;
    
    /// <summary>Temperature for sampling (0.0 = deterministic, 1.0 = creative)</summary>
    public double Temperature { get; init; } = 0.8;
}

/// <summary>
/// Request to generate an image from a text prompt
/// </summary>
public class GenerateImageRequest : GenerationRequestBase
{
    /// <summary>Image width in pixels</summary>
    public int Width { get; init; } = 512;
    
    /// <summary>Image height in pixels</summary>
    public int Height { get; init; } = 512;
    
    /// <summary>Patch size for spatial diffusion</summary>
    public int PatchSize { get; init; } = 32;
    
    /// <summary>Number of diffusion steps</summary>
    public int Steps { get; init; } = 32;
    
    /// <summary>Guidance scale for prompt adherence</summary>
    public double GuidanceScale { get; init; } = 6.5;
    
    /// <summary>Output format: 'patches', 'binary', 'geometry'</summary>
    public string OutputFormat { get; init; } = "patches";
}

/// <summary>
/// Request to generate audio from a text prompt
/// </summary>
public class GenerateAudioRequest : GenerationRequestBase
{
    /// <summary>Target duration in milliseconds</summary>
    public int TargetDurationMs { get; init; } = 5000;
    
    /// <summary>Sample rate in Hz</summary>
    public int SampleRate { get; init; } = 44100;
    
    /// <summary>Temperature for sampling</summary>
    public double Temperature { get; init; } = 0.6;
}

/// <summary>
/// Request to generate video from a text prompt
/// </summary>
public class GenerateVideoRequest : GenerationRequestBase
{
    /// <summary>Target duration in milliseconds</summary>
    public int TargetDurationMs { get; init; } = 4000;
    
    /// <summary>Target frames per second</summary>
    public int TargetFps { get; init; } = 24;
}

/// <summary>
/// Response containing generated content
/// </summary>
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
public class GenerationJobStatus
{
    public required long JobId { get; init; }
    public required string Status { get; init; }
    public required string ContentType { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ResultUrl { get; init; }
    public string? ErrorMessage { get; init; }
}
