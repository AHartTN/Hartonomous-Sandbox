using Microsoft.ML.OnnxRuntime;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ModelIngestion.ModelFormats
{
    /// <summary>
    /// ONNX model reader - reads .onnx files and outputs Core entities
    /// </summary>
    public class OnnxModelReader : IModelFormatReader<OnnxMetadata>
    {
        private readonly ILogger<OnnxModelReader> _logger;

        public string FormatName => "ONNX";
        public IEnumerable<string> SupportedExtensions => new[] { ".onnx" };

        public OnnxModelReader(ILogger<OnnxModelReader> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<Hartonomous.Core.Entities.Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reading ONNX model from: {Path}", modelPath);

            using var session = new InferenceSession(modelPath);
            var metadata = await GetMetadataAsync(modelPath, cancellationToken);

            // Create Core entity (DO NOT persist here - return for service layer)
            var model = new Hartonomous.Core.Entities.Model
            {
                ModelName = session.ModelMetadata.GraphName ?? System.IO.Path.GetFileNameWithoutExtension(modelPath),
                ModelType = "ONNX",
                Architecture = session.ModelMetadata.Domain,
                Config = System.Text.Json.JsonSerializer.Serialize(new
                {
                    inputs = session.InputMetadata.Count,
                    outputs = session.OutputMetadata.Count,
                    producer = session.ModelMetadata.ProducerName
                }),
                IngestionDate = System.DateTime.UtcNow,
                Layers = new List<ModelLayer>() // Initialize collection
            };

            // Add input layers
            int layerIdx = 0;
            foreach (var input in session.InputMetadata)
            {
                var layer = new ModelLayer
                {
                    LayerIdx = layerIdx++,
                    LayerName = input.Key,
                    LayerType = "Input",
                    Parameters = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        shape = input.Value.Dimensions,
                        element_type = input.Value.ElementType.ToString()
                    })
                };

                model.Layers.Add(layer);
                _logger.LogDebug("Added input layer: {LayerName}", layer.LayerName);
            }

            // Add output layers
            foreach (var output in session.OutputMetadata)
            {
                var layer = new ModelLayer
                {
                    LayerIdx = layerIdx++,
                    LayerName = output.Key,
                    LayerType = "Output",
                    Parameters = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        shape = output.Value.Dimensions,
                        element_type = output.Value.ElementType.ToString()
                    })
                };

                model.Layers.Add(layer);
                _logger.LogDebug("Added output layer: {LayerName}", layer.LayerName);
            }

            _logger.LogInformation("✓ ONNX model parsed: {LayerCount} layers", layerIdx);
            return model; // Return entity for service layer to persist
        }

        public async Task<OnnxMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            using var session = new InferenceSession(modelPath);
            var meta = session.ModelMetadata;

            return await Task.FromResult(new OnnxMetadata
            {
                GraphName = meta.GraphName,
                ProducerName = meta.ProducerName,
                Domain = meta.Domain,
                Description = meta.Description,
                Version = meta.Version,
                InputShapes = session.InputMetadata.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Dimensions.Select(d => d.ToString()).ToArray()),
                OutputShapes = session.OutputMetadata.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Dimensions.Select(d => d.ToString()).ToArray())
            });
        }

        public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            try
            {
                using var session = new InferenceSession(modelPath);
                return await Task.FromResult(session != null);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "ONNX validation failed for: {Path}", modelPath);
                return await Task.FromResult(false);
            }
        }
    }
}