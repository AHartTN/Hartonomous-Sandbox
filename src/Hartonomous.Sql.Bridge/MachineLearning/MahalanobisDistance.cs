using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Hartonomous.Sql.Bridge.MachineLearning
{
    /// <summary>
    /// Proper Mahalanobis distance with full covariance matrix.
    /// REPLACES: AnomalyDetectionAggregates.cs:497-525 (diagonal-only covariance - mathematically incorrect)
    /// 
    /// Formula: d_M(x) = sqrt((x - μ)ᵀ Σ⁻¹ (x - μ))
    /// where Σ is the FULL covariance matrix (not just diagonal).
    /// </summary>
    public class MahalanobisDistance
    {
        /// <summary>
        /// Compute Mahalanobis distance from point to distribution.
        /// Uses Cholesky decomposition for numerical stability.
        /// </summary>
        /// <param name="point">Point to measure distance from</param>
        /// <param name="mean">Mean vector of distribution</param>
        /// <param name="covariance">FULL covariance matrix (D x D)</param>
        /// <returns>Mahalanobis distance</returns>
        public static double Compute(float[] point, float[] mean, float[][] covariance)
        {
            if (point == null || mean == null || covariance == null)
                throw new ArgumentNullException("Arguments cannot be null");

            if (point.Length != mean.Length)
                throw new ArgumentException("Point and mean must have same dimensionality");

            if (covariance.Length != point.Length || covariance[0].Length != point.Length)
                throw new ArgumentException("Covariance matrix must be D x D where D is point dimensionality");

            int D = point.Length;

            // Convert to MathNet matrices for robust linear algebra
            var pointVec = DenseVector.OfArray(point);
            var meanVec = DenseVector.OfArray(mean);
            var covMatrix = CreateMatrix(covariance);

            // Compute difference: (x - μ)
            var diff = pointVec.Subtract(meanVec);

            // Compute inverse of covariance using Cholesky decomposition (more stable than direct inversion)
            Matrix<float> covInverse;
            try
            {
                var chol = covMatrix.Cholesky();
                covInverse = chol.Solve(DenseMatrix.CreateIdentity(D));
            }
            catch (Exception)
            {
                // Fallback: Add small regularization if matrix is singular
                var regularized = covMatrix.Add(DenseMatrix.CreateIdentity(D).Multiply(1e-6f));
                var chol = regularized.Cholesky();
                covInverse = chol.Solve(DenseMatrix.CreateIdentity(D));
            }

            // Compute quadratic form: (x - μ)ᵀ Σ⁻¹ (x - μ)
            var product = covInverse.Multiply(diff);
            var mahalanobis = diff.DotProduct(product);

            return Math.Sqrt(Math.Max(0, mahalanobis)); // Ensure non-negative due to numerical errors
        }

        /// <summary>
        /// Compute FULL covariance matrix from vectors (not just diagonal).
        /// Cov[i,j] = E[(X_i - μ_i)(X_j - μ_j)]
        /// </summary>
        public static float[][] ComputeCovarianceMatrix(float[][] vectors)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors cannot be empty");

            int N = vectors.Length;
            int D = vectors[0].Length;

            // Compute mean
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

            // Compute full covariance matrix (not diagonal!)
            var covariance = new float[D][];
            for (int i = 0; i < D; i++)
            {
                covariance[i] = new float[D];
                for (int j = 0; j < D; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < N; k++)
                    {
                        sum += (vectors[k][i] - mean[i]) * (vectors[k][j] - mean[j]);
                    }
                    covariance[i][j] = sum / N;
                }
            }

            return covariance;
        }

        private static DenseMatrix CreateMatrix(float[][] array)
        {
            int rows = array.Length;
            int cols = array[0].Length;
            var matrix = new DenseMatrix(rows, cols);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = array[i][j];
                }
            }
            return matrix;
        }
    }
}
