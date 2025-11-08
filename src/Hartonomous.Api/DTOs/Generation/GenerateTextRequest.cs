namespace Hartonomous.Api.DTOs.Generation;

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
