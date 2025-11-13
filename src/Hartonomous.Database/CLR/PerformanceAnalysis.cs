using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    /// <summary>
    /// Performance vector construction for anomaly detection
    /// Builds normalized feature vectors from performance metrics
    /// </summary>
    public static class PerformanceAnalysis
    {
        /// <summary>
        /// Build performance vector for anomaly detection
        /// Creates normalized feature vector [duration_norm, tokens_norm, hour_norm, weekday_norm, ...]
        /// </summary>
        /// <param name="durationMs">Request duration in milliseconds</param>
        /// <param name="tokenCount">Number of tokens processed</param>
        /// <param name="hourOfDay">Hour of day (0-23)</param>
        /// <param name="dayOfWeek">Day of week (1-7)</param>
        /// <param name="vectorDimension">Target vector dimension (default 1998)</param>
        /// <returns>VECTOR(dimension) for anomaly detection</returns>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes BuildPerformanceVector(
            SqlInt32 durationMs,
            SqlInt32 tokenCount,
            SqlInt32 hourOfDay,
            SqlInt32 dayOfWeek,
            SqlInt32 vectorDimension)
        {
            if (durationMs.IsNull || tokenCount.IsNull || hourOfDay.IsNull || dayOfWeek.IsNull)
                return SqlBytes.Null;

            int dim = vectorDimension.IsNull ? 1998 : vectorDimension.Value;
            float[] vector = new float[dim];

            // Core performance metrics (first 4 dimensions)
            vector[0] = durationMs.Value / 1000.0f; // Normalize to seconds
            vector[1] = tokenCount.Value / 100.0f; // Normalize to hundreds of tokens
            vector[2] = hourOfDay.Value / 24.0f; // Normalize hour to [0,1]
            vector[3] = dayOfWeek.Value / 7.0f; // Normalize day to [0,1]

            // Additional engineered features (dimensions 4-19)
            vector[4] = (float)Math.Log(durationMs.Value + 1); // Log duration
            vector[5] = (float)Math.Log(tokenCount.Value + 1); // Log tokens
            vector[6] = tokenCount.Value > 0 ? durationMs.Value / (float)tokenCount.Value : 0; // Time per token
            vector[7] = (float)Math.Sin(2 * Math.PI * hourOfDay.Value / 24.0); // Cyclic hour encoding (sin)
            vector[8] = (float)Math.Cos(2 * Math.PI * hourOfDay.Value / 24.0); // Cyclic hour encoding (cos)
            vector[9] = (float)Math.Sin(2 * Math.PI * dayOfWeek.Value / 7.0); // Cyclic day encoding (sin)
            vector[10] = (float)Math.Cos(2 * Math.PI * dayOfWeek.Value / 7.0); // Cyclic day encoding (cos)

            // Is weekend indicator
            vector[11] = (dayOfWeek.Value == 1 || dayOfWeek.Value == 7) ? 1.0f : 0.0f;

            // Is peak hours (9am-5pm)
            vector[12] = (hourOfDay.Value >= 9 && hourOfDay.Value <= 17) ? 1.0f : 0.0f;

            // Duration bins (for categorical analysis)
            if (durationMs.Value < 100)
                vector[13] = 1.0f; // Very fast
            else if (durationMs.Value < 500)
                vector[14] = 1.0f; // Fast
            else if (durationMs.Value < 1000)
                vector[15] = 1.0f; // Normal
            else if (durationMs.Value < 5000)
                vector[16] = 1.0f; // Slow
            else
                vector[17] = 1.0f; // Very slow

            // Token count bins
            if (tokenCount.Value < 50)
                vector[18] = 1.0f; // Short
            else if (tokenCount.Value < 200)
                vector[19] = 1.0f; // Medium

            // Remaining dimensions: pad with zeros (can be used for future features)
            // Examples: cache hit rate, model version, tenant characteristics, etc.

            // Serialize to binary format for VECTOR(dim) type
            byte[] bytes = new byte[dim * sizeof(float)];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);

            return new SqlBytes(bytes);
        }

        /// <summary>
        /// Compute z-score for anomaly detection
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble ComputeZScore(SqlDouble value, SqlDouble mean, SqlDouble stdDev)
        {
            if (value.IsNull || mean.IsNull || stdDev.IsNull || stdDev.Value == 0)
                return SqlDouble.Null;

            double zScore = (value.Value - mean.Value) / stdDev.Value;
            return new SqlDouble(zScore);
        }

        /// <summary>
        /// Detect if value is an outlier using IQR method
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBoolean IsOutlierIQR(SqlDouble value, SqlDouble q1, SqlDouble q3, SqlDouble iqrMultiplier)
        {
            if (value.IsNull || q1.IsNull || q3.IsNull)
                return SqlBoolean.Null;

            double multiplier = iqrMultiplier.IsNull ? 1.5 : iqrMultiplier.Value;
            double iqr = q3.Value - q1.Value;
            double lowerBound = q1.Value - (multiplier * iqr);
            double upperBound = q3.Value + (multiplier * iqr);

            bool isOutlier = value.Value < lowerBound || value.Value > upperBound;
            return new SqlBoolean(isOutlier);
        }
    }
}
