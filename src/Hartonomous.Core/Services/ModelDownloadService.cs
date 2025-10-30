using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Abstracts;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Services;

/// <summary>
/// Service for downloading models from various repositories.
/// Provides a unified interface for different model sources.
/// </summary>
public class ModelDownloadService : BaseService
{
    private readonly IModelDownloader _huggingFaceDownloader;
    private readonly IModelDownloader _ollamaDownloader;

    public ModelDownloadService(
        ILogger<ModelDownloadService> logger,
        IModelDownloader huggingFaceDownloader,
        IModelDownloader ollamaDownloader)
        : base(logger)
    {
        _huggingFaceDownloader = huggingFaceDownloader ?? throw new ArgumentNullException(nameof(huggingFaceDownloader));
        _ollamaDownloader = ollamaDownloader ?? throw new ArgumentNullException(nameof(ollamaDownloader));
    }

    public override string ServiceName => "ModelDownloadService";

    /// <summary>
    /// Download a model from Hugging Face.
    /// </summary>
    /// <param name="modelId">The model identifier (organization/model-name)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The local path to the downloaded model</returns>
    public async Task<string> DownloadFromHuggingFaceAsync(string modelId, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Downloading model from Hugging Face: {ModelId}", modelId);

        try
        {
            var modelPath = await _huggingFaceDownloader.DownloadModelAsync(modelId, cancellationToken);
            Logger.LogInformation("✓ Model downloaded successfully: {Path}", modelPath);
            return modelPath;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to download model from Hugging Face: {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// Download a model from Ollama.
    /// </summary>
    /// <param name="modelName">The model name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The local path to the downloaded model</returns>
    public async Task<string> DownloadFromOllamaAsync(string modelName, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Downloading model from Ollama: {ModelName}", modelName);

        try
        {
            var modelPath = await _ollamaDownloader.DownloadModelAsync(modelName, cancellationToken);
            Logger.LogInformation("✓ Model downloaded successfully: {Path}", modelPath);
            return modelPath;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to download model from Ollama: {ModelName}", modelName);
            throw;
        }
    }

    /// <summary>
    /// Download a model from Hugging Face and return usage instructions.
    /// </summary>
    /// <param name="modelId">The model identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download result with path and usage info</returns>
    public async Task<ModelDownloadResult> DownloadAndIngestHuggingFaceAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var modelPath = await DownloadFromHuggingFaceAsync(modelId, cancellationToken);

        return new ModelDownloadResult
        {
            ModelPath = modelPath,
            ModelId = modelId,
            Source = "HuggingFace",
            UsageInstructions = $"To ingest: dotnet run ingest-model {modelPath}"
        };
    }

    /// <summary>
    /// Get available models from a specific source.
    /// </summary>
    /// <param name="source">The source (huggingface or ollama)</param>
    /// <param name="filter">Optional filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available model identifiers</returns>
    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(string source, string? filter = null, CancellationToken cancellationToken = default)
    {
        var downloader = source.ToLowerInvariant() switch
        {
            "huggingface" => _huggingFaceDownloader,
            "ollama" => _ollamaDownloader,
            _ => throw new ArgumentException($"Unknown source: {source}", nameof(source))
        };

        return await downloader.GetAvailableModelsAsync(filter, cancellationToken);
    }
}

/// <summary>
/// Result of a model download operation.
/// </summary>
public class ModelDownloadResult
{
    public string ModelPath { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string UsageInstructions { get; set; } = string.Empty;
}

/// <summary>
/// Service for testing embedding operations.
/// Provides methods to test deduplication, storage, and retrieval.
/// </summary>
public class EmbeddingTestService : BaseService
{
    private readonly IEmbeddingIngestionService _embeddingService;
    private readonly IEmbeddingStorageProvider _embeddingStorage;

    public EmbeddingTestService(
        ILogger<EmbeddingTestService> logger,
        IEmbeddingIngestionService embeddingService,
        IEmbeddingStorageProvider embeddingStorage)
        : base(logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _embeddingStorage = embeddingStorage ?? throw new ArgumentNullException(nameof(embeddingStorage));
    }

    public override string ServiceName => "EmbeddingTestService";

    /// <summary>
    /// Ingest sample embeddings with deduplication tracking.
    /// </summary>
    /// <param name="count">Number of embeddings to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    public async Task<EmbeddingTestResults> IngestSampleEmbeddingsAsync(int count, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Ingesting {Count} sample embeddings with deduplication...", count);

        var random = new Random(42);
        int newCount = 0;
        int duplicateCount = 0;

        for (int i = 0; i < count; i++)
        {
            // Generate sample embedding (768-dimensional)
            var embedding = GenerateRandomEmbedding(random, 768);
            var sourceText = $"Sample sentence number {i} with some unique content.";

            var result = await _embeddingService.IngestEmbeddingAsync(
                sourceText,
                "sentence",
                embedding,
                null,
                cancellationToken);

            if (result.WasDuplicate)
            {
                duplicateCount++;
                Logger.LogDebug("Duplicate detected: {Reason}", result.DuplicateReason);
            }
            else
            {
                newCount++;
            }

            if ((i + 1) % 100 == 0)
            {
                Logger.LogInformation("Progress: {Current}/{Total} (New: {New}, Duplicates: {Dup})",
                    i + 1, count, newCount, duplicateCount);
            }
        }

        var results = new EmbeddingTestResults
        {
            TotalEmbeddings = count,
            NewEmbeddings = newCount,
            DuplicateEmbeddings = duplicateCount
        };

        Logger.LogInformation("✓ Ingestion complete: {New} new, {Dup} duplicates", newCount, duplicateCount);
        return results;
    }

    /// <summary>
    /// Test deduplication by intentionally inserting duplicates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deduplication test results</returns>
    public async Task<DeduplicationTestResults> TestDeduplicationAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Testing deduplication with intentional duplicates...");

        var random = new Random(42);
        var embedding1 = GenerateRandomEmbedding(random, 768);
        var spatial1 = new float[] { 0.1f, 0.2f, 0.3f };
        var text1 = "This is a unique test sentence.";

        // Insert first time - should be new
        Logger.LogInformation("Test 1: Inserting new embedding...");
        var result1 = await _embeddingService.IngestEmbeddingAsync(
            text1, "sentence", embedding1, spatial1, cancellationToken);

        // Insert exact same text - should detect content hash duplicate
        Logger.LogInformation("\nTest 2: Inserting same text (exact content hash match)...");
        var result2 = await _embeddingService.IngestEmbeddingAsync(
            text1, "sentence", embedding1, spatial1, cancellationToken);

        // Insert different text but very similar embedding - should detect semantic duplicate
        Logger.LogInformation("\nTest 3: Inserting different text but similar embedding (semantic match)...");
        // Create embedding with 5% difference (0.95 threshold should catch this)
        var embedding2 = embedding1.Select(v => v * 0.95f).ToArray();
        // Normalize to unit length
        var mag = (float)Math.Sqrt(embedding2.Sum(v => v * v));
        embedding2 = embedding2.Select(v => v / mag).ToArray();

        var text2 = "This is a different sentence but semantically similar.";

        var result3 = await _embeddingService.IngestEmbeddingAsync(
            text2, "sentence", embedding2, spatial1, cancellationToken);

        var results = new DeduplicationTestResults
        {
            Test1_NewInsertion = !result1.WasDuplicate,
            Test2_HashDuplicate = result2.WasDuplicate,
            Test3_SemanticDuplicate = result3.WasDuplicate
        };

        Logger.LogInformation("\n=== Deduplication Test Results ===");
        Logger.LogInformation("Test 1 (new): Expected=true, Actual={0}", results.Test1_NewInsertion);
        Logger.LogInformation("Test 2 (hash): Expected=true, Actual={0}", results.Test2_HashDuplicate);
        Logger.LogInformation("Test 3 (semantic): Expected=true, Actual={0}", results.Test3_SemanticDuplicate);
        Logger.LogInformation("✓ Deduplication test complete");

        return results;
    }

    private float[] GenerateRandomEmbedding(Random random, int dimension)
    {
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range: -1 to 1
        }

        // Normalize to unit length (cosine similarity requirement)
        var magnitude = (float)Math.Sqrt(embedding.Sum(v => v * v));
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}

/// <summary>
/// Results from embedding ingestion testing.
/// </summary>
public class EmbeddingTestResults
{
    public int TotalEmbeddings { get; set; }
    public int NewEmbeddings { get; set; }
    public int DuplicateEmbeddings { get; set; }
}

/// <summary>
/// Results from deduplication testing.
/// </summary>
public class DeduplicationTestResults
{
    public bool Test1_NewInsertion { get; set; }
    public bool Test2_HashDuplicate { get; set; }
    public bool Test3_SemanticDuplicate { get; set; }
}