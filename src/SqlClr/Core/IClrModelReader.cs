namespace SqlClrFunctions.Core
{
    using System.IO;

    /// <summary>
    /// Defines a common interface for synchronous, self-contained model format readers
    /// designed to operate within the SQL CLR environment.
    /// </summary>
    public interface IClrModelReader
    {
        /// <summary>
        /// Gets the name of the format this reader supports (e.g., "GGUF", "ONNX").
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Reads a specific tensor from a model stream.
        /// </summary>
        /// <param name="reader">A BinaryReader positioned at the start of the model stream.</param>
        /// <param name="tensorName">The name of the tensor to extract.</param>
        /// <returns>A float array containing the dequantized tensor data, or null if the tensor is not found.</returns>
        float[] ReadTensor(BinaryReader reader, string tensorName);
    }
}
