using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;

namespace ModelIngestion.ModelFormats;

/// <summary>
/// Reads GGUF (GPT-Generated Unified Format) quantized models.
/// Specification: https://github.com/ggerganov/ggml/blob/master/docs/gguf.md
/// 
/// Supports all GGML quantization types:
/// - F32 (0): Full 32-bit floats
/// - F16 (1): 16-bit floats
/// - Q4_0 (2): 4-bit quantization, block size 32
/// - Q4_1 (3): 4-bit quantization with min, block size 32
/// - Q5_0 (6): 5-bit quantization, block size 32
/// - Q5_1 (7): 5-bit quantization with min, block size 32
/// - Q8_0 (8): 8-bit quantization, block size 32
/// - Q2_K (10): 2-bit super-block quantization
/// - Q3_K (11): 3-bit super-block quantization
/// - Q4_K (12): 4-bit super-block quantization
/// - Q5_K (13): 5-bit super-block quantization
/// - Q6_K (14): 6-bit super-block quantization
/// - BF16 (30): Brain Float 16
/// </summary>
public class GGUFModelReader : IModelFormatReader<GGUFMetadata>
{
    private readonly IModelRepository _modelRepository;
    private readonly IModelLayerRepository _layerRepository;
    private readonly ILayerTensorSegmentRepository _tensorSegmentRepository;
    private readonly ILogger<GGUFModelReader> _logger;

    private const uint GGUF_MAGIC = 0x46554747; // "GGUF" in little-endian
    private const uint GGUF_VERSION = 3;
    private const int QK_K = 256; // Super-block size for K-quantizations
    private const int QK4_0 = 32; // Block size for Q4_0
    private const int QK4_1 = 32; // Block size for Q4_1
    private const int QK5_0 = 32; // Block size for Q5_0
    private const int QK5_1 = 32; // Block size for Q5_1
    private const int QK8_0 = 32; // Block size for Q8_0
    private const int PreviewPointLimit = 4096;

    public string FormatName => "GGUF";
    public IEnumerable<string> SupportedExtensions => new[] { ".gguf" };

    public GGUFModelReader(
        IModelRepository modelRepository,
        IModelLayerRepository layerRepository,
        ILayerTensorSegmentRepository tensorSegmentRepository,
        ILogger<GGUFModelReader> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
        _tensorSegmentRepository = tensorSegmentRepository ?? throw new ArgumentNullException(nameof(tensorSegmentRepository));
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

        // Read metadata KV pairs
        string architecture = "Unknown";
        uint alignment = 32; // Default GGUF alignment
        var metadataDict = new Dictionary<string, object>();

        for (ulong i = 0; i < kvCount; i++)
        {
            var key = ReadGGUFString(reader);
            var valueType = (GGUFMetadataValueType)reader.ReadUInt32();
            var value = ReadGGUFValue(reader, valueType);

            metadataDict[key] = value;

            if (key == "general.architecture")
                architecture = value?.ToString() ?? "Unknown";
            else if (key == "general.alignment")
                alignment = Convert.ToUInt32(value);
        }

        _logger.LogInformation("Model architecture: {Architecture}, alignment: {Alignment}", architecture, alignment);

        // Read tensor infos
        var tensorInfos = new List<GGUFTensorInfo>();
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

            tensorInfos.Add(new GGUFTensorInfo
            {
                Name = tensorName,
                Dimensions = dims,
                Type = (GGMLType)tensorType,
                Offset = offset,
                ElementCount = paramCount
            });
        }

        _logger.LogInformation("Total tensors: {Count}, total parameters: {Params}", tensorCount, totalParams);

        // Calculate aligned data section start
        var dataStartOffset = (ulong)fileStream.Position;
        if (dataStartOffset % alignment != 0)
            dataStartOffset += alignment - (dataStartOffset % alignment);

        _logger.LogInformation("Tensor data starts at offset: {Offset} (alignment: {Alignment})", dataStartOffset, alignment);

        var totalDataSectionLength = (ulong)Math.Max(0, fileStream.Length - (long)dataStartOffset);
        for (int t = 0; t < tensorInfos.Count; t++)
        {
            var current = tensorInfos[t];
            var start = current.Offset;
            ulong end;

            if (t + 1 < tensorInfos.Count)
            {
                end = tensorInfos[t + 1].Offset;
            }
            else
            {
                end = totalDataSectionLength;
            }

            current.DataLengthBytes = end > start ? end - start : 0UL;
        }

        // Create model entity
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

        // Read and dequantize tensor data
        int layersProcessed = 0;
        int layersWithWeights = 0;

