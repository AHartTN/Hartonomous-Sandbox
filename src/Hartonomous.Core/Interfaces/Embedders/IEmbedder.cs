namespace Hartonomous.Core.Interfaces.Embedders;

public interface IEmbedder
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
    /// Gets the modality this embedder handles.
    /// </summary>
    string Modality { get; }

    /// <summary>
    /// Generate embeddings for the given input.
    /// </summary>
    /// <param name="input">The input data (text string or byte array)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> EmbedAsync(object input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple inputs in batch.
    /// </summary>
    /// <param name="inputs">The input data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vectors</returns>
    Task<float[][]> EmbedBatchAsync(IEnumerable<object> inputs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
