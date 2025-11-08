using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services.Embedding;
using Microsoft.Extensions.Logging;
using CrossModalResult = Hartonomous.Core.Interfaces.CrossModalResult;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Unified embedding service that delegates to modality-specific embedders.
/// REFACTORED to follow Single Responsibility Principle - this class only coordinates
/// between specialized embedders and the storage layer. Feature extraction moved to
/// TextEmbedder, ImageEmbedder, AudioEmbedder classes.
/// 
/// Cross-modal operations (zero-shot classification, cross-modal search) moved to
/// CrossModalSearchService for better separation of concerns.
/// </summary>
public sealed class EmbeddingServiceRefactored : IEmbeddingService
{
    private readonly TextEmbedder _textEmbedder;
    private readonly ImageEmbedder _imageEmbedder;
    private readonly AudioEmbedder _audioEmbedder;
    private readonly CrossModalSearchService _crossModalSearch;
    private readonly IAtomIngestionService _atomIngestionService;
    private readonly ILogger<EmbeddingServiceRefactored> _logger;
    private const int EmbeddingDimension = 768;

    public EmbeddingServiceRefactored(
        TextEmbedder textEmbedder,
        ImageEmbedder imageEmbedder,
        AudioEmbedder audioEmbedder,
        CrossModalSearchService crossModalSearch,
        IAtomIngestionService atomIngestionService,
        ILogger<EmbeddingServiceRefactored> logger)
    {
        _textEmbedder = textEmbedder ?? throw new ArgumentNullException(nameof(textEmbedder));
        _imageEmbedder = imageEmbedder ?? throw new ArgumentNullException(nameof(imageEmbedder));
        _audioEmbedder = audioEmbedder ?? throw new ArgumentNullException(nameof(audioEmbedder));
        _crossModalSearch = crossModalSearch ?? throw new ArgumentNullException(nameof(crossModalSearch));
        _atomIngestionService = atomIngestionService ?? throw new ArgumentNullException(nameof(atomIngestionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Text embedding via TF-IDF from existing corpus vocabulary.
    /// Delegates to TextEmbedder for feature extraction.
    /// </summary>
    public Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return _textEmbedder.EmbedAsync(text, cancellationToken);
    }

    /// <summary>
    /// Image embedding via pixel histogram and edge detection.
    /// Delegates to ImageEmbedder for feature extraction.
    /// </summary>
    public Task<float[]> EmbedImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        return _imageEmbedder.EmbedAsync(imageData, cancellationToken);
    }

    /// <summary>
    /// Audio embedding via FFT spectrum and MFCC.
    /// Delegates to AudioEmbedder for feature extraction.
    /// </summary>
    public Task<float[]> EmbedAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        return _audioEmbedder.EmbedAsync(audioData, cancellationToken);
    }

    /// <summary>
    /// Video frame embedding (delegates to image embedding).
    /// </summary>
    public Task<float[]> EmbedVideoFrameAsync(byte[] frameBytes, CancellationToken cancellationToken = default)
    {
        return EmbedImageAsync(frameBytes, cancellationToken);
    }

    /// <summary>
    /// Store embedding in database with automatic spatial projection.
    /// Inserts into Embeddings table, triggers sp_ComputeSpatialProjection for 768D → 3D GEOMETRY.
    /// </summary>
    public async Task<long> StoreEmbeddingAsync(
        float[] embedding,
        object sourceData,
        string sourceType,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (embedding is null || embedding.Length != EmbeddingDimension)
        {
            throw new ArgumentException($"Embedding must be {EmbeddingDimension} dimensions.", nameof(embedding));
        }

        var normalizedType = string.IsNullOrWhiteSpace(sourceType)
            ? "unknown"
            : sourceType.Trim().ToLowerInvariant();

        var (hashInput, canonicalText) = BuildHashInput(sourceData, normalizedType);

        var request = new AtomIngestionRequest
        {
            HashInput = hashInput,
            Modality = normalizedType,
            Subtype = normalizedType,
            SourceType = sourceType,
            CanonicalText = canonicalText,
            Metadata = metadata,
            Embedding = embedding,
            EmbeddingType = "unified",
            ModelId = null,
            PolicyName = "default"
        };

        var result = await _atomIngestionService
            .IngestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (result.WasDuplicate)
        {
            _logger.LogInformation(
                "Reused atom {AtomId} for {SourceType} input (Reason: {Reason})",
                result.Atom.AtomId,
                sourceType,
                result.DuplicateReason ?? "deduplicated");
        }
        else
        {
            _logger.LogInformation(
                "Stored new atom {AtomId} with embedding {EmbeddingId} for {SourceType}",
                result.Atom.AtomId,
                result.Embedding?.AtomEmbeddingId,
                sourceType);
        }

        return result.Embedding?.AtomEmbeddingId ?? result.Atom.AtomId;
    }

    /// <summary>
    /// Generate and store embedding in one operation.
    /// </summary>
    public async Task<(long embeddingId, float[] embedding)> GenerateAndStoreAsync(
        object input,
        string inputType,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        float[] embedding = inputType.ToLowerInvariant() switch
        {
            "text" => await EmbedTextAsync((string)input, cancellationToken),
            "image" => await EmbedImageAsync((byte[])input, cancellationToken),
            "audio" => await EmbedAudioAsync((byte[])input, cancellationToken),
            "video_frame" => await EmbedVideoFrameAsync((byte[])input, cancellationToken),
            _ => throw new ArgumentException($"Unknown input type: {inputType}", nameof(inputType))
        };

        var embeddingId = await StoreEmbeddingAsync(embedding, input, inputType, metadata, cancellationToken);

        return (embeddingId, embedding);
    }

    /// <summary>
    /// Zero-shot classification. Delegates to CrossModalSearchService.
    /// </summary>
    public Task<Dictionary<string, float>> ZeroShotClassifyAsync(
        byte[] imageBytes,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default)
    {
        return _crossModalSearch.ZeroShotClassifyAsync(imageBytes, labels, cancellationToken);
    }

    /// <summary>
    /// Zero-shot image retrieval. Delegates to CrossModalSearchService.
    /// </summary>
    public Task<IReadOnlyList<(long imageId, float similarity)>> ZeroShotImageRetrievalAsync(
        string textDescription,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        return _crossModalSearch.ZeroShotImageRetrievalAsync(textDescription, topK, cancellationToken);
    }

    /// <summary>
    /// Cross-modal search using pre-computed embedding vector.
    /// </summary>
    public Task<IReadOnlyList<CrossModalResult>> CrossModalSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        string? filterModality = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement proper search with pre-computed embedding
        // For now, return empty results since repository interface needs refactoring
        _logger.LogWarning("CrossModalSearchAsync with pre-computed embedding not yet implemented");
        return Task.FromResult<IReadOnlyList<CrossModalResult>>(Array.Empty<CrossModalResult>());
    }

    private static (string HashInput, string CanonicalText) BuildHashInput(object sourceData, string sourceType)
    {
        return sourceType switch
        {
            "text" => (sourceData.ToString() ?? "", sourceData.ToString() ?? ""),
            "image" or "audio" or "video_frame" =>
                ($"binary_{sourceType}_{((byte[])sourceData).Length}", $"{sourceType}_data"),
            _ => (sourceData.ToString() ?? "unknown", "unknown")
        };
    }
}
