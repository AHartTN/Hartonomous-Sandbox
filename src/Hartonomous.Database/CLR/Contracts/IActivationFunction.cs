using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Interface for activation function implementations.
    /// Enables strategy pattern and SIMD optimizations.
    /// </summary>
    public interface IActivationFunction
    {
        /// <summary>
        /// Gets the activation function type.
        /// </summary>
        ActivationFunction Type { get; }

        /// <summary>
        /// Applies activation function element-wise to input array.
        /// </summary>
        /// <param name="input">Input array (modified in-place for performance)</param>
        void Apply(float[] input);

        /// <summary>
        /// Computes derivative of activation function for backpropagation.
        /// </summary>
        /// <param name="output">Output of forward pass</param>
        /// <returns>Derivative array</returns>
        float[] Derivative(float[] output);
    }
}
