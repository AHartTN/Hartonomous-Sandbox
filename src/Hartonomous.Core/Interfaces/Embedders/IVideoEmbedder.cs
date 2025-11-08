namespace Hartonomous.Core.Interfaces.Embedders;

public interface IVideoEmbedder
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
    /// Generate embeddings for the given video frame.
    /// </summary>
    /// <param name="frameBytes">The video frame bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> EmbedVideoFrameAsync(byte[] frameBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple video frames in batch.
    /// </summary>
    /// <param name="frameBytes">The video frame byte arrays</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vectors</returns>
    Task<float[][]> EmbedVideoFramesAsync(IEnumerable<byte[]> frameBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Unified interface for all embedding providers.
/// Allows treating different modality embedders uniformly.
/// </summary>
