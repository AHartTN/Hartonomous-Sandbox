using System.ComponentModel.DataAnnotations;
using Hartonomous.Core.Interfaces.Validation;

namespace Hartonomous.Infrastructure.Services.Validation;

/// <summary>
/// Default implementation of IValidationService using DataAnnotations.
/// </summary>
public class ValidationService : IValidationService
{
    /// <summary>
    /// Validates a model using DataAnnotations and returns validation results.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="model">The model instance to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    public Hartonomous.Core.Interfaces.Validation.ValidationResult Validate<T>(T model) where T : class
    {
        if (model == null)
        {
            return new Hartonomous.Core.Interfaces.Validation.ValidationResult
            {
                IsValid = false,
                Errors = new[]
                {
                    new Hartonomous.Core.Interfaces.Validation.ValidationError
                    {
                        PropertyName = string.Empty,
                        ErrorMessage = "Model cannot be null",
                        AttemptedValue = null
                    }
                }
            };
        }

        var validationContext = new ValidationContext(model);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        if (isValid)
        {
            return new Hartonomous.Core.Interfaces.Validation.ValidationResult { IsValid = true };
        }

        var errors = validationResults.Select(r => new Hartonomous.Core.Interfaces.Validation.ValidationError
        {
            PropertyName = r.MemberNames.FirstOrDefault() ?? string.Empty,
            ErrorMessage = r.ErrorMessage ?? "Validation failed",
            AttemptedValue = GetPropertyValue(model, r.MemberNames.FirstOrDefault())
        });

        return new Hartonomous.Core.Interfaces.Validation.ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Validates a model and throws ValidationException if invalid.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="model">The model instance to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public void ValidateAndThrow<T>(T model) where T : class
    {
        var result = Validate(model);
        if (!result.IsValid)
        {
            throw new Hartonomous.Core.Interfaces.Validation.ValidationException(result.Errors);
        }
    }

    private static object? GetPropertyValue(object obj, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }
}