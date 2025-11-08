namespace Hartonomous.Core.Interfaces.Embedders;

public interface IAudioEmbedder
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
    /// Generate embeddings for the given audio.
    /// </summary>
    /// <param name="audioBytes">The audio bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> EmbedAudioAsync(byte[] audioBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple audio samples in batch.
    /// </summary>
    /// <param name="audioBytes">The audio byte arrays</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vectors</returns>
    Task<float[][]> EmbedAudiosAsync(IEnumerable<byte[]> audioBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for video embedding providers.
/// </summary>
