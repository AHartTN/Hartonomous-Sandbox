using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;
using Hartonomous.Clr.MachineLearning;

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

            // Use extracted pattern discovery algorithm
            var patterns = TimeSeriesForecasting.DiscoverPatterns(entries, patternLength, 0.8);

            if (patterns.Length == 0)
            {
                sequence.Clear(clearItems: true);
                return new SqlString("{\"patterns\":[]}");
            }

            // Format top 5 patterns as JSON
            int take = Math.Min(5, patterns.Length);
            var builder = new StringBuilder();
            builder.Append("{\"patterns\":[");
            for (int idx = 0; idx < take; idx++)
            {
                if (idx > 0)
                    builder.Append(',');

                var item = patterns[idx];
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
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
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
                w.WriteFloatArray(entries[i].Vector);
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

            // Use extracted AR forecasting algorithm
            var forecast = TimeSeriesForecasting.ARForecast(entries, order, dimension);

            sequence.Clear(clearItems: true);
            
            if (forecast == null)
                return SqlString.Null;

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
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
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
                w.WriteFloatArray(entries[i].Vector);
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

            // Use extracted DTW algorithm
            double distance = DTWAlgorithm.ComputeDistance(sequence1.ToArray(), sequence2.ToArray());

            var result = new SqlDouble(distance);

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
                    float[]? vec = r.ReadFloatArray();
                    if (vec != null)
                        sequence1.Add(vec);
                }
            }

            int count2 = r.ReadInt32();
            if (dimension > 0 && count2 > 0)
            {
                sequence2.Reserve(count2);
                for (int i = 0; i < count2; i++)
                {
                    float[]? vec = r.ReadFloatArray();
                    if (vec != null)
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
                w.WriteFloatArray(span1[i]);
            }

            var span2 = sequence2.ToArray();
            w.Write(span2.Length);
            for (int i = 0; i < span2.Length; i++)
            {
                w.WriteFloatArray(span2[i]);
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

            // Use extracted CUSUM algorithm for change point detection
            var vectors = new float[entries.Length][];
            for (int i = 0; i < entries.Length; i++)
                vectors[i] = entries[i].Vector;

            var changePoints = CUSUMDetector.DetectChangePointsMultivariate(vectors, threshold);

            // Return top change points
            if (changePoints.Count == 0)
            {
                sequence.Clear(clearItems: true);
                return new SqlString("{\"change_points\":[]}");
            }

            // Sort by score descending
            changePoints.Sort((a, b) => b.Score.CompareTo(a.Score));

            int take = Math.Min(5, changePoints.Count);
            var builder = new StringBuilder();
            JsonFormatter.BeginObject(builder);
            builder.Append("\"change_points\":");
            JsonFormatter.BeginArray(builder);
            
            for (int i = 0; i < take; i++)
            {
                if (i > 0) builder.Append(',');

                var cp = changePoints[i];
                JsonFormatter.BeginObject(builder);
                JsonFormatter.AppendProperty(builder, "index", cp.Index);
                JsonFormatter.AppendProperty(builder, "timestamp", entries[cp.Index].Timestamp.ToString("O"));
                JsonFormatter.AppendProperty(builder, "score", cp.Score);
                JsonFormatter.AppendProperty(builder, "mean_before", cp.MeanBefore);
                JsonFormatter.AppendProperty(builder, "mean_after", cp.MeanAfter, true);
                JsonFormatter.EndObject(builder);
            }
            
            JsonFormatter.EndArray(builder);
            JsonFormatter.EndObject(builder);

            sequence.Clear(clearItems: true);
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
                float[]? vec = r.ReadFloatArray();
                if (vec != null)
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
                w.WriteFloatArray(entries[i].Vector);
            }
        }
    }
}
