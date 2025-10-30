using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Generic processor for embedding ingestion with deduplication.
/// Handles the complete embedding ingestion workflow.
/// </summary>
public class EmbeddingIngestionProcessor : BaseProcessor<EmbeddingIngestionRequest, EmbeddingIngestionResult>
{
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IEmbeddingIngestionService _ingestionService;

    public EmbeddingIngestionProcessor(
        ILogger<EmbeddingIngestionProcessor> logger,
        IEmbeddingRepository embeddingRepository,
        IEmbeddingIngestionService ingestionService)
        : base(logger)
    {
        _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
        _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
    }

    public override string ServiceName => "EmbeddingIngestionProcessor";

    public override async Task<EmbeddingIngestionResult> ProcessAsync(
        EmbeddingIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        Logger.LogInformation("Processing embedding ingestion: {SourceType}, Length={Length}",
            request.SourceType, request.Embedding.Length);

        try
        {
            var result = await _ingestionService.IngestEmbeddingAsync(
                request.SourceText,
                request.SourceType,
                request.Embedding,
                request.SpatialProjection,
                cancellationToken);

            if (result.WasDuplicate)
            {
                Logger.LogInformation("Duplicate embedding detected: {Reason}", result.DuplicateReason);
            }
            else
            {
                Logger.LogInformation("New embedding ingested: ID={Id}", result.EmbeddingId);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process embedding: {SourceType}", request.SourceType);
            throw;
        }
    }

    public override async Task<IEnumerable<EmbeddingIngestionResult>> ProcessBatchAsync(
        IEnumerable<EmbeddingIngestionRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests == null)
            throw new ArgumentNullException(nameof(requests));

        Logger.LogInformation("Processing batch of {Count} embeddings", requests.Count());

        var batch = requests.Select(r => (r.SourceText, r.SourceType, r.Embedding));
        return await _ingestionService.IngestBatchAsync(batch, cancellationToken);
    }

    public override bool CanProcess(EmbeddingIngestionRequest input)
    {
        return input != null &&
               !string.IsNullOrEmpty(input.SourceText) &&
               !string.IsNullOrEmpty(input.SourceType) &&
               input.Embedding != null &&
               input.Embedding.Length > 0;
    }
}

/// <summary>
/// Request object for embedding ingestion.
/// </summary>
public class EmbeddingIngestionRequest
{
    /// <summary>
    /// Gets or sets the source text that was embedded.
    /// </summary>
    public string SourceText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of source (e.g., 'sentence', 'document').
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding vector.
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets or sets the optional pre-computed spatial projection.
    /// </summary>
    public float[]? SpatialProjection { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Generic service for atomic storage operations.
/// Handles deduplication for pixels, audio samples, tokens, etc.
/// </summary>
public class AtomicStorageService : BaseService, IAtomicStorageService
{
    private readonly ILogger<AtomicStorageService> _logger;

    // TODO: Inject actual repositories when implemented
    public AtomicStorageService(ILogger<AtomicStorageService> logger) : base(logger)
    {
        _logger = logger;
    }

    public override string ServiceName => "AtomicStorageService";

    public async Task<long> StoreAtomicPixelAsync(byte r, byte g, byte b, byte a, CancellationToken cancellationToken = default)
    {
        // TODO: Implement with actual repository
        _logger.LogInformation("Storing atomic pixel: RGBA({R},{G},{B},{A})", r, g, b, a);
        return await Task.FromResult(1L); // Placeholder
    }

    public async Task<long> StoreAtomicAudioSampleAsync(float sample, CancellationToken cancellationToken = default)
    {
        // TODO: Implement with actual repository
        _logger.LogInformation("Storing atomic audio sample: {Sample}", sample);
        return await Task.FromResult(1L); // Placeholder
    }

    public async Task<long> StoreAtomicTextTokenAsync(string token, string? embeddingHash, CancellationToken cancellationToken = default)
    {
        // TODO: Implement with actual repository
        _logger.LogInformation("Storing atomic text token: '{Token}'", token);
        return await Task.FromResult(1L); // Placeholder
    }

    public async Task<IEnumerable<long>> StoreBatchPixelsAsync(IEnumerable<(byte r, byte g, byte b, byte a)> pixels, CancellationToken cancellationToken = default)
    {
        // TODO: Implement with actual repository
        _logger.LogInformation("Storing batch of {Count} pixels", pixels.Count());
        return await Task.FromResult(Enumerable.Range(1, pixels.Count()).Select(i => (long)i)); // Placeholder
    }
}

/// <summary>
/// Generic hash utility for content-addressable storage.
/// </summary>
public static class HashUtility
{
    /// <summary>
    /// Compute SHA256 hash of content and return as hex string.
    /// </summary>
    /// <param name="content">The content to hash</param>
    /// <returns>Hex string representation of the hash</returns>
    public static string ComputeSHA256Hash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Compute SHA256 hash of byte array.
    /// </summary>
    /// <param name="bytes">The bytes to hash</param>
    /// <returns>Hex string representation of the hash</returns>
    public static string ComputeSHA256Hash(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}