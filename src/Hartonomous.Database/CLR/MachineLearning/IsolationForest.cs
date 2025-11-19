using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Isolation Forest algorithm for anomaly detection.
    /// Anomalies are isolated faster in random trees (shorter path lengths).
    /// </summary>
    internal static class IsolationForest
    {
        /// <summary>
        /// Compute anomaly scores for a collection of vectors.
        /// Higher scores indicate more anomalous points.
        /// </summary>
        /// <param name="vectors">Collection of vectors to score</param>
        /// <param name="numTrees">Number of isolation trees to build (default: 10)</param>
        /// <param name="randomSeed">Random seed for reproducibility (default: 42)</param>
        /// <returns>Array of anomaly scores (0-1, higher = more anomalous)</returns>
        public static float[] ComputeAnomalyScores(float[][] vectors, int numTrees = 10, int randomSeed = 42)
        {
            if (vectors == null || vectors.Length < 2)
                return Array.Empty<float>();

            int dimension = vectors[0].Length;
            var random = new Random(randomSeed);
            
            // Limit number of trees to reasonable range
            numTrees = Math.Min(numTrees, vectors.Length / 2);
            if (numTrees < 1) numTrees = 1;

            double[] avgPathLengths = new double[vectors.Length];

            for (int tree = 0; tree < numTrees; tree++)
            {
                // Select random feature for this tree
                int feature = random.Next(dimension);
                
                // Sort vectors by this feature
                var sorted = vectors
                    .Select((v, idx) => new { Vector = v, Index = idx, Value = v[feature] })
                    .OrderBy(x => x.Value)
                    .ToList();

                // Path length approximation: position in sorted order
                // Points that are isolated (far from others) will be at extremes
                for (int i = 0; i < sorted.Count; i++)
                {
                    avgPathLengths[sorted[i].Index] += i;
                }
            }

            // Normalize path lengths and compute anomaly scores
            // Lower average path length = easier to isolate = more anomalous
            double maxDepth = vectors.Length * numTrees;
            var scores = new float[vectors.Length];
            for (int i = 0; i < vectors.Length; i++)
            {
                scores[i] = (float)(1.0 - (avgPathLengths[i] / maxDepth));
            }

            return scores;
        }

        /// <summary>
        /// Compute anomaly score for a single vector against a reference set.
        /// </summary>
        /// <param name="vector">Vector to score</param>
        /// <param name="referenceVectors">Reference vectors for building isolation trees</param>
        /// <param name="numTrees">Number of isolation trees to build</param>
        /// <param name="randomSeed">Random seed for reproducibility</param>
        /// <returns>Anomaly score (0-1, higher = more anomalous)</returns>
        public static float ComputeSingleScore(float[] vector, float[][] referenceVectors, int numTrees = 10, int randomSeed = 42)
        {
            if (vector == null || referenceVectors == null || referenceVectors.Length == 0)
                return 0f;

            int dimension = vector.Length;
            var random = new Random(randomSeed);
            
            numTrees = Math.Min(numTrees, referenceVectors.Length);
            if (numTrees < 1) numTrees = 1;

            double totalPathLength = 0;

            for (int tree = 0; tree < numTrees; tree++)
            {
                int feature = random.Next(dimension);
                float testValue = vector[feature];
                
                // Count how many reference vectors are less than test value
                int position = 0;
                foreach (var refVec in referenceVectors)
                {
                    if (refVec[feature] < testValue)
                        position++;
                }

                totalPathLength += position;
            }

            // Normalize and compute score
            double maxDepth = referenceVectors.Length * numTrees;
            return (float)(1.0 - (totalPathLength / maxDepth));
        }
    }
}
