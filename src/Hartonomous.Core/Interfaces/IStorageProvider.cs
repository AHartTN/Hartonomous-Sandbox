using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Vendor-agnostic interface for embedding storage providers.
/// Allows swapping between different storage backends (SQL Server, PostgreSQL, Redis, etc.).
/// </summary>
public interface IEmbeddingStorageProvider
{
    /// <summary>
    /// Gets the name of the storage provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Store an embedding with its metadata.
    /// </summary>
    /// <param name="embedding">The embedding vector</param>
    /// <param name="sourceData">The original source data</param>
    /// <param name="sourceType">The type of source data</param>
    /// <param name="metadata">Optional metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The embedding ID</returns>
    Task<long> StoreEmbeddingAsync(
        float[] embedding,
        object sourceData,
        string sourceType,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Store multiple embeddings in batch.
    /// </summary>
    /// <param name="embeddings">The embedding data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The embedding IDs</returns>
    Task<long[]> StoreEmbeddingsAsync(
        IEnumerable<(float[] embedding, object sourceData, string sourceType, string? metadata)> embeddings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve an embedding by ID.
    /// </summary>
    /// <param name="embeddingId">The embedding ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The embedding data</returns>
    Task<EmbeddingData?> GetEmbeddingAsync(long embeddingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar embeddings using exact vector search.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding</param>
    /// <param name="topK">Number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    Task<IReadOnlyList<EmbeddingSearchResult>> ExactSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar embeddings using approximate/spatial search.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding</param>
    /// <param name="topK">Number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    Task<IReadOnlyList<EmbeddingSearchResult>> ApproximateSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform hybrid search (approximate + exact rerank).
    /// </summary>
    /// <param name="queryEmbedding">The query embedding</param>
    /// <param name="topK">Number of final results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    Task<IReadOnlyList<EmbeddingSearchResult>> HybridSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the storage provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for model storage providers.
/// </summary>
public interface IModelStorageProvider
{
    /// <summary>
    /// Gets the name of the storage provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Store a model with its metadata and layers.
    /// </summary>
    /// <param name="model">The model data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The model ID</returns>
    Task<int> StoreModelAsync(Model model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a model by ID.
    /// </summary>
    /// <param name="modelId">The model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The model data</returns>
    Task<Model?> GetModelAsync(int modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All models</returns>
    Task<IReadOnlyList<Model>> GetAllModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the storage provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for atomic storage providers.
/// Handles content-addressable storage for deduplication.
/// </summary>
public interface IAtomicStorageProvider
{
    /// <summary>
    /// Gets the name of the storage provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Store atomic pixel data with deduplication.
    /// </summary>
    /// <param name="r">Red component</param>
    /// <param name="g">Green component</param>
    /// <param name="b">Blue component</param>
    /// <param name="a">Alpha component</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The pixel ID</returns>
    Task<long> StorePixelAsync(byte r, byte g, byte b, byte a, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store atomic audio sample with deduplication.
    /// </summary>
    /// <param name="sample">Audio sample value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sample ID</returns>
    Task<long> StoreAudioSampleAsync(float sample, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store atomic text token with deduplication.
    /// </summary>
    /// <param name="token">Text token</param>
    /// <param name="metadata">Optional metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The token ID</returns>
    Task<long> StoreTextTokenAsync(string token, string? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the storage provider is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents stored embedding data.
/// </summary>
public class EmbeddingData
{
    public long EmbeddingId { get; set; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public object SourceData { get; set; } = null!;
    public string SourceType { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Result from an embedding search operation.
/// </summary>
public class EmbeddingSearchResult
{
    public long EmbeddingId { get; set; }
    public float Distance { get; set; }
    public float SimilarityScore { get; set; }
    public object SourceData { get; set; } = null!;
    public string SourceType { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}