using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Inference;

public class GenerateTextRequest
{
    [Required]
    [StringLength(10000, MinimumLength = 1)]
    public required string Prompt { get; set; }

    [Range(1, 2048)]
    public int MaxTokens { get; set; } = 64;

    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.8;

    [Range(1, 100)]
    public int TopK { get; set; } = 6;

    public List<int>? ModelIds { get; set; }
    public bool StreamResponse { get; set; }
}

public class GenerateTextResponse
{
    public long InferenceId { get; set; }
    public Guid StreamId { get; set; }
    public required string OriginalPrompt { get; set; }
    public required string GeneratedText { get; set; }
    public int TokensGenerated { get; set; }
    public int DurationMs { get; set; }
    public List<TokenDetail>? Tokens { get; set; }
}

public class TokenDetail
{
    public int StepNumber { get; set; }
    public long? AtomId { get; set; }
    public string? Token { get; set; }
    public double Score { get; set; }
    public double Distance { get; set; }
    public int ModelCount { get; set; }
}
