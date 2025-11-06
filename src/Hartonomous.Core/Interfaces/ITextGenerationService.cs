using Hartonomous.Core.ValueObjects;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides text generation capabilities using spatial search and vector embeddings.
/// Generates text by finding relevant context through spatial proximity and feeding it to SQL-based generation.
/// </summary>
public interface ITextGenerationService
{
    /// <summary>
    /// Generates text by invoking a spatial search powered stored procedure.
    /// Uses hybrid search to find context tokens, then generates text via sp_GenerateText.
    /// </summary>
    /// <param name="promptEmbedding">Embedding for the generation prompt.</param>
    /// <param name="maxTokens">Maximum tokens to produce (1-100).</param>
    /// <param name="temperature">Generation temperature value (0.0-2.0, higher = more creative).</param>
    /// <param name="cancellationToken">Token to cancel SQL execution.</param>
    /// <returns>Generation result with produced text, token IDs, and confidence scores.</returns>
    Task<GenerationResult> GenerateViaSpatialAsync(
        float[] promptEmbedding,
        int maxTokens = 50,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default);
}
