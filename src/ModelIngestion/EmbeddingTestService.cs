using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hartonomous.Core;

namespace ModelIngestion;

/// <summary>
/// Service for testing embedding operations including ingestion and deduplication
/// </summary>
public class EmbeddingTestService
{
    private readonly EmbeddingIngestionService _embeddingService;
    private readonly ILogger<EmbeddingTestService> _logger;

    public EmbeddingTestService(
        EmbeddingIngestionService embeddingService,
        ILogger<EmbeddingTestService> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ingest sample embeddings with deduplication tracking
    /// </summary>
    public async Task IngestSampleEmbeddingsAsync(int count, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting {Count} sample embeddings with deduplication...", count);

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
                _logger.LogDebug("Duplicate detected: {Reason}", result.DuplicateReason);
            }
            else
            {
                newCount++;
            }

            if ((i + 1) % 100 == 0)
            {
                _logger.LogInformation("Progress: {Current}/{Total} (New: {New}, Duplicates: {Dup})",
                    i + 1, count, newCount, duplicateCount);
            }
        }

        _logger.LogInformation("✓ Ingestion complete: {New} new, {Dup} duplicates", newCount, duplicateCount);
    }

    /// <summary>
    /// Test deduplication by intentionally inserting duplicates
    /// </summary>
    public async Task TestDeduplicationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing deduplication with intentional duplicates...");

        var random = new Random(42);
        var embedding1 = GenerateRandomEmbedding(random, 768);
        var spatial1 = new float[] { 0.1f, 0.2f, 0.3f }; // Simple spatial projection for testing
        var text1 = "This is a unique test sentence.";

        // Insert first time - should be new
        _logger.LogInformation("Test 1: Inserting new embedding...");
        var result1 = await _embeddingService.IngestEmbeddingAsync(
            text1, "sentence", embedding1, spatial1, cancellationToken);

        _logger.LogInformation("✓ First insert: ID={Id}, Duplicate={IsDup}",
            result1.EmbeddingId, result1.WasDuplicate);

        // Insert exact same text - should detect content hash duplicate
        _logger.LogInformation("\nTest 2: Inserting same text (exact content hash match)...");
        var result2 = await _embeddingService.IngestEmbeddingAsync(
            text1, "sentence", embedding1, spatial1, cancellationToken);

        _logger.LogInformation("✓ Second insert (same text): ID={Id}, Duplicate={IsDup}, Reason={Reason}",
            result2.EmbeddingId, result2.WasDuplicate, result2.DuplicateReason);

        // Insert different text but very similar embedding - should detect semantic duplicate
        _logger.LogInformation("\nTest 3: Inserting different text but similar embedding (semantic match)...");
        // Create embedding with 5% difference (0.95 threshold should catch this)
        var embedding2 = embedding1.Select(v => v * 0.95f).ToArray();
        // Normalize to unit length
        var mag = (float)Math.Sqrt(embedding2.Sum(v => v * v));
        embedding2 = embedding2.Select(v => v / mag).ToArray();

        var text2 = "This is a different sentence but semantically similar.";

        var result3 = await _embeddingService.IngestEmbeddingAsync(
            text2, "sentence", embedding2, spatial1, cancellationToken);

        _logger.LogInformation("✓ Third insert (similar embedding): ID={Id}, Duplicate={IsDup}, Reason={Reason}",
            result3.EmbeddingId, result3.WasDuplicate, result3.DuplicateReason);

        _logger.LogInformation("\n=== Deduplication Test Results ===");
        _logger.LogInformation("Test 1 (new): Expected=false, Actual={0}", result1.WasDuplicate);
        _logger.LogInformation("Test 2 (hash): Expected=true, Actual={0}", result2.WasDuplicate);
        _logger.LogInformation("Test 3 (semantic): Expected=true, Actual={0}", result3.WasDuplicate);
        _logger.LogInformation("✓ Deduplication test complete");
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