using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hartonomous.Core.Performance;

/// <summary>
/// High-performance JSON serialization using source generators.
/// Zero-allocation UTF-8 parsing for vectors and common types.
/// </summary>
public static class FastJson
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Parse JSON array to float vector.
    /// Optimized for common embedding formats: [1.0, 2.0, 3.0, ...]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[]? ParseFloatArray(ReadOnlySpan<char> json)
    {
        if (json.IsEmpty || json.Length < 2) return null;

        // Quick validation
        if (json[0] != '[' || json[^1] != ']') return null;

        // Count commas to estimate size
        int commaCount = 0;
        for (int i = 1; i < json.Length - 1; i++)
        {
            if (json[i] == ',') commaCount++;
        }

        int elementCount = commaCount + 1;
        var result = new float[elementCount];
        int resultIdx = 0;
        int startIdx = 1;

        for (int i = 1; i < json.Length; i++)
        {
            if (json[i] == ',' || json[i] == ']')
            {
                if (i > startIdx)
                {
                    var span = json.Slice(startIdx, i - startIdx);
                    if (!float.TryParse(span, out result[resultIdx]))
                        return null;
                    resultIdx++;
                }
                startIdx = i + 1;
            }
        }

        return resultIdx == elementCount ? result : null;
    }

    /// <summary>
    /// Parse JSON array directly into provided span (no allocation).
    /// </summary>
    public static bool TryParseFloatArray(ReadOnlySpan<char> json, Span<float> destination, out int elementsParsed)
    {
        elementsParsed = 0;

        if (json.Length < 2 || json[0] != '[' || json[^1] != ']')
            return false;

        int startIdx = 1;
        int destIdx = 0;

        for (int i = 1; i < json.Length && destIdx < destination.Length; i++)
        {
            if (json[i] == ',' || json[i] == ']')
            {
                if (i > startIdx)
                {
                    var span = json.Slice(startIdx, i - startIdx);
                    if (!float.TryParse(span, out destination[destIdx]))
                        return false;
                    destIdx++;
                }
                startIdx = i + 1;
            }
        }

        elementsParsed = destIdx;
        return destIdx > 0;
    }

    /// <summary>
    /// Format float array as JSON using pooled buffers.
    /// </summary>
    public static string FormatFloatArray(ReadOnlySpan<float> values, int precision = 6)
    {
        if (values.IsEmpty) return "[]";

        using var builder = new PooledStringBuilder(values.Length * 12);
        builder.Append('[');

        char[] buffer = ArrayPool<char>.Shared.Rent(32);
        try
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) builder.Append(',');

                // Format with specified precision
                if (values[i].TryFormat(buffer, out int charsWritten, $"G{precision}"))
                {
                    builder.Append(buffer.AsSpan(0, charsWritten));
                }
                else
                {
                    builder.Append(values[i].ToString($"G{precision}"));
                }
            }

            builder.Append(']');
            return builder.ToString();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Deserialize from UTF-8 bytes (avoids string allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<T>(utf8Json, DefaultOptions);
    }

    /// <summary>
    /// Serialize to UTF-8 bytes (use with Memory<byte> pools).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] SerializeToUtf8<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, DefaultOptions);
    }

    /// <summary>
    /// Serialize directly to stream (zero intermediate allocations).
    /// </summary>
    public static async ValueTask SerializeToStreamAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, value, DefaultOptions, cancellationToken);
    }
}

/// <summary>
/// JSON source generator context for common Hartonomous types.
/// Enables ahead-of-time serialization code generation.
/// </summary>
[JsonSerializable(typeof(EmbeddingSearchResultDto))]
[JsonSerializable(typeof(AtomDto))]
[JsonSerializable(typeof(VectorDto))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<float>))]
[JsonSerializable(typeof(float[]))]
public partial class HartonomousJsonContext : JsonSerializerContext
{
}

// DTOs for source generation
public sealed record EmbeddingSearchResultDto
{
    public long EmbeddingId { get; init; }
    public string? SourceText { get; init; }
    public float SimilarityScore { get; init; }
    public float Distance { get; init; }
}

public sealed record AtomDto
{
    public long AtomId { get; init; }
    public string? AtomType { get; init; }
    public string? Content { get; init; }
    public DateTime CreatedTimestamp { get; init; }
}

public sealed record VectorDto
{
    public float[]? Embedding { get; init; }
    public int Dimension { get; init; }
}
