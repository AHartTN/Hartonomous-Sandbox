using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Decorator that adds caching to IEmbeddingService.
/// Caches expensive search/retrieval operations, passes through generation and storage.
/// </summary>
public class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedEmbeddingService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    public CachedEmbeddingService(
        IEmbeddingService inner,
        ICacheService cache,
        ILogger<CachedEmbeddingService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    // Embedding generation - pass through (deterministic, cheap)
    public Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default)
        => _inner.EmbedTextAsync(text, cancellationToken);

    public Task<float[]> EmbedImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        => _inner.EmbedImageAsync(imageBytes, cancellationToken);

    public Task<float[]> EmbedAudioAsync(byte[] audioBytes, CancellationToken cancellationToken = default)
        => _inner.EmbedAudioAsync(audioBytes, cancellationToken);

    public Task<float[]> EmbedVideoFrameAsync(byte[] frameBytes, CancellationToken cancellationToken = default)
        => _inner.EmbedVideoFrameAsync(frameBytes, cancellationToken);

    // Storage operations - pass through (writes should not be cached)
    public Task<long> StoreEmbeddingAsync(
        float[] embedding,
        object sourceData,
        string sourceType,
        string? metadata = null,
        CancellationToken cancellationToken = default)
        => _inner.StoreEmbeddingAsync(embedding, sourceData, sourceType, metadata, cancellationToken);

    public Task<(long embeddingId, float[] embedding)> GenerateAndStoreAsync(
        object input,
        string inputType,
        string? metadata = null,
        CancellationToken cancellationToken = default)
        => _inner.GenerateAndStoreAsync(input, inputType, metadata, cancellationToken);

    // Zero-shot classification - CACHED (expensive)
    public async Task<Dictionary<string, float>> ZeroShotClassifyAsync(
        byte[] imageBytes,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default)
    {
        var imageHash = ComputeHash(imageBytes).Substring(0, 16);
        var labelsKey = string.Join(",", labels.OrderBy(l => l));
        var cacheKey = $"zeroshot:classify:{imageHash}:{ComputeHash(labelsKey).Substring(0, 16)}";

        var cached = await _cache.GetAsync<Dictionary<string, float>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for zero-shot classification");
            return cached;
        }

        _logger.LogDebug("Cache miss for zero-shot classification, executing");
        var result = await _inner.ZeroShotClassifyAsync(imageBytes, labels, cancellationToken);
        await _cache.SetAsync(cacheKey, result, DefaultExpiration, cancellationToken);
        return result;
    }

    // Zero-shot retrieval - CACHED (expensive database query)
    public async Task<IReadOnlyList<(long imageId, float similarity)>> ZeroShotImageRetrievalAsync(
        string textDescription,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        var textHash = ComputeHash(textDescription).Substring(0, 16);
        var cacheKey = $"zeroshot:retrieval:{textHash}:{topK}";

        var cached = await _cache.GetAsync<List<(long imageId, float similarity)>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for zero-shot retrieval");
            return cached;
        }

        _logger.LogDebug("Cache miss for zero-shot retrieval, querying database");
        var result = await _inner.ZeroShotImageRetrievalAsync(textDescription, topK, cancellationToken);
        await _cache.SetAsync(cacheKey, result, DefaultExpiration, cancellationToken);
        return result;
    }

    // Cross-modal search - CACHED (very expensive)
    public async Task<IReadOnlyList<CrossModalResult>> CrossModalSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        string? filterByType = null,
        CancellationToken cancellationToken = default)
    {
        var embeddingHash = ComputeHash(System.Text.Encoding.UTF8.GetBytes(
            string.Join(",", queryEmbedding.Take(32)))).Substring(0, 16);
        var cacheKey = $"crossmodal:{embeddingHash}:{topK}:{filterByType ?? "all"}";

        var cached = await _cache.GetAsync<List<CrossModalResult>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for cross-modal search");
            return cached;
        }

        _logger.LogDebug("Cache miss for cross-modal search, querying database");
        var result = await _inner.CrossModalSearchAsync(queryEmbedding, topK, filterByType, cancellationToken);
        await _cache.SetAsync(cacheKey, result, DefaultExpiration, cancellationToken);
        return result;
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static string ComputeHash(byte[] input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(input);
        return Convert.ToHexString(hash);
    }
}
