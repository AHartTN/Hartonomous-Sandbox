namespace Hartonomous.Core.Interfaces.Generic;

public interface IValidator<T>
{
    /// <summary>
    /// Validate a single object.
    /// </summary>
    /// <param name="obj">The object to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(T obj);

    /// <summary>
    /// Validate multiple objects.
    /// </summary>
    /// <param name="objects">The objects to validate</param>
    /// <returns>Collection of validation results</returns>
    IEnumerable<ValidationResult> ValidateBatch(IEnumerable<T> objects);
}

/// <summary>
/// Result of a validation operation.
/// </summary>
