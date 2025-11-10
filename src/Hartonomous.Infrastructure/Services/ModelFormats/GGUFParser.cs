using System.Buffers.Binary;
using System.Text;
using Hartonomous.Core.Interfaces;

namespace Hartonomous.Infrastructure.Services.ModelFormats;

/// <summary>
/// Handles parsing of GGUF (GPT-Generated Unified Format) file headers, metadata, and tensor information.
/// </summary>
public class GGUFParser
{
    private const uint GGUF_MAGIC = 0x46554747; // "GGUF" in little-endian
    private const uint GGUF_VERSION = 3;

    public GGUFHeader ReadHeader(BinaryReader reader)
    {
        var magic = reader.ReadUInt32();
        if (magic != GGUF_MAGIC)
            throw new InvalidDataException($"Invalid GGUF magic: 0x{magic:X8}");

        var version = reader.ReadUInt32();
        var tensorCount = reader.ReadUInt64();
        var kvCount = reader.ReadUInt64();

        return new GGUFHeader
        {
            Version = version,
            TensorCount = tensorCount,
            MetadataCount = kvCount
        };
    }

    public Dictionary<string, object?> ReadMetadata(BinaryReader reader, ulong kvCount)
    {
        var metadata = new Dictionary<string, object?>();

        for (ulong i = 0; i < kvCount; i++)
        {
            var key = ReadGGUFString(reader);
            var valueType = (GGUFMetadataValueType)reader.ReadUInt32();
            var value = ReadGGUFValue(reader, valueType);

            metadata[key] = value;
        }

        return metadata;
    }

    public List<GGUFTensorInfo> ReadTensorInfos(BinaryReader reader, ulong tensorCount)
    {
        var tensorInfos = new List<GGUFTensorInfo>();

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

            tensorInfos.Add(new GGUFTensorInfo
            {
                Name = tensorName,
                Dimensions = dims,
                Type = (GGMLType)tensorType,
                Offset = offset,
                ElementCount = paramCount
            });
        }

        return tensorInfos;
    }

    public ulong CalculateDataStartOffset(long currentPosition, uint alignment)
    {
        var dataStartOffset = (ulong)currentPosition;
        if (dataStartOffset % alignment != 0)
            dataStartOffset += alignment - (dataStartOffset % alignment);
        return dataStartOffset;
    }

    public void UpdateTensorDataLengths(List<GGUFTensorInfo> tensorInfos, ulong totalDataSectionLength)
    {
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

    public string GetFileTypeName(uint fileType)
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

    public async Task<GGUFMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
    {
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

        // Read metadata KV pairs
        var metadataDict = ReadMetadata(reader, kvCount);

        foreach (var kvp in metadataDict)
        {
            metadata.MetadataKV[kvp.Key] = kvp.Value;

            // Extract important fields
            if (kvp.Key == "general.architecture")
                metadata.Architecture = kvp.Value?.ToString() ?? string.Empty;
            else if (kvp.Key == "general.file_type")
            {
                var fileType = Convert.ToUInt32(kvp.Value);
                metadata.FileType = GetFileTypeName(fileType);
                metadata.QuantizationType = metadata.FileType;
            }
            else if (kvp.Key.EndsWith(".context_length"))
                metadata.ContextLength = Convert.ToInt32(kvp.Value);
            else if (kvp.Key.EndsWith(".embedding_length") || kvp.Key.EndsWith(".n_embd"))
                metadata.EmbeddingLength = Convert.ToInt32(kvp.Value);
            else if (kvp.Key.EndsWith(".attention.head_count") || kvp.Key.EndsWith(".n_head"))
                metadata.AttentionHeadCount = Convert.ToInt32(kvp.Value);
            else if (kvp.Key.EndsWith(".block_count") || kvp.Key.EndsWith(".n_layer"))
                metadata.LayerCount = Convert.ToInt32(kvp.Value);
        }

        // Read tensor info to calculate parameters
        var tensorInfos = ReadTensorInfos(reader, tensorCount);
        metadata.ParameterCount = tensorInfos.Sum(t => t.ElementCount);

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
}

public class GGUFHeader
{
    public uint Version { get; set; }
    public ulong TensorCount { get; set; }
    public ulong MetadataCount { get; set; }
}

public enum GGUFMetadataValueType : uint
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

public enum GGMLType : uint
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

public class GGUFTensorInfo
{
    public string Name { get; set; } = string.Empty;
    public ulong[] Dimensions { get; set; } = Array.Empty<ulong>();
    public GGMLType Type { get; set; }
    public ulong Offset { get; set; }
    public long ElementCount { get; set; }
    public ulong DataLengthBytes { get; set; }
}
