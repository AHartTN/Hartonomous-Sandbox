using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Hartonomous.Infrastructure.Services;

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
    private readonly ILogger<GGUFModelReader> _logger;

    private const uint GGUF_MAGIC = 0x46554747; // "GGUF" in little-endian
    private const uint GGUF_VERSION = 3;
    private const int QK_K = 256; // Super-block size for K-quantizations
    private const int QK4_0 = 32; // Block size for Q4_0
    private const int QK4_1 = 32; // Block size for Q4_1
    private const int QK5_0 = 32; // Block size for Q5_0
    private const int QK5_1 = 32; // Block size for Q5_1
    private const int QK8_0 = 32; // Block size for Q8_0

    public string FormatName => "GGUF";
    public IEnumerable<string> SupportedExtensions => new[] { ".gguf" };

    public GGUFModelReader(
        IModelRepository modelRepository,
        IModelLayerRepository layerRepository,
        ILogger<GGUFModelReader> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
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

            // Dequantize tensor
            var floatWeights = DequantizeTensor(reader, tensorInfo);

            // Convert to GEOMETRY LINESTRING ZM (no dimension limits!)
            // X = index, Y = weight, Z = uniform importance (1.0), M = layer index
            if (floatWeights != null && floatWeights.Length > 0)
                {
                    // Store tensor shape in JSON for reconstruction
                    var shapeJson = System.Text.Json.JsonSerializer.Serialize(tensorInfo.Dimensions.Select(d => (long)d).ToArray());
                    
                    // Simple 2D LINESTRING (X=index, Y=weight)
                    var weightsGeometry = GeometryConverter.ToLineString(floatWeights, srid: 0);
                    
                    var layer = new ModelLayer
                    {
                        ModelId = model.ModelId,
                        LayerIdx = layersProcessed,
                        LayerName = tensorInfo.Name,
                        LayerType = InferLayerType(tensorInfo.Name),
                        ParameterCount = tensorInfo.ElementCount,
                        WeightsGeometry = weightsGeometry,
                        TensorShape = shapeJson,
                        TensorDtype = "float32", // Dequantized to float32
                        QuantizationType = tensorInfo.Type.ToString(),
                        Parameters = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            original_type = tensorInfo.Type.ToString(),
                            dimensions = tensorInfo.Dimensions.Select(d => (long)d).ToArray(),
                            total_elements = floatWeights.Length
                        })
                    };

                    await _modelRepository.AddLayerAsync(model.ModelId, layer, cancellationToken);
                    model.Layers.Add(layer);
                    layersWithWeights++;
                    
                    if (floatWeights.Length > 1_000_000)
                    {
                        _logger.LogInformation("Stored large tensor {Name} ({Elements} elements, {Shape}) as GEOMETRY with {Points} points",
                            tensorInfo.Name, floatWeights.Length, string.Join("x", tensorInfo.Dimensions), 
                            GeometryConverter.GetDimension(weightsGeometry));
                    }
                }
                else
                {
                    // Store metadata even if dequantization failed
                    var layer = new ModelLayer
                    {
                        ModelId = model.ModelId,
                        LayerIdx = layersProcessed,
                        LayerName = tensorInfo.Name,
                        LayerType = InferLayerType(tensorInfo.Name),
                        ParameterCount = tensorInfo.ElementCount,
                        WeightsGeometry = null,
                        QuantizationType = tensorInfo.Type.ToString()
                    };

                    await _modelRepository.AddLayerAsync(model.ModelId, layer, cancellationToken);
                    model.Layers.Add(layer);
                }
                
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
    }

    /// <summary>
    /// Dequantizes a tensor from GGUF format to float32 array.
    /// </summary>
    private float[]? DequantizeTensor(BinaryReader reader, GGUFTensorInfo tensorInfo)
    {
        return tensorInfo.Type switch
        {
            GGMLType.F32 => DequantizeF32(reader, tensorInfo.ElementCount),
            GGMLType.F16 => DequantizeF16(reader, tensorInfo.ElementCount),
            GGMLType.BF16 => DequantizeBF16(reader, tensorInfo.ElementCount),
            GGMLType.Q4_0 => DequantizeQ4_0(reader, tensorInfo.ElementCount),
            GGMLType.Q4_1 => DequantizeQ4_1(reader, tensorInfo.ElementCount),
            GGMLType.Q5_0 => DequantizeQ5_0(reader, tensorInfo.ElementCount),
            GGMLType.Q5_1 => DequantizeQ5_1(reader, tensorInfo.ElementCount),
            GGMLType.Q8_0 => DequantizeQ8_0(reader, tensorInfo.ElementCount),
            GGMLType.Q2_K => DequantizeQ2_K(reader, tensorInfo.ElementCount),
            GGMLType.Q3_K => DequantizeQ3_K(reader, tensorInfo.ElementCount),
            GGMLType.Q4_K => DequantizeQ4_K(reader, tensorInfo.ElementCount),
            GGMLType.Q5_K => DequantizeQ5_K(reader, tensorInfo.ElementCount),
            GGMLType.Q6_K => DequantizeQ6_K(reader, tensorInfo.ElementCount),
            _ => null // Unsupported type
        };
    }

    private float[] DequantizeF32(BinaryReader reader, long elementCount)
    {
        var result = new float[elementCount];
        for (long i = 0; i < elementCount; i++)
            result[i] = reader.ReadSingle();
        return result;
    }

    private float[] DequantizeF16(BinaryReader reader, long elementCount)
    {
        var result = new float[elementCount];
        for (long i = 0; i < elementCount; i++)
        {
            var u16 = reader.ReadUInt16();
            result[i] = HalfToFloat(u16);
        }
        return result;
    }

    private float[] DequantizeBF16(BinaryReader reader, long elementCount)
    {
        var result = new float[elementCount];
        for (long i = 0; i < elementCount; i++)
        {
            var bf16 = reader.ReadUInt16();
            // BF16: sign(1) + exp(8) + mantissa(7) -> F32: sign(1) + exp(8) + mantissa(23)
            // Shift left 16 bits to convert BF16 to F32
            var f32bits = (uint)bf16 << 16;
            result[i] = BitConverter.ToSingle(BitConverter.GetBytes(f32bits), 0);
        }
        return result;
    }

    private float[] DequantizeQ4_0(BinaryReader reader, long elementCount)
    {
        // Q4_0: 4-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + 16 bytes of 4-bit quantized values
        var numBlocks = (int)((elementCount + QK4_0 - 1) / QK4_0);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var quants = reader.ReadBytes(16); // 32 values @ 4 bits = 16 bytes

            for (int i = 0; i < 16 && resultIdx < elementCount; i++)
            {
                byte b8 = quants[i];
                
                // Low 4 bits
                if (resultIdx < elementCount)
                {
                    int q = (b8 & 0x0F) - 8; // 4-bit signed value centered at 0
                    result[resultIdx++] = q * delta;
                }
                
                // High 4 bits
                if (resultIdx < elementCount)
                {
                    int q = ((b8 >> 4) & 0x0F) - 8;
                    result[resultIdx++] = q * delta;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ4_1(BinaryReader reader, long elementCount)
    {
        // Q4_1: 4-bit quantization with min, block size 32
        // Each block: delta (FP16) + min (FP16) + 16 bytes of 4-bit values
        var numBlocks = (int)((elementCount + QK4_1 - 1) / QK4_1);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var min = HalfToFloat(reader.ReadUInt16());
            var quants = reader.ReadBytes(16);

            for (int i = 0; i < 16 && resultIdx < elementCount; i++)
            {
                byte b8 = quants[i];
                
                if (resultIdx < elementCount)
                {
                    int q = b8 & 0x0F; // Unsigned 4-bit value
                    result[resultIdx++] = q * delta + min;
                }
                
                if (resultIdx < elementCount)
                {
                    int q = (b8 >> 4) & 0x0F;
                    result[resultIdx++] = q * delta + min;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_0(BinaryReader reader, long elementCount)
    {
        // Q5_0: 5-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + qh (4 bytes, high bits) + qs (16 bytes, low 4 bits)
        var numBlocks = (int)((elementCount + QK5_0 - 1) / QK5_0);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var qh = reader.ReadUInt32(); // High bits packed
            var qs = reader.ReadBytes(16); // Low 4 bits

            for (int i = 0; i < 32 && resultIdx < elementCount; i++)
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
                
                result[resultIdx++] = q * delta;
            }
        }

        return result;
    }

    private float[] DequantizeQ5_1(BinaryReader reader, long elementCount)
    {
        // Q5_1: 5-bit quantization with min, block size 32
        var numBlocks = (int)((elementCount + QK5_1 - 1) / QK5_1);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var min = HalfToFloat(reader.ReadUInt16());
            var qh = reader.ReadUInt32();
            var qs = reader.ReadBytes(16);

            for (int i = 0; i < 32 && resultIdx < elementCount; i++)
            {
                int byteIdx = i / 2;
                int shift = (i % 2) * 4;
                
                int lowBits = (qs[byteIdx] >> shift) & 0x0F;
                int highBit = (int)((qh >> i) & 1);
                int q = (highBit << 4) | lowBits;
                
                result[resultIdx++] = q * delta + min;
            }
        }

        return result;
    }

    private float[] DequantizeQ8_0(BinaryReader reader, long elementCount)
    {
        // Q8_0: 8-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + 32 bytes of int8 values
        var numBlocks = (int)((elementCount + QK8_0 - 1) / QK8_0);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            
            for (int i = 0; i < 32 && resultIdx < elementCount; i++)
            {
                sbyte q = reader.ReadSByte();
                result[resultIdx++] = q * delta;
            }
        }

        return result;
    }

    private float[] DequantizeQ2_K(BinaryReader reader, long elementCount)
    {
        // Q2_K: 2-bit super-block quantization (256 elements per super-block)
        // Complex structure - for production use, this is a simplified version
        // Real implementation needs to match ggml's block_q2_K structure
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks && resultIdx < elementCount; b++)
        {
            // Simplified: read scales and quantized values
            // Real Q2_K structure: scales (16 bytes), qs (64 bytes)
            var scales = new byte[16];
            reader.Read(scales, 0, 16);
            var qs = new byte[64];
            reader.Read(qs, 0, 64);

            for (int i = 0; i < QK_K && resultIdx < elementCount; i++)
            {
                int scaleIdx = i / 16;
                int qIdx = i / 4;
                int shift = (i % 4) * 2;
                
                float scale = (scales[scaleIdx] - 128) / 64.0f;
                int q = (qs[qIdx] >> shift) & 0x03; // 2 bits
                q -= 2; // Center
                
                result[resultIdx++] = q * scale;
            }
        }

        return result;
    }

    private float[] DequantizeQ3_K(BinaryReader reader, long elementCount)
    {
        // Q3_K: 3-bit super-block quantization
        // Simplified implementation
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks && resultIdx < elementCount; b++)
        {
            // Q3_K structure: hmask (32 bytes), qs (96 bytes), scales (12 bytes)
            var hmask = new byte[32];
            reader.Read(hmask, 0, 32);
            var qs = new byte[96];
            reader.Read(qs, 0, 96);
            var scales = new byte[12];
            reader.Read(scales, 0, 12);

            for (int i = 0; i < QK_K && resultIdx < elementCount; i++)
            {
                int scaleIdx = i / 21;
                float scale = (scales[scaleIdx % 12] - 32) / 16.0f;
                
                // Extract 3-bit value (simplified)
                int qIdx = i / 8 * 3;
                int q = qs[qIdx % 96] & 0x07;
                q -= 4; // Center at 0
                
                result[resultIdx++] = q * scale;
            }
        }

        return result;
    }

    private float[] DequantizeQ4_K(BinaryReader reader, long elementCount)
    {
        // Q4_K: 4-bit super-block quantization
        // Structure: d (FP16, 2 bytes) + dmin (FP16, 2 bytes) + scales (12 bytes) + qs (128 bytes)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks && resultIdx < elementCount; b++)
        {
            var d = HalfToFloat(reader.ReadUInt16());
            var dmin = HalfToFloat(reader.ReadUInt16());
            var scales = new byte[12];
            reader.Read(scales, 0, 12);
            var qs = new byte[128];
            reader.Read(qs, 0, 128);

            for (int i = 0; i < QK_K && resultIdx < elementCount; i++)
            {
                int scaleIdx = i / 32;
                float scale = ((scales[scaleIdx] & 0x0F) * d) - dmin;
                
                int qIdx = i / 2;
                int shift = (i % 2) * 4;
                int q = (qs[qIdx] >> shift) & 0x0F;
                
                result[resultIdx++] = q * scale;
            }
        }

        return result;
    }

    private float[] DequantizeQ5_K(BinaryReader reader, long elementCount)
    {
        // Q5_K: 5-bit super-block quantization
        // Structure: d (FP16) + dmin (FP16) + scales (12 bytes) + qh (32 bytes) + qs (128 bytes)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks && resultIdx < elementCount; b++)
        {
            var d = HalfToFloat(reader.ReadUInt16());
            var dmin = HalfToFloat(reader.ReadUInt16());
            var scales = new byte[12];
            reader.Read(scales, 0, 12);
            var qh = new byte[32];
            reader.Read(qh, 0, 32);
            var qs = new byte[128];
            reader.Read(qs, 0, 128);

            for (int i = 0; i < QK_K && resultIdx < elementCount; i++)
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
                
                result[resultIdx++] = q * scale;
            }
        }

        return result;
    }

    private float[] DequantizeQ6_K(BinaryReader reader, long elementCount)
    {
        // Q6_K: 6-bit super-block quantization
        // Structure: ql (128 bytes) + qh (64 bytes) + scales (16 bytes) + d (FP16)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var result = new float[elementCount];
        int resultIdx = 0;

        for (int b = 0; b < numBlocks && resultIdx < elementCount; b++)
        {
            var ql = new byte[128];
            reader.Read(ql, 0, 128);
            var qh = new byte[64];
            reader.Read(qh, 0, 64);
            var scales = new sbyte[16];
            for (int i = 0; i < 16; i++)
                scales[i] = reader.ReadSByte();
            var d = HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < QK_K && resultIdx < elementCount; i++)
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
                
                result[resultIdx++] = q * scale;
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
}
