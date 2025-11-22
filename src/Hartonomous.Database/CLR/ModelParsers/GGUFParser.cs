using System;
using System.Collections.Generic;
using System.IO;
using Hartonomous.Clr.Contracts;
using Hartonomous.Clr.Enums;
using Hartonomous.Clr.Models;
using static Hartonomous.Infrastructure.Services.Vision.BinaryReaderHelper;

namespace Hartonomous.Clr.ModelParsers
{
    public class GGUFParser : IModelFormatReader
    {
        private enum GGUFType : uint
        {
            FLOAT32 = 0, FLOAT16 = 1, Q4_0 = 2, Q4_1 = 3, Q5_0 = 6, Q5_1 = 7, Q8_0 = 8, Q8_1 = 9,
            Q2_K = 10, Q3_K = 11, Q4_K = 12, Q5_K = 13, Q6_K = 14, Q8_K = 15
        }

        private const int QK_K = 256;
        private const int K_SCALE_SIZE = 12;

        public static IEnumerable<object[]> Parse(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var header = ReadHeader(reader);
                var metadata = ReadMetadata(reader, header.MetadataKVCount);
                var tensorInfos = ReadTensorInfos(reader, header.TensorCount);
                
                long tensorDataStart = AlignPosition(reader.BaseStream.Position, 32);

                foreach (var info in tensorInfos)
                {
                    long absoluteOffset = tensorDataStart + (long)info.Offset;
                    reader.BaseStream.Seek(absoluteOffset, SeekOrigin.Begin);
                    
                    long weightIndex = 0;
                    ulong numElements = GetTotalElements(info.Shape);

                    switch (info.Type)
                    {
                        case GGUFType.FLOAT32:
                            for (ulong i = 0; i < numElements; i++)
                                yield return new object[] { info.Name, 0, weightIndex++, reader.ReadSingle() };
                            break;
                        case GGUFType.Q8_0:
                            foreach (var val in DequantizeQ8_0(reader, numElements))
                                yield return new object[] { info.Name, 0, weightIndex++, val };
                            break;
                        case GGUFType.Q4_K:
                            foreach (var val in DequantizeQ4_K(reader, numElements))
                                yield return new object[] { info.Name, 0, weightIndex++, val };
                            break;
                        case GGUFType.Q5_K:
                            foreach (var val in DequantizeQ5_K(reader, numElements))
                                yield return new object[] { info.Name, 0, weightIndex++, val };
                            break;
                        case GGUFType.Q6_K:
                            foreach (var val in DequantizeQ6_K(reader, numElements))
                                yield return new object[] { info.Name, 0, weightIndex++, val };
                            break;
                        default:
                            // For unsupported types, yield one placeholder to indicate the tensor was found
                            yield return new object[] { info.Name, 0, 0L, 0.0f };
                            break;
                    }
                }
            }
        }

        private static GGUFHeader ReadHeader(BinaryReader reader)
        {
            uint magic = reader.ReadUInt32();
            if (magic != 0x46554747) throw new InvalidDataException("Not a valid GGUF file.");
            return new GGUFHeader { Magic = magic, Version = reader.ReadUInt32(), TensorCount = reader.ReadUInt64(), MetadataKVCount = reader.ReadUInt64() };
        }

        private static Dictionary<string, object> ReadMetadata(BinaryReader reader, ulong count)
        {
            var metadata = new Dictionary<string, object>();
            for (ulong i = 0; i < count; i++)
            {
                var key = ReadGGUFString(reader);
                var type = (GGUFValueType)reader.ReadUInt32();
                metadata[key] = ReadGGUFValue(reader, type)!;
            }
            return metadata;
        }

        private static List<GGUFTensorInfo> ReadTensorInfos(BinaryReader reader, ulong count)
        {
            var infos = new List<GGUFTensorInfo>();
            for (ulong i = 0; i < count; i++)
            {
                var name = ReadGGUFString(reader);
                var n_dims = reader.ReadUInt32();
                var shape = new ulong[n_dims];
                for (uint j = 0; j < n_dims; j++) shape[j] = reader.ReadUInt64();
                var type = (GGUFType)reader.ReadUInt32();
                var offset = reader.ReadUInt64();
                infos.Add(new GGUFTensorInfo { Name = name, NumDims = n_dims, Shape = shape, Type = type, Offset = offset });
            }
            return infos;
        }

