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
    /// Parses TensorFlow SavedModel format.
    /// Handles protobuf .pb files containing GraphDef with variable metadata.
    /// </summary>
    public class TensorFlowParser : IModelFormatReader
    {
        public ModelFormat Format => ModelFormat.TensorFlow;

        public bool ValidateFormat(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                return false;

            long originalPosition = stream.Position;
            try
            {
                stream.Position = 0;
                
                // TensorFlow SavedModel uses protobuf (.pb files)
                // Check for protobuf magic and SavedModel structure
                
                byte[] header = new byte[32];
                int read = stream.Read(header, 0, 32);
                
                if (read < 4)
                    return false;

                // Look for common TensorFlow field tags
                // SavedModel has specific structure with MetaGraphDef
                for (int i = 0; i < read - 1; i++)
                {
                    // Field 1 (saved_model_schema_version): tag 0x08
                    // Field 2 (meta_graphs): tag 0x12
                    if (header[i] == 0x08 || header[i] == 0x12)
                        return true;
                }

                return false;
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
                throw new ArgumentException("Invalid TensorFlow SavedModel format", nameof(stream));

            stream.Position = 0;

            var metadata = new ModelMetadata
            {
                Format = ModelFormat.TensorFlow,
                Name = "TensorFlow SavedModel",
                Architecture = "Unknown",
                LayerCount = 0,
                EmbeddingDimension = 0,
                ParameterCount = 0
            };

            try
            {
                // Parse SavedModel protobuf structure
                // This is simplified - full implementation would use TensorFlow protobuf definitions
                
                var tensorInfos = new List<TensorInfo>();

                // Look for MetaGraphDef (field 2)
                while (stream.Position < stream.Length)
                {
                    int tag = ReadVarint32(stream);
                    int fieldNumber = tag >> 3;
                    int wireType = tag & 0x07;

                    if (fieldNumber == 2 && wireType == 2) // meta_graphs
                    {
                        int metaGraphLength = ReadVarint32(stream);
                        long metaGraphEnd = stream.Position + metaGraphLength;

                        // Inside MetaGraphDef, look for GraphDef (field 2)
                        while (stream.Position < metaGraphEnd)
                        {
                            int mgTag = ReadVarint32(stream);
                            int mgField = mgTag >> 3;
                            int mgWire = mgTag & 0x07;

                            if (mgField == 2 && mgWire == 2) // graph_def
                            {
                                tensorInfos = ParseGraphDef(stream);
                                break;
                            }
                            else if (mgWire == 2)
                            {
                                int length = ReadVarint32(stream);
                                stream.Position += length;
                            }
                            else if (mgWire == 0)
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

                metadata.LayerCount = tensorInfos.Count;
                metadata.ParameterCount = tensorInfos.Sum(t => t.ElementCount);

                // Infer embedding dimension
                var embeddingLayer = tensorInfos.FirstOrDefault(t =>
                    t.LayerType == LayerType.Embedding ||
                    t.Name.Contains("embedding", StringComparison.OrdinalIgnoreCase));

                if (embeddingLayer.Shape != null && embeddingLayer.Shape.Length > 0)
                {
                    metadata.EmbeddingDimension = (int)embeddingLayer.Shape[embeddingLayer.Shape.Length - 1];
                }
            }
            catch (Exception ex)
            {
                metadata.Name = $"TensorFlow SavedModel (parse error: {ex.Message})";
            }

            return metadata;
        }

        public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
        {
            if (!ValidateFormat(stream))
                throw new ArgumentException("Invalid TensorFlow SavedModel format", nameof(stream));

            stream.Position = 0;
            var weights = new Dictionary<string, TensorInfo>();

            try
            {
                // Parse SavedModel for variables
                while (stream.Position < stream.Length)
                {
                    int tag = ReadVarint32(stream);
                    int fieldNumber = tag >> 3;
                    int wireType = tag & 0x07;

                    if (fieldNumber == 2 && wireType == 2) // meta_graphs
                    {
                        int metaGraphLength = ReadVarint32(stream);
                        long metaGraphEnd = stream.Position + metaGraphLength;

                        while (stream.Position < metaGraphEnd)
                        {
                            int mgTag = ReadVarint32(stream);
                            int mgField = mgTag >> 3;
                            int mgWire = mgTag & 0x07;

                            if (mgField == 2 && mgWire == 2) // graph_def
                            {
                                var tensorInfos = ParseGraphDef(stream);
                                foreach (var tensorInfo in tensorInfos)
                                {
                                    if (!string.IsNullOrEmpty(tensorInfo.Name))
                                    {
                                        weights[tensorInfo.Name] = tensorInfo;
                                    }
                                }
                                break;
                            }
                            else if (mgWire == 2)
                            {
                                int length = ReadVarint32(stream);
                                stream.Position += length;
                            }
                            else if (mgWire == 0)
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
                throw new InvalidOperationException($"Failed to read TensorFlow weights: {ex.Message}", ex);
            }

            return weights;
        }

        #region Protobuf Parsing

        private List<TensorInfo> ParseGraphDef(Stream stream)
        {
            int graphDefLength = ReadVarint32(stream);
            long graphDefEnd = stream.Position + graphDefLength;
            
            var tensorInfos = new List<TensorInfo>();

            // Parse NodeDef messages (field 1)
            while (stream.Position < graphDefEnd)
            {
                int tag = ReadVarint32(stream);
                int fieldNumber = tag >> 3;
                int wireType = tag & 0x07;

                if (fieldNumber == 1 && wireType == 2) // node
                {
                    var tensorInfo = ParseNodeDef(stream);
                    if (tensorInfo.ElementCount > 0)
                    {
                        tensorInfos.Add(tensorInfo);
                    }
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

            return tensorInfos;
        }

        private TensorInfo ParseNodeDef(Stream stream)
        {
            int nodeLength = ReadVarint32(stream);
            long nodeEnd = stream.Position + nodeLength;

            string name = string.Empty;
            string op = string.Empty;
            TensorDtype dtype = TensorDtype.F32;
            int[] shape = Array.Empty<int>();

            while (stream.Position < nodeEnd)
            {
                int tag = ReadVarint32(stream);
                int fieldNumber = tag >> 3;
                int wireType = tag & 0x07;

                switch (fieldNumber)
                {
                    case 1: // name
                        if (wireType == 2)
                        {
                            int nameLength = ReadVarint32(stream);
                            byte[] nameBytes = new byte[nameLength];
                            stream.Read(nameBytes, 0, nameLength);
                            name = System.Text.Encoding.UTF8.GetString(nameBytes);
                        }
                        break;

                    case 2: // op (operation type: "Variable", "Const", etc.)
                        if (wireType == 2)
                        {
                            int opLength = ReadVarint32(stream);
                            byte[] opBytes = new byte[opLength];
                            stream.Read(opBytes, 0, opLength);
                            op = System.Text.Encoding.UTF8.GetString(opBytes);
                        }
                        break;

                    case 5: // attr (attributes containing dtype, shape)
                        if (wireType == 2)
                        {
                            int attrLength = ReadVarint32(stream);
                            stream.Position += attrLength; // Simplified: skip attr parsing
                        }
                        break;

                    default:
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

            // Only include Variable and Const nodes (actual weights)
            if (op != "Variable" && op != "Const" && op != "VarHandleOp")
            {
                return new TensorInfo(); // Empty TensorInfo
            }

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
                DataOffset = 0,
                DataSize = 0,
                LayerIndex = TensorInfo.ExtractLayerIndex(name),
                LayerType = TensorInfo.InferLayerType(name)
            };
        }

        private TensorDtype MapTensorFlowDataType(int tfType)
        {
            // TensorFlow DataType enum values
            return tfType switch
            {
                1 => TensorDtype.F32,   // DT_FLOAT
                19 => TensorDtype.F16,  // DT_HALF
                14 => TensorDtype.BF16, // DT_BFLOAT16
                4 => TensorDtype.U8,    // DT_UINT8
                6 => TensorDtype.I8,    // DT_INT8
                3 => TensorDtype.I32,   // DT_INT32
                9 => TensorDtype.I64,   // DT_INT64
                10 => TensorDtype.Bool, // DT_BOOL
                _ => TensorDtype.F32    // Default
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
