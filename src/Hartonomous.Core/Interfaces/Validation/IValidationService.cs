using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Interfaces.Validation;

/// <summary>
/// Service for validating request models and converting validation errors to problem details.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a model and returns validation results.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="model">The model instance to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    ValidationResult Validate<T>(T model) where T : class;

    /// <summary>
    /// Validates a model and throws ValidationException if invalid.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="model">The model instance to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    void ValidateAndThrow<T>(T model) where T : class;
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IEnumerable<ValidationError> Errors { get; set; } = Array.Empty<ValidationError>();
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets the property name that failed validation.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets the attempted value that failed validation.
    /// </summary>
    public object? AttemptedValue { get; set; }
}

/// <summary>
/// Exception thrown when model validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors that caused this exception.
    /// </summary>
    public IEnumerable<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationException class.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base("Model validation failed")
    {
        Errors = errors;
    }
}