namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Quantization types with int backing for SQL Server CLR performance.
    /// Matches GGML quantization schemes. Use with Core/Dequantizers.cs.
    /// </summary>
    public enum QuantizationType : int
    {
        None = 0,       // No quantization (full precision)
        F32 = 1,        // 32-bit float
        F16 = 2,        // 16-bit float
        Q8_0 = 8,       // 8-bit symmetric quantization
        Q4_0 = 9,       // 4-bit quantization (32-element blocks)
        Q4_1 = 10,      // 4-bit with bias
        Q5_0 = 11,      // 5-bit quantization
        Q5_1 = 12,      // 5-bit with bias
        Q2_K = 13,      // 2-bit K-quantization
        Q3_K = 14,      // 3-bit K-quantization
        Q4_K = 15,      // 4-bit K-quantization
        Q5_K = 16,      // 5-bit K-quantization
        Q6_K = 17,      // 6-bit K-quantization
        Q8_K = 18,      // 8-bit K-quantization
        IQ1_S = 20,     // 1-bit importance quantization
        IQ2_XXS = 21,
        IQ2_XS = 22,
        IQ3_XXS = 23,
        IQ3_S = 24,
        IQ4_XS = 25
    }
}
