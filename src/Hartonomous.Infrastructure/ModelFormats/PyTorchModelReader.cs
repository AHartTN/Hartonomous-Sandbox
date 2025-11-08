using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Interfaces.ModelFormats;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.ModelFormats
{
    /// <summary>
    /// PyTorch model reader - reads .pt/.pth files and outputs Core entities.
    /// Relies on a loader abstraction so tests can supply lightweight fixtures.
    /// </summary>
    public class PyTorchModelReader : IModelFormatReader<PyTorchMetadata>
    {
        private readonly ILogger<PyTorchModelReader> _logger;
        private readonly IModelLayerRepository _layerRepository;
        private readonly IPyTorchModelLoader _modelLoader;

        public string FormatName => "PyTorch";
        public IEnumerable<string> SupportedExtensions => new[] { ".pt", ".pth" };

        public PyTorchModelReader(
            ILogger<PyTorchModelReader> logger,
            IModelLayerRepository layerRepository,
            IPyTorchModelLoader? modelLoader = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
            _modelLoader = modelLoader ?? new TorchSharpModelLoader(logger);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Console ingestion tool - trimming not enabled")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Console ingestion tool - native AOT not used")]
        public Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(modelPath);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Reading PyTorch model from: {Path}", modelPath);

            PyTorchModelLoadResult loadResult;
            try
            {
                loadResult = _modelLoader.Load(modelPath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load PyTorch model: {Path}", modelPath);
                throw;
            }

            var metadata = BuildMetadata(loadResult);

            var model = new Model
            {
                ModelName = Path.GetFileNameWithoutExtension(modelPath),
                ModelType = metadata.ModelType ?? "PyTorch",
                Architecture = metadata.Architecture,
                IngestionDate = DateTime.UtcNow,
                Layers = new List<ModelLayer>()
            };

            model.Config = JsonSerializer.Serialize(new
            {
                ModelType = metadata.ModelType,
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

            var layerIdx = 0;
            long totalParameters = 0;

            foreach (var parameter in loadResult.Parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (parameter.Weights is null || parameter.Weights.Length == 0)
                {
                    _logger.LogDebug("Skipping parameter {Param} with no weights", parameter.Name);
                    continue;
                }

                var tensorShape = parameter.Shape.Length > 0
                    ? $"[{string.Join(",", parameter.Shape)}]"
                    : "[]";

                var layer = new ModelLayer
                {
                    LayerIdx = layerIdx++,
                    LayerName = parameter.Name,
                    LayerType = "Parameter",
                    TensorShape = tensorShape,
                    TensorDtype = parameter.DType,
                    ParameterCount = parameter.Weights.Length,
                    Parameters = JsonSerializer.Serialize(new
                    {
                        shape = parameter.Shape,
                        dtype = parameter.DType,
                        requires_grad = parameter.RequiresGrad
                    }),
                    WeightsGeometry = _layerRepository.CreateGeometryFromWeights(parameter.Weights)
                };

                model.Layers.Add(layer);
                totalParameters += parameter.Weights.LongLength;
                _logger.LogDebug("Added parameter layer: {LayerName} ({Count} weights)", layer.LayerName, layer.ParameterCount);
            }

            if (model.Layers.Count == 0)
            {
                model.Layers.Add(new ModelLayer
                {
                    LayerIdx = 0,
                    LayerName = "model",
                    LayerType = "PyTorchModel",
                    Parameters = JsonSerializer.Serialize(new
                    {
                        file_path = modelPath,
                        file_size = new FileInfo(modelPath).Length
                    })
                });
            }

            if (totalParameters > 0)
            {
                model.ParameterCount = totalParameters;
            }

            _logger.LogInformation("✓ PyTorch model parsed: {LayerCount} layers", model.Layers.Count);
            return Task.FromResult(model);
        }

        public Task<PyTorchMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var loadResult = _modelLoader.Load(modelPath, cancellationToken);
                var metadata = BuildMetadata(loadResult);
                _logger.LogDebug("Extracted PyTorch metadata: {ParamCount} parameters", loadResult.Parameters.Count);
                return Task.FromResult(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract PyTorch metadata from: {Path}", modelPath);
                return Task.FromResult(new PyTorchMetadata { ModelType = "PyTorch" });
            }
        }

        public Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var loadResult = _modelLoader.Load(modelPath, cancellationToken);
                return Task.FromResult(loadResult.Parameters.Count > 0 || loadResult.StateDict.Count > 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PyTorch validation failed for: {Path}", modelPath);
                return Task.FromResult(false);
            }
        }

        private PyTorchMetadata BuildMetadata(PyTorchModelLoadResult loadResult)
        {
            var metadata = new PyTorchMetadata
            {
                ModelType = "PyTorch JIT",
                Architecture = InferArchitecture(loadResult.Parameters.Select(p => p.Name)),
                RawConfig = new Dictionary<string, object>(),
                ShardFiles = new List<string>(),
                HasTokenizer = false,
                StateDict = loadResult.StateDict
            };

            var embedTokens = loadResult.Parameters.FirstOrDefault(p => p.Name.Contains("embed_tokens", StringComparison.OrdinalIgnoreCase));
            if (embedTokens is not null && embedTokens.Shape.Length >= 2)
            {
                metadata.VocabSize = (int)embedTokens.Shape[0];
                metadata.HiddenSize = (int)embedTokens.Shape[1];
            }

            metadata.NumLayers = CalculateLayerCount(loadResult.Parameters.Select(p => p.Name));

            if (metadata.HiddenSize is null)
            {
                var qProj = loadResult.Parameters.FirstOrDefault(p => p.Name.EndsWith("q_proj.weight", StringComparison.OrdinalIgnoreCase));
                if (qProj is not null && qProj.Shape.Length >= 2)
                {
                    metadata.HiddenSize = (int)qProj.Shape[1];
                }
            }

            if (metadata.IntermediateSize is null)
            {
                var ffn = loadResult.Parameters.FirstOrDefault(p => p.Name.Contains("ffn", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("mlp", StringComparison.OrdinalIgnoreCase));
                if (ffn is not null && ffn.Shape.Length >= 1)
                {
                    metadata.IntermediateSize = (int)ffn.Shape[0];
                }
            }

            return metadata;
        }

        private static string? InferArchitecture(IEnumerable<string> parameterNames)
        {
            foreach (var name in parameterNames)
            {
                if (name.Contains("attention", StringComparison.OrdinalIgnoreCase) || name.Contains("attn", StringComparison.OrdinalIgnoreCase))
                {
                    return "Transformer";
                }

                if (name.Contains("conv", StringComparison.OrdinalIgnoreCase))
                {
                    return "CNN";
                }

                if (name.Contains("lstm", StringComparison.OrdinalIgnoreCase) || name.Contains("rnn", StringComparison.OrdinalIgnoreCase))
                {
                    return "RNN";
                }
            }

            return null;
        }

        private static int? CalculateLayerCount(IEnumerable<string> parameterNames)
        {
            var layerIndices = new HashSet<int>();

            foreach (var name in parameterNames)
            {
                var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    continue;
                }

                if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                {
                    layerIndices.Add(index);
                }
            }

            return layerIndices.Count > 0 ? layerIndices.Count : null;
        }
    }
}

