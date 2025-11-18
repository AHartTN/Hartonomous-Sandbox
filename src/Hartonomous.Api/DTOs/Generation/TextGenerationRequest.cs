namespace Hartonomous.Api.DTOs.Generation;

public sealed class TextGenerationRequest
{
    public required string Prompt { get; set; }
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public int? TopK { get; set; }
    public string? ModelIds { get; set; }
}
