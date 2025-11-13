using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using Hartonomous.Infrastructure.Services.ModelFormats;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.ModelFormats.Readers;

/// <summary>
/// Safetensors model reader - reads .safetensors files and outputs Core entities
/// Safetensors is a binary format with JSON header containing tensor metadata
/// </summary>
public class SafetensorsModelReader : IModelFormatReader<SafetensorsMetadata>
{
    private readonly ILogger<SafetensorsModelReader> _logger;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IModelLayerRepository _layerRepository;

    public string FormatName => "Safetensors";
    public IEnumerable<string> SupportedExtensions => new[] { ".safetensors" };

    public SafetensorsModelReader(ILogger<SafetensorsModelReader> logger, IModelLayerRepository layerRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
    }

    public async Task<Model> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading Safetensors model from: {Path}", modelPath);

        var model = new Model
        {
            ModelName = Path.GetFileNameWithoutExtension(modelPath),
            ModelType = "Safetensors",
            IngestionDate = DateTime.UtcNow,
            Layers = new List<ModelLayer>()
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

                var header = JsonSerializer.Deserialize<SafetensorsHeader>(headerJson, SerializerOptions);

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
                        var tensorInfo = JsonSerializer.Deserialize<SafetensorTensorInfo>(tensorEntry.Value.GetRawText(), SerializerOptions);

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
                                    weights = TensorDataReader.Read(reader, dtype, (int)numElements, dataLength, _logger);
                                }
                            }

                            var layer = new ModelLayer
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
                                }, SerializerOptions)
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

                var header = JsonSerializer.Deserialize<SafetensorsHeader>(headerJson, SerializerOptions);

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
                        var tensorInfo = JsonSerializer.Deserialize<SafetensorTensorInfo>(tensorEntry.Value.GetRawText(), SerializerOptions);

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
                    JsonSerializer.Deserialize<object>(headerJson, SerializerOptions);

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