        foreach (var tensorInfo in tensorInfos)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Seek to tensor data
            var absoluteOffset = (long)(dataStartOffset + tensorInfo.Offset);
            fileStream.Seek(absoluteOffset, SeekOrigin.Begin);

            // Extract raw payload for segment storage
            if (tensorInfo.DataLengthBytes > int.MaxValue)
            {
                _logger.LogWarning(
                    "Tensor {Name} payload ({Size} bytes) exceeds ingestion limit. Skipping layer for now.",
                    tensorInfo.Name,
                    tensorInfo.DataLengthBytes);
                continue;
            }

            var expectedLength = (int)tensorInfo.DataLengthBytes;

            var rawBytes = reader.ReadBytes(expectedLength);

            if (rawBytes.Length != expectedLength)
            {
                _logger.LogWarning(
                    "Tensor {Name} expected {Expected} bytes but read {Actual} bytes. Skipping layer.",
                    tensorInfo.Name,
                    expectedLength,
                    rawBytes.Length);
                continue;
            }

            float[]? previewWeights = null;

            try
            {
                previewWeights = DequantizeTensorPreview(rawBytes, tensorInfo, PreviewPointLimit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate preview for tensor {Tensor}", tensorInfo.Name);
            }

            LineString? previewGeometry = null;

            if (previewWeights != null && previewWeights.Length > 1)
            {
                try
                {
                    previewGeometry = CreateLineStringFromWKT(previewWeights, 0) as LineString;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert preview geometry for tensor {Tensor}", tensorInfo.Name);
                }
            }

            var shapeJson = System.Text.Json.JsonSerializer.Serialize(tensorInfo.Dimensions.Select(d => (long)d).ToArray());

            var layer = new ModelLayer
            {
                ModelId = model.ModelId,
                LayerIdx = layersProcessed,
                LayerName = tensorInfo.Name,
                LayerType = InferLayerType(tensorInfo.Name),
                ParameterCount = tensorInfo.ElementCount,
                WeightsGeometry = previewGeometry,
                PreviewPointCount = previewWeights?.Length,
                TensorShape = shapeJson,
                TensorDtype = MapTensorDtype(tensorInfo.Type),
                QuantizationType = tensorInfo.Type.ToString(),
                Parameters = System.Text.Json.JsonSerializer.Serialize(new
                {
                    original_type = tensorInfo.Type.ToString(),
                    dimensions = tensorInfo.Dimensions.Select(d => (long)d).ToArray(),
                    total_elements = tensorInfo.ElementCount
                })
            };

            await _modelRepository.AddLayerAsync(model.ModelId, layer, cancellationToken);
            model.Layers.Add(layer);

            if (previewGeometry != null)
            {
                layersWithWeights++;
            }

            var segment = new LayerTensorSegment
            {
                LayerId = layer.LayerId,
                SegmentOrdinal = 0,
                PointOffset = 0,
                PointCount = tensorInfo.ElementCount > int.MaxValue
                    ? int.MaxValue
                    : (int)tensorInfo.ElementCount,
                QuantizationType = tensorInfo.Type.ToString(),
                RawPayload = rawBytes,
                CreatedAt = DateTime.UtcNow
            };

            await _tensorSegmentRepository.AddAsync(segment, cancellationToken);

            layersProcessed++;

            if (layersProcessed % 50 == 0)
                _logger.LogInformation("Processed {Count}/{Total} tensors ({WithWeights} with GEOMETRY data)",
                    layersProcessed, tensorCount, layersWithWeights);
        }

        _logger.LogInformation("GGUF ingestion complete: {ModelName} ({LayerCount} layers, {WithWeights} with GEOMETRY weights)",
            model.ModelName, layersProcessed, layersWithWeights);

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

