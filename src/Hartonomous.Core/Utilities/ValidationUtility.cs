using System;

namespace Hartonomous.Core.Utilities;

/// <summary>
/// Common validation helpers to eliminate repeated validation boilerplate.
/// Provides consistent error messages and reduces code duplication across services.
/// </summary>
public static class ValidationUtility
{
    /// <summary>
    /// Validates that a parameter is not null.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static T NotNull<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return value;
    }

    /// <summary>
    /// Validates that a string parameter is not null or whitespace.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value is null or whitespace</exception>
    public static string NotNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{parameterName}' cannot be null or whitespace.", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Validates that a number is within a specified range.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside range</exception>
    public static T InRange<T>(T value, T min, T max, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"'{parameterName}' must be between {min} and {max}.");
        }
        return value;
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when collection is null or empty</exception>
    public static T NotNullOrEmpty<T>(T? collection, string parameterName)
        where T : System.Collections.ICollection
    {
        if (collection == null || collection.Count == 0)
        {
            throw new ArgumentException($"'{parameterName}' cannot be null or empty.", parameterName);
        }
        return collection;
    }

    /// <summary>
    /// Validates vector dimensions for embedding operations.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid</exception>
    public static int ValidateVectorDimensions(int dimensions, string parameterName = "dimensions")
    {
        if (dimensions <= 0)
        {
            throw new ArgumentException("Vector dimensions must be positive.", parameterName);
        }
        
        if (dimensions > 100000)
        {
            throw new ArgumentException(
                "Vector dimensions exceed maximum supported size of 100,000.",
                parameterName);
        }
        
        return dimensions;
    }

    /// <summary>
    /// Validates that a vector array matches expected dimensions.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when vector length doesn't match expected dimensions</exception>
    public static float[] ValidateVectorLength(float[]? vector, int expectedDimensions, string parameterName = "vector")
    {
        NotNull(vector, parameterName);
        
        if (vector!.Length != expectedDimensions)
        {
            throw new ArgumentException(
                $"Vector length ({vector.Length}) does not match expected dimensions ({expectedDimensions}).",
                parameterName);
        }
        
        return vector;
    }

    /// <summary>
    /// Validates that a GUID is not empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when GUID is empty</exception>
    public static Guid NotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"'{parameterName}' cannot be empty GUID.", parameterName);
        }
        return value;
    }
}
