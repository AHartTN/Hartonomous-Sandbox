using System;
using System.Collections.Generic;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Cumulative Sum (CUSUM) algorithm for change point detection.
    /// Detects shifts in statistical properties of sequential data.
    /// </summary>
    internal static class CUSUMDetector
    {
        /// <summary>
        /// Represents a detected change point.
        /// </summary>
        public struct ChangePoint
        {
            public int Index;
            public double Score;
            public double MeanBefore;
            public double MeanAfter;
        }

        /// <summary>
        /// Detect change points in a univariate time series using CUSUM.
        /// </summary>
        /// <param name="values">Time series values</param>
        /// <param name="threshold">Detection threshold (higher = fewer false positives)</param>
        /// <returns>List of detected change points</returns>
        public static List<ChangePoint> DetectChangePoints(double[] values, double threshold)
        {
            var changePoints = new List<ChangePoint>();
            
            if (values == null || values.Length < 3)
                return changePoints;

            // Compute mean and standard deviation
            double mean = 0;
            foreach (var val in values)
                mean += val;
            mean /= values.Length;

            double variance = 0;
            foreach (var val in values)
                variance += (val - mean) * (val - mean);
            variance /= values.Length;
            double stdDev = Math.Sqrt(variance);

            if (stdDev < 1e-10)
                return changePoints; // No variation

            // CUSUM algorithm
            double cumSum = 0;
            double maxCumSum = 0;
            int potentialChangePoint = -1;

            for (int i = 1; i < values.Length; i++)
            {
                double deviation = (values[i] - mean) / stdDev;
                cumSum = Math.Max(0, cumSum + deviation);

                if (cumSum > maxCumSum)
                {
                    maxCumSum = cumSum;
                    potentialChangePoint = i;
                }

                // Detect change point if threshold exceeded
                if (cumSum > threshold && potentialChangePoint >= 0)
                {
                    // Compute means before and after
                    double meanBefore = 0;
                    for (int j = 0; j < potentialChangePoint; j++)
                        meanBefore += values[j];
                    meanBefore /= potentialChangePoint;

                    double meanAfter = 0;
                    for (int j = potentialChangePoint; j < values.Length; j++)
                        meanAfter += values[j];
                    meanAfter /= (values.Length - potentialChangePoint);

                    changePoints.Add(new ChangePoint
                    {
                        Index = potentialChangePoint,
                        Score = cumSum,
                        MeanBefore = meanBefore,
                        MeanAfter = meanAfter
                    });

                    // Reset for next potential change point
                    cumSum = 0;
                    maxCumSum = 0;
                    potentialChangePoint = -1;
                }
            }

            return changePoints;
        }

        /// <summary>
        /// Detect change points in multivariate vector sequences.
        /// Uses magnitude of vectors as the univariate signal.
        /// </summary>
        public static List<ChangePoint> DetectChangePointsMultivariate(float[][] vectors, double threshold)
        {
            if (vectors == null || vectors.Length == 0)
                return new List<ChangePoint>();

            // Convert to magnitudes
            double[] magnitudes = new double[vectors.Length];
            for (int i = 0; i < vectors.Length; i++)
            {
                if (vectors[i] != null)
                {
                    double mag = 0;
                    foreach (var val in vectors[i])
                        mag += val * val;
                    magnitudes[i] = Math.Sqrt(mag);
                }
            }

            return DetectChangePoints(magnitudes, threshold);
        }
    }
}
