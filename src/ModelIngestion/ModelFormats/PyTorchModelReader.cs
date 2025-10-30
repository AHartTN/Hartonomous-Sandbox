using TorchSharp;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace ModelIngestion.ModelFormats
{
    /// <summary>
    /// PyTorch model reader - reads .pt/.pth files and outputs Core entities
    /// Uses TorchSharp library for loading and inspecting PyTorch models
    /// </summary>
    public class PyTorchModelReader : IModelFormatReader<PyTorchMetadata>
    {
        private readonly ILogger<PyTorchModelReader> _logger;

        public string FormatName => "PyTorch";
        public IEnumerable<string> SupportedExtensions => new[] { ".pt", ".pth" };

        public PyTorchModelReader(ILogger<PyTorchModelReader> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<Hartonomous.Core.Entities.Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reading PyTorch model from: {Path}", modelPath);

            var model = new Hartonomous.Core.Entities.Model
            {
                ModelName = Path.GetFileNameWithoutExtension(modelPath),
                ModelType = "PyTorch",
                IngestionDate = System.DateTime.UtcNow,
                Layers = new List<ModelLayer>()
            };

            try
            {
                // Load the PyTorch model using TorchSharp
                using var torchModel = torch.jit.load(modelPath);

                // Get model metadata
                var metadata = await GetMetadataAsync(modelPath, cancellationToken);

                // Set architecture from metadata if available
                if (!string.IsNullOrEmpty(metadata.Architecture))
                {
                    model.Architecture = metadata.Architecture;
                }

                // Store configuration as JSON
                model.Config = System.Text.Json.JsonSerializer.Serialize(new
                {
                    model_type = metadata.ModelType,
                    architecture = metadata.Architecture,
                    num_layers = metadata.NumLayers,
                    hidden_size = metadata.HiddenSize,
                    intermediate_size = metadata.IntermediateSize,
                    num_attention_heads = metadata.NumAttentionHeads,
                    vocab_size = metadata.VocabSize,
                    max_position_embeddings = metadata.MaxPositionEmbeddings,
                    activation_function = metadata.ActivationFunction,
                    rms_norm_eps = metadata.RmsNormEps
                });

                // Extract layers from the model
                var layerIdx = 0;

                // Try to get named modules/parameters from the model
                try
                {
                    // Get all named parameters
                    var namedParameters = torchModel.named_parameters();
                    foreach (var param in namedParameters)
                    {
                        var layer = new ModelLayer
                        {
                            LayerIdx = layerIdx++,
                            LayerName = param.name,
                            LayerType = "Parameter",
                            Parameters = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                shape = param.parameter.shape,
                                dtype = param.parameter.dtype.ToString(),
                                requires_grad = param.parameter.requires_grad
                            })
                        };

                        model.Layers.Add(layer);
                        _logger.LogDebug("Added parameter layer: {LayerName}", layer.LayerName);
                    }

                    // Get named modules (layers)
                    var namedModules = torchModel.named_modules();
                    foreach (var module in namedModules)
                    {
                        // Skip the root module
                        if (string.IsNullOrEmpty(module.name)) continue;

                        var layer = new ModelLayer
                        {
                            LayerIdx = layerIdx++,
                            LayerName = module.name,
                            LayerType = module.module.GetType().Name,
                            Parameters = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                module_type = module.module.GetType().Name,
                                // Additional module-specific parameters could be extracted here
                            })
                        };

                        model.Layers.Add(layer);
                        _logger.LogDebug("Added module layer: {LayerName}", layer.LayerName);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "Could not extract detailed layer information from PyTorch model");

                    // Fallback: create a single layer with basic info
                    var fallbackLayer = new ModelLayer
                    {
                        LayerIdx = 0,
                        LayerName = "model",
                        LayerType = "PyTorchModel",
                        Parameters = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            file_path = modelPath,
                            file_size = new FileInfo(modelPath).Length
                        })
                    };

                    model.Layers.Add(fallbackLayer);
                }

                _logger.LogInformation("✓ PyTorch model parsed: {LayerCount} layers", model.Layers.Count);
                return model;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to read PyTorch model from: {Path}", modelPath);
                throw;
            }
        }

        public async Task<PyTorchMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            var metadata = new PyTorchMetadata();

            try
            {
                using var torchModel = torch.jit.load(modelPath);

                // Try to extract metadata from the model
                // Note: This is limited by what TorchSharp exposes
                metadata.ModelType = "PyTorch JIT";

                // Get basic model information
                var namedParameters = torchModel.named_parameters();
                metadata.StateDict = new Dictionary<string, object>();

                foreach (var param in namedParameters)
                {
                    metadata.StateDict[param.name] = new
                    {
                        shape = param.parameter.shape,
                        dtype = param.parameter.dtype.ToString(),
                        requires_grad = param.parameter.requires_grad
                    };
                }

                // Try to infer architecture from parameter names
                var paramNames = namedParameters.Select(p => p.name).ToList();
                if (paramNames.Any(n => n.Contains("attention") || n.Contains("attn")))
                {
                    metadata.Architecture = "Transformer";
                }
                else if (paramNames.Any(n => n.Contains("conv")))
                {
                    metadata.Architecture = "CNN";
                }
                else if (paramNames.Any(n => n.Contains("lstm") || n.Contains("rnn")))
                {
                    metadata.Architecture = "RNN";
                }

                // Try to extract common transformer parameters
                try
                {
                    // Look for common parameter patterns to extract dimensions
                    var embedTokens = namedParameters.FirstOrDefault(p => p.name.Contains("embed_tokens"));
                    if (embedTokens.parameter is not null)
                    {
                        metadata.VocabSize = (int?)embedTokens.parameter.shape[0];
                        metadata.HiddenSize = (int?)embedTokens.parameter.shape[1];
                    }

                    var layers = namedParameters.Where(p => p.name.Contains("layers")).GroupBy(p => p.name.Split('.')[1]).Count();
                    if (layers > 0)
                    {
                        metadata.NumLayers = layers;
                    }
                }
                catch
                {
                    // Ignore metadata extraction errors
                }

                metadata.HasTokenizer = false; // Would need separate tokenizer file

                _logger.LogDebug("Extracted PyTorch metadata: {ParamCount} parameters", namedParameters.Count());
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract detailed metadata from PyTorch model");
                metadata.ModelType = "PyTorch";
            }

            return await Task.FromResult(metadata);
        }

        public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            try
            {
                // Quick validation by attempting to load the model
                using var torchModel = torch.jit.load(modelPath);
                return await Task.FromResult(torchModel is not null);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "PyTorch validation failed for: {Path}", modelPath);
                return await Task.FromResult(false);
            }
        }
    }
}