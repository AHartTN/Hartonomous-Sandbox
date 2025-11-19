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
    /// NEURAL NETWORK-INSPIRED AGGREGATES
    /// Bring deep learning concepts into SQL aggregation
    /// </summary>

    /// <summary>
    /// MULTI-HEAD ATTENTION AGGREGATE
    /// Computes attention-weighted average of vectors (like Transformer models)
    /// 
    /// SELECT category, 
    ///        dbo.VectorAttentionAggregate(query_vector, key_vector, value_vector, 4)
    /// FROM embeddings GROUP BY category
    /// 
    /// Returns: Attention-weighted centroid (4 heads)
    /// USE CASE: Context-aware embedding summarization, find most representative vectors
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct VectorAttentionAggregate : IBinarySerialize
    {
        private List<float[]> queries;
        private List<float[]> keys;
        private List<float[]> values;
        private int numHeads;
        private int dimension;

        public void Init()
        {
            queries = new List<float[]>();
            keys = new List<float[]>();
            values = new List<float[]>();
            numHeads = 0;
            dimension = 0;
        }

        /// <summary>
        /// In simplified form: query = key = value = embedding vector
        /// numHeads: Number of attention heads (typically 4, 8, or 16)
        /// </summary>
        public void Accumulate(SqlString queryJson, SqlString keyJson, SqlString valueJson, SqlInt32 heads)
        {
            if (queryJson.IsNull || keyJson.IsNull || valueJson.IsNull || heads.IsNull)
                return;

            if (numHeads == 0) numHeads = heads.Value;

            var q = VectorUtilities.ParseVectorJson(queryJson.Value);
            var k = VectorUtilities.ParseVectorJson(keyJson.Value);
            var v = VectorUtilities.ParseVectorJson(valueJson.Value);

            if (q == null || k == null || v == null)
                return;

            if (dimension == 0)
                dimension = q.Length;
            else if (q.Length != dimension || k.Length != dimension || v.Length != dimension)
                return;

            queries.Add(q);
            keys.Add(k);
            values.Add(v);
        }

        public void Merge(VectorAttentionAggregate other)
        {
            if (other.queries != null)
            {
                queries.AddRange(other.queries);
                keys.AddRange(other.keys);
                values.AddRange(other.values);
            }
        }

        public SqlString Terminate()
        {
            if (queries.Count == 0 || dimension == 0 || numHeads == 0)
                return SqlString.Null;

            // Proper multi-head scaled dot-product attention
            // Implements: Attention(Q,K,V) = softmax(QK^T / sqrt(d_k)) V
            int headDim = dimension / numHeads;
            float[] output = new float[dimension];

            for (int h = 0; h < numHeads; h++)
            {
                int startIdx = h * headDim;
                int endIdx = Math.Min((h + 1) * headDim, dimension);
                int actualHeadDim = endIdx - startIdx;
                
                // Scaling factor for dot products (prevents softmax saturation)
                double scale = Math.Sqrt(actualHeadDim);

                // Compute attention matrix: QK^T / sqrt(d_k)
                // Shape: [queries.Count x keys.Count]
                double[,] attentionMatrix = new double[queries.Count, keys.Count];
                
                for (int i = 0; i < queries.Count; i++)
                {
                    for (int j = 0; j < keys.Count; j++)
                    {
                        // Scaled dot-product: q_i · k_j / sqrt(d_k)
                        double dotProduct = DotProduct(queries[i], keys[j], startIdx, endIdx);
                        attentionMatrix[i, j] = dotProduct / scale;
                    }
                }

                // Apply softmax over keys for each query
                for (int i = 0; i < queries.Count; i++)
                {
                    // Find max for numerical stability
                    double maxScore = double.NegativeInfinity;
                    for (int j = 0; j < keys.Count; j++)
                    {
                        if (attentionMatrix[i, j] > maxScore)
                            maxScore = attentionMatrix[i, j];
                    }

                    // Compute exp and sum
                    double sumExp = 0;
                    for (int j = 0; j < keys.Count; j++)
                    {
                        attentionMatrix[i, j] = Math.Exp(attentionMatrix[i, j] - maxScore);
                        sumExp += attentionMatrix[i, j];
                    }

                    // Normalize
                    for (int j = 0; j < keys.Count; j++)
                    {
                        attentionMatrix[i, j] /= sumExp;
                    }
                }

                // Apply attention weights to values: output = attention_weights * V
                // For aggregate, average across all queries
                for (int d = startIdx; d < endIdx; d++)
                {
                    double headOutput = 0;
                    for (int i = 0; i < queries.Count; i++)
                    {
                        for (int j = 0; j < values.Count; j++)
                        {
                            headOutput += attentionMatrix[i, j] * values[j][d];
                        }
                    }
                    output[d] = (float)(headOutput / queries.Count);
                }
            }

            return new SqlString(JsonConvert.SerializeObject(output));
        }

        public void Read(BinaryReader r)
        {
            numHeads = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            queries = new List<float[]>(count);
            keys = new List<float[]>(count);
            values = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[]? q = r.ReadFloatArray();
                float[]? k = r.ReadFloatArray();
                float[]? v = r.ReadFloatArray();
                if (q != null && k != null && v != null)
                {
                    queries.Add(q);
                    keys.Add(k);
                    values.Add(v);
                }
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(numHeads);
            w.Write(dimension);
            w.Write(queries.Count);
            for (int i = 0; i < queries.Count; i++)
            {
                w.WriteFloatArray(queries[i]);
                w.WriteFloatArray(keys[i]);
                w.WriteFloatArray(values[i]);
            }
        }

        private static double DotProduct(float[] a, float[] b, int start, int end)
        {
            double sum = 0;
            for (int i = start; i < end && i < a.Length && i < b.Length; i++)
                sum += a[i] * b[i];
            return sum;
        }
    }

    /// <summary>
    /// AUTOENCODER COMPRESSION AGGREGATE
    /// Learn compressed representation during aggregation
    /// 
    /// SELECT category, 
    ///        dbo.AutoencoderCompression(embedding_vector, 128)
    /// FROM embeddings GROUP BY category
    /// 
    /// Returns: 128-dim compressed representation of the cluster
    /// USE CASE: Dimensionality reduction, lossy compression, feature extraction
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct AutoencoderCompression : IBinarySerialize
    {
        private List<float[]> vectors;
        private int targetDim;
        private int sourceDim;

        public void Init()
        {
            vectors = new List<float[]>();
            targetDim = 0;
            sourceDim = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlInt32 compressedDimension)
        {
            if (vectorJson.IsNull || compressedDimension.IsNull) return;

            if (targetDim == 0) targetDim = compressedDimension.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (sourceDim == 0)
                sourceDim = vec.Length;
            else if (vec.Length != sourceDim)
                return;

            vectors.Add(vec);
        }

        public void Merge(AutoencoderCompression other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count == 0 || sourceDim == 0 || targetDim == 0)
                return SqlString.Null;

            // Use bridge library for PROPER SVD-based compression
            // Replaces: Variance-picking "simplified PCA-like compression" (greedy approximation)
            
            var vectorArray = vectors.ToArray();
            
            // Run proper SVD compression
            var compressed = Hartonomous.Clr.MachineLearning.SVDCompression.Compress(
                vectorArray, 
                targetDim
            );

            // Average the compressed vectors (aggregate function behavior)
            float[] avgCompressed = new float[targetDim];
            for (int i = 0; i < compressed.Length; i++)
            {
                for (int j = 0; j < targetDim; j++)
                {
                    avgCompressed[j] += compressed[i][j];
                }
            }
            for (int j = 0; j < targetDim; j++)
            {
                avgCompressed[j] /= compressed.Length;
            }

            return new SqlString(JsonConvert.SerializeObject(avgCompressed));
        }

        public void Read(BinaryReader r)
        {
            targetDim = r.ReadInt32();
            sourceDim = r.ReadInt32();
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
            w.Write(targetDim);
            w.Write(sourceDim);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                w.WriteFloatArray(vec);
        }
    }

    /// <summary>
    /// GRADIENT STATISTICS AGGREGATE
    /// Track gradient statistics for online learning/fine-tuning
    /// 
    /// SELECT layer_id,
    ///        dbo.GradientStatistics(gradient_vector)
    /// FROM model_gradients GROUP BY layer_id
    /// 
    /// Returns: JSON with gradient norm, mean, variance, explosion/vanishing indicators
    /// USE CASE: Monitor neural network training, detect gradient problems
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct GradientStatistics : IBinarySerialize
    {
        private List<float[]> gradients;
        private int dimension;

        public void Init()
        {
            gradients = new List<float[]>();
            dimension = 0;
        }

        public void Accumulate(SqlString gradientJson)
        {
            if (gradientJson.IsNull) return;

            var grad = VectorUtilities.ParseVectorJson(gradientJson.Value);
            if (grad == null) return;

            if (dimension == 0)
                dimension = grad.Length;
            else if (grad.Length != dimension)
                return;

            gradients.Add(grad);
        }

        public void Merge(GradientStatistics other)
        {
            if (other.gradients != null)
                gradients.AddRange(other.gradients);
        }

        public SqlString Terminate()
        {
            if (gradients.Count == 0 || dimension == 0)
                return SqlString.Null;

            // Compute gradient statistics
            double[] norms = new double[gradients.Count];
            double totalNorm = 0;
            double maxNorm = 0;
            double minNorm = double.MaxValue;

            for (int i = 0; i < gradients.Count; i++)
            {
                double norm = 0;
                foreach (var g in gradients[i])
                    norm += g * g;
                norm = Math.Sqrt(norm);
                norms[i] = norm;
                totalNorm += norm;
                if (norm > maxNorm) maxNorm = norm;
                if (norm < minNorm) minNorm = norm;
            }

            double meanNorm = totalNorm / gradients.Count;

            // Variance
            double variance = 0;
            foreach (var norm in norms)
            {
                double diff = norm - meanNorm;
                variance += diff * diff;
            }
            variance /= gradients.Count;
            double stddev = Math.Sqrt(variance);

            // Detect problems
            bool vanishing = meanNorm < 1e-7;
            bool exploding = maxNorm > 100.0;

            var result = $"{{" +
                $"\"mean_norm\":{meanNorm:G9}," +
                $"\"max_norm\":{maxNorm:G9}," +
                $"\"min_norm\":{minNorm:G9}," +
                $"\"stddev_norm\":{stddev:G9}," +
                $"\"count\":{gradients.Count}," +
                $"\"vanishing\":{vanishing.ToString().ToLower()}," +
                $"\"exploding\":{exploding.ToString().ToLower()}" +
                $"}}";

            return new SqlString(result);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            gradients = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[]? grad = r.ReadFloatArray();
                if (grad != null)
                    gradients.Add(grad);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(gradients.Count);
            foreach (var grad in gradients)
                w.WriteFloatArray(grad);
        }
    }

    /// <summary>
    /// COSINE ANNEALING SCHEDULE AGGREGATE
    /// Compute learning rate schedule for optimization
    /// 
    /// SELECT epoch, dbo.CosineAnnealingSchedule(epoch, 100, 0.001, 0.00001)
    /// FROM training_metrics GROUP BY epoch
    /// 
    /// Returns: Optimal learning rate for this epoch
    /// USE CASE: Database-native training orchestration
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = true,
        IsInvariantToOrder = true,
        MaxByteSize = 8000)]
    public struct CosineAnnealingSchedule : IBinarySerialize
    {
        private int maxEpoch;
        private double minEpoch;
        private double maxLR;
        private double minLR;
        private int count;

        public void Init()
        {
            maxEpoch = 0;
            minEpoch = double.MaxValue;
            maxLR = 0;
            minLR = 0;
            count = 0;
        }

        public void Accumulate(SqlInt32 epoch, SqlInt32 totalEpochs, SqlDouble maxLearningRate, SqlDouble minLearningRate)
        {
            if (epoch.IsNull || totalEpochs.IsNull || maxLearningRate.IsNull || minLearningRate.IsNull)
                return;

            if (count == 0)
            {
                maxLR = maxLearningRate.Value;
                minLR = minLearningRate.Value;
                maxEpoch = totalEpochs.Value;
            }

            if (epoch.Value < minEpoch)
                minEpoch = epoch.Value;

            count++;
        }

        public void Merge(CosineAnnealingSchedule other)
        {
            if (other.minEpoch < minEpoch)
                minEpoch = other.minEpoch;
            count += other.count;
        }

        public SqlDouble Terminate()
        {
            if (count == 0 || maxEpoch == 0)
                return SqlDouble.Null;

            // Cosine annealing formula
            double progress = minEpoch / maxEpoch;
            double lr = minLR + (maxLR - minLR) * 0.5 * (1 + Math.Cos(Math.PI * progress));

            return new SqlDouble(lr);
        }

        public void Read(BinaryReader r)
        {
            maxEpoch = r.ReadInt32();
            minEpoch = r.ReadDouble();
            maxLR = r.ReadDouble();
            minLR = r.ReadDouble();
            count = r.ReadInt32();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(maxEpoch);
            w.Write(minEpoch);
            w.Write(maxLR);
            w.Write(minLR);
            w.Write(count);
        }
    }
}
