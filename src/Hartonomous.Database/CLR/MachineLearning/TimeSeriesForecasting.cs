using System;
using System.Collections.Generic;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Time series forecasting algorithms for vector sequences
    /// </summary>
    internal static class TimeSeriesForecasting
    {
        /// <summary>
        /// Autoregressive (AR) forecast: predict next vector based on last N observations
        /// Uses exponential weighting (recent observations have higher weight)
        /// </summary>
        /// <param name="sequence">Time-ordered vector sequence</param>
        /// <param name="order">Number of past observations to use (AR order)</param>
        /// <param name="dimension">Vector dimension</param>
        /// <returns>Forecasted vector</returns>
        public static float[]? ARForecast(TimestampedVector[] sequence, int order, int dimension)
        {
            if (sequence == null || sequence.Length <= order || order <= 0 || dimension <= 0)
                return null;

            float[] forecast = new float[dimension];
            double[] weights = new double[order];
            double weightSum = 0;

            // Exponential weights (recent = more important)
            // weights[0] corresponds to oldest, weights[order-1] to most recent
            for (int i = 0; i < order; i++)
            {
                weights[i] = Math.Exp(i); // More recent = higher weight
                weightSum += weights[i];
            }

            // Normalize weights
            for (int i = 0; i < order; i++)
                weights[i] /= weightSum;

            // Compute weighted forecast from last N vectors
            int startIdx = sequence.Length - order;
            for (int i = 0; i < order; i++)
            {
                var vec = sequence[startIdx + i].Vector;
                for (int d = 0; d < dimension; d++)
                {
                    forecast[d] += (float)(vec[d] * weights[i]);
                }
            }

            return forecast;
        }

        /// <summary>
        /// Pattern discovery result
        /// </summary>
        public struct PatternResult
        {
            public int StartIndex;
            public double AvgSimilarity;
            public int Occurrences;
        }

        /// <summary>
        /// Find repeated subsequence patterns in a vector time series
        /// Uses sliding window comparison with cosine similarity
        /// </summary>
        /// <param name="sequence">Time-ordered vector sequence</param>
        /// <param name="patternLength">Length of patterns to discover</param>
        /// <param name="similarityThreshold">Minimum similarity for pattern match (default 0.8)</param>
        /// <returns>Array of discovered patterns sorted by occurrence count</returns>
        public static PatternResult[] DiscoverPatterns(
            TimestampedVector[] sequence, 
            int patternLength,
            double similarityThreshold = 0.8)
        {
            if (sequence == null || sequence.Length < patternLength * 2 || patternLength <= 0)
                return Array.Empty<PatternResult>();

            int total = sequence.Length;
            var patternsList = new List<PatternResult>();

            // Scan for patterns
            for (int i = 0; i <= total - patternLength; i++)
            {
                int occurrences = 0;
                double totalSimilarity = 0;

                // Compare this window with all subsequent windows
                for (int j = i + patternLength; j <= total - patternLength; j++)
                {
                    double windowSim = 0;

                    // Compute average similarity across the pattern window
                    for (int k = 0; k < patternLength; k++)
                    {
                        windowSim += VectorUtilities.CosineSimilarity(
                            sequence[i + k].Vector,
                            sequence[j + k].Vector);
                    }
                    windowSim /= patternLength;

                    if (windowSim > similarityThreshold)
                    {
                        occurrences++;
                        totalSimilarity += windowSim;
                    }
                }

                if (occurrences > 0)
                {
                    patternsList.Add(new PatternResult
                    {
                        StartIndex = i,
                        AvgSimilarity = totalSimilarity / occurrences,
                        Occurrences = occurrences + 1 // Include original occurrence
                    });
                }
            }

            if (patternsList.Count == 0)
                return Array.Empty<PatternResult>();

            // Sort by occurrences (descending), then by similarity
            var patterns = patternsList.ToArray();
            Array.Sort(patterns, (a, b) =>
            {
                int cmp = b.Occurrences.CompareTo(a.Occurrences);
                if (cmp != 0)
                    return cmp;
                return b.AvgSimilarity.CompareTo(a.AvgSimilarity);
            });

            return patterns;
        }

        /// <summary>
        /// Moving average forecast: simple average of last N observations
        /// Simpler alternative to AR forecast
        /// </summary>
        /// <param name="sequence">Time-ordered vector sequence</param>
        /// <param name="window">Number of past observations to average</param>
        /// <param name="dimension">Vector dimension</param>
        /// <returns>Forecasted vector (average of last N)</returns>
        public static float[]? MovingAverageForecast(TimestampedVector[] sequence, int window, int dimension)
        {
            if (sequence == null || sequence.Length < window || window <= 0 || dimension <= 0)
                return null;

            float[] forecast = new float[dimension];
            int startIdx = sequence.Length - window;

            // Simple average of last N vectors
            for (int i = startIdx; i < sequence.Length; i++)
            {
                var vec = sequence[i].Vector;
                for (int d = 0; d < dimension; d++)
                {
                    forecast[d] += vec[d];
                }
            }

            // Normalize by window size
            for (int d = 0; d < dimension; d++)
            {
                forecast[d] /= window;
            }

            return forecast;
        }
    }
}
