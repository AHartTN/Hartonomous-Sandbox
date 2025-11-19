using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;
using Hartonomous.Clr.MachineLearning;

namespace Hartonomous.Clr
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

            // Use extracted IsolationForest algorithm
            var scores = IsolationForest.ComputeAnomalyScores(
                vectors.ToArray(),
                numTrees: Math.Min(10, vectors.Count / 2),
                randomSeed: 42
            );

            return new SqlString(JsonConvert.SerializeObject(scores));
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
                    vectors.Add(vec);
            }
            random = new Random(42);
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                w.WriteFloatArray(vec);
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

            // Use extracted LOF algorithm with Euclidean metric (default for aggregate)
            var lofScores = MachineLearning.LocalOutlierFactor.Compute(
                vectors.ToArray(),
                k,
                new Core.EuclideanDistance());

            var scores = lofScores.Select(s => (float)s).ToArray();
            return new SqlString(JsonConvert.SerializeObject(scores));
        }

        public void Read(BinaryReader r)
        {
            k = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
                    vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(k);
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                w.WriteFloatArray(vec);
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

            // Use extracted DBSCAN algorithm
            int[] clusterIds = DBSCANClustering.Cluster(
                vectors.ToArray(),
                epsilon,
                minPoints
            );

            // Return cluster assignments
            var json = "[" + string.Join(",", clusterIds) + "]";
            return new SqlString(json);
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
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
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
                w.WriteFloatArray(vec);
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
        private float[]? referenceVector;
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

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
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

            // Use bridge library for PROPER Mahalanobis distance
            // Replaces: Diagonal-only covariance (mathematically incorrect)
            
            // Compute FULL covariance matrix (not diagonal approximation)
            var vectorArray = vectors.ToArray();
            var covariance = Hartonomous.Clr.MachineLearning.MahalanobisDistance.ComputeCovarianceMatrix(vectorArray);

            // Compute Mahalanobis distance for each vector using full covariance
            double[] distances = new double[vectors.Count];
            for (int v = 0; v < vectors.Count; v++)
            {
                distances[v] = Hartonomous.Clr.MachineLearning.MahalanobisDistance.Compute(
                    vectors[v], 
                    referenceVector, 
                    covariance
                );
            }

            var json = JsonConvert.SerializeObject(distances.Select(d => (float)d).ToArray());
            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            
            bool hasReference = r.ReadBoolean();
            if (hasReference)
            {
                referenceVector = r.ReadFloatArray();
            }

            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
                    vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            
            w.Write(referenceVector != null);
            if (referenceVector != null)
            {
                w.WriteFloatArray(referenceVector);
            }

            w.Write(vectors.Count);
            foreach (var vec in vectors)
                w.WriteFloatArray(vec);
        }
    }
}
