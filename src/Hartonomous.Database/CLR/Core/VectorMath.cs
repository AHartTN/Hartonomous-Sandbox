using System;
using System.Numerics;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Provides a centralized, high-performance implementation of common vector mathematical operations,
    /// accelerated with SIMD (Single Instruction, Multiple Data) using System.Numerics.Vectors.
    /// This class adheres to the DRY principle by consolidating logic previously duplicated
    /// across multiple classes.
    /// </summary>
    public static class VectorMath
    {
        /// <summary>
        /// Computes the dot product of two vectors, accelerated with SIMD.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product of the two vectors.</returns>
        public static float DotProduct(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            float result = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;
            int length = a.Length;

            // Process vectors in SIMD chunks
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(a, i);
                var v2 = new Vector<float>(b, i);
                result += Vector.Dot(v1, v2);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                result += a[i] * b[i];
            }
            return result;
        }

        /// <summary>
        /// Computes the L2 Norm (or magnitude) of a vector, accelerated with SIMD.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The L2 Norm of the vector.</returns>
        public static float Norm(float[] a)
        {
            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;
            int length = a.Length;

            // Process in SIMD chunks
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(a, i);
                sumSquares += Vector.Dot(v, v);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                sumSquares += a[i] * a[i];
            }
            return (float)Math.Sqrt(sumSquares);
        }

        /// <summary>
        /// Computes the cosine similarity between two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The cosine similarity (-1 to 1).</returns>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            float dotProduct = DotProduct(a, b);
            float normA = Norm(a);
            float normB = Norm(b);

            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / (normA * normB);
        }

        /// <summary>
        /// Computes the Euclidean distance between two vectors with SIMD acceleration.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The Euclidean distance.</returns>
        public static float EuclideanDistance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;
            int length = a.Length;

            // Process in SIMD chunks
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(a, i);
                var v2 = new Vector<float>(b, i);
                var diff = v1 - v2;
                sumSquares += Vector.Dot(diff, diff);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                float diff = a[i] - b[i];
                sumSquares += diff * diff;
            }
            
            return (float)Math.Sqrt(sumSquares);
        }

        /// <summary>
        /// Computes the centroid (mean vector) of a collection of vectors.
        /// </summary>
        /// <param name="vectors">The collection of vectors.</param>
        /// <returns>The centroid vector.</returns>
        public static float[] ComputeCentroid(float[][] vectors)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors array cannot be null or empty");

            int dimensions = vectors[0].Length;
            float[] centroid = new float[dimensions];

            foreach (var vector in vectors)
            {
                if (vector.Length != dimensions)
                    throw new ArgumentException("All vectors must have the same dimension");

                for (int i = 0; i < dimensions; i++)
                {
                    centroid[i] += vector[i];
                }
            }

            for (int i = 0; i < dimensions; i++)
            {
                centroid[i] /= vectors.Length;
            }

            return centroid;
        }

        /// <summary>
        /// Computes the centroid (mean vector) of a collection of vectors and stores it in an output parameter.
        /// </summary>
        /// <param name="vectors">The collection of vectors.</param>
        /// <param name="centroid">Output array to store the centroid.</param>
        public static void ComputeCentroid(float[][] vectors, float[] centroid)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors array cannot be null or empty");

            int dimensions = vectors[0].Length;
            if (centroid.Length != dimensions)
                throw new ArgumentException("Centroid array must match vector dimensions");

            Array.Clear(centroid, 0, centroid.Length);

            foreach (var vector in vectors)
            {
                if (vector.Length != dimensions)
                    throw new ArgumentException("All vectors must have the same dimension");

                for (int i = 0; i < dimensions; i++)
                {
                    centroid[i] += vector[i];
                }
            }

            for (int i = 0; i < dimensions; i++)
            {
                centroid[i] /= vectors.Length;
            }
        }
    }
}
