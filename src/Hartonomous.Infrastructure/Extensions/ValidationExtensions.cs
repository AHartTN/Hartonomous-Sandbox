using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for validation and defensive programming.
/// Provides consistent parameter checking with standardized error messages.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Throws ArgumentNullException if value is null.
    /// </summary>
    public static T ThrowIfNull<T>(
        [NotNull] this T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if string is null, empty, or whitespace.
    /// </summary>
    public static string ThrowIfNullOrWhiteSpace(
        [NotNull] this string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if collection is null or empty.
    /// </summary>
    public static TCollection ThrowIfNullOrEmpty<TCollection>(
        [NotNull] this TCollection? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TCollection : System.Collections.IEnumerable
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!value.GetEnumerator().MoveNext())
        {
            throw new ArgumentException("Collection cannot be empty.", paramName);
        }

        return value;
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is less than minimum.
    /// </summary>
    public static T ThrowIfLessThan<T>(
        this T value,
        T minimum,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Value must be greater than or equal to {minimum}.");
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is greater than maximum.
    /// </summary>
    public static T ThrowIfGreaterThan<T>(
        this T value,
        T maximum,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(maximum) > 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Value must be less than or equal to {maximum}.");
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is not within range.
    /// </summary>
    public static T ThrowIfNotInRange<T>(
        this T value,
        T minimum,
        T maximum,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0 || value.CompareTo(maximum) > 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Value must be between {minimum} and {maximum}.");
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if condition is false.
    /// </summary>
    public static void ThrowIfFalse(
        [DoesNotReturnIf(false)] this bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))] string? paramName = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if condition is true.
    /// </summary>
    public static void ThrowIfTrue(
        [DoesNotReturnIf(true)] this bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))] string? paramName = null)
    {
        if (condition)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    /// <summary>
    /// Throws InvalidOperationException if value is null (for state validation).
    /// </summary>
    public static T ThrowIfNullState<T>(
        [NotNull] this T? value,
        string? message = null,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        if (value is null)
        {
            throw new InvalidOperationException(
                message ?? $"Required state '{expression}' is null.");
        }
        return value;
    }

    /// <summary>
    /// Throws InvalidOperationException if condition is false (for state validation).
    /// </summary>
    public static void ThrowIfInvalidState(
        [DoesNotReturnIf(false)] this bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Validates Guid is not empty.
    /// </summary>
    public static Guid ThrowIfEmpty(
        this Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Guid cannot be empty.", paramName);
        }
        return value;
    }

    /// <summary>
    /// Validates file path exists.
    /// </summary>
    public static string ThrowIfFileNotExists(
        this string filePath,
        [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
    {
        filePath.ThrowIfNullOrWhiteSpace(paramName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }
        return filePath;
    }

    /// <summary>
    /// Validates directory path exists.
    /// </summary>
    public static string ThrowIfDirectoryNotExists(
        this string directoryPath,
        [CallerArgumentExpression(nameof(directoryPath))] string? paramName = null)
    {
        directoryPath.ThrowIfNullOrWhiteSpace(paramName);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }
        return directoryPath;
    }

    /// <summary>
    /// Validates enum value is defined.
    /// </summary>
    public static TEnum ThrowIfNotDefined<TEnum>(
        this TEnum value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Value {value} is not defined in enum {typeof(TEnum).Name}.");
        }
        return value;
    }
}
