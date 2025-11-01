using Microsoft.Extensions.Logging;
using Hartonomous.Core.Services;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Refactored orchestrator for model and embedding ingestion workflows.
    /// Now uses separate, focused services instead of monolithic implementation.
    /// </summary>
    public class IngestionOrchestrator
    {
        private readonly ILogger<IngestionOrchestrator> _logger;
        private readonly IModelRepository _models;
    private readonly ModelDownloader _downloadService;
        private readonly EmbeddingTestService _embeddingTestService;
        private readonly QueryService _queryService;
        private readonly AtomicStorageTestService _atomicTestService;
        private readonly IModelIngestionService _modelIngestion;
        private readonly EmbeddingIngestionService _embeddingService;
        private readonly IAtomicPixelRepository _pixelRepository;
        private readonly IAtomicAudioSampleRepository _audioSampleRepository;
        private readonly IAtomicTextTokenRepository _textTokenRepository;

        /// <summary>
        /// Initializes a new orchestrator that wires together ingestion, download, and validation services.
        /// </summary>
        /// <param name="logger">Diagnostic logger instance.</param>
        /// <param name="models">Repository for model metadata queries.</param>
        /// <param name="downloadService">Service responsible for downloading external model artifacts.</param>
        /// <param name="embeddingTestService">Utility service for embedding ingestion smoke tests.</param>
        /// <param name="queryService">Semantic query execution helper.</param>
        /// <param name="atomicTestService">Atomic storage validation helper.</param>
        /// <param name="modelIngestion">Primary model ingestion workflow.</param>
        /// <param name="embeddingService">Embedding ingestion workflow for production inserts.</param>
        /// <param name="pixelRepository">Repository for pixel atom persistence checks.</param>
        /// <param name="audioSampleRepository">Repository for audio sample atom persistence checks.</param>
        /// <param name="textTokenRepository">Repository for text token atom persistence checks.</param>
        public IngestionOrchestrator(
            ILogger<IngestionOrchestrator> logger,
            IModelRepository models,
            ModelDownloader downloadService,
            EmbeddingTestService embeddingTestService,
            QueryService queryService,
            AtomicStorageTestService atomicTestService,
            IModelIngestionService modelIngestion,
            EmbeddingIngestionService embeddingService,
            IAtomicPixelRepository pixelRepository,
            IAtomicAudioSampleRepository audioSampleRepository,
            IAtomicTextTokenRepository textTokenRepository)
        {
            _logger = logger;
            _models = models;
            _downloadService = downloadService;
            _embeddingTestService = embeddingTestService;
            _queryService = queryService;
            _atomicTestService = atomicTestService;
            _modelIngestion = modelIngestion;
            _embeddingService = embeddingService;
            _pixelRepository = pixelRepository;
            _audioSampleRepository = audioSampleRepository;
            _textTokenRepository = textTokenRepository;
        }

        /// <summary>
        /// Executes a command based on CLI arguments; dispatches to download, ingestion, and diagnostics flows.
        /// </summary>
        /// <param name="args">Command line arguments provided to the worker.</param>
        /// <param name="cancellationToken">Cancellation token used to stop long-running operations.</param>
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

        /// <summary>
        /// Displays CLI usage instructions for available orchestrator commands.
        /// </summary>
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
                var modelDir = await _downloadService.DownloadFromHuggingFaceAsync(modelId, cancellationToken);
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
                var modelPath = await _downloadService.DownloadFromOllamaAsync(modelName, cancellationToken);
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
                var modelPathRoot = await _downloadService.DownloadFromHuggingFaceAsync(modelId, cancellationToken);

                // Find safetensors file
                var modelFiles = Directory.GetFiles(modelPathRoot, "*.safetensors");
                if (modelFiles.Length == 0)
                {
                    modelFiles = Directory.GetFiles(modelPathRoot, "*.onnx");
                }
                if (modelFiles.Length == 0)
                {
                    _logger.LogError("No compatible model files found in: {Dir}", modelPathRoot);
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
            await _embeddingTestService.IngestSampleEmbeddingsAsync(count, cancellationToken);
        }

        /// <summary>
        /// Test deduplication by intentionally inserting duplicates
        /// </summary>
        private async Task TestDeduplicationAsync(CancellationToken cancellationToken)
        {
            await _embeddingTestService.TestDeduplicationAsync(cancellationToken);
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
            await _queryService.ExecuteSemanticQueryAsync(queryText, cancellationToken);
        }

        /// <summary>
        /// Test atomic storage with deduplication
        /// </summary>
        private async Task TestAtomicStorageAsync(CancellationToken cancellationToken)
        {
            await _atomicTestService.TestAtomicStorageAsync(cancellationToken);
        }


    }
}
