using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SqlClrFunctions.Core
{
    /// <summary>
    /// High-performance vector parsing and serialization for SQL CLR.
    /// Uses ArrayPool to minimize allocations.
    /// </summary>
    public static class VectorParser
    {
        private const int StackAllocThreshold = 256; // Use stackalloc for vectors smaller than this

        /// <summary>
        /// Parse JSON array string to float array.
        /// For small vectors, uses stackalloc. For large vectors, uses ArrayPool.
        /// Returns null if parsing fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] ParseVectorJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            json = json.Trim();
            if (json.Length < 2 || json[0] != '[' || json[json.Length - 1] != ']')
                return null;

            // Quick scan to count elements
            int commaCount = 0;
            for (int i = 1; i < json.Length - 1; i++)
            {
                if (json[i] == ',') commaCount++;
            }

            int elementCount = commaCount + 1;
            if (elementCount == 0) return null;

            try
            {
                var result = new float[elementCount];
                int resultIdx = 0;
                int startIdx = 1;

                for (int i = 1; i < json.Length; i++)
                {
                    if (json[i] == ',' || json[i] == ']')
                    {
                        if (i > startIdx)
                        {
                            var span = json.AsSpan(startIdx, i - startIdx);
                            if (!float.TryParse(span, out result[resultIdx]))
                                return null;
                            resultIdx++;
                        }
                        startIdx = i + 1;
                    }
                }

                return resultIdx == elementCount ? result : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse vector to span without allocation (caller provides destination).
        /// </summary>
        public static bool TryParseVectorJson(ReadOnlySpan<char> json, Span<float> destination, out int elementsParsed)
        {
            elementsParsed = 0;

            if (json.Length < 2 || json[0] != '[' || json[json.Length - 1] != ']')
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
        /// Format float array as JSON array string.
        /// Uses ArrayPool for temporary buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatVectorJson(ReadOnlySpan<float> vector, string format = "G9")
        {
            if (vector.Length == 0) return "[]";

            // Estimate size (conservative)
            int estimatedSize = 2 + (vector.Length * 16); // '[' + ']' + ~16 chars per float
            char[] buffer = ArrayPool<char>.Shared.Rent(estimatedSize);

            try
            {
                int pos = 0;
                buffer[pos++] = '[';

                for (int i = 0; i < vector.Length; i++)
                {
                    if (i > 0)
                        buffer[pos++] = ',';

                    string valueStr = vector[i].ToString(format);
                    valueStr.AsSpan().CopyTo(buffer.AsSpan(pos));
                    pos += valueStr.Length;

                    // Expand buffer if needed (rare)
                    if (pos > buffer.Length - 20)
                    {
                        char[] newBuffer = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
                        Array.Copy(buffer, newBuffer, pos);
                        ArrayPool<char>.Shared.Return(buffer);
                        buffer = newBuffer;
                    }
                }

                buffer[pos++] = ']';
                return new string(buffer, 0, pos);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Format multiple vectors as JSON array of arrays.
        /// </summary>
        public static string FormatVectorArrayJson(ReadOnlySpan<float[]> vectors, string format = "G9")
        {
            if (vectors.Length == 0) return "[]";

            var parts = new string[vectors.Length];
            for (int i = 0; i < vectors.Length; i++)
            {
                parts[i] = FormatVectorJson(vectors[i], format);
            }

            return "[" + string.Join(",", parts) + "]";
        }
    }

    /// <summary>
    /// JSON formatting utilities for aggregate results.
    /// </summary>
    public static class JsonFormatter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatValue(object value)
        {
            return value switch
            {
                null => "null",
                string s => $"\"{EscapeString(s)}\"",
                bool b => b.ToString().ToLowerInvariant(),
                double d => d.ToString("G9"),
                float f => f.ToString("G9"),
                int i => i.ToString(),
                long l => l.ToString(),
                _ => value.ToString()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            
            // Fast path: no escaping needed
            if (s.IndexOfAny(new[] { '"', '\\', '\n', '\r', '\t' }) == -1)
                return s;

            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        /// <summary>
        /// Build JSON object from key-value pairs.
        /// </summary>
        public static string BuildJsonObject(params (string Key, object Value)[] properties)
        {
            var parts = new string[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                parts[i] = $"\"{properties[i].Key}\":{FormatValue(properties[i].Value)}";
            }
            return "{" + string.Join(",", parts) + "}";
        }

        /// <summary>
        /// Build JSON array from values.
        /// </summary>
        public static string BuildJsonArray<T>(ReadOnlySpan<T> values)
        {
            if (values.Length == 0) return "[]";

            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                parts[i] = FormatValue(values[i]);
            }
            return "[" + string.Join(",", parts) + "]";
        }
    }
}
