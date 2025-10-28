using Microsoft.Extensions.Logging;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Production service for ingesting AI models into Hartonomous.
    /// Supports: Safetensors (Llama 4, FLUX), ONNX, PyTorch, GGUF formats.
    /// </summary>
    public class ModelIngestionService
    {
        private readonly ILogger<ModelIngestionService> _logger;
        private readonly IModelRepository _repository;
        private readonly ModelIngestionOrchestrator _orchestrator;

        public ModelIngestionService(
            ILogger<ModelIngestionService> logger,
            IModelRepository repository,
            ModelIngestionOrchestrator orchestrator)
        {
            _logger = logger;
            _repository = repository;
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// Ingest a model from file or directory path.
        /// Auto-detects format (Safetensors, ONNX, PyTorch, GGUF).
        /// </summary>
        public async Task<int> IngestAsync(
            string modelPath, 
            string? modelName = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentException("Model path cannot be empty", nameof(modelPath));

            if (!File.Exists(modelPath) && !Directory.Exists(modelPath))
                throw new FileNotFoundException($"Model path not found: {modelPath}");

            _logger.LogInformation("Starting model ingestion from: {Path}", modelPath);

            try
            {
                // Use orchestrator to auto-detect format and ingest
                var model = await _orchestrator.IngestModelAsync(modelPath, cancellationToken);

                _logger.LogInformation("Model ingested successfully. ModelId={ModelId}", model.ModelId);

                // Verify ingestion by reloading from database
                var verifyModel = await _repository.GetByIdAsync(model.ModelId, cancellationToken);
                if (verifyModel == null)
                {
                    _logger.LogError("Model ingestion verification failed - model not found in database");
                    throw new InvalidOperationException("Model ingestion failed verification");
                }

                _logger.LogInformation(
                    "Ingestion verified: {ModelName} ({Architecture}), {ParamCount} parameters",
                    model.ModelName, 
                    model.Architecture,
                    model.ParameterCount);

                return model.ModelId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model ingestion failed for: {Path}", modelPath);
                throw;
            }
        }

        /// <summary>
        /// Ingest multiple models from a directory (batch ingestion).
        /// </summary>
        public async Task<int[]> IngestDirectoryAsync(
            string directoryPath,
            string searchPattern = "*",
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            _logger.LogInformation("Batch ingestion from directory: {Path}", directoryPath);

            var modelIds = new System.Collections.Generic.List<int>();
            var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                // Skip non-model files
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".safetensors" && ext != ".onnx" && ext != ".pt" && ext != ".pth" && ext != ".gguf")
                    continue;

                try
                {
                    var modelId = await IngestAsync(file, null, cancellationToken);
                    modelIds.Add(modelId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping file due to error: {File}", file);
                }
            }

            _logger.LogInformation("Batch ingestion complete: {Count} models ingested", modelIds.Count);
            return modelIds.ToArray();
        }

        /// <summary>
        /// Get ingestion statistics from database.
        /// </summary>
        public async Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var models = await _repository.GetAllAsync(cancellationToken);
            
            long totalParams = 0;
            long totalLayers = 0;
            var architectures = new System.Collections.Generic.Dictionary<string, int>();

            foreach (var model in models)
            {
                totalParams += model.ParameterCount ?? 0;
                
                var arch = model.Architecture ?? "Unknown";
                architectures[arch] = architectures.GetValueOrDefault(arch, 0) + 1;

                // Count layers for this model
                var layers = await _repository.GetLayersByModelIdAsync(model.ModelId, cancellationToken);
                totalLayers += layers.Count();
            }

            return new IngestionStats
            {
                TotalModels = models.Count(),
                TotalParameters = totalParams,
                TotalLayers = totalLayers,
                ArchitectureBreakdown = architectures
            };
        }
    }

    /// <summary>
    /// Ingestion statistics
    /// </summary>
    public class IngestionStats
    {
        public int TotalModels { get; set; }
        public long TotalParameters { get; set; }
        public long TotalLayers { get; set; }
        public System.Collections.Generic.Dictionary<string, int> ArchitectureBreakdown { get; set; } = new();
    }
}
