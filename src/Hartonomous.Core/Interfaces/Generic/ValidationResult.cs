namespace Hartonomous.Core.Interfaces.Generic;

public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors, if any.
    /// </summary>
    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets any warnings from validation.
    /// </summary>
    public IEnumerable<string> Warnings { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Generic configuration interface for services.
/// Provides a consistent pattern for configuration management.
/// </summary>
/// <typeparam name="TConfig">The configuration type</typeparam>
