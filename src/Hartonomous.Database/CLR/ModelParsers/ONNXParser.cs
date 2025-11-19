using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hartonomous.Clr.Contracts;
using Hartonomous.Clr.Enums;
using Hartonomous.Clr.Models;

namespace Hartonomous.Clr.ModelParsers
{
    /// <summary>
    /// Parses ONNX (Open Neural Network Exchange) format models.
    /// Uses lightweight protobuf parsing to extract tensor metadata and weights.
    /// </summary>
    public class ONNXParser : IModelFormatReader
    {
        public ModelFormat Format => ModelFormat.ONNX;

        public bool ValidateFormat(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                return false;

            long originalPosition = stream.Position;
            try
            {
                stream.Position = 0;
                
                // Read first byte for protobuf tag
                int firstByte = stream.ReadByte();
                
                // ONNX starts with field 1 (ir_version): tag 0x08
                return firstByte == 0x08;
            }
            catch
            {
                return false;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        public ModelMetadata ReadMetadata(Stream stream)
        {
            if (!ValidateFormat(stream))
                throw new ArgumentException("Invalid ONNX format", nameof(stream));

            stream.Position = 0;

            var metadata = new ModelMetadata
            {
                Format = ModelFormat.ONNX,
                Name = "ONNX Model",
                Architecture = "Unknown",
                LayerCount = 0,
                EmbeddingDimension = 0,
                ParameterCount = 0
            };

            try
            {
                // Lightweight protobuf parsing without ONNX Runtime dependency
                // This is simplified - full implementation would use Google.Protobuf
                // or hand-coded parser for ModelProto, GraphProto, TensorProto

                var tensorInfos = new List<TensorInfo>();
                long parameterCount = 0;

                // Parse initializers (weights) from GraphProto
                // Field 7 (graph) is length-delimited: tag 0x3A
                while (stream.Position < stream.Length)
                {
                    int tag = ReadVarint32(stream);
                    int fieldNumber = tag >> 3;
                    int wireType = tag & 0x07;

                    if (fieldNumber == 7 && wireType == 2) // graph field
                    {
                        int graphLength = ReadVarint32(stream);
                        long graphEnd = stream.Position + graphLength;

                        // Inside GraphProto, look for field 5 (initializer)
                        while (stream.Position < graphEnd)
                        {
                            int graphTag = ReadVarint32(stream);
                            int graphField = graphTag >> 3;
                            int graphWire = graphTag & 0x07;

                            if (graphField == 5 && graphWire == 2) // initializer
                            {
                                var tensorInfo = ParseTensorProto(stream);
                                if (tensorInfo.ElementCount > 0)
                                {
                                    tensorInfos.Add(tensorInfo);
                                    parameterCount += tensorInfo.ElementCount;
                                }
                            }
                            else if (graphWire == 2) // length-delimited
                            {
                                int length = ReadVarint32(stream);
                                stream.Position += length; // Skip
                            }
                            else if (graphWire == 0) // varint
                            {
                                ReadVarint64(stream);
                            }
                        }

                        break;
                    }
                    else if (wireType == 2) // length-delimited
                    {
                        int length = ReadVarint32(stream);
                        stream.Position += length; // Skip
                    }
                    else if (wireType == 0) // varint
                    {
                        ReadVarint64(stream);
                    }
                }

                metadata.LayerCount = tensorInfos.Count;
                metadata.ParameterCount = parameterCount;

                // Infer embedding dimension from embedding layer if present
                var embeddingLayer = tensorInfos.FirstOrDefault(t => 
                    t.LayerType == LayerType.Embedding || 
                    t.Name.Contains("embed", StringComparison.OrdinalIgnoreCase));
                
                if (embeddingLayer.Shape != null && embeddingLayer.Shape.Length > 0)
                {
                    metadata.EmbeddingDimension = (int)embeddingLayer.Shape[embeddingLayer.Shape.Length - 1];
                }
            }
            catch (Exception ex)
            {
                // Return partial metadata on parse error
                metadata.Name = $"ONNX Model (parse error: {ex.Message})";
            }

            return metadata;
        }

        public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
        {
            if (!ValidateFormat(stream))
                throw new ArgumentException("Invalid ONNX format", nameof(stream));

            stream.Position = 0;
            var weights = new Dictionary<string, TensorInfo>();

            try
            {
                // Parse GraphProto for initializers
                while (stream.Position < stream.Length)
                {
                    int tag = ReadVarint32(stream);
                    int fieldNumber = tag >> 3;
                    int wireType = tag & 0x07;

                    if (fieldNumber == 7 && wireType == 2) // graph field
                    {
                        int graphLength = ReadVarint32(stream);
                        long graphEnd = stream.Position + graphLength;

                        while (stream.Position < graphEnd)
                        {
                            int graphTag = ReadVarint32(stream);
                            int graphField = graphTag >> 3;
                            int graphWire = graphTag & 0x07;

                            if (graphField == 5 && graphWire == 2) // initializer
                            {
                                var tensorInfo = ParseTensorProto(stream);
                                if (!string.IsNullOrEmpty(tensorInfo.Name))
                                {
                                    weights[tensorInfo.Name] = tensorInfo;
                                }
                            }
                            else if (graphWire == 2)
                            {
                                int length = ReadVarint32(stream);
                                stream.Position += length;
                            }
                            else if (graphWire == 0)
                            {
                                ReadVarint64(stream);
                            }
                        }

                        break;
                    }
                    else if (wireType == 2)
                    {
                        int length = ReadVarint32(stream);
                        stream.Position += length;
                    }
                    else if (wireType == 0)
                    {
                        ReadVarint64(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read ONNX weights: {ex.Message}", ex);
            }

            return weights;
        }

        #region Lightweight Protobuf Parsing

        private TensorInfo ParseTensorProto(Stream stream)
        {
            int tensorLength = ReadVarint32(stream);
            long tensorEnd = stream.Position + tensorLength;

            string name = string.Empty;
            TensorDtype dtype = TensorDtype.F32;
            int[] shape = Array.Empty<int>();
            long dataOffset = 0;
            int dataSize = 0;

            while (stream.Position < tensorEnd)
            {
                int tag = ReadVarint32(stream);
                int fieldNumber = tag >> 3;
                int wireType = tag & 0x07;

                switch (fieldNumber)
                {
                    case 1: // name (string)
                        if (wireType == 2)
                        {
                            int nameLength = ReadVarint32(stream);
                            byte[] nameBytes = new byte[nameLength];
                            stream.Read(nameBytes, 0, nameLength);
                            name = System.Text.Encoding.UTF8.GetString(nameBytes);
                        }
                        break;

                    case 2: // data_type (int32)
                        if (wireType == 0)
                        {
                            int onnxDataType = ReadVarint32(stream);
                            dtype = MapOnnxDataType(onnxDataType);
                        }
                        break;

                    case 7: // dims (repeated int64)
                        if (wireType == 2) // packed
                        {
                            int dimsLength = ReadVarint32(stream);
                            long dimsEnd = stream.Position + dimsLength;
                            var dimsList = new List<int>();
                            
                            while (stream.Position < dimsEnd)
                            {
                                dimsList.Add((int)ReadVarint64(stream));
                            }
                            
                            shape = dimsList.ToArray();
                        }
                        break;

                    case 9: // raw_data (bytes)
                        if (wireType == 2)
                        {
                            dataSize = ReadVarint32(stream);
                            dataOffset = stream.Position;
                            stream.Position += dataSize; // Skip data
                        }
                        break;

                    default:
                        // Skip unknown fields
                        if (wireType == 2)
                        {
                            int length = ReadVarint32(stream);
                            stream.Position += length;
                        }
                        else if (wireType == 0)
                        {
                            ReadVarint64(stream);
                        }
                        break;
                }
            }

            // Calculate element count
            long elementCount = 1;
            foreach (int dim in shape)
                elementCount *= dim;

            // Convert int[] shape to long[] for TensorInfo
            long[] shapeInt64 = new long[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                shapeInt64[i] = shape[i];

            return new TensorInfo
            {
                Name = name,
                Dtype = dtype,
                Quantization = QuantizationType.None,
                Shape = shapeInt64,
                ElementCount = elementCount,
                DataOffset = dataOffset,
                DataSize = dataSize,
                LayerIndex = TensorInfo.ExtractLayerIndex(name),
                LayerType = TensorInfo.InferLayerType(name)
            };
        }

        private TensorDtype MapOnnxDataType(int onnxType)
        {
            // ONNX TensorProto.DataType enum values
            return onnxType switch
            {
                1 => TensorDtype.F32,   // FLOAT
                10 => TensorDtype.F16,  // FLOAT16
                16 => TensorDtype.BF16, // BFLOAT16
                2 => TensorDtype.U8,    // UINT8
                3 => TensorDtype.I8,    // INT8
                6 => TensorDtype.I32,   // INT32
                7 => TensorDtype.I64,   // INT64
                9 => TensorDtype.Bool,  // BOOL
                _ => TensorDtype.F32    // Default to F32
            };
        }

        private int ReadVarint32(Stream stream)
        {
            int result = 0;
            int shift = 0;
            
            while (true)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                
                result |= (b & 0x7F) << shift;
                
                if ((b & 0x80) == 0)
                    return result;
                
                shift += 7;
            }
        }

        private long ReadVarint64(Stream stream)
        {
            long result = 0;
            int shift = 0;
            
            while (true)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                
                result |= (long)(b & 0x7F) << shift;
                
                if ((b & 0x80) == 0)
                    return result;
                
                shift += 7;
            }
        }

        #endregion
    }
}
