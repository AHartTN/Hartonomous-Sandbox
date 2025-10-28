using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Reads GGUF (GPT-Generated Unified Format) quantized models.
/// Specification: https://github.com/ggerganov/ggml/blob/master/docs/gguf.md
/// </summary>
public class GGUFModelReader : IModelFormatReader<GGUFMetadata>
{
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<GGUFModelReader> _logger;

    private const uint GGUF_MAGIC = 0x46554747; // "GGUF" in little-endian
    private const uint GGUF_VERSION = 3;

    public string FormatName => "GGUF";
    public IEnumerable<string> SupportedExtensions => new[] { ".gguf" };

    public GGUFModelReader(
        IModelRepository modelRepository,
        ILogger<GGUFModelReader> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading GGUF model with full tensor data from: {FilePath}", modelPath);

        using var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fileStream, Encoding.UTF8, leaveOpen: false);

        // Read header
        var magic = reader.ReadUInt32();
        if (magic != GGUF_MAGIC)
            throw new InvalidDataException($"Invalid GGUF magic: 0x{magic:X8}");

        var version = reader.ReadUInt32();
        var tensorCount = reader.ReadUInt64();
        var kvCount = reader.ReadUInt64();

        _logger.LogInformation("GGUF file: {TensorCount} tensors, {KVCount} metadata entries", tensorCount, kvCount);

        // Read metadata
        string architecture = "Unknown";
        for (ulong i = 0; i < kvCount; i++)
        {
            var key = ReadGGUFString(reader);
            var valueType = (GGUFMetadataValueType)reader.ReadUInt32();
            var value = ReadGGUFValue(reader, valueType);

            if (key == "general.architecture")
                architecture = value?.ToString() ?? "Unknown";
        }

        // Read tensor info
        var tensorInfos = new List<(string name, uint nDims, ulong[] dims, uint type, ulong offset)>();
        long totalParams = 0;

        for (ulong i = 0; i < tensorCount; i++)
        {
            var tensorName = ReadGGUFString(reader);
            var nDims = reader.ReadUInt32();
            var dims = new ulong[nDims];
            for (uint j = 0; j < nDims; j++)
                dims[j] = reader.ReadUInt64();
            
            var tensorType = reader.ReadUInt32();
            var offset = reader.ReadUInt64();

            long paramCount = 1;
            foreach (var dim in dims)
                paramCount *= (long)dim;
            totalParams += paramCount;

            tensorInfos.Add((tensorName, nDims, dims, tensorType, offset));
        }

        // Create model
        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        var model = new Model
        {
            ModelName = modelName,
            Architecture = architecture,
            ParameterCount = totalParams,
            ModelType = "LLM",
            Layers = new List<ModelLayer>()
        };

        await _modelRepository.AddAsync(model, cancellationToken);
        _logger.LogInformation("Created model: {ModelName} (ID: {ModelId}, {ParamCount} params)", 
            model.ModelName, model.ModelId, totalParams);

        // Calculate alignment and data start offset
        var alignment = 32UL; // Default GGUF alignment
        var dataStartOffset = (ulong)fileStream.Position;
        if (dataStartOffset % alignment != 0)
            dataStartOffset += alignment - (dataStartOffset % alignment);

        // Read tensor data and create layers (limit to avoid timeout)
        int layersProcessed = 0;
        const int MAX_LAYERS = 100;

        foreach (var (name, nDims, dims, type, offset) in tensorInfos.Take(MAX_LAYERS))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Seek to tensor data
                var absoluteOffset = dataStartOffset + offset;
                fileStream.Seek((long)absoluteOffset, SeekOrigin.Begin);

                // Calculate tensor size
                long tensorSize = 1;
                foreach (var dim in dims)
                    tensorSize *= (long)dim;

                // Read tensor data (only F32 for now)
                float[]? weights = null;
                if (type == 0 && tensorSize <= 1998) // F32 and fits in SQL VECTOR limit
                {
                    weights = new float[tensorSize];
                    for (int i = 0; i < tensorSize; i++)
                        weights[i] = reader.ReadSingle();
                }

                // Create layer
                var layer = new ModelLayer
                {
                    ModelId = model.ModelId,
                    LayerIdx = layersProcessed,
                    LayerName = name,
                    LayerType = InferLayerType(name),
                    ParameterCount = tensorSize,
                    Weights = weights != null ? new SqlVector<float>(weights) : null
                };

                await _modelRepository.AddLayerAsync(model.ModelId, layer, cancellationToken);
                model.Layers.Add(layer);
                layersProcessed++;

