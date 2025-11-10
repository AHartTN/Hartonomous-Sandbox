using SqlClrFunctions.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SqlClrFunctions.ModelReaders
{
    /// <summary>
    /// A self-contained, synchronous reader for GGUF model files,
    /// designed to run within the SQL CLR environment.
    /// </summary>
    public class ClrGgufReader : IClrModelReader
    {
        public string FormatName => "GGUF";

        public float[] ReadTensor(BinaryReader reader, string tensorName)
        {
            // 1. Read Header
            var header = ReadHeader(reader);

            // 2. Read Metadata Key-Value Store
            var metadata = ReadMetadata(reader, header.MetadataKvpCount);

            // 3. Read Tensor Info
            var tensorInfos = ReadTensorInfos(reader, header.TensorCount);

            // 4. Find the target tensor
            GGUFTensorInfo targetTensor = null;
            foreach (var info in tensorInfos)
            {
                if (info.Name == tensorName)
                {
                    targetTensor = info;
                    break;
                }
            }

            if (targetTensor == null)
            {
                return null; // Tensor not found
            }

            // 5. Calculate data offset and seek
            long alignment = 32; // Default alignment
            if (metadata.TryGetValue("general.alignment", out var alignValue))
            {
                alignment = Convert.ToInt64(alignValue);
            }
            long dataOffset = reader.BaseStream.Position;
            long remainder = dataOffset % alignment;
            if (remainder != 0)
            {
                dataOffset += (alignment - remainder);
            }

            reader.BaseStream.Seek(dataOffset + (long)targetTensor.Offset, SeekOrigin.Begin);

            // 6. Read and Dequantize tensor data
            // This is a simplified dequantizer. A full implementation would be much larger.
            // For now, we'll support F32 and F16.
            switch (targetTensor.Type)
            {
                case GGUFType.F32:
                    return ReadF32(reader, targetTensor.NumElements);
                case GGUFType.F16:
                    return ReadF16(reader, targetTensor.NumElements);
                // Other quantization types (Q4_0, Q8_0, etc.) would be handled here.
                default:
                    throw new NotSupportedException($"GGUF tensor type '{targetTensor.Type}' is not supported by this reader.");
            }
        }

        private GGUFHeader ReadHeader(BinaryReader reader)
        {
            if (reader.ReadUInt32() != 0x46554747) // "GGUF"
                throw new InvalidDataException("Not a GGUF file.");

            return new GGUFHeader
            {
                Version = reader.ReadUInt32(),
                TensorCount = reader.ReadUInt64(),
                MetadataKvpCount = reader.ReadUInt64()
            };
        }

        private Dictionary<string, object> ReadMetadata(BinaryReader reader, ulong count)
        {
            var metadata = new Dictionary<string, object>();
            for (ulong i = 0; i < count; i++)
            {
                var key = ReadGgufString(reader);
                var type = (GGUFValueType)reader.ReadUInt32();
                object value = ReadGgufValue(reader, type);
                metadata[key] = value;
            }
            return metadata;
        }

        private List<GGUFTensorInfo> ReadTensorInfos(BinaryReader reader, ulong count)
        {
            var infos = new List<GGUFTensorInfo>();
            for (ulong i = 0; i < count; i++)
            {
                var name = ReadGgufString(reader);
                var nDims = reader.ReadUInt32();
                var shape = new ulong[4];
                for (int j = 0; j < nDims; j++)
                {
                    shape[j] = reader.ReadUInt64();
                }
                var type = (GGUFType)reader.ReadUInt32();
                var offset = reader.ReadUInt64();

                infos.Add(new GGUFTensorInfo { Name = name, Shape = shape, Type = type, Offset = offset });
            }
            return infos;
        }

        private string ReadGgufString(BinaryReader reader)
        {
            var len = reader.ReadUInt64();
            var bytes = reader.ReadBytes((int)len);
            return Encoding.UTF8.GetString(bytes);
        }

        private object ReadGgufValue(BinaryReader reader, GGUFValueType type)
        {
            switch (type)
            {
                case GGUFValueType.UINT32: return reader.ReadUInt32();
                case GGUFValueType.FLOAT32: return reader.ReadSingle();
                case GGUFValueType.STRING: return ReadGgufString(reader);
                // Add other types as needed
                default:
                    // For simplicity, we'll just skip unsupported types
                    return null;
            }
        }

        private float[] ReadF32(BinaryReader reader, ulong numElements)
        {
            var result = new float[numElements];
            for (ulong i = 0; i < numElements; i++)
            {
                result[i] = reader.ReadSingle();
            }
            return result;
        }

        private float[] ReadF16(BinaryReader reader, ulong numElements)
        {
            var result = new float[numElements];
            for (ulong i = 0; i < numElements; i++)
            {
                result[i] = ConvertHalfToFloat(reader.ReadUInt16());
            }
            return result;
        }

        /// <summary>
        /// Converts a 16-bit half-precision float (ushort) to a 32-bit single-precision float.
        /// This is a manual implementation to avoid dependency on System.Half.
        /// </summary>
        private static float ConvertHalfToFloat(ushort half)
        {
            int sign = (half >> 15) & 0x0001;
            int exponent = (half >> 10) & 0x001F;
            int mantissa = half & 0x03FF;

            if (exponent == 0)
            {
                if (mantissa == 0) // Plus or minus zero
                    return sign == 0 ? 0f : -0f;
                
                // Subnormal number
                while ((mantissa & 0x0400) == 0)
                {
                    mantissa <<= 1;
                    exponent--;
                }
                exponent++;
                mantissa &= ~0x0400;
            }
            else if (exponent == 31)
            {
                if (mantissa == 0) // Infinity
                    return sign == 0 ? float.PositiveInfinity : float.NegativeInfinity;
                
                return float.NaN; // NaN
            }

            exponent = exponent + (127 - 15);
            mantissa = mantissa << 13;

            int s = (sign << 31);
            int e = (exponent << 23);
            int m = mantissa;

            int intRepresentation = s | e | m;
            
            return BitConverter.ToSingle(BitConverter.GetBytes(intRepresentation), 0);
        }

        private class GGUFHeader
        {
            public uint Version;
            public ulong TensorCount;
            public ulong MetadataKvpCount;
        }

        private class GGUFTensorInfo
        {
            public string Name;
            public ulong[] Shape;
            public GGUFType Type;
            public ulong Offset;
            public ulong NumElements => Shape[0] * Shape[1] * Shape[2] * Shape[3];
        }

        private enum GGUFValueType : uint
        {
            UINT32 = 3,
            FLOAT32 = 4,
            STRING = 9,
        }

        private enum GGUFType : uint
        {
            F32 = 0,
            F16 = 1,
            // Other types omitted for brevity
        }
    }
}
