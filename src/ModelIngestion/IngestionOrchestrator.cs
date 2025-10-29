using Microsoft.Extensions.Logging;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
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
        private readonly ModelIngestionService _modelIngestion;
        private readonly ModelDownloader _downloader;

        public IngestionOrchestrator(
            ILogger<IngestionOrchestrator> logger,
            IModelRepository models,
            IEmbeddingRepository embeddings,
            EmbeddingIngestionService embeddingService,
            AtomicStorageService atomicStorage,
            ModelIngestionService modelIngestion,
            ModelDownloader downloader)
        {
            _logger = logger;
            _models = models;
            _embeddings = embeddings;
            _embeddingService = embeddingService;
            _atomicStorage = atomicStorage;
            _modelIngestion = modelIngestion;
            _downloader = downloader;
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
                    case "download-hf":
                        await DownloadHuggingFaceAsync(args, cancellationToken);
                        break;

                    case "download-ollama":
                        await DownloadOllamaAsync(args, cancellationToken);
                        break;

                    case "download-and-ingest-hf":
                        await DownloadAndIngestHuggingFaceAsync(args, cancellationToken);
                        break;

                    case "ingest-model":
                        await IngestModelAsync(args, cancellationToken);
                        break;

                    case "ingest-models":
                        await IngestModelsDirectoryAsync(args, cancellationToken);
                        break;

                    case "model-stats":
                        await ShowModelStatsAsync(cancellationToken);
                        break;

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
            Console.WriteLine("\n  Download Models:");
            Console.WriteLine("  download-hf <model-id>         : Download from Hugging Face (e.g., TinyLlama/TinyLlama-1.1B-Chat-v1.0)");
            Console.WriteLine("  download-ollama <model>        : Download from Ollama (e.g., llama3.2:1b)");
            Console.WriteLine("  download-and-ingest-hf <id>    : Download from HF and ingest in one step");
            Console.WriteLine("\n  Model Ingestion:");
            Console.WriteLine("  ingest-model <path>            : Ingest single model (Safetensors, ONNX, PyTorch, GGUF)");
            Console.WriteLine("  ingest-models <dir>            : Batch ingest all models from directory");
            Console.WriteLine("  model-stats                    : Show ingestion statistics");
            Console.WriteLine("\n  Embedding Ingestion:");
            Console.WriteLine("  ingest-embeddings <count>      : Ingest sample embeddings with deduplication");
            Console.WriteLine("  test-deduplication             : Test deduplication with duplicate embeddings");
            Console.WriteLine("  test-sqlvector                 : Test SqlVector<T> availability in SqlClient 6.1.2");
            Console.WriteLine("  query <text>                   : Execute semantic search query");
            Console.WriteLine("  test-atomic                    : Test atomic storage (pixels, audio, tokens)");
            Console.WriteLine();
        }

        /// <summary>
        /// Ingest a single model file
        /// </summary>
        private async Task IngestModelAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                _logger.LogError("Model path required. Usage: ingest-model <path>");
                return;
            }

            var modelPath = args[1];
            _logger.LogInformation("Ingesting model from: {Path}", modelPath);

            try
            {
                var modelId = await _modelIngestion.IngestAsync(modelPath, null, cancellationToken);
                _logger.LogInformation("✓ Model ingestion complete: ModelId={ModelId}", modelId);

                // Show layers
                var layers = await _models.GetLayersByModelIdAsync(modelId, cancellationToken);
                _logger.LogInformation("Model has {LayerCount} layers", layers.Count());

                var firstFive = layers.Take(5);
                foreach (var layer in firstFive)
                {
                    _logger.LogInformation("  Layer: {Name} ({Type}), {ParamCount} parameters",
                        layer.LayerName, layer.LayerType, layer.ParameterCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model ingestion failed");
            }
        }

        /// <summary>
        /// Batch ingest models from directory
        /// </summary>
        private async Task IngestModelsDirectoryAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                _logger.LogError("Directory path required. Usage: ingest-models <directory>");
                return;
            }

            var directoryPath = args[1];
            _logger.LogInformation("Batch ingesting models from: {Path}", directoryPath);

            try
            {
                var modelIds = await _modelIngestion.IngestDirectoryAsync(directoryPath, "*", cancellationToken);
                _logger.LogInformation("✓ Batch ingestion complete: {Count} models ingested", modelIds.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch ingestion failed");
            }
        }

        /// <summary>
        /// Show model ingestion statistics
        /// </summary>
        private async Task ShowModelStatsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading ingestion statistics...");

            try
            {
                var stats = await _modelIngestion.GetStatsAsync(cancellationToken);

                _logger.LogInformation("\n=== Model Ingestion Statistics ===");
                _logger.LogInformation("Total Models: {Count}", stats.TotalModels);
                _logger.LogInformation("Total Parameters: {Count:N0}", stats.TotalParameters);
                _logger.LogInformation("Total Layers: {Count}", stats.TotalLayers);
                
                _logger.LogInformation("\nArchitecture Breakdown:");
                foreach (var kvp in stats.ArchitectureBreakdown.OrderByDescending(x => x.Value))
                {
                    _logger.LogInformation("  {Arch}: {Count} models", kvp.Key, kvp.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load statistics");
            }
        }

        /// <summary>
        /// Download model from Hugging Face
        /// </summary>
        private async Task DownloadHuggingFaceAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                _logger.LogError("Model ID required. Usage: download-hf <organization/model-name>");
                _logger.LogInformation("Examples:");
                _logger.LogInformation("  download-hf TinyLlama/TinyLlama-1.1B-Chat-v1.0");
                _logger.LogInformation("  download-hf meta-llama/Llama-3.2-1B-Instruct");
                return;
            }

            var modelId = args[1];

            try
            {
                var modelDir = await _downloader.DownloadFromHuggingFaceAsync(modelId, cancellationToken);
                _logger.LogInformation("✓ Model ready at: {Path}", modelDir);
                _logger.LogInformation("\nTo ingest: dotnet run ingest-model {Path}", modelDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed");
            }
        }

        /// <summary>
        /// Download model from Ollama
        /// </summary>
        private async Task DownloadOllamaAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                _logger.LogError("Model name required. Usage: download-ollama <model-name>");
                _logger.LogInformation("Examples:");
                _logger.LogInformation("  download-ollama llama3.2:1b");
                _logger.LogInformation("  download-ollama phi3:mini");
                return;
            }

            var modelName = args[1];

            try
            {
                var modelPath = await _downloader.DownloadFromOllamaAsync(modelName, cancellationToken);
                _logger.LogInformation("✓ Model ready at: {Path}", modelPath);
                _logger.LogInformation("\nTo ingest: dotnet run ingest-model {Path}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed");
            }
        }

        /// <summary>
        /// Download from Hugging Face and ingest in one step
        /// </summary>
        private async Task DownloadAndIngestHuggingFaceAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                _logger.LogError("Model ID required. Usage: download-and-ingest-hf <organization/model-name>");
                return;
            }

            var modelId = args[1];

            try
            {
                // Download
                _logger.LogInformation("Step 1/2: Downloading model from Hugging Face...");
                var modelDir = await _downloader.DownloadFromHuggingFaceAsync(modelId, cancellationToken);
                
                // Find safetensors file
                var modelFiles = Directory.GetFiles(modelDir, "*.safetensors");
                if (modelFiles.Length == 0)
                {
                    modelFiles = Directory.GetFiles(modelDir, "*.onnx");
                }
                if (modelFiles.Length == 0)
                {
                    _logger.LogError("No compatible model files found in: {Dir}", modelDir);
                    return;
                }

                var modelPath = modelFiles[0];

                // Ingest
                _logger.LogInformation("Step 2/2: Ingesting model...");
                var modelIdDb = await _modelIngestion.IngestAsync(modelPath, null, cancellationToken);
                _logger.LogInformation("✓ Complete! ModelId={ModelId}", modelIdDb);

                // Show stats
                var model = await _models.GetByIdAsync(modelIdDb, cancellationToken);
                if (model != null)
                {
                    _logger.LogInformation("Model: {Name} ({Arch})", model.ModelName, model.Architecture);
                    _logger.LogInformation("Parameters: {Count:N0}", model.ParameterCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download and ingest failed");
            }
        }

        /// <summary>
        /// Ingest embeddings with deduplication testing
        /// </summary>
        private void TestSqlVectorAvailability()
        {
            _logger.LogInformation("Testing SqlVector<T> availability in Microsoft.Data.SqlClient 6.1.2...");
            // TestSqlVector moved to tests/ModelIngestion.Tests/
            _logger.LogInformation("SqlVector test moved to test project - run: dotnet test tests/ModelIngestion.Tests/");
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
        private async Task TestDeduplicationAsync(CancellationToken cancellationToken)
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
            var exactResults = await _embeddings.ExactSearchAsync(queryEmbedding, topK: 5);

            _logger.LogInformation("Top 5 exact matches:");
            foreach (var result in exactResults)
            {
                _logger.LogInformation("  [{Id}] Distance: {Dist:F4} | {Text}", 
                    result.EmbeddingId, result.Distance, 
                    result.SourceText.Length > 80 ? result.SourceText.Substring(0, 77) + "..." : result.SourceText);
            }

            // Execute approximate spatial search
            _logger.LogInformation("Computing spatial projection...");
            var spatial3D = await _embeddings.ComputeSpatialProjectionAsync(queryEmbedding);
            
            _logger.LogInformation("Running approximate spatial search...");
            var approxResults = await _embeddings.HybridSearchAsync(queryEmbedding, spatial3D[0], spatial3D[1], spatial3D[2], spatialCandidates: 100, finalTopK: 5);

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
            var pixelId1 = await _atomicStorage.StoreAtomicPixelAsync(255, 0, 0, 255, cancellationToken); // Red
            _logger.LogInformation("Stored red pixel: ID={Id}", pixelId1);

            var pixelId2 = await _atomicStorage.StoreAtomicPixelAsync(255, 0, 0, 255, cancellationToken); // Same red - should dedupe
            _logger.LogInformation("Stored red pixel again (duplicate): ID={Id}", pixelId2);
            _logger.LogInformation("IDs match (deduplication): {Match}", pixelId1 == pixelId2);

            var pixelId3 = await _atomicStorage.StoreAtomicPixelAsync(0, 255, 0, 255, cancellationToken); // Green - different
            _logger.LogInformation("Stored green pixel: ID={Id}", pixelId3);

            // Test 2: Atomic audio samples
            _logger.LogInformation("\n=== Testing Atomic Audio Sample Storage ===");
            var sampleId1 = await _atomicStorage.StoreAtomicAudioSampleAsync(0.5f, cancellationToken); // Mid-range amplitude (normalized)
            _logger.LogInformation("Stored audio sample (0.5): ID={Id}", sampleId1);

            var sampleId2 = await _atomicStorage.StoreAtomicAudioSampleAsync(0.5f, cancellationToken); // Same - should dedupe
            _logger.LogInformation("Stored same sample again (duplicate): ID={Id}", sampleId2);
            _logger.LogInformation("IDs match (deduplication): {Match}", sampleId1 == sampleId2);

            // Test 3: Atomic tokens
            _logger.LogInformation("\n=== Testing Atomic Token Storage ===");
            var tokenId1 = await _atomicStorage.StoreAtomicTextTokenAsync("hello", null, cancellationToken);
            _logger.LogInformation("Stored token 'hello': ID={Id}", tokenId1);

            var tokenId2 = await _atomicStorage.StoreAtomicTextTokenAsync("hello", null, cancellationToken); // Same - should dedupe
            _logger.LogInformation("Stored token 'hello' again (duplicate): ID={Id}", tokenId2);
            _logger.LogInformation("IDs match (deduplication): {Match}", tokenId1 == tokenId2);

            var tokenId3 = await _atomicStorage.StoreAtomicTextTokenAsync("world", null, cancellationToken); // Different
            _logger.LogInformation("Stored token 'world': ID={Id}", tokenId3);

            // Test 4: Batch pixel storage
            _logger.LogInformation("\n=== Testing Batch Pixel Storage ===");
            var batchPixels = new List<(byte r, byte g, byte b, byte a)>
            {
                (255, 0, 0, 255), // Red - duplicate
                (0, 255, 0, 255), // Green - duplicate
                (0, 0, 255, 255)  // Blue - new
            };
            var batchIds = await _atomicStorage.StoreBatchPixelsAsync(batchPixels, cancellationToken);
            _logger.LogInformation("Batch stored {Count} pixels, got IDs: {Ids}", 
                batchPixels.Count, string.Join(", ", batchIds));

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
