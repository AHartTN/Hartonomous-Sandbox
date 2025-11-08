namespace Hartonomous.Core.Interfaces.Embedders;

public interface IImageEmbedder
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
    /// Generate embeddings for the given image.
    /// </summary>
    /// <param name="imageBytes">The image bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> EmbedImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple images in batch.
    /// </summary>
    /// <param name="imageBytes">The image byte arrays</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vectors</returns>
    Task<float[][]> EmbedImagesAsync(IEnumerable<byte[]> imageBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for audio embedding providers.
/// </summary>
