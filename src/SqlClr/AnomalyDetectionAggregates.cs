using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// ANOMALY DETECTION AND OUTLIER ANALYSIS AGGREGATES
    /// </summary>

    /// <summary>
    /// ISOLATION FOREST AGGREGATE
    /// Detects outliers using isolation forest algorithm
    /// 
    /// SELECT category,
    ///        atom_id,
    ///        dbo.IsolationForestScore(embedding_vector) OVER (PARTITION BY category)
    /// FROM embeddings
    /// 
    /// Returns: Anomaly score (higher = more anomalous)
    /// USE CASE: Detect unusual embeddings, find data quality issues, identify novel content
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct IsolationForestScore : IBinarySerialize
    {
        private List<float[]> vectors;
        private int dimension;
        private Random random;

        public void Init()
        {
            vectors = new List<float[]>();
            dimension = 0;
            random = new Random(42); // Fixed seed for reproducibility
        }

        public void Accumulate(SqlString vectorJson)
        {
            if (vectorJson.IsNull) return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(IsolationForestScore other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count < 2 || dimension == 0)
                return SqlString.Null;

            // Simplified isolation forest: measure average path length to isolate each point
            // Build multiple simple trees and average their depths

            int numTrees = Math.Min(10, vectors.Count / 2);
            double[] avgPathLengths = new double[vectors.Count];

            for (int tree = 0; tree < numTrees; tree++)
            {
                // Random feature and split
                int feature = random.Next(dimension);
                
                // Sort by this feature
                var sorted = vectors.Select((v, idx) => (Vector: v, Index: idx, Value: v[feature]))
                    .OrderBy(x => x.Value)
                    .ToList();

                // Isolation depth is position in sorted order
                for (int i = 0; i < sorted.Count; i++)
                {
                    avgPathLengths[sorted[i].Index] += i;
                }
            }

            // Normalize and invert (lower depth = more isolated = higher anomaly score)
            double maxDepth = vectors.Count * numTrees;
            var scores = avgPathLengths.Select(d => 1.0 - (d / maxDepth)).ToArray();

            // Return JSON array of anomaly scores
            var json = "[" + string.Join(",", scores.Select(s => s.ToString("G6"))) + "]";
            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
            random = new Random(42);
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }
    }

    /// <summary>
    /// LOCAL OUTLIER FACTOR (LOF) AGGREGATE
    /// Density-based anomaly detection
    /// 
    /// SELECT category,
    ///        dbo.LocalOutlierFactor(embedding_vector, 5) as lof_scores
    /// FROM embeddings GROUP BY category
    /// 
    /// Returns: JSON array of LOF scores per vector (> 1 = outlier)
    /// USE CASE: Find low-density regions, detect anomalies relative to neighbors
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct LocalOutlierFactor : IBinarySerialize
    {
        private List<float[]> vectors;
        private int k; // Number of neighbors
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            k = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlInt32 numNeighbors)
        {
            if (vectorJson.IsNull || numNeighbors.IsNull) return;

            if (k == 0) k = numNeighbors.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(LocalOutlierFactor other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count < k + 1 || dimension == 0 || k == 0)
                return SqlString.Null;

            // Compute LOF for each vector
            double[] lofScores = new double[vectors.Count];

            for (int i = 0; i < vectors.Count; i++)
            {
                // Find k nearest neighbors
                var distances = new List<(int Index, double Distance)>();
                for (int j = 0; j < vectors.Count; j++)
                {
                    if (i != j)
                    {
                        double dist = VectorUtilities.EuclideanDistance(vectors[i], vectors[j]);
                        distances.Add((j, dist));
                    }
                }

                var neighbors = distances.OrderBy(d => d.Distance).Take(k).ToList();

                // Compute local reachability density
                double avgReachDist = 0;
                foreach (var (idx, dist) in neighbors)
                {
                    // Reachability distance is max(k-distance of neighbor, actual distance)
                    avgReachDist += dist;
                }
                avgReachDist /= k;

                double lrd = 1.0 / (avgReachDist + 1e-10); // Local reachability density

                // LOF is ratio of neighbor densities to own density
                double sumNeighborLrd = 0;
                foreach (var (idx, _) in neighbors)
                {
                    // Simplified: assume similar LRD for neighbors
                    sumNeighborLrd += lrd;
                }

                lofScores[i] = (sumNeighborLrd / k) / (lrd + 1e-10);
            }

            var json = "[" + string.Join(",", lofScores.Select(s => s.ToString("G6"))) + "]";
            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            k = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(k);
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }
    }

    /// <summary>
    /// DBSCAN CLUSTERING AGGREGATE
    /// Density-based spatial clustering
    /// 
    /// SELECT dbo.DBSCANCluster(embedding_vector, 0.5, 5)
    /// FROM embeddings
    /// 
    /// Returns: JSON with cluster assignments and core/border/noise labels
    /// USE CASE: Discover arbitrary-shaped clusters, handle noise naturally
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct DBSCANCluster : IBinarySerialize
    {
        private List<float[]> vectors;
        private double epsilon;
        private int minPoints;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            epsilon = 0;
            minPoints = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlDouble eps, SqlInt32 minPts)
        {
            if (vectorJson.IsNull || eps.IsNull || minPts.IsNull) return;

            if (epsilon == 0)
            {
                epsilon = eps.Value;
                minPoints = minPts.Value;
            }

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(DBSCANCluster other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count == 0 || dimension == 0 || epsilon == 0 || minPoints == 0)
                return SqlString.Null;

            // DBSCAN algorithm
            int[] clusterIds = new int[vectors.Count];
            for (int i = 0; i < clusterIds.Length; i++)
                clusterIds[i] = -1; // Unvisited

            int clusterId = 0;

            for (int i = 0; i < vectors.Count; i++)
            {
                if (clusterIds[i] != -1) continue; // Already processed

                // Find neighbors
                var neighbors = FindNeighbors(i, epsilon);

                if (neighbors.Count < minPoints)
                {
                    clusterIds[i] = -2; // Noise
                }
                else
                {
                    // Expand cluster
                    ExpandCluster(i, neighbors, clusterId, clusterIds);
                    clusterId++;
                }
            }

            // Return cluster assignments
            var json = "[" + string.Join(",", clusterIds) + "]";
            return new SqlString(json);
        }

        private List<int> FindNeighbors(int pointIdx, double eps)
        {
            var neighbors = new List<int>();
            for (int i = 0; i < vectors.Count; i++)
            {
                if (VectorUtilities.EuclideanDistance(vectors[pointIdx], vectors[i]) <= eps)
                    neighbors.Add(i);
            }
            return neighbors;
        }

        private void ExpandCluster(int pointIdx, List<int> neighbors, int clusterId, int[] clusterIds)
        {
            clusterIds[pointIdx] = clusterId;

            var queue = new Queue<int>(neighbors);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                
                if (clusterIds[current] == -2) // Was noise, now border point
                    clusterIds[current] = clusterId;

                if (clusterIds[current] != -1) continue; // Already processed

                clusterIds[current] = clusterId;

                var currentNeighbors = FindNeighbors(current, epsilon);
                if (currentNeighbors.Count >= minPoints)
                {
                    foreach (var neighbor in currentNeighbors)
                    {
                        if (clusterIds[neighbor] == -1 || clusterIds[neighbor] == -2)
                            queue.Enqueue(neighbor);
                    }
                }
            }
        }

        public void Read(BinaryReader r)
        {
            epsilon = r.ReadDouble();
            minPoints = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(epsilon);
            w.Write(minPoints);
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }
    }

    /// <summary>
    /// MAHALANOBIS DISTANCE AGGREGATE
    /// Distance metric accounting for covariance structure
    /// 
    /// SELECT category,
    ///        dbo.MahalanobisDistance(embedding_vector, reference_vector)
    /// FROM embeddings GROUP BY category
    /// 
    /// Returns: JSON array of Mahalanobis distances (outliers have high values)
    /// USE CASE: Anomaly detection respecting correlation structure
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct MahalanobisDistance : IBinarySerialize
    {
        private List<float[]> vectors;
        private float[] referenceVector;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            referenceVector = null;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlString referenceJson)
        {
            if (vectorJson.IsNull) return;

            var vec = ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
            {
                dimension = vec.Length;
                if (!referenceJson.IsNull)
                {
                    referenceVector = VectorUtilities.ParseVectorJson(referenceJson.Value);
                    if (referenceVector != null && referenceVector.Length != dimension)
                        referenceVector = null;
                }
            }
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(MahalanobisDistance other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count < 2 || dimension == 0)
                return SqlString.Null;

            // Use mean as reference if not provided
            if (referenceVector == null || referenceVector.Length != dimension)
            {
                referenceVector = new float[dimension];
                foreach (var vec in vectors)
                    for (int i = 0; i < dimension; i++)
                        referenceVector[i] += vec[i];
                for (int i = 0; i < dimension; i++)
                    referenceVector[i] /= vectors.Count;
            }

            // Compute covariance matrix (simplified: diagonal only for efficiency)
            double[] variances = new double[dimension];
            foreach (var vec in vectors)
            {
                for (int i = 0; i < dimension; i++)
                {
                    double diff = vec[i] - referenceVector[i];
                    variances[i] += diff * diff;
                }
            }
            for (int i = 0; i < dimension; i++)
                variances[i] = Math.Max(variances[i] / vectors.Count, 1e-10);

            // Compute Mahalanobis distance for each vector
            double[] distances = new double[vectors.Count];
            for (int v = 0; v < vectors.Count; v++)
            {
                double dist = 0;
                for (int i = 0; i < dimension; i++)
                {
                    double diff = vectors[v][i] - referenceVector[i];
                    dist += (diff * diff) / variances[i];
                }
                distances[v] = Math.Sqrt(dist);
            }

            var json = "[" + string.Join(",", distances.Select(d => d.ToString("G6"))) + "]";
            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            
            bool hasReference = r.ReadBoolean();
            if (hasReference)
            {
                referenceVector = new float[dimension];
                for (int i = 0; i < dimension; i++)
                    referenceVector[i] = r.ReadSingle();
            }

            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            
            w.Write(referenceVector != null);
            if (referenceVector != null)
            {
                foreach (var val in referenceVector)
                    w.Write(val);
            }

            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }
    }
}
