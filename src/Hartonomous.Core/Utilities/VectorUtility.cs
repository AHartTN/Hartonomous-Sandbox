using System.Linq;
using Hartonomous.Data.Entities;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Utilities;

/// <summary>
/// Helper methods for working with vectors that are persisted via SQL Server 2025 <c>VECTOR</c> columns.
/// </summary>
public static class VectorUtility
{
    /// <summary>
    /// Maximum number of float32 components supported by SQL Server 2025 for a single VECTOR column.
    /// </summary>
    public const int SqlVectorMaxDimensions = 1998;

    /// <summary>
    /// Ensures the requested dimension can be represented in a SQL Server VECTOR column.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the dimension is less than or equal to zero.</exception>
    /// <exception cref="NotSupportedException">Thrown when the dimension exceeds <see cref="SqlVectorMaxDimensions"/>.</exception>
    public static void EnsureSupportedDimension(int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Vector dimension must be positive.");
        }

        if (dimension > SqlVectorMaxDimensions)
        {
            throw new NotSupportedException($"SQL Server VECTOR columns support at most {SqlVectorMaxDimensions} float32 components.");
        }
    }

    /// <summary>
    /// Pads the provided vector with zeros so its length matches <see cref="SqlVectorMaxDimensions"/>.
    /// </summary>
    public static float[] PadToSqlLength(ReadOnlySpan<float> source, out bool usedPadding)
    {
        EnsureSupportedDimension(source.Length);

        if (source.Length == SqlVectorMaxDimensions)
        {
            usedPadding = false;
            return source.ToArray();
        }

        var buffer = new float[SqlVectorMaxDimensions];
        source.CopyTo(buffer);
        usedPadding = true;
        return buffer;
    }

    /// <summary>
    /// Materialises a <see cref="SqlVector{T}"/> into a dense float array, trimming trailing padding based on the provided dimension.
    /// </summary>
    public static float[] Materialize(SqlVector<float> vector, int actualDimension)
    {
        if (vector.IsNull)
        {
            return Array.Empty<float>();
        }

        if (actualDimension <= 0)
        {
            actualDimension = Math.Min(vector.Length, SqlVectorMaxDimensions);
        }
        else
        {
            EnsureSupportedDimension(actualDimension);
        }

        var span = vector.Memory.Span;
        var length = Math.Min(actualDimension, span.Length);
        var result = new float[length];
        span.Slice(0, length).CopyTo(result);
        return result;
    }

    /// <summary>
    /// Builds a dense vector from persisted <see cref="AtomEmbeddingComponent"/> records.
    /// </summary>
    public static float[] MaterializeFromComponents(IEnumerable<AtomEmbeddingComponent> components, int expectedDimension)
    {
        var ordered = components
            .OrderBy(static c => c.ComponentIndex)
            .ToArray();

        var dimension = Math.Max(expectedDimension, ordered.Length == 0 ? 0 : ordered[^1].ComponentIndex + 1);
        if (dimension <= 0)
        {
            return Array.Empty<float>();
        }

        dimension = Math.Min(dimension, SqlVectorMaxDimensions);

        var target = new float[dimension];
        foreach (var component in ordered)
        {
            if ((uint)component.ComponentIndex >= (uint)target.Length)
            {
                continue;
            }

            target[component.ComponentIndex] = component.ComponentValue;
        }

        return target;
    }

    /// <summary>
    /// Computes cosine distance (1 - cosine similarity) between two dense vectors.
    /// </summary>
    public static double ComputeCosineDistance(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0)
        {
            return 1d;
        }

        double dot = 0d;
        double normLeft = 0d;
        double normRight = 0d;

        for (var i = 0; i < length; i++)
        {
            var l = left[i];
            var r = right[i];
            dot += l * r;
            normLeft += l * l;
            normRight += r * r;
        }

        if (normLeft == 0d || normRight == 0d)
        {
            return 1d;
        }

        var similarity = dot / (Math.Sqrt(normLeft) * Math.Sqrt(normRight));
        similarity = Math.Clamp(similarity, -1d, 1d);
        return 1d - similarity;
    }
}
