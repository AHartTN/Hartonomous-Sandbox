namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Tensor data types with int backing for SQL Server CLR performance.
    /// Maps to GGML types, SafeTensors dtypes, ONNX tensor types.
    /// </summary>
    public enum TensorDtype : int
    {
        Unknown = 0,
        F32 = 1,    // 32-bit float
        F16 = 2,    // 16-bit float (half precision)
        BF16 = 3,   // BFloat16 (Brain Floating Point)
        I8 = 4,     // 8-bit signed integer
        U8 = 5,     // 8-bit unsigned integer
        I16 = 6,    // 16-bit signed integer
        U16 = 7,    // 16-bit unsigned integer
        I32 = 8,    // 32-bit signed integer
        U32 = 9,    // 32-bit unsigned integer
        I64 = 10,   // 64-bit signed integer
        U64 = 11,   // 64-bit unsigned integer
        Bool = 12,  // Boolean
        Q8_0 = 20,  // 8-bit quantization (symmetric)
        Q4_0 = 21,  // 4-bit quantization block
        Q4_1 = 22,  // 4-bit quantization block with bias
        Q5_0 = 23,  // 5-bit quantization
        Q5_1 = 24,  // 5-bit quantization with bias
        Q4_K = 25,  // 4-bit K-quantization
        Q5_K = 26,  // 5-bit K-quantization
        Q6_K = 27,  // 6-bit K-quantization
        IQ1_S = 30, // 1-bit importance quantization
        IQ2_XXS = 31,
        IQ2_XS = 32,
        IQ3_XXS = 33,
        IQ3_S = 34,
        IQ4_XS = 35
    }
}
