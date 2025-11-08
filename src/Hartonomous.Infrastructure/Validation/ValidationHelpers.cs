using System;

namespace Hartonomous.Infrastructure.Validation;

/// <summary>
/// Helper methods for common validation patterns.
/// Eliminates 200+ instances of repeated validation code.
/// </summary>
public static class ValidationHelpers
{
    /// <summary>
    /// Validates that a string value is not null or whitespace.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or whitespace.</exception>
    /// <example>
    /// ValidationHelpers.ValidateRequired(prompt, nameof(prompt));
    /// </example>
    public static void ValidateRequired(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
    }

    /// <summary>
    /// Validates that an object is not null.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static void ValidateNotNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName, $"{paramName} cannot be null.");
    }

    /// <summary>
    /// Validates that a numeric value is positive (greater than 0).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if value is not positive.</exception>
    public static void ValidatePositive(long value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0. Actual value: {value}", paramName);
    }

    /// <summary>
    /// Validates that a numeric value is positive (greater than 0).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if value is not positive.</exception>
    public static void ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0. Actual value: {value}", paramName);
    }

    /// <summary>
    /// Validates that a numeric value is non-negative (greater than or equal to 0).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if value is negative.</exception>
    public static void ValidateNonNegative(long value, string paramName)
    {
        if (value < 0)
            throw new ArgumentException($"{paramName} cannot be negative. Actual value: {value}", paramName);
    }

    /// <summary>
    /// Validates that a numeric value is non-negative (greater than or equal to 0).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if value is negative.</exception>
    public static void ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
            throw new ArgumentException($"{paramName} cannot be negative. Actual value: {value}", paramName);
    }

    /// <summary>
    /// Validates that a value falls within a specified range (inclusive).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is outside the range.</exception>
    public static void ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"{paramName} must be between {min} and {max}. Actual value: {value}");
    }

    /// <summary>
    /// Validates that a value falls within a specified range (inclusive).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is outside the range.</exception>
    public static void ValidateRange(long value, long min, long max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"{paramName} must be between {min} and {max}. Actual value: {value}");
    }

    /// <summary>
    /// Validates that a string does not exceed a maximum length.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if string exceeds max length.</exception>
    public static void ValidateMaxLength(string? value, int maxLength, string paramName)
    {
        if (value != null && value.Length > maxLength)
            throw new ArgumentException(
                $"{paramName} cannot exceed {maxLength} characters. Actual length: {value.Length}",
                paramName);
    }

    /// <summary>
    /// Validates that a GUID is not empty.
    /// </summary>
    /// <param name="value">The GUID to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if GUID is empty.</exception>
    public static void ValidateNotEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="paramName">The parameter name for exception message.</param>
    /// <exception cref="ArgumentException">Thrown if collection is null or empty.</exception>
    public static void ValidateNotNullOrEmpty<T>(System.Collections.Generic.IEnumerable<T>? collection, string paramName)
    {
        if (collection == null || !collection.Any())
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
    }

    /// <summary>
    /// Validates that a tenant ID is valid (greater than 0).
    /// Common pattern used throughout the codebase.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown if tenant ID is invalid.</exception>
    public static void ValidateTenantId(int tenantId)
    {
        ValidatePositive(tenantId, nameof(tenantId));
    }

    /// <summary>
    /// Validates that a model ID is valid (greater than 0).
    /// Common pattern used throughout the codebase.
    /// </summary>
    /// <param name="modelId">The model ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown if model ID is invalid.</exception>
    public static void ValidateModelId(int modelId)
    {
        ValidatePositive(modelId, nameof(modelId));
    }

    /// <summary>
    /// Validates that an atom ID is valid (greater than 0).
    /// Common pattern used throughout the codebase.
    /// </summary>
    /// <param name="atomId">The atom ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown if atom ID is invalid.</exception>
    public static void ValidateAtomId(long atomId)
    {
        ValidatePositive(atomId, nameof(atomId));
    }
}

/// <summary>
/// Extension methods for Linq.
/// </summary>
internal static class LinqExtensions
{
    public static bool Any<T>(this System.Collections.Generic.IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext();
    }
}
