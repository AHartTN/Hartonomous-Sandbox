namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Interface for tensor operations (matrix multiply, softmax, layer norm, etc).
    /// Enables abstraction over different computation backends.
    /// </summary>
    public interface ITensorOperation
    {
        /// <summary>
        /// Matrix multiplication: C = A * B
        /// </summary>
        /// <param name="A">Left matrix (M x K)</param>
        /// <param name="B">Right matrix (K x N)</param>
        /// <param name="rowsA">Number of rows in A</param>
        /// <param name="colsA">Number of columns in A (rows in B)</param>
        /// <param name="colsB">Number of columns in B</param>
        /// <returns>Result matrix C (M x N)</returns>
        float[] MatMul(float[] A, float[] B, int rowsA, int colsA, int colsB);

        /// <summary>
        /// Softmax activation: softmax(x_i) = exp(x_i) / sum(exp(x_j))
        /// </summary>
        /// <param name="input">Input array (modified in-place)</param>
        void Softmax(float[] input);

        /// <summary>
        /// Layer normalization: (x - mean) / sqrt(variance + epsilon)
        /// </summary>
        /// <param name="input">Input array</param>
        /// <param name="gamma">Scale parameter</param>
        /// <param name="beta">Shift parameter</param>
        /// <param name="epsilon">Numerical stability constant</param>
        /// <returns>Normalized array</returns>
        float[] LayerNorm(float[] input, float[] gamma, float[] beta, float epsilon = 1e-5f);
    }
}
