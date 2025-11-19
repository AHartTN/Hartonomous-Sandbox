using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Model metadata extracted from model files.
    /// Unified structure for GGUF, SafeTensors, ONNX, PyTorch, etc.
    /// </summary>
    public struct ModelMetadata
    {
        /// <summary>
        /// Model format (GGUF, SafeTensors, etc.)
        /// </summary>
        public ModelFormat Format { get; set; }

        /// <summary>
        /// Model name (e.g., "Qwen3-Coder-32B")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Model architecture (e.g., "llama", "gpt2", "stable-diffusion")
        /// </summary>
        public string Architecture { get; set; }

        /// <summary>
        /// Number of layers
        /// </summary>
        public int LayerCount { get; set; }

        /// <summary>
        /// Embedding dimension
        /// </summary>
        public int EmbeddingDimension { get; set; }

        /// <summary>
        /// Context length / max sequence length
        /// </summary>
        public int ContextLength { get; set; }

        /// <summary>
        /// Vocabulary size
        /// </summary>
        public int VocabSize { get; set; }

        /// <summary>
        /// Number of attention heads
        /// </summary>
        public int AttentionHeads { get; set; }

        /// <summary>
        /// Total parameter count
        /// </summary>
        public long ParameterCount { get; set; }

        /// <summary>
        /// Primary quantization type used in model
        /// </summary>
        public QuantizationType QuantizationType { get; set; }

        /// <summary>
        /// Model file size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        public ModelMetadata(ModelFormat format, string name, string architecture)
        {
            Format = format;
            Name = name;
            Architecture = architecture;
            LayerCount = 0;
            EmbeddingDimension = 0;
            ContextLength = 0;
            VocabSize = 0;
            AttentionHeads = 0;
            ParameterCount = 0;
            QuantizationType = QuantizationType.None;
            FileSizeBytes = 0;
        }
    }
}
