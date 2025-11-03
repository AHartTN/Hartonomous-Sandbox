using System;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Utilities;

/// <summary>
/// Extension helpers for zero-copy interop with SQL Server <see cref="SqlVector{T}"/> values.
/// </summary>
public static class SqlVectorExtensions
{
    /// <summary>
    /// Returns a live <see cref="ReadOnlySpan{T}"/> projection over the vector without copying.
    /// </summary>
    public static ReadOnlySpan<float> AsReadOnlySpan(this in SqlVector<float> vector, int? length = null)
    {
        if (vector.IsNull)
        {
            return ReadOnlySpan<float>.Empty;
        }

        var memory = vector.Memory;
        var span = memory.Span;
        if (length.HasValue)
        {
            var effectiveLength = Math.Min(length.Value, span.Length);
            return span[..effectiveLength];
        }

        return span;
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlyMemory{T}"/> projection over the vector, truncated when requested.
    /// </summary>
    public static ReadOnlyMemory<float> AsReadOnlyMemory(this in SqlVector<float> vector, int? length = null)
    {
        if (vector.IsNull)
        {
            return ReadOnlyMemory<float>.Empty;
        }

        if (!length.HasValue)
        {
            return vector.Memory;
        }

        var memory = vector.Memory;
        var effectiveLength = Math.Min(length.Value, memory.Length);
        return memory[..effectiveLength];
    }

    /// <summary>
    /// Wraps a float array as a SQL vector without copying.
    /// </summary>
    public static SqlVector<float> ToSqlVector(this float[] source, bool validateDimension = true)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (validateDimension)
        {
            VectorUtility.EnsureSupportedDimension(source.Length);
        }

        return new SqlVector<float>(source);
    }

    /// <summary>
    /// Wraps a read-only memory buffer as a SQL vector without copying.
    /// </summary>
    public static SqlVector<float> ToSqlVector(this ReadOnlyMemory<float> source, bool validateDimension = true)
    {
        if (validateDimension)
        {
            VectorUtility.EnsureSupportedDimension(source.Length);
        }

        return new SqlVector<float>(source);
    }
}
