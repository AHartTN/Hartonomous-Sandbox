using System.Security.Cryptography;
using System.Text;

namespace Hartonomous.Core.Utilities;

/// <summary>
/// Static utility class for common vector operations.
/// Eliminates duplication across embedders and avoids repeated implementations.
/// </summary>
public static class VectorUtilities
{
    /// <summary>
    /// Normalizes a vector to unit length (L2 normalization).
    /// Required for cosine similarity to work correctly.
    /// </summary>
    /// <param name="vector">Vector to normalize (modified in-place)</param>
    public static void Normalize(float[] vector)
    {
        if (vector == null || vector.Length == 0)
            throw new ArgumentException("Vector cannot be null or empty", nameof(vector));

        var magnitude = L2Norm(vector);

        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }
    }

    /// <summary>
    /// Computes L2 norm (Euclidean length) of a vector.
    /// </summary>
    public static float L2Norm(float[] vector)
    {
        double sumOfSquares = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumOfSquares += vector[i] * vector[i];
        }
        return (float)Math.Sqrt(sumOfSquares);
    }

    /// <summary>
    /// Computes cosine similarity between two vectors.
    /// Assumes vectors are already normalized for performance.
    /// Range: [-1, 1] where 1 = identical, -1 = opposite, 0 = orthogonal
    /// </summary>
    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same dimension");

        float dotProduct = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
        }
        return dotProduct;
    }

    /// <summary>
    /// Computes Euclidean distance between two vectors.
    /// </summary>
    public static float EuclideanDistance(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same dimension");

        double sumOfSquares = 0;
        for (int i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            sumOfSquares += diff * diff;
        }
        return (float)Math.Sqrt(sumOfSquares);
    }

    /// <summary>
    /// Computes content hash of a vector for deduplication.
    /// Uses SHA256 for cryptographic-strength uniqueness.
    /// </summary>
    public static byte[] ComputeContentHash(float[] vector)
    {
        // Convert float array to byte array
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);

        // Compute SHA256 hash
        return SHA256.HashData(bytes);
    }

    /// <summary>
    /// Computes content hash of text for deduplication.
    /// Uses SHA256 on UTF8 encoding of the text.
    /// </summary>
    public static byte[] ComputeContentHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return SHA256.HashData(bytes);
    }

    /// <summary>
    /// Serializes a vector to JSON format for SQL Server VECTOR type.
    /// Format: "[1.0, 2.0, 3.0]"
    /// </summary>
    public static string SerializeToJson(float[] vector)
    {
        return "[" + string.Join(", ", vector.Select(v => v.ToString("G9"))) + "]";
    }

    /// <summary>
    /// Deserializes a vector from JSON format.
    /// </summary>
    public static float[] DeserializeFromJson(string json)
    {
        // Remove brackets and split
        var trimmed = json.Trim('[', ']', ' ');
        var parts = trimmed.Split(',');

        var result = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            result[i] = float.Parse(parts[i].Trim());
        }
        return result;
    }

    /// <summary>
    /// Validates that a vector has no NaN or Infinity values.
    /// </summary>
    public static bool IsValid(float[] vector)
    {
        for (int i = 0; i < vector.Length; i++)
        {
            if (float.IsNaN(vector[i]) || float.IsInfinity(vector[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Adds two vectors element-wise (v3 = v1 + v2).
    /// </summary>
    public static float[] Add(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same dimension");

        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] + b[i];
        }
        return result;
    }

    /// <summary>
    /// Subtracts two vectors element-wise (v3 = v1 - v2).
    /// </summary>
    public static float[] Subtract(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same dimension");

        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] - b[i];
        }
        return result;
    }

    /// <summary>
    /// Scales a vector by a scalar multiplier.
    /// </summary>
    public static float[] Scale(float[] vector, float scalar)
    {
        var result = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = vector[i] * scalar;
        }
        return result;
    }

    /// <summary>
    /// Computes the dot product of two vectors.
    /// </summary>
    public static float DotProduct(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same dimension");

        float sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }
        return sum;
    }

    /// <summary>
    /// Linear interpolation between two vectors.
    /// t=0 returns a, t=1 returns b, t=0.5 returns midpoint.
    /// </summary>
    public static float[] Lerp(float[] a, float[] b, float t)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same dimension");

        t = Math.Clamp(t, 0f, 1f);

        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] + (b[i] - a[i]) * t;
        }
        return result;
    }

    /// <summary>
    /// Converts a high-dimensional vector to hexadecimal string for logging/debugging.
    /// Shows first N and last N values to avoid overwhelming logs.
    /// </summary>
    public static string ToDebugString(float[] vector, int showCount = 5)
    {
        if (vector.Length <= showCount * 2)
        {
            return $"[{string.Join(", ", vector.Select(v => v.ToString("F4")))}]";
        }

        var first = vector.Take(showCount).Select(v => v.ToString("F4"));
        var last = vector.TakeLast(showCount).Select(v => v.ToString("F4"));

        return $"[{string.Join(", ", first)}, ... ({vector.Length - showCount * 2} more), {string.Join(", ", last)}]";
    }
}
