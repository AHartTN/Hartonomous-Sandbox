namespace Hartonomous.Api.DTOs.Generation;

public abstract class GenerationRequestBase
{
    /// <summary>Text prompt describing what to generate</summary>
    public required string Prompt { get; init; }

    /// <summary>Optional: Specific model IDs to use (comma-separated)</summary>
    public string? ModelIds { get; init; }

    /// <summary>Top-K sampling for model selection</summary>
    public int TopK { get; init; } = 5;
}
