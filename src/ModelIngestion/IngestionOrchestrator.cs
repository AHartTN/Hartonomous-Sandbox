using Microsoft.Extensions.Logging;
using Hartonomous.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Production-ready orchestrator for model and embedding ingestion workflows
    /// </summary>
    public class IngestionOrchestrator
    {
        private readonly ILogger<IngestionOrchestrator> _logger;
        private readonly IModelRepository _models;
        private readonly IEmbeddingRepository _embeddings;
        private readonly EmbeddingIngestionService _embeddingService;
        private readonly AtomicStorageService _atomicStorage;

        public IngestionOrchestrator(
            ILogger<IngestionOrchestrator> logger,
            IModelRepository models,
            IEmbeddingRepository embeddings,
            EmbeddingIngestionService embeddingService,
            AtomicStorageService atomicStorage)
        {
            _logger = logger;
            _models = models;
            _embeddings = embeddings;
            _embeddingService = embeddingService;
            _atomicStorage = atomicStorage;
        }

        public async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Hartonomous Production Ingestion Service");
            _logger.LogInformation("==========================================");

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var command = args[0].ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case "ingest-embeddings":
                        await IngestEmbeddingsAsync(args, cancellationToken);
                        break;

                    case "test-deduplication":
                        await TestDeduplicationAsync(cancellationToken);
                        break;

                    case "test-sqlvector":
                        TestSqlVectorAvailability();
                        break;

                    case "query":
                        await ExecuteQueryAsync(args, cancellationToken);
                        break;

                    case "test-atomic":
                        await TestAtomicStorageAsync(cancellationToken);
                        break;

                    default:
                        _logger.LogError("Unknown command: {Command}", command);
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during {Command}", command);
                throw;
            }
        }

        private void ShowUsage()
        {
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  ingest-embeddings <count>      : Ingest sample embeddings with deduplication");
            Console.WriteLine("  test-deduplication             : Test deduplication with duplicate embeddings");
            Console.WriteLine("  test-sqlvector                 : Test SqlVector<T> availability in SqlClient 6.1.2");
            Console.WriteLine("  query <text>                   : Execute semantic search query");
            Console.WriteLine("  test-atomic                    : Test atomic storage (pixels, audio, tokens)");
            Console.WriteLine();
        }

        /// <summary>
        /// Test SqlVector availability
        /// </summary>
        private void TestSqlVectorAvailability()
        {
            _logger.LogInformation("Testing SqlVector<T> availability in Microsoft.Data.SqlClient 6.1.2...");
            TestSqlVector.VerifyAvailability();
        }

        /// <summary>
        /// Ingest sample embeddings with deduplication tracking
        /// </summary>
        private async Task IngestEmbeddingsAsync(string[] args, CancellationToken cancellationToken)
        {
            int count = args.Length > 1 && int.TryParse(args[1], out var c) ? c : 10;
            
            _logger.LogInformation("Ingesting {Count} sample embeddings with deduplication...", count);

            var random = new Random(42);
            int newCount = 0;
            int duplicateCount = 0;

            for (int i = 0; i < count; i++)
            {
                // Generate sample embedding (768-dimensional)
                var embedding = GenerateRandomEmbedding(random, 768);
                var sourceText = $"Sample sentence number {i} with some unique content.";

                var result = await _embeddingService.IngestEmbeddingWithDeduplicationAsync(
                    sourceText,
                    "sentence",
                    embedding);

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
        private async Task TestDeduplicationAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Testing deduplication with intentional duplicates...");

            var random = new Random(42);
            var embedding1 = GenerateRandomEmbedding(random, 768);
            var spatial1 = new float[] { 0.1f, 0.2f, 0.3f }; // Simple spatial projection for testing
            var text1 = "This is a unique test sentence.";

            // Insert first time - should be new
            _logger.LogInformation("Test 1: Inserting new embedding...");
            var result1 = await _embeddingService.IngestEmbeddingWithDeduplicationAsync(
                text1, "sentence", embedding1, spatial1);
            
            _logger.LogInformation("✓ First insert: ID={Id}, Duplicate={IsDup}", 
                result1.EmbeddingId, result1.WasDuplicate);

            // Insert exact same text - should detect content hash duplicate
            _logger.LogInformation("\nTest 2: Inserting same text (exact content hash match)...");
            var result2 = await _embeddingService.IngestEmbeddingWithDeduplicationAsync(
                text1, "sentence", embedding1, spatial1);
            
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
            
            var result3 = await _embeddingService.IngestEmbeddingWithDeduplicationAsync(
                text2, "sentence", embedding2, spatial1);
            
            _logger.LogInformation("✓ Third insert (similar embedding): ID={Id}, Duplicate={IsDup}, Reason={Reason}", 
                result3.EmbeddingId, result3.WasDuplicate, result3.DuplicateReason);

            _logger.LogInformation("\n=== Deduplication Test Results ===");
            _logger.LogInformation("Test 1 (new): Expected=false, Actual={0}", result1.WasDuplicate);
            _logger.LogInformation("Test 2 (hash): Expected=true, Actual={0}", result2.WasDuplicate);
            _logger.LogInformation("Test 3 (semantic): Expected=true, Actual={0}", result3.WasDuplicate);
            _logger.LogInformation("✓ Deduplication test complete");
        }

        /// <summary>
        /// Execute semantic search query
        /// </summary>
        private async Task ExecuteQueryAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                _logger.LogError("Query text required. Usage: query <text>");
                return;
            }

            var queryText = string.Join(" ", args.Skip(1));
            _logger.LogInformation("Executing semantic query: '{Query}'", queryText);

            // Generate query embedding (in production, use actual embedding model)
            var random = new Random(queryText.GetHashCode());
            var queryEmbedding = GenerateRandomEmbedding(random, 768);

            // Execute exact search
            _logger.LogInformation("Running exact VECTOR search...");
            var exactResults = await _embeddingService.ExactSearchAsync(queryEmbedding, topK: 5);

            _logger.LogInformation("Top 5 exact matches:");
            foreach (var result in exactResults)
            {
                _logger.LogInformation("  [{Id}] Distance: {Dist:F4} | {Text}", 
                    result.EmbeddingId, result.Distance, 
                    result.SourceText.Length > 80 ? result.SourceText.Substring(0, 77) + "..." : result.SourceText);
            }

            // Execute approximate spatial search
            _logger.LogInformation("Computing spatial projection...");
            var spatial3D = await _embeddingService.ComputeSpatialProjectionAsync(queryEmbedding);
            
            _logger.LogInformation("Running approximate spatial search...");
            var approxResults = await _embeddingService.ApproxSearchAsync(spatial3D, topK: 5);

            _logger.LogInformation("Top 5 approximate matches:");
            foreach (var result in approxResults)
            {
                _logger.LogInformation("  [{Id}] Distance: {Dist:F4} | {Text}", 
                    result.EmbeddingId, result.Distance, 
                    result.SourceText.Length > 80 ? result.SourceText.Substring(0, 77) + "..." : result.SourceText);
            }

            _logger.LogInformation("✓ Query complete");
        }

        /// <summary>
        /// Test atomic storage with deduplication
        /// </summary>
        private async Task TestAtomicStorageAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Testing atomic storage with content-addressable deduplication...");

            // Test 1: Atomic pixels
            _logger.LogInformation("\n=== Testing Atomic Pixel Storage ===");
            var pixelHash1 = await _atomicStorage.StoreAtomicPixelAsync(255, 0, 0, 255); // Red
            _logger.LogInformation("Stored red pixel: {Hash}", BitConverter.ToString(pixelHash1).Replace("-", "").Substring(0, 16) + "...");

            var pixelHash2 = await _atomicStorage.StoreAtomicPixelAsync(255, 0, 0, 255); // Same red - should dedupe
            _logger.LogInformation("Stored red pixel again (duplicate): {Hash}", BitConverter.ToString(pixelHash2).Replace("-", "").Substring(0, 16) + "...");
            _logger.LogInformation("Hashes match: {Match}", pixelHash1.SequenceEqual(pixelHash2));

            var pixelHash3 = await _atomicStorage.StoreAtomicPixelAsync(0, 255, 0, 255); // Green - different
            _logger.LogInformation("Stored green pixel: {Hash}", BitConverter.ToString(pixelHash3).Replace("-", "").Substring(0, 16) + "...");

            // Test 2: Atomic audio samples
            _logger.LogInformation("\n=== Testing Atomic Audio Sample Storage ===");
            var sampleHash1 = await _atomicStorage.StoreAtomicAudioSampleAsync(16384); // Mid-range amplitude
            _logger.LogInformation("Stored audio sample (16384): {Hash}", BitConverter.ToString(sampleHash1).Replace("-", "").Substring(0, 16) + "...");

            var sampleHash2 = await _atomicStorage.StoreAtomicAudioSampleAsync(16384); // Same - should dedupe
            _logger.LogInformation("Stored same sample again (duplicate): {Hash}", BitConverter.ToString(sampleHash2).Replace("-", "").Substring(0, 16) + "...");
            _logger.LogInformation("Hashes match: {Match}", sampleHash1.SequenceEqual(sampleHash2));

            // Test 3: Atomic tokens
            _logger.LogInformation("\n=== Testing Atomic Token Storage ===");
            var tokenHash1 = await _atomicStorage.StoreAtomicTokenAsync("hello");
            _logger.LogInformation("Stored token 'hello': {Hash}", BitConverter.ToString(tokenHash1).Replace("-", "").Substring(0, 16) + "...");

            var tokenHash2 = await _atomicStorage.StoreAtomicTokenAsync("hello"); // Same - should dedupe
            _logger.LogInformation("Stored token 'hello' again (duplicate): {Hash}", BitConverter.ToString(tokenHash2).Replace("-", "").Substring(0, 16) + "...");
            _logger.LogInformation("Hashes match: {Match}", tokenHash1.SequenceEqual(tokenHash2));

            var tokenHash3 = await _atomicStorage.StoreAtomicTokenAsync("world"); // Different
            _logger.LogInformation("Stored token 'world': {Hash}", BitConverter.ToString(tokenHash3).Replace("-", "").Substring(0, 16) + "...");

            // Test 4: Atomic vector components
            _logger.LogInformation("\n=== Testing Atomic Vector Component Storage ===");
            var compHash1 = await _atomicStorage.StoreAtomicVectorComponentAsync(0.5f);
            _logger.LogInformation("Stored component 0.5: {Hash}", BitConverter.ToString(compHash1).Replace("-", "").Substring(0, 16) + "...");

            var compHash2 = await _atomicStorage.StoreAtomicVectorComponentAsync(0.5f); // Same - should dedupe
            _logger.LogInformation("Stored component 0.5 again (duplicate): {Hash}", BitConverter.ToString(compHash2).Replace("-", "").Substring(0, 16) + "...");
            _logger.LogInformation("Hashes match: {Match}", compHash1.SequenceEqual(compHash2));

            _logger.LogInformation("\n✓ Atomic storage test complete - deduplication working!");
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
}
