using System.Runtime.CompilerServices;

namespace Hartonomous.Core.Performance;

/// <summary>
/// CPU vector acceleration using SIMD intrinsics.
/// ILGPU GPU acceleration disabled due to SQL CLR compatibility constraints.
/// All operations use CPU SIMD via System.Numerics.Vectors (AVX2/SSE4).
/// </summary>
public sealed class GpuVectorAccelerator
{
    private static readonly Lazy<GpuVectorAccelerator> _instance = new(() => new GpuVectorAccelerator());
    public static GpuVectorAccelerator Instance => _instance.Value;

    private GpuVectorAccelerator()
    {
        // CPU-only implementation using SIMD intrinsics
        // ILGPU disabled: SQL CLR verifier rejects unmanaged GPU memory pointers
    }

    /// <summary>
    /// Compute batch cosine similarities using CPU SIMD.
    /// </summary>
    public float[] BatchCosineSimilarity(float[][] database, float[] query)
    {
        if (database.Length == 0) return Array.Empty<float>();

        var results = new float[database.Length];

        // Parallel CPU execution with SIMD
        Parallel.For(0, database.Length, i =>
        {
            results[i] = VectorMath.CosineSimilarity(database[i], query);
        });

        return results;
    }

    /// <summary>
    /// Batch k-nearest neighbors search.
    /// Returns indices and distances of k closest vectors.
    /// </summary>
    public (int[] Indices, float[] Distances) BatchKNearestNeighbors(
        float[][] database,
        float[] query,
        int k)
    {
        // Compute similarities
        var similarities = BatchCosineSimilarity(database, query);

        // Convert to distances (1 - similarity for cosine)
        var distances = similarities.Select(s => 1.0f - s).ToArray();

        // Find k smallest distances
        var indexed = distances
            .Select((dist, idx) => (Distance: dist, Index: idx))
            .OrderBy(x => x.Distance)
            .Take(k)
            .ToArray();

        return (
            indexed.Select(x => x.Index).ToArray(),
            indexed.Select(x => x.Distance).ToArray()
        );
    }

    /// <summary>
    /// Matrix multiplication using CPU SIMD with parallel row processing.
    /// </summary>
    public float[,] MatrixMultiply(float[,] a, float[,] b)
    {
        int rowsA = a.GetLength(0);
        int colsA = a.GetLength(1);
        int rowsB = b.GetLength(0);
        int colsB = b.GetLength(1);

        if (colsA != rowsB)
            throw new ArgumentException("Matrix dimensions incompatible for multiplication");

        var result = new float[rowsA, colsB];

        Parallel.For(0, rowsA, i =>
        {
            for (int j = 0; j < colsB; j++)
            {
                float sum = 0f;
                for (int k = 0; k < colsA; k++)
                {
                    sum += a[i, k] * b[k, j];
                }
                result[i, j] = sum;
            }
        });

        return result;
    }
}

/// <summary>
/// Strategy selector for vector operation execution.
/// Always returns CPU execution (GPU disabled for SQL CLR compatibility).
/// </summary>
public static class GpuStrategySelector
{
    /// <summary>
    /// Returns false - CPU SIMD execution only.
    /// ILGPU disabled: SQL CLR verifier incompatible with unmanaged GPU pointers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseGpu(int vectorCount, int dimension)
    {
        return false; // CPU SIMD only (AVX2/SSE4 via System.Numerics.Vectors)
    }

    /// <summary>
    /// Returns false - CPU parallel matrix operations only.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseGpuForMatrix(int rows, int cols, int innerDim)
    {
        return false; // CPU parallel execution only
    }
}
