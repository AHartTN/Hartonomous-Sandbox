using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.OpenCL;

namespace SqlClrFunctions.Core
{
    /// <summary>
    /// GPU acceleration for .NET Framework 4.8.1 SQL CLR using ILGPU 0.8.0.
    /// Falls back to CPU SIMD code in VectorMath if GPU initialization fails.
    /// </summary>
    public static class GpuAccelerator
    {
        private static readonly Lazy<(Context context, Accelerator device, bool isGpuAvailable)> _lazyGpu
            = new Lazy<(Context, Accelerator, bool)>(InitializeGpu);

        private static Context GpuContext => _lazyGpu.Value.context;
        private static Accelerator GpuDevice => _lazyGpu.Value.device;
        public static bool IsGpuAvailable => _lazyGpu.Value.isGpuAvailable;

        /// <summary>
        /// Gets the GPU device type (CUDA, OpenCL, or CPU fallback).
        /// </summary>
        public static string DeviceType
        {
            get
            {
                if (!IsGpuAvailable) return "CPU";
                if (GpuDevice is CudaAccelerator) return "CUDA";
                if (GpuDevice is CLAccelerator) return "OpenCL";
                return "CPU";
            }
        }

        private static (Context context, Accelerator device, bool isGpuAvailable) InitializeGpu()
        {
            try
            {
                var context = new Context();

                // Attempt CUDA first (highest performance)
                if (CudaAccelerator.CudaAccelerators.Length > 0)
                {
                    var cudaAccelerator = new CudaAccelerator(context);
                    return (context, cudaAccelerator, true);
                }

                // Fallback to OpenCL (broader hardware support)
                if (CLAccelerator.CLAccelerators.Length > 0)
                {
                    var clAcceleratorId = CLAccelerator.CLAccelerators[0];
                    var clAccelerator = new CLAccelerator(context, clAcceleratorId);
                    return (context, clAccelerator, true);
                }

                // No GPU available - use CPU accelerator
                var cpuAccelerator = new CPUAccelerator(context);
                return (context, cpuAccelerator, false);
            }
            catch
            {
                // GPU initialization failed - silent fallback to CPU
                try
                {
                    var context = new Context();
                    var cpuAccelerator = new CPUAccelerator(context);
                    return (context, cpuAccelerator, false);
                }
                catch
                {
                    return (null, null, false);
                }
            }
        }

        /// <summary>
        /// Computes dot product using GPU if available, otherwise falls back to VectorMath SIMD CPU code.
        /// </summary>
        public static float DotProduct(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            // Use GPU for large vectors (>1000 elements where GPU overhead is justified)
            if (IsGpuAvailable && a.Length > 1000)
            {
                return DotProductGpu(a, b);
            }

            // Fallback to existing SIMD CPU implementation
            return VectorMath.DotProduct(a, b);
        }

        /// <summary>
        /// Computes cosine similarity using GPU if available, otherwise falls back to VectorMath CPU code.
        /// </summary>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            // Use GPU for large vectors
            if (IsGpuAvailable && a.Length > 1000)
            {
                return CosineSimilarityGpu(a, b);
            }

            // Fallback to existing SIMD CPU implementation
            return VectorMath.CosineSimilarity(a, b);
        }

        /// <summary>
        /// Computes Euclidean distance using GPU if available, otherwise falls back to VectorMath CPU code.
        /// </summary>
        public static float EuclideanDistance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same dimension");

            // Use GPU for large vectors
            if (IsGpuAvailable && a.Length > 1000)
            {
                return EuclideanDistanceGpu(a, b);
            }

