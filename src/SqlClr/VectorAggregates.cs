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
    /// User-Defined Aggregates for vector statistics operating on SQL Server 2025 VECTOR type
    /// VECTOR is stored as NVARCHAR(MAX) containing JSON arrays like "[1.0, 2.0, 3.0]"
    /// These aggregates compute statistics across collections of vectors
    /// </summary>

    /// <summary>
    /// Computes component-wise mean and variance across a collection of vectors using Welford's algorithm
    /// Reference: https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Welford's_online_algorithm
    /// 
    /// Example: 
    /// SELECT category, dbo.VectorMeanVariance(embedding_vector) 
    /// FROM embeddings GROUP BY category
    /// 
    /// Returns: JSON with mean vector, variance vector, and count
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct VectorMeanVariance : IBinarySerialize
    {
        private long count;
        private float[] mean;      // Component-wise running mean
        private float[] m2;        // Component-wise sum of squared deviations
        private int dimension;

        public void Init()
        {
            count = 0;
            mean = null;
            m2 = null;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson)
        {
            if (vectorJson.IsNull)
                return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null)
                return;

            if (dimension == 0)
            {
                dimension = vec.Length;
                mean = new float[dimension];
                m2 = new float[dimension];
            }
            else if (vec.Length != dimension)
                return; // Skip mismatched dimensions

            count++;
            for (int i = 0; i < dimension; i++)
            {
                float delta = vec[i] - mean[i];
                mean[i] += delta / count;
                float delta2 = vec[i] - mean[i];
                m2[i] += delta * delta2;
            }
        }

        public void Merge(VectorMeanVariance other)
        {
            if (other.count == 0 || other.dimension == 0)
                return;

            if (count == 0 || dimension == 0)
            {
                count = other.count;
                dimension = other.dimension;
                mean = other.mean;
                m2 = other.m2;
                return;
            }

            if (dimension != other.dimension)
                return;

            // Parallel Welford merge (Chan's algorithm) applied component-wise
            long newCount = count + other.count;
            for (int i = 0; i < dimension; i++)
            {
                float delta = other.mean[i] - mean[i];
                float newMean = mean[i] + delta * other.count / newCount;
                float newM2 = m2[i] + other.m2[i] + delta * delta * count * other.count / newCount;
                mean[i] = newMean;
                m2[i] = newM2;
            }
            count = newCount;
        }

        public SqlString Terminate()
        {
            if (count == 0 || dimension == 0)
                return SqlString.Null;

            float[] variance = new float[dimension];
            float[] stddev = new float[dimension];

            for (int i = 0; i < dimension; i++)
            {
                variance[i] = count > 1 ? m2[i] / (count - 1) : 0.0f;
                stddev[i] = (float)Math.Sqrt(variance[i]);
            }

            var result = new { mean, variance, stddev, count };
            var serializer = new Hartonomous.Sql.Bridge.JsonProcessing.JsonSerializerImpl();
            return new SqlString(serializer.Serialize(result));
        }

        public void Read(BinaryReader r)
        {
            count = r.ReadInt64();
            dimension = r.ReadInt32();
            if (dimension > 0)
            {
                mean = new float[dimension];
                m2 = new float[dimension];
                for (int i = 0; i < dimension; i++)
                {
                    mean[i] = r.ReadSingle();
                    m2[i] = r.ReadSingle();
                }
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(count);
            w.Write(dimension);
            if (dimension > 0)
            {
                for (int i = 0; i < dimension; i++)
                {
                    w.Write(mean[i]);
                    w.Write(m2[i]);
                }
            }
        }
    }

    /// <summary>
    /// Computes geometric median (L1 spatial median) of a collection of 3D vectors using Weiszfeld's algorithm
    /// Reference: https://en.wikipedia.org/wiki/Geometric_median#Computation
    /// Finds the point minimizing sum of Euclidean distances to all input vectors
    /// Used for robust spatial averaging resistant to outliers
    /// 
    /// Example:
    /// SELECT category, dbo.GeometricMedian(spatial_vector) 
    /// FROM spatial_embeddings GROUP BY category
    /// 
    /// Returns: POINT WKT like "POINT (1.23 4.56 7.89)"
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct GeometricMedian : IBinarySerialize
    {
        private List<float[]> vectors;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson)
        {
            if (vectorJson.IsNull)
                return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null)
                return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(GeometricMedian other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count == 0 || dimension == 0)
                return SqlString.Null;

            if (vectors.Count == 1)
            {
                if (dimension >= 3)
                    return new SqlString($"POINT ({vectors[0][0]:G9} {vectors[0][1]:G9} {vectors[0][2]:G9})");
                else if (dimension == 2)
                    return new SqlString($"POINT ({vectors[0][0]:G9} {vectors[0][1]:G9})");
                else
                {
                    var serializer = new Hartonomous.Sql.Bridge.JsonProcessing.JsonSerializerImpl();
                    return new SqlString(serializer.SerializeFloatArray(vectors[0]));
                }
            }

            // Initialize with component-wise median
            float[] estimate = new float[dimension];
            for (int d = 0; d < dimension; d++)
                estimate[d] = ComponentMedian(vectors, d);

            // Weiszfeld's iterative algorithm
            const int maxIterations = 100;
            const double tolerance = 1e-7;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                float[] weightedSum = new float[dimension];
                double sumWeights = 0;

                foreach (var vec in vectors)
                {
                    double dist = EuclideanDistance(vec, estimate);
                    if (dist < 1e-10)
                        continue;

                    double weight = 1.0 / dist;
                    for (int d = 0; d < dimension; d++)
                        weightedSum[d] += (float)(weight * vec[d]);
                    sumWeights += weight;
                }

                if (sumWeights < 1e-10)
                    break;

                float[] newEstimate = new float[dimension];
                for (int d = 0; d < dimension; d++)
                    newEstimate[d] = (float)(weightedSum[d] / sumWeights);

                double change = EuclideanDistance(estimate, newEstimate);
                estimate = newEstimate;

                if (change < tolerance)
                    break;
            }

            // Return appropriate format based on dimension
            if (dimension >= 3)
                return new SqlString($"POINT ({estimate[0]:G9} {estimate[1]:G9} {estimate[2]:G9})");
            else if (dimension == 2)
                return new SqlString($"POINT ({estimate[0]:G9} {estimate[1]:G9})");
            else
            {
                var serializer = new Hartonomous.Sql.Bridge.JsonProcessing.JsonSerializerImpl();
                return new SqlString(serializer.SerializeFloatArray(estimate));
            }
        }

        private float ComponentMedian(List<float[]> vecs, int componentIndex)
        {
            var values = vecs.Select(v => v[componentIndex]).OrderBy(x => x).ToList();
            int n = values.Count;
            if (n % 2 == 0)
                return (values[n / 2 - 1] + values[n / 2]) / 2.0f;
            else
                return values[n / 2];
        }

        private double EuclideanDistance(float[] a, float[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
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
    /// Component-wise streaming softmax computation using log-sum-exp trick
    /// Accepts SQL Server 2025 VECTOR type (NVARCHAR(MAX) containing JSON array)
    /// Reference: https://gregorygundersen.com/blog/2020/02/09/log-sum-exp/
    /// Prevents overflow/underflow when computing exp() of large/small values
    /// Used for attention mechanisms and probability distributions
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct StreamingSoftmax : IBinarySerialize
    {
        private int dimension;
        private float[] maxValues;  // Max per component
        private List<float[]> vectors;

        public void Init()
        {
            dimension = 0;
            maxValues = null;
            vectors = new List<float[]>();
        }

        public void Accumulate(SqlString vectorJson)
        {
            if (vectorJson.IsNull)
                return;

            float[] vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null || vec.Length == 0)
                return;

            if (dimension == 0)
            {
                dimension = vec.Length;
                maxValues = new float[dimension];
                for (int i = 0; i < dimension; i++)
                    maxValues[i] = float.NegativeInfinity;
            }

            if (vec.Length != dimension)
                throw new ArgumentException($"Vector dimension mismatch: expected {dimension}, got {vec.Length}");

            // Update max per component
            for (int i = 0; i < dimension; i++)
            {
                if (vec[i] > maxValues[i])
                    maxValues[i] = vec[i];
            }

            vectors.Add(vec);
        }

        public void Merge(StreamingSoftmax other)
        {
            if (other.vectors.Count == 0)
                return;

            if (dimension == 0)
            {
                dimension = other.dimension;
                maxValues = (float[])other.maxValues.Clone();
                vectors = new List<float[]>(other.vectors);
            }
            else
            {
                if (other.dimension != dimension)
                    throw new ArgumentException($"Dimension mismatch in merge: {dimension} vs {other.dimension}");

                // Update max per component
                for (int i = 0; i < dimension; i++)
                {
                    if (other.maxValues[i] > maxValues[i])
                        maxValues[i] = other.maxValues[i];
                }

                vectors.AddRange(other.vectors);
            }
        }

        public SqlString Terminate()
        {
            if (vectors.Count == 0 || dimension == 0)
                return SqlString.Null;

            // Component-wise log-sum-exp: log(sum(exp(x_i - max_i))) + max_i per component
            float[] logSumExp = new float[dimension];
            float[] sumExp = new float[dimension];
            
            // Pass 1: Compute sum(exp(x - max)) per component
            for (int i = 0; i < dimension; i++)
            {
                sumExp[i] = 0;
                foreach (var vec in vectors)
                {
                    sumExp[i] += (float)Math.Exp(vec[i] - maxValues[i]);
                }
                logSumExp[i] = (float)Math.Log(sumExp[i]) + maxValues[i];
            }

            // Pass 2: Compute average normalized probabilities per component
            float[] avgProbs = new float[dimension];
            for (int i = 0; i < dimension; i++)
            {
                double probSum = 0;
                foreach (var vec in vectors)
                {
                    probSum += Math.Exp(vec[i] - maxValues[i]) / sumExp[i];
                }
                avgProbs[i] = (float)(probSum / vectors.Count);
            }

            // Use bridge library for proper JSON serialization
            var result = new
            {
                log_sum_exp = logSumExp,
                avg_probabilities = avgProbs,
                count = vectors.Count
            };
            var serializer = new Hartonomous.Sql.Bridge.JsonProcessing.JsonSerializerImpl();
            return new SqlString(serializer.Serialize(result));
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            if (dimension > 0)
            {
                maxValues = new float[dimension];
                for (int i = 0; i < dimension; i++)
                    maxValues[i] = r.ReadSingle();
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
            if (dimension > 0)
            {
                foreach (var val in maxValues)
                    w.Write(val);
            }

            w.Write(vectors.Count);
            foreach (var vec in vectors)
            {
                foreach (var val in vec)
                    w.Write(val);
            }
        }
    }
}
