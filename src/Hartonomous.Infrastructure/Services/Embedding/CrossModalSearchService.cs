using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Performance;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Embedding;

/// <summary>
/// Service for cross-modal operations: zero-shot classification, image retrieval by text, etc.
/// Separated from EmbeddingService to follow Single Responsibility Principle.
/// </summary>
public sealed class CrossModalSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IAtomEmbeddingRepository _embeddingRepository;
    private readonly ILogger<CrossModalSearchService> _logger;

    public CrossModalSearchService(
        IEmbeddingService embeddingService,
        IAtomEmbeddingRepository embeddingRepository,
        ILogger<CrossModalSearchService> logger)
    {
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _logger = logger;
    }

    /// <summary>
    /// Zero-shot classification: Compute similarity between input and each label.
    /// OPTIMIZED with parallel label embedding and SIMD similarity calculations.
    /// </summary>
    public async Task<Dictionary<string, float>> ZeroShotClassifyAsync(
        byte[] imageBytes,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default)
    {
        // Generate image embedding
        var imageEmbedding = await _embeddingService.EmbedImageAsync(imageBytes, cancellationToken);

        // Generate label embeddings in parallel (OPTIMIZED)
        var labelEmbeddingTasks = labels.Select(async label =>
        {
            var embedding = await _embeddingService.EmbedTextAsync(label, cancellationToken);
            return (label, embedding);
        });
        var labelEmbeddings = await Task.WhenAll(labelEmbeddingTasks);

        // Compute cosine similarities using SIMD (OPTIMIZED)
        var similarities = new Dictionary<string, float>(labels.Count);
        foreach (var (label, labelEmbedding) in labelEmbeddings)
        {
            var similarity = VectorMath.CosineSimilarity(imageEmbedding, labelEmbedding);
            similarities[label] = similarity;
        }

        // Softmax to get probabilities
        var probabilities = Softmax(similarities.Values.ToArray());

        var results = new Dictionary<string, float>(labels.Count);
        int index = 0;
        foreach (var label in similarities.Keys)
        {
            results[label] = probabilities[index++];
        }

        _logger.LogInformation("Zero-shot classification complete: {Results}",
            string.Join(", ", results.Select(kv => $"{kv.Key}={kv.Value:F3}")));

        return results;
    }

    /// <summary>
    /// Zero-shot image retrieval: Find images matching text description.
    /// </summary>
    public async Task<IReadOnlyList<(long imageId, float similarity)>> ZeroShotImageRetrievalAsync(
        string textDescription,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        // Generate text embedding
        var queryEmbedding = await _embeddingService.EmbedTextAsync(textDescription, cancellationToken);

        // TODO: Repository interface needs HybridSearchAsync or similar method
        // For now, return empty until repository refactoring is complete
        _logger.LogWarning("Zero-shot image retrieval: Repository search method not yet available");

        return Array.Empty<(long, float)>();
    }

    /// <summary>
    /// Cross-modal search: Find items matching any combination of text/image/audio.
    /// </summary>
    public async Task<IReadOnlyList<CrossModalResult>> CrossModalSearchAsync(
        string? textQuery = null,
        byte[]? imageQuery = null,
        byte[]? audioQuery = null,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        var queryEmbeddings = new List<float[]>();

        if (!string.IsNullOrWhiteSpace(textQuery))
        {
            queryEmbeddings.Add(await _embeddingService.EmbedTextAsync(textQuery, cancellationToken));
        }

        if (imageQuery != null && imageQuery.Length > 0)
        {
            queryEmbeddings.Add(await _embeddingService.EmbedImageAsync(imageQuery, cancellationToken));
        }

        if (audioQuery != null && audioQuery.Length > 0)
        {
            queryEmbeddings.Add(await _embeddingService.EmbedAudioAsync(audioQuery, cancellationToken));
        }

        if (queryEmbeddings.Count == 0)
        {
            throw new ArgumentException("At least one query (text, image, or audio) must be provided.");
        }

        // Average all query embeddings (ensemble approach)
        var fusedEmbedding = new float[768];
        foreach (var embedding in queryEmbeddings)
        {
            for (int i = 0; i < fusedEmbedding.Length; i++)
            {
                fusedEmbedding[i] += embedding[i];
            }
        }

        // Normalize fused embedding
        VectorMath.Normalize(fusedEmbedding.AsSpan());

        // TODO: Repository search needs proper interface method
        _logger.LogWarning("Cross-modal search: Repository search method not yet available");

        return Array.Empty<CrossModalResult>();
    }

    private static float[] Softmax(float[] values)
    {
        if (values.Length == 0)
            return Array.Empty<float>();

        var maxValue = values.Max();
        var exponents = values.Select(v => MathF.Exp(v - maxValue)).ToArray();
        var sum = exponents.Sum();

        if (sum == 0)
            return values.Select(_ => 1.0f / values.Length).ToArray();

        return exponents.Select(e => e / sum).ToArray();
    }
}

/// <summary>
/// Result from cross-modal search operation.
/// </summary>
public sealed class CrossModalResult
{
    public required long AtomId { get; init; }
    public required float Similarity { get; init; }
    public required string MatchedModality { get; init; }
}
