using System.Collections.Generic;
using Hartonomous.Clr.Enums;
using Hartonomous.Clr.Models;

namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Interface for parsing different model file formats.
    /// Enables strategy pattern for GGUF, SafeTensors, ONNX, PyTorch, TensorFlow, Stable Diffusion, etc.
    /// </summary>
    public interface IModelFormatReader
    {
        /// <summary>
        /// Gets the format type this reader handles.
        /// </summary>
        ModelFormat Format { get; }

        /// <summary>
        /// Validates that the stream contains valid data for this format.
        /// Checks magic numbers, header structure, etc.
        /// </summary>
        /// <param name="stream">Stream positioned at start of file</param>
        /// <returns>True if format is valid, false otherwise</returns>
        bool ValidateFormat(System.IO.Stream stream);

        /// <summary>
        /// Reads model metadata (architecture, layer count, parameter count, embedding dimension).
        /// Returns high-level model information without loading all tensor data.
        /// </summary>
        /// <param name="stream">Stream positioned at start of file</param>
        /// <returns>Model metadata structure</returns>
        ModelMetadata ReadMetadata(System.IO.Stream stream);

        /// <summary>
        /// Reads tensor metadata and optional weights from the model file.
        /// Returns dictionary mapping tensor names to TensorInfo structures.
        /// Tensor data can be retrieved using DataOffset/DataSize for lazy loading.
        /// </summary>
        /// <param name="stream">Stream positioned at start of file</param>
        /// <returns>Dictionary of tensor name → TensorInfo (with shape, dtype, quantization, offsets)</returns>
        Dictionary<string, TensorInfo> ReadWeights(System.IO.Stream stream);
    }
}

