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
        private List<(DateTime Timestamp, float[] Vector)> sequence;
        private int patternLength;
        private int dimension;

        public void Init()
        {
            sequence = new List<(DateTime, float[])>();
            patternLength = 0;
            dimension = 0;
        }

        public void Accumulate(SqlDateTime timestamp, SqlString vectorJson, SqlInt32 windowSize)
        {
            if (timestamp.IsNull || vectorJson.IsNull || windowSize.IsNull)
                return;

            if (patternLength == 0)
                patternLength = windowSize.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            sequence.Add((timestamp.Value, vec));
        }

        public void Merge(VectorSequencePatterns other)
        {
            if (other.sequence != null)
                sequence.AddRange(other.sequence);
        }

        public SqlString Terminate()
        {
            if (sequence.Count < patternLength * 2 || dimension == 0)
                return SqlString.Null;

            // Sort by timestamp
            sequence.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Find repeated subsequences using simple distance-based matching
            var patterns = new List<(int StartIdx, double AvgSimilarity, int Occurrences)>();

            for (int i = 0; i <= sequence.Count - patternLength; i++)
            {
                // Extract pattern
                var pattern = sequence.Skip(i).Take(patternLength).Select(s => s.Vector).ToArray();

                // Look for similar subsequences
                int occurrences = 0;
                double totalSimilarity = 0;

                for (int j = i + patternLength; j <= sequence.Count - patternLength; j++)
                {
                    var candidate = sequence.Skip(j).Take(patternLength).Select(s => s.Vector).ToArray();
                    
                    // Compute average similarity across the window
                    double windowSim = 0;
                    for (int k = 0; k < patternLength; k++)
                    {
                        windowSim += VectorUtilities.CosineSimilarity(pattern[k], candidate[k]);
                    }
                    windowSim /= patternLength;

                    if (windowSim > 0.8) // High similarity threshold
                    {
                        occurrences++;
                        totalSimilarity += windowSim;
                    }
                }

                if (occurrences > 0)
                {
                    patterns.Add((i, totalSimilarity / occurrences, occurrences + 1));
                }
            }

            // Return top 5 patterns
            var topPatterns = patterns.OrderByDescending(p => p.Occurrences)
                .ThenByDescending(p => p.AvgSimilarity)
                .Take(5)
                .ToList();

            if (topPatterns.Count == 0)
                return new SqlString("{\"patterns\":[]}");

            var json = "{\"patterns\":[" +
                string.Join(",",
                    topPatterns.Select(p =>
                        $"{{\"start_index\":{p.StartIdx}," +
                        $"\"similarity\":{p.AvgSimilarity:G6}," +
                        $"\"occurrences\":{p.Occurrences}}}"
                    )
                ) + "]}";

            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            patternLength = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            sequence = new List<(DateTime, float[])>(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence.Add((timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(patternLength);
            w.Write(dimension);
            w.Write(sequence.Count);
            foreach (var (timestamp, vec) in sequence)
            {
                w.Write(timestamp.ToBinary());
                foreach (var val in vec)
                    w.Write(val);
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
        private List<(DateTime Timestamp, float[] Vector)> sequence;
        private int order;
        private int dimension;

        public void Init()
        {
            sequence = new List<(DateTime, float[])>();
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

            sequence.Add((timestamp.Value, vec));
        }

        public void Merge(VectorARForecast other)
        {
            if (other.sequence != null)
                sequence.AddRange(other.sequence);
        }

        public SqlString Terminate()
        {
            if (sequence.Count <= order || dimension == 0 || order == 0)
                return SqlString.Null;

            // Sort by timestamp
            sequence.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

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
                var vec = sequence[sequence.Count - order + i].Vector;
                for (int d = 0; d < dimension; d++)
                {
                    forecast[d] += (float)(vec[d] * weights[i]);
                }
            }

            return new SqlString("[" + string.Join(",", forecast.Select(v => v.ToString("G9"))) + "]");
        }

        public void Read(BinaryReader r)
        {
            order = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            sequence = new List<(DateTime, float[])>(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence.Add((timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(order);
            w.Write(dimension);
            w.Write(sequence.Count);
            foreach (var (timestamp, vec) in sequence)
            {
                w.Write(timestamp.ToBinary());
                foreach (var val in vec)
                    w.Write(val);
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
        private List<float[]> sequence1;
        private List<float[]> sequence2;
        private bool isSeq1;
        private int dimension;

        public void Init()
        {
            sequence1 = new List<float[]>();
            sequence2 = new List<float[]>();
            isSeq1 = true;
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
            if (other.sequence1 != null)
                sequence1.AddRange(other.sequence1);
            if (other.sequence2 != null)
                sequence2.AddRange(other.sequence2);
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

            return new SqlDouble(dtw[n, m]);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            
            int count1 = r.ReadInt32();
            sequence1 = new List<float[]>(count1);
            for (int i = 0; i < count1; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence1.Add(vec);
            }

            int count2 = r.ReadInt32();
            sequence2 = new List<float[]>(count2);
            for (int i = 0; i < count2; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence2.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            
            w.Write(sequence1.Count);
            foreach (var vec in sequence1)
                foreach (var val in vec)
                    w.Write(val);

            w.Write(sequence2.Count);
            foreach (var vec in sequence2)
                foreach (var val in vec)
                    w.Write(val);
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
        private List<(DateTime Timestamp, float[] Vector)> sequence;
        private double threshold;
        private int dimension;

        public void Init()
        {
            sequence = new List<(DateTime, float[])>();
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

            sequence.Add((timestamp.Value, vec));
        }

        public void Merge(ChangePointDetection other)
        {
            if (other.sequence != null)
                sequence.AddRange(other.sequence);
        }

        public SqlString Terminate()
        {
            if (sequence.Count < 10 || dimension == 0)
                return SqlString.Null;

            // Sort by timestamp
            sequence.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // CUSUM algorithm for change point detection
            var changePoints = new List<(int Index, DateTime Timestamp, double Score)>();

            // Compute cumulative deviation from mean
            float[] overallMean = new float[dimension];
            foreach (var (_, vec) in sequence)
                for (int i = 0; i < dimension; i++)
                    overallMean[i] += vec[i];
            for (int i = 0; i < dimension; i++)
                overallMean[i] /= sequence.Count;

            // Sliding window approach
            int windowSize = Math.Max(10, sequence.Count / 10);
            for (int i = windowSize; i < sequence.Count - windowSize; i++)
            {
                // Compute means before and after
                float[] meanBefore = new float[dimension];
                float[] meanAfter = new float[dimension];

                for (int j = i - windowSize; j < i; j++)
                    for (int d = 0; d < dimension; d++)
                        meanBefore[d] += sequence[j].Vector[d];
                
                for (int j = i; j < i + windowSize; j++)
                    for (int d = 0; d < dimension; d++)
                        meanAfter[d] += sequence[j].Vector[d];

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
                    changePoints.Add((i, sequence[i].Timestamp, diff));
                }
            }

            // Return top change points
            var topChanges = changePoints.OrderByDescending(cp => cp.Score).Take(5).ToList();

            if (topChanges.Count == 0)
                return new SqlString("{\"change_points\":[]}");

            var json = "{\"change_points\":[" +
                string.Join(",",
                    topChanges.Select(cp =>
                        $"{{\"index\":{cp.Index}," +
                        $"\"timestamp\":\"{cp.Timestamp:O}\"," +
                        $"\"score\":{cp.Score:G6}}}"
                    )
                ) + "]}";

            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            threshold = r.ReadDouble();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            sequence = new List<(DateTime, float[])>(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                sequence.Add((timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(threshold);
            w.Write(dimension);
            w.Write(sequence.Count);
            foreach (var (timestamp, vec) in sequence)
            {
                w.Write(timestamp.ToBinary());
                foreach (var val in vec)
                    w.Write(val);
            }
        }
    }
}
