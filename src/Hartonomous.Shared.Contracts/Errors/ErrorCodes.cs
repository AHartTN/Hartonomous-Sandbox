namespace Hartonomous.Shared.Contracts.Errors;

/// <summary>
/// Canonical error codes shared across clients and services.
/// Aggregates all error code categories for backward compatibility.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Validation error codes
    /// </summary>
    public static class Validation
    {
        public const string InvalidRequest = ValidationErrors.InvalidRequest;
        public const string MissingRequiredField = ValidationErrors.MissingRequiredField;
        public const string InvalidFieldValue = ValidationErrors.InvalidFieldValue;
    }

    /// <summary>
    /// Not found error codes
    /// </summary>
    public static class NotFound
    {
        public const string Resource = NotFoundErrors.Resource;
        public const string Document = NotFoundErrors.Document;
    }

    /// <summary>
    /// Conflict error codes
    /// </summary>
    public static class Conflict
    {
        public const string ResourceConflict = ConflictErrors.ResourceConflict;
    }

    /// <summary>
    /// Infrastructure error codes
    /// </summary>
    public static class Infrastructure
    {
        public const string DatabaseUnavailable = InfrastructureErrors.DatabaseUnavailable;
        public const string ExternalDependencyFailure = InfrastructureErrors.ExternalDependencyFailure;
        public const string OperationFailed = InfrastructureErrors.OperationFailed;
    }

    /// <summary>
    /// System error codes
    /// </summary>
    public static class System
    {
        public const string Unhandled = SystemErrors.Unhandled;
    }

    /// <summary>
    /// Security error codes
    /// </summary>
    public static class Security
    {
        public const string Unauthorized = SecurityErrors.Unauthorized;
        public const string Forbidden = SecurityErrors.Forbidden;
    }
}
