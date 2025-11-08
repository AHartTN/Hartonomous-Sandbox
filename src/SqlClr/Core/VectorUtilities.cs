using System;
using System.Linq;

namespace SqlClrFunctions.Core;

/// <summary>
/// Shared vector utility methods for SQL CLR aggregates.
/// Eliminates duplication across 9+ aggregate files (300+ lines).
/// </summary>
/// <remarks>
/// This class consolidates:
/// - ParseVectorJson (duplicated in 9 files)
/// - CosineSimilarity (duplicated in 4 files)
/// - EuclideanDistance (duplicated in 5 files)
///
/// Making these internal static methods accessible to all aggregates in SqlClrFunctions namespace.
/// </remarks>
internal static class VectorUtilities
{
    /// <summary>
    /// Parses a JSON array string into a float array.
    /// Expected format: "[1.0, 2.5, 3.7, ...]"
    /// </summary>
    /// <param name="json">JSON array string representation.</param>
    /// <returns>Parsed float array, or null if parsing fails.</returns>
    /// <remarks>
    /// This method is used across 9 SQL CLR aggregate files for vector deserialization.
    /// Uses bridge library for robust JSON parsing with System.Text.Json.
    /// </remarks>
    internal static float[]? ParseVectorJson(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // Use bridge library for proper JSON parsing
            var serializer = new Hartonomous.Sql.Bridge.JsonProcessing.JsonSerializerImpl();
            return serializer.ParseFloatArray(json);
        }
        catch
        {
            // Return null on any parsing error (SQL CLR pattern)
            return null;
        }
    }

    /// <summary>
    /// Computes cosine similarity between two vectors.
    /// Returns value in range [-1, 1] where 1 = identical direction, 0 = orthogonal, -1 = opposite.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Cosine similarity score, or 0 if either vector has zero norm.</returns>
    /// <remarks>
    /// Formula: cos(θ) = (A · B) / (||A|| * ||B||)
    /// Used in: TimeSeriesVectorAggregates, RecommenderAggregates, ReasoningFrameworkAggregates, GraphVectorAggregates
    /// </remarks>
    internal static double CosineSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0)
            return 0;

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        int minLength = Math.Min(a.Length, b.Length);

        for (int i = 0; i < minLength; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        // Handle remaining dimensions if vectors have different lengths
        for (int i = minLength; i < a.Length; i++)
            normA += a[i] * a[i];

        for (int i = minLength; i < b.Length; i++)
            normB += b[i] * b[i];

        // Avoid division by zero
        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>
    /// Computes Euclidean distance (L2 norm) between two vectors.
    /// Returns value >= 0 where 0 = identical vectors.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Euclidean distance.</returns>
    /// <remarks>
    /// Formula: sqrt(Σ(a[i] - b[i])²)
    /// Used in: ReasoningFrameworkAggregates, TimeSeriesVectorAggregates, AnomalyDetectionAggregates,
    ///          GraphVectorAggregates, AdvancedVectorAggregates
    /// </remarks>
    internal static double EuclideanDistance(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0)
            return double.MaxValue;

        double sum = 0;
        int minLength = Math.Min(a.Length, b.Length);

        for (int i = 0; i < minLength; i++)
        {
            double diff = a[i] - b[i];
            sum += diff * diff;
        }

        // Handle remaining dimensions if vectors have different lengths
        // (treat missing dimensions as 0)
        for (int i = minLength; i < a.Length; i++)
            sum += a[i] * a[i];

        for (int i = minLength; i < b.Length; i++)
            sum += b[i] * b[i];

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Computes Manhattan distance (L1 norm) between two vectors.
    /// Returns value >= 0 where 0 = identical vectors.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Manhattan distance.</returns>
    /// <remarks>
    /// Formula: Σ|a[i] - b[i]|
    /// Useful for high-dimensional spaces where Euclidean distance can be less meaningful.
    /// </remarks>
    internal static double ManhattanDistance(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0)
            return double.MaxValue;

        double sum = 0;
        int minLength = Math.Min(a.Length, b.Length);

        for (int i = 0; i < minLength; i++)
        {
            sum += Math.Abs(a[i] - b[i]);
        }

        // Handle remaining dimensions
        for (int i = minLength; i < a.Length; i++)
            sum += Math.Abs(a[i]);

        for (int i = minLength; i < b.Length; i++)
            sum += Math.Abs(b[i]);

        return sum;
    }

    /// <summary>
    /// Computes dot product between two vectors.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Dot product value.</returns>
    /// <remarks>
    /// Formula: Σ(a[i] * b[i])
    /// Note: This is not normalized. For similarity use CosineSimilarity instead.
    /// </remarks>
    internal static double DotProduct(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0)
            return 0;

        double sum = 0;
        int minLength = Math.Min(a.Length, b.Length);

        for (int i = 0; i < minLength; i++)
        {
            sum += a[i] * b[i];
        }

        return sum;
    }

    /// <summary>
    /// Computes L2 norm (magnitude) of a vector.
    /// </summary>
    /// <param name="vector">Input vector.</param>
    /// <returns>L2 norm (Euclidean length).</returns>
    internal static double Norm(float[] vector)
    {
        if (vector == null || vector.Length == 0)
            return 0;

        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sum += vector[i] * vector[i];
        }

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Normalizes a vector to unit length (L2 normalization).
    /// </summary>
    /// <param name="vector">Vector to normalize.</param>
    /// <returns>Normalized vector, or original if norm is zero.</returns>
    internal static float[] Normalize(float[] vector)
    {
        if (vector == null || vector.Length == 0)
            return vector;

        double norm = Norm(vector);
        if (norm == 0)
            return vector;

        var result = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = (float)(vector[i] / norm);
        }

        return result;
    }

    /// <summary>
    /// Validates that two vectors have the same dimensionality.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>True if both vectors are non-null and have same length.</returns>
    internal static bool ValidateSameDimension(float[] a, float[] b)
    {
        return a != null && b != null && a.Length > 0 && a.Length == b.Length;
    }

    /// <summary>
    /// Validates that a vector is not null and has expected dimension.
    /// </summary>
    /// <param name="vector">Vector to validate.</param>
    /// <param name="expectedDimension">Expected dimension (0 means any non-zero dimension is valid).</param>
    /// <returns>True if vector is valid.</returns>
    internal static bool ValidateVector(float[] vector, int expectedDimension = 0)
    {
        if (vector == null || vector.Length == 0)
            return false;

        if (expectedDimension > 0 && vector.Length != expectedDimension)
            return false;

        return true;
    }

    /// <summary>
    /// Parses a WKT POINT string into a tuple of (X, Y) coordinates.
    /// Expected format: "POINT(x y)" or "POINT (x y)"
    /// </summary>
    /// <param name="wkt">WKT POINT string.</param>
    /// <returns>Tuple of (X, Y) or null if parsing fails.</returns>
    internal static (double X, double Y)? ParsePointWkt(string wkt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(wkt))
                return null;

            wkt = wkt.Trim().ToUpperInvariant();
            
            if (!wkt.StartsWith("POINT"))
                return null;

            int startIdx = wkt.IndexOf('(');
            int endIdx = wkt.IndexOf(')');
            
            if (startIdx == -1 || endIdx == -1 || endIdx <= startIdx)
                return null;

            string coords = wkt.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
            string[] parts = coords.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return null;

            if (double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y))
                return (x, y);

            return null;
        }
        catch
        {
            return null;
        }
    }
}
