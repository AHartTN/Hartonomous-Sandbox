using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Abstract base class for embedding storage providers.
/// Provides common functionality and error handling.
/// </summary>
public abstract class BaseEmbeddingStorageProvider : BaseService, IEmbeddingStorageProvider
{
    protected BaseEmbeddingStorageProvider(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }

    public abstract Task<long> StoreEmbeddingAsync(
        float[] embedding,
        object sourceData,
        string sourceType,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    public virtual async Task<long[]> StoreEmbeddingsAsync(
        IEnumerable<(float[] embedding, object sourceData, string sourceType, string? metadata)> embeddings,
        CancellationToken cancellationToken = default)
    {
        var results = new List<long>();
        foreach (var (embedding, sourceData, sourceType, metadata) in embeddings)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var id = await StoreEmbeddingAsync(embedding, sourceData, sourceType, metadata, cancellationToken);
                results.Add(id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to store embedding for source type: {SourceType}", sourceType);
                // Continue with other embeddings
            }
        }
        return results.ToArray();
    }

    public abstract Task<EmbeddingData?> GetEmbeddingAsync(long embeddingId, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<EmbeddingSearchResult>> ExactSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<EmbeddingSearchResult>> ApproximateSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default);

    public virtual async Task<IReadOnlyList<EmbeddingSearchResult>> HybridSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        // Default implementation: try approximate search first, fall back to exact
        try
        {
            var approxResults = await ApproximateSearchAsync(queryEmbedding, topK * 2, cancellationToken);
            if (approxResults.Count >= topK)
            {
                return approxResults.Take(topK).ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Approximate search failed, falling back to exact search");
        }

        return await ExactSearchAsync(queryEmbedding, topK, cancellationToken);
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to store and retrieve a test embedding
            var testEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var testId = await StoreEmbeddingAsync(testEmbedding, "test", "test", null, cancellationToken);
            var retrieved = await GetEmbeddingAsync(testId, cancellationToken);
            return retrieved != null;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for model storage providers.
/// </summary>
public abstract class BaseModelStorageProvider : BaseService, IModelStorageProvider
{
    protected BaseModelStorageProvider(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }

    public abstract Task<int> StoreModelAsync(Model model, CancellationToken cancellationToken = default);

    public abstract Task<Model?> GetModelAsync(int modelId, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<Model>> GetAllModelsAsync(CancellationToken cancellationToken = default);

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to get all models
            var models = await GetAllModelsAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for atomic storage providers.
/// </summary>
public abstract class BaseAtomicStorageProvider : BaseService, IAtomicStorageProvider
{
    protected BaseAtomicStorageProvider(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }

    public abstract Task<long> StorePixelAsync(byte r, byte g, byte b, byte a, CancellationToken cancellationToken = default);

    public abstract Task<long> StoreAudioSampleAsync(float sample, CancellationToken cancellationToken = default);

    public abstract Task<long> StoreTextTokenAsync(string token, string? metadata = null, CancellationToken cancellationToken = default);

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to store test data
            await StorePixelAsync(255, 0, 0, 255, cancellationToken);
            await StoreAudioSampleAsync(0.5f, cancellationToken);
            await StoreTextTokenAsync("test", null, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for message publishers.
/// </summary>
public abstract class BaseMessagePublisher : BaseService, IMessagePublisher
{
    protected BaseMessagePublisher(ILogger logger) : base(logger) { }

    public abstract string PublisherName { get; }

    public abstract Task PublishAsync(object message, CancellationToken cancellationToken = default);

    public virtual async Task PublishBatchAsync(IEnumerable<object> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await PublishAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to publish message of type: {Type}", message.GetType().Name);
                // Continue with other messages
            }
        }
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to publish a test message
            var testMessage = new { Test = true, Timestamp = DateTimeOffset.UtcNow };
            await PublishAsync(testMessage, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for model downloaders.
/// </summary>
public abstract class BaseModelDownloader : BaseService, IModelDownloader
{
    protected BaseModelDownloader(ILogger logger) : base(logger) { }

    public abstract string DownloaderName { get; }

    public abstract Task<string> DownloadModelAsync(string modelIdentifier, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<string>> GetAvailableModelsAsync(string? filter = null, CancellationToken cancellationToken = default);

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to get available models
            var models = await GetAvailableModelsAsync(null, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for model format readers.
/// </summary>
public abstract class BaseModelFormatReader : BaseService, IModelFormatReader
{
    protected BaseModelFormatReader(ILogger logger) : base(logger) { }

    public abstract string ReaderName { get; }
    public abstract IReadOnlyList<string> SupportedExtensions { get; }

    public abstract Task<bool> CanReadAsync(string modelPath, CancellationToken cancellationToken = default);

    public abstract Task<Model> ReadModelAsync(string modelPath, CancellationToken cancellationToken = default);

    public virtual async Task<bool> ValidateAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await CanReadAsync(modelPath, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Model readers are typically always available since they don't depend on external services
        return await Task.FromResult(true);
    }
}