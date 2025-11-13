using System;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// High-performance vector operations using CPU SIMD/AVX acceleration via System.Numerics.Vectors.
    /// Production-ready for SQL Server CLR with .NET Framework 4.8.1.
    /// </summary>
    public static class GpuAccelerator
    {
        /// <summary>
        /// Returns false - GPU acceleration not implemented (CPU SIMD/AVX used instead).
        /// </summary>
        public static bool IsGpuAvailable => false;

        /// <summary>
        /// Gets the device type (CPU-SIMD using System.Numerics.Vectors).
        /// </summary>
        public static string DeviceType => "CPU-SIMD";

        /// <summary>
        /// Computes dot product using CPU SIMD/AVX acceleration.
        /// </summary>
        public static float DotProduct(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            return VectorMath.DotProduct(a, b);
        }

        /// <summary>
        /// Computes cosine similarity using CPU SIMD/AVX acceleration.
        /// </summary>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            return VectorMath.CosineSimilarity(a, b);
        }

        /// <summary>
        /// Computes Euclidean distance using CPU SIMD/AVX acceleration.
        /// </summary>
        public static float EuclideanDistance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            return VectorMath.EuclideanDistance(a, b);
        }
    }
}
