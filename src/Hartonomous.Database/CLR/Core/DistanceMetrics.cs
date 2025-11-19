using System;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Core distance/similarity metric interfaces and implementations.
    /// Foundation for all spatial operations, ML algorithms, search, clustering, etc.
    /// </summary>
    /// 
    public enum DistanceMetricType
    {
        Euclidean = 0,
        Cosine = 1,
        Manhattan = 2,
        Chebyshev = 3,
        Minkowski = 4,
        Hamming = 5,
        Canberra = 6,
        Mahalanobis = 7
    }

    public interface IDistanceMetric
    {
        /// <summary>
        /// Calculate distance between two vectors (0 = identical, higher = more different)
        /// </summary>
        double Distance(float[] a, float[] b);

        /// <summary>
        /// Calculate similarity between two vectors (1 = identical, 0 = completely different)
        /// Inverse relationship to distance, normalized to [0,1]
        /// </summary>
        double Similarity(float[] a, float[] b);
    }

    /// <summary>
    /// Euclidean (L2) distance - straight-line distance in n-dimensional space.
    /// Use for: spatial coordinates, embeddings, general-purpose.
    /// </summary>
    public class EuclideanDistance : IDistanceMetric
    {
        public double Distance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        public double Similarity(float[] a, float[] b)
        {
            double dist = Distance(a, b);
            return 1.0 / (1.0 + dist);
        }
    }

    /// <summary>
    /// Cosine similarity - angle between vectors (ignores magnitude).
    /// Use for: semantic embeddings, text similarity, normalized features.
    /// </summary>
    public class CosineDistance : IDistanceMetric
    {
        public double Distance(float[] a, float[] b)
        {
            return 1.0 - Similarity(a, b);
        }

        public double Similarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            double denom = Math.Sqrt(normA) * Math.Sqrt(normB);
            if (denom < 1e-10)
                return 0.0;

            return dotProduct / denom;
        }
    }

    /// <summary>
    /// Manhattan (L1, Taxicab) distance - sum of absolute differences.
    /// Use for: sparse features, grid-based movement, high-dimensional spaces.
    /// </summary>
    public class ManhattanDistance : IDistanceMetric
    {
        public double Distance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += Math.Abs(a[i] - b[i]);
            }
            return sum;
        }

        public double Similarity(float[] a, float[] b)
        {
            double dist = Distance(a, b);
            return 1.0 / (1.0 + dist);
        }
    }

    /// <summary>
    /// Chebyshev (L∞) distance - maximum absolute difference across dimensions.
    /// Use for: worst-case scenarios, grid distances, chess king movement.
    /// </summary>
    public class ChebyshevDistance : IDistanceMetric
    {
        public double Distance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            double max = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = Math.Abs(a[i] - b[i]);
                if (diff > max)
                    max = diff;
            }
            return max;
        }

        public double Similarity(float[] a, float[] b)
        {
            double dist = Distance(a, b);
            return 1.0 / (1.0 + dist);
        }
    }

    /// <summary>
    /// Minkowski distance - generalization of Euclidean and Manhattan.
    /// p=1: Manhattan, p=2: Euclidean, p=∞: Chebyshev
    /// Use for: configurable distance with power parameter.
    /// </summary>
    public class MinkowskiDistance : IDistanceMetric
    {
        private readonly double p;

        public MinkowskiDistance(double p = 2.0)
        {
            if (p <= 0)
                throw new ArgumentException("p must be positive");
            this.p = p;
        }

        public double Distance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += Math.Pow(Math.Abs(a[i] - b[i]), p);
            }
            return Math.Pow(sum, 1.0 / p);
        }

        public double Similarity(float[] a, float[] b)
        {
            double dist = Distance(a, b);
            return 1.0 / (1.0 + dist);
        }
    }

    /// <summary>
    /// Hamming distance - count of differing elements.
    /// Use for: binary vectors, categorical features, error detection.
    /// </summary>
    public class HammingDistance : IDistanceMetric
    {
        private readonly float tolerance;

        public HammingDistance(float tolerance = 1e-6f)
        {
            this.tolerance = tolerance;
        }

        public double Distance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            int diffCount = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (Math.Abs(a[i] - b[i]) > tolerance)
                    diffCount++;
            }
            return diffCount;
        }

        public double Similarity(float[] a, float[] b)
        {
            double dist = Distance(a, b);
            return 1.0 - (dist / a.Length);
        }
    }

    /// <summary>
    /// Canberra distance - weighted version of Manhattan for positive values.
    /// Use for: comparing distributions, ranking problems, non-negative features.
    /// </summary>
    public class CanberraDistance : IDistanceMetric
    {
        public double Distance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions must match");

            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double num = Math.Abs(a[i] - b[i]);
                double denom = Math.Abs(a[i]) + Math.Abs(b[i]);
                if (denom > 1e-10)
                    sum += num / denom;
            }
            return sum;
        }

        public double Similarity(float[] a, float[] b)
        {
            double dist = Distance(a, b);
            return 1.0 / (1.0 + dist);
        }
    }

    /// <summary>
    /// Factory for creating distance metric instances.
    /// </summary>
    public static class DistanceMetricFactory
    {
        public static IDistanceMetric Create(DistanceMetricType type, double parameter = 2.0)
        {
            switch (type)
            {
                case DistanceMetricType.Euclidean:
                    return new EuclideanDistance();
                case DistanceMetricType.Cosine:
                    return new CosineDistance();
                case DistanceMetricType.Manhattan:
                    return new ManhattanDistance();
                case DistanceMetricType.Chebyshev:
                    return new ChebyshevDistance();
                case DistanceMetricType.Minkowski:
                    return new MinkowskiDistance(parameter);
                case DistanceMetricType.Hamming:
                    return new HammingDistance((float)parameter);
                case DistanceMetricType.Canberra:
                    return new CanberraDistance();
                default:
                    return new EuclideanDistance();
            }
        }

        /// <summary>
        /// Create metric from string name (for SQL parameter passing)
        /// </summary>
        public static IDistanceMetric Create(string metricName, double parameter = 2.0)
        {
            if (string.IsNullOrEmpty(metricName))
                return new EuclideanDistance();

            switch (metricName.ToLowerInvariant())
            {
                case "euclidean":
                case "l2":
                    return new EuclideanDistance();
                case "cosine":
                case "angular":
                    return new CosineDistance();
                case "manhattan":
                case "l1":
                case "taxicab":
                    return new ManhattanDistance();
                case "chebyshev":
                case "linf":
                case "chessboard":
                    return new ChebyshevDistance();
                case "minkowski":
                    return new MinkowskiDistance(parameter);
                case "hamming":
                    return new HammingDistance((float)parameter);
                case "canberra":
                    return new CanberraDistance();
                default:
                    return new EuclideanDistance();
            }
        }
    }

    /// <summary>
    /// Modality-aware distance metric wrapper.
    /// Handles cross-modal and intra-modal distance calculations with appropriate metrics.
    /// </summary>
    public class ModalityAwareDistance : IDistanceMetric
    {
        private readonly IDistanceMetric intraModalMetric;
        private readonly IDistanceMetric crossModalMetric;
        private readonly bool normalize;

        public ModalityAwareDistance(
            IDistanceMetric intraModalMetric,
            IDistanceMetric crossModalMetric,
            bool normalize = false)
        {
            this.intraModalMetric = intraModalMetric ?? new EuclideanDistance();
            this.crossModalMetric = crossModalMetric ?? new CosineDistance();
            this.normalize = normalize;
        }

        public double Distance(float[] a, float[] b)
        {
            return Distance(a, b, string.Empty, string.Empty);
        }

        public double Distance(float[] a, float[] b, string modalityA, string modalityB)
        {
            float[] vecA = normalize ? Normalize(a) : a;
            float[] vecB = normalize ? Normalize(b) : b;

            // If modalities specified and different, use cross-modal metric
            if (!string.IsNullOrEmpty(modalityA) && !string.IsNullOrEmpty(modalityB) 
                && modalityA != modalityB)
            {
                return crossModalMetric.Distance(vecA, vecB);
            }

            return intraModalMetric.Distance(vecA, vecB);
        }

        public double Similarity(float[] a, float[] b)
        {
            return Similarity(a, b, string.Empty, string.Empty);
        }

        public double Similarity(float[] a, float[] b, string modalityA, string modalityB)
        {
            float[] vecA = normalize ? Normalize(a) : a;
            float[] vecB = normalize ? Normalize(b) : b;

            if (!string.IsNullOrEmpty(modalityA) && !string.IsNullOrEmpty(modalityB) 
                && modalityA != modalityB)
            {
                return crossModalMetric.Similarity(vecA, vecB);
            }

            return intraModalMetric.Similarity(vecA, vecB);
        }

        private float[] Normalize(float[] vec)
        {
            double norm = 0;
            for (int i = 0; i < vec.Length; i++)
                norm += vec[i] * vec[i];
            
            norm = Math.Sqrt(norm);
            if (norm < 1e-10)
                return vec;

            float[] normalized = new float[vec.Length];
            for (int i = 0; i < vec.Length; i++)
                normalized[i] = (float)(vec[i] / norm);

            return normalized;
        }
    }
}
