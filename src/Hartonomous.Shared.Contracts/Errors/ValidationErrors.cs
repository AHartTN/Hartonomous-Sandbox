namespace Hartonomous.Shared.Contracts.Errors;

/// <summary>
/// Validation error codes.
/// </summary>
public static class ValidationErrors
{
    public const string InvalidRequest = "validation.invalid_request";
    public const string MissingRequiredField = "validation.missing_required_field";
    public const string InvalidFieldValue = "validation.invalid_field_value";
}
