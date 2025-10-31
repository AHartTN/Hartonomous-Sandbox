using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ModelIngestion.ModelFormats
{
    /// <summary>
    /// Safetensors model reader - reads .safetensors files and outputs Core entities
    /// Safetensors is a binary format with JSON header containing tensor metadata
    /// </summary>
    public class SafetensorsModelReader : IModelFormatReader<SafetensorsMetadata>
    {
        private readonly ILogger<SafetensorsModelReader> _logger;
        private readonly IModelLayerRepository _layerRepository;

        public string FormatName => "Safetensors";
        public IEnumerable<string> SupportedExtensions => new[] { ".safetensors" };

        public SafetensorsModelReader(ILogger<SafetensorsModelReader> logger, IModelLayerRepository layerRepository)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _layerRepository = layerRepository ?? throw new System.ArgumentNullException(nameof(layerRepository));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Console app, not trimming")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Console app, not AOT")]
        public async Task<Hartonomous.Core.Entities.Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reading Safetensors model from: {Path}", modelPath);

            var model = new Hartonomous.Core.Entities.Model
            {
                ModelName = Path.GetFileNameWithoutExtension(modelPath),
                ModelType = "Safetensors",
                IngestionDate = System.DateTime.UtcNow,
                Layers = new List<Hartonomous.Core.Entities.ModelLayer>()
            };

            using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    // 1. Read the header length
                    var headerLength = reader.ReadInt64();

                    // 2. Read the header
                    var headerBytes = reader.ReadBytes((int)headerLength);
                    var headerJson = Encoding.UTF8.GetString(headerBytes);

                    var header = JsonSerializer.Deserialize<SafetensorsHeader>(headerJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (header?.Metadata != null && header.Metadata.TryGetValue("format", out var format))
                    {
                        model.Architecture = format;
                    }

                    // 3. Read the tensor data
                    if (header?.Tensors != null)
                    {
                        var layerIdx = 0;
                        var dataStartOffset = 8 + headerLength;

                        foreach (var tensorEntry in header.Tensors)
                        {
                            var tensorInfo = JsonSerializer.Deserialize<SafetensorTensorInfo>(tensorEntry.Value.GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (tensorInfo != null && tensorInfo.DataOffsets != null && tensorInfo.DataOffsets.Count == 2)
                            {
                                var startOffset = dataStartOffset + tensorInfo.DataOffsets[0];
                                var endOffset = dataStartOffset + tensorInfo.DataOffsets[1];
                                var dataLength = endOffset - startOffset;

                                fileStream.Seek(startOffset, SeekOrigin.Begin);

                                float[]? weights = null;
                                var tensorShape = tensorInfo.Shape != null ? $"[{string.Join(",", tensorInfo.Shape)}]" : "[]";
                                var dtype = tensorInfo.DType ?? "float32";

                                if (tensorInfo.Shape != null && tensorInfo.Shape.Count > 0)
                                {
                                    var numElements = tensorInfo.Shape.Aggregate(1L, (a, b) => a * b);
                                    if (numElements > 0 && numElements <= int.MaxValue)
                                    {
                                        weights = ReadTensorData(reader, dtype, (int)numElements, dataLength);
                                    }
                                }

                                var layer = new Hartonomous.Core.Entities.ModelLayer
                                {
                                    LayerIdx = layerIdx,
                                    LayerName = tensorEntry.Key,
                                    LayerType = tensorInfo.DType ?? "Unknown",
                                    TensorShape = tensorShape,
                                    TensorDtype = dtype,
                                    Parameters = JsonSerializer.Serialize(new
                                    {
                                        shape = tensorInfo.Shape,
                                        data_offsets = tensorInfo.DataOffsets
                                    }, new JsonSerializerOptions { WriteIndented = false })
                                };

                                if (weights != null && weights.Length > 0)
                                {
                                    layer.WeightsGeometry = _layerRepository.CreateGeometryFromWeights(weights);
                                    layer.ParameterCount = weights.Length;
                                }

                                model.Layers.Add(layer);
                                layerIdx++;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("✓ Safetensors model parsed: {LayerCount} layers", model.Layers.Count);
            return model;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Console app, not trimming")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Console app, not AOT")]
        public async Task<SafetensorsMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            var metadata = new SafetensorsMetadata();

            using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    var headerLength = reader.ReadInt64();
                    var headerBytes = reader.ReadBytes((int)headerLength);
                    var headerJson = Encoding.UTF8.GetString(headerBytes);

                    var header = JsonSerializer.Deserialize<SafetensorsHeader>(headerJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (header?.Metadata != null)
                    {
                        metadata.GlobalMetadata = header.Metadata;
                        if (header.Metadata.TryGetValue("format", out var format))
                            metadata.Format = format;
                        if (header.Metadata.TryGetValue("architecture", out var arch))
                            metadata.Architecture = arch;
                    }

                    if (header?.Tensors != null)
                    {
                        metadata.TensorCount = header.Tensors.Count;
                        metadata.Tensors = new Dictionary<string, SafetensorsTensorInfo>();

                        foreach (var tensorEntry in header.Tensors)
                        {
                            var tensorInfo = JsonSerializer.Deserialize<SafetensorTensorInfo>(tensorEntry.Value.GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (tensorInfo != null)
                            {
                                metadata.Tensors[tensorEntry.Key] = new SafetensorsTensorInfo
                                {
                                    DType = tensorInfo.DType,
                                    Shape = tensorInfo.Shape?.ToArray(),
                                    DataOffsets = tensorInfo.DataOffsets?.ToArray()
                                };
                            }
                        }
                    }

                    metadata.TotalSizeBytes = fileStream.Length;
                }
            }

            return await Task.FromResult(metadata);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Console app, not trimming")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Console app, not AOT")]
        public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(fileStream))
                    {
                        // Check for valid safetensors header
                        var headerLength = reader.ReadInt64();
                        if (headerLength <= 0 || headerLength > fileStream.Length)
                            return await Task.FromResult(false);

                        var headerBytes = reader.ReadBytes((int)headerLength);
                        var headerJson = Encoding.UTF8.GetString(headerBytes);

                        // Try to parse as JSON
                        JsonSerializer.Deserialize<object>(headerJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return await Task.FromResult(true);
                    }
                }
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        private float[] ReadTensorData(BinaryReader reader, string dtype, int numElements, long dataLength)
        {
            var weights = new float[numElements];

            switch (dtype.ToLowerInvariant())
            {
                case "f32":
                case "float32":
                    for (int i = 0; i < numElements && i * 4 < dataLength; i++)
                    {
                        weights[i] = reader.ReadSingle();
                    }
                    break;

                case "f16":
                case "float16":
                    for (int i = 0; i < numElements && i * 2 < dataLength; i++)
                    {
                        var halfBits = reader.ReadUInt16();
                        weights[i] = HalfToFloat(halfBits);
                    }
                    break;

                case "bf16":
                case "bfloat16":
                    for (int i = 0; i < numElements && i * 2 < dataLength; i++)
                    {
                        var bfloat16Bits = reader.ReadUInt16();
                        weights[i] = BFloat16ToFloat(bfloat16Bits);
                    }
                    break;

                default:
                    _logger.LogWarning("Unsupported dtype: {DType}, skipping tensor data", dtype);
                    break;
            }

            return weights;
        }

        private static float HalfToFloat(ushort halfBits)
        {
            uint sign = (uint)(halfBits & 0x8000) << 16;
            uint exponent = (uint)(halfBits & 0x7C00) >> 10;
            uint mantissa = (uint)(halfBits & 0x03FF);

            if (exponent == 0)
            {
                if (mantissa == 0) return BitConverter.UInt32BitsToSingle(sign);
                exponent = 1;
                while ((mantissa & 0x400) == 0)
                {
                    mantissa <<= 1;
                    exponent--;
                }
                mantissa &= 0x3FF;
            }
            else if (exponent == 31)
            {
                return BitConverter.UInt32BitsToSingle(sign | 0x7F800000 | (mantissa << 13));
            }

            exponent += 127 - 15;
            mantissa <<= 13;

            return BitConverter.UInt32BitsToSingle(sign | (exponent << 23) | mantissa);
        }

        private static float BFloat16ToFloat(ushort bfloat16Bits)
        {
            uint floatBits = (uint)bfloat16Bits << 16;
            return BitConverter.UInt32BitsToSingle(floatBits);
        }
    }

    public class SafetensorsHeader
    {
        [JsonPropertyName("__metadata__")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Tensors { get; set; }
    }

    public class SafetensorTensorInfo
    {
        [JsonPropertyName("dtype")]
        public string? DType { get; set; }

        [JsonPropertyName("shape")]
        public List<long>? Shape { get; set; }

        [JsonPropertyName("data_offsets")]
        public List<long>? DataOffsets { get; set; }
    }
}