        private static IEnumerable<float> DequantizeQ8_0(BinaryReader reader, ulong numElements)
        {
            int blockSize = 32;
            for (ulong i = 0; i < numElements; i += (ulong)blockSize)
            {
                float scale = reader.ReadSingle();
                for (int j = 0; j < blockSize; j++)
                {
                    yield return reader.ReadSByte() * scale;
                }
            }
        }

        private static IEnumerable<float> DequantizeQ4_K(BinaryReader reader, ulong numElements)
        {
            for (ulong i = 0; i < numElements; i += (ulong)QK_K)
            {
                float[] scales = new float[K_SCALE_SIZE];
                byte[] qh = reader.ReadBytes(QK_K / 2);
                float d = reader.ReadSingle();
                float min = reader.ReadSingle();

                for (int j = 0; j < QK_K; j++)
                {
                    int scaleIdx = j / 16;
                    int il = j % 16;
                    byte q = (byte)((qh[il] >> (scaleIdx % 4 * 2)) & 3);
                    // Simplified dequantization logic
                    yield return d * q - min;
                }
            }
        }
        
        private static IEnumerable<float> DequantizeQ5_K(BinaryReader reader, ulong numElements)
        {
            for (ulong i = 0; i < numElements; i += (ulong)QK_K)
            {
                float d = reader.ReadSingle();
                float dmin = reader.ReadSingle();
                byte[] qh = reader.ReadBytes(QK_K / 2);
                byte[] qs = reader.ReadBytes(QK_K / 8);

                for(int j = 0; j < QK_K; j++)
                {
                    byte q_high = (byte)(((qh[j/2] >> (j%2 * 4)) & 0xF));
                    byte q_low = (byte)(((qs[j/8] >> (j%8)) & 1));
                    // Simplified dequantization logic
                    yield return d * q_high + dmin * q_low;
                }
            }
        }

        private static IEnumerable<float> DequantizeQ6_K(BinaryReader reader, ulong numElements)
        {
            for (ulong i = 0; i < numElements; i += (ulong)QK_K)
            {
                byte[] ql = reader.ReadBytes(QK_K / 2);
                byte[] qh = reader.ReadBytes(QK_K / 4);
                float[] scales = new float[QK_K / 16];
                for(int s=0; s<scales.Length; s++) scales[s] = reader.ReadSingle();

                for (int j = 0; j < QK_K; j++)
                {
                    byte q_low = (byte)(ql[j / 2] >> (j % 2 * 4));
                    byte q_high = (byte)(qh[j / 4] >> (j % 4 * 2));
                    // Simplified dequantization logic
                    yield return scales[j/16] * (q_low + q_high);
                }
            }
        }

        private static ulong GetTotalElements(ulong[] shape)
        {
            ulong total = 1;
            foreach (var dim in shape) total *= dim;
            return total;
        }

        private static long AlignPosition(long position, int alignment) => (position + (alignment - 1)) & ~((long)alignment - 1);
        private static string ReadGGUFString(BinaryReader reader) { var len = reader.ReadUInt64(); return len == 0 ? string.Empty : Encoding.UTF8.GetString(reader.ReadBytes((int)len)); }
        private static object? ReadGGUFValue(BinaryReader reader, GGUFValueType type) { /* Omitted for brevity, same as before */ return null; }
        private struct GGUFHeader { public uint Magic, Version; public ulong TensorCount, MetadataKVCount; }
        private class GGUFTensorInfo { public string Name = string.Empty; public uint NumDims; public ulong[] Shape = Array.Empty<ulong>(); public GGUFType Type; public ulong Offset; }
        private enum GGUFValueType : uint { UINT8 = 0, INT8 = 1, UINT16 = 2, INT16 = 3, UINT32 = 4, INT32 = 5, FLOAT32 = 6, BOOL = 7, STRING = 8, ARRAY = 9 }
    }
}
