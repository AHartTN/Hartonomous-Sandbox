namespace Hartonomous.Shared.Contracts.Errors;

/// <summary>
/// Canonical error codes shared across clients and services.
/// </summary>
public static class ErrorCodes
{
    public static class Validation
    {
        public const string InvalidRequest = "validation.invalid_request";
        public const string MissingRequiredField = "validation.missing_required_field";
        public const string InvalidFieldValue = "validation.invalid_field_value";
    }

    public static class NotFound
    {
        public const string Resource = "not_found.resource";
        public const string Document = "not_found.document";
    }

    public static class Conflict
    {
        public const string ResourceConflict = "conflict.resource";
    }

    public static class Infrastructure
    {
        public const string DatabaseUnavailable = "infrastructure.database_unavailable";
        public const string ExternalDependencyFailure = "infrastructure.external_dependency_failure";
        public const string OperationFailed = "infrastructure.operation_failed";
    }

    public static class System
    {
        public const string Unhandled = "system.unhandled";
    }

    public static class Security
    {
        public const string Unauthorized = "security.unauthorized";
        public const string Forbidden = "security.forbidden";
    }
}
