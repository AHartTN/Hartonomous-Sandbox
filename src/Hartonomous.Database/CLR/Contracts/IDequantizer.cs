using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Interface for dequantization strategies.
    /// Converts quantized weights to full-precision floats.
    /// </summary>
    public interface IDequantizer
    {
        /// <summary>
        /// Gets the quantization type this dequantizer handles.
        /// </summary>
        QuantizationType Type { get; }

        /// <summary>
        /// Dequantizes quantized bytes to float array.
        /// </summary>
        /// <param name="quantizedData">Quantized byte array</param>
        /// <param name="elementCount">Number of elements to dequantize</param>
        /// <returns>Dequantized float array</returns>
        float[] Dequantize(byte[] quantizedData, int elementCount);

        /// <summary>
        /// Gets the number of bytes per element for this quantization type.
        /// Used for buffer size calculations.
        /// </summary>
        int BytesPerElement { get; }
    }
}
