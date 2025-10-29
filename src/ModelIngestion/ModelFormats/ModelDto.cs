using System.Collections.Generic;

namespace ModelIngestion.ModelFormats
{
    public class Model
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Architecture { get; set; }
        public List<Layer> Layers { get; set; } = new List<Layer>();
    }

    public class Layer
    {
        public int layer_idx { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public byte[]? Weights { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
