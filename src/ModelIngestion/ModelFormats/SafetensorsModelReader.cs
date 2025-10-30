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

        public string FormatName => "Safetensors";
        public IEnumerable<string> SupportedExtensions => new[] { ".safetensors" };

        public SafetensorsModelReader(ILogger<SafetensorsModelReader> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
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
                        foreach (var tensorEntry in header.Tensors)
                        {
                            var tensorInfo = JsonSerializer.Deserialize<SafetensorTensorInfo>(tensorEntry.Value.GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (tensorInfo != null)
                            {
                                var layer = new Hartonomous.Core.Entities.ModelLayer
                                {
                                    LayerIdx = layerIdx,
                                    LayerName = tensorEntry.Key,
                                    LayerType = tensorInfo.DType ?? "Unknown",
                                    Parameters = JsonSerializer.Serialize(new
                                    {
                                        shape = tensorInfo.Shape,
                                        data_offsets = tensorInfo.DataOffsets
                                    }, new JsonSerializerOptions { WriteIndented = false })
                                };

                                // Note: In production, weights would be stored as VECTOR columns
                                // For now, we just record the metadata
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