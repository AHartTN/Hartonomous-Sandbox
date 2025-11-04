using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hartonomous.Core.Performance;

/// <summary>
/// High-performance string operations using ReadOnlySpan<char>.
/// Zero-allocation parsing and transformations.
/// </summary>
public static class StringUtilities
{
    /// <summary>
    /// Parse delimited string into float array (zero allocation version).
    /// Common format: "1.0,2.0,3.0,4.0"
    /// </summary>
    public static bool TryParseDelimited(
        ReadOnlySpan<char> input,
        char delimiter,
        Span<float> destination,
        out int elementsParsed)
    {
        elementsParsed = 0;
        if (input.IsEmpty) return false;

        int start = 0;
        int destIdx = 0;

        for (int i = 0; i <= input.Length && destIdx < destination.Length; i++)
        {
            if (i == input.Length || input[i] == delimiter)
            {
                if (i > start)
                {
                    var segment = input.Slice(start, i - start);
                    if (!float.TryParse(segment, out destination[destIdx]))
                        return false;
                    destIdx++;
                }
                start = i + 1;
            }
        }

        elementsParsed = destIdx;
        return destIdx > 0;
    }

    /// <summary>
    /// Parse delimited string allocating new array.
    /// </summary>
    public static float[]? ParseDelimited(ReadOnlySpan<char> input, char delimiter = ',')
    {
        if (input.IsEmpty) return null;

        // Count delimiters to size array
        int count = 1;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == delimiter) count++;
        }

        var result = new float[count];
        if (TryParseDelimited(input, delimiter, result, out int parsed) && parsed == count)
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Format float array to delimited string using pooled StringBuilder.
    /// </summary>
    public static string FormatDelimited(ReadOnlySpan<float> values, char delimiter = ',', int precision = 6)
    {
        if (values.IsEmpty) return string.Empty;

        using var builder = new PooledStringBuilder(values.Length * 10);
        char[] buffer = ArrayPool<char>.Shared.Rent(32);

        try
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) builder.Append(delimiter);

                if (values[i].TryFormat(buffer, out int charsWritten, $"G{precision}"))
                {
                    builder.Append(buffer.AsSpan(0, charsWritten));
                }
                else
                {
                    builder.Append(values[i].ToString($"G{precision}"));
                }
            }

            return builder.ToString();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Case-insensitive equality check without allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCase(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive contains check without allocation.
    /// </summary>
    public static bool ContainsIgnoreCase(ReadOnlySpan<char> haystack, ReadOnlySpan<char> needle)
    {
        if (needle.IsEmpty) return true;
        if (haystack.Length < needle.Length) return false;

        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.Slice(i, needle.Length).Equals(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Split string on delimiter without allocation.
    /// Returns enumerable of ReadOnlySpan slices.
    /// </summary>
    public static SpanSplitEnumerator Split(ReadOnlySpan<char> input, char delimiter)
    {
        return new SpanSplitEnumerator(input, delimiter);
    }

    /// <summary>
    /// Trim whitespace from ReadOnlySpan without allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> Trim(ReadOnlySpan<char> input)
    {
        return input.Trim();
    }

    /// <summary>
    /// Join string values with delimiter using pooled StringBuilder.
    /// </summary>
    public static string Join(char delimiter, IEnumerable<string> parts)
    {
        if (!parts.Any()) return string.Empty;

        using var builder = new PooledStringBuilder(parts.Sum(p => p.Length) + parts.Count() - 1);

        bool first = true;
        foreach (var part in parts)
        {
            if (!first) builder.Append(delimiter);
            builder.Append(part);
            first = false;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Join ReadOnlySpan values with delimiter using pooled StringBuilder.
    /// </summary>
    public static string JoinSpans(char delimiter, ReadOnlySpan<char> part1, ReadOnlySpan<char> part2)
    {
        using var builder = new PooledStringBuilder(part1.Length + part2.Length + 1);
        builder.Append(part1);
        builder.Append(delimiter);
        builder.Append(part2);
        return builder.ToString();
    }

    /// <summary>
    /// Convert string to lowercase without allocation (uses stackalloc for small strings).
    /// </summary>
    public static string ToLowerFast(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty) return string.Empty;

        // Small strings: use stack
        if (input.Length <= 256)
        {
            Span<char> buffer = stackalloc char[input.Length];
            input.ToLowerInvariant(buffer);
            return new string(buffer);
        }

        // Large strings: rent from pool
        char[] rented = ArrayPool<char>.Shared.Rent(input.Length);
        try
        {
            input.ToLowerInvariant(rented);
            return new string(rented, 0, input.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Convert string to uppercase without allocation (uses stackalloc for small strings).
    /// </summary>
    public static string ToUpperFast(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty) return string.Empty;

        if (input.Length <= 256)
        {
            Span<char> buffer = stackalloc char[input.Length];
            input.ToUpperInvariant(buffer);
            return new string(buffer);
        }

        char[] rented = ArrayPool<char>.Shared.Rent(input.Length);
        try
        {
            input.ToUpperInvariant(rented);
            return new string(rented, 0, input.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Tokenize text on whitespace without allocation.
    /// Returns enumerable of word spans.
    /// </summary>
    public static SpanTokenEnumerator TokenizeOnWhitespace(ReadOnlySpan<char> input)
    {
        return new SpanTokenEnumerator(input);
    }

    /// <summary>
    /// Hash string to int32 using FNV-1a (for dictionary keys, bucketing).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFnv1aHashCode(ReadOnlySpan<char> input)
    {
        const int FnvPrime = 16777619;
        const int FnvOffsetBasis = unchecked((int)2166136261);

        int hash = FnvOffsetBasis;
        for (int i = 0; i < input.Length; i++)
        {
            hash ^= input[i];
            hash *= FnvPrime;
        }
        return hash;
    }

    /// <summary>
    /// Compare spans for sorting (ordinal).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        return a.CompareTo(b, StringComparison.Ordinal);
    }
}

/// <summary>
/// Enumerator for splitting spans without allocation.
/// </summary>
public ref struct SpanSplitEnumerator
{
    private ReadOnlySpan<char> _remaining;
    private readonly char _delimiter;
    private ReadOnlySpan<char> _current;

    public SpanSplitEnumerator(ReadOnlySpan<char> input, char delimiter)
    {
        _remaining = input;
        _delimiter = delimiter;
        _current = default;
    }

    public readonly SpanSplitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_remaining.IsEmpty)
        {
            _current = default;
            return false;
        }

        int idx = _remaining.IndexOf(_delimiter);
        if (idx >= 0)
        {
            _current = _remaining.Slice(0, idx);
            _remaining = _remaining.Slice(idx + 1);
        }
        else
        {
            _current = _remaining;
            _remaining = default;
        }

        return true;
    }

    public readonly ReadOnlySpan<char> Current => _current;
}

/// <summary>
/// Enumerator for tokenizing on whitespace without allocation.
/// </summary>
public ref struct SpanTokenEnumerator
{
    private ReadOnlySpan<char> _remaining;
    private ReadOnlySpan<char> _current;

    public SpanTokenEnumerator(ReadOnlySpan<char> input)
    {
        _remaining = input;
        _current = default;
    }

    public readonly SpanTokenEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        while (!_remaining.IsEmpty)
        {
            // Skip leading whitespace
            int start = 0;
            while (start < _remaining.Length && char.IsWhiteSpace(_remaining[start]))
            {
                start++;
            }

            if (start == _remaining.Length)
            {
                _remaining = default;
                _current = default;
                return false;
            }

            // Find end of token
            int end = start;
            while (end < _remaining.Length && !char.IsWhiteSpace(_remaining[end]))
            {
                end++;
            }

            _current = _remaining.Slice(start, end - start);
            _remaining = _remaining.Slice(end);
            return true;
        }

        _current = default;
        return false;
    }

    public readonly ReadOnlySpan<char> Current => _current;
}
