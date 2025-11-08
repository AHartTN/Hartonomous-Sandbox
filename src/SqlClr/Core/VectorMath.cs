using System;
using System.Runtime.CompilerServices;

namespace SqlClrFunctions.Core
{
    /// <summary>
    /// High-performance vector math operations.
    /// Uses AggressiveInlining for hot-path performance optimization.
    /// </summary>
    public static class VectorMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null) return 0f;
            int len = Math.Min(a.Length, b.Length);
            if (len == 0) return 0f;
            
            float dot = 0f, normA = 0f, normB = 0f;
            for (int i = 0; i < len; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            
            if (normA == 0f || normB == 0f) return 0f;
            return dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EuclideanDistance(float[] a, float[] b)
        {
            if (a == null || b == null) return float.MaxValue;
            int len = Math.Min(a.Length, b.Length);
            if (len == 0) return float.MaxValue;
            
            float sum = 0f;
            for (int i = 0; i < len; i++)
            {
                float d = a[i] - b[i];
                sum += d * d;
            }
            return (float)Math.Sqrt(sum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotProduct(float[] a, float[] b)
        {
            if (a == null || b == null) return 0f;
            int len = Math.Min(a.Length, b.Length);
            float sum = 0f;
            for (int i = 0; i < len; i++)
                sum += a[i] * b[i];
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Norm(float[] a)
        {
            if (a == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * a[i];
            return (float)Math.Sqrt(sum);
        }

        public static void ComputeCentroid(float[][] vectors, float[] result)
        {
            if (vectors == null || vectors.Length == 0 || result == null) return;
            
            for (int i = 0; i < result.Length; i++)
                result[i] = 0f;
            
            foreach (var vec in vectors)
            {
                for (int i = 0; i < Math.Min(vec.Length, result.Length); i++)
                    result[i] += vec[i];
            }
            
            float count = vectors.Length;
            for (int i = 0; i < result.Length; i++)
                result[i] /= count;
        }
    }
}
