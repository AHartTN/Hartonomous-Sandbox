using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ModelIngestion.ModelFormats
{
    public class SafetensorsModelReader : IModelFormatReader<SafetensorsMetadata>
    {
        private readonly ILogger<SafetensorsModelReader> _logger;

        public string FormatName => "Safetensors";
        public IEnumerable<string> SupportedExtensions => new[] { ".safetensors" };

        public SafetensorsModelReader(ILogger<SafetensorsModelReader> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

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
                    var header = JsonConvert.DeserializeObject<SafetensorsHeader>(headerJson);

                    if (header?.Metadata != null && header.Metadata.TryGetValue("format", out var format))
                    {
                        model.Architecture = format;
                    }

                    // 3. Read the tensor data
                    if (header?.Tensors != null)
                    {
                        var layerIdx = 0;
                        foreach (var tensorToken in header.Tensors)
                        {
                            var tensorInfo = tensorToken.Value.ToObject<SafetensorTensorInfo>();
                            if (tensorInfo != null)
                            {
                                var layer = new Hartonomous.Core.Entities.ModelLayer
                                {
                                    LayerIdx = layerIdx,
                                    LayerName = tensorToken.Key,
                                    LayerType = tensorInfo.DType ?? "Unknown",
                                    Parameters = JsonConvert.SerializeObject(new
                                    {
                                        shape = tensorInfo.Shape,
                                        data_offsets = tensorInfo.DataOffsets
                                    })
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
                    var header = JsonConvert.DeserializeObject<SafetensorsHeader>(headerJson);

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

                        foreach (var tensorToken in header.Tensors)
                        {
                            var tensorInfo = tensorToken.Value.ToObject<SafetensorTensorInfo>();
                            if (tensorInfo != null)
                            {
                                metadata.Tensors[tensorToken.Key] = new SafetensorsTensorInfo
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
                        JsonConvert.DeserializeObject(headerJson);
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
        [JsonProperty("__metadata__")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken>? Tensors { get; set; }
    }

    public class SafetensorTensorInfo
    {
        [JsonProperty("dtype")]
        public string? DType { get; set; }

        [JsonProperty("shape")]
        public List<long>? Shape { get; set; }

        [JsonProperty("data_offsets")]
        public List<long>? DataOffsets { get; set; }
    }
}