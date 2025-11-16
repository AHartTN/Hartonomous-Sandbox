using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace Hartonomous.Clr
{
    /// <summary>
    /// CLR functions for model ingestion pipeline.
    /// Handles GGUF parsing, FILESTREAM reading, and tensor-to-GEOMETRY conversion.
    /// </summary>
    public static class ModelIngestionFunctions
    {
        /// <summary>
        /// Parse GGUF model file header and return tensor catalog.
        /// Returns: Table with TensorName, DataType, Shape, ShapeRank, ElementCount, ByteOffset, ByteSize
        /// </summary>
        [SqlFunction(
            FillRowMethodName = "FillGGUFTensorRow",
            TableDefinition = "TensorName NVARCHAR(500), DataType NVARCHAR(50), Shape NVARCHAR(500), ShapeRank INT, ElementCount BIGINT, ByteOffset BIGINT, ByteSize BIGINT",
            DataAccess = DataAccessKind.Read)]
        public static System.Collections.IEnumerable ParseGGUFTensorCatalog(SqlGuid payloadId)
        {
            if (payloadId.IsNull)
                yield break;

            // Get FILESTREAM path from database
            string filestreamPath = null;
            using (var connection = new SqlConnection("context connection=true"))
            {
                connection.Open();
                using (var cmd = new SqlCommand(
                    "SELECT PayloadData.PathName() FROM dbo.AtomPayloadStore WHERE PayloadId = @PayloadId",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@PayloadId", payloadId.Value);
                    filestreamPath = cmd.ExecuteScalar() as string;
                }
            }

            if (string.IsNullOrEmpty(filestreamPath))
                yield break;

            // Parse GGUF file
            using (var fileStream = new FileStream(filestreamPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fileStream, Encoding.UTF8))
            {
                // Read GGUF magic and version
                uint magic = reader.ReadUInt32();
                if (magic != 0x46554747) // "GGUF" in little-endian
                    yield break;

                uint version = reader.ReadUInt32();
                ulong tensorCount = reader.ReadUInt64();
                ulong metadataCount = reader.ReadUInt64();

                // Skip metadata section (we only need tensor info)
                for (ulong i = 0; i < metadataCount; i++)
                {
                    SkipMetadataEntry(reader);
                }

                // Read tensor information
                long dataOffset = 0;
                for (ulong i = 0; i < tensorCount; i++)
                {
                    var tensorInfo = ReadTensorInfo(reader, version);
                    
                    // Calculate byte offset (tensors are aligned to 32-byte boundaries)
                    if (dataOffset == 0)
                    {
                        dataOffset = AlignOffset(fileStream.Position, 32);
                    }
                    
                    tensorInfo.ByteOffset = dataOffset;
                    dataOffset += tensorInfo.ByteSize;
                    
                    yield return tensorInfo;
                }
            }
        }

        /// <summary>
        /// Read a chunk of data from FILESTREAM at the specified offset.
        /// Used to extract individual tensor weights from the model file.
        /// </summary>
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static SqlBytes ReadFilestreamChunk(SqlGuid payloadId, SqlInt64 offset, SqlInt64 size)
        {
            if (payloadId.IsNull || offset.IsNull || size.IsNull)
                return SqlBytes.Null;

            // Get FILESTREAM path
            string filestreamPath = null;
            using (var connection = new SqlConnection("context connection=true"))
            {
                connection.Open();
                using (var cmd = new SqlCommand(
                    "SELECT PayloadData.PathName() FROM dbo.AtomPayloadStore WHERE PayloadId = @PayloadId",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@PayloadId", payloadId.Value);
                    filestreamPath = cmd.ExecuteScalar() as string;
                }
            }

            if (string.IsNullOrEmpty(filestreamPath))
                return SqlBytes.Null;

            // Read the chunk
            using (var fileStream = new FileStream(filestreamPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Seek(offset.Value, SeekOrigin.Begin);
                
                int bytesToRead = (int)Math.Min(size.Value, int.MaxValue);
                byte[] buffer = new byte[bytesToRead];
                
                int totalRead = 0;
                while (totalRead < bytesToRead)
                {
                    int read = fileStream.Read(buffer, totalRead, bytesToRead - totalRead);
                    if (read == 0)
                        break;
                    totalRead += read;
                }
                
                if (totalRead < bytesToRead)
                {
                    Array.Resize(ref buffer, totalRead);
                }
                
                return new SqlBytes(buffer);
            }
        }

        /// <summary>
        /// Convert raw tensor weights to GEOMETRY LINESTRING representation.
        /// Each weight becomes a point (index, value) in the line string.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlGeometry CreateMultiLineStringFromWeights(
            SqlBytes rawWeights,
            SqlString dataType,
            SqlInt32 maxPoints)
        {
            if (rawWeights.IsNull || dataType.IsNull)
                return SqlGeometry.Null;

            int pointLimit = maxPoints.IsNull || maxPoints.Value <= 0 ? 4096 : maxPoints.Value;
            string dtype = dataType.Value.ToLowerInvariant();

            var buffer = SqlBytesInterop.GetBuffer(rawWeights, out var byteLength);
            
            // Determine element size based on data type
            int elementSize = GetElementSize(dtype);
            if (elementSize == 0)
                return SqlGeometry.Null;

            long elementCount = byteLength / elementSize;
            if (elementCount < 2)
                return SqlGeometry.Null;

            // Calculate stride for downsampling if needed
            long stride = Math.Max(1, elementCount / pointLimit);
            int actualPoints = (int)Math.Min(pointLimit, elementCount);

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.LineString);

            bool firstPoint = true;
            for (long i = 0; i < elementCount; i += stride)
            {
                double value = ReadElement(buffer, (int)i, elementSize, dtype);
                
                if (firstPoint)
                {
                    builder.BeginFigure(i, value);
                    firstPoint = false;
                }
                else
                {
                    builder.AddLine(i, value);
                }
            }

            // Ensure the last element is always included
            if ((elementCount - 1) % stride != 0)
            {
                double lastValue = ReadElement(buffer, (int)(elementCount - 1), elementSize, dtype);
                builder.AddLine(elementCount - 1, lastValue);
            }

            builder.EndFigure();
            builder.EndGeometry();

            return builder.ConstructedGeometry;
        }

        #region Helper Methods

        private static void FillGGUFTensorRow(
            object obj,
            out SqlString tensorName,
            out SqlString dataType,
            out SqlString shape,
            out SqlInt32 shapeRank,
            out SqlInt64 elementCount,
            out SqlInt64 byteOffset,
            out SqlInt64 byteSize)
        {
            var info = (GGUFTensorInfo)obj;
            tensorName = new SqlString(info.TensorName);
            dataType = new SqlString(info.DataType);
            shape = new SqlString(info.Shape);
            shapeRank = new SqlInt32(info.ShapeRank);
            elementCount = new SqlInt64(info.ElementCount);
            byteOffset = new SqlInt64(info.ByteOffset);
            byteSize = new SqlInt64(info.ByteSize);
        }

        private static GGUFTensorInfo ReadTensorInfo(BinaryReader reader, uint version)
        {
            var info = new GGUFTensorInfo();
            
            // Read tensor name
            ulong nameLength = reader.ReadUInt64();
            info.TensorName = Encoding.UTF8.GetString(reader.ReadBytes((int)nameLength));
            
            // Read shape rank
            uint rank = reader.ReadUInt32();
            info.ShapeRank = (int)rank;
            
            // Read dimensions
            long[] dimensions = new long[rank];
            long elementCount = 1;
            for (uint i = 0; i < rank; i++)
            {
                dimensions[i] = (long)reader.ReadUInt64();
                elementCount *= dimensions[i];
            }
            info.ElementCount = elementCount;
            info.Shape = string.Join(",", dimensions);
            
            // Read data type
            uint ggmlType = reader.ReadUInt32();
            info.DataType = MapGGMLType(ggmlType);
            
            // Read offset (relative to data section start)
            ulong offset = reader.ReadUInt64();
            
            // Calculate byte size based on GGML type
            info.ByteSize = CalculateTensorByteSize(elementCount, ggmlType);
            
            return info;
        }

        private static void SkipMetadataEntry(BinaryReader reader)
        {
            // Skip key
            ulong keyLength = reader.ReadUInt64();
            reader.ReadBytes((int)keyLength);
            
            // Read value type and skip value
            uint valueType = reader.ReadUInt32();
            SkipMetadataValue(reader, valueType);
        }

        private static void SkipMetadataValue(BinaryReader reader, uint valueType)
        {
            switch (valueType)
            {
                case 0: // UINT8
                    reader.ReadByte();
                    break;
                case 1: // INT8
                    reader.ReadSByte();
                    break;
                case 2: // UINT16
                    reader.ReadUInt16();
                    break;
                case 3: // INT16
                    reader.ReadInt16();
                    break;
                case 4: // UINT32
                    reader.ReadUInt32();
                    break;
                case 5: // INT32
                    reader.ReadInt32();
                    break;
                case 6: // FLOAT32
                    reader.ReadSingle();
                    break;
                case 7: // BOOL
                    reader.ReadBoolean();
                    break;
                case 8: // STRING
                    ulong strLen = reader.ReadUInt64();
                    reader.ReadBytes((int)strLen);
                    break;
                case 9: // ARRAY
                    uint arrayType = reader.ReadUInt32();
                    ulong arrayLen = reader.ReadUInt64();
                    for (ulong i = 0; i < arrayLen; i++)
                    {
                        SkipMetadataValue(reader, arrayType);
                    }
                    break;
                case 10: // UINT64
                    reader.ReadUInt64();
                    break;
                case 11: // INT64
                    reader.ReadInt64();
                    break;
                case 12: // FLOAT64
                    reader.ReadDouble();
                    break;
            }
        }

        private static string MapGGMLType(uint ggmlType)
        {
            return ggmlType switch
            {
                0 => "F32",
                1 => "F16",
                2 => "Q4_0",
                3 => "Q4_1",
                6 => "Q5_0",
                7 => "Q5_1",
                8 => "Q8_0",
                10 => "Q2_K",
                11 => "Q3_K",
                12 => "Q4_K",
                13 => "Q5_K",
                14 => "Q6_K",
                30 => "BF16",
                _ => $"Unknown_{ggmlType}"
            };
        }

        private static long CalculateTensorByteSize(long elementCount, uint ggmlType)
        {
            return ggmlType switch
            {
                0 => elementCount * 4, // F32
                1 => elementCount * 2, // F16
                2 => (elementCount / 32) * 18, // Q4_0: 32 elements per block, 18 bytes per block
                3 => (elementCount / 32) * 20, // Q4_1
                6 => (elementCount / 32) * 22, // Q5_0
                7 => (elementCount / 32) * 24, // Q5_1
                8 => (elementCount / 32) * 34, // Q8_0
                30 => elementCount * 2, // BF16
                _ => elementCount * 4 // Default to F32 size
            };
        }

        private static long AlignOffset(long offset, int alignment)
        {
            long remainder = offset % alignment;
            return remainder == 0 ? offset : offset + (alignment - remainder);
        }

        private static int GetElementSize(string dataType)
        {
            return dataType switch
            {
                "f32" => 4,
                "f16" => 2,
                "bf16" => 2,
                "int8" => 1,
                "uint8" => 1,
                "int16" => 2,
                "uint16" => 2,
                "int32" => 4,
                "uint32" => 4,
                _ => 4 // Default to float32
            };
        }

        private static double ReadElement(byte[] buffer, int index, int elementSize, string dataType)
        {
            int offset = index * elementSize;
            if (offset + elementSize > buffer.Length)
                return 0;

            return dataType switch
            {
                "f32" => BitConverter.ToSingle(buffer, offset),
                "f16" => ConvertFloat16ToFloat32(buffer, offset),
                "bf16" => ConvertBFloat16ToFloat32(buffer, offset),
                "int8" => (sbyte)buffer[offset],
                "uint8" => buffer[offset],
                "int16" => BitConverter.ToInt16(buffer, offset),
                "uint16" => BitConverter.ToUInt16(buffer, offset),
                "int32" => BitConverter.ToInt32(buffer, offset),
                "uint32" => BitConverter.ToUInt32(buffer, offset),
                _ => BitConverter.ToSingle(buffer, offset)
            };
        }

        private static float ConvertFloat16ToFloat32(byte[] buffer, int offset)
        {
            ushort half = BitConverter.ToUInt16(buffer, offset);
            
            uint sign = (uint)((half >> 15) & 0x1);
            uint exponent = (uint)((half >> 10) & 0x1F);
            uint mantissa = (uint)(half & 0x3FF);
            
            if (exponent == 0)
            {
                if (mantissa == 0)
                    return sign == 1 ? -0.0f : 0.0f;
                
                // Denormalized number
                exponent = 1;
            }
            else if (exponent == 31)
            {
                // Infinity or NaN
                return mantissa == 0 
                    ? (sign == 1 ? float.NegativeInfinity : float.PositiveInfinity)
                    : float.NaN;
            }
            
            // Normalized number
            uint f32 = (sign << 31) | ((exponent + 112) << 23) | (mantissa << 13);
            return BitConverter.ToSingle(BitConverter.GetBytes(f32), 0);
        }

        private static float ConvertBFloat16ToFloat32(byte[] buffer, int offset)
        {
            ushort bf16 = BitConverter.ToUInt16(buffer, offset);
            uint f32Bits = (uint)bf16 << 16;
            return BitConverter.ToSingle(BitConverter.GetBytes(f32Bits), 0);
        }

        private class GGUFTensorInfo
        {
            public string TensorName { get; set; }
            public string DataType { get; set; }
            public string Shape { get; set; }
            public int ShapeRank { get; set; }
            public long ElementCount { get; set; }
            public long ByteOffset { get; set; }
            public long ByteSize { get; set; }
        }

        #endregion
    }
}
