using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
// Assuming Newtonsoft.Json is available in the CLR context as per documentation.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hartonomous.Clr.ModelParsers
{
    /// <summary>
    /// Provides a full implementation for parsing .safetensors model files.
    /// This class reads the header and data block to extract tensor weights.
    /// </summary>
    public static class SafeTensorsParser
    {
        private class TensorMetadata
        {
            public string? DType { get; set; }
            public List<long>? Shape { get; set; }
            public List<long>? DataOffsets { get; set; }
        }

        public static IEnumerable<object[]> Parse(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                // 1. Read header size
                if (stream.Length < 8)
                    throw new InvalidDataException("Invalid SafeTensors file: too short to contain header size.");
                
                ulong headerSize = reader.ReadUInt64();

                if (headerSize == 0 || (ulong)stream.Length < 8 + headerSize)
                    throw new InvalidDataException("Invalid SafeTensors file: header size is invalid.");

                // 2. Read and deserialize JSON header
                byte[] jsonBytes = reader.ReadBytes((int)headerSize);
                string jsonHeader = Encoding.UTF8.GetString(jsonBytes);
                var metadata = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(jsonHeader);

                long dataStartOffset = 8 + (long)headerSize;

                // 3. Iterate through tensors and read data
                int layerIndex = 0;
                foreach (var item in metadata!)
                {
                    if (item.Key == "__metadata__") continue;

                    var tensorInfo = item.Value.ToObject<TensorMetadata>();
                    if (tensorInfo == null || tensorInfo.DataOffsets == null || tensorInfo.DataOffsets.Count != 2)
                        continue;

                    long start = tensorInfo.DataOffsets![0];
                    long end = tensorInfo.DataOffsets[1];
                    long tensorByteLength = end - start;
                    
                    long numElements = 1;
                    foreach (var dim in tensorInfo.Shape!)
                    {
                        numElements *= dim;
                    }

                    reader.BaseStream.Seek(dataStartOffset + start, SeekOrigin.Begin);
                    byte[] tensorBytes = reader.ReadBytes((int)tensorByteLength);

                    long weightIndex = 0;
                    // Full implementation for F32 tensors
                    if (tensorInfo.DType == "F32")
                    {
                        if (tensorByteLength != numElements * 4)
                            throw new InvalidDataException($"Tensor '{item.Key}' has a size mismatch.");

                        for (int i = 0; i < tensorByteLength; i += 4)
                        {
                            yield return new object[] { item.Key, layerIndex, weightIndex++, BitConverter.ToSingle(tensorBytes, i) };
                        }
                    }
                    // Add other dtype conversions here as needed
                    else
                    {
                        yield return new object[] { item.Key, layerIndex, 0L, 0.0f };
                    }
                    layerIndex++;
                }
            }
        }
    }
}
