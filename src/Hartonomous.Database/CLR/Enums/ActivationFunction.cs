namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Activation function types with int backing for SQL Server CLR performance.
    /// Use Core/ActivationFunctions.cs for SIMD-optimized implementations.
    /// </summary>
    public enum ActivationFunction : int
    {
        None = 0,
        ReLU = 1,       // Rectified Linear Unit
        GELU = 2,       // Gaussian Error Linear Unit
        Tanh = 3,       // Hyperbolic tangent
        Sigmoid = 4,    // Logistic sigmoid
        Swish = 5,      // Swish (SiLU)
        Mish = 6,       // Mish
        SiLU = 7,       // Sigmoid Linear Unit (same as Swish)
        Softmax = 8,    // Softmax (for attention/output)
        LeakyReLU = 9,  // Leaky ReLU
        ELU = 10,       // Exponential Linear Unit
        SELU = 11       // Scaled Exponential Linear Unit
    }
}