    private string MapTensorDtype(GGMLType tensorType)
    {
        return tensorType switch
        {
            GGMLType.F32 => "float32",
            GGMLType.F16 => "float16",
            GGMLType.BF16 => "bfloat16",
            _ => tensorType.ToString()
        };
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

    private float[]? DequantizeTensorPreview(byte[] rawBytes, GGUFTensorInfo tensorInfo, int previewLimit)
    {
        if (previewLimit <= 0 || rawBytes.Length == 0)
        {
            return Array.Empty<float>();
        }

        using var stream = new MemoryStream(rawBytes, writable: false);
        using var reader = new BinaryReader(stream);
        return DequantizeTensor(reader, tensorInfo, previewLimit);
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

    private enum GGMLType : uint
    {
        F32 = 0,
        F16 = 1,
        Q4_0 = 2,
        Q4_1 = 3,
        Q5_0 = 6,
        Q5_1 = 7,
        Q8_0 = 8,
        Q8_1 = 9,
        Q2_K = 10,
        Q3_K = 11,
        Q4_K = 12,
        Q5_K = 13,
        Q6_K = 14,
        Q8_K = 15,
        IQ2_XXS = 16,
        IQ2_XS = 17,
        IQ3_XXS = 18,
        IQ1_S = 19,
        IQ4_NL = 20,
        IQ3_S = 21,
        IQ2_S = 22,
        IQ4_XS = 23,
        I8 = 24,
        I16 = 25,
        I32 = 26,
        I64 = 27,
        F64 = 28,
        IQ1_M = 29,
        BF16 = 30
    }

    private class GGUFTensorInfo
    {
        public string Name { get; set; } = string.Empty;
        public ulong[] Dimensions { get; set; } = Array.Empty<ulong>();
        public GGMLType Type { get; set; }
        public ulong Offset { get; set; }
        public long ElementCount { get; set; }
        public ulong DataLengthBytes { get; set; }
    }

    /// <summary>
    /// Dequantizes a tensor from GGUF format to float32 array.
    /// </summary>
    private float[]? DequantizeTensor(BinaryReader reader, GGUFTensorInfo tensorInfo, int previewLimit)
    {
        var limit = Math.Min(previewLimit, tensorInfo.ElementCount > int.MaxValue ? int.MaxValue : (int)tensorInfo.ElementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        return tensorInfo.Type switch
        {
            GGMLType.F32 => DequantizeF32(reader, tensorInfo.ElementCount, limit),
            GGMLType.F16 => DequantizeF16(reader, tensorInfo.ElementCount, limit),
            GGMLType.BF16 => DequantizeBF16(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q4_0 => DequantizeQ4_0(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q4_1 => DequantizeQ4_1(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q5_0 => DequantizeQ5_0(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q5_1 => DequantizeQ5_1(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q8_0 => DequantizeQ8_0(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q2_K => DequantizeQ2_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q3_K => DequantizeQ3_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q4_K => DequantizeQ4_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q5_K => DequantizeQ5_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q6_K => DequantizeQ6_K(reader, tensorInfo.ElementCount, limit),
            _ => null // Unsupported type
        };
    }

    private float[] DequantizeF32(BinaryReader reader, long elementCount, int previewLimit)
    {
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        for (long i = 0; i < elementCount; i++)
        {
            var value = reader.ReadSingle();
            if (i < limit)
            {
                result[(int)i] = value;
            }
        }

        return result;
    }

    private float[] DequantizeF16(BinaryReader reader, long elementCount, int previewLimit)
    {
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        for (long i = 0; i < elementCount; i++)
        {
            var u16 = reader.ReadUInt16();
            if (i < limit)
            {
                result[(int)i] = HalfToFloat(u16);
            }
        }

        return result;
    }

    private float[] DequantizeBF16(BinaryReader reader, long elementCount, int previewLimit)
    {
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        for (long i = 0; i < elementCount; i++)
        {
            var bf16 = reader.ReadUInt16();
            var f32bits = (uint)bf16 << 16;
            if (i < limit)
            {
                result[(int)i] = BitConverter.ToSingle(BitConverter.GetBytes(f32bits), 0);
            }
        }

        return result;
    }

    private float[] DequantizeQ4_0(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q4_0: 4-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + 16 bytes of 4-bit quantized values
        var numBlocks = (int)((elementCount + QK4_0 - 1) / QK4_0);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var quants = reader.ReadBytes(16); // 32 values @ 4 bits = 16 bytes

            for (int i = 0; i < 16 && globalIndex < elementCount; i++)
            {
                byte b8 = quants[i];

                // Low 4 bits
                int qLow = (b8 & 0x0F) - 8;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qLow * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
                if (globalIndex >= elementCount)
                {
                    break;
                }

                // High 4 bits
                int qHigh = ((b8 >> 4) & 0x0F) - 8;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qHigh * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ4_1(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q4_1: 4-bit quantization with min, block size 32
        // Each block: delta (FP16) + min (FP16) + 16 bytes of 4-bit values
        var numBlocks = (int)((elementCount + QK4_1 - 1) / QK4_1);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var min = HalfToFloat(reader.ReadUInt16());
            var quants = reader.ReadBytes(16);

            for (int i = 0; i < 16 && globalIndex < elementCount; i++)
            {
                byte b8 = quants[i];

                int qLow = b8 & 0x0F;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qLow * delta + min;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
                if (globalIndex >= elementCount)
                {
                    break;
                }

                int qHigh = (b8 >> 4) & 0x0F;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qHigh * delta + min;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_0(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q5_0: 5-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + qh (4 bytes, high bits) + qs (16 bytes, low 4 bits)
        var numBlocks = (int)((elementCount + QK5_0 - 1) / QK5_0);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var qh = reader.ReadUInt32(); // High bits packed
            var qs = reader.ReadBytes(16); // Low 4 bits

            for (int i = 0; i < 32 && globalIndex < elementCount; i++)
            {
                int byteIdx = i / 2;
                int shift = (i % 2) * 4;

                // Get low 4 bits
                int lowBits = (qs[byteIdx] >> shift) & 0x0F;

                // Get high bit from qh
                int highBit = (int)((qh >> i) & 1);

                // Combine to 5-bit value
                int q = (highBit << 4) | lowBits;
                q -= 16; // Center at 0

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_1(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q5_1: 5-bit quantization with min, block size 32
        var numBlocks = (int)((elementCount + QK5_1 - 1) / QK5_1);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var min = HalfToFloat(reader.ReadUInt16());
            var qh = reader.ReadUInt32();
            var qs = reader.ReadBytes(16);

            for (int i = 0; i < 32 && globalIndex < elementCount; i++)
            {
                int byteIdx = i / 2;
                int shift = (i % 2) * 4;

                int lowBits = (qs[byteIdx] >> shift) & 0x0F;
                int highBit = (int)((qh >> i) & 1);
                int q = (highBit << 4) | lowBits;

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * delta + min;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ8_0(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q8_0: 8-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + 32 bytes of int8 values
        var numBlocks = (int)((elementCount + QK8_0 - 1) / QK8_0);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < 32 && globalIndex < elementCount; i++)
            {
                sbyte q = reader.ReadSByte();
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ2_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q2_K: 2-bit super-block quantization (256 elements per super-block)
        // Complex structure - for production use, this is a simplified version
        // Real implementation needs to match ggml's block_q2_K structure
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            // Simplified: read scales and quantized values
            // Real Q2_K structure: scales (16 bytes), qs (64 bytes)
            var scales = new byte[16];
            reader.Read(scales, 0, 16);
            var qs = new byte[64];
            reader.Read(qs, 0, 64);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 16;
                int qIdx = i / 4;
                int shift = (i % 4) * 2;

                float scale = (scales[scaleIdx] - 128) / 64.0f;
                int q = (qs[qIdx] >> shift) & 0x03; // 2 bits
                q -= 2; // Center

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ3_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q3_K: 3-bit super-block quantization
        // Simplified implementation
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            // Q3_K structure: hmask (32 bytes), qs (96 bytes), scales (12 bytes)
            var hmask = new byte[32];
            reader.Read(hmask, 0, 32);
            var qs = new byte[96];
            reader.Read(qs, 0, 96);
            var scales = new byte[12];
            reader.Read(scales, 0, 12);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 21;
                float scale = (scales[scaleIdx % 12] - 32) / 16.0f;

                // Extract 3-bit value (simplified)
                int qIdx = i / 8 * 3;
                int q = qs[qIdx % 96] & 0x07;
                q -= 4; // Center at 0

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ4_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q4_K: 4-bit super-block quantization
        // Structure: d (FP16, 2 bytes) + dmin (FP16, 2 bytes) + scales (12 bytes) + qs (128 bytes)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var d = HalfToFloat(reader.ReadUInt16());
            var dmin = HalfToFloat(reader.ReadUInt16());
            var scales = new byte[12];
            reader.Read(scales, 0, 12);
            var qs = new byte[128];
            reader.Read(qs, 0, 128);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 32;
                float scale = ((scales[scaleIdx] & 0x0F) * d) - dmin;

                int qIdx = i / 2;
                int shift = (i % 2) * 4;
                int q = (qs[qIdx] >> shift) & 0x0F;

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q5_K: 5-bit super-block quantization
        // Structure: d (FP16) + dmin (FP16) + scales (12 bytes) + qh (32 bytes) + qs (128 bytes)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var d = HalfToFloat(reader.ReadUInt16());
            var dmin = HalfToFloat(reader.ReadUInt16());
            var scales = new byte[12];
            reader.Read(scales, 0, 12);
            var qh = new byte[32];
            reader.Read(qh, 0, 32);
            var qs = new byte[128];
            reader.Read(qs, 0, 128);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 32;
                float scale = ((scales[scaleIdx] & 0x0F) * d) - dmin;

                int qIdx = i / 2;
                int shift = (i % 2) * 4;
                int lowBits = (qs[qIdx] >> shift) & 0x0F;

                int highBitIdx = i / 8;
                int highBitShift = i % 8;
                int highBit = (qh[highBitIdx] >> highBitShift) & 1;

                int q = (highBit << 4) | lowBits;

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ6_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q6_K: 6-bit super-block quantization
        // Structure: ql (128 bytes) + qh (64 bytes) + scales (16 bytes) + d (FP16)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var ql = new byte[128];
            reader.Read(ql, 0, 128);
            var qh = new byte[64];
            reader.Read(qh, 0, 64);
            var scales = new sbyte[16];
            for (int i = 0; i < 16; i++)
                scales[i] = reader.ReadSByte();
            var d = HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 16;
                float scale = scales[scaleIdx] * d;

                // Fix: Reconstruct 6-bit value correctly - ql contains 4 bits per element (packed 2 per byte)
                int qlIdx = i / 2;  // 2 values per byte
                int qlShift = (i % 2) * 4;  // 0 or 4 bit offset
                int lowBits = (ql[qlIdx] >> qlShift) & 0x0F;  // Extract 4 bits

                // qh contains 2 high bits per element (packed 4 per byte)
                int qhIdx = i / 4;  // 4 values per byte
                int qhShift = (i % 4) * 2;  // 0, 2, 4, or 6 bit offset
                int highBits = (qh[qhIdx] >> qhShift) & 0x03;  // Extract 2 bits

                int q = lowBits | (highBits << 4);  // Combine into 6-bit value
                q -= 32; // Center at 0

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Converts IEEE 754 half-precision (FP16) to single-precision float.
    /// </summary>
    private static float HalfToFloat(ushort half)
    {
        uint sign = (uint)(half >> 15) << 31;
        uint exponent = (uint)((half >> 10) & 0x1F);
        uint mantissa = (uint)(half & 0x3FF);

        if (exponent == 0)
        {
            if (mantissa == 0)
                return BitConverter.ToSingle(BitConverter.GetBytes(sign), 0);

            // Denormalized number
            while ((mantissa & 0x400) == 0)
            {
                mantissa <<= 1;
                exponent--;
            }
            exponent++;
            mantissa &= 0x3FF;
        }
        else if (exponent == 31)
        {
            // Infinity or NaN
            return BitConverter.ToSingle(BitConverter.GetBytes(sign | 0x7F800000 | (mantissa << 13)), 0);
        }

        exponent = exponent + (127 - 15);
        mantissa = mantissa << 13;

        uint result = sign | (exponent << 23) | mantissa;
        return BitConverter.ToSingle(BitConverter.GetBytes(result), 0);
    }

    /// <summary>
    /// Creates a LINESTRING geometry from float array by building WKT string manually.
    /// X = index, Y = weight value
    /// Pattern from GISParser: build coordinate string, then use WKT parser
    /// </summary>
    private NetTopologySuite.Geometries.Geometry CreateLineStringFromWKT(float[] weights, int srid)
    {
        // Build WKT: LINESTRING(0 1.5, 1 2.3, 2 -0.5, ...)
        var sb = new StringBuilder("LINESTRING(");
        
        for (int i = 0; i < weights.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(i);
            sb.Append(' ');
            sb.Append(weights[i].ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
        }
        
        sb.Append(')');
        
        // Parse WKT to create geometry (NetTopologySuite equivalent of DbGeography.FromText)
        var reader = new NetTopologySuite.IO.WKTReader();
        var geometry = reader.Read(sb.ToString());
        geometry.SRID = srid;
        
        return geometry;
    }

    /// <summary>
    /// Creates a MULTILINESTRING geometry from float array with chunking.
    /// Builds WKT string manually for each segment.
    /// </summary>
    private NetTopologySuite.Geometries.Geometry CreateMultiLineStringFromWKT(float[] weights, int srid, int pointsPerSegment)
    {
        var sb = new StringBuilder("MULTILINESTRING(");
        
        int currentIndex = 0;
        int segmentCount = 0;
        
        while (currentIndex < weights.Length)
        {
            int endIndex = Math.Min(currentIndex + pointsPerSegment, weights.Length);
            
            if (segmentCount > 0) sb.Append(", ");
            sb.Append('(');
            
            for (int i = currentIndex; i < endIndex; i++)
            {
                if (i > currentIndex) sb.Append(", ");
                sb.Append(i);
                sb.Append(' ');
                sb.Append(weights[i].ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
            }
            
            sb.Append(')');
            currentIndex = endIndex;
            segmentCount++;
        }
        
        sb.Append(')');
        
        // Parse WKT to create geometry
        var reader = new NetTopologySuite.IO.WKTReader();
        var geometry = reader.Read(sb.ToString());
        geometry.SRID = srid;
        
        return geometry;
    }
}

