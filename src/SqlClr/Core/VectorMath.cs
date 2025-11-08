using System;
using System.Numerics;

namespace SqlClrFunctions.Core
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
    }
}