namespace Hartonomous.Api.DTOs;

/// <summary>
/// Response model for text generation results.
/// </summary>
public record GenerationResponse(
    /// <summary>
    /// Generated text output.
    /// </summary>
    string GeneratedText,

    /// <summary>
    /// Number of tokens generated.
    /// </summary>
    int TokenCount,

    /// <summary>
    /// Average confidence score across all generated tokens.
    /// </summary>
    float AverageConfidence,

    /// <summary>
    /// Inference request ID for tracking.
    /// </summary>
    long InferenceId
);
