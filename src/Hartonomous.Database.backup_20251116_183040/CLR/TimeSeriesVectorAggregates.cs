using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr
{
    /// <summary>
    /// TIME SERIES AND SEQUENCE ANALYSIS AGGREGATES
    /// Analyze temporal patterns in embeddings
    /// </summary>

    /// <summary>
    /// VECTOR SEQUENCE PATTERN AGGREGATE
    /// Find repeated patterns in vector sequences (like motif discovery)
    /// 
    /// SELECT session_id,
    ///        dbo.VectorSequencePatterns(timestamp, embedding_vector, 3)
    /// FROM user_embeddings GROUP BY session_id
    /// 
    /// Returns: JSON with discovered patterns (subsequences that repeat)
    /// USE CASE: Find behavioral patterns, detect recurring semantic themes
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,  // Order matters for sequences!
        MaxByteSize = -1)]
    public struct VectorSequencePatterns : IBinarySerialize
    {
        private PooledList<TimestampedVector> sequence;
        private int patternLength;
        private int dimension;

        private struct PatternResult
        {
            public int StartIndex;
            public double AvgSimilarity;
            public int Occurrences;
        }

        public void Init()
        {
            sequence = default;
            patternLength = 0;
            dimension = 0;
        }

        public void Accumulate(SqlDateTime timestamp, SqlString vectorJson, SqlInt32 windowSize)
        {
            if (timestamp.IsNull || vectorJson.IsNull || windowSize.IsNull)
                return;

            if (patternLength == 0)
                patternLength = Math.Max(1, windowSize.Value);

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null)
                return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            sequence.Add(new TimestampedVector(timestamp.Value, vec));
        }

        public void Merge(VectorSequencePatterns other)
        {
            if (other.sequence.Count == 0)
                return;

            if (dimension == 0)
                dimension = other.dimension;

            if (dimension != other.dimension)
                return;

            if (patternLength == 0)
                patternLength = other.patternLength;

            sequence.AddRange(other.sequence.ToArray());
        }

        public SqlString Terminate()
        {
            if (sequence.Count < patternLength * 2 || dimension == 0)
            {
                sequence.Clear(clearItems: true);
                return SqlString.Null;
            }

            sequence.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            var entries = sequence.ToArray();
            int total = entries.Length;

            PooledList<PatternResult> patterns = default;

            for (int i = 0; i <= total - patternLength; i++)
            {
                int occurrences = 0;
                double totalSimilarity = 0;

                for (int j = i + patternLength; j <= total - patternLength; j++)
                {
                    double windowSim = 0;
                    for (int k = 0; k < patternLength; k++)
                    {
                        windowSim += VectorUtilities.CosineSimilarity(
                            entries[i + k].Vector,
                            entries[j + k].Vector);
                    }
                    windowSim /= patternLength;

                    if (windowSim > 0.8)
                    {
                        occurrences++;
                        totalSimilarity += windowSim;
                    }
                }

                if (occurrences > 0)
                {
                    patterns.Add(new PatternResult
                    {
                        StartIndex = i,
                        AvgSimilarity = totalSimilarity / occurrences,
                        Occurrences = occurrences + 1
                    });
                }
            }

            var patternSpan = patterns.ToArray();
            if (patternSpan.Length == 0)
            {
                sequence.Clear(clearItems: true);
                patterns.Clear();
                return new SqlString("{\"patterns\":[]}");
            }

            PatternResult[] ordered = patternSpan;
            Array.Sort(ordered, (a, b) =>
            {
                int cmp = b.Occurrences.CompareTo(a.Occurrences);
                if (cmp != 0)
                    return cmp;
                return b.AvgSimilarity.CompareTo(a.AvgSimilarity);
            });

            int take = Math.Min(5, ordered.Length);
            var builder = new StringBuilder();
            builder.Append("{\"patterns\":[");
            for (int idx = 0; idx < take; idx++)
            {
                if (idx > 0)
                    builder.Append(',');

                var item = ordered[idx];
                builder.Append("{\"start_index\":");
                builder.Append(item.StartIndex);
                builder.Append(",\"similarity\":");
                builder.Append(item.AvgSimilarity.ToString("G6", CultureInfo.InvariantCulture));
                builder.Append(",\"occurrences\":");
                builder.Append(item.Occurrences);
                builder.Append('}');
            }
            builder.Append("]}");

            sequence.Clear(clearItems: true);
            patterns.Clear();
            return new SqlString(builder.ToString());
        }

        public void Read(BinaryReader r)
        {
            sequence.Clear(clearItems: true);

            patternLength = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();

            if (dimension <= 0 || count <= 0)
                return;

            sequence.Reserve(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence.Add(new TimestampedVector(timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            var entries = sequence.ToArray();
            w.Write(patternLength);
            w.Write(dimension);
            w.Write(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                w.Write(entries[i].Timestamp.ToBinary());
                var vec = entries[i].Vector;
                for (int j = 0; j < dimension; j++)
                    w.Write(vec[j]);
            }
        }
    }

    /// <summary>
    /// AUTOREGRESSIVE FORECAST AGGREGATE
    /// Simple AR model for vector time series
    /// 
    /// SELECT dbo.VectorARForecast(timestamp, embedding_vector, 3)
    /// FROM time_series_embeddings
    /// 
    /// Returns: Next predicted vector based on last N observations
    /// USE CASE: Predict next embedding in sequence, anticipate semantic drift
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct VectorARForecast : IBinarySerialize
    {
        private PooledList<TimestampedVector> sequence;
        private int order;
        private int dimension;

        public void Init()
        {
            sequence = default;
            order = 0;
            dimension = 0;
        }

        public void Accumulate(SqlDateTime timestamp, SqlString vectorJson, SqlInt32 arOrder)
        {
            if (timestamp.IsNull || vectorJson.IsNull || arOrder.IsNull)
                return;

            if (order == 0)
                order = arOrder.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            sequence.Add(new TimestampedVector(timestamp.Value, vec));
        }

        public void Merge(VectorARForecast other)
        {
            if (other.sequence.Count == 0)
                return;

            if (dimension == 0)
                dimension = other.dimension;

            if (dimension != other.dimension)
                return;

            if (order == 0)
                order = other.order;

            sequence.AddRange(other.sequence.ToArray());
        }

        public SqlString Terminate()
        {
            if (sequence.Count <= order || dimension == 0 || order == 0)
            {
                sequence.Clear(clearItems: true);
                return SqlString.Null;
            }

            // Sort by timestamp
            sequence.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            var entries = sequence.ToArray();

            // Simple AR model: weighted average of last N vectors
            float[] forecast = new float[dimension];
            double[] weights = new double[order];
            double weightSum = 0;

            // Exponential weights (recent = more important)
            for (int i = 0; i < order; i++)
            {
                weights[i] = Math.Exp(i); // More recent = higher weight
                weightSum += weights[i];
            }
            for (int i = 0; i < order; i++)
                weights[i] /= weightSum;

            // Compute forecast
            for (int i = 0; i < order; i++)
            {
                var vec = entries[entries.Length - order + i].Vector;
                for (int d = 0; d < dimension; d++)
                {
                    forecast[d] += (float)(vec[d] * weights[i]);
                }
            }

            sequence.Clear(clearItems: true);
            return new SqlString(JsonConvert.SerializeObject(forecast));
        }

        public void Read(BinaryReader r)
        {
            order = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            sequence.Clear(clearItems: true);
            if (dimension <= 0 || count <= 0)
                return;

            sequence.Reserve(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence.Add(new TimestampedVector(timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(order);
            w.Write(dimension);
            var entries = sequence.ToArray();
            w.Write(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                w.Write(entries[i].Timestamp.ToBinary());
                var vec = entries[i].Vector;
                for (int j = 0; j < dimension; j++)
                    w.Write(vec[j]);
            }
        }
    }

    /// <summary>
    /// DYNAMIC TIME WARPING AGGREGATE
    /// Find optimal alignment between vector sequences
    /// 
    /// SELECT user1_id, user2_id,
    ///        dbo.DTWDistance(seq1_vector, seq2_vector)
    /// FROM user_behavior_sequences
    /// GROUP BY user1_id, user2_id
    /// 
    /// Returns: DTW distance (lower = more similar sequences)
    /// USE CASE: Compare user journeys, find similar behavioral patterns regardless of speed
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct DTWDistance : IBinarySerialize
    {
        private PooledList<float[]> sequence1;
        private PooledList<float[]> sequence2;
        private int dimension;

        public void Init()
        {
            sequence1 = default;
            sequence2 = default;
            dimension = 0;
        }

        /// <summary>
        /// Accumulate alternates between seq1 and seq2 based on row order
        /// Better: use two separate columns and parse based on a flag parameter
        /// </summary>
        public void Accumulate(SqlString seq1VectorJson, SqlString seq2VectorJson)
        {
            if (!seq1VectorJson.IsNull)
            {
                var vec = VectorUtilities.ParseVectorJson(seq1VectorJson.Value);
                if (vec != null)
                {
                    if (dimension == 0)
                        dimension = vec.Length;
                    else if (vec.Length != dimension)
                        return;
                    sequence1.Add(vec);
                }
            }

            if (!seq2VectorJson.IsNull)
            {
                var vec = VectorUtilities.ParseVectorJson(seq2VectorJson.Value);
                if (vec != null && vec.Length == dimension)
                {
                    sequence2.Add(vec);
                }
            }
        }

        public void Merge(DTWDistance other)
        {
            if (other.sequence1.Count > 0)
                sequence1.AddRange(other.sequence1.ToArray());
            if (other.sequence2.Count > 0)
                sequence2.AddRange(other.sequence2.ToArray());
        }

        public SqlDouble Terminate()
        {
            if (sequence1.Count == 0 || sequence2.Count == 0 || dimension == 0)
                return SqlDouble.Null;

            // DTW algorithm
            int n = sequence1.Count;
            int m = sequence2.Count;

            // Initialize cost matrix (use smaller window for efficiency)
            double[,] dtw = new double[n + 1, m + 1];
            
            for (int i = 0; i <= n; i++)
                dtw[i, 0] = double.PositiveInfinity;
            for (int j = 0; j <= m; j++)
                dtw[0, j] = double.PositiveInfinity;
            dtw[0, 0] = 0;

            // Fill matrix
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    double cost = VectorUtilities.EuclideanDistance(sequence1[i - 1], sequence2[j - 1]);
                    dtw[i, j] = cost + Math.Min(Math.Min(dtw[i - 1, j], dtw[i, j - 1]), dtw[i - 1, j - 1]);
                }
            }

            var result = new SqlDouble(dtw[n, m]);

            sequence1.Clear(clearItems: true);
            sequence2.Clear(clearItems: true);
            return result;
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            sequence1.Clear(clearItems: true);
            sequence2.Clear(clearItems: true);

            int count1 = r.ReadInt32();
            if (dimension > 0 && count1 > 0)
            {
                sequence1.Reserve(count1);
                for (int i = 0; i < count1; i++)
                {
                    float[] vec = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        vec[j] = r.ReadSingle();
                    sequence1.Add(vec);
                }
            }

            int count2 = r.ReadInt32();
            if (dimension > 0 && count2 > 0)
            {
                sequence2.Reserve(count2);
                for (int i = 0; i < count2; i++)
                {
                    float[] vec = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        vec[j] = r.ReadSingle();
                    sequence2.Add(vec);
                }
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            var span1 = sequence1.ToArray();
            w.Write(span1.Length);
            for (int i = 0; i < span1.Length; i++)
            {
                var vec = span1[i];
                for (int j = 0; j < dimension; j++)
                    w.Write(vec[j]);
            }

            var span2 = sequence2.ToArray();
            w.Write(span2.Length);
            for (int i = 0; i < span2.Length; i++)
            {
                var vec = span2[i];
                for (int j = 0; j < dimension; j++)
                    w.Write(vec[j]);
            }
        }
    }

    /// <summary>
    /// CHANGE POINT DETECTION AGGREGATE
    /// Detect when vector distribution shifts significantly
    /// 
    /// SELECT DATEPART(hour, timestamp) as hour,
    ///        dbo.ChangePointDetection(timestamp, embedding_vector, 0.05)
    /// FROM stream_embeddings
    /// GROUP BY DATEPART(hour, timestamp)
    /// 
    /// Returns: JSON with detected change points and confidence scores
    /// USE CASE: Detect distribution shifts, concept drift, regime changes
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct ChangePointDetection : IBinarySerialize
    {
        private PooledList<TimestampedVector> sequence;
        private double threshold;
        private int dimension;

        private struct ChangePointRecord
        {
            public int Index;
            public DateTime Timestamp;
            public double Score;
        }

        public void Init()
        {
            sequence = default;
            threshold = 0;
            dimension = 0;
        }

        public void Accumulate(SqlDateTime timestamp, SqlString vectorJson, SqlDouble significanceThreshold)
        {
            if (timestamp.IsNull || vectorJson.IsNull || significanceThreshold.IsNull)
                return;

            if (threshold == 0)
                threshold = significanceThreshold.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            sequence.Add(new TimestampedVector(timestamp.Value, vec));
        }

        public void Merge(ChangePointDetection other)
        {
            if (other.sequence.Count == 0)
                return;

            if (dimension == 0)
                dimension = other.dimension;

            if (dimension != other.dimension)
                return;

            if (threshold == 0)
                threshold = other.threshold;

            sequence.AddRange(other.sequence.ToArray());
        }

        public SqlString Terminate()
        {
            if (sequence.Count < 10 || dimension == 0)
            {
                sequence.Clear(clearItems: true);
                return SqlString.Null;
            }

            // Sort by timestamp
            sequence.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            var entries = sequence.ToArray();

            // CUSUM algorithm for change point detection
            PooledList<ChangePointRecord> changePoints = default;

            // Compute cumulative deviation from mean
            float[] overallMean = new float[dimension];
            for (int idx = 0; idx < entries.Length; idx++)
            {
                var vec = entries[idx].Vector;
                for (int i = 0; i < dimension; i++)
                    overallMean[i] += vec[i];
            }
            for (int i = 0; i < dimension; i++)
                overallMean[i] /= entries.Length;

            // Sliding window approach
            int windowSize = Math.Max(10, entries.Length / 10);
            for (int i = windowSize; i < entries.Length - windowSize; i++)
            {
                // Compute means before and after
                float[] meanBefore = new float[dimension];
                float[] meanAfter = new float[dimension];

                for (int j = i - windowSize; j < i; j++)
                    for (int d = 0; d < dimension; d++)
                        meanBefore[d] += entries[j].Vector[d];
                
                for (int j = i; j < i + windowSize; j++)
                    for (int d = 0; d < dimension; d++)
                        meanAfter[d] += entries[j].Vector[d];

                for (int d = 0; d < dimension; d++)
                {
                    meanBefore[d] /= windowSize;
                    meanAfter[d] /= windowSize;
                }

                // Compute difference magnitude
                double diff = 0;
                for (int d = 0; d < dimension; d++)
                {
                    double delta = meanAfter[d] - meanBefore[d];
                    diff += delta * delta;
                }
                diff = Math.Sqrt(diff);

                if (diff > threshold)
                {
                    changePoints.Add(new ChangePointRecord
                    {
                        Index = i,
                        Timestamp = entries[i].Timestamp,
                        Score = diff
                    });
                }
            }

            // Return top change points
            var changeSpan = changePoints.ToArray();
            if (changeSpan.Length == 0)
            {
                sequence.Clear(clearItems: true);
                changePoints.Clear();
                return new SqlString("{\"change_points\":[]}");
            }

            ChangePointRecord[] ordered = changeSpan;
            Array.Sort(ordered, (a, b) => b.Score.CompareTo(a.Score));

            int take = Math.Min(5, ordered.Length);
            var builder = new StringBuilder();
            builder.Append("{\"change_points\":[");
            for (int i = 0; i < take; i++)
            {
                if (i > 0)
                    builder.Append(',');

                var cp = ordered[i];
                builder.Append("{\"index\":");
                builder.Append(cp.Index);
                builder.Append(",\"timestamp\":\"");
                builder.Append(cp.Timestamp.ToString("O"));
                builder.Append("\",\"score\":");
                builder.Append(cp.Score.ToString("G6", CultureInfo.InvariantCulture));
                builder.Append('}');
            }
            builder.Append("]}");

            sequence.Clear(clearItems: true);
            changePoints.Clear();
            return new SqlString(builder.ToString());
        }

        public void Read(BinaryReader r)
        {
            threshold = r.ReadDouble();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            sequence.Clear(clearItems: true);
            if (dimension <= 0 || count <= 0)
                return;

            sequence.Reserve(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence.Add(new TimestampedVector(timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(threshold);
            w.Write(dimension);
            var entries = sequence.ToArray();
            w.Write(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                w.Write(entries[i].Timestamp.ToBinary());
                var vec = entries[i].Vector;
                for (int j = 0; j < dimension; j++)
                    w.Write(vec[j]);
            }
        }
    }
}
