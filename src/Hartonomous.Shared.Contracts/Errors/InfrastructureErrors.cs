namespace Hartonomous.Shared.Contracts.Errors;

/// <summary>
/// Infrastructure-related error codes.
/// </summary>
public static class InfrastructureErrors
{
    public const string DatabaseUnavailable = "infrastructure.database_unavailable";
    public const string ExternalDependencyFailure = "infrastructure.external_dependency_failure";
    public const string OperationFailed = "infrastructure.operation_failed";
}
