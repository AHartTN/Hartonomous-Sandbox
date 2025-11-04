using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Hartonomous.Core.Performance;

/// <summary>
/// Common SIMD patterns for data processing.
/// Provides vectorized sum, min, max, clamp operations.
/// </summary>
public static class SimdHelpers
{
    /// <summary>
    /// Compute sum of all elements using SIMD.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sum(ReadOnlySpan<float> values)
    {
        if (values.IsEmpty) return 0f;

        int i = 0;
        float sum = 0f;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            Vector256<float> vsum = Vector256<float>.Zero;
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        vsum = Avx.Add(vsum, v);
                    }

                    // Horizontal sum
                    vsum = Avx.HorizontalAdd(vsum, vsum);
                    vsum = Avx.HorizontalAdd(vsum, vsum);
                    sum = vsum.GetElement(0) + vsum.GetElement(4);
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && values.Length >= 4)
        {
            Vector128<float> vsum = Vector128<float>.Zero;
            int simdLength = values.Length & ~3;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        vsum = Sse.Add(vsum, v);
                    }

                    // Horizontal sum
                    vsum = Sse3.HorizontalAdd(vsum, vsum);
                    vsum = Sse3.HorizontalAdd(vsum, vsum);
                    sum = vsum.ToScalar();
                }
            }
        }
        // System.Numerics.Vector fallback
        else if (Vector.IsHardwareAccelerated && values.Length >= Vector<float>.Count)
        {
            var vsum = Vector<float>.Zero;
            int simdLength = values.Length - values.Length % Vector<float>.Count;

            for (; i < simdLength; i += Vector<float>.Count)
            {
                var v = new Vector<float>(values.Slice(i, Vector<float>.Count));
                vsum += v;
            }

            for (int j = 0; j < Vector<float>.Count; j++)
            {
                sum += vsum[j];
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum;
    }

    /// <summary>
    /// Find minimum value using SIMD.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(ReadOnlySpan<float> values)
    {
        if (values.IsEmpty) return float.MaxValue;

        int i = 0;
        float min = float.MaxValue;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            Vector256<float> vmin = Vector256.Create(float.MaxValue);
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        vmin = Avx.Min(vmin, v);
                    }

                    // Extract minimum from vector
                    Span<float> temp = stackalloc float[8];
                    Avx.Store((float*)Unsafe.AsPointer(ref temp[0]), vmin);
                    min = temp[0];
                    for (int j = 1; j < 8; j++)
                    {
                        if (temp[j] < min) min = temp[j];
                    }
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && values.Length >= 4)
        {
            Vector128<float> vmin = Vector128.Create(float.MaxValue);
            int simdLength = values.Length & ~3;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        vmin = Sse.Min(vmin, v);
                    }

                    Span<float> temp = stackalloc float[4];
                    Sse.Store((float*)Unsafe.AsPointer(ref temp[0]), vmin);
                    min = temp[0];
                    for (int j = 1; j < 4; j++)
                    {
                        if (temp[j] < min) min = temp[j];
                    }
                }
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            if (values[i] < min) min = values[i];
        }

        return min;
    }

    /// <summary>
    /// Find maximum value using SIMD.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(ReadOnlySpan<float> values)
    {
        if (values.IsEmpty) return float.MinValue;

        int i = 0;
        float max = float.MinValue;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            Vector256<float> vmax = Vector256.Create(float.MinValue);
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        vmax = Avx.Max(vmax, v);
                    }

                    Span<float> temp = stackalloc float[8];
                    Avx.Store((float*)Unsafe.AsPointer(ref temp[0]), vmax);
                    max = temp[0];
                    for (int j = 1; j < 8; j++)
                    {
                        if (temp[j] > max) max = temp[j];
                    }
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && values.Length >= 4)
        {
            Vector128<float> vmax = Vector128.Create(float.MinValue);
            int simdLength = values.Length & ~3;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        vmax = Sse.Max(vmax, v);
                    }

                    Span<float> temp = stackalloc float[4];
                    Sse.Store((float*)Unsafe.AsPointer(ref temp[0]), vmax);
                    max = temp[0];
                    for (int j = 1; j < 4; j++)
                    {
                        if (temp[j] > max) max = temp[j];
                    }
                }
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            if (values[i] > max) max = values[i];
        }

        return max;
    }

    /// <summary>
    /// Clamp all values to [min, max] range using SIMD.
    /// Modifies values in place.
    /// </summary>
    public static void Clamp(Span<float> values, float min, float max)
    {
        if (values.IsEmpty) return;

        int i = 0;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            var vmin = Vector256.Create(min);
            var vmax = Vector256.Create(max);
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        v = Avx.Max(vmin, v);
                        v = Avx.Min(vmax, v);
                        Avx.Store(ptr + i, v);
                    }
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && values.Length >= 4)
        {
            var vmin = Vector128.Create(min);
            var vmax = Vector128.Create(max);
            int simdLength = values.Length & ~3;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        v = Sse.Max(vmin, v);
                        v = Sse.Min(vmax, v);
                        Sse.Store(ptr + i, v);
                    }
                }
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            if (values[i] < min) values[i] = min;
            else if (values[i] > max) values[i] = max;
        }
    }

    /// <summary>
    /// Scale all values by a constant using SIMD.
    /// Modifies values in place.
    /// </summary>
    public static void Scale(Span<float> values, float scalar)
    {
        if (values.IsEmpty) return;

        int i = 0;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            var vscalar = Vector256.Create(scalar);
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        v = Avx.Multiply(v, vscalar);
                        Avx.Store(ptr + i, v);
                    }
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && values.Length >= 4)
        {
            var vscalar = Vector128.Create(scalar);
            int simdLength = values.Length & ~3;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        v = Sse.Multiply(v, vscalar);
                        Sse.Store(ptr + i, v);
                    }
                }
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            values[i] *= scalar;
        }
    }

    /// <summary>
    /// Add constant to all values using SIMD.
    /// Modifies values in place.
    /// </summary>
    public static void AddConstant(Span<float> values, float constant)
    {
        if (values.IsEmpty) return;

        int i = 0;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            var vconstant = Vector256.Create(constant);
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        v = Avx.Add(v, vconstant);
                        Avx.Store(ptr + i, v);
                    }
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && values.Length >= 4)
        {
            var vconstant = Vector128.Create(constant);
            int simdLength = values.Length & ~3;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        v = Sse.Add(v, vconstant);
                        Sse.Store(ptr + i, v);
                    }
                }
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            values[i] += constant;
        }
    }

    /// <summary>
    /// Element-wise addition of two vectors using SIMD.
    /// result = a + b
    /// </summary>
    public static void AddVectors(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
    {
        if (a.Length != b.Length || a.Length != result.Length)
            throw new ArgumentException("All spans must have the same length");

        int i = 0;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && a.Length >= 8)
        {
            int simdLength = a.Length & ~7;

            unsafe
            {
                fixed (float* ptrA = a)
                fixed (float* ptrB = b)
                fixed (float* ptrResult = result)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var va = Avx.LoadVector256(ptrA + i);
                        var vb = Avx.LoadVector256(ptrB + i);
                        var vr = Avx.Add(va, vb);
                        Avx.Store(ptrResult + i, vr);
                    }
                }
            }
        }
        // SSE: 4 floats at a time
        else if (Sse.IsSupported && a.Length >= 4)
        {
            int simdLength = a.Length & ~3;

            unsafe
            {
                fixed (float* ptrA = a)
                fixed (float* ptrB = b)
                fixed (float* ptrResult = result)
                {
                    for (; i < simdLength; i += 4)
                    {
                        var va = Sse.LoadVector128(ptrA + i);
                        var vb = Sse.LoadVector128(ptrB + i);
                        var vr = Sse.Add(va, vb);
                        Sse.Store(ptrResult + i, vr);
                    }
                }
            }
        }

        // Scalar remainder
        for (; i < a.Length; i++)
        {
            result[i] = a[i] + b[i];
        }
    }

    /// <summary>
    /// Compute mean and standard deviation in single pass using SIMD.
    /// </summary>
    public static (float mean, float stdDev) ComputeStatistics(ReadOnlySpan<float> values)
    {
        if (values.IsEmpty) return (0f, 0f);

        float sum = Sum(values);
        float mean = sum / values.Length;

        // Compute variance
        int i = 0;
        float variance = 0f;

        // AVX: 8 floats at a time
        if (Avx.IsSupported && values.Length >= 8)
        {
            var vmean = Vector256.Create(mean);
            var vvariance = Vector256<float>.Zero;
            int simdLength = values.Length & ~7;

            unsafe
            {
                fixed (float* ptr = values)
                {
                    for (; i < simdLength; i += 8)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        var diff = Avx.Subtract(v, vmean);
                        var squared = Avx.Multiply(diff, diff);
                        vvariance = Avx.Add(vvariance, squared);
                    }

                    // Sum variance vector
                    vvariance = Avx.HorizontalAdd(vvariance, vvariance);
                    vvariance = Avx.HorizontalAdd(vvariance, vvariance);
                    variance = vvariance.GetElement(0) + vvariance.GetElement(4);
                }
            }
        }

        // Scalar remainder
        for (; i < values.Length; i++)
        {
            float diff = values[i] - mean;
            variance += diff * diff;
        }

        float stdDev = MathF.Sqrt(variance / values.Length);
        return (mean, stdDev);
    }
}
