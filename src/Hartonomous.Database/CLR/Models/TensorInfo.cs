using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Unified tensor metadata structure for all model formats.
    /// Replaces duplicated GGUFTensorInfo, TensorMetadata classes.
    /// </summary>
    public struct TensorInfo
    {
        /// <summary>
        /// Tensor name (e.g., "model.layers.0.self_attn.q_proj.weight")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data type (F32, F16, Q8_0, etc.)
        /// </summary>
        public TensorDtype Dtype { get; set; }

        /// <summary>
        /// Quantization type (if quantized)
        /// </summary>
        public QuantizationType Quantization { get; set; }

        /// <summary>
        /// Shape dimensions (e.g., [4096, 4096] for weight matrix)
        /// </summary>
        public long[] Shape { get; set; }

        /// <summary>
        /// Total number of elements (product of shape)
        /// </summary>
        public long ElementCount { get; set; }

        /// <summary>
        /// Byte offset in file where tensor data starts
        /// </summary>
        public long DataOffset { get; set; }

        /// <summary>
        /// Size of tensor data in bytes
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        /// Layer index (extracted from name, e.g., layer 0 from "model.layers.0...")
        /// </summary>
        public int LayerIndex { get; set; }

        /// <summary>
        /// Layer type (Dense, Attention, LayerNorm, etc.)
        /// </summary>
        public LayerType LayerType { get; set; }

        public TensorInfo(string name, TensorDtype dtype, long[] shape, long dataOffset, long dataSize)
        {
            Name = name;
            Dtype = dtype;
            Quantization = QuantizationType.None;
            Shape = shape;
            ElementCount = 1;
            foreach (var dim in shape)
                ElementCount *= dim;
            DataOffset = dataOffset;
            DataSize = dataSize;
            LayerIndex = ExtractLayerIndex(name);
            LayerType = InferLayerType(name);
        }

        public static int ExtractLayerIndex(string name)
        {
            // Extract layer number from names like "model.layers.0.self_attn.q_proj.weight"
            var parts = name.Split('.');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "layers" && int.TryParse(parts[i + 1], out int layerIdx))
                    return layerIdx;
            }
            return 0;
        }

        public static LayerType InferLayerType(string name)
        {
            var lower = name.ToLowerInvariant();
            if (lower.Contains("embed")) return LayerType.Embedding;
            if (lower.Contains("attn")) return LayerType.Attention;
            if (lower.Contains("norm")) return LayerType.LayerNorm;
            if (lower.Contains("mlp") || lower.Contains("ffn") || lower.Contains("fc")) return LayerType.FeedForward;
            if (lower.Contains("down")) return LayerType.UNetDown;
            if (lower.Contains("mid")) return LayerType.UNetMid;
            if (lower.Contains("up")) return LayerType.UNetUp;
            if (lower.Contains("vae")) return LayerType.VAE;
            if (lower.Contains("conv")) return LayerType.Convolution;
            return LayerType.Dense;
        }
    }
}
