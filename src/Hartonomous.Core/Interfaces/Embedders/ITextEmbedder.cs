namespace Hartonomous.Core.Interfaces.Embedders;

public interface ITextEmbedder
{
    /// <summary>
    /// Gets the name of the embedding provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the dimensionality of the embeddings produced by this provider.
    /// </summary>
    int EmbeddingDimension { get; }

    /// <summary>
    /// Generate embeddings for the given text.
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vectors</returns>
    Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for image embedding providers.
/// </summary>
