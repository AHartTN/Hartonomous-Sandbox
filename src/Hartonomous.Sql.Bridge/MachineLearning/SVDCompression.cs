using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Hartonomous.Sql.Bridge.MachineLearning
{
    /// <summary>
    /// Proper SVD-based dimensionality reduction (replaces variance-picking "autoencoder").
    /// REPLACES: NeuralVectorAggregates.cs:247-285 (fake autoencoder that just picks high-variance dimensions)
    /// 
    /// Uses Singular Value Decomposition to find optimal low-rank approximation.
    /// </summary>
    public class SVDCompression
    {
        /// <summary>
        /// Compress high-dimensional vectors to lower dimensions using SVD.
        /// Finds the optimal k-dimensional subspace that minimizes reconstruction error.
        /// </summary>
        /// <param name="vectors">Input vectors (N x D)</param>
        /// <param name="targetDim">Target dimensionality k (k &lt; D)</param>
        /// <returns>Compressed vectors (N x k)</returns>
        public static float[][] Compress(float[][] vectors, int targetDim)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors cannot be empty", nameof(vectors));

            int N = vectors.Length;
            int D = vectors[0].Length;

            if (targetDim >= D)
                throw new ArgumentException($"Target dimension {targetDim} must be less than original dimension {D}");

            // Step 1: Center the data (subtract mean)
            var mean = new float[D];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < D; j++)
                {
                    mean[j] += vectors[i][j];
                }
            }
            for (int j = 0; j < D; j++)
            {
                mean[j] /= N;
            }

            // Step 2: Create centered data matrix (N x D)
            var dataMatrix = DenseMatrix.Create(N, D, (i, j) => vectors[i][j] - mean[j]);

            // Step 3: Compute SVD: X = U * Σ * V^T
            var svd = dataMatrix.Svd(computeVectors: true);

            // Step 4: Extract top k singular vectors (U_k)
            // U is N x N, we want N x k
            var U_k = svd.U.SubMatrix(0, N, 0, targetDim);

            // Step 5: Extract top k singular values (Σ_k)
            var S_k = svd.S.SubVector(0, targetDim);

            // Step 6: Compressed representation = U_k * Σ_k
            // This is the N x k matrix of compressed vectors
            var compressed = new float[N][];
            for (int i = 0; i < N; i++)
            {
                compressed[i] = new float[targetDim];
                for (int j = 0; j < targetDim; j++)
                {
                    compressed[i][j] = (float)(U_k[i, j] * S_k[j]);
                }
            }

            return compressed;
        }

        /// <summary>
        /// Decompress vectors back to original dimensionality.
        /// Note: This is lossy compression, reconstruction will have error.
        /// </summary>
        /// <param name="compressed">Compressed vectors (N x k)</param>
        /// <param name="originalDim">Original dimensionality D</param>
        /// <param name="basis">Right singular vectors V_k (D x k) from Compress operation</param>
        /// <returns>Reconstructed vectors (N x D)</returns>
        public static float[][] Decompress(float[][] compressed, int originalDim, float[][] basis)
        {
            if (compressed == null || compressed.Length == 0)
                throw new ArgumentException("Compressed vectors cannot be empty", nameof(compressed));

            int N = compressed.Length;
            int k = compressed[0].Length;

            if (basis == null || basis.Length != originalDim || basis[0].Length != k)
                throw new ArgumentException($"Basis must be {originalDim} x {k}", nameof(basis));

            // Reconstruct: X_reconstructed = compressed * V_k^T
            var reconstructed = new float[N][];
            for (int i = 0; i < N; i++)
            {
                reconstructed[i] = new float[originalDim];
                for (int j = 0; j < originalDim; j++)
                {
                    float sum = 0;
                    for (int kk = 0; kk < k; kk++)
                    {
                        sum += compressed[i][kk] * basis[j][kk];
                    }
                    reconstructed[i][j] = sum;
                }
            }

            return reconstructed;
        }

        /// <summary>
        /// Compute reconstruction error for compressed representation.
        /// </summary>
        /// <param name="original">Original vectors</param>
        /// <param name="reconstructed">Reconstructed vectors</param>
        /// <returns>Mean squared error</returns>
        public static double ComputeReconstructionError(float[][] original, float[][] reconstructed)
        {
            if (original.Length != reconstructed.Length)
                throw new ArgumentException("Vector counts must match");

            double totalError = 0;
            int count = 0;

            for (int i = 0; i < original.Length; i++)
            {
                for (int j = 0; j < original[i].Length; j++)
                {
                    double diff = original[i][j] - reconstructed[i][j];
                    totalError += diff * diff;
                    count++;
                }
            }

            return totalError / count;
        }

        /// <summary>
        /// Get explained variance ratio for each principal component.
        /// Helps determine how many dimensions to keep.
        /// </summary>
        /// <param name="vectors">Input vectors</param>
        /// <returns>Variance explained by each component (sorted descending)</returns>
        public static float[] GetExplainedVariance(float[][] vectors)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors cannot be empty", nameof(vectors));

            int N = vectors.Length;
            int D = vectors[0].Length;

            // Center the data
            var mean = new float[D];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < D; j++)
                {
                    mean[j] += vectors[i][j];
                }
            }
            for (int j = 0; j < D; j++)
            {
                mean[j] /= N;
            }

            // Create centered data matrix
            var dataMatrix = DenseMatrix.Create(N, D, (i, j) => vectors[i][j] - mean[j]);

            // Compute SVD
            var svd = dataMatrix.Svd(computeVectors: false);

            // Singular values squared give variance
            var singularValues = svd.S.ToArray();
            var totalVariance = singularValues.Sum(s => s * s);

            var explainedVariance = new float[singularValues.Length];
            for (int i = 0; i < singularValues.Length; i++)
            {
                explainedVariance[i] = (float)((singularValues[i] * singularValues[i]) / totalVariance);
            }

            return explainedVariance;
        }
    }
}
