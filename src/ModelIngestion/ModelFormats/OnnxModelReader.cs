using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;

namespace ModelIngestion.ModelFormats
{
    /// <summary>
    /// ONNX model reader - reads .onnx files and outputs Core entities
    /// </summary>
    public class OnnxModelReader : IModelFormatReader<OnnxMetadata>
    {
        private readonly ILogger<OnnxModelReader> _logger;
        private readonly IModelLayerRepository _layerRepository;
        private readonly IOnnxModelLoader _modelLoader;

        public string FormatName => "ONNX";
        public IEnumerable<string> SupportedExtensions => new[] { ".onnx" };

        public OnnxModelReader(
            ILogger<OnnxModelReader> logger,
            IModelLayerRepository layerRepository,
            IOnnxModelLoader? modelLoader = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
            _modelLoader = modelLoader ?? new OnnxModelLoader(logger);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Console ingestion tool - trimming not enabled")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Console ingestion tool - native AOT not used")]
        public async Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(modelPath);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Reading ONNX model from: {Path}", modelPath);

            var loadResult = _modelLoader.Load(modelPath, cancellationToken);

            var model = new Model
            {
                ModelName = loadResult.ModelName,
                ModelType = "ONNX",
                Architecture = loadResult.Domain,
                Config = JsonSerializer.Serialize(new
                {
                    graph = loadResult.GraphName,
                    domain = loadResult.Domain,
                    producer = loadResult.ProducerName,
                    description = loadResult.Description,
                    inputs = loadResult.Inputs,
                    outputs = loadResult.Outputs
                }),
                IngestionDate = DateTime.UtcNow,
                Layers = new List<ModelLayer>()
            };

            var layerIdx = 0;
            var totalParameters = 0L;

            foreach (var initializer in loadResult.Initializers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var weights = ExtractWeights(initializer);
                if (weights.Length == 0)
                {
                    _logger.LogDebug("Skipping initializer {Name} with no weights", initializer.Name);
                    continue;
                }

                var dims = initializer.Dimensions.Length > 0 ? initializer.Dimensions : Array.Empty<long>();
                var layer = new ModelLayer
                {
                    LayerIdx = layerIdx,
                    LayerName = string.IsNullOrWhiteSpace(initializer.Name) ? $"initializer_{layerIdx}" : initializer.Name,
                    LayerType = TensorTypeToString(initializer.DataType),
                    TensorShape = dims.Length > 0 ? $"[{string.Join(",", dims)}]" : "[]",
                    TensorDtype = TensorTypeToString(initializer.DataType),
                    Parameters = JsonSerializer.Serialize(new
                    {
                        dims,
                        initializer.DocString
                    }),
                    ParameterCount = weights.LongLength,
                    WeightsGeometry = _layerRepository.CreateGeometryFromWeights(weights)
                };

                model.Layers.Add(layer);
                totalParameters += layer.ParameterCount ?? 0;
                _logger.LogDebug("Added initializer layer {LayerName} with {Count} parameters", layer.LayerName, layer.ParameterCount);
                layerIdx++;
            }

            model.ParameterCount = totalParameters;

            _logger.LogInformation("✓ ONNX model parsed: {LayerCount} layers", model.Layers.Count);
            return model;
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ONNX validation failed for: {Path}", modelPath);
                return await Task.FromResult(false);
            }
        }

        private static float[] ExtractWeights(OnnxInitializerInfo tensor)
        {
            if (tensor == null)
            {
                return Array.Empty<float>();
            }

            var elementCount = tensor.Dimensions.Length == 0 ? 1L : tensor.Dimensions.Aggregate(1L, (acc, dim) => acc * dim);
            if (elementCount <= 0 || elementCount > int.MaxValue)
            {
                return Array.Empty<float>();
            }

            var dataType = tensor.DataType;

            switch (dataType)
            {
                case 1: // float32
                    if (tensor.FloatData.Count > 0)
                    {
                        return tensor.FloatData.ToArray();
                    }
                    return ReadRawFloatData(tensor.RawData);

                case 11: // float64
                    if (tensor.DoubleData.Count > 0)
                    {
                        return tensor.DoubleData.Select(static d => (float)d).ToArray();
                    }
                    return ReadRawDoubleData(tensor.RawData);

                case 10: // float16
                    return ReadRawHalfData(tensor.RawData);

                case 16: // bfloat16
                    return ReadRawBFloat16Data(tensor.RawData);

                default:
                    return Array.Empty<float>();
            }
        }

        private static string TensorTypeToString(int dataType)
        {
            return dataType switch
            {
                1 => "float32",
                2 => "uint8",
                3 => "int8",
                4 => "uint16",
                5 => "int16",
                6 => "int32",
                7 => "int64",
                8 => "string",
                9 => "bool",
                10 => "float16",
                11 => "float64",
                12 => "uint32",
                13 => "uint64",
                14 => "complex64",
                15 => "complex128",
                16 => "bfloat16",
                _ => dataType.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static float[] ReadRawFloatData(byte[] rawData)
        {
            if (rawData == null || rawData.Length == 0)
            {
                return Array.Empty<float>();
            }

            var result = new float[rawData.Length / sizeof(float)];
            Buffer.BlockCopy(rawData, 0, result, 0, rawData.Length);
            return result;
        }

        private static float[] ReadRawDoubleData(byte[] rawData)
        {
            if (rawData == null || rawData.Length == 0)
            {
                return Array.Empty<float>();
            }

            var result = new float[rawData.Length / sizeof(double)];
            for (int i = 0; i < result.Length; i++)
            {
                var value = BitConverter.ToDouble(rawData, i * sizeof(double));
                result[i] = (float)value;
            }
            return result;
        }

        private static float[] ReadRawHalfData(byte[] rawData)
        {
            if (rawData == null || rawData.Length == 0)
            {
                return Array.Empty<float>();
            }

            var result = new float[rawData.Length / sizeof(ushort)];
            for (int i = 0; i < result.Length; i++)
            {
                var halfBits = BitConverter.ToUInt16(rawData, i * sizeof(ushort));
                result[i] = Float16Utilities.HalfToFloat(halfBits);
            }
            return result;
        }

        private static float[] ReadRawBFloat16Data(byte[] rawData)
        {
            if (rawData == null || rawData.Length == 0)
            {
                return Array.Empty<float>();
            }

            var result = new float[rawData.Length / sizeof(ushort)];
            for (int i = 0; i < result.Length; i++)
            {
                var bfloatBits = BitConverter.ToUInt16(rawData, i * sizeof(ushort));
                result[i] = Float16Utilities.BFloat16ToFloat(bfloatBits);
            }
            return result;
        }
    }
}