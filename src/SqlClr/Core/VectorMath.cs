using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SqlClrFunctions.Core
{
    /// <summary>
    /// High-performance vector math operations using SIMD/AVX when available.
    /// Zero-allocation, span-based operations for SQL CLR aggregates.
    /// </summary>
    public static class VectorMath
    {
        private const int SimdThreshold = 128; // Use SIMD for vectors >= this size

        #region SIMD-Optimized Distance Calculations

        /// <summary>
        /// Cosine similarity with AVX2/AVX512 acceleration when available.
        /// Returns value in [-1, 1] where 1 = identical, 0 = orthogonal, -1 = opposite.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            int length = Math.Min(a.Length, b.Length);
            if (length == 0) return 0f;

            float dotProduct, normA, normB;

            if (Avx.IsSupported && length >= SimdThreshold)
            {
                DotProductAndNormsAvx(a, b, length, out dotProduct, out normA, out normB);
            }
            else if (Vector.IsHardwareAccelerated && length >= 32)
            {
                DotProductAndNormsVectorized(a, b, length, out dotProduct, out normB, out normA);
            }
            else
            {
                DotProductAndNormsScalar(a, b, length, out dotProduct, out normA, out normB);
            }

            if (normA == 0f || normB == 0f) return 0f;
            return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
        }

        /// <summary>
        /// Euclidean distance (L2 norm) with SIMD acceleration.
        /// Returns sqrt(sum((a[i] - b[i])^2)).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EuclideanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            int length = Math.Min(a.Length, b.Length);
            if (length == 0) return 0f;

            float sumSquared;

            if (Avx.IsSupported && length >= SimdThreshold)
            {
                sumSquared = EuclideanDistanceAvx(a, b, length);
            }
            else if (Vector.IsHardwareAccelerated && length >= 32)
            {
                sumSquared = EuclideanDistanceVectorized(a, b, length);
            }
            else
            {
                sumSquared = EuclideanDistanceScalar(a, b, length);
            }

            return MathF.Sqrt(sumSquared);
        }

        /// <summary>
        /// Squared Euclidean distance (avoids sqrt for performance).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EuclideanDistanceSquared(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            int length = Math.Min(a.Length, b.Length);
            if (length == 0) return 0f;

            if (Avx.IsSupported && length >= SimdThreshold)
                return EuclideanDistanceAvx(a, b, length);
            else if (Vector.IsHardwareAccelerated && length >= 32)
                return EuclideanDistanceVectorized(a, b, length);
            else
                return EuclideanDistanceScalar(a, b, length);
        }

        /// <summary>
        /// Dot product with SIMD acceleration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            int length = Math.Min(a.Length, b.Length);
            if (length == 0) return 0f;

            if (Avx.IsSupported && length >= SimdThreshold)
            {
                DotProductAndNormsAvx(a, b, length, out float dot, out _, out _);
                return dot;
            }
            else if (Vector.IsHardwareAccelerated && length >= 32)
            {
                return DotProductVectorized(a, b, length);
            }
            else
            {
                return DotProductScalar(a, b, length);
            }
        }

        /// <summary>
        /// L1 (Manhattan) distance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ManhattanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            int length = Math.Min(a.Length, b.Length);
            if (length == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < length; i++)
            {
                sum += MathF.Abs(a[i] - b[i]);
            }
            return sum;
        }

        #endregion

        #region SIMD Implementation Details

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void DotProductAndNormsAvx(
            ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length,
            out float dotProduct, out float normA, out float normB)
        {
            Vector256<float> vDot = Vector256<float>.Zero;
            Vector256<float> vNormA = Vector256<float>.Zero;
            Vector256<float> vNormB = Vector256<float>.Zero;

            int simdLength = length & ~7; // Process 8 floats at a time
            int i = 0;

            fixed (float* pA = a, pB = b)
            {
                for (; i < simdLength; i += 8)
                {
                    Vector256<float> va = Avx.LoadVector256(pA + i);
                    Vector256<float> vb = Avx.LoadVector256(pB + i);

                    vDot = Avx.Add(vDot, Avx.Multiply(va, vb));
                    vNormA = Avx.Add(vNormA, Avx.Multiply(va, va));
                    vNormB = Avx.Add(vNormB, Avx.Multiply(vb, vb));
                }
            }

            // Horizontal sum
            dotProduct = HorizontalSum(vDot);
            normA = HorizontalSum(vNormA);
            normB = HorizontalSum(vNormB);

            // Process remaining elements
            for (; i < length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DotProductAndNormsVectorized(
            ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length,
            out float dotProduct, out float normA, out float normB)
        {
            Vector<float> vDot = Vector<float>.Zero;
            Vector<float> vNormA = Vector<float>.Zero;
            Vector<float> vNormB = Vector<float>.Zero;

            int simdLength = length - (length % Vector<float>.Count);
            int i = 0;

            for (; i < simdLength; i += Vector<float>.Count)
            {
                var va = new Vector<float>(a.Slice(i, Vector<float>.Count));
                var vb = new Vector<float>(b.Slice(i, Vector<float>.Count));

                vDot += va * vb;
                vNormA += va * va;
                vNormB += vb * vb;
            }

            dotProduct = Vector.Dot(vDot, Vector<float>.One);
            normA = Vector.Dot(vNormA, Vector<float>.One);
            normB = Vector.Dot(vNormB, Vector<float>.One);

            for (; i < length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DotProductAndNormsScalar(
            ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length,
            out float dotProduct, out float normA, out float normB)
        {
            dotProduct = 0f;
            normA = 0f;
            normB = 0f;

            for (int i = 0; i < length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float EuclideanDistanceAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length)
        {
            Vector256<float> vSum = Vector256<float>.Zero;
            int simdLength = length & ~7;
            int i = 0;

            fixed (float* pA = a, pB = b)
            {
                for (; i < simdLength; i += 8)
                {
                    Vector256<float> va = Avx.LoadVector256(pA + i);
                    Vector256<float> vb = Avx.LoadVector256(pB + i);
                    Vector256<float> diff = Avx.Subtract(va, vb);
                    vSum = Avx.Add(vSum, Avx.Multiply(diff, diff));
                }
            }

            float sumSquared = HorizontalSum(vSum);

            for (; i < length; i++)
            {
                float diff = a[i] - b[i];
                sumSquared += diff * diff;
            }

            return sumSquared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EuclideanDistanceVectorized(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length)
        {
            Vector<float> vSum = Vector<float>.Zero;
            int simdLength = length - (length % Vector<float>.Count);
            int i = 0;

            for (; i < simdLength; i += Vector<float>.Count)
            {
                var va = new Vector<float>(a.Slice(i, Vector<float>.Count));
                var vb = new Vector<float>(b.Slice(i, Vector<float>.Count));
                var diff = va - vb;
                vSum += diff * diff;
            }

            float sumSquared = Vector.Dot(vSum, Vector<float>.One);

            for (; i < length; i++)
            {
                float diff = a[i] - b[i];
                sumSquared += diff * diff;
            }

            return sumSquared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EuclideanDistanceScalar(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length)
        {
            float sumSquared = 0f;
            for (int i = 0; i < length; i++)
            {
                float diff = a[i] - b[i];
                sumSquared += diff * diff;
            }
            return sumSquared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DotProductVectorized(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length)
        {
            Vector<float> vSum = Vector<float>.Zero;
            int simdLength = length - (length % Vector<float>.Count);
            int i = 0;

            for (; i < simdLength; i += Vector<float>.Count)
            {
                var va = new Vector<float>(a.Slice(i, Vector<float>.Count));
                var vb = new Vector<float>(b.Slice(i, Vector<float>.Count));
                vSum += va * vb;
            }

            float sum = Vector.Dot(vSum, Vector<float>.One);

            for (; i < length; i++)
                sum += a[i] * b[i];

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DotProductScalar(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length)
        {
            float sum = 0f;
            for (int i = 0; i < length; i++)
                sum += a[i] * b[i];
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float HorizontalSum(Vector256<float> v)
        {
            // AVX horizontal sum
            Vector128<float> low = v.GetLower();
            Vector128<float> high = v.GetUpper();
            Vector128<float> sum = Sse.Add(low, high);
            
            // Shuffle and add
            Vector128<float> shuf = Sse.Shuffle(sum, sum, 0b_00_01_10_11);
            sum = Sse.Add(sum, shuf);
            shuf = Sse.MoveHighToLow(shuf, sum);
            sum = Sse.Add(sum, shuf);
            
            return sum.ToScalar();
        }

        #endregion

        #region Vector Operations

        /// <summary>
        /// Compute centroid (mean) of vectors with SIMD.
        /// </summary>
        public static void ComputeCentroid(ReadOnlySpan<float[]> vectors, Span<float> centroid)
        {
            if (vectors.Length == 0) return;

            int dimension = centroid.Length;
            centroid.Clear();

            // Accumulate
            foreach (var vec in vectors)
            {
                for (int i = 0; i < dimension && i < vec.Length; i++)
                    centroid[i] += vec[i];
            }

            // Divide by count
            float invCount = 1f / vectors.Length;
            if (Vector.IsHardwareAccelerated && dimension >= 32)
            {
                int simdLength = dimension - (dimension % Vector<float>.Count);
                for (int i = 0; i < simdLength; i += Vector<float>.Count)
                {
                    var v = new Vector<float>(centroid.Slice(i, Vector<float>.Count));
                    v *= invCount;
                    v.CopyTo(centroid.Slice(i));
                }
                for (int i = simdLength; i < dimension; i++)
                    centroid[i] *= invCount;
            }
            else
            {
                for (int i = 0; i < dimension; i++)
                    centroid[i] *= invCount;
            }
        }

        /// <summary>
        /// Normalize vector to unit length (L2 norm).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(Span<float> vector)
        {
            float norm = 0f;
            for (int i = 0; i < vector.Length; i++)
                norm += vector[i] * vector[i];

            norm = MathF.Sqrt(norm);
            if (norm == 0f) return;

            float invNorm = 1f / norm;
            
            if (Vector.IsHardwareAccelerated && vector.Length >= 32)
            {
                int simdLength = vector.Length - (vector.Length % Vector<float>.Count);
                for (int i = 0; i < simdLength; i += Vector<float>.Count)
                {
                    var v = new Vector<float>(vector.Slice(i, Vector<float>.Count));
                    v *= invNorm;
                    v.CopyTo(vector.Slice(i));
                }
                for (int i = simdLength; i < vector.Length; i++)
                    vector[i] *= invNorm;
            }
            else
            {
                for (int i = 0; i < vector.Length; i++)
                    vector[i] *= invNorm;
            }
        }

        /// <summary>
        /// Add two vectors with SIMD: result = a + b
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
        {
            int length = Math.Min(Math.Min(a.Length, b.Length), result.Length);

            if (Vector.IsHardwareAccelerated && length >= 32)
            {
                int simdLength = length - (length % Vector<float>.Count);
                for (int i = 0; i < simdLength; i += Vector<float>.Count)
                {
                    var va = new Vector<float>(a.Slice(i, Vector<float>.Count));
                    var vb = new Vector<float>(b.Slice(i, Vector<float>.Count));
                    (va + vb).CopyTo(result.Slice(i));
                }
                for (int i = simdLength; i < length; i++)
                    result[i] = a[i] + b[i];
            }
            else
            {
                for (int i = 0; i < length; i++)
                    result[i] = a[i] + b[i];
            }
        }

        /// <summary>
        /// Scale vector by scalar with SIMD: result = a * scale
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Scale(ReadOnlySpan<float> a, float scale, Span<float> result)
        {
            int length = Math.Min(a.Length, result.Length);

            if (Vector.IsHardwareAccelerated && length >= 32)
            {
                var vScale = new Vector<float>(scale);
                int simdLength = length - (length % Vector<float>.Count);
                for (int i = 0; i < simdLength; i += Vector<float>.Count)
                {
                    var va = new Vector<float>(a.Slice(i, Vector<float>.Count));
                    (va * vScale).CopyTo(result.Slice(i));
                }
                for (int i = simdLength; i < length; i++)
                    result[i] = a[i] * scale;
            }
            else
            {
                for (int i = 0; i < length; i++)
                    result[i] = a[i] * scale;
            }
        }

        #endregion
    }
}
