namespace Hartonomous.Core.Models;

/// <summary>
/// Represents the capabilities of an AI model derived from database metadata.
/// </summary>
public class ModelCapabilities
{
    public bool SupportsTextGeneration { get; init; }
    public bool SupportsImageGeneration { get; init; }
    public bool SupportsAudioGeneration { get; init; }
    public bool SupportsVideoGeneration { get; init; }
    public bool SupportsEmbeddings { get; init; }
    public bool SupportsVisionAnalysis { get; init; }
    public bool SupportsFunctionCalling { get; init; }
    public bool SupportsStreaming { get; init; }
    public string PrimaryModality { get; init; } = "text";
    public int MaxTokens { get; init; }
    public int MaxContextWindow { get; init; }
}
