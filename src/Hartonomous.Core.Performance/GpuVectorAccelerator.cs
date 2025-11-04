using System.Runtime.CompilerServices;

namespace Hartonomous.Core.Performance;

/// <summary>
/// GPU acceleration interface for vector operations.
/// Simplified implementation that can be extended with ILGPU when needed.
/// For now, provides CPU-SIMD fallback using VectorMath.
/// </summary>
public sealed class GpuVectorAccelerator
{
    private static readonly Lazy<GpuVectorAccelerator> _instance = new(() => new GpuVectorAccelerator());
    public static GpuVectorAccelerator Instance => _instance.Value;

    private GpuVectorAccelerator()
    {
        // Future: Initialize ILGPU context and accelerator
        // For now, all operations use CPU SIMD via VectorMath
    }

    /// <summary>
    /// Compute batch cosine similarities.
    /// Currently uses CPU SIMD. GPU implementation TODO.
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
    /// Matrix multiplication.
    /// Currently uses CPU. GPU implementation TODO.
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
/// Strategy selector for CPU vs GPU execution.
/// </summary>
public static class GpuStrategySelector
{
    /// <summary>
    /// Determine if GPU should be used based on data size.
    /// Currently always returns false (CPU-only implementation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseGpu(int vectorCount, int dimension)
    {
        // GPU overhead ~50μs, so need enough work to amortize
        // Threshold: 100+ vectors for batch operations
        // Currently using CPU SIMD for all operations
        return false; // TODO: Enable when ILGPU kernels are implemented
    }

    /// <summary>
    /// Determine if GPU should be used for matrix operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseGpuForMatrix(int rows, int cols, int innerDim)
    {
        // GPU better for large matrices (>= 100K elements)
        // Currently using CPU for all operations
        return false; // TODO: Enable when ILGPU kernels are implemented
    }
}