            // Fallback to existing SIMD CPU implementation
            return VectorMath.EuclideanDistance(a, b);
        }

        // GPU kernel implementations
        private static float DotProductGpu(float[] a, float[] b)
        {
            try
            {
                using (var bufferA = GpuDevice.Allocate<float>(a.Length))
                using (var bufferB = GpuDevice.Allocate<float>(b.Length))
                using (var bufferResult = GpuDevice.Allocate<float>(1))
                {
                    bufferA.CopyFrom(a, 0, 0, a.Length);
                    bufferB.CopyFrom(b, 0, 0, b.Length);

                    // Load kernel
                    var kernel = GpuDevice.LoadAutoGroupedStreamKernel<
                        Index1,
                        ArrayView<float>,
                        ArrayView<float>,
                        ArrayView<float>>(DotProductKernel);

                    // Execute
                    kernel(a.Length, bufferA.View, bufferB.View, bufferResult.View);
                    GpuDevice.Synchronize();

                    // Retrieve result
                    var result = new float[1];
                    bufferResult.CopyTo(result, 0, 0, 1);
                    return result[0];
                }
            }
            catch
            {
                // GPU execution failed - fallback to CPU
                return VectorMath.DotProduct(a, b);
            }
        }

        private static float CosineSimilarityGpu(float[] a, float[] b)
        {
            try
            {
                float dotProduct = DotProductGpu(a, b);
                float normA = NormGpu(a);
                float normB = NormGpu(b);

                if (normA == 0 || normB == 0)
                    return 0;

                return dotProduct / (normA * normB);
            }
            catch
            {
                return VectorMath.CosineSimilarity(a, b);
            }
        }

        private static float EuclideanDistanceGpu(float[] a, float[] b)
        {
            try
            {
                using (var bufferA = GpuDevice.Allocate<float>(a.Length))
                using (var bufferB = GpuDevice.Allocate<float>(b.Length))
                using (var bufferResult = GpuDevice.Allocate<float>(1))
                {
                    bufferA.CopyFrom(a, 0, 0, a.Length);
                    bufferB.CopyFrom(b, 0, 0, b.Length);

                    var kernel = GpuDevice.LoadAutoGroupedStreamKernel<
                        Index1,
                        ArrayView<float>,
                        ArrayView<float>,
                        ArrayView<float>>(EuclideanDistanceKernel);

                    kernel(a.Length, bufferA.View, bufferB.View, bufferResult.View);
                    GpuDevice.Synchronize();

                    var result = new float[1];
                    bufferResult.CopyTo(result, 0, 0, 1);
                    return (float)Math.Sqrt(result[0]);
                }
            }
            catch
            {
                return VectorMath.EuclideanDistance(a, b);
            }
        }

        private static float NormGpu(float[] a)
        {
            try
            {
                using (var buffer = GpuDevice.Allocate<float>(a.Length))
                using (var bufferResult = GpuDevice.Allocate<float>(1))
                {
                    buffer.CopyFrom(a, 0, 0, a.Length);

                    var kernel = GpuDevice.LoadAutoGroupedStreamKernel<
                        Index1,
                        ArrayView<float>,
                        ArrayView<float>>(NormKernel);

                    kernel(a.Length, buffer.View, bufferResult.View);
                    GpuDevice.Synchronize();

                    var result = new float[1];
                    bufferResult.CopyTo(result, 0, 0, 1);
                    return (float)Math.Sqrt(result[0]);
                }
            }
            catch
            {
                return VectorMath.Norm(a);
            }
        }

        // ILGPU kernels (executed on GPU)
        private static void DotProductKernel(
            Index1 index,
            ArrayView<float> a,
            ArrayView<float> b,
            ArrayView<float> result)
        {
            var temp = a[index] * b[index];
            Atomic.Add(ref result[0], temp);
        }

        private static void EuclideanDistanceKernel(
            Index1 index,
            ArrayView<float> a,
            ArrayView<float> b,
            ArrayView<float> result)
        {
            var diff = a[index] - b[index];
            Atomic.Add(ref result[0], diff * diff);
        }

        private static void NormKernel(
            Index1 index,
            ArrayView<float> a,
            ArrayView<float> result)
        {
            Atomic.Add(ref result[0], a[index] * a[index]);
        }

        /// <summary>
        /// Disposes GPU resources (call on application shutdown).
        /// </summary>
        public static void Cleanup()
        {
            if (_lazyGpu.IsValueCreated && IsGpuAvailable)
            {
                GpuDevice?.Dispose();
                GpuContext?.Dispose();
            }
        }
    }
}
