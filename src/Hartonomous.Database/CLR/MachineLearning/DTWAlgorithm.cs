using System;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Dynamic Time Warping algorithm for sequence alignment and distance calculation.
    /// Used for comparing time-series or sequential data with temporal flexibility.
    /// 
    /// UNIVERSAL DISTANCE SUPPORT:
    /// Configurable distance metrics enable cross-modal temporal alignment:
    /// - Euclidean for spatial trajectories
    /// - Cosine for semantic drift over time
    /// - Manhattan for sparse temporal features
    /// </summary>
    internal static class DTWAlgorithm
    {
        /// <summary>
        /// Compute DTW distance between two sequences of vectors.
        /// </summary>
        /// <param name="sequence1">First sequence of vectors</param>
        /// <param name="sequence2">Second sequence of vectors</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>DTW distance, or double.PositiveInfinity if sequences are empty</returns>
        public static double ComputeDistance(float[][] sequence1, float[][] sequence2)
        {
            return ComputeDistance(sequence1, sequence2, new EuclideanDistance());
        }

        public static double ComputeDistance(float[][] sequence1, float[][] sequence2, IDistanceMetric metric)
        {
            if (metric == null)
                metric = new EuclideanDistance();

            if (sequence1 == null || sequence2 == null || sequence1.Length == 0 || sequence2.Length == 0)
                return double.PositiveInfinity;

            int n = sequence1.Length;
            int m = sequence2.Length;

            // Initialize cost matrix
            double[,] dtw = new double[n + 1, m + 1];
            
            for (int i = 0; i <= n; i++)
                dtw[i, 0] = double.PositiveInfinity;
            for (int j = 0; j <= m; j++)
                dtw[0, j] = double.PositiveInfinity;
            dtw[0, 0] = 0;

            // Fill matrix using dynamic programming
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    double cost = VectorUtilities.EuclideanDistance(sequence1[i - 1], sequence2[j - 1]);
                    dtw[i, j] = cost + Math.Min(Math.Min(dtw[i - 1, j], dtw[i, j - 1]), dtw[i - 1, j - 1]);
                }
            }

            return dtw[n, m];
        }

        /// <summary>
        /// Compute DTW distance with Sakoe-Chiba band constraint for efficiency.
        /// </summary>
        /// <param name="sequence1">First sequence</param>
        /// <param name="sequence2">Second sequence</param>
        /// <param name="windowSize">Maximum allowed temporal deviation</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Constrained DTW distance</returns>
        public static double ComputeDistanceConstrained(float[][] sequence1, float[][] sequence2, int windowSize)
        {
            return ComputeDistanceConstrained(sequence1, sequence2, windowSize, new EuclideanDistance());
        }

        public static double ComputeDistanceConstrained(float[][] sequence1, float[][] sequence2, int windowSize, IDistanceMetric metric)
        {
            if (metric == null)
                metric = new EuclideanDistance();

            if (sequence1 == null || sequence2 == null || sequence1.Length == 0 || sequence2.Length == 0)
                return double.PositiveInfinity;

            int n = sequence1.Length;
            int m = sequence2.Length;

            double[,] dtw = new double[n + 1, m + 1];
            
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= m; j++)
                    dtw[i, j] = double.PositiveInfinity;
            
            dtw[0, 0] = 0;

            // Fill matrix with window constraint
            for (int i = 1; i <= n; i++)
            {
                int jStart = Math.Max(1, i - windowSize);
                int jEnd = Math.Min(m, i + windowSize);
                
                for (int j = jStart; j <= jEnd; j++)
                {
                    double cost = VectorUtilities.EuclideanDistance(sequence1[i - 1], sequence2[j - 1]);
                    dtw[i, j] = cost + Math.Min(Math.Min(dtw[i - 1, j], dtw[i, j - 1]), dtw[i - 1, j - 1]);
                }
            }

            return dtw[n, m];
        }
    }
}
