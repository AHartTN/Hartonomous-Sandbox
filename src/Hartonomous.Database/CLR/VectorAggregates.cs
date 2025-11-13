using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr
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

            var result = new
            {
                mean,
                variance,
                stddev,
                count
            };
            return new SqlString(JsonConvert.SerializeObject(result));
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
        private PooledList<float[]> vectors;
        private int dimension;

        public void Init()
        {
            vectors = default;
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
            if (other.dimension == 0 || other.vectors.Count == 0)
                return;

            if (dimension == 0)
                dimension = other.dimension;

            if (dimension != other.dimension)
                return;

            vectors.AddRange(other.vectors.ToArray());
        }

        public SqlString Terminate()
        {
            var vectorArray = vectors.ToArray();
            if (vectorArray.Length == 0 || dimension == 0)
                return SqlString.Null;

            if (vectorArray.Length == 1)
            {
                var single = vectorArray[0];
                var result = dimension >= 3
                    ? new SqlString($"POINT ({single[0]:G9} {single[1]:G9} {single[2]:G9})")
                    : dimension == 2
                        ? new SqlString($"POINT ({single[0]:G9} {single[1]:G9})")
                        : new SqlString(JsonConvert.SerializeObject(single));

                vectors.Clear(clearItems: true);
                return result;
            }

            // Initialize with component-wise median
            float[] estimate = new float[dimension];
            for (int d = 0; d < dimension; d++)
                estimate[d] = ComponentMedian(vectorArray, d);

            // Weiszfeld's iterative algorithm
            const int maxIterations = 100;
            const double tolerance = 1e-7;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                float[] weightedSum = new float[dimension];
                double sumWeights = 0;

                for (int idx = 0; idx < vectorArray.Length; idx++)
                {
                    var vec = vectorArray[idx];
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
            var terminated = dimension >= 3
                ? new SqlString($"POINT ({estimate[0]:G9} {estimate[1]:G9} {estimate[2]:G9})")
                : dimension == 2
                    ? new SqlString($"POINT ({estimate[0]:G9} {estimate[1]:G9})")
                    : new SqlString(JsonConvert.SerializeObject(estimate));

            vectors.Clear(clearItems: true);
            return terminated;
        }

        private float ComponentMedian(float[][] vecs, int componentIndex)
        {
            int length = vecs.Length;
            if (length == 0)
                return 0f;

            float[] scratch = new float[length];
            for (int i = 0; i < length; i++)
                scratch[i] = vecs[i][componentIndex];

            Array.Sort(scratch, 0, length);
            float median = (length & 1) == 0
                ? (scratch[length / 2 - 1] + scratch[length / 2]) * 0.5f
                : scratch[length / 2];

            return median;
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
            vectors.Clear(clearItems: true);

            dimension = r.ReadInt32();
            int count = r.ReadInt32();

            if (dimension <= 0 || count <= 0)
                return;

            vectors.Reserve(count);
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
            var arr = vectors.ToArray();
            w.Write(dimension);
            w.Write(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                var vec = arr[i];
                for (int j = 0; j < dimension; j++)
                    w.Write(vec[j]);
            }
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
        private double[] sumExp;    // Sum of exp(x - max)
        private int count;

        public void Init()
        {
            dimension = 0;
            maxValues = null;
            sumExp = null;
            count = 0;
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
                sumExp = new double[dimension];
                for (int i = 0; i < dimension; i++)
                {
                    maxValues[i] = vec[i];
                    sumExp[i] = 1.0d;
                }
                count = 1;
                return;
            }

            if (vec.Length != dimension)
                throw new ArgumentException($"Vector dimension mismatch: expected {dimension}, got {vec.Length}");

            for (int i = 0; i < dimension; i++)
            {
                float currentMax = maxValues[i];
                float value = vec[i];

                if (value > currentMax)
                {
                    double scale = Math.Exp(currentMax - value);
                    sumExp[i] = sumExp[i] * scale + 1.0d;
                    maxValues[i] = value;
                }
                else
                {
                    sumExp[i] += Math.Exp(value - currentMax);
                }
            }

            count++;
        }

        public void Merge(StreamingSoftmax other)
        {
            if (other.dimension == 0 || other.count == 0)
                return;

            if (dimension == 0)
            {
                dimension = other.dimension;
                maxValues = (float[])other.maxValues.Clone();
                sumExp = (double[])other.sumExp.Clone();
                count = other.count;
                return;
            }

            if (other.dimension != dimension)
                throw new ArgumentException($"Dimension mismatch in merge: {dimension} vs {other.dimension}");

            for (int i = 0; i < dimension; i++)
            {
                float currentMax = maxValues[i];
                float otherMax = other.maxValues[i];
                float finalMax = currentMax >= otherMax ? currentMax : otherMax;

                double adjustedCurrent = sumExp[i] * Math.Exp(currentMax - finalMax);
                double adjustedOther = other.sumExp[i] * Math.Exp(otherMax - finalMax);

                sumExp[i] = adjustedCurrent + adjustedOther;
                maxValues[i] = finalMax;
            }

            count += other.count;
        }

        public SqlString Terminate()
        {
            if (dimension == 0 || count == 0 || maxValues == null || sumExp == null)
                return SqlString.Null;

            float[] logSumExp = new float[dimension];
            float[] avgProbs = new float[dimension];

            for (int i = 0; i < dimension; i++)
            {
                double componentSum = sumExp[i];
                if (componentSum <= 0)
                {
                    logSumExp[i] = float.NegativeInfinity;
                    avgProbs[i] = 0f;
                    continue;
                }

                logSumExp[i] = (float)(Math.Log(componentSum) + maxValues[i]);
                avgProbs[i] = count > 0 ? (float)(1.0d / count) : 0f;
            }

            var result = new
            {
                log_sum_exp = logSumExp,
                avg_probabilities = avgProbs,
                count
            };

            // reset state to allow reuse in pooled scenarios
            maxValues = null;
            sumExp = null;
            count = 0;
            dimension = 0;

            return new SqlString(JsonConvert.SerializeObject(result));
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            count = r.ReadInt32();

            if (dimension <= 0 || count <= 0)
            {
                maxValues = null;
                sumExp = null;
                return;
            }

            maxValues = new float[dimension];
            sumExp = new double[dimension];

            for (int i = 0; i < dimension; i++)
                maxValues[i] = r.ReadSingle();

            for (int i = 0; i < dimension; i++)
                sumExp[i] = r.ReadDouble();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(count);

            if (dimension <= 0 || maxValues == null || sumExp == null)
                return;

            for (int i = 0; i < dimension; i++)
                w.Write(maxValues[i]);

            for (int i = 0; i < dimension; i++)
                w.Write(sumExp[i]);
        }
    }
}
