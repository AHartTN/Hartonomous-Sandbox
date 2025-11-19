using System;
using System.Collections.Generic;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// DBSCAN (Density-Based Spatial Clustering of Applications with Noise) algorithm.
    /// Discovers clusters of arbitrary shape and identifies noise points.
    /// 
    /// UNIVERSAL DISTANCE SUPPORT:
    /// Configurable distance metrics enable cross-modal density-based clustering:
    /// - Euclidean for spatial data
    /// - Cosine for semantic embeddings
    /// - Manhattan for sparse features
    /// </summary>
    internal static class DBSCANClustering
    {
        /// <summary>
        /// Cluster label constants
        /// </summary>
        public const int UNVISITED = -1;
        public const int NOISE = -2;

        /// <summary>
        /// Perform DBSCAN clustering on a collection of vectors.
        /// </summary>
        /// <param name="vectors">Vectors to cluster</param>
        /// <param name="epsilon">Maximum distance for two points to be neighbors</param>
        /// <param name="minPoints">Minimum points to form a dense region (core point)</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Array of cluster IDs (-2 = noise, -1 = unvisited, 0+ = cluster ID)</returns>
        public static int[] Cluster(float[][] vectors, double epsilon, int minPoints, IDistanceMetric? metric = null)
        {
            if (vectors == null || vectors.Length == 0)
                return Array.Empty<int>();

            int[] clusterIds = new int[vectors.Length];
            for (int i = 0; i < clusterIds.Length; i++)
                clusterIds[i] = UNVISITED;

            int currentClusterId = 0;

            for (int i = 0; i < vectors.Length; i++)
            {
                if (clusterIds[i] != UNVISITED)
                    continue; // Already processed

                            var neighbors = FindNeighbors(vectors, i, epsilon, metric ?? new EuclideanDistance());

                if (neighbors.Count < minPoints)
                {
                    clusterIds[i] = NOISE;
                }
                else
                {
                    ExpandCluster(vectors, i, neighbors, currentClusterId, epsilon, minPoints, clusterIds, metric!);
                    currentClusterId++;
                }
            }

            return clusterIds;
        }

        /// <summary>
        /// Find all neighbors within epsilon distance of a point.
        /// </summary>
        private static List<int> FindNeighbors(float[][] vectors, int pointIdx, double epsilon, IDistanceMetric metric)
        {
            var neighbors = new List<int>();
            for (int i = 0; i < vectors.Length; i++)
            {
                double distance = metric.Distance(vectors[pointIdx], vectors[i]);
                if (distance <= epsilon)
                    neighbors.Add(i);
            }
            return neighbors;
        }

        /// <summary>
        /// Expand a cluster from a core point using density-connectivity.
        /// </summary>
        private static void ExpandCluster(
            float[][] vectors,
            int pointIdx,
            List<int> neighbors,
            int clusterId,
            double epsilon,
            int minPoints,
            int[] clusterIds,
            IDistanceMetric metric)
        {
            clusterIds[pointIdx] = clusterId;

            var queue = new Queue<int>(neighbors);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                // Convert noise to border point
                if (clusterIds[current] == NOISE)
                    clusterIds[current] = clusterId;

                if (clusterIds[current] != UNVISITED)
                    continue; // Already processed

                clusterIds[current] = clusterId;

                // If this point is a core point, add its neighbors to expansion queue
                var currentNeighbors = FindNeighbors(vectors, current, epsilon, metric);
                if (currentNeighbors.Count >= minPoints)
                {
                    foreach (var neighborIdx in currentNeighbors)
                    {
                        if (clusterIds[neighborIdx] == UNVISITED || clusterIds[neighborIdx] == NOISE)
                            queue.Enqueue(neighborIdx);
                    }
                }
            }
        }

        /// <summary>
        /// Get cluster statistics from clustering results.
        /// </summary>
        public static ClusterStats GetStatistics(int[] clusterIds)
        {
            if (clusterIds == null || clusterIds.Length == 0)
                return new ClusterStats();

            var stats = new ClusterStats();
            var clusterCounts = new Dictionary<int, int>();

            foreach (int clusterId in clusterIds)
            {
                if (clusterId == NOISE)
                {
                    stats.NoisePoints++;
                }
                else if (clusterId >= 0)
                {
                    if (!clusterCounts.ContainsKey(clusterId))
                        clusterCounts[clusterId] = 0;
                    clusterCounts[clusterId]++;
                }
            }

            stats.NumClusters = clusterCounts.Count;
            stats.TotalPoints = clusterIds.Length;
            stats.ClusterSizes = clusterCounts;

            return stats;
        }

        /// <summary>
        /// Clustering statistics
        /// </summary>
        public struct ClusterStats
        {
            public int NumClusters;
            public int TotalPoints;
            public int NoisePoints;
            public Dictionary<int, int> ClusterSizes;
        }
    }
}
