using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModelIngestion
{
    public class SafetensorsModelReader : IModelReader
    {
        public Model Read(string modelPath)
        {
            var model = new Model();

            using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    // 1. Read the header length
                    var headerLength = reader.ReadInt64();

                    // 2. Read the header
                    var headerBytes = reader.ReadBytes((int)headerLength);
                    var headerJson = Encoding.UTF8.GetString(headerBytes);
                    var header = JsonConvert.DeserializeObject<SafetensorsHeader>(headerJson);

                    model.Name = Path.GetFileNameWithoutExtension(modelPath);
                    model.Type = "Safetensors";
                    if (header?.Metadata != null && header.Metadata.TryGetValue("format", out var format))
                    {
                        model.Architecture = format;
                    }

                    // 3. Read the tensor data
                    if (header?.Tensors != null)
                    {
                        var layer_idx = 0;
                        foreach (var tensorToken in header.Tensors)
                        {
                            var tensorInfo = tensorToken.Value.ToObject<SafetensorTensorInfo>();
                            if (tensorInfo != null)
                            {
                                var layer = new Layer
                                {
                                    layer_idx = layer_idx,
                                    Name = tensorToken.Key,
                                    Type = tensorInfo.DType,
                                    Parameters = new Dictionary<string, object>()
                                };

                                if (tensorInfo.Shape != null)
                                {
                                    layer.Parameters.Add("shape", tensorInfo.Shape);
                                }

                                if (tensorInfo.DataOffsets != null)
                                {
                                    var dataOffsets = tensorInfo.DataOffsets;
                                    var tensorLength = dataOffsets[1] - dataOffsets[0];

                                    reader.BaseStream.Seek(dataOffsets[0], SeekOrigin.Current);
                                    layer.Weights = reader.ReadBytes((int)tensorLength);
                                }

                                model.Layers.Add(layer);
                                layer_idx++;
                            }
                        }
                    }
                }
            }

            return model;
        }
    }

    public class SafetensorsHeader
    {
        [JsonProperty("__metadata__")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken>? Tensors { get; set; }
    }

    public class SafetensorTensorInfo
    {
        [JsonProperty("dtype")]
        public string? DType { get; set; }

        [JsonProperty("shape")]
        public List<long>? Shape { get; set; }

        [JsonProperty("data_offsets")]
        public List<long>? DataOffsets { get; set; }
    }
}