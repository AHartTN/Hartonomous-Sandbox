using System;
// ILGPU removed - using SIMD/AVX CPU-only optimizations
// using ILGPU;
// using ILGPU.Runtime;
// using ILGPU.Runtime.Cuda;
// using ILGPU.Runtime.CPU;
// using ILGPU.Runtime.OpenCL;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// GPU acceleration removed - now using SIMD/AVX CPU optimizations via VectorMath.
    /// All operations fall through to VectorMath for hardware intrinsics (AVX/AVX2/SSE).
    /// </summary>
    public static class GpuAccelerator
    {
        // GPU removed - always use CPU SIMD
        public static bool IsGpuAvailable => false;

        /// <summary>
        /// Gets the device type (always CPU now).
        /// </summary>
        public static string DeviceType => "CPU-SIMD";

        /// <summary>
        /// Computes dot product using SIMD/AVX CPU code.
        /// </summary>
        public static float DotProduct(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            // Use SIMD CPU implementation
            return VectorMath.DotProduct(a, b);
        }

        /// <summary>
        /// Computes cosine similarity using SIMD/AVX CPU code.
        /// </summary>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            // Use SIMD CPU implementation
            return VectorMath.CosineSimilarity(a, b);
        }

        /// <summary>
        /// Computes Euclidean distance using SIMD/AVX CPU code.
        /// </summary>
        public static float EuclideanDistance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            // Use SIMD CPU implementation
            return VectorMath.EuclideanDistance(a, b);
        }

        /// <summary>
        /// No GPU resources to cleanup.
        /// </summary>
        public static void Cleanup()
        {
            // No-op - no GPU resources
        }
    }
}
