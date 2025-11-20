namespace Hartonomous.Core.Validation;

/// <summary>
/// Provides centralized parameter validation using Guard clause pattern.
/// Throws appropriate exceptions for invalid inputs.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures the value is not null.
    /// </summary>
    public static T NotNull<T>(T value, string parameterName, string? message = null)
        where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, message ?? $"{parameterName} cannot be null.");
        }
        return value;
    }

    /// <summary>
    /// Ensures the string is not null, empty, or whitespace.
    /// </summary>
    public static string NotNullOrWhiteSpace(string? value, string parameterName, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message ?? $"{parameterName} cannot be null, empty, or whitespace.", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Ensures the collection is not null or empty.
    /// </summary>
    public static T NotNullOrEmpty<T>(T? collection, string parameterName, string? message = null)
        where T : System.Collections.IEnumerable
    {
        if (collection == null)
        {
            throw new ArgumentNullException(parameterName, message ?? $"{parameterName} cannot be null.");
        }
        
        if (!collection.GetEnumerator().MoveNext())
        {
            throw new ArgumentException(message ?? $"{parameterName} cannot be empty.", parameterName);
        }
        
        return collection;
    }

    /// <summary>
    /// Ensures the value is greater than zero.
    /// </summary>
    public static T Positive<T>(T value, string parameterName, string? message = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default(T)) <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, 
                message ?? $"{parameterName} must be greater than zero.");
        }
        return value;
    }

    /// <summary>
    /// Ensures the value is greater than or equal to zero.
    /// </summary>
    public static T NotNegative<T>(T value, string parameterName, string? message = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default(T)) < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, 
                message ?? $"{parameterName} cannot be negative.");
        }
        return value;
    }

    /// <summary>
    /// Ensures the value is within the specified range (inclusive).
    /// </summary>
    public static T InRange<T>(T value, T min, T max, string parameterName, string? message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value,
                message ?? $"{parameterName} must be between {min} and {max}.");
        }
        return value;
    }

    /// <summary>
    /// Ensures the condition is true.
    /// </summary>
    public static void That(bool condition, string parameterName, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    /// <summary>
    /// Ensures the file exists at the specified path.
    /// </summary>
    public static string FileExists(string path, string parameterName, string? message = null)
    {
        NotNullOrWhiteSpace(path, parameterName);
        
        if (!System.IO.File.Exists(path))
        {
            throw new System.IO.FileNotFoundException(
                message ?? $"File not found: {path}", path);
        }
        
        return path;
    }

    /// <summary>
    /// Ensures the directory exists at the specified path.
    /// </summary>
    public static string DirectoryExists(string path, string parameterName, string? message = null)
    {
        NotNullOrWhiteSpace(path, parameterName);
        
        if (!System.IO.Directory.Exists(path))
        {
            throw new System.IO.DirectoryNotFoundException(
                message ?? $"Directory not found: {path}");
        }
        
        return path;
    }

    /// <summary>
    /// Ensures the value is of the expected type.
    /// </summary>
    public static T OfType<T>(object value, string parameterName, string? message = null)
    {
        NotNull(value, parameterName);
        
        if (value is not T typedValue)
        {
            throw new ArgumentException(
                message ?? $"{parameterName} must be of type {typeof(T).Name}, but was {value.GetType().Name}.",
                parameterName);
        }
        
        return typedValue;
    }
}
