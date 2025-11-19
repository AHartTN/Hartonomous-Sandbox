using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Local Outlier Factor (LOF) - Density-based anomaly detection algorithm.
    /// 
    /// LOF detects outliers by comparing the local density of a point to the
    /// densities of its neighbors. Points in sparse regions have higher LOF scores.
    /// 
    /// Key Concepts:
    /// - k-distance: Distance to the k-th nearest neighbor
    /// - Reachability distance: max(k-distance of neighbor, actual distance)
    /// - Local Reachability Density (LRD): Inverse of average reachability distance
    /// - LOF: Ratio of neighbor LRDs to own LRD (>1 = outlier, ~1 = normal)
    /// 
    /// UNIVERSAL DISTANCE SUPPORT:
    /// Works with ANY distance metric via IDistanceMetric interface:
    /// - Euclidean for spatial/general embeddings
    /// - Cosine for semantic/text embeddings
    /// - Manhattan for sparse/grid features
    /// - Custom metrics for domain-specific data
    /// 
    /// Applications:
    /// - Anomaly detection in embeddings (text, image, audio, code, model weights)
    /// - Quality control (outlier detection in production data)
    /// - Fraud detection (unusual transaction patterns)
    /// - Network intrusion detection (abnormal traffic patterns)
    /// - Medical diagnosis (rare conditions in patient data)
    /// 
    /// Usage:
    ///   var metric = DistanceMetricFactory.Create(DistanceMetricType.Euclidean);
    ///   var lof = LocalOutlierFactor.Compute(vectors, k: 5, metric);
    ///   // scores > 1.5 are typically outliers
    /// </summary>
    public static class LocalOutlierFactor
    {
        /// <summary>
        /// Compute LOF scores for all points in the dataset.
        /// </summary>
        /// <param name="data">Array of vectors to analyze</param>
        /// <param name="k">Number of nearest neighbors to consider (typically 3-20)</param>
        /// <param name="metric">Distance metric to use (null = Euclidean)</param>
        /// <returns>Array of LOF scores (one per point). Score > 1 indicates outlier.</returns>
        public static double[] Compute(float[][] data, int k)
        {
            return Compute(data, k, new EuclideanDistance());
        }

        public static double[] Compute(float[][] data, int k, IDistanceMetric metric)
        {
            if (data == null || data.Length < k + 1)
                throw new ArgumentException($"Dataset must contain at least {k + 1} points for k={k}");

            if (k < 1)
                throw new ArgumentException("k must be at least 1");

            // Default to Euclidean if no metric specified
            if (metric == null)
                metric = new EuclideanDistance();

            int n = data.Length;
            var lofScores = new double[n];

            // Step 1: Compute k-distances for all points
            var kDistances = new double[n];
            var kNeighbors = new List<int>[n];

            for (int i = 0; i < n; i++)
            {
                // Find k nearest neighbors using configured metric
                var distances = new List<(int Index, double Distance)>();
                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        double dist = metric.Distance(data[i], data[j]);
                        distances.Add((j, dist));
                    }
                }

                // Sort by distance and take k nearest
                var neighbors = distances.OrderBy(d => d.Distance).Take(k).ToList();
                kNeighbors[i] = neighbors.Select(x => x.Index).ToList();
                kDistances[i] = neighbors.Last().Distance; // k-distance is distance to k-th neighbor
            }

            // Step 2: Compute Local Reachability Density (LRD) for all points
            var lrds = new double[n];

            for (int i = 0; i < n; i++)
            {
                double sumReachDist = 0;

                foreach (int neighborIdx in kNeighbors[i])
                {
                    // Reachability distance = max(k-distance of neighbor, actual distance)
                    double actualDist = metric.Distance(data[i], data[neighborIdx]);
                    double reachDist = Math.Max(kDistances[neighborIdx], actualDist);
                    sumReachDist += reachDist;
                }

                double avgReachDist = sumReachDist / k;
                lrds[i] = 1.0 / (avgReachDist + 1e-10); // Avoid division by zero
            }

            // Step 3: Compute LOF for all points
            for (int i = 0; i < n; i++)
            {
                double sumNeighborLrd = 0;

                foreach (int neighborIdx in kNeighbors[i])
                {
                    sumNeighborLrd += lrds[neighborIdx];
                }

                double avgNeighborLrd = sumNeighborLrd / k;
                lofScores[i] = avgNeighborLrd / (lrds[i] + 1e-10); // Ratio of neighbor density to own density
            }

            return lofScores;
        }

        /// <summary>
        /// Compute LOF scores with metric specified by name (for SQL interop).
        /// </summary>
        public static double[] Compute(float[][] data, int k, string metricName, double metricParameter = 2.0)
        {
            var metric = DistanceMetricFactory.Create(metricName, metricParameter);
            return Compute(data, k, metric);
        }

        /// <summary>
        /// Identify outliers based on LOF threshold.
        /// </summary>
        /// <param name="lofScores">LOF scores from Compute()</param>
        /// <param name="threshold">Threshold for outlier classification (default 1.5)</param>
        /// <returns>Indices of outlier points</returns>
        public static int[] GetOutliers(double[] lofScores, double threshold = 1.5)
        {
            return lofScores
                .Select((score, idx) => (Score: score, Index: idx))
                .Where(x => x.Score > threshold)
                .Select(x => x.Index)
                .ToArray();
        }

        /// <summary>
        /// Compute LOF and return top-N outliers.
        /// </summary>
        public static (int Index, double LOF)[] FindTopOutliers(float[][] data, int k, int topN)
        {
            return FindTopOutliers(data, k, topN, new EuclideanDistance());
        }

        public static (int Index, double LOF)[] FindTopOutliers(float[][] data, int k, int topN, IDistanceMetric metric)
        {
            var scores = Compute(data, k, metric);
            
            return scores
                .Select((score, idx) => (Index: idx, LOF: score))
                .OrderByDescending(x => x.LOF)
                .Take(topN)
                .ToArray();
        }
    }
}