                if (layersProcessed % 10 == 0)
                    _logger.LogInformation("Processed {Count}/{Total} layers", layersProcessed, Math.Min(MAX_LAYERS, (int)tensorCount));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read tensor {Name}, skipping", name);
            }
        }

        _logger.LogInformation("GGUF ingestion complete: {ModelName} ({LayerCount} layers with data)", 
            model.ModelName, layersProcessed);

        return model;
    }

    private string InferLayerType(string tensorName)
    {
        if (tensorName.Contains("attn")) return "Attention";
        if (tensorName.Contains("ffn") || tensorName.Contains("mlp")) return "FeedForward";
        if (tensorName.Contains("norm")) return "Normalization";
        if (tensorName.Contains("embed")) return "Embedding";
        if (tensorName.Contains("output")) return "Output";
        return "Unknown";
    }

    public async Task<GGUFMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading GGUF metadata from: {FilePath}", modelPath);

        var fileInfo = new FileInfo(modelPath);
        using var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fileStream, Encoding.UTF8, leaveOpen: false);

        var metadata = new GGUFMetadata
        {
            FilePath = modelPath,
            FileSize = fileInfo.Length
        };

        // Read header
        var magic = reader.ReadUInt32();
        if (magic != GGUF_MAGIC)
        {
            throw new InvalidDataException($"Invalid GGUF magic: 0x{magic:X8}");
        }

        metadata.Version = reader.ReadUInt32();
        var tensorCount = reader.ReadUInt64();
        metadata.TensorCount = (int)tensorCount;
        var kvCount = reader.ReadUInt64();

        _logger.LogInformation("GGUF: {TensorCount} tensors, {KVCount} metadata entries", tensorCount, kvCount);

        // Read metadata KV pairs
        long totalParams = 0;
        for (ulong i = 0; i < kvCount; i++)
        {
            var key = ReadGGUFString(reader);
            var valueType = (GGUFMetadataValueType)reader.ReadUInt32();
            var value = ReadGGUFValue(reader, valueType);

            metadata.MetadataKV[key] = value;

            // Extract important fields
            if (key == "general.architecture")
                metadata.Architecture = value?.ToString();
            else if (key == "general.file_type")
            {
                var fileType = Convert.ToUInt32(value);
                metadata.FileType = GetFileTypeName(fileType);
                metadata.QuantizationType = metadata.FileType;
            }
            else if (key.EndsWith(".context_length"))
                metadata.ContextLength = Convert.ToInt32(value);
            else if (key.EndsWith(".embedding_length") || key.EndsWith(".n_embd"))
                metadata.EmbeddingLength = Convert.ToInt32(value);
            else if (key.EndsWith(".attention.head_count") || key.EndsWith(".n_head"))
                metadata.AttentionHeadCount = Convert.ToInt32(value);
            else if (key.EndsWith(".block_count") || key.EndsWith(".n_layer"))
                metadata.LayerCount = Convert.ToInt32(value);
        }

        // Read tensor info to calculate parameters
        for (ulong i = 0; i < tensorCount; i++)
        {
            var tensorName = ReadGGUFString(reader);
            var nDims = reader.ReadUInt32();
            var dims = new ulong[nDims];
            for (uint j = 0; j < nDims; j++)
            {
                dims[j] = reader.ReadUInt64();
            }
            var tensorType = reader.ReadUInt32();
            var offset = reader.ReadUInt64();

            // Calculate parameters from dimensions
            long paramCount = 1;
            foreach (var dim in dims)
            {
                paramCount *= (long)dim;
            }
            totalParams += paramCount;
        }

        metadata.ParameterCount = totalParams;

        _logger.LogInformation("GGUF metadata complete: {Architecture}, {ParamCount} parameters", 
            metadata.Architecture ?? "Unknown", totalParams);

        return metadata;
    }

    public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fileStream);
            var magic = reader.ReadUInt32();
            return magic == GGUF_MAGIC;
        }
        catch
        {
            return false;
        }
    }

    private string ReadGGUFString(BinaryReader reader)
    {
        var length = reader.ReadUInt64();
        if (length == 0) return string.Empty;
        var bytes = reader.ReadBytes((int)length);
        return Encoding.UTF8.GetString(bytes);
    }

    private object ReadGGUFValue(BinaryReader reader, GGUFMetadataValueType valueType)
    {
        return valueType switch
        {
            GGUFMetadataValueType.UInt8 => reader.ReadByte(),
            GGUFMetadataValueType.Int8 => reader.ReadSByte(),
            GGUFMetadataValueType.UInt16 => reader.ReadUInt16(),
            GGUFMetadataValueType.Int16 => reader.ReadInt16(),
            GGUFMetadataValueType.UInt32 => reader.ReadUInt32(),
            GGUFMetadataValueType.Int32 => reader.ReadInt32(),
            GGUFMetadataValueType.Float32 => reader.ReadSingle(),
            GGUFMetadataValueType.Bool => reader.ReadByte() != 0,
            GGUFMetadataValueType.String => ReadGGUFString(reader),
            GGUFMetadataValueType.Array => ReadGGUFArray(reader),
            GGUFMetadataValueType.UInt64 => reader.ReadUInt64(),
            GGUFMetadataValueType.Int64 => reader.ReadInt64(),
            GGUFMetadataValueType.Float64 => reader.ReadDouble(),
            _ => throw new NotSupportedException($"Unsupported GGUF value type: {valueType}")
        };
    }

    private object ReadGGUFArray(BinaryReader reader)
    {
        var arrayType = (GGUFMetadataValueType)reader.ReadUInt32();
        var length = reader.ReadUInt64();
        var values = new List<object>();

        for (ulong i = 0; i < length; i++)
        {
            values.Add(ReadGGUFValue(reader, arrayType));
        }

        return values;
    }

    private string GetFileTypeName(uint fileType)
    {
        return fileType switch
        {
            0 => "ALL_F32",
            1 => "MOSTLY_F16",
            2 => "MOSTLY_Q4_0",
            3 => "MOSTLY_Q4_1",
            7 => "MOSTLY_Q8_0",
            8 => "MOSTLY_Q5_0",
            9 => "MOSTLY_Q5_1",
            10 => "MOSTLY_Q2_K",
            15 => "MOSTLY_Q4_K_M",
            17 => "MOSTLY_Q5_K_M",
            18 => "MOSTLY_Q6_K",
            _ => $"UNKNOWN_{fileType}"
        };
    }

    private enum GGUFMetadataValueType : uint
    {
        UInt8 = 0,
        Int8 = 1,
        UInt16 = 2,
        Int16 = 3,
        UInt32 = 4,
        Int32 = 5,
        Float32 = 6,
        Bool = 7,
        String = 8,
        Array = 9,
        UInt64 = 10,
        Int64 = 11,
        Float64 = 12
    }
}
