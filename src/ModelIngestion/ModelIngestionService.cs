using Microsoft.Extensions.Logging;
using Hartonomous.Core.Services;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Production service for ingesting AI models into Hartonomous.
    /// Supports: Safetensors (Llama 4, FLUX), ONNX, PyTorch, GGUF formats.
    /// Now uses generic processors and factories for extensibility.
    /// Focused solely on ingestion operations.
    /// </summary>
    public class ModelIngestionService : BaseService, IModelIngestionService
    {
        private readonly IModelRepository _repository;
        private readonly ModelIngestionProcessor _processor;
        private readonly IIngestionStatisticsService _statisticsService;

        public ModelIngestionService(
            ILogger<ModelIngestionService> logger,
            IModelRepository repository,
            ModelIngestionProcessor processor,
            IIngestionStatisticsService statisticsService)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        }

        public override string ServiceName => "ModelIngestionService";

        /// <summary>
        /// Ingest a model from file or directory path.
        /// Auto-detects format using generic factory pattern.
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

            Logger.LogInformation("Starting model ingestion from: {Path}", modelPath);

            try
            {
                // Create ingestion request
                var request = new ModelIngestionRequest
                {
                    ModelPath = modelPath,
                    CustomName = modelName
                };

                // Process using generic processor
                var result = await _processor.ProcessAsync(request, cancellationToken);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Ingestion failed: {result.ErrorMessage}");
                }

                // Verify ingestion by reloading from database
                var verifyModel = await _repository.GetByIdAsync(result.ModelId, cancellationToken);
                if (verifyModel == null)
                {
                    Logger.LogError("Model ingestion verification failed - model not found in database");
                    throw new InvalidOperationException("Model ingestion failed verification");
                }

                Logger.LogInformation(
                    "Ingestion verified: {ModelName} ({Architecture}), {ParamCount} parameters",
                    result.Model?.ModelName,
                    result.Model?.Architecture,
                    result.Model?.ParameterCount);

                return result.ModelId;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Model ingestion failed for: {Path}", modelPath);
                throw;
            }
        }

        /// <summary>
        /// Ingest multiple models from a directory (batch ingestion).
        /// Uses parallel processing for efficiency.
        /// </summary>
        public async Task<int[]> IngestDirectoryAsync(
            string directoryPath,
            string searchPattern = "*",
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            Logger.LogInformation("Batch ingestion from directory: {Path}", directoryPath);

            var modelIds = new List<int>();
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
                    Logger.LogWarning(ex, "Skipping file due to error: {File}", file);
                }
            }

            Logger.LogInformation("Batch ingestion complete: {Count} models ingested", modelIds.Count);
            return modelIds.ToArray();
        }

        /// <summary>
        /// Get ingestion statistics from the statistics service.
        /// </summary>
        public async Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            return await _statisticsService.GetStatsAsync(cancellationToken);
        }
    }
}
