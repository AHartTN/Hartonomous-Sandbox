namespace Hartonomous.Api.DTOs.Generation;

public sealed class TextGenerationResponse
{
    public long InferenceId { get; set; }
    public Guid StreamId { get; set; }
    public required string OriginalPrompt { get; set; }
    public required string GeneratedText { get; set; }
    public int TokensGenerated { get; set; }
    public int DurationMs { get; set; }
    public string? TokenDetails { get; set; }
}
