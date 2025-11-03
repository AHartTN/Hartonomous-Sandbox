namespace Hartonomous.Api.DTOs;

/// <summary>
/// Request model for text generation via spatial nearest neighbor interpolation.
/// </summary>
public record GenerationRequest(
    /// <summary>
    /// Prompt text to generate continuation from.
    /// </summary>
    string Prompt,

    /// <summary>
    /// Maximum number of tokens to generate.
    /// </summary>
    int MaxTokens = 50,

    /// <summary>
    /// Sampling temperature (0.0 = deterministic, 1.0 = creative).
    /// </summary>
    float Temperature = 0.7f
);